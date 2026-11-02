namespace VivaBev.Catalogue.Contracts;

public sealed record CategorySummaryDto(Guid Id, string Code, string Name, bool IsActive);

public sealed record ItemForPurchasingDto(
    Guid Id, string Code, string Name, bool IsService, Guid CategoryId, string BaseUomCode, bool IsStocked, bool IsActive);

public interface ICatalogueQueries
{
    Task<CategorySummaryDto?> GetCategoryAsync(Guid categoryId, CancellationToken ct = default);

    Task<CategorySummaryDto?> GetCategoryByCodeAsync(string code, CancellationToken ct = default);

    Task<IReadOnlyList<CategorySummaryDto>> GetCategoriesAsync(
        IReadOnlyCollection<Guid> categoryIds, CancellationToken ct = default);

    Task<ItemForPurchasingDto?> GetItemAsync(Guid itemId, CancellationToken ct = default);
}