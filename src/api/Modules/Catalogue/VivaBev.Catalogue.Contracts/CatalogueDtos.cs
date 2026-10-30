namespace VivaBev.Catalogue.Contracts;

public sealed record UnitOfMeasureDto(string Code, string Name, string Dimension);

public sealed record ItemSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string ItemType,
    string Category,
    string BaseUomCode,
    bool IsStocked,
    bool IsActive);