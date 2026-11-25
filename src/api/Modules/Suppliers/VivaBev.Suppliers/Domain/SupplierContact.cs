using VivaBev.SharedKernel.Domain;

namespace VivaBev.Suppliers.Domain;

public sealed class SupplierContact : AuditableEntity
{
    private SupplierContact() { }

    internal SupplierContact(Guid supplierId, string name, string email, string? phone, string? role, bool isPrimary)
    {
        SupplierId = supplierId;
        Update(name, email, phone, role);
        IsPrimary = isPrimary;
    }

    internal void Update(string name, string email, string? phone, string? role)
    {
        Name = Guard.Required(name, nameof(name), 100);
        Email = Guard.Required(email, nameof(email), 200).ToLowerInvariant();
        if (!Email.Contains('@'))
            throw new DomainException("Email must be a valid address.");
        if (phone?.Trim().Length > 30)
            throw new DomainException("Keep the phone number under 30 characters.");
        if (role?.Trim().Length > 100)
            throw new DomainException("Keep the role under 100 characters.");
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Role = string.IsNullOrWhiteSpace(role) ? null : role.Trim();
    }

    public Guid SupplierId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? Phone { get; private set; }
    public string? Role { get; private set; }
    public bool IsPrimary { get; private set; }

    internal void ClearPrimary() => IsPrimary = false;

    internal void MakePrimary() => IsPrimary = true;
}