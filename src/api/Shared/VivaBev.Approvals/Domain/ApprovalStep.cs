using VivaBev.SharedKernel.Domain;

namespace VivaBev.Approvals.Domain;

public sealed class ApprovalStep : AuditableEntity
{
    private ApprovalStep() { }

    internal ApprovalStep(Guid requestId, int sequence, string name, Guid approverId)
    {
        RequestId = requestId;
        Sequence = sequence;
        Name = name;
        ApproverId = approverId;
        Decision = StepDecision.Pending;
    }

    public Guid RequestId { get; private set; }
    public int Sequence { get; private set; }
    public string Name { get; private set; } = null!;
    public Guid ApproverId { get; private set; }
    public StepDecision Decision { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? Comment { get; private set; }

    internal void Approve(string? comment, DateTime now)
    {
        Decision = StepDecision.Approved;
        DecidedAt = now;
        Comment = Clean(comment);
    }

    internal void Reject(string reason, DateTime now)
    {
        Decision = StepDecision.Rejected;
        DecidedAt = now;
        Comment = Clean(reason);
    }

    private static string? Clean(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var trimmed = text.Trim();
        return trimmed.Length <= 500 ? trimmed : throw new DomainException("Comments must be 500 characters or fewer.");
    }
}
