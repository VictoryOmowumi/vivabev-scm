namespace VivaBev.Suppliers.Contracts;

// ---- responses ----

public sealed record SupplierSummaryDto(
    Guid Id, string Code, string LegalName, string Status, string CountryCode, int ApprovedCategories);

public sealed record SupplierContactDto(
    Guid Id, string Name, string Email, string? Phone, string? Role, bool IsPrimary);

public sealed record SupplierAddressDto(
    Guid Id, string AddressType, string Line1, string? Line2, string City, string State, string CountryCode);

public sealed record SupplierCategoryDto(
    Guid CategoryId, string CategoryCode, string CategoryName, string Status, DateTime? ApprovedAt);

public sealed record SupplierDetailDto(
    Guid Id,
    string Code,
    string LegalName,
    string? TradingName,
    string TaxId,
    string Status,
    string? StatusReason,
    string CountryCode,
    string DefaultCurrencyCode,
    int PaymentTermsDays,
    IReadOnlyList<SupplierContactDto> Contacts,
    IReadOnlyList<SupplierAddressDto> Addresses,
    IReadOnlyList<SupplierCategoryDto> Categories);

// ---- requests ----

public sealed record CreateSupplierRequest(
    string Code,
    string LegalName,
    string TaxId,
    string ContactName,
    string ContactEmail,
    string? ContactPhone = null,
    string? TradingName = null,
    string CountryCode = "NG",
    string DefaultCurrencyCode = "NGN",
    int PaymentTermsDays = 30);

public sealed record RequestCategoryRequest(Guid CategoryId);

public sealed record ReasonRequest(string Reason);
