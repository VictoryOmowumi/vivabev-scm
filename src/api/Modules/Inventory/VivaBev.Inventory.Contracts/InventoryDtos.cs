namespace VivaBev.Inventory.Contracts;

// ---- requests ----

public sealed record ReceiveLineRequest(Guid PurchaseOrderLineId, decimal QuantityAccepted, decimal QuantityRejected,
    string? RejectionReason);

public sealed record ReceiveGoodsRequest(Guid PurchaseOrderId, string? DeliveryNoteNumber, string? Notes,
    IReadOnlyList<ReceiveLineRequest> Lines);

// ---- responses ----

/// <summary>An order due in at the warehouse, with what's still to come.</summary>
public sealed record ReceivableOrderSummaryDto(
    Guid Id, string Number, string Status, Guid SupplierId, string SupplierName, Guid SiteId, string SiteCode,
    string SiteName, DateOnly ExpectedDelivery, int LinesOutstanding, int LineCount, decimal Total);

public sealed record ReceivableOrderLineDto(
    Guid Id, int LineNo, string? ItemCode, string Description, string UomCode, decimal Ordered, decimal Received,
    decimal Outstanding, bool Stocked);

public sealed record ReceivableOrderDetailDto(
    Guid Id, string Number, string Status, Guid SupplierId, string SupplierName, Guid SiteId, string SiteCode,
    string SiteName, DateOnly ExpectedDelivery, IReadOnlyList<ReceivableOrderLineDto> Lines,
    IReadOnlyList<GoodsReceiptSummaryDto> Receipts);

public sealed record GoodsReceiptSummaryDto(
    Guid Id, string Number, Guid PurchaseOrderId, string PurchaseOrderNumber, string SupplierName, string SiteCode,
    string? DeliveryNoteNumber, string ReceivedByName, DateTime ReceivedAt, int LineCount, decimal AcceptedValue,
    bool HasRejections);

public sealed record GoodsReceiptLineDto(
    int LineNo, string? ItemCode, string Description, string UomCode, decimal QuantityAccepted, decimal QuantityRejected,
    string? RejectionReason, decimal UnitPrice, decimal AcceptedValue);

public sealed record GoodsReceiptDetailDto(
    Guid Id, string Number, Guid PurchaseOrderId, string PurchaseOrderNumber, Guid SupplierId, string SupplierName,
    string SiteCode, string SiteName, string? DeliveryNoteNumber, string? Notes, string ReceivedByName,
    DateTime ReceivedAt, decimal AcceptedValue, IReadOnlyList<GoodsReceiptLineDto> Lines);

/// <summary>Stock of one item at one site: the sum of its movements, valued at the average cost.</summary>
public sealed record StockBalanceDto(
    Guid SiteId, string SiteCode, Guid ItemId, string ItemCode, string Description, string UomCode, decimal OnHand,
    decimal AverageCost, decimal Value, DateTime LastMovementAt);

/// <summary>One line in an item's stock ledger. DocumentType is GoodsReceipt, StockRequest or StockCount.</summary>
public sealed record StockMovementDto(DateTime OccurredAt, string DocumentType, Guid DocumentId, string DocumentNumber,
    decimal Quantity, decimal UnitCost);
