using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using VivaBev.Approvals.Contracts;
using VivaBev.Finance.Contracts;
using VivaBev.Identity.Contracts;
using VivaBev.Procurement.Application;
using VivaBev.Procurement.Contracts;
using VivaBev.Procurement.Domain.Requisitions;
using VivaBev.Procurement.Infrastructure;
using VivaBev.SharedKernel.Abstractions;
using VivaBev.SharedKernel.Security;

namespace VivaBev.Procurement.Endpoints;

internal static class RequisitionEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // Any signed-in user may call these; what they see is filtered below.
        var group = app.MapGroup("/api/requisitions").WithTags("Requisitions").RequireAuthorization();

        // ---------- queries ----------

        // ?scope=mine|department|all. Left out, you get the widest list you're allowed to see.
        group.MapGet("/", async (string? scope, ClaimsPrincipal principal, ICurrentUser currentUser,
            ProcurementDbContext db, IUserDirectory users, IOrganisationQueries organisation, CancellationToken ct) =>
        {
            var permissions = principal.GetPermissions();
            var requested = RequisitionVisibility.Widest(permissions);

            if (scope is not null &&
                (!Enum.TryParse(scope, ignoreCase: true, out requested) || !Enum.IsDefined(requested)))
                return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                    title: $"Unknown scope '{scope}'. Use mine, department or all.");

            if (!RequisitionVisibility.Allows(permissions, requested))
                return Results.Problem(statusCode: StatusCodes.Status403Forbidden,
                    title: $"You can't list {scope} requisitions.");

            var me = currentUser.UserId!.Value; // the group requires a signed-in user
            var query = db.Requisitions.AsNoTracking();

            if (requested == RequisitionScope.Mine)
            {
                query = query.Where(r => r.RequesterId == me);
            }
            else if (requested == RequisitionScope.Department)
            {
                var myDepartment = await DepartmentOfAsync(users, me, ct);
                query = query.Where(r => r.DepartmentId == myDepartment);
            }

            var rows = await query.OrderByDescending(r => r.Number).Take(200).ToListAsync(ct);

            // Two lookups for the whole page, not two per row
            var requesters = await users.GetUsersAsync(rows.Select(r => r.RequesterId).ToList(), ct);
            var costCentres = (await organisation.GetCostCentresAsync(ct)).ToDictionary(c => c.Id, c => c.Code);

            return Results.Ok(rows.Select(r => new RequisitionSummaryDto(
                    r.Id, r.Number, r.Title, r.Status.ToString(),
                    r.Status == RequisitionStatus.Draft && r.RejectionReason is not null,
                    r.RequesterId, requesters.GetValueOrDefault(r.RequesterId)?.DisplayName ?? "(unknown user)",
                    costCentres.GetValueOrDefault(r.CostCentreId) ?? "?", r.BudgetStatus.ToString(),
                    r.RequiredBy, r.EstimatedTotal, r.CurrencyCode, r.SubmittedAt))
                .ToList());
        });

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal principal, ICurrentUser currentUser,
            ProcurementDbContext db, IOrganisationQueries organisation, IUserDirectory users, IApprovals approvals,
            CancellationToken ct) =>
        {
            var r = await db.Requisitions.AsNoTracking()
                .Include(x => x.Lines)
                .SingleOrDefaultAsync(x => x.Id == id, ct);

            if (r is null)
                return Results.NotFound();
            if (!await CanViewAsync(r, principal, currentUser, users, approvals, ct))
                return Results.Forbid();

            var requester = await users.GetUserAsync(r.RequesterId, ct);
            var costCentre = await organisation.GetCostCentreAsync(r.CostCentreId, ct);
            var site = await organisation.GetSiteAsync(r.DeliverToSiteId, ct);

            return Results.Ok(new RequisitionDetailDto(
                r.Id, r.Number, r.Title, r.Justification, r.Status.ToString(),
                r.RequesterId, requester?.DisplayName ?? "(unknown user)",
                costCentre?.Code ?? "?", site?.Code ?? "?",
                r.RequiredBy, r.CurrencyCode, r.EstimatedTotal, r.SubmittedAt, r.CancellationReason,
                r.RejectionReason, r.ApprovedAt,
                r.BudgetStatus.ToString(), r.BudgetAvailableAtSubmit,
                r.Lines.OrderBy(l => l.LineNo)
                    .Select(l => new RequisitionLineDto(
                        l.LineNo, l.LineType.ToString(), l.ItemId, l.ItemCode, l.Description, l.CategoryId,
                        l.Quantity, l.UomCode, l.EstimatedUnitPrice, l.LineTotal))
                    .ToList(),
                r.CostCentreId, r.DeliverToSiteId));
        });

        // The approval chain, for anyone who may see the requisition (204 if it was never submitted)
        group.MapGet("/{id:guid}/approval", async (Guid id, ClaimsPrincipal principal, ICurrentUser currentUser,
            ProcurementDbContext db, IUserDirectory users, IApprovals approvals, CancellationToken ct) =>
        {
            var r = await db.Requisitions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);

            if (r is null)
                return Results.NotFound();
            if (!await CanViewAsync(r, principal, currentUser, users, approvals, ct))
                return Results.Forbid();

            var approval = await approvals.GetLatestAsync(Requisition.ApprovalDocumentType, r.Id, ct);
            return approval is null ? Results.NoContent() : Results.Ok(approval);
        });

        // What the requisition form needs: your department's cost centres, each with its budget position now
        group.MapGet("/form-options", async (ICurrentUser currentUser, IUserDirectory users,
            IOrganisationQueries organisation, IBudgets budgets, ProcurementDbContext db, TimeProvider clock,
            CancellationToken ct) =>
        {
            var me = await users.GetUserAsync(currentUser.UserId!.Value, ct);
            if (me is null)
                return Results.NotFound();

            var costCentres = (await organisation.GetCostCentresAsync(ct))
                .Where(c => c.IsActive && c.DepartmentId == me.DepartmentId)
                .ToList();
            var ids = costCentres.Select(c => c.Id).ToList();

            // Submitted but not yet approved: checked against the budget, not yet committed to it
            var pending = await db.Requisitions.AsNoTracking()
                .Where(r => r.Status == RequisitionStatus.PendingApproval && ids.Contains(r.CostCentreId))
                .Select(r => new { r.CostCentreId, r.BudgetFiscalYear, r.EstimatedTotal })
                .ToListAsync(ct);

            var now = clock.GetUtcNow().UtcDateTime;
            var options = new List<CostCentreOptionDto>();
            foreach (var c in costCentres)
            {
                var budget = await budgets.CheckAsync(c.Id, now, 0m, ct);
                var inApproval = pending
                    .Where(p => p.CostCentreId == c.Id && p.BudgetFiscalYear == budget.FiscalYear)
                    .Sum(p => p.EstimatedTotal);

                options.Add(new CostCentreOptionDto(c.Id, c.Code, c.Name, budget.FiscalYear, budget.HasBudget,
                    budget.Budget, budget.Available, inApproval));
            }

            return Results.Ok(options);
        }).RequireAuthorization(Permissions.Requisitions.Create);

        // ---------- commands ----------

        group.MapPost("/", async (CreateRequisitionRequest request, RequisitionService service, CancellationToken ct) =>
        {
            var (id, number) = await service.CreateAsync(request, ct);
            return Results.Created($"/api/requisitions/{id}", new { id, number });
        }).RequireAuthorization(Permissions.Requisitions.Create);

        group.MapPut("/{id:guid}", async (Guid id, UpdateRequisitionRequest request, RequisitionService service,
            CancellationToken ct) =>
        {
            await service.UpdateAsync(id, request, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Requisitions.Create);

        group.MapPost("/{id:guid}/lines", async (Guid id, AddRequisitionLineRequest request,
            RequisitionService service, CancellationToken ct) =>
        {
            var lineNo = await service.AddLineAsync(id, request, ct);
            return Results.Ok(new { lineNo });
        }).RequireAuthorization(Permissions.Requisitions.Create);

        group.MapDelete("/{id:guid}/lines/{lineNo:int}", async (Guid id, int lineNo,
            RequisitionService service, CancellationToken ct) =>
        {
            await service.RemoveLineAsync(id, lineNo, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Requisitions.Create);

        group.MapPost("/{id:guid}/submit", async (Guid id, RequisitionService service, CancellationToken ct) =>
        {
            await service.SubmitAsync(id, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Requisitions.Create);

        group.MapPost("/{id:guid}/cancel", async (Guid id, CancelRequisitionRequest request,
            RequisitionService service, CancellationToken ct) =>
        {
            await service.CancelAsync(id, request.Reason, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Requisitions.Create);
    }

    // ---------- helpers ----------

    /// <summary>Requester, department managers (own department), view-all roles, and anyone on the approval chain.</summary>
    private static async Task<bool> CanViewAsync(Requisition r, ClaimsPrincipal principal, ICurrentUser currentUser,
        IUserDirectory users, IApprovals approvals, CancellationToken ct)
    {
        if (currentUser.UserId is not { } me)
            return false;
        if (r.RequesterId == me)
            return true;

        var widest = RequisitionVisibility.Widest(principal.GetPermissions());
        if (widest == RequisitionScope.All)
            return true;
        if (widest == RequisitionScope.Department && r.DepartmentId == await DepartmentOfAsync(users, me, ct))
            return true;

        // Approvers see what they're asked to approve, whatever their department
        return await approvals.IsApproverAsync(Requisition.ApprovalDocumentType, r.Id, me, ct);
    }

    private static async Task<Guid?> DepartmentOfAsync(IUserDirectory users, Guid userId, CancellationToken ct) =>
        (await users.GetUserAsync(userId, ct))?.DepartmentId;
}
