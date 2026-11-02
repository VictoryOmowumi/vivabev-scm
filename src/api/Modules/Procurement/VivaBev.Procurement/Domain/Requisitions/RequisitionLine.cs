using VivaBev.SharedKernel.Domain;

namespace VivaBev.Procurement.Domain.Requisitions;

public sealed class RequisitionLine : AuditableEntity
{
    private RequisitionLine() { }

    internal RequisitionLine(Guid requisitionId, int lineNo, LineType lineType, Guid? itemId, string? itemCode,
        string description, Guid categoryId, decimal quantity, string uomCode, decimal estimatedUnitPrice)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");
        if (estimatedUnitPrice <= 0)
            throw new DomainException("Estimated unit price must be greater than zero.");

        RequisitionId = requisitionId;
        LineNo = lineNo;
        LineType = lineType;
        ItemId = itemId;
        ItemCode = itemCode;
        Description = Guard.Required(description, nameof(description), 500);
        CategoryId = categoryId;
        Quantity = quantity;
        UomCode = Guard.Code(uomCode, nameof(uomCode), 10);
        EstimatedUnitPrice = estimatedUnitPrice;
        LineTotal = decimal.Round(quantity * estimatedUnitPrice, 2);
    }

    public Guid RequisitionId { get; private set; }
    public int LineNo { get; private set; }
    public LineType LineType { get; private set; }
    public Guid? ItemId { get; private set; }
    public string? ItemCode { get; private set; }
    public string Description { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public decimal Quantity { get; private set; }
    public string UomCode { get; private set; } = null!;
    public decimal EstimatedUnitPrice { get; private set; }
    public decimal LineTotal { get; private set; }
}
