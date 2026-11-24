using VivaBev.SharedKernel.Domain;

namespace VivaBev.Procurement.Domain.PurchaseOrders;

public enum PurchaseOrderStatus
{
    Draft,     // raised from an award; the buyer checks delivery details
    Issued,    // sent to the supplier; requisitions are Ordered and the budget holds the real amount
    Cancelled  // a draft that was dropped
}

/// <summary>
/// A purchase order: the awarded quotation's lines, priced as quoted, going to one supplier.
/// Lines are fixed when it's raised; the buyer can only change delivery details and notes while it's a draft.
/// </summary>
public sealed class PurchaseOrder : AggregateRoot
{
    private readonly List<PurchaseOrderLine> _lines = [];

    private PurchaseOrder() { }

    public PurchaseOrder(string number, Guid supplierId, Guid sourcingEventId, string sourcingEventNumber,
        Guid quotationId, string? supplierReference, Guid buyerId, int paymentTermsDays, Guid deliverToSiteId,
        DateOnly expectedDelivery, IReadOnlyCollection<OrderedLine> lines)
    {
        if (lines.Count == 0)
            throw new DomainException("A purchase order needs at least one line.");
        if (paymentTermsDays is < 0 or > 365)
            throw new DomainException("Payment terms must be between 0 and 365 days.");

        Number = Guard.Code(number, nameof(number), 20);
        SupplierId = supplierId;
        SourcingEventId = sourcingEventId;
        SourcingEventNumber = Guard.Code(sourcingEventNumber, nameof(sourcingEventNumber), 20);
        QuotationId = quotationId;
        SupplierReference = string.IsNullOrWhiteSpace(supplierReference) ? null : supplierReference.Trim();
        BuyerId = buyerId;
        PaymentTermsDays = paymentTermsDays;
        DeliverToSiteId = deliverToSiteId;
        ExpectedDelivery = expectedDelivery;
        CurrencyCode = "NGN";
        Status = PurchaseOrderStatus.Draft;

        var lineNo = 1;
        foreach (var line in lines)
            _lines.Add(new PurchaseOrderLine(Id, lineNo++, line));
    }

    public string Number { get; private set; } = null!;
    public Guid SupplierId { get; private set; }
    public Guid SourcingEventId { get; private set; }
    public string SourcingEventNumber { get; private set; } = null!;
    public Guid QuotationId { get; private set; }
    public string? SupplierReference { get; private set; }
    public Guid BuyerId { get; private set; }
    public int PaymentTermsDays { get; private set; }
    public Guid DeliverToSiteId { get; private set; }
    public DateOnly ExpectedDelivery { get; private set; }
    public string? Notes { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public PurchaseOrderStatus Status { get; private set; }
    public DateTime? IssuedAt { get; private set; }
    public string? CancellationReason { get; private set; }

    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    public decimal Total => _lines.Sum(l => l.LineTotal);
    public IEnumerable<Guid> RequisitionIds => _lines.Select(l => l.RequisitionId).Distinct();

    public void UpdateDelivery(Guid deliverToSiteId, DateOnly expectedDelivery, string? notes, DateOnly today)
    {
        EnsureDraft("change");
        if (expectedDelivery < today)
            throw new DomainException("The expected delivery date can't be in the past.");
        if (notes?.Trim().Length > 2000)
            throw new DomainException("Keep the notes under 2,000 characters.");

        DeliverToSiteId = deliverToSiteId;
        ExpectedDelivery = expectedDelivery;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public void Issue(DateTime now)
    {
        EnsureDraft("issue");
        if (ExpectedDelivery < DateOnly.FromDateTime(now))
            throw new DomainException("The expected delivery date has passed. Change it before issuing.");

        Status = PurchaseOrderStatus.Issued;
        IssuedAt = now;
    }

    public void Cancel(string reason)
    {
        EnsureDraft("cancel");
        CancellationReason = Guard.Required(reason, nameof(reason), 500);
        Status = PurchaseOrderStatus.Cancelled;
    }

    private void EnsureDraft(string action)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new DomainException(Status == PurchaseOrderStatus.Issued && action == "cancel"
                ? "This order has been issued to the supplier. Cancelling issued orders comes with goods receipt."
                : $"Cannot {action} a purchase order that is {Status}.");
    }
}

/// <summary>What one PO line is made from: the RFQ line, its requisition, and the awarded price.</summary>
public sealed record OrderedLine(
    Guid SourcingEventLineId, Guid RequisitionId, Guid RequisitionLineId, string RequisitionNumber, Guid CostCentreId,
    int BudgetFiscalYear, Guid? ItemId, string? ItemCode, string Description, decimal Quantity, string UomCode,
    decimal UnitPrice);
