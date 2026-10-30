namespace VivaBev.Identity.Contracts;

// ---- responses ----

public sealed record BusinessUnitDto(Guid Id, string Code, string Name, bool IsActive);

public sealed record CostCentreSummaryDto(Guid Id, string Code, string Name, bool IsActive);

public sealed record DepartmentDto(
    Guid Id,
    string Code,
    string Name,
    string BusinessUnitCode,
    bool IsActive,
    IReadOnlyList<CostCentreSummaryDto> CostCentres);

public sealed record SiteDto(
    Guid Id, string Code, string Name, string SiteType, string BusinessUnitCode,
    string City, string State, bool CanHoldStock, bool IsActive);

/// <summary>A cost centre with its department, as other modules need it.</summary>
public sealed record CostCentreDto(
    Guid Id, string Code, string Name, Guid DepartmentId, string DepartmentCode, bool IsActive);

// ---- requests ----

public sealed record CreateCostCentreRequest(string Code, string Name);
