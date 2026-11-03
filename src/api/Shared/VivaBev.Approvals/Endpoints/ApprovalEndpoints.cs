using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using VivaBev.Approvals.Application;
using VivaBev.Approvals.Contracts;
using VivaBev.Approvals.Domain;
using VivaBev.Approvals.Infrastructure;
using VivaBev.Identity.Contracts;
using VivaBev.SharedKernel.Abstractions;

namespace VivaBev.Approvals.Endpoints;

internal static class ApprovalEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/approvals").WithTags("Approvals").RequireAuthorization();

        group.MapGet("/inbox", async (ICurrentUser currentUser, ApprovalsDbContext db, IUserDirectory users,
            CancellationToken ct) =>
        {
            var me = currentUser.UserId;

            var requests = await db.Requests.AsNoTracking()
                .Include(r => r.Steps)
                .Where(r => r.Status == ApprovalStatus.Pending &&
                            r.Steps.Where(s => s.Decision == StepDecision.Pending)
                                .OrderBy(s => s.Sequence)
                                .Select(s => s.ApproverId)
                                .FirstOrDefault() == me)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync(ct);

            var names = await NamesAsync(users, requests.Select(r => r.RequestedBy), ct);

            return requests.Select(r => new ApprovalTaskDto(
                r.Id, r.DocumentType, r.DocumentId, r.DocumentNumber, r.Title, r.Amount,
                names[r.RequestedBy], r.RequestedAt,
                r.CurrentStep!.Name, r.CurrentStep.Sequence, r.Steps.Count, r.OverBudget));
        });

        group.MapGet("/documents/{documentType}/{documentId:guid}", async (string documentType, Guid documentId,
            ICurrentUser currentUser, ApprovalsDbContext db, IUserDirectory users, CancellationToken ct) =>
        {
            var request = await db.Requests.AsNoTracking()
                .Include(r => r.Steps)
                .Where(r => r.DocumentType == documentType && r.DocumentId == documentId)
                .OrderByDescending(r => r.RequestedAt)
                .FirstOrDefaultAsync(ct);

            if (request is null)
                return Results.NotFound();

            var me = currentUser.UserId;
            if (request.RequestedBy != me && request.Steps.All(s => s.ApproverId != me))
                return Results.Forbid();

            var names = await NamesAsync(users, request.Steps.Select(s => s.ApproverId), ct);

            return Results.Ok(new ApprovalStatusDto(
                request.Id, request.DocumentNumber, request.Status.ToString(), request.Amount,
                request.Steps.OrderBy(s => s.Sequence)
                    .Select(s => new ApprovalStepDto(s.Sequence, s.Name, s.ApproverId, names[s.ApproverId],
                        s.Decision.ToString(), s.DecidedAt, s.Comment))
                    .ToList()));
        });

        group.MapPost("/{requestId:guid}/approve", async (Guid requestId, DecisionRequest request,
            ApprovalService service, CancellationToken ct) =>
        {
            await service.ApproveAsync(requestId, request.Comment, ct);
            return Results.NoContent();
        });

        group.MapPost("/{requestId:guid}/reject", async (Guid requestId, DecisionRequest request,
            ApprovalService service, CancellationToken ct) =>
        {
            await service.RejectAsync(requestId, request.Comment, ct);
            return Results.NoContent();
        });
    }

    private static async Task<Dictionary<Guid, string>> NamesAsync(IUserDirectory users, IEnumerable<Guid> ids,
        CancellationToken ct)
    {
        var names = new Dictionary<Guid, string>();
        foreach (var id in ids.Distinct())
            names[id] = (await users.GetUserAsync(id, ct))?.DisplayName ?? "(unknown user)";
        return names;
    }
}
