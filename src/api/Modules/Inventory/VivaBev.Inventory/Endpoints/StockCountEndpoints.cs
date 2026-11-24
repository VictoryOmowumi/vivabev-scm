using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using VivaBev.Identity.Contracts;
using VivaBev.Inventory.Application;
using VivaBev.Inventory.Contracts;
using VivaBev.Inventory.Domain.Stock;
using VivaBev.Inventory.Infrastructure;
using VivaBev.SharedKernel.Security;

namespace VivaBev.Inventory.Endpoints;

internal static class StockCountEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory/stock-counts").WithTags("Stock counts")
            .RequireAuthorization(Permissions.Inventory.View);

        group.MapGet("/", async (InventoryDbContext db, IUserDirectory users, IOrganisationQueries organisation,
            CancellationToken ct) =>
        {
            var counts = await db.StockCounts.AsNoTracking().Include(c => c.Lines)
                .OrderByDescending(c => c.Number).Take(100).ToListAsync(ct);
            var people = await users.GetUsersAsync(counts.Select(c => c.StartedBy).Distinct().ToList(), ct);
            var sites = new Dictionary<Guid, SiteDto?>();
            foreach (var siteId in counts.Select(c => c.SiteId).Distinct())
                sites[siteId] = await organisation.GetSiteAsync(siteId, ct);

            return counts.Select(c => new StockCountSummaryDto(
                c.Id, c.Number, c.Status.ToString(), sites.GetValueOrDefault(c.SiteId)?.Code ?? "?",
                people.GetValueOrDefault(c.StartedBy)?.DisplayName ?? "(unknown user)", c.StartedAt, c.Lines.Count,
                c.Lines.Count(l => l.Counted is not null), c.Lines.Count(l => l.Variance != 0),
                c.Lines.Sum(l => l.VarianceValue), c.PostedAt)).ToList();
        });

        group.MapGet("/{id:guid}", async (Guid id, InventoryDbContext db, StockLedger ledger, IUserDirectory users,
            IOrganisationQueries organisation, CancellationToken ct) =>
        {
            var c = await db.StockCounts.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (c is null)
                return Results.NotFound();

            var site = await organisation.GetSiteAsync(c.SiteId, ct);
            var ids = new List<Guid> { c.StartedBy };
            if (c.PostedBy is { } poster)
                ids.Add(poster);
            var people = await users.GetUsersAsync(ids.Distinct().ToList(), ct);

            // While counting, show the ledger as it is now (stock may have moved since the count started);
            // once posted, the line keeps the figure it was compared with
            var now = c.Status == StockCountStatus.Open
                ? await ledger.AtSiteAsync(c.SiteId, c.Lines.Select(l => l.ItemId).ToList(), ct)
                : null;
            StockCountLineDto ToDto(StockCountLine l)
            {
                var expected = now is null ? l.Expected : now.GetValueOrDefault(l.ItemId)?.OnHand ?? 0;
                var variance = l.Counted is { } counted ? counted - expected : 0;
                return new StockCountLineDto(l.Id, l.LineNo, l.ItemCode, l.Description, l.UomCode, expected, l.Counted,
                    variance, l.UnitCost, decimal.Round(variance * l.UnitCost, 2));
            }

            return Results.Ok(new StockCountDetailDto(
                c.Id, c.Number, c.Status.ToString(), c.SiteId, site?.Code ?? "?", site?.Name ?? "(unknown site)",
                people.GetValueOrDefault(c.StartedBy)?.DisplayName ?? "(unknown user)", c.StartedAt,
                c.PostedBy is { } p ? people.GetValueOrDefault(p)?.DisplayName ?? "(unknown user)" : null, c.PostedAt, c.Notes,
                c.Lines.OrderBy(l => l.LineNo).Select(ToDto).ToList()));
        });

        group.MapPost("/", async (StartStockCountRequest request, StockCountService service, CancellationToken ct) =>
        {
            var (id, number) = await service.StartAsync(request, ct);
            return Results.Created($"/api/inventory/stock-counts/{id}", new { id, number });
        }).RequireAuthorization(Permissions.Inventory.CountStock);

        group.MapPut("/{id:guid}/counts", async (Guid id, RecordCountsRequest request, StockCountService service,
            CancellationToken ct) =>
        {
            await service.RecordAsync(id, request, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Inventory.CountStock);

        group.MapPost("/{id:guid}/post", async (Guid id, PostStockCountRequest request, StockCountService service,
            CancellationToken ct) =>
        {
            await service.PostAsync(id, request.Notes, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Inventory.CountStock);

        group.MapPost("/{id:guid}/cancel", async (Guid id, StockCountService service, CancellationToken ct) =>
        {
            await service.CancelAsync(id, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Inventory.CountStock);
    }
}
