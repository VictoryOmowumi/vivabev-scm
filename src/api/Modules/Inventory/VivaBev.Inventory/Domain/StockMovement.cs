using VivaBev.SharedKernel.Domain;

namespace VivaBev.Inventory.Domain;

/// <summary>
/// One change to stock of a catalogue item at a site. Stock on hand is the sum of its movements; a receipt adds
/// stock, and later issues and adjustments will take it away. Movements are never edited, only added.
/// </summary>
public sealed class StockMovement : AggregateRoot
{
    public const string ReceiptDocumentType = "GoodsReceipt";

    private StockMovement() { }

    public StockMovement(Guid siteId, Guid itemId, string itemCode, string description, string uomCode, decimal quantity,
        decimal unitCost, string documentType, Guid documentId, string documentNumber, DateTime occurredAt)
    {
        if (quantity == 0)
            throw new DomainException("A stock movement needs a quantity.");

        SiteId = siteId;
        ItemId = itemId;
        ItemCode = Guard.Code(itemCode, nameof(itemCode), 30);
        Description = Guard.Required(description, nameof(description), 500);
        UomCode = Guard.Code(uomCode, nameof(uomCode), 10);
        Quantity = quantity;
        UnitCost = unitCost;
        DocumentType = Guard.Required(documentType, nameof(documentType), 30);
        DocumentId = documentId;
        DocumentNumber = Guard.Code(documentNumber, nameof(documentNumber), 20);
        OccurredAt = occurredAt;
    }

    public Guid SiteId { get; private set; }
    public Guid ItemId { get; private set; }
    public string ItemCode { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string UomCode { get; private set; } = null!;
    public decimal Quantity { get; private set; }  // positive in, negative out
    public decimal UnitCost { get; private set; }
    public string DocumentType { get; private set; } = null!;
    public Guid DocumentId { get; private set; }
    public string DocumentNumber { get; private set; } = null!;
    public DateTime OccurredAt { get; private set; }
}
