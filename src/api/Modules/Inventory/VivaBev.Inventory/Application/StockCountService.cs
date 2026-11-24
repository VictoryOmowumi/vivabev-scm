using Microsoft.EntityFrameworkCore;
using VivaBev.Identity.Contracts;
using VivaBev.Inventory.Contracts;
using VivaBev.Inventory.Domain;
using VivaBev.Inventory.Domain.Stock;
using VivaBev.Inventory.Infrastructure;
using VivaBev.SharedKernel.Abstractions;
using VivaBev.SharedKernel.Domain;
using VivaBev.SharedKernel.Errors;

namespace VivaBev.Inventory.Application;

/// <summary>Starts, records and posts stock counts. Posting writes one adjustment per difference.</summary>
internal sealed class StockCountService(
    InventoryDbContext db,
    StockLedger ledger,
    IOrganisationQueries organisation,
    IDocumentNumberGenerator numbers,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public async Task<(Guid Id, string Number)> StartAsync(StartStockCountRequest request, CancellationToken ct)
    {
        var site = await organisation.GetSiteAsync(request.SiteId, ct) ?? throw new DomainException("Choose a site.");
        if (await db.StockCounts.AnyAsync(c => c.SiteId == site.Id && c.Status == StockCountStatus.Open, ct))
            throw new DomainException($"A count is already open at {site.Name}. Post or cancel it first.");

        // Everything the ledger has seen at the site, including items now at zero: those are worth a look too
        var expected = (await ledger.BalancesAsync(site.Id, null, ct))
            .Select(b => new ExpectedStock(b.ItemId, b.ItemCode, b.Description, b.UomCode, b.OnHand, b.AverageCost))
            .ToList();

        var count = new StockCount(await numbers.NextAsync("SC", ct), site.Id, Me(), Now(), expected);
        db.StockCounts.Add(count);
        await db.SaveChangesAsync(ct);
        return (count.Id, count.Number);
    }

    public async Task RecordAsync(Guid id, RecordCountsRequest request, CancellationToken ct)
    {
        var count = await LoadAsync(id, ct);
        if (request.Lines.Select(l => l.LineId).Distinct().Count() != request.Lines.Count)
            throw new DomainException("A line appears twice.");
        count.RecordCounts(request.Lines.ToDictionary(l => l.LineId, l => l.Counted));
        await db.SaveChangesAsync(ct);
    }

    public async Task PostAsync(Guid id, string? notes, CancellationToken ct)
    {
        var count = await LoadAsync(id, ct);
        var balances = await ledger.AtSiteAsync(count.SiteId, count.Lines.Select(l => l.ItemId).ToList(), ct);
        var now = Now();

        var differences = count.Post(balances.ToDictionary(b => b.Key, b => b.Value.OnHand), notes, Me(), now);
        foreach (var line in differences)
            db.StockMovements.Add(new StockMovement(count.SiteId, line.ItemId, line.ItemCode, line.Description,
                line.UomCode, line.Variance, balances.GetValueOrDefault(line.ItemId)?.AverageCost ?? line.UnitCost,
                StockMovement.CountDocumentType, count.Id, count.Number, now));

        await db.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(Guid id, CancellationToken ct)
    {
        var count = await LoadAsync(id, ct);
        count.Cancel();
        await db.SaveChangesAsync(ct);
    }

    private async Task<StockCount> LoadAsync(Guid id, CancellationToken ct) =>
        await db.StockCounts.Include(c => c.Lines).SingleOrDefaultAsync(c => c.Id == id, ct)
        ?? throw new NotFoundException($"Stock count {id} was not found.");

    private Guid Me() => currentUser.UserId ?? throw new ForbiddenException("You must be signed in.");

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;
}
