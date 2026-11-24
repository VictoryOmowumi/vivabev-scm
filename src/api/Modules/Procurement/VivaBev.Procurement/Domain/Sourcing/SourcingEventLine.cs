using VivaBev.SharedKernel.Domain;

namespace VivaBev.Procurement.Domain.Sourcing;

/// <summary>One thing being quoted for: a copy of a requisition line, frozen when it joined the event.</summary>
public sealed class SourcingEventLine : AuditableEntity
{
    private SourcingEventLine() { }

    internal SourcingEventLine(Guid sourcingEventId, int lineNo, SourcedLine source)
    {
        SourcingEventId = sourcingEventId;
        LineNo = lineNo;
        RequisitionId = source.RequisitionId;
        RequisitionLineId = source.RequisitionLineId;
        RequisitionNumber = source.RequisitionNumber;
        ItemId = source.ItemId;
        ItemCode = source.ItemCode;
        Description = source.Description;
        CategoryId = source.CategoryId;
        Quantity = source.Quantity;
        UomCode = source.UomCode;
        EstimatedUnitPrice = source.EstimatedUnitPrice;
    }

    public Guid SourcingEventId { get; private set; }
    public int LineNo { get; private set; }
    public Guid RequisitionId { get; private set; }
    public Guid RequisitionLineId { get; private set; }
    public string RequisitionNumber { get; private set; } = null!;
    public Guid? ItemId { get; private set; }
    public string? ItemCode { get; private set; }
    public string Description { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public decimal Quantity { get; private set; }
    public string UomCode { get; private set; } = null!;
    public decimal EstimatedUnitPrice { get; private set; }

    public decimal EstimatedTotal => decimal.Round(Quantity * EstimatedUnitPrice, 2);
}

/// <summary>What the event needs from a requisition line. Built by the application layer.</summary>
public sealed record SourcedLine(
    Guid RequisitionId, string RequisitionNumber, Guid RequisitionLineId, Guid? ItemId, string? ItemCode,
    string Description, Guid CategoryId, decimal Quantity, string UomCode, decimal EstimatedUnitPrice);
