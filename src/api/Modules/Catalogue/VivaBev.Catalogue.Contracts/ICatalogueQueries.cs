namespace VivaBev.Catalogue.Contracts;

public sealed record CategorySummaryDto(Guid Id, string Code, string Name, bool IsActive);

public interface ICatalogueQueries
{
    Task<CategorySummaryDto?> GetCategoryAsync(Guid categoryId, CancellationToken ct = default);

    Task<CategorySummaryDto?> GetCategoryByCodeAsync(string code, CancellationToken ct = default);

    Task<IReadOnlyList<CategorySummaryDto>> GetCategoriesAsync(
        IReadOnlyCollection<Guid> categoryIds, CancellationToken ct = default);
}