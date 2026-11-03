using VivaBev.SharedKernel.Domain;

namespace VivaBev.Approvals.Domain;

public sealed class ApprovalPolicyStep : AggregateRoot
{
    private ApprovalPolicyStep() { }

    public ApprovalPolicyStep(string documentType, int sequence, string name, decimal minAmount,
        ApproverRule rule, string? roleName = null, bool overBudgetOnly = false)
    {
        if (sequence < 1)
            throw new DomainException("Sequence starts at 1.");
        if (minAmount < 0)
            throw new DomainException("The minimum amount cannot be negative.");
        if (rule != ApproverRule.RequesterManager && string.IsNullOrWhiteSpace(roleName))
            throw new DomainException($"The {rule} rule needs a role name.");

        DocumentType = Guard.Required(documentType, nameof(documentType), 30);
        Sequence = sequence;
        Name = Guard.Required(name, nameof(name), 100);
        MinAmount = minAmount;
        Rule = rule;
        RoleName = string.IsNullOrWhiteSpace(roleName) ? null : roleName.Trim();
        OverBudgetOnly = overBudgetOnly;
    }

    public string DocumentType { get; private set; } = null!;
    public int Sequence { get; private set; }
    public string Name { get; private set; } = null!;

    /// <summary>The step applies when the document's amount is greater than this.</summary>
    public decimal MinAmount { get; private set; }

    public ApproverRule Rule { get; private set; }
    public string? RoleName { get; private set; }

    /// <summary>The step only applies when the document is over budget.</summary>
    public bool OverBudgetOnly { get; private set; }

    public bool IsActive { get; private set; } = true;

    public void Deactivate() => IsActive = false;
}
