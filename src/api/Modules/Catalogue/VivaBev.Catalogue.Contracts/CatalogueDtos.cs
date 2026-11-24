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
    bool IsActive,
    Guid CategoryId);

public sealed record ItemDetailDto(
    Guid Id, string Code, string Name, string? Description, string ItemType, Guid CategoryId, string CategoryCode,
    string CategoryName, string BaseUomCode, bool IsStocked, bool IsLotControlled, int? ShelfLifeDays, bool IsActive);

// ---- requests ----

public sealed record CreateItemRequest(
    string Code, string Name, string ItemType, Guid CategoryId, string BaseUomCode, bool IsStocked,
    bool IsLotControlled, int? ShelfLifeDays, string? Description);

public sealed record UpdateItemRequest(
    string Name, string? Description, Guid CategoryId, bool IsStocked, bool IsLotControlled, int? ShelfLifeDays);

public sealed record CreateCategoryRequest(string Code, string Name);

public sealed record RenameCategoryRequest(string Name);