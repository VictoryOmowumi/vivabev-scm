using VivaBev.SharedKernel.Domain;

namespace VivaBev.Catalogue.Domain;

public sealed class UnitOfMeasure : AggregateRoot
{
    public const int CodeMaxLength = 10;

    private UnitOfMeasure() { }

    public UnitOfMeasure(string code, string name, UomDimension dimension)
    {
        Code = Guard.Code(code, nameof(code), CodeMaxLength);
        Name = Guard.Required(name, nameof(name), 50);
        Dimension = dimension;
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public UomDimension Dimension { get; private set; }
}