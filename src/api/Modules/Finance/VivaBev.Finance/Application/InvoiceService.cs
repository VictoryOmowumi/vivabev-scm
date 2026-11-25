using Microsoft.EntityFrameworkCore;
using VivaBev.Finance.Contracts;
using VivaBev.Finance.Domain;
using VivaBev.Finance.Domain.Invoices;
using VivaBev.Finance.Infrastructure;
using VivaBev.Procurement.Contracts;
using VivaBev.SharedKernel.Abstractions;
using VivaBev.SharedKernel.Domain;
using VivaBev.SharedKernel.Errors;

namespace VivaBev.Finance.Application;

/// <summary>
/// Records supplier invoices and three-way matches them. When an invoice is approved for payment (by matching, or
/// by Finance overriding a hold), its amount moves from the order's budget commitment to spent.
/// </summary>
internal sealed class InvoiceService(
    FinanceDbContext db,
    IPurchaseOrderReceiving orders,
    IDocumentNumberGenerator numbers,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public const string BudgetDocumentType = "SupplierInvoice";
    private const string OrderCommitmentType = "PurchaseOrder"; // what PurchaseOrderService commits under

    public async Task<(Guid Id, string Number, string Status)> RecordAsync(RecordInvoiceRequest request, CancellationToken ct)
    {
        var me = Me();
        var order = await orders.GetOrderAsync(request.PurchaseOrderId, ct)
            ?? throw new NotFoundException($"Purchase order {request.PurchaseOrderId} was not found.");
        if (order.Status is not ("Issued" or "PartiallyReceived" or "Received" or "Closed"))
            throw new DomainException($"{order.Number} is {order.Status}; only issued orders can be invoiced.");

        var supplierNumber = request.SupplierInvoiceNumber.Trim();
        if (await db.SupplierInvoices.AnyAsync(i => i.SupplierId == order.SupplierId
                && i.SupplierInvoiceNumber == supplierNumber && i.Status != InvoiceStatus.Rejected, ct))
            throw new DomainException($"Invoice {supplierNumber} from this supplier has already been recorded.");
        if (request.Lines.Select(l => l.PurchaseOrderLineId).Distinct().Count() != request.Lines.Count)
            throw new DomainException("A line appears twice.");

        var lines = request.Lines.Select(l =>
        {
            var line = order.Lines.SingleOrDefault(x => x.Id == l.PurchaseOrderLineId)
                ?? throw new DomainException("A line is not on this purchase order.");
            return new InvoicedLine(line.Id, line.Description, line.UomCode, l.Quantity, l.UnitPrice, line.UnitPrice,
                line.CostCentreId, line.BudgetFiscalYear);
        }).ToList();

        var invoice = new SupplierInvoice(await numbers.NextAsync("INV", ct), order.SupplierId, supplierNumber, order.Id,
            order.Number, request.InvoiceDate, order.PaymentTermsDays, request.VatAmount, me, Now, lines);
        db.SupplierInvoices.Add(invoice);

        await MatchAsync(invoice, order, ct);
        await db.SaveChangesAsync(ct);
        return (invoice.Id, invoice.Number, invoice.Status.ToString());
    }

    /// <summary>Tries a held invoice again, e.g. after the rest of the goods have been received.</summary>
    public async Task<string> RematchAsync(Guid id, CancellationToken ct)
    {
        var invoice = await LoadAsync(id, ct);
        var order = await orders.GetOrderAsync(invoice.PurchaseOrderId, ct)
            ?? throw new NotFoundException($"Purchase order {invoice.PurchaseOrderId} was not found.");
        await MatchAsync(invoice, order, ct);
        await db.SaveChangesAsync(ct);
        return invoice.Status.ToString();
    }

    public async Task ApproveAnywayAsync(Guid id, string reason, CancellationToken ct)
    {
        var invoice = await LoadAsync(id, ct);
        invoice.ApproveAnyway(reason, Me(), Now);
        await ConsumeBudgetAsync(invoice, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task RejectAsync(Guid id, string reason, CancellationToken ct)
    {
        var invoice = await LoadAsync(id, ct);
        invoice.Reject(reason, Me(), Now);
        await db.SaveChangesAsync(ct);
    }

    public async Task PayAsync(Guid id, PayInvoiceRequest request, CancellationToken ct)
    {
        var invoice = await LoadAsync(id, ct);
        invoice.MarkPaid(request.PaymentReference, request.PaidOn, Now);
        await db.SaveChangesAsync(ct);
    }

    // ---------- helpers ----------

    private async Task MatchAsync(SupplierInvoice invoice, ReceivableOrderDto order, CancellationToken ct)
    {
        // What other live invoices on this order already bill, per order line
        var billed = await db.SupplierInvoices.AsNoTracking()
            .Where(i => i.PurchaseOrderId == order.Id && i.Id != invoice.Id && i.Status != InvoiceStatus.Rejected)
            .SelectMany(i => i.Lines)
            .GroupBy(l => l.PurchaseOrderLineId)
            .Select(g => new { LineId = g.Key, Quantity = g.Sum(l => l.Quantity) })
            .ToDictionaryAsync(x => x.LineId, x => x.Quantity, ct);

        var facts = order.Lines.ToDictionary(l => l.Id,
            l => new LineFacts(l.Received, billed.GetValueOrDefault(l.Id)));

        if (invoice.Match(facts, Now))
            await ConsumeBudgetAsync(invoice, ct);
    }

    /// <summary>Moves the invoice's amount (before VAT) from the order's commitment to spent, per cost centre and year.</summary>
    private async Task ConsumeBudgetAsync(SupplierInvoice invoice, CancellationToken ct)
    {
        foreach (var share in invoice.Lines.GroupBy(l => new { l.CostCentreId, l.BudgetFiscalYear }))
        {
            var budget = await db.Budgets.Include(b => b.Commitments)
                .SingleOrDefaultAsync(b => b.CostCentreId == share.Key.CostCentreId && b.FiscalYear == share.Key.BudgetFiscalYear, ct);
            if (budget is null)
            {
                budget = new Budget(share.Key.CostCentreId, share.Key.BudgetFiscalYear, 0m); // unbudgeted spend still shows
                db.Budgets.Add(budget);
            }
            budget.Consume(OrderCommitmentType, invoice.PurchaseOrderId, BudgetDocumentType, invoice.Id, invoice.Number,
                share.Sum(l => l.LineTotal), Now);
        }
    }

    private Guid Me() => currentUser.UserId ?? throw new ForbiddenException("You must be signed in.");
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    private async Task<SupplierInvoice> LoadAsync(Guid id, CancellationToken ct) =>
        await db.SupplierInvoices.Include(i => i.Lines).SingleOrDefaultAsync(i => i.Id == id, ct)
        ?? throw new NotFoundException($"Invoice {id} was not found.");
}
