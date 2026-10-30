using VivaBev.SharedKernel.Domain;

namespace VivaBev.Suppliers.Domain;

public sealed class SupplierContact : AuditableEntity
{
    private SupplierContact() { }

    internal SupplierContact(Guid supplierId, string name, string email, string? phone, string? role, bool isPrimary)
    {
        SupplierId = supplierId;
        Name = Guard.Required(name, nameof(name), 100);
        Email = Guard.Required(email, nameof(email), 200).ToLowerInvariant();
        if (!Email.Contains('@'))
            throw new DomainException("Email must be a valid address.");
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Role = string.IsNullOrWhiteSpace(role) ? null : role.Trim();
        IsPrimary = isPrimary;
    }

    public Guid SupplierId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? Phone { get; private set; }
    public string? Role { get; private set; }
    public bool IsPrimary { get; private set; }

    internal void ClearPrimary() => IsPrimary = false;
}