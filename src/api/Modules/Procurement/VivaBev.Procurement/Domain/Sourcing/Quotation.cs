using VivaBev.SharedKernel.Domain;

namespace VivaBev.Procurement.Domain.Sourcing;

/// <summary>A supplier's prices for some or all of the event's lines. Entered by the buyer in release 1.</summary>
public sealed class Quotation : AuditableEntity
{
    private readonly List<QuotationLine> _lines = [];

    private Quotation() { }

    internal Quotation(Guid sourcingEventId, Guid supplierId, string? supplierReference, DateOnly validUntil,
        int leadTimeDays, int paymentTermsDays, DateTime receivedAt)
    {
        if (leadTimeDays < 0)
            throw new DomainException("Lead time cannot be negative.");
        if (paymentTermsDays < 0)
            throw new DomainException("Payment terms cannot be negative.");

        SourcingEventId = sourcingEventId;
        SupplierId = supplierId;
        SupplierReference = string.IsNullOrWhiteSpace(supplierReference) ? null : Guard.Required(supplierReference, nameof(supplierReference), 50);
        ValidUntil = validUntil;
        LeadTimeDays = leadTimeDays;
        PaymentTermsDays = paymentTermsDays;
        ReceivedAt = receivedAt;
        CurrencyCode = "NGN";
        Status = QuotationStatus.Received;
    }

    public Guid SourcingEventId { get; private set; }
    public Guid SupplierId { get; private set; }
    public string? SupplierReference { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public DateOnly ValidUntil { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public int LeadTimeDays { get; private set; }
    public int PaymentTermsDays { get; private set; }
    public decimal Total { get; private set; }
    public QuotationStatus Status { get; private set; }
    public string? WithdrawalReason { get; private set; }

    public IReadOnlyCollection<QuotationLine> Lines => _lines.AsReadOnly();

    internal void AddLine(Guid eventLineId, decimal unitPrice, decimal quantity, string? notes)
    {
        if (_lines.Any(l => l.SourcingEventLineId == eventLineId))
            throw new DomainException("A quotation can price each line only once.");

        _lines.Add(new QuotationLine(Id, eventLineId, unitPrice, quantity, notes));
        Total = _lines.Sum(l => l.LineTotal);
    }

    internal void Withdraw(string reason)
    {
        if (Status == QuotationStatus.Withdrawn)
            throw new DomainException("This quotation is already withdrawn.");
        WithdrawalReason = Guard.Required(reason, nameof(reason), 500);
        Status = QuotationStatus.Withdrawn;
    }
}

public sealed class QuotationLine : AuditableEntity
{
    private QuotationLine() { }

    internal QuotationLine(Guid quotationId, Guid sourcingEventLineId, decimal unitPrice, decimal quantity, string? notes)
    {
        if (unitPrice <= 0)
            throw new DomainException("Unit price must be greater than zero.");
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        QuotationId = quotationId;
        SourcingEventLineId = sourcingEventLineId;
        UnitPrice = unitPrice;
        Quantity = quantity;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : Guard.Required(notes, nameof(notes), 500);
        LineTotal = decimal.Round(unitPrice * quantity, 2);
    }

    public Guid QuotationId { get; private set; }
    public Guid SourcingEventLineId { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal LineTotal { get; private set; }
    public string? Notes { get; private set; }
}
