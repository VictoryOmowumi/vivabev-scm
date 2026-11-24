using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using VivaBev.Identity.Contracts;
using VivaBev.Procurement.Application;
using VivaBev.Procurement.Contracts;
using VivaBev.Procurement.Domain.PurchaseOrders;
using VivaBev.Procurement.Infrastructure;
using VivaBev.SharedKernel.Security;
using VivaBev.Suppliers.Contracts;

namespace VivaBev.Procurement.Endpoints;

internal static class PurchaseOrderEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // Procurement raises and issues orders; Finance, the MD and the warehouse can look
        var group = app.MapGroup("/api/purchase-orders").WithTags("Purchase orders")
            .RequireAuthorization(Permissions.PurchaseOrders.View);

        group.MapGet("/", async (string? status, ProcurementDbContext db, ISupplierQueries suppliers,
            CancellationToken ct) =>
        {
            var query = db.PurchaseOrders.AsNoTracking().Include(p => p.Lines).AsQueryable();
            if (Enum.TryParse<PurchaseOrderStatus>(status, ignoreCase: true, out var wanted))
                query = query.Where(p => p.Status == wanted);

            var orders = await query.OrderByDescending(p => p.Number).Take(200).ToListAsync(ct);
            var names = await suppliers.GetNamesAsync(orders.Select(p => p.SupplierId).ToList(), ct);

            return orders.Select(p => new PurchaseOrderSummaryDto(
                p.Id, p.Number, p.Status.ToString(), p.SupplierId,
                names.GetValueOrDefault(p.SupplierId) ?? "(unknown supplier)",
                p.SourcingEventId, p.SourcingEventNumber, p.Total, p.ExpectedDelivery, p.IssuedAt, p.Lines.Count,
                p.Lines.Select(l => l.RequisitionNumber).Distinct().Order().ToList())).ToList();
        });

        group.MapGet("/{id:guid}", async (Guid id, ProcurementDbContext db, ISupplierQueries suppliers,
            IOrganisationQueries organisation, IUserDirectory users, CancellationToken ct) =>
        {
            var p = await db.PurchaseOrders.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (p is null)
                return Results.NotFound();

            var party = await suppliers.GetPartyAsync(p.SupplierId, ct);
            var site = await organisation.GetSiteAsync(p.DeliverToSiteId, ct);
            var buyer = await users.GetUserAsync(p.BuyerId, ct);
            var costCentres = (await organisation.GetCostCentresAsync(ct)).ToDictionary(c => c.Id, c => c.Code);

            return Results.Ok(new PurchaseOrderDetailDto(
                p.Id, p.Number, p.Status.ToString(),
                party is null
                    ? new PurchaseOrderSupplierDto(p.SupplierId, "?", "(unknown supplier)", "(unknown supplier)", "?", null, null, null, null)
                    : new PurchaseOrderSupplierDto(party.Id, party.Code, party.Name, party.LegalName, party.TaxId,
                        party.Address, party.ContactName, party.ContactEmail, party.ContactPhone),
                p.SourcingEventId, p.SourcingEventNumber, p.SupplierReference, buyer?.DisplayName ?? "(unknown user)",
                p.PaymentTermsDays,
                site is null
                    ? new PurchaseOrderSiteDto(p.DeliverToSiteId, "?", "(unknown site)", "", "")
                    : new PurchaseOrderSiteDto(site.Id, site.Code, site.Name, site.City, site.State),
                p.ExpectedDelivery, p.Notes, p.CurrencyCode, p.IssuedAt, p.CancellationReason, p.Total,
                p.Lines.OrderBy(l => l.LineNo)
                    .Select(l => new PurchaseOrderLineDto(l.Id, l.LineNo, l.RequisitionId, l.RequisitionNumber,
                        costCentres.GetValueOrDefault(l.CostCentreId) ?? "?", l.ItemCode, l.Description, l.Quantity,
                        l.UomCode, l.UnitPrice, l.LineTotal))
                    .ToList()));
        });

        // ---------- commands (buyers only) ----------

        group.MapPost("/", async (CreatePurchaseOrderRequest request, PurchaseOrderService service, CancellationToken ct) =>
        {
            var (id, number) = await service.CreateFromAwardAsync(request.SourcingEventId, ct);
            return Results.Created($"/api/purchase-orders/{id}", new { id, number });
        }).RequireAuthorization(Permissions.PurchaseOrders.Manage);

        group.MapPut("/{id:guid}/delivery", async (Guid id, UpdatePurchaseOrderDeliveryRequest request,
            PurchaseOrderService service, CancellationToken ct) =>
        {
            await service.UpdateDeliveryAsync(id, request, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.PurchaseOrders.Manage);

        group.MapPost("/{id:guid}/issue", async (Guid id, PurchaseOrderService service, CancellationToken ct) =>
        {
            await service.IssueAsync(id, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.PurchaseOrders.Manage);

        group.MapPost("/{id:guid}/cancel", async (Guid id, PurchaseOrderReasonRequest request,
            PurchaseOrderService service, CancellationToken ct) =>
        {
            await service.CancelAsync(id, request.Reason, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.PurchaseOrders.Manage);
    }
}
