using VivaBev.SharedKernel.Domain;

namespace VivaBev.Procurement.Domain.PurchaseOrders;

public sealed class PurchaseOrderLine : AuditableEntity
{
    private PurchaseOrderLine() { }

    internal PurchaseOrderLine(Guid purchaseOrderId, int lineNo, OrderedLine source)
    {
        if (source.Quantity <= 0)
            throw new DomainException("Quantities must be more than zero.");
        if (source.UnitPrice < 0)
            throw new DomainException("Prices can't be negative.");

        PurchaseOrderId = purchaseOrderId;
        LineNo = lineNo;
        SourcingEventLineId = source.SourcingEventLineId;
        RequisitionId = source.RequisitionId;
        RequisitionLineId = source.RequisitionLineId;
        RequisitionNumber = source.RequisitionNumber;
        CostCentreId = source.CostCentreId;
        BudgetFiscalYear = source.BudgetFiscalYear;
        ItemId = source.ItemId;
        ItemCode = source.ItemCode;
        Description = source.Description;
        Quantity = source.Quantity;
        UomCode = source.UomCode;
        UnitPrice = source.UnitPrice;
    }

    public Guid PurchaseOrderId { get; private set; }
    public int LineNo { get; private set; }
    public Guid SourcingEventLineId { get; private set; }
    public Guid RequisitionId { get; private set; }
    public Guid RequisitionLineId { get; private set; }
    public string RequisitionNumber { get; private set; } = null!;
    public Guid CostCentreId { get; private set; }  // copied from the requisition, for the budget commitment
    public int BudgetFiscalYear { get; private set; }
    public Guid? ItemId { get; private set; }
    public string? ItemCode { get; private set; }
    public string Description { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public string UomCode { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => decimal.Round(Quantity * UnitPrice, 2);
}
