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

public sealed record UpdateRequisitionRequest(
    string Title, string Justification, Guid CostCentreId, Guid DeliverToSiteId, DateOnly RequiredBy);

public sealed record CancelRequisitionRequest(string Reason);

// ---- responses ----

public sealed record RequisitionSummaryDto(
    Guid Id,
    string Number,
    string Title,
    string Status,
    bool Returned,            // a draft an approver sent back
    Guid RequesterId,
    string RequesterName,
    string CostCentreCode,
    string BudgetStatus,
    DateOnly RequiredBy,
    decimal EstimatedTotal,
    string CurrencyCode,
    DateTime? SubmittedAt);

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
    string? RejectionReason,
    DateTime? ApprovedAt,
    string BudgetStatus,
    decimal? BudgetAvailableAtSubmit,
    IReadOnlyList<RequisitionLineDto> Lines,
    Guid CostCentreId,
    Guid DeliverToSiteId);

/// <summary>A cost centre the requester may charge, with where its budget stands right now.</summary>
public sealed record CostCentreOptionDto(
    Guid Id,
    string Code,
    string Name,
    int FiscalYear,
    bool HasBudget,
    decimal Budget,
    decimal Available,    // budget - committed - consumed
    decimal InApproval);  // submitted requisitions not yet approved (not committed yet)
