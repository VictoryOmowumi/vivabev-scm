using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using VivaBev.Catalogue.Contracts;
using VivaBev.Identity.Contracts;
using VivaBev.Inventory.Application;
using VivaBev.Inventory.Contracts;
using VivaBev.Inventory.Domain;
using VivaBev.Inventory.Infrastructure;
using VivaBev.Procurement.Contracts;
using VivaBev.SharedKernel.Security;
using VivaBev.Suppliers.Contracts;

namespace VivaBev.Inventory.Endpoints;

internal static class InventoryEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory").WithTags("Inventory")
            .RequireAuthorization(Permissions.Inventory.View);

        // Orders due in: issued or partly received
        group.MapGet("/receivable-orders", async (IPurchaseOrderReceiving orders, ISupplierQueries suppliers,
            IOrganisationQueries organisation, CancellationToken ct) =>
        {
            var due = await orders.GetReceivableAsync(ct);
            var names = await suppliers.GetNamesAsync(due.Select(o => o.SupplierId).ToList(), ct);
            var sites = await SitesAsync(organisation, due.Select(o => o.DeliverToSiteId), ct);

            return due.Select(o => new ReceivableOrderSummaryDto(
                o.Id, o.Number, o.Status, o.SupplierId, names.GetValueOrDefault(o.SupplierId) ?? "(unknown supplier)",
                o.DeliverToSiteId, sites.GetValueOrDefault(o.DeliverToSiteId)?.Code ?? "?",
                sites.GetValueOrDefault(o.DeliverToSiteId)?.Name ?? "(unknown site)", o.ExpectedDelivery,
                o.Lines.Count(l => l.Outstanding > 0), o.Lines.Count, o.Total)).ToList();
        });

        group.MapGet("/receivable-orders/{id:guid}", async (Guid id, IPurchaseOrderReceiving orders,
            ISupplierQueries suppliers, IOrganisationQueries organisation, ICatalogueQueries catalogue,
            IUserDirectory users, InventoryDbContext db, CancellationToken ct) =>
        {
            var o = await orders.GetOrderAsync(id, ct);
            if (o is null)
                return Results.NotFound();

            var supplier = (await suppliers.GetNamesAsync([o.SupplierId], ct)).GetValueOrDefault(o.SupplierId) ?? "(unknown supplier)";
            var site = await organisation.GetSiteAsync(o.DeliverToSiteId, ct);
            var stocked = new HashSet<Guid>();
            foreach (var itemId in o.Lines.Where(l => l.ItemId is not null).Select(l => l.ItemId!.Value).Distinct())
                if ((await catalogue.GetItemAsync(itemId, ct)) is { IsStocked: true })
                    stocked.Add(itemId);

            var receipts = await db.GoodsReceipts.AsNoTracking().Include(r => r.Lines)
                .Where(r => r.PurchaseOrderId == id).OrderByDescending(r => r.ReceivedAt).ToListAsync(ct);

            return Results.Ok(new ReceivableOrderDetailDto(
                o.Id, o.Number, o.Status, o.SupplierId, supplier, o.DeliverToSiteId, site?.Code ?? "?",
                site?.Name ?? "(unknown site)", o.ExpectedDelivery,
                o.Lines.Select(l => new ReceivableOrderLineDto(l.Id, l.LineNo, l.ItemCode, l.Description, l.UomCode,
                    l.Ordered, l.Received, l.Outstanding, l.ItemId is { } i && stocked.Contains(i))).ToList(),
                await SummariesAsync(receipts, suppliers, organisation, users, ct)));
        });

        group.MapGet("/receipts", async (Guid? purchaseOrderId, InventoryDbContext db, ISupplierQueries suppliers,
            IOrganisationQueries organisation, IUserDirectory users, CancellationToken ct) =>
        {
            var query = db.GoodsReceipts.AsNoTracking().Include(r => r.Lines).AsQueryable();
            if (purchaseOrderId is not null)
                query = query.Where(r => r.PurchaseOrderId == purchaseOrderId);

            var receipts = await query.OrderByDescending(r => r.Number).Take(200).ToListAsync(ct);
            return await SummariesAsync(receipts, suppliers, organisation, users, ct);
        });

        group.MapGet("/receipts/{id:guid}", async (Guid id, InventoryDbContext db, ISupplierQueries suppliers,
            IOrganisationQueries organisation, IUserDirectory users, CancellationToken ct) =>
        {
            var r = await db.GoodsReceipts.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (r is null)
                return Results.NotFound();

            var supplier = (await suppliers.GetNamesAsync([r.SupplierId], ct)).GetValueOrDefault(r.SupplierId) ?? "(unknown supplier)";
            var site = await organisation.GetSiteAsync(r.SiteId, ct);
            var receiver = await users.GetUserAsync(r.ReceivedBy, ct);

            return Results.Ok(new GoodsReceiptDetailDto(
                r.Id, r.Number, r.PurchaseOrderId, r.PurchaseOrderNumber, r.SupplierId, supplier, site?.Code ?? "?",
                site?.Name ?? "(unknown site)", r.DeliveryNoteNumber, r.Notes, receiver?.DisplayName ?? "(unknown user)",
                r.ReceivedAt, r.AcceptedValue,
                r.Lines.OrderBy(l => l.LineNo)
                    .Select(l => new GoodsReceiptLineDto(l.LineNo, l.ItemCode, l.Description, l.UomCode,
                        l.QuantityAccepted, l.QuantityRejected, l.RejectionReason, l.UnitPrice, l.AcceptedValue))
                    .ToList()));
        });

        // Stock on hand per site and item: the sum of movements, valued at the average receipt cost
        group.MapGet("/stock", async (Guid? siteId, InventoryDbContext db, IOrganisationQueries organisation,
            CancellationToken ct) =>
        {
            var query = db.StockMovements.AsNoTracking();
            if (siteId is not null)
                query = query.Where(m => m.SiteId == siteId);

            // Grouped in memory: movements are few per item, and this keeps the query simple to translate
            var movements = await query.ToListAsync(ct);
            var rows = movements
                .GroupBy(m => new { m.SiteId, m.ItemId })
                .Select(g => new
                {
                    g.Key.SiteId, g.Key.ItemId,
                    g.OrderByDescending(m => m.OccurredAt).First().ItemCode,
                    g.OrderByDescending(m => m.OccurredAt).First().Description,
                    g.OrderByDescending(m => m.OccurredAt).First().UomCode,
                    OnHand = g.Sum(m => m.Quantity),
                    ReceivedQty = g.Where(m => m.Quantity > 0).Sum(m => m.Quantity),
                    ReceivedValue = g.Where(m => m.Quantity > 0).Sum(m => m.Quantity * m.UnitCost),
                    Last = g.Max(m => m.OccurredAt),
                })
                .ToList();

            var sites = await SitesAsync(organisation, rows.Select(r => r.SiteId), ct);
            return rows
                .Select(r =>
                {
                    var average = r.ReceivedQty == 0 ? 0 : decimal.Round(r.ReceivedValue / r.ReceivedQty, 2);
                    return new StockBalanceDto(r.SiteId, sites.GetValueOrDefault(r.SiteId)?.Code ?? "?", r.ItemId,
                        r.ItemCode, r.Description, r.UomCode, r.OnHand, average, decimal.Round(r.OnHand * average, 2), r.Last);
                })
                .OrderBy(r => r.SiteCode).ThenBy(r => r.ItemCode)
                .ToList();
        });

        group.MapPost("/receipts", async (ReceiveGoodsRequest request, GoodsReceiptService service, CancellationToken ct) =>
        {
            var (id, number) = await service.ReceiveAsync(request, ct);
            return Results.Created($"/api/inventory/receipts/{id}", new { id, number });
        }).RequireAuthorization(Permissions.Inventory.Receive);
    }

    private static async Task<Dictionary<Guid, SiteDto>> SitesAsync(IOrganisationQueries organisation,
        IEnumerable<Guid> ids, CancellationToken ct)
    {
        var sites = new Dictionary<Guid, SiteDto>();
        foreach (var id in ids.Distinct())
            if (await organisation.GetSiteAsync(id, ct) is { } site)
                sites[id] = site;
        return sites;
    }

    private static async Task<List<GoodsReceiptSummaryDto>> SummariesAsync(List<GoodsReceipt> receipts,
        ISupplierQueries suppliers, IOrganisationQueries organisation, IUserDirectory users, CancellationToken ct)
    {
        var names = await suppliers.GetNamesAsync(receipts.Select(r => r.SupplierId).ToList(), ct);
        var sites = await SitesAsync(organisation, receipts.Select(r => r.SiteId), ct);
        var receivers = await users.GetUsersAsync(receipts.Select(r => r.ReceivedBy).ToList(), ct);

        return receipts.Select(r => new GoodsReceiptSummaryDto(
            r.Id, r.Number, r.PurchaseOrderId, r.PurchaseOrderNumber,
            names.GetValueOrDefault(r.SupplierId) ?? "(unknown supplier)", sites.GetValueOrDefault(r.SiteId)?.Code ?? "?",
            r.DeliveryNoteNumber, receivers.GetValueOrDefault(r.ReceivedBy)?.DisplayName ?? "(unknown user)",
            r.ReceivedAt, r.Lines.Count, r.AcceptedValue, r.HasRejections)).ToList();
    }
}
