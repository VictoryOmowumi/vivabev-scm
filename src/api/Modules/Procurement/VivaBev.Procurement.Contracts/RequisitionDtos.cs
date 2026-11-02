namespace VivaBev.Procurement.Contracts;

// ---- requests ----

public sealed record CreateRequisitionRequest(
    string Title, string Justification, Guid CostCentreId, Guid DeliverToSiteId, DateOnly RequiredBy);

/// <summary>Either ItemId (catalogue line) or Description + CategoryId + UomCode (free-text line).</summary>
public sealed record AddRequisitionLineRequest(
    decimal Quantity,
    decimal EstimatedUnitPrice,
    Guid? ItemId = null,
    string? Description = null,
    Guid? CategoryId = null,
    string? LineType = null,
    string? UomCode = null);

public sealed record CancelRequisitionRequest(string Reason);

// ---- responses ----

public sealed record RequisitionSummaryDto(
    Guid Id, string Number, string Title, string Status, Guid RequesterId,
    DateOnly RequiredBy, decimal EstimatedTotal, string CurrencyCode, DateTime? SubmittedAt);

public sealed record RequisitionLineDto(
    int LineNo, string LineType, Guid? ItemId, string? ItemCode, string Description, Guid CategoryId,
    decimal Quantity, string UomCode, decimal EstimatedUnitPrice, decimal LineTotal);

public sealed record RequisitionDetailDto(
    Guid Id,
    string Number,
    string Title,
    string Justification,
    string Status,
    Guid RequesterId,
    string RequesterName,
    string CostCentreCode,
    string DeliverToSiteCode,
    DateOnly RequiredBy,
    string CurrencyCode,
    decimal EstimatedTotal,
    DateTime? SubmittedAt,
    string? CancellationReason,
    IReadOnlyList<RequisitionLineDto> Lines);
