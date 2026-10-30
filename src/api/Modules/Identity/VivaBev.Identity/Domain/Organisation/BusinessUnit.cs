using VivaBev.SharedKernel.Domain;

namespace VivaBev.Identity.Domain.Organisation;

public sealed class BusinessUnit : AggregateRoot
{
    public const int CodeMaxLength = 10;

    private BusinessUnit() { }

    public BusinessUnit(string code, string name)
    {
        Code = Guard.Code(code, nameof(code), CodeMaxLength);
        Name = Guard.Required(name, nameof(name), 100);
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    public void Rename(string name) => Name = Guard.Required(name, nameof(name), 100);
    public void Deactivate() => IsActive = false;
}
