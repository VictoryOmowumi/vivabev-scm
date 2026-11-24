using Microsoft.EntityFrameworkCore;
using VivaBev.Approvals.Contracts;
using VivaBev.Infrastructure.Persistence;
using VivaBev.Procurement.Contracts;
using VivaBev.Procurement.Domain.Requisitions;
using VivaBev.Procurement.Domain.Sourcing;
using VivaBev.Procurement.Infrastructure;
using VivaBev.SharedKernel.Abstractions;
using VivaBev.SharedKernel.Domain;
using VivaBev.SharedKernel.Errors;
using VivaBev.Suppliers.Contracts;

namespace VivaBev.Procurement.Application;

/// <summary>
/// Runs RFQs. Requisitions and sourcing events live in the same module and database schema, so moving a
/// requisition in or out of sourcing and changing the event are saved together in one SaveChanges.
/// </summary>
internal sealed class SourcingService(
    ProcurementDbContext db,
    IDocumentNumberGenerator numbers,
    ISupplierQueries suppliers,
    IApprovals approvals,
    CrossModuleTransaction transaction,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public async Task<(Guid Id, string Number)> CreateAsync(CreateSourcingEventRequest request, CancellationToken ct)
    {
        var buyerId = currentUser.UserId ?? throw new ForbiddenException("You must be signed in.");
        if (request.RequisitionIds.Count == 0)
            throw new DomainException("Choose at least one approved requisition.");

        var requisitions = await LoadRequisitionsAsync(request.RequisitionIds, ct);

        var number = await numbers.NextAsync("RFQ", ct);
        var sourcingEvent = new SourcingEvent(number, request.Title, buyerId, request.ResponseDeadline, Today);

        foreach (var requisition in requisitions)
        {
            sourcingEvent.AddLines(ToSourcedLines(requisition));
            requisition.MarkInSourcing();
        }

        db.SourcingEvents.Add(sourcingEvent);
        await db.SaveChangesAsync(ct);
        return (sourcingEvent.Id, sourcingEvent.Number);
    }

    public async Task AddRequisitionAsync(Guid eventId, Guid requisitionId, CancellationToken ct)
    {
        var sourcingEvent = await LoadAsync(eventId, ct);
        var requisition = (await LoadRequisitionsAsync([requisitionId], ct)).Single();

        sourcingEvent.AddLines(ToSourcedLines(requisition));
        requisition.MarkInSourcing();
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveRequisitionAsync(Guid eventId, Guid requisitionId, CancellationToken ct)
    {
        var sourcingEvent = await LoadAsync(eventId, ct);
        sourcingEvent.RemoveRequisition(requisitionId);

        var requisition = await db.Requisitions.SingleOrDefaultAsync(r => r.Id == requisitionId, ct)
            ?? throw new NotFoundException($"Requisition {requisitionId} was not found.");
        requisition.ReturnFromSourcing();
        await db.SaveChangesAsync(ct);
    }

    public async Task InviteAsync(Guid eventId, Guid supplierId, CancellationToken ct)
    {
        var sourcingEvent = await LoadAsync(eventId, ct);

        // Only suppliers approved for at least one of the RFQ's categories (and not suspended)
        var eligible = await suppliers.GetApprovedForCategoriesAsync(
            sourcingEvent.Lines.Select(l => l.CategoryId).ToList(), ct);
        if (eligible.All(s => s.Id != supplierId))
            throw new DomainException("That supplier isn't approved for any of the categories on this RFQ.");

        sourcingEvent.Invite(supplierId, Now);
        await db.SaveChangesAsync(ct);
    }

    public async Task UninviteAsync(Guid eventId, Guid supplierId, CancellationToken ct)
    {
        var sourcingEvent = await LoadAsync(eventId, ct);
        sourcingEvent.Uninvite(supplierId);
        await db.SaveChangesAsync(ct);
    }

    public async Task PublishAsync(Guid eventId, CancellationToken ct)
    {
        var sourcingEvent = await LoadAsync(eventId, ct);
        sourcingEvent.Publish(Now);
        await db.SaveChangesAsync(ct);
    }

    public async Task ChangeDeadlineAsync(Guid eventId, DateOnly deadline, CancellationToken ct)
    {
        var sourcingEvent = await LoadAsync(eventId, ct);
        sourcingEvent.ChangeDeadline(deadline, Today);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> RecordQuotationAsync(Guid eventId, RecordQuotationRequest request, CancellationToken ct)
    {
        var sourcingEvent = await LoadAsync(eventId, ct);

        // A supplier may only price lines in categories it's approved for
        var supplier = (await suppliers.GetApprovedForCategoriesAsync(
                sourcingEvent.Lines.Select(l => l.CategoryId).ToList(), ct))
            .SingleOrDefault(s => s.Id == request.SupplierId)
            ?? throw new DomainException("That supplier is no longer approved for this RFQ's categories.");

        foreach (var priced in request.Lines)
        {
            var line = sourcingEvent.Lines.SingleOrDefault(l => l.Id == priced.SourcingEventLineId)
                ?? throw new DomainException("A priced line is not part of this RFQ.");
            if (!supplier.ApprovedCategoryIds.Contains(line.CategoryId))
                throw new DomainException($"{supplier.Name} isn't approved to supply line {line.LineNo} ({line.Description}).");
        }

        var quotation = sourcingEvent.RecordQuotation(request.SupplierId, request.SupplierReference, request.ValidUntil,
            request.LeadTimeDays, request.PaymentTermsDays,
            request.Lines.Select(l => new QuotedLine(l.SourcingEventLineId, l.UnitPrice, l.Quantity, l.Notes)).ToList(),
            Now);

        await db.SaveChangesAsync(ct);
        return quotation.Id;
    }

    public async Task WithdrawQuotationAsync(Guid eventId, Guid quotationId, string reason, CancellationToken ct)
    {
        var sourcingEvent = await LoadAsync(eventId, ct);
        sourcingEvent.WithdrawQuotation(quotationId, reason);
        await db.SaveChangesAsync(ct);
    }

    public async Task RecordDeclineAsync(Guid eventId, Guid supplierId, CancellationToken ct)
    {
        var sourcingEvent = await LoadAsync(eventId, ct);
        sourcingEvent.RecordDecline(supplierId);
        await db.SaveChangesAsync(ct);
    }

    public async Task CloseResponsesAsync(Guid eventId, CancellationToken ct)
    {
        var sourcingEvent = await LoadAsync(eventId, ct);
        sourcingEvent.CloseResponses(Now);
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(Guid eventId, string reason, CancellationToken ct)
    {
        var sourcingEvent = await LoadAsync(eventId, ct);
        var requisitionIds = sourcingEvent.RequisitionIds.ToList();
        sourcingEvent.Cancel(reason);

        // Its requisitions go back to waiting for sourcing
        var requisitions = await db.Requisitions
            .Where(r => requisitionIds.Contains(r.Id) && r.Status == RequisitionStatus.InSourcing)
            .ToListAsync(ct);
        foreach (var requisition in requisitions)
            requisition.ReturnFromSourcing();

        await db.SaveChangesAsync(ct);
    }

    // ---------- evaluation and award ----------

    public async Task SetCriteriaAsync(Guid eventId, SetCriteriaRequest request, CancellationToken ct)
    {
        var sourcingEvent = await LoadAsync(eventId, ct);
        sourcingEvent.SetCriteria(request.Criteria
            .Select(c => new CriterionSpec(c.Id, c.Name, ParseKind(c.Kind), c.Weight))
            .ToList());
        await db.SaveChangesAsync(ct);
    }

    public async Task SetScoresAsync(Guid eventId, SetScoresRequest request, CancellationToken ct)
    {
        var sourcingEvent = await LoadAsync(eventId, ct);
        foreach (var score in request.Scores)
            sourcingEvent.Score(score.CriterionId, score.QuotationId, score.Score);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Records the recommendation and starts its approval, in one transaction.</summary>
    public Task RecommendAwardAsync(Guid eventId, RecommendAwardRequest request, CancellationToken ct) =>
        transaction.RunAsync(async () =>
        {
            var buyerId = currentUser.UserId ?? throw new ForbiddenException("You must be signed in.");
            var sourcingEvent = await LoadAsync(eventId, ct);
            sourcingEvent.RecommendAward(request.QuotationId, request.Justification, Now);
            await db.SaveChangesAsync(ct);

            var quotation = sourcingEvent.AwardQuotation!;
            var supplierName = (await suppliers.GetNamesAsync([quotation.SupplierId], ct))
                .GetValueOrDefault(quotation.SupplierId) ?? "supplier";

            // Awarding above the estimate means more than the requisitions committed, so Finance sees it too
            await approvals.StartAsync(new ApprovalSubject(
                SourcingEvent.AwardDocumentType, sourcingEvent.Id, sourcingEvent.Number,
                Truncate($"Award to {supplierName}: {sourcingEvent.Title}", 200), quotation.Total, buyerId,
                OverBudget: quotation.Total > sourcingEvent.EstimatedTotal), ct);
        }, ct);

    public Task WithdrawRecommendationAsync(Guid eventId, CancellationToken ct) =>
        transaction.RunAsync(async () =>
        {
            var sourcingEvent = await LoadAsync(eventId, ct);
            sourcingEvent.WithdrawRecommendation();
            await db.SaveChangesAsync(ct);
            await approvals.WithdrawAsync(SourcingEvent.AwardDocumentType, sourcingEvent.Id, ct);
        }, ct);

    // ---------- helpers ----------

    private static string Truncate(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";

    private static CriterionKind ParseKind(string kind) =>
        Enum.TryParse<CriterionKind>(kind, ignoreCase: true, out var parsed)
            ? parsed
            : throw new DomainException($"'{kind}' is not a way of scoring. Use Price, Delivery, PaymentTerms or Manual.");

    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private DateOnly Today => DateOnly.FromDateTime(Now);

    private async Task<SourcingEvent> LoadAsync(Guid id, CancellationToken ct) =>
        await db.SourcingEvents.WithEverything().SingleOrDefaultAsync(e => e.Id == id, ct)
        ?? throw new NotFoundException($"RFQ {id} was not found.");

    /// <summary>Tracked, with lines; every one must be approved and not already on an RFQ.</summary>
    private async Task<List<Requisition>> LoadRequisitionsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var wanted = ids.Distinct().ToList();
        var requisitions = await db.Requisitions
            .Include(r => r.Lines)
            .Where(r => wanted.Contains(r.Id))
            .ToListAsync(ct);

        if (requisitions.Count != wanted.Count)
            throw new NotFoundException("One of the requisitions was not found.");

        var notReady = requisitions.FirstOrDefault(r => r.Status != RequisitionStatus.Approved);
        if (notReady is not null)
            throw new DomainException($"{notReady.Number} is {notReady.Status}; only approved requisitions can be sourced.");

        return requisitions;
    }

    private static List<SourcedLine> ToSourcedLines(Requisition r) =>
        r.Lines.OrderBy(l => l.LineNo)
            .Select(l => new SourcedLine(r.Id, r.Number, l.Id, l.ItemId, l.ItemCode, l.Description, l.CategoryId,
                l.Quantity, l.UomCode, l.EstimatedUnitPrice))
            .ToList();
}
