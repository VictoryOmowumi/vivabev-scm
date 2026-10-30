using VivaBev.SharedKernel.Domain;

namespace VivaBev.Identity.Domain.Organisation;

public sealed class CostCentre : AuditableEntity
{
    public const int CodeMaxLength = 20;

    private CostCentre() { }

    internal CostCentre(Guid departmentId, string code, string name)
    {
        DepartmentId = departmentId;
        Code = Guard.Code(code, nameof(code), CodeMaxLength);
        Name = Guard.Required(name, nameof(name), 100);
    }

    public Guid DepartmentId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    internal void Deactivate() => IsActive = false;
}
