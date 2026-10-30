namespace VivaBev.Suppliers.Contracts;

public interface ISupplierQueries
{
    Task<bool> IsApprovedForCategoryAsync(Guid supplierId, Guid categoryId, CancellationToken ct = default);
}
