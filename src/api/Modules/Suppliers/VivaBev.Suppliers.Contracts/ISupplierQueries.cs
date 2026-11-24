namespace VivaBev.Suppliers.Contracts;

/// <summary>An approved supplier another module may buy from, with the categories it's approved for.</summary>
public sealed record SupplierOptionDto(
    Guid Id, string Code, string Name, int PaymentTermsDays, IReadOnlyList<Guid> ApprovedCategoryIds);

public interface ISupplierQueries
{
    Task<bool> IsApprovedForCategoryAsync(Guid supplierId, Guid categoryId, CancellationToken ct = default);

    /// <summary>Approved suppliers approved for at least one of the categories, ordered by name.</summary>
    Task<IReadOnlyList<SupplierOptionDto>> GetApprovedForCategoriesAsync(
        IReadOnlyCollection<Guid> categoryIds, CancellationToken ct = default);

    /// <summary>Display names (trading name, else legal name), keyed by id. Unknown ids are left out.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
        IReadOnlyCollection<Guid> supplierIds, CancellationToken ct = default);
}
