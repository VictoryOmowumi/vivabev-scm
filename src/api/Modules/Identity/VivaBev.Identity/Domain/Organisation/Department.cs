using VivaBev.SharedKernel.Domain;

namespace VivaBev.Identity.Domain.Organisation;

public sealed class Department : AggregateRoot
{
    public const int CodeMaxLength = 10;

    private readonly List<CostCentre> _costCentres = [];

    private Department() { }

    public Department(string code, string name, Guid businessUnitId)
    {
        Code = Guard.Code(code, nameof(code), CodeMaxLength);
        Name = Guard.Required(name, nameof(name), 100);
        BusinessUnitId = businessUnitId;
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Guid BusinessUnitId { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<CostCentre> CostCentres => _costCentres.AsReadOnly();

    public CostCentre AddCostCentre(string code, string name)
    {
        if (!IsActive)
            throw new DomainException($"Department {Code} is inactive; it cannot have new cost centres.");

        var costCentre = new CostCentre(Id, code, name);
        if (_costCentres.Any(c => c.Code == costCentre.Code))
            throw new DomainException($"Department {Code} already has cost centre {costCentre.Code}.");

        _costCentres.Add(costCentre);
        return costCentre;
    }

    public void DeactivateCostCentre(Guid costCentreId)
    {
        var costCentre = _costCentres.SingleOrDefault(c => c.Id == costCentreId)
            ?? throw new DomainException($"Cost centre {costCentreId} does not belong to department {Code}.");

        costCentre.Deactivate();
    }

    public void Deactivate()
    {
        IsActive = false;
        _costCentres.ForEach(c => c.Deactivate());
    }

    public void Rename(string name) => Name = Guard.Required(name, nameof(name), 100);
}
