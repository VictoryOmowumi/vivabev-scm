using Microsoft.EntityFrameworkCore;
using VivaBev.Suppliers.Contracts;
using VivaBev.Suppliers.Domain;
using VivaBev.Suppliers.Infrastructure;

namespace VivaBev.Suppliers.Application;

internal sealed class SupplierQueries(SuppliersDbContext db) : ISupplierQueries
{
    public Task<bool> IsApprovedForCategoryAsync(Guid supplierId, Guid categoryId, CancellationToken ct = default) =>
        db.Suppliers.AsNoTracking().AnyAsync(s =>
            s.Id == supplierId &&
            s.Status == SupplierStatus.Approved &&
            s.Categories.Any(c => c.CategoryId == categoryId && c.Status == CategoryApprovalStatus.Approved), ct);

    public async Task<IReadOnlyList<SupplierOptionDto>> GetApprovedForCategoriesAsync(
        IReadOnlyCollection<Guid> categoryIds, CancellationToken ct = default)
    {
        if (categoryIds.Count == 0)
            return [];

        var ids = categoryIds.Distinct().ToList();
        var suppliers = await db.Suppliers.AsNoTracking()
            .Include(s => s.Categories)
            .Where(s => s.Status == SupplierStatus.Approved &&
                        s.Categories.Any(c => ids.Contains(c.CategoryId) && c.Status == CategoryApprovalStatus.Approved))
            .ToListAsync(ct);

        return suppliers
            .Select(s => new SupplierOptionDto(s.Id, s.Code, s.TradingName ?? s.LegalName, s.PaymentTermsDays,
                s.Categories.Where(c => c.Status == CategoryApprovalStatus.Approved).Select(c => c.CategoryId).ToList()))
            .OrderBy(s => s.Name)
            .ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetNamesAsync(
        IReadOnlyCollection<Guid> supplierIds, CancellationToken ct = default)
    {
        if (supplierIds.Count == 0)
            return new Dictionary<Guid, string>();

        var ids = supplierIds.Distinct().ToList();
        return await db.Suppliers.AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.TradingName ?? s.LegalName, ct);
    }
}
