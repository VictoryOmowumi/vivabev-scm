using VivaBev.SharedKernel.Domain;

namespace VivaBev.Catalogue.Domain;

public sealed class Category : AggregateRoot
{
    public const int CodeMaxLength = 30;

    private Category() { }

    public Category(string code, string name, Guid? parentId = null)
    {
        Code = Guard.Code(code, nameof(code), CodeMaxLength);
        Name = Guard.Required(name, nameof(name), 100);
        ParentId = parentId;
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Guid? ParentId { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Rename(string name) => Name = Guard.Required(name, nameof(name), 100);

    public void Deactivate() => IsActive = false;
}