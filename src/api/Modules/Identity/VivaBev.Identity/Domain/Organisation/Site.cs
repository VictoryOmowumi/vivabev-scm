using VivaBev.SharedKernel.Domain;

namespace VivaBev.Identity.Domain.Organisation;

public enum SiteType
{
    Plant,
    Warehouse,
    Office
}

public sealed class Site : AggregateRoot
{
    public const int CodeMaxLength = 10;

    private Site() { }

    public Site(string code, string name, SiteType siteType, Guid businessUnitId, string city, string state)
    {
        Code = Guard.Code(code, nameof(code), CodeMaxLength);
        Name = Guard.Required(name, nameof(name), 100);
        SiteType = siteType;
        BusinessUnitId = businessUnitId;
        City = Guard.Required(city, nameof(city), 100);
        State = Guard.Required(state, nameof(state), 100);
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public SiteType SiteType { get; private set; }
    public Guid BusinessUnitId { get; private set; }
    public string City { get; private set; } = null!;
    public string State { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    public bool CanHoldStock => SiteType is SiteType.Plant or SiteType.Warehouse;

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
}
