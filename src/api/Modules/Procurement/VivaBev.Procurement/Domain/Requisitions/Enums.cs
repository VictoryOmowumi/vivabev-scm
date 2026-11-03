namespace VivaBev.Procurement.Domain.Requisitions;

public enum RequisitionStatus
{
    Draft,
    PendingApproval,
    Approved,
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
