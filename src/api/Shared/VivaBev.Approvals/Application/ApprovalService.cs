using Microsoft.EntityFrameworkCore;
using VivaBev.Approvals.Contracts;
using VivaBev.Approvals.Domain;
using VivaBev.Approvals.Infrastructure;
using VivaBev.Identity.Contracts;
using VivaBev.Infrastructure.Persistence;
using VivaBev.SharedKernel.Abstractions;
using VivaBev.SharedKernel.Domain;
using VivaBev.SharedKernel.Errors;

namespace VivaBev.Approvals.Application;

internal sealed class ApprovalService(
    ApprovalsDbContext db,
    IUserDirectory users,
    IEnumerable<IApprovalOutcomeHandler> handlers,
    CrossModuleTransaction transaction,
    ICurrentUser currentUser,
    TimeProvider clock) : IApprovals
{
    // ---------- called by modules ----------

    public async Task StartAsync(ApprovalSubject subject, CancellationToken ct = default)
    {
        var alreadyPending = await db.Requests.AnyAsync(r =>
            r.DocumentType == subject.DocumentType &&
            r.DocumentId == subject.DocumentId &&
            r.Status == ApprovalStatus.Pending, ct);
        if (alreadyPending)
            throw new DomainException($"{subject.DocumentNumber} is already awaiting approval.");

        var requester = await users.GetUserAsync(subject.RequestedBy, ct)
            ?? throw new NotFoundException($"User {subject.RequestedBy} was not found.");

        var policy = await db.PolicySteps.AsNoTracking()
            .Where(p => p.DocumentType == subject.DocumentType && p.IsActive && subject.Amount > p.MinAmount
                        && (!p.OverBudgetOnly || subject.OverBudget))
            .OrderBy(p => p.Sequence)
            .ToListAsync(ct);

        if (policy.Count == 0)
            throw new DomainException($"No approval policy is configured for {subject.DocumentType}.");

        var approvers = new List<(string Name, Guid ApproverId)>();
        foreach (var step in policy)
        {
            var approverId = await ResolveAsync(step, requester, ct)
                ?? throw new DomainException($"No approver found for '{step.Name}'. Ask an administrator to assign one.");
            approvers.Add((step.Name, approverId));
        }

        db.Requests.Add(new ApprovalRequest(subject.DocumentType, subject.DocumentId, subject.DocumentNumber,
            subject.Title, subject.Amount, subject.RequestedBy, Now, approvers, subject.OverBudget));
        await db.SaveChangesAsync(ct);
    }

    public async Task WithdrawAsync(string documentType, Guid documentId, CancellationToken ct = default)
    {
        var request = await db.Requests.SingleOrDefaultAsync(r =>
            r.DocumentType == documentType && r.DocumentId == documentId && r.Status == ApprovalStatus.Pending, ct);

        if (request is null)
            return;

        request.Withdraw(Now);
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> IsApproverAsync(string documentType, Guid documentId, Guid userId, CancellationToken ct = default) =>
        db.Requests.AsNoTracking().AnyAsync(r =>
            r.DocumentType == documentType &&
            r.DocumentId == documentId &&
            r.Steps.Any(s => s.ApproverId == userId), ct);

    // ---------- called by the approvals endpoints ----------

    public Task ApproveAsync(Guid requestId, string? comment, CancellationToken ct) =>
        transaction.RunAsync(async () =>
        {
            var (request, me) = await LoadForDecisionAsync(requestId, ct);
            request.Approve(me, comment, Now);
            await db.SaveChangesAsync(ct);

            if (request.Status == ApprovalStatus.Approved)
                await HandlerFor(request.DocumentType).ApplyApprovedAsync(request.DocumentId, ct);
        }, ct);

    public Task RejectAsync(Guid requestId, string? comment, CancellationToken ct) =>
        transaction.RunAsync(async () =>
        {
            var (request, me) = await LoadForDecisionAsync(requestId, ct);
            request.Reject(me, comment ?? string.Empty, Now);
            await db.SaveChangesAsync(ct);

            await HandlerFor(request.DocumentType).ApplyRejectedAsync(request.DocumentId, comment!.Trim(), ct);
        }, ct);

    // ---------- helpers ----------

    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    private async Task<Guid?> ResolveAsync(ApprovalPolicyStep step, UserSummaryDto requester, CancellationToken ct)
    {
        switch (step.Rule)
        {
            case ApproverRule.RequesterManager:
                return requester.ManagerId;
            case ApproverRule.DepartmentRole:
                var inDepartment = await users.GetUsersInRoleAsync(step.RoleName!, requester.DepartmentId, ct);
                return inDepartment.FirstOrDefault()?.Id;
            case ApproverRule.Role:
                var anywhere = await users.GetUsersInRoleAsync(step.RoleName!, null, ct);
                return anywhere.FirstOrDefault()?.Id;
            default:
                return null;
        }
    }

    private async Task<(ApprovalRequest Request, Guid Me)> LoadForDecisionAsync(Guid requestId, CancellationToken ct)
    {
        var me = currentUser.UserId ?? throw new ForbiddenException("You must be signed in.");

        var request = await db.Requests
            .Include(r => r.Steps)
            .SingleOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new NotFoundException($"Approval request {requestId} was not found.");

        if (request.Status != ApprovalStatus.Pending)
            throw new DomainException($"This request is already {request.Status}.");
        if (request.CurrentStep?.ApproverId != me)
            throw new ForbiddenException("This approval is not waiting for you.");

        return (request, me);
    }

    private IApprovalOutcomeHandler HandlerFor(string documentType) =>
        handlers.SingleOrDefault(h => h.DocumentType == documentType)
        ?? throw new InvalidOperationException($"No approval outcome handler is registered for {documentType}.");
}
