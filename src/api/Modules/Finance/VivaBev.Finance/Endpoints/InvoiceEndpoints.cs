using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using VivaBev.Finance.Application;
using VivaBev.Finance.Contracts;
using VivaBev.Finance.Domain.Invoices;
using VivaBev.Finance.Infrastructure;
using VivaBev.Identity.Contracts;
using VivaBev.Procurement.Contracts;
using VivaBev.SharedKernel.Security;
using VivaBev.Suppliers.Contracts;

namespace VivaBev.Finance.Endpoints;

internal static class InvoiceEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // Finance records and pays; Procurement and the MD can look
        var group = app.MapGroup("/api/finance/invoices").WithTags("Invoices")
            .RequireAuthorization(Permissions.Finance.ViewInvoices);

        group.MapGet("/", async (string? status, Guid? purchaseOrderId, FinanceDbContext db, ISupplierQueries suppliers,
            CancellationToken ct) =>
        {
            var query = db.SupplierInvoices.AsNoTracking().Include(i => i.Lines).AsQueryable();
            if (Enum.TryParse<InvoiceStatus>(status, ignoreCase: true, out var wanted))
                query = query.Where(i => i.Status == wanted);
            if (purchaseOrderId is not null)
                query = query.Where(i => i.PurchaseOrderId == purchaseOrderId);

            var invoices = await query.OrderByDescending(i => i.Number).Take(200).ToListAsync(ct);
            var names = await suppliers.GetNamesAsync(invoices.Select(i => i.SupplierId).ToList(), ct);

            return invoices.Select(i => new InvoiceSummaryDto(
                i.Id, i.Number, i.Status.ToString(), i.SupplierId, names.GetValueOrDefault(i.SupplierId) ?? "(unknown supplier)",
                i.SupplierInvoiceNumber, i.PurchaseOrderId, i.PurchaseOrderNumber, i.InvoiceDate, i.DueDate, i.Total,
                i.Lines.Count(l => l.Exception is not null), i.RecordedAt)).ToList();
        });

        group.MapGet("/{id:guid}", async (Guid id, FinanceDbContext db, ISupplierQueries suppliers, IUserDirectory users,
            CancellationToken ct) =>
        {
            var i = await db.SupplierInvoices.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (i is null)
                return Results.NotFound();

            var supplier = (await suppliers.GetNamesAsync([i.SupplierId], ct)).GetValueOrDefault(i.SupplierId) ?? "(unknown supplier)";
            var ids = new List<Guid> { i.RecordedBy };
            if (i.DecidedBy is { } decider)
                ids.Add(decider);
            var people = await users.GetUsersAsync(ids, ct);

            return Results.Ok(new InvoiceDetailDto(
                i.Id, i.Number, i.Status.ToString(), i.SupplierId, supplier, i.SupplierInvoiceNumber, i.PurchaseOrderId,
                i.PurchaseOrderNumber, i.InvoiceDate, i.DueDate, i.Subtotal, i.VatAmount, i.Total, i.CurrencyCode,
                people.GetValueOrDefault(i.RecordedBy)?.DisplayName ?? "(unknown user)", i.RecordedAt, i.MatchedAt,
                i.DecisionReason, i.DecidedBy is { } by ? people.GetValueOrDefault(by)?.DisplayName : null, i.DecidedAt,
                i.PaymentReference, i.PaidOn,
                i.Lines.OrderBy(l => l.LineNo)
                    .Select(l => new InvoiceLineDto(l.LineNo, l.PurchaseOrderLineId, l.Description, l.UomCode, l.Quantity,
                        l.UnitPrice, l.LineTotal, l.OrderedUnitPrice, l.ReceivedAtMatch, l.InvoicedBeforeAtMatch, l.Exception))
                    .ToList()));
        });

        // Orders that can be invoiced, with received and already-billed quantities per line
        group.MapGet("/invoiceable-orders", async (IPurchaseOrderReceiving orders, FinanceDbContext db,
            ISupplierQueries suppliers, CancellationToken ct) =>
        {
            var open = await orders.GetInvoiceableAsync(ct);
            var ids = open.Select(o => o.Id).ToList();
            var billed = await db.SupplierInvoices.AsNoTracking()
                .Where(i => ids.Contains(i.PurchaseOrderId) && i.Status != InvoiceStatus.Rejected)
                .SelectMany(i => i.Lines)
                .GroupBy(l => l.PurchaseOrderLineId)
                .Select(g => new { LineId = g.Key, Quantity = g.Sum(l => l.Quantity), Amount = g.Sum(l => l.Quantity * l.UnitPrice) })
                .ToDictionaryAsync(x => x.LineId, ct);
            var names = await suppliers.GetNamesAsync(open.Select(o => o.SupplierId).ToList(), ct);

            return open.Select(o => new InvoiceableOrderDto(
                o.Id, o.Number, o.Status, o.SupplierId, names.GetValueOrDefault(o.SupplierId) ?? "(unknown supplier)",
                o.PaymentTermsDays, o.Total, decimal.Round(o.Lines.Sum(l => billed.GetValueOrDefault(l.Id)?.Amount ?? 0), 2),
                o.Lines.Select(l => new InvoiceableLineDto(l.Id, l.LineNo, l.ItemCode, l.Description, l.UomCode, l.Ordered,
                    l.Received, billed.GetValueOrDefault(l.Id)?.Quantity ?? 0, l.UnitPrice)).ToList())).ToList();
        });

        // ---------- commands (Finance only) ----------

        group.MapPost("/", async (RecordInvoiceRequest request, InvoiceService service, CancellationToken ct) =>
        {
            var (id, number, status) = await service.RecordAsync(request, ct);
            return Results.Created($"/api/finance/invoices/{id}", new { id, number, status });
        }).RequireAuthorization(Permissions.Finance.ManageInvoices);

        group.MapPost("/{id:guid}/rematch", async (Guid id, InvoiceService service, CancellationToken ct) =>
            Results.Ok(new { status = await service.RematchAsync(id, ct) }))
            .RequireAuthorization(Permissions.Finance.ManageInvoices);

        group.MapPost("/{id:guid}/approve", async (Guid id, InvoiceDecisionRequest request, InvoiceService service,
            CancellationToken ct) =>
        {
            await service.ApproveAnywayAsync(id, request.Reason, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Finance.ManageInvoices);

        group.MapPost("/{id:guid}/reject", async (Guid id, InvoiceDecisionRequest request, InvoiceService service,
            CancellationToken ct) =>
        {
            await service.RejectAsync(id, request.Reason, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Finance.ManageInvoices);

        group.MapPost("/{id:guid}/pay", async (Guid id, PayInvoiceRequest request, InvoiceService service,
            CancellationToken ct) =>
        {
            await service.PayAsync(id, request, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Finance.ManageInvoices);
    }
}
