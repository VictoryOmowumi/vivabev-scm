namespace VivaBev.Inventory.Contracts;

// ---- stock requests: requests ----

public sealed record StockRequestLineRequest(Guid ItemId, decimal Quantity);

public sealed record CreateStockRequestRequest(Guid SiteId, Guid CostCentreId, string Purpose, DateOnly? NeededBy,
    IReadOnlyList<StockRequestLineRequest> Lines);

public sealed record IssueLineRequest(Guid LineId, decimal Quantity);

public sealed record IssueStockRequest(IReadOnlyList<IssueLineRequest> Lines);

public sealed record StockDecisionRequest(string Reason);

// ---- stock requests: responses ----

public sealed record StockOptionDto(Guid Id, string Code, string Name);

/// <summary>What the request form chooses from: your department's cost centres and the sites holding stock.</summary>
public sealed record StockRequestFormDto(IReadOnlyList<StockOptionDto> CostCentres, IReadOnlyList<StockOptionDto> Sites,
    Guid? DefaultSiteId);

/// <summary>An item in stock at a site. Reserved is what open requests are still waiting for.</summary>
public sealed record AvailableStockDto(Guid ItemId, string ItemCode, string Description, string UomCode, decimal OnHand,
    decimal Reserved);

public sealed record StockRequestSummaryDto(
    Guid Id, string Number, string Status, string SiteCode, string CostCentreCode, string DepartmentCode,
    Guid RequestedById, string RequestedByName, DateTime RequestedAt, DateOnly? NeededBy, string Purpose,
    int LineCount, int LinesOutstanding);

public sealed record StockRequestLineDto(
    Guid Id, int LineNo, Guid ItemId, string ItemCode, string Description, string UomCode, decimal Requested,
    decimal Issued, decimal Outstanding, decimal OnHand);

/// <summary>One line of one issue, from the stock ledger.</summary>
public sealed record StockIssueDto(DateTime IssuedAt, string IssuedByName, string ItemCode, string Description,
    string UomCode, decimal Quantity, decimal Value);

public sealed record StockRequestDetailDto(
    Guid Id, string Number, string Status, Guid SiteId, string SiteCode, string SiteName, string CostCentreCode,
    string CostCentreName, string DepartmentCode, Guid RequestedById, string RequestedByName, DateTime RequestedAt,
    DateOnly? NeededBy, string Purpose, DateTime? LastIssuedAt, string? DecidedByName, DateTime? DecidedAt,
    string? DecisionReason, IReadOnlyList<StockRequestLineDto> Lines, IReadOnlyList<StockIssueDto> Issues);

// ---- stock counts ----

public sealed record StartStockCountRequest(Guid SiteId);

public sealed record CountLineRequest(Guid LineId, decimal? Counted);

public sealed record RecordCountsRequest(IReadOnlyList<CountLineRequest> Lines);

public sealed record PostStockCountRequest(string? Notes);

public sealed record StockCountSummaryDto(
    Guid Id, string Number, string Status, string SiteCode, string StartedByName, DateTime StartedAt, int LineCount,
    int LinesCounted, int LinesDifferent, decimal VarianceValue, DateTime? PostedAt);

public sealed record StockCountLineDto(
    Guid Id, int LineNo, string ItemCode, string Description, string UomCode, decimal Expected, decimal? Counted,
    decimal Variance, decimal UnitCost, decimal VarianceValue);

public sealed record StockCountDetailDto(
    Guid Id, string Number, string Status, Guid SiteId, string SiteCode, string SiteName, string StartedByName,
    DateTime StartedAt, string? PostedByName, DateTime? PostedAt, string? Notes, IReadOnlyList<StockCountLineDto> Lines);
