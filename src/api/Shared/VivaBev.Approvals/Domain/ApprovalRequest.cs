using VivaBev.SharedKernel.Domain;

namespace VivaBev.Approvals.Domain;

public sealed class ApprovalRequest : AggregateRoot
{
    private readonly List<ApprovalStep> _steps = [];

    private ApprovalRequest() { }

    public ApprovalRequest(string documentType, Guid documentId, string documentNumber, string title,
        decimal amount, Guid requestedBy, DateTime requestedAt, IEnumerable<(string Name, Guid ApproverId)> approvers,
        bool overBudget = false)
    {
        DocumentType = Guard.Required(documentType, nameof(documentType), 30);
        DocumentId = documentId;
        DocumentNumber = Guard.Required(documentNumber, nameof(documentNumber), 30);
        Title = Guard.Required(title, nameof(title), 200);
        Amount = amount;
        RequestedBy = requestedBy;
        RequestedAt = requestedAt;
        OverBudget = overBudget;
        Status = ApprovalStatus.Pending;

        // Segregation of duties: never the requester, and each person only once.
        var chain = approvers
            .Where(a => a.ApproverId != requestedBy)
            .DistinctBy(a => a.ApproverId)
            .ToList();

        if (chain.Count == 0)
            throw new DomainException("No one other than the requester can approve this.");

        var sequence = 1;
        foreach (var (name, approverId) in chain)
            _steps.Add(new ApprovalStep(Id, sequence++, name, approverId));
    }

    public string DocumentType { get; private set; } = null!;
    public Guid DocumentId { get; private set; }
    public string DocumentNumber { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public Guid RequestedBy { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public bool OverBudget { get; private set; }
    public ApprovalStatus Status { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public IReadOnlyCollection<ApprovalStep> Steps => _steps.AsReadOnly();

    /// <summary>The step waiting for a decision, or null when the request is finished.</summary>
    public ApprovalStep? CurrentStep =>
        Status == ApprovalStatus.Pending
            ? _steps.OrderBy(s => s.Sequence).FirstOrDefault(s => s.Decision == StepDecision.Pending)
            : null;

    public void Approve(Guid approverId, string? comment, DateTime now)
    {
        RequireTurnOf(approverId).Approve(comment, now);

        if (_steps.All(s => s.Decision == StepDecision.Approved))
        {
            Status = ApprovalStatus.Approved;
            CompletedAt = now;
        }
    }

    public void Reject(Guid approverId, string reason, DateTime now)
    {
        var cleanReason = Guard.Required(reason, nameof(reason), 500);
        RequireTurnOf(approverId).Reject(cleanReason, now);
        Status = ApprovalStatus.Rejected;
        CompletedAt = now;
    }

    public void Withdraw(DateTime now)
    {
        if (Status != ApprovalStatus.Pending)
            throw new DomainException($"Only a pending request can be withdrawn; this one is {Status}.");

        Status = ApprovalStatus.Withdrawn;
        CompletedAt = now;
    }

    private ApprovalStep RequireTurnOf(Guid approverId)
    {
        var step = CurrentStep
            ?? throw new DomainException($"This request is {Status}; there is nothing left to decide.");

        if (step.ApproverId != approverId)
            throw new DomainException("It is not this person's turn to decide.");

        return step;
    }
}
