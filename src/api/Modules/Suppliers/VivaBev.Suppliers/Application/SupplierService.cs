using Microsoft.EntityFrameworkCore;
using VivaBev.Catalogue.Contracts;
using VivaBev.SharedKernel.Abstractions;
using VivaBev.SharedKernel.Domain;
using VivaBev.SharedKernel.Errors;
using VivaBev.Suppliers.Contracts;
using VivaBev.Suppliers.Domain;
using VivaBev.Suppliers.Infrastructure;

namespace VivaBev.Suppliers.Application;

internal sealed class SupplierService(
    SuppliersDbContext db,
    ICatalogueQueries catalogue,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public async Task<Guid> CreateAsync(CreateSupplierRequest request, CancellationToken ct)
    {
        var supplier = new Supplier(
            request.Code, request.LegalName, request.TaxId,
            request.CountryCode, request.DefaultCurrencyCode, request.PaymentTermsDays, request.TradingName);

        supplier.AddContact(request.ContactName, request.ContactEmail, request.ContactPhone, isPrimary: true);

        if (await db.Suppliers.AnyAsync(s => s.Code == supplier.Code, ct))
            throw new DomainException($"Supplier code {supplier.Code} is already in use.");
        if (await db.Suppliers.AnyAsync(s => s.TaxId == supplier.TaxId, ct))
            throw new DomainException($"A supplier with tax ID {supplier.TaxId} already exists.");

        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(ct);
        return supplier.Id;
    }

    public async Task RequestCategoryAsync(Guid supplierId, Guid categoryId, CancellationToken ct)
    {
        var category = await catalogue.GetCategoryAsync(categoryId, ct)
            ?? throw new NotFoundException($"Category {categoryId} was not found.");
        if (!category.IsActive)
            throw new DomainException($"Category {category.Code} is inactive.");

        await UpdateAsync(supplierId, s => s.RequestCategory(categoryId), ct);
    }

    public Task SubmitAsync(Guid id, CancellationToken ct) =>
        UpdateAsync(id, s => s.SubmitForApproval(), ct);

    public Task ApproveAsync(Guid id, CancellationToken ct) =>
        UpdateAsync(id, s => s.Approve(currentUser.UserId, Now), ct);

    public Task RejectAsync(Guid id, string reason, CancellationToken ct) =>
        UpdateAsync(id, s => s.Reject(reason), ct);

    public Task SuspendAsync(Guid id, string reason, CancellationToken ct) =>
        UpdateAsync(id, s => s.Suspend(reason), ct);

    public Task ReinstateAsync(Guid id, CancellationToken ct) =>
        UpdateAsync(id, s => s.Reinstate(), ct);

    public Task ApproveCategoryAsync(Guid id, Guid categoryId, CancellationToken ct) =>
        UpdateAsync(id, s => s.ApproveCategory(categoryId, currentUser.UserId, Now), ct);

    public Task RevokeCategoryAsync(Guid id, Guid categoryId, CancellationToken ct) =>
        UpdateAsync(id, s => s.RevokeCategory(categoryId), ct);

    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    private async Task UpdateAsync(Guid id, Action<Supplier> change, CancellationToken ct)
    {
        var supplier = await LoadAsync(id, ct);
        change(supplier);
        await db.SaveChangesAsync(ct);
    }

    private async Task<Supplier> LoadAsync(Guid id, CancellationToken ct) =>
        await db.Suppliers
            .Include(s => s.Contacts)
            .Include(s => s.Addresses)
            .Include(s => s.Categories)
            .AsSplitQuery()
            .SingleOrDefaultAsync(s => s.Id == id, ct)
        ?? throw new NotFoundException($"Supplier {id} was not found.");
}
