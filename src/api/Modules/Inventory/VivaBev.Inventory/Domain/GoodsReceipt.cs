using VivaBev.SharedKernel.Domain;

namespace VivaBev.Inventory.Domain;

/// <summary>
/// A goods received note (GRN): what arrived against one purchase order, line by line. Accepted quantities count
/// against the order and go into stock; rejected ones are sent back and need a reason. Posted when recorded.
/// </summary>
public sealed class GoodsReceipt : AggregateRoot
{
    private readonly List<GoodsReceiptLine> _lines = [];

    private GoodsReceipt() { }

    public GoodsReceipt(string number, Guid purchaseOrderId, string purchaseOrderNumber, Guid supplierId, Guid siteId,
        string? deliveryNoteNumber, string? notes, Guid receivedBy, DateTime receivedAt, IReadOnlyCollection<ReceiptLine> lines)
    {
        var counted = lines.Where(l => l.Accepted + l.Rejected > 0).ToList();
        if (counted.Count == 0)
            throw new DomainException("Enter a quantity for at least one line.");
        if (notes?.Trim().Length > 1000)
            throw new DomainException("Keep the notes under 1,000 characters.");

        Number = Guard.Code(number, nameof(number), 20);
        PurchaseOrderId = purchaseOrderId;
        PurchaseOrderNumber = Guard.Code(purchaseOrderNumber, nameof(purchaseOrderNumber), 20);
        SupplierId = supplierId;
        SiteId = siteId;
        DeliveryNoteNumber = string.IsNullOrWhiteSpace(deliveryNoteNumber) ? null : deliveryNoteNumber.Trim();
        if (DeliveryNoteNumber?.Length > 50)
            throw new DomainException("Keep the delivery note number under 50 characters.");
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        ReceivedBy = receivedBy;
        ReceivedAt = receivedAt;

        var lineNo = 1;
        foreach (var line in counted)
            _lines.Add(new GoodsReceiptLine(Id, lineNo++, line));
    }

    public string Number { get; private set; } = null!;
    public Guid PurchaseOrderId { get; private set; }
    public string PurchaseOrderNumber { get; private set; } = null!;
    public Guid SupplierId { get; private set; }
    public Guid SiteId { get; private set; }
    public string? DeliveryNoteNumber { get; private set; }
    public string? Notes { get; private set; }
    public Guid ReceivedBy { get; private set; }
    public DateTime ReceivedAt { get; private set; }

    public IReadOnlyCollection<GoodsReceiptLine> Lines => _lines.AsReadOnly();

    public decimal AcceptedValue => _lines.Sum(l => l.AcceptedValue);
    public bool HasRejections => _lines.Any(l => l.QuantityRejected > 0);
}

public sealed class GoodsReceiptLine : AuditableEntity
{
    private GoodsReceiptLine() { }

    internal GoodsReceiptLine(Guid goodsReceiptId, int lineNo, ReceiptLine source)
    {
        if (source.Accepted < 0 || source.Rejected < 0)
            throw new DomainException("Quantities can't be negative.");
        if (source.Rejected > 0 && string.IsNullOrWhiteSpace(source.RejectionReason))
            throw new DomainException($"Say why {source.Description} was rejected.");

        GoodsReceiptId = goodsReceiptId;
        LineNo = lineNo;
        PurchaseOrderLineId = source.PurchaseOrderLineId;
        ItemId = source.ItemId;
        ItemCode = source.ItemCode;
        Description = source.Description;
        UomCode = source.UomCode;
        UnitPrice = source.UnitPrice;
        QuantityAccepted = source.Accepted;
        QuantityRejected = source.Rejected;
        RejectionReason = source.Rejected > 0 ? Guard.Required(source.RejectionReason, "rejectionReason", 500) : null;
    }

    public Guid GoodsReceiptId { get; private set; }
    public int LineNo { get; private set; }
    public Guid PurchaseOrderLineId { get; private set; }
    public Guid? ItemId { get; private set; }
    public string? ItemCode { get; private set; }
    public string Description { get; private set; } = null!;
    public string UomCode { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public decimal QuantityAccepted { get; private set; }
    public decimal QuantityRejected { get; private set; }
    public string? RejectionReason { get; private set; }

    public decimal AcceptedValue => decimal.Round(QuantityAccepted * UnitPrice, 2);
}

/// <summary>One counted line, as the receiver enters it, with what the order says about it.</summary>
public sealed record ReceiptLine(
    Guid PurchaseOrderLineId, Guid? ItemId, string? ItemCode, string Description, string UomCode, decimal UnitPrice,
    decimal Accepted, decimal Rejected, string? RejectionReason);
