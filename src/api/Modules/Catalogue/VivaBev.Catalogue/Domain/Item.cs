using VivaBev.SharedKernel.Domain;

namespace VivaBev.Catalogue.Domain;

public sealed class Item : AggregateRoot
{
    public const int CodeMaxLength = 30;

    private Item() { }

    public Item(
        string code,
        string name,
        ItemType itemType,
        Guid categoryId,
        string baseUomCode,
        bool isStocked,
        bool isLotControlled,
        int? shelfLifeDays = null,
        string? description = null)
    {
        Code = Guard.Code(code, nameof(code), CodeMaxLength);
        Name = Guard.Required(name, nameof(name), 200);
        ItemType = itemType;
        CategoryId = categoryId;
        BaseUomCode = Guard.Code(baseUomCode, nameof(baseUomCode), UnitOfMeasure.CodeMaxLength);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SetStockControl(isStocked, isLotControlled, shelfLifeDays);
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public ItemType ItemType { get; private set; }
    public Guid CategoryId { get; private set; }
    public string BaseUomCode { get; private set; } = null!;
    public bool IsStocked { get; private set; }
    public bool IsLotControlled { get; private set; }
    public int? ShelfLifeDays { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void SetStockControl(bool isStocked, bool isLotControlled, int? shelfLifeDays)
    {
        if (ItemType == ItemType.Service && (isStocked || isLotControlled))
            throw new DomainException("A service cannot be stocked or lot-controlled.");

        if (isLotControlled && !isStocked)
            throw new DomainException("Only stocked items can be lot-controlled.");

        if (shelfLifeDays is <= 0)
            throw new DomainException("Shelf life must be a positive number of days.");

        IsStocked = isStocked;
        IsLotControlled = isLotControlled;
        ShelfLifeDays = shelfLifeDays;
    }

    public void Deactivate() => IsActive = false;
}