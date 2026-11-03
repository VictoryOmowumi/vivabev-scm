using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using VivaBev.Finance.Application;
using VivaBev.Finance.Contracts;
using VivaBev.Finance.Domain;
using VivaBev.Finance.Infrastructure;
using VivaBev.Identity.Contracts;
using VivaBev.SharedKernel.Security;

namespace VivaBev.Finance.Endpoints;

internal static class BudgetEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/finance/budgets").WithTags("Finance")
            .RequireAuthorization(Permissions.Finance.ViewBudgets);

        group.MapGet("/", async (int? fiscalYear, FinanceDbContext db, IOrganisationQueries organisation,
            CancellationToken ct) =>
        {
            var year = fiscalYear ?? 2027;
            var costCentres = (await organisation.GetCostCentresAsync(ct)).ToDictionary(c => c.Id);

            var budgets = await db.Budgets.AsNoTracking()
                .Include(b => b.Commitments)
                .Where(b => b.FiscalYear == year)
                .ToListAsync(ct);

            return budgets
                .Select(b => ToSummary(b, costCentres))
                .OrderBy(b => b.CostCentreCode);
        });

        group.MapGet("/{id:guid}", async (Guid id, FinanceDbContext db, IOrganisationQueries organisation,
            CancellationToken ct) =>
        {
            var budget = await db.Budgets.AsNoTracking()
                .Include(b => b.Commitments)
                .SingleOrDefaultAsync(b => b.Id == id, ct);

            if (budget is null)
                return Results.NotFound();

            var costCentres = (await organisation.GetCostCentresAsync(ct)).ToDictionary(c => c.Id);

            return Results.Ok(new BudgetDetailDto(
                ToSummary(budget, costCentres),
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

    private static BudgetSummaryDto ToSummary(Budget b, IReadOnlyDictionary<Guid, CostCentreDto> costCentres)
    {
        var cc = costCentres.GetValueOrDefault(b.CostCentreId);
        return new BudgetSummaryDto(b.Id, b.CostCentreId, cc?.Code ?? "?", cc?.Name ?? "(unknown)", b.FiscalYear,
            b.CurrencyCode, b.Amount, b.Committed, b.Consumed, b.Available);
    }
}
