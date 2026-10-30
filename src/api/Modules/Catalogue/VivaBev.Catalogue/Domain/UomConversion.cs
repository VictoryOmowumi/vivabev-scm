using VivaBev.SharedKernel.Domain;

namespace VivaBev.Catalogue.Domain;

public sealed class UomConversion : AuditableEntity
{
    private UomConversion() { }

    public UomConversion(string fromUomCode, string toUomCode, decimal factor, Guid? itemId = null)
    {
        FromUomCode = Guard.Code(fromUomCode, nameof(fromUomCode), UnitOfMeasure.CodeMaxLength);
        ToUomCode = Guard.Code(toUomCode, nameof(toUomCode), UnitOfMeasure.CodeMaxLength);

        if (FromUomCode == ToUomCode)
            throw new DomainException("A conversion needs two different units.");
        if (factor <= 0)
            throw new DomainException("The conversion factor must be greater than zero.");

        Factor = factor;
        ItemId = itemId;
    }

    public Guid? ItemId { get; private set; }
    public string FromUomCode { get; private set; } = null!;
    public string ToUomCode { get; private set; } = null!;
    public decimal Factor { get; private set; }
}