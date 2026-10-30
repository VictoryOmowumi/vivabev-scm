using VivaBev.SharedKernel.Domain;

namespace VivaBev.Suppliers.Domain;

public sealed class Supplier : AggregateRoot
{
    public const int CodeMaxLength = 20;

    private readonly List<SupplierContact> _contacts = [];
    private readonly List<SupplierAddress> _addresses = [];
    private readonly List<SupplierCategory> _categories = [];

    private Supplier() { }

    public Supplier(
        string code,
        string legalName,
        string taxId,
        string countryCode = "NG",
        string defaultCurrencyCode = "NGN",
        int paymentTermsDays = 30,
        string? tradingName = null)
    {
        Code = Guard.Code(code, nameof(code), CodeMaxLength);
        LegalName = Guard.Required(legalName, nameof(legalName), 200);
        TradingName = string.IsNullOrWhiteSpace(tradingName) ? null : tradingName.Trim();
        TaxId = Guard.Code(taxId, nameof(taxId), 20);
        CountryCode = Guard.Code(countryCode, nameof(countryCode), 2);
        DefaultCurrencyCode = Guard.Code(defaultCurrencyCode, nameof(defaultCurrencyCode), 3);
        SetPaymentTerms(paymentTermsDays);
        Status = SupplierStatus.Prospect;
    }

    public string Code { get; private set; } = null!;
    public string LegalName { get; private set; } = null!;
    public string? TradingName { get; private set; }
    public string TaxId { get; private set; } = null!;
    public string CountryCode { get; private set; } = null!;
    public string DefaultCurrencyCode { get; private set; } = null!;
    public int PaymentTermsDays { get; private set; }
    public SupplierStatus Status { get; private set; }
    public string? StatusReason { get; private set; }

    public IReadOnlyCollection<SupplierContact> Contacts => _contacts.AsReadOnly();
    public IReadOnlyCollection<SupplierAddress> Addresses => _addresses.AsReadOnly();
    public IReadOnlyCollection<SupplierCategory> Categories => _categories.AsReadOnly();

    // ---- details ----

    public void SetPaymentTerms(int days)
    {
        if (days is < 0 or > 180)
            throw new DomainException("Payment terms must be between 0 and 180 days.");
        PaymentTermsDays = days;
    }

    public SupplierContact AddContact(string name, string email, string? phone = null, string? role = null,
        bool isPrimary = false)
    {
        EnsureNotBlocked();

        var makePrimary = isPrimary || _contacts.Count == 0;
        if (makePrimary)
            _contacts.ForEach(c => c.ClearPrimary());

        var contact = new SupplierContact(Id, name, email, phone, role, makePrimary);
        _contacts.Add(contact);
        return contact;
    }

    public SupplierAddress AddAddress(AddressType addressType, string line1, string city, string state,
        string countryCode = "NG", string? line2 = null)
    {
        EnsureNotBlocked();
        var address = new SupplierAddress(Id, addressType, line1, line2, city, state, countryCode);
        _addresses.Add(address);
        return address;
    }

    // ---- categories ----

    public void RequestCategory(Guid categoryId)
    {
        EnsureNotBlocked();

        var existing = _categories.SingleOrDefault(c => c.CategoryId == categoryId);
        if (existing is null)
        {
            _categories.Add(new SupplierCategory(Id, categoryId));
            return;
        }

        if (existing.Status != CategoryApprovalStatus.Revoked)
            throw new DomainException("This category has already been requested for the supplier.");

        existing.Reopen();
    }

    public void ApproveCategory(Guid categoryId, Guid? approvedBy, DateTime approvedAt)
    {
        RequireStatus(SupplierStatus.Approved, "approve a category for");
        FindCategory(categoryId).Approve(approvedBy, approvedAt);
    }

    public void RevokeCategory(Guid categoryId) => FindCategory(categoryId).Revoke();

    public bool IsApprovedFor(Guid categoryId) =>
        Status == SupplierStatus.Approved &&
        _categories.Any(c => c.CategoryId == categoryId && c.Status == CategoryApprovalStatus.Approved);

    // ---- lifecycle ----

    public void SubmitForApproval()
    {
        RequireStatus(SupplierStatus.Prospect, "submit");

        if (_contacts.Count == 0)
            throw new DomainException("Add at least one contact before submitting.");
        if (!_categories.Any(c => c.Status == CategoryApprovalStatus.Pending))
            throw new DomainException("Request at least one category before submitting.");

        MoveTo(SupplierStatus.PendingApproval, reason: null);
    }

    public void Approve(Guid? approvedBy, DateTime approvedAt)
    {
        RequireStatus(SupplierStatus.PendingApproval, "approve");
        MoveTo(SupplierStatus.Approved, reason: null);

        foreach (var category in _categories.Where(c => c.Status == CategoryApprovalStatus.Pending))
            category.Approve(approvedBy, approvedAt);
    }

    public void Reject(string reason)
    {
        RequireStatus(SupplierStatus.PendingApproval, "reject");
        MoveTo(SupplierStatus.Prospect, Guard.Required(reason, nameof(reason), 500));
    }

    public void Suspend(string reason)
    {
        RequireStatus(SupplierStatus.Approved, "suspend");
        MoveTo(SupplierStatus.Suspended, Guard.Required(reason, nameof(reason), 500));
    }

    public void Reinstate()
    {
        RequireStatus(SupplierStatus.Suspended, "reinstate");
        MoveTo(SupplierStatus.Approved, reason: null);
    }

    public void Block(string reason)
    {
        EnsureNotBlocked();
        MoveTo(SupplierStatus.Blocked, Guard.Required(reason, nameof(reason), 500));
    }

    // ---- helpers ----

    private void MoveTo(SupplierStatus status, string? reason)
    {
        Status = status;
        StatusReason = reason;
    }

    private void RequireStatus(SupplierStatus required, string action)
    {
        if (Status != required)
            throw new DomainException($"Cannot {action} a supplier that is {Status}; it must be {required}.");
    }

    private void EnsureNotBlocked()
    {
        if (Status == SupplierStatus.Blocked)
            throw new DomainException("This supplier is blocked and cannot be changed.");
    }

    private SupplierCategory FindCategory(Guid categoryId) =>
        _categories.SingleOrDefault(c => c.CategoryId == categoryId)
        ?? throw new DomainException("The supplier has not requested this category.");
}