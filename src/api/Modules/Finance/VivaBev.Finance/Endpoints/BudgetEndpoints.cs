using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using VivaBev.Finance.Application;
using VivaBev.Finance.Contracts;
using VivaBev.Finance.Domain;
using VivaBev.Finance.Infrastructure;
using VivaBev.Identity.Contracts;
using VivaBev.Procurement.Contracts;
using VivaBev.SharedKernel.Abstractions;
using VivaBev.SharedKernel.Security;
using VivaBev.SharedKernel.Time;

namespace VivaBev.Finance.Endpoints;

internal static class BudgetEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // Finance and the MD see every budget; department managers see their own department's, read-only.
        var group = app.MapGroup("/api/finance/budgets").WithTags("Finance").RequireAuthorization();

        group.MapGet("/", async (int? fiscalYear, ClaimsPrincipal principal, ICurrentUser currentUser,
            FinanceDbContext db, IOrganisationQueries organisation, IUserDirectory users,
            IRequisitionQueries requisitions, TimeProvider clock, CancellationToken ct) =>
        {
            var visible = await VisibleCostCentresAsync(principal, currentUser, organisation, users, ct);
            if (visible is null)
                return Results.Problem(statusCode: StatusCodes.Status403Forbidden,
                    title: "You don't have access to budgets.");

            var year = fiscalYear ?? FiscalCalendar.FiscalYearOf(clock.GetUtcNow().UtcDateTime);
            var ids = visible.Keys.ToList();

            var budgets = await db.Budgets.AsNoTracking()
                .Include(b => b.Commitments) // Committed and Consumed are sums over these
                .Where(b => b.FiscalYear == year && ids.Contains(b.CostCentreId))
                .ToListAsync(ct);
            var inApproval = await requisitions.GetInApprovalTotalsAsync(year, ct);

            return Results.Ok(budgets
                .Select(b => ToSummary(b, visible, inApproval))
                .OrderBy(b => b.CostCentreCode)
                .ToList());
        });

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal principal, ICurrentUser currentUser,
            FinanceDbContext db, IOrganisationQueries organisation, IUserDirectory users,
            IRequisitionQueries requisitions, CancellationToken ct) =>
        {
            var visible = await VisibleCostCentresAsync(principal, currentUser, organisation, users, ct);
            if (visible is null)
                return Results.Problem(statusCode: StatusCodes.Status403Forbidden,
                    title: "You don't have access to budgets.");

            var budget = await db.Budgets.AsNoTracking()
                .Include(b => b.Commitments)
                .SingleOrDefaultAsync(b => b.Id == id, ct);

            if (budget is null)
                return Results.NotFound();
            if (!visible.ContainsKey(budget.CostCentreId))
                return Results.Problem(statusCode: StatusCodes.Status403Forbidden,
                    title: "This budget belongs to another department.");

            var inApproval = await requisitions.GetInApprovalTotalsAsync(budget.FiscalYear, ct);

            return Results.Ok(new BudgetDetailDto(
                ToSummary(budget, visible, inApproval),
                budget.Commitments.OrderByDescending(c => c.OpenedAt)
                    .Select(c => new BudgetCommitmentDto(c.DocumentType, c.DocumentId, c.DocumentNumber, c.Amount,
                        c.Status.ToString(), c.OpenedAt, c.ClosedAt))
                    .ToList()));
        });

        group.MapPost("/", async (SetBudgetRequest request, BudgetService service, CancellationToken ct) =>
        {
            var id = await service.SetBudgetAsync(request, ct);
            return Results.Ok(new { id });
        }).RequireAuthorization(Permissions.Finance.ManageBudgets);
    }

    /// <summary>The cost centres whose budgets this user may see, keyed by id; null if none.</summary>
    private static async Task<Dictionary<Guid, CostCentreDto>?> VisibleCostCentresAsync(ClaimsPrincipal principal,
        ICurrentUser currentUser, IOrganisationQueries organisation, IUserDirectory users, CancellationToken ct)
    {
        var permissions = principal.GetPermissions();
        var all = await organisation.GetCostCentresAsync(ct);

        if (permissions.Contains(Permissions.Finance.ViewBudgets))
            return all.ToDictionary(c => c.Id);

        if (permissions.Contains(Permissions.Finance.ViewDepartmentBudgets) && currentUser.UserId is { } me)
        {
            var departmentId = (await users.GetUserAsync(me, ct))?.DepartmentId;
            return all.Where(c => c.DepartmentId == departmentId).ToDictionary(c => c.Id);
        }

        return null;
    }

    private static BudgetSummaryDto ToSummary(Budget b, IReadOnlyDictionary<Guid, CostCentreDto> costCentres,
        IReadOnlyDictionary<Guid, decimal> inApproval)
    {
        var cc = costCentres.GetValueOrDefault(b.CostCentreId);
        return new BudgetSummaryDto(b.Id, b.CostCentreId, cc?.Code ?? "?", cc?.Name ?? "(unknown)", b.FiscalYear,
            b.CurrencyCode, b.Amount, b.Committed, b.Consumed, b.Available,
            inApproval.GetValueOrDefault(b.CostCentreId), cc?.DepartmentCode ?? "?");
    }
}
