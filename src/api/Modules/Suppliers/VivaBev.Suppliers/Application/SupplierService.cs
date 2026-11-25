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

    // ---- details, contacts and addresses ----

    public Task UpdateDetailsAsync(Guid id, UpdateSupplierDetailsRequest request, CancellationToken ct) =>
        UpdateAsync(id, s => s.UpdateDetails(request.TradingName, request.PaymentTermsDays), ct);

    public async Task<Guid> AddContactAsync(Guid id, SupplierContactRequest request, CancellationToken ct)
    {
        var supplier = await LoadAsync(id, ct);
        var contact = supplier.AddContact(request.Name, request.Email, request.Phone, request.Role, request.IsPrimary);
        await db.SaveChangesAsync(ct);
        return contact.Id;
    }

    public Task UpdateContactAsync(Guid id, Guid contactId, SupplierContactRequest request, CancellationToken ct) =>
        UpdateAsync(id, s => s.UpdateContact(contactId, request.Name, request.Email, request.Phone, request.Role,
            request.IsPrimary), ct);

    public Task RemoveContactAsync(Guid id, Guid contactId, CancellationToken ct) =>
        UpdateAsync(id, s => s.RemoveContact(contactId), ct);

    public async Task<Guid> AddAddressAsync(Guid id, SupplierAddressRequest request, CancellationToken ct)
    {
        var supplier = await LoadAsync(id, ct);
        var address = supplier.AddAddress(ParseType(request.AddressType), request.Line1, request.City, request.State,
            request.CountryCode, request.Line2);
        await db.SaveChangesAsync(ct);
        return address.Id;
    }

    public Task UpdateAddressAsync(Guid id, Guid addressId, SupplierAddressRequest request, CancellationToken ct) =>
        UpdateAsync(id, s => s.UpdateAddress(addressId, ParseType(request.AddressType), request.Line1, request.Line2,
            request.City, request.State, request.CountryCode), ct);

    public Task RemoveAddressAsync(Guid id, Guid addressId, CancellationToken ct) =>
        UpdateAsync(id, s => s.RemoveAddress(addressId), ct);

    private static AddressType ParseType(string value) =>
        Enum.TryParse<AddressType>(value, ignoreCase: true, out var type)
            ? type
            : throw new DomainException("Address type must be Billing or Shipping.");

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
