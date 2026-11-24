namespace VivaBev.Procurement.Contracts;

// ---- requests ----

public sealed record CreateSourcingEventRequest(string Title, DateOnly ResponseDeadline, IReadOnlyList<Guid> RequisitionIds);

public sealed record AddRequisitionToEventRequest(Guid RequisitionId);

public sealed record InviteSupplierRequest(Guid SupplierId);

public sealed record ChangeDeadlineRequest(DateOnly ResponseDeadline);

public sealed record QuotationLineRequest(Guid SourcingEventLineId, decimal UnitPrice, decimal Quantity, string? Notes = null);

public sealed record RecordQuotationRequest(
    Guid SupplierId,
    string? SupplierReference,
    DateOnly ValidUntil,
    int LeadTimeDays,
    int PaymentTermsDays,
    IReadOnlyList<QuotationLineRequest> Lines);

public sealed record SourcingReasonRequest(string Reason);

// ---- responses ----

public sealed record SourcingEventSummaryDto(
    Guid Id,
    string Number,
    string Title,
    string Status,
    string BuyerName,
    DateOnly ResponseDeadline,
    int LineCount,
    IReadOnlyList<string> RequisitionNumbers,
    int Invited,
    int Quoted,
    decimal EstimatedTotal,
    decimal? LowestQuote,       // lowest total among received quotations that price every line
    DateTime? PublishedAt);

public sealed record SourcingEventLineDto(
    Guid Id, int LineNo, Guid RequisitionId, string RequisitionNumber, Guid? ItemId, string? ItemCode,
    string Description, Guid CategoryId, decimal Quantity, string UomCode, decimal EstimatedUnitPrice,
    decimal EstimatedTotal);

public sealed record SourcingInvitationDto(Guid SupplierId, string SupplierName, string Status, DateTime? InvitedAt);

public sealed record QuotationLineDto(Guid SourcingEventLineId, decimal UnitPrice, decimal Quantity, decimal LineTotal, string? Notes);

public sealed record QuotationDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    string? SupplierReference,
    DateTime ReceivedAt,
    DateOnly ValidUntil,
    int LeadTimeDays,
    int PaymentTermsDays,
    decimal Total,
    string Status,
    string? WithdrawalReason,
    IReadOnlyList<QuotationLineDto> Lines);

public sealed record SourcingEventDetailDto(
    Guid Id,
    string Number,
    string Title,
    string Status,
    Guid BuyerId,
    string BuyerName,
    DateOnly ResponseDeadline,
    string CurrencyCode,
    DateTime? PublishedAt,
    DateTime? ResponsesClosedAt,
    string? CancellationReason,
    decimal EstimatedTotal,
    bool NeedsCompetition,      // estimated total at or above the competitive threshold
    int MinimumSuppliers,
    decimal CompetitiveThreshold,
    IReadOnlyList<SourcingEventLineDto> Lines,
    IReadOnlyList<SourcingInvitationDto> Invitations,
    IReadOnlyList<QuotationDto> Quotations);

/// <summary>An approved requisition waiting to be put on an RFQ.</summary>
public sealed record SourceableRequisitionDto(
    Guid Id, string Number, string Title, string RequesterName, string CostCentreCode, DateOnly RequiredBy,
    decimal EstimatedTotal, int LineCount, DateTime? ApprovedAt);

/// <summary>A supplier who may be invited, and how many of the RFQ's lines it's approved to quote for.</summary>
public sealed record EligibleSupplierDto(
    Guid Id, string Code, string Name, int PaymentTermsDays, int LinesCovered, int TotalLines, bool Invited);
