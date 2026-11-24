using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using VivaBev.Catalogue.Contracts;
using VivaBev.Catalogue.Domain;
using VivaBev.Catalogue.Infrastructure;
using VivaBev.SharedKernel.Domain;
using VivaBev.SharedKernel.Errors;
using VivaBev.SharedKernel.Security;

namespace VivaBev.Catalogue.Endpoints;

internal static class CatalogueEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalogue").WithTags("Catalogue")
            .RequireAuthorization(Permissions.Catalogue.View);

        group.MapGet("/units-of-measure", async (CatalogueDbContext db, CancellationToken ct) =>
        {
            var units = await db.UnitsOfMeasure
                .AsNoTracking()
                .OrderBy(u => u.Code)
                .ToListAsync(ct);

            return units.Select(u => new UnitOfMeasureDto(u.Code, u.Name, u.Dimension.ToString()));
        });

        group.MapGet("/categories", async (CatalogueDbContext db, CancellationToken ct) =>
        {
            var categories = await db.Categories.AsNoTracking().OrderBy(c => c.Code).ToListAsync(ct);
            return categories.Select(c => new CategorySummaryDto(c.Id, c.Code, c.Name, c.IsActive));
        });

        // take: the requisition line picker asks for a few; the catalogue page asks for up to 500
        group.MapGet("/items", async (string? search, Guid? categoryId, int? take, CatalogueDbContext db,
            CancellationToken ct) =>
        {
            var items = db.Items.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
                items = items.Where(i => i.Code.Contains(search) || i.Name.Contains(search));
            if (categoryId is not null)
                items = items.Where(i => i.CategoryId == categoryId);

            var rows = await items
                .Join(db.Categories, i => i.CategoryId, c => c.Id, (i, c) => new { Item = i, CategoryName = c.Name })
                .OrderBy(x => x.Item.Code)
                .Take(Math.Clamp(take ?? 100, 1, 500))
                .ToListAsync(ct);

            return rows.Select(x => new ItemSummaryDto(
                x.Item.Id, x.Item.Code, x.Item.Name, x.Item.ItemType.ToString(),
                x.CategoryName, x.Item.BaseUomCode, x.Item.IsStocked, x.Item.IsActive, x.Item.CategoryId));
        });

        group.MapGet("/items/{id:guid}", async (Guid id, CatalogueDbContext db, CancellationToken ct) =>
        {
            var row = await db.Items.AsNoTracking().Where(i => i.Id == id)
                .Join(db.Categories, i => i.CategoryId, c => c.Id, (i, c) => new { Item = i, Category = c })
                .SingleOrDefaultAsync(ct);
            if (row is null)
                return Results.NotFound();

            var i = row.Item;
            return Results.Ok(new ItemDetailDto(i.Id, i.Code, i.Name, i.Description, i.ItemType.ToString(), i.CategoryId,
                row.Category.Code, row.Category.Name, i.BaseUomCode, i.IsStocked, i.IsLotControlled, i.ShelfLifeDays,
                i.IsActive));
        });

        // ---------- commands (Procurement maintains the catalogue) ----------

        group.MapPost("/items", async (CreateItemRequest request, CatalogueDbContext db, CancellationToken ct) =>
        {
            var code = request.Code.Trim().ToUpperInvariant();
            if (await db.Items.AnyAsync(i => i.Code == code, ct))
                throw new DomainException($"An item with code {code} already exists.");
            if (!Enum.TryParse<ItemType>(request.ItemType, ignoreCase: true, out var type))
                throw new DomainException($"'{request.ItemType}' is not an item type.");
            await EnsureCategoryAsync(db, request.CategoryId, ct);
            if (!await db.UnitsOfMeasure.AnyAsync(u => u.Code == request.BaseUomCode, ct))
                throw new DomainException($"'{request.BaseUomCode}' is not a unit of measure.");

            var item = new Item(code, request.Name, type, request.CategoryId, request.BaseUomCode, request.IsStocked,
                request.IsLotControlled, request.ShelfLifeDays, request.Description);
            db.Items.Add(item);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/catalogue/items/{item.Id}", new { id = item.Id, code = item.Code });
        }).RequireAuthorization(Permissions.Catalogue.Manage);

        group.MapPut("/items/{id:guid}", async (Guid id, UpdateItemRequest request, CatalogueDbContext db,
            CancellationToken ct) =>
        {
            var item = await db.Items.SingleOrDefaultAsync(i => i.Id == id, ct)
                ?? throw new NotFoundException($"Item {id} was not found.");
            await EnsureCategoryAsync(db, request.CategoryId, ct);

            item.UpdateDetails(request.Name, request.Description, request.CategoryId);
            item.SetStockControl(request.IsStocked, request.IsLotControlled, request.ShelfLifeDays);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Catalogue.Manage);

        foreach (var (route, activate) in new[] { ("activate", true), ("deactivate", false) })
        {
            group.MapPost($"/items/{{id:guid}}/{route}", async (Guid id, CatalogueDbContext db, CancellationToken ct) =>
            {
                var item = await db.Items.SingleOrDefaultAsync(i => i.Id == id, ct)
                    ?? throw new NotFoundException($"Item {id} was not found.");
                if (activate) item.Activate(); else item.Deactivate();
                await db.SaveChangesAsync(ct);
                return Results.NoContent();
            }).RequireAuthorization(Permissions.Catalogue.Manage);

            group.MapPost($"/categories/{{id:guid}}/{route}", async (Guid id, CatalogueDbContext db, CancellationToken ct) =>
            {
                var category = await db.Categories.SingleOrDefaultAsync(c => c.Id == id, ct)
                    ?? throw new NotFoundException($"Category {id} was not found.");
                if (activate) category.Activate(); else category.Deactivate();
                await db.SaveChangesAsync(ct);
                return Results.NoContent();
            }).RequireAuthorization(Permissions.Catalogue.Manage);
        }

        group.MapPost("/categories", async (CreateCategoryRequest request, CatalogueDbContext db, CancellationToken ct) =>
        {
            var code = request.Code.Trim().ToUpperInvariant();
            if (await db.Categories.AnyAsync(c => c.Code == code, ct))
                throw new DomainException($"A category with code {code} already exists.");

            var category = new Category(code, request.Name);
            db.Categories.Add(category);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/catalogue/categories/{category.Id}", new { id = category.Id });
        }).RequireAuthorization(Permissions.Catalogue.Manage);

        group.MapPut("/categories/{id:guid}", async (Guid id, RenameCategoryRequest request, CatalogueDbContext db,
            CancellationToken ct) =>
        {
            var category = await db.Categories.SingleOrDefaultAsync(c => c.Id == id, ct)
                ?? throw new NotFoundException($"Category {id} was not found.");
            category.Rename(request.Name);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Catalogue.Manage);

    }

    private static async Task EnsureCategoryAsync(CatalogueDbContext db, Guid categoryId, CancellationToken ct)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == categoryId && c.IsActive, ct))
            throw new DomainException("Choose an active category.");
    }
}