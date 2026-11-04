namespace VivaBev.Approvals.Contracts;

/// <summary>What a module hands to Approvals when a document needs approving.</summary>
public sealed record ApprovalSubject(
    string DocumentType, Guid DocumentId, string DocumentNumber, string Title, decimal Amount, Guid RequestedBy,
    bool OverBudget = false);

public interface IApprovals
{
    /// <summary>Builds the chain from policy and starts it. Call inside the caller's transaction.</summary>
    Task StartAsync(ApprovalSubject subject, CancellationToken ct = default);

    /// <summary>Withdraws the pending request for a document, if there is one.</summary>
    Task WithdrawAsync(string documentType, Guid documentId, CancellationToken ct = default);

    /// <summary>True if the user is an approver at any step of the document's requests.</summary>
    Task<bool> IsApproverAsync(string documentType, Guid documentId, Guid userId, CancellationToken ct = default);

    /// <summary>The most recent approval request for a document, with its steps; null if it was never submitted.
    /// No access check: the owning module decides who may see its documents.</summary>
    Task<ApprovalStatusDto?> GetLatestAsync(string documentType, Guid documentId, CancellationToken ct = default);
}

/// <summary>Implemented by the module that owns a document type, to apply the final decision.</summary>
public interface IApprovalOutcomeHandler
{
    string DocumentType { get; }
    Task ApplyApprovedAsync(Guid documentId, CancellationToken ct);
    Task ApplyRejectedAsync(Guid documentId, string reason, CancellationToken ct);
}

// ---- API shapes ----

public sealed record DecisionRequest(string? Comment);

public sealed record ApprovalTaskDto(
    Guid RequestId, string DocumentType, Guid DocumentId, string DocumentNumber, string Title, decimal Amount,
    string RequestedByName, DateTime RequestedAt, string StepName, int StepNumber, int TotalSteps, bool OverBudget);

/// <summary>A request you have already approved or rejected, with your own decision.</summary>
public sealed record DecidedTaskDto(
    Guid RequestId, string DocumentType, Guid DocumentId, string DocumentNumber, string Title, decimal Amount,
    string RequestedByName, string MyDecision, DateTime DecidedAt, string? MyComment, string RequestStatus,
    bool OverBudget);

public sealed record ApprovalStepDto(
    int Sequence, string Name, Guid ApproverId, string ApproverName, string Decision, DateTime? DecidedAt, string? Comment);

public sealed record ApprovalStatusDto(
    Guid RequestId, string DocumentNumber, string Status, decimal Amount, IReadOnlyList<ApprovalStepDto> Steps);
