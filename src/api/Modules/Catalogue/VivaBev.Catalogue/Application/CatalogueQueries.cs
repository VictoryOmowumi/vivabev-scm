using Microsoft.EntityFrameworkCore;
using VivaBev.Catalogue.Contracts;
using VivaBev.Catalogue.Infrastructure;

namespace VivaBev.Catalogue.Application;

internal sealed class CatalogueQueries(CatalogueDbContext db) : ICatalogueQueries
{
    public async Task<CategorySummaryDto?> GetCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
        await db.Categories.AsNoTracking()
            .Where(c => c.Id == categoryId)
            .Select(c => new CategorySummaryDto(c.Id, c.Code, c.Name, c.IsActive))
            .SingleOrDefaultAsync(ct);

    public async Task<CategorySummaryDto?> GetCategoryByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalised = code.Trim().ToUpperInvariant();
        return await db.Categories.AsNoTracking()
            .Where(c => c.Code == normalised)
            .Select(c => new CategorySummaryDto(c.Id, c.Code, c.Name, c.IsActive))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<CategorySummaryDto>> GetCategoriesAsync(
        IReadOnlyCollection<Guid> categoryIds, CancellationToken ct = default) =>
        await db.Categories.AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .Select(c => new CategorySummaryDto(c.Id, c.Code, c.Name, c.IsActive))
            .ToListAsync(ct);
}