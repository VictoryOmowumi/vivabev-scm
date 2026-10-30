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
}
