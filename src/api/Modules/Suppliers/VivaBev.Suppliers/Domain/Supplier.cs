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

    /// <summary>Trading name and payment terms. The legal name and tax ID are fixed once a supplier is created.</summary>
    public void UpdateDetails(string? tradingName, int paymentTermsDays)
    {
        EnsureNotBlocked();
        if (tradingName?.Trim().Length > 200)
            throw new DomainException("Keep the trading name under 200 characters.");
        TradingName = string.IsNullOrWhiteSpace(tradingName) ? null : tradingName.Trim();
        SetPaymentTerms(paymentTermsDays);
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

    public void UpdateContact(Guid contactId, string name, string email, string? phone, string? role, bool isPrimary)
    {
        EnsureNotBlocked();
        var contact = FindContact(contactId);
        contact.Update(name, email, phone, role);
        if (isPrimary)
            MakePrimary(contact);
        else if (contact.IsPrimary)
            throw new DomainException("A supplier needs a primary contact. Make someone else primary instead.");
    }

    /// <summary>Remove a contact. The last one can't go; if the primary goes, the next contact becomes primary.</summary>
    public void RemoveContact(Guid contactId)
    {
        EnsureNotBlocked();
        var contact = FindContact(contactId);
        if (_contacts.Count == 1)
            throw new DomainException("A supplier needs at least one contact. Add another before removing this one.");

        _contacts.Remove(contact);
        if (contact.IsPrimary)
            _contacts.OrderBy(c => c.Name).First().MakePrimary();
    }

    public SupplierAddress AddAddress(AddressType addressType, string line1, string city, string state,
        string countryCode = "NG", string? line2 = null)
    {
        EnsureNotBlocked();
        var address = new SupplierAddress(Id, addressType, line1, line2, city, state, countryCode);
        _addresses.Add(address);
        return address;
    }

    public void UpdateAddress(Guid addressId, AddressType addressType, string line1, string? line2, string city,
        string state, string countryCode)
    {
        EnsureNotBlocked();
        FindAddress(addressId).Update(addressType, line1, line2, city, state, countryCode);
    }

    public void RemoveAddress(Guid addressId)
    {
        EnsureNotBlocked();
        _addresses.Remove(FindAddress(addressId));
    }

    private void MakePrimary(SupplierContact contact)
    {
        _contacts.ForEach(c => c.ClearPrimary());
        contact.MakePrimary();
    }

    private SupplierContact FindContact(Guid contactId) =>
        _contacts.SingleOrDefault(c => c.Id == contactId) ?? throw new DomainException("That contact is not on this supplier.");

    private SupplierAddress FindAddress(Guid addressId) =>
        _addresses.SingleOrDefault(a => a.Id == addressId) ?? throw new DomainException("That address is not on this supplier.");

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