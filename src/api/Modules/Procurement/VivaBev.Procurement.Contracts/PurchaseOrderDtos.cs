namespace VivaBev.Procurement.Contracts;

// ---- requests ----

public sealed record CreatePurchaseOrderRequest(Guid SourcingEventId);

public sealed record UpdatePurchaseOrderDeliveryRequest(Guid DeliverToSiteId, DateOnly ExpectedDelivery, string? Notes);

public sealed record PurchaseOrderReasonRequest(string Reason);

// ---- responses ----

public sealed record PurchaseOrderSummaryDto(
    Guid Id, string Number, string Status, Guid SupplierId, string SupplierName, Guid SourcingEventId,
    string SourcingEventNumber, decimal Total, DateOnly ExpectedDelivery, DateTime? IssuedAt, int LineCount,
    IReadOnlyList<string> RequisitionNumbers);

public sealed record PurchaseOrderLineDto(
    Guid Id, int LineNo, Guid RequisitionId, string RequisitionNumber, string CostCentreCode, string? ItemCode,
    string Description, decimal Quantity, string UomCode, decimal UnitPrice, decimal LineTotal);

/// <summary>Who the order is addressed to, as it prints on the PO.</summary>
public sealed record PurchaseOrderSupplierDto(
    Guid Id, string Code, string Name, string LegalName, string TaxId, string? Address,
    string? ContactName, string? ContactEmail, string? ContactPhone);

public sealed record PurchaseOrderSiteDto(Guid Id, string Code, string Name, string City, string State);

public sealed record PurchaseOrderDetailDto(
    Guid Id,
    string Number,
    string Status,
    PurchaseOrderSupplierDto Supplier,
    Guid SourcingEventId,
    string SourcingEventNumber,
    string? SupplierReference,
    string BuyerName,
    int PaymentTermsDays,
    PurchaseOrderSiteDto DeliverTo,
    DateOnly ExpectedDelivery,
    string? Notes,
    string CurrencyCode,
    DateTime? IssuedAt,
    string? CancellationReason,
    decimal Total,
    IReadOnlyList<PurchaseOrderLineDto> Lines);
