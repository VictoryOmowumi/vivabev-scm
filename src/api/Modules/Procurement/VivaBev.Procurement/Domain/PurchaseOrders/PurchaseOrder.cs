using VivaBev.SharedKernel.Domain;

namespace VivaBev.Procurement.Domain.PurchaseOrders;

public enum PurchaseOrderStatus
{
    Draft,     // raised from an award; the buyer checks delivery details
    Issued,            // sent to the supplier; requisitions are Ordered and the budget holds the real amount
    PartiallyReceived, // the warehouse has received some of it
    Received,          // everything ordered has been received
    Closed,            // partly received, and the rest won't come
    Cancelled          // dropped: a draft, or an issued order before anything arrived
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
    public string? CancellationReason { get; private set; } // why it was cancelled or closed
    public Guid? ClosedBy { get; private set; }               // who cancelled or closed it
    public DateTime? ClosedAt { get; private set; }

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

    /// <summary>The warehouse accepted these quantities against the order's lines (rejected goods don't count).</summary>
    public void RecordReceipt(IReadOnlyCollection<(Guid LineId, decimal Quantity)> received)
    {
        if (Status is not (PurchaseOrderStatus.Issued or PurchaseOrderStatus.PartiallyReceived))
            throw new DomainException($"Cannot receive against a purchase order that is {Status}.");
        if (received.Count == 0)
            throw new DomainException("Receive at least one line.");

        foreach (var (lineId, quantity) in received)
        {
            var line = _lines.SingleOrDefault(l => l.Id == lineId)
                ?? throw new DomainException("A received line is not on this purchase order.");
            line.Receive(quantity);
        }

        Status = _lines.All(l => l.Outstanding == 0) ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
    }

    /// <summary>
    /// Drop a draft, or an issued order before anything has arrived (the supplier can't deliver, say).
    /// Once goods have arrived it can only be closed short.
    /// </summary>
    public void Cancel(string reason, Guid by, DateTime now)
    {
        if (Status == PurchaseOrderStatus.PartiallyReceived)
            throw new DomainException("Some of this order has arrived. Close it short instead of cancelling it.");
        if (Status is not (PurchaseOrderStatus.Draft or PurchaseOrderStatus.Issued))
            throw new DomainException($"Cannot cancel a purchase order that is {Status}.");
        End(PurchaseOrderStatus.Cancelled, reason, by, now);
    }

    /// <summary>Stop waiting for the rest of a partly received order. What arrived stays received and billable.</summary>
    public void CloseShort(string reason, Guid by, DateTime now)
    {
        if (Status != PurchaseOrderStatus.PartiallyReceived)
            throw new DomainException(Status == PurchaseOrderStatus.Issued
                ? "Nothing has arrived on this order yet. Cancel it instead."
                : $"Only a partly received order can be closed short; this one is {Status}.");
        End(PurchaseOrderStatus.Closed, reason, by, now);
    }

    /// <summary>The value not yet received, per line: what closing short or cancelling takes off the budget.</summary>
    public IEnumerable<(PurchaseOrderLine Line, decimal Value)> OutstandingValue() =>
        _lines.Where(l => l.Outstanding > 0).Select(l => (l, decimal.Round(l.Outstanding * l.UnitPrice, 2)));

    private void End(PurchaseOrderStatus status, string reason, Guid by, DateTime now)
    {
        CancellationReason = Guard.Required(reason, nameof(reason), 500);
        Status = status;
        ClosedBy = by;
        ClosedAt = now;
    }

    private void EnsureDraft(string action)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new DomainException($"Cannot {action} a purchase order that is {Status}.");
    }
}

/// <summary>What one PO line is made from: the RFQ line, its requisition, and the awarded price.</summary>
public sealed record OrderedLine(
    Guid SourcingEventLineId, Guid RequisitionId, Guid RequisitionLineId, string RequisitionNumber, Guid CostCentreId,
    int BudgetFiscalYear, Guid? ItemId, string? ItemCode, string Description, decimal Quantity, string UomCode,
    decimal UnitPrice);
