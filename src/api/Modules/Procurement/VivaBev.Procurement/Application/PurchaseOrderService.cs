using Microsoft.EntityFrameworkCore;
using VivaBev.Finance.Contracts;
using VivaBev.Identity.Contracts;
using VivaBev.Infrastructure.Persistence;
using VivaBev.Procurement.Contracts;
using VivaBev.Procurement.Domain.PurchaseOrders;
using VivaBev.Procurement.Domain.Requisitions;
using VivaBev.Procurement.Domain.Sourcing;
using VivaBev.Procurement.Infrastructure;
using VivaBev.SharedKernel.Abstractions;
using VivaBev.SharedKernel.Domain;
using VivaBev.SharedKernel.Errors;
using VivaBev.SharedKernel.Time;

namespace VivaBev.Procurement.Application;

/// <summary>
/// Raises purchase orders from approved awards and issues them. Issuing moves the budget from the requisitions'
/// estimates to the order's real prices, and marks the requisitions Ordered, in one transaction.
/// </summary>
internal sealed class PurchaseOrderService(
    ProcurementDbContext db,
    IDocumentNumberGenerator numbers,
    IOrganisationQueries organisation,
    IBudgets budgets,
    CrossModuleTransaction transaction,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public const string BudgetDocumentType = "PurchaseOrder";

    /// <summary>A draft order from the RFQ's approved award: one line per RFQ line, at the awarded prices.</summary>
    public async Task<(Guid Id, string Number)> CreateFromAwardAsync(Guid sourcingEventId, CancellationToken ct)
    {
        var buyerId = currentUser.UserId ?? throw new ForbiddenException("You must be signed in.");

        var rfq = await db.SourcingEvents.WithEverything().SingleOrDefaultAsync(e => e.Id == sourcingEventId, ct)
            ?? throw new NotFoundException($"RFQ {sourcingEventId} was not found.");
        if (rfq.Status != SourcingEventStatus.Awarded)
            throw new DomainException($"{rfq.Number} is {rfq.Status}; only an awarded RFQ can become a purchase order.");

        var existing = await db.PurchaseOrders.AsNoTracking()
            .Where(p => p.SourcingEventId == rfq.Id && p.Status != PurchaseOrderStatus.Cancelled)
            .Select(p => p.Number)
            .FirstOrDefaultAsync(ct);
        if (existing is not null)
            throw new DomainException($"{rfq.Number} already has purchase order {existing}.");

        var quotation = rfq.AwardQuotation!;
        var requisitionIds = rfq.RequisitionIds.ToList();
        var requisitions = await db.Requisitions.AsNoTracking()
            .Where(r => requisitionIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, ct);

        var lines = rfq.Lines.OrderBy(l => l.LineNo).Select(l =>
        {
            var priced = quotation.Lines.Single(q => q.SourcingEventLineId == l.Id);
            var requisition = requisitions[l.RequisitionId];
            return new OrderedLine(l.Id, l.RequisitionId, l.RequisitionLineId, l.RequisitionNumber,
                requisition.CostCentreId, requisition.BudgetFiscalYear ?? FiscalCalendar.FiscalYearOf(Now),
                l.ItemId, l.ItemCode, l.Description, priced.Quantity, l.UomCode, priced.UnitPrice);
        }).ToList();

        // Deliver where the first requisition asked; expect it after the quoted lead time
        var first = requisitions[rfq.Lines.OrderBy(l => l.LineNo).First().RequisitionId];
        var expected = Today.AddDays(Math.Max(quotation.LeadTimeDays, 1));

        var order = new PurchaseOrder(await numbers.NextAsync("PO", ct), quotation.SupplierId, rfq.Id, rfq.Number,
            quotation.Id, quotation.SupplierReference, buyerId, quotation.PaymentTermsDays, first.DeliverToSiteId,
            expected, lines);

        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync(ct);
        return (order.Id, order.Number);
    }

    public async Task UpdateDeliveryAsync(Guid id, UpdatePurchaseOrderDeliveryRequest request, CancellationToken ct)
    {
        var order = await LoadAsync(id, ct);
        var site = await organisation.GetSiteAsync(request.DeliverToSiteId, ct);
        if (site is null || !site.IsActive)
            throw new DomainException("Choose an active delivery site.");

        order.UpdateDelivery(request.DeliverToSiteId, request.ExpectedDelivery, request.Notes, Today);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Sends the order: requisitions become Ordered, their estimate commitments are released, and the order's
    /// real amounts are committed, one commitment per cost centre and fiscal year.
    /// </summary>
    public Task IssueAsync(Guid id, CancellationToken ct) =>
        transaction.RunAsync(async () =>
        {
            var order = await LoadAsync(id, ct);
            order.Issue(Now);

            var requisitionIds = order.RequisitionIds.ToList();
            var requisitions = await db.Requisitions.Where(r => requisitionIds.Contains(r.Id)).ToListAsync(ct);
            foreach (var requisition in requisitions)
                requisition.MarkOrdered();
            await db.SaveChangesAsync(ct);

            foreach (var requisition in requisitions)
                await budgets.ReleaseAsync(Requisition.ApprovalDocumentType, requisition.Id, ct);

            foreach (var share in order.Lines.GroupBy(l => new { l.CostCentreId, l.BudgetFiscalYear }))
                await budgets.CommitAsync(new BudgetCommitmentRequest(share.Key.CostCentreId, share.Key.BudgetFiscalYear,
                    BudgetDocumentType, order.Id, order.Number, share.Sum(l => l.LineTotal)), ct);
        }, ct);

    /// <summary>Drops a draft. The RFQ stays awarded, so a new order can be raised from it.</summary>
    public async Task CancelAsync(Guid id, string reason, CancellationToken ct)
    {
        var order = await LoadAsync(id, ct);
        order.Cancel(reason);
        await db.SaveChangesAsync(ct);
    }

    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private DateOnly Today => DateOnly.FromDateTime(Now);

    private async Task<PurchaseOrder> LoadAsync(Guid id, CancellationToken ct) =>
        await db.PurchaseOrders.Include(p => p.Lines).SingleOrDefaultAsync(p => p.Id == id, ct)
        ?? throw new NotFoundException($"Purchase order {id} was not found.");
}
