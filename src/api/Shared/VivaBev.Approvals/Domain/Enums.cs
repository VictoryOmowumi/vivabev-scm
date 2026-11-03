namespace VivaBev.Approvals.Domain;

public enum ApproverRule
{
    RequesterManager, // the requester's manager
    DepartmentRole,   // a holder of RoleName in the requester's department
    Role              // a holder of RoleName anywhere
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Withdrawn
}

public enum StepDecision
{
    Pending,
    Approved,
    Rejected
}
