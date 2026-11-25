using Microsoft.EntityFrameworkCore;
using VivaBev.Inventory.Domain;
using VivaBev.Inventory.Infrastructure;

namespace VivaBev.Inventory.Application;

/// <summary>One item's balance at one site, from its movements.</summary>
public sealed record StockBalance(Guid SiteId, Guid ItemId, string ItemCode, string Description, string UomCode,
    decimal OnHand, decimal AverageCost, DateTime LastMovementAt);

/// <summary>
/// Reads stock on hand from the movement ledger. Average cost is the weighted cost of everything that came in
/// (receipts and count gains); issues and losses go out at that average.
/// </summary>
internal sealed class StockLedger(InventoryDbContext db)
{
    public async Task<List<StockBalance>> BalancesAsync(Guid? siteId, IReadOnlyCollection<Guid>? itemIds, CancellationToken ct)
    {
        var query = db.StockMovements.AsNoTracking();
        if (siteId is not null)
            query = query.Where(m => m.SiteId == siteId);
        if (itemIds is not null)
            query = query.Where(m => itemIds.Contains(m.ItemId));

        // Grouped in memory: movements are few per item, and this keeps the query simple to translate
        var movements = await query.ToListAsync(ct);
        return movements
            .GroupBy(m => new { m.SiteId, m.ItemId })
            .Select(g =>
            {
                var latest = g.OrderByDescending(m => m.OccurredAt).First();
                var inQty = g.Where(m => m.Quantity > 0).Sum(m => m.Quantity);
                var inValue = g.Where(m => m.Quantity > 0).Sum(m => m.Quantity * m.UnitCost);
                return new StockBalance(g.Key.SiteId, g.Key.ItemId, latest.ItemCode, latest.Description, latest.UomCode,
                    g.Sum(m => m.Quantity), inQty == 0 ? 0 : decimal.Round(inValue / inQty, 2), latest.OccurredAt);
            })
            .ToList();
    }

    /// <summary>Balances at one site keyed by item.</summary>
    public async Task<Dictionary<Guid, StockBalance>> AtSiteAsync(Guid siteId, IReadOnlyCollection<Guid>? itemIds, CancellationToken ct) =>
        (await BalancesAsync(siteId, itemIds, ct)).ToDictionary(b => b.ItemId);
}
