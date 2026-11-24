namespace VivaBev.Finance.Contracts;

// ---- requests ----

public sealed record InvoiceLineRequest(Guid PurchaseOrderLineId, decimal Quantity, decimal UnitPrice);

public sealed record RecordInvoiceRequest(Guid PurchaseOrderId, string SupplierInvoiceNumber, DateOnly InvoiceDate,
    decimal VatAmount, IReadOnlyList<InvoiceLineRequest> Lines);

public sealed record InvoiceDecisionRequest(string Reason);

public sealed record PayInvoiceRequest(string PaymentReference, DateOnly PaidOn);

// ---- responses ----

public sealed record InvoiceSummaryDto(
    Guid Id, string Number, string Status, Guid SupplierId, string SupplierName, string SupplierInvoiceNumber,
    Guid PurchaseOrderId, string PurchaseOrderNumber, DateOnly InvoiceDate, DateOnly DueDate, decimal Total,
    int Exceptions, DateTime RecordedAt);

public sealed record InvoiceLineDto(
    int LineNo, Guid PurchaseOrderLineId, string Description, string UomCode, decimal Quantity, decimal UnitPrice,
    decimal LineTotal, decimal OrderedUnitPrice, decimal? Received, decimal? InvoicedBefore, string? Exception);

public sealed record InvoiceDetailDto(
    Guid Id, string Number, string Status, Guid SupplierId, string SupplierName, string SupplierInvoiceNumber,
    Guid PurchaseOrderId, string PurchaseOrderNumber, DateOnly InvoiceDate, DateOnly DueDate, decimal Subtotal,
    decimal VatAmount, decimal Total, string CurrencyCode, string RecordedByName, DateTime RecordedAt,
    DateTime? MatchedAt, string? DecisionReason, string? DecidedByName, DateTime? DecidedAt,
    string? PaymentReference, DateOnly? PaidOn, IReadOnlyList<InvoiceLineDto> Lines);

/// <summary>An order Finance can invoice against, with how much of each line is received and already billed.</summary>
public sealed record InvoiceableOrderDto(
    Guid Id, string Number, string Status, Guid SupplierId, string SupplierName, int PaymentTermsDays,
    decimal Total, decimal Invoiced, IReadOnlyList<InvoiceableLineDto> Lines);

public sealed record InvoiceableLineDto(
    Guid Id, int LineNo, string? ItemCode, string Description, string UomCode, decimal Ordered, decimal Received,
    decimal Invoiced, decimal UnitPrice);
