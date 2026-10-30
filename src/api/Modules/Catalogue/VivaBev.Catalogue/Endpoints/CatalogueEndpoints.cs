using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using VivaBev.Catalogue.Contracts;
using VivaBev.Catalogue.Infrastructure;

namespace VivaBev.Catalogue.Endpoints;

internal static class CatalogueEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalogue").WithTags("Catalogue");

        group.MapGet("/units-of-measure", async (CatalogueDbContext db, CancellationToken ct) =>
        {
            var units = await db.UnitsOfMeasure
                .AsNoTracking()
                .OrderBy(u => u.Code)
                .ToListAsync(ct);

            return units.Select(u => new UnitOfMeasureDto(u.Code, u.Name, u.Dimension.ToString()));
        });

        group.MapGet("/items", async (string? search, CatalogueDbContext db, CancellationToken ct) =>
        {
            var items = db.Items.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
                items = items.Where(i => i.Code.Contains(search) || i.Name.Contains(search));

            var rows = await items
                .Join(db.Categories, i => i.CategoryId, c => c.Id, (i, c) => new { Item = i, CategoryName = c.Name })
                .OrderBy(x => x.Item.Code)
                .Take(100)
                .ToListAsync(ct);

            return rows.Select(x => new ItemSummaryDto(
                x.Item.Id, x.Item.Code, x.Item.Name, x.Item.ItemType.ToString(),
                x.CategoryName, x.Item.BaseUomCode, x.Item.IsStocked, x.Item.IsActive));
        });
    }
}