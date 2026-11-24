namespace VivaBev.Procurement.Contracts;

/// <summary>What the warehouse (Inventory) needs from purchase orders: what's due in, and recording what arrived.</summary>
public interface IPurchaseOrderReceiving
{
    /// <summary>Issued and partly received orders, oldest expected delivery first.</summary>
    Task<IReadOnlyList<ReceivableOrderDto>> GetReceivableAsync(CancellationToken ct = default);

    /// <summary>Orders Finance can invoice against: issued, partly received or received, newest first.</summary>
    Task<IReadOnlyList<ReceivableOrderDto>> GetInvoiceableAsync(CancellationToken ct = default);

    /// <summary>Any order (whatever its status), with how much of each line is still to come. Null if not found.</summary>
    Task<ReceivableOrderDto?> GetOrderAsync(Guid purchaseOrderId, CancellationToken ct = default);

    /// <summary>Adds accepted quantities to the order's lines and moves it to PartiallyReceived or Received.
    /// Call inside the caller's transaction.</summary>
    Task RecordReceiptAsync(Guid purchaseOrderId, IReadOnlyCollection<ReceivedQuantity> lines, CancellationToken ct = default);
}

public sealed record ReceivedQuantity(Guid PurchaseOrderLineId, decimal Quantity);

public sealed record ReceivableOrderDto(
    Guid Id, string Number, string Status, Guid SupplierId, Guid DeliverToSiteId, DateOnly ExpectedDelivery,
    DateTime? IssuedAt, decimal Total, IReadOnlyList<ReceivableLineDto> Lines, int PaymentTermsDays);

public sealed record ReceivableLineDto(
    Guid Id, int LineNo, Guid? ItemId, string? ItemCode, string Description, string UomCode, decimal Ordered,
    decimal Received, decimal Outstanding, decimal UnitPrice, Guid CostCentreId, int BudgetFiscalYear);
