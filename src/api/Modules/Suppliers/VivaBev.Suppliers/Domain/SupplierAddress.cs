using VivaBev.SharedKernel.Domain;

namespace VivaBev.Suppliers.Domain;

public sealed class SupplierAddress : AuditableEntity
{
    private SupplierAddress() { }

    internal SupplierAddress(Guid supplierId, AddressType addressType, string line1, string? line2,
        string city, string state, string countryCode)
    {
        SupplierId = supplierId;
        AddressType = addressType;
        Line1 = Guard.Required(line1, nameof(line1), 200);
        Line2 = string.IsNullOrWhiteSpace(line2) ? null : line2.Trim();
        City = Guard.Required(city, nameof(city), 100);
        State = Guard.Required(state, nameof(state), 100);
        CountryCode = Guard.Code(countryCode, nameof(countryCode), 2);
    }

    public Guid SupplierId { get; private set; }
    public AddressType AddressType { get; private set; }
    public string Line1 { get; private set; } = null!;
    public string? Line2 { get; private set; }
    public string City { get; private set; } = null!;
    public string State { get; private set; } = null!;
    public string CountryCode { get; private set; } = null!;
}