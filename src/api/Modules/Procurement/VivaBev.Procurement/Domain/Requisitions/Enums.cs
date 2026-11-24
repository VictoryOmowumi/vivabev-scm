namespace VivaBev.Procurement.Domain.Requisitions;

public enum RequisitionStatus
{
    Draft,
    PendingApproval,
    Approved,
    InSourcing, // on an RFQ; Procurement is collecting quotations
    Cancelled
}

public enum BudgetStatus
{
    NotChecked,
    WithinBudget,
    OverBudget
}

public enum LineType
{
    Goods,
    Service
}
