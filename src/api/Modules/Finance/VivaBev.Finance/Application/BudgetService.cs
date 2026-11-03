using Microsoft.EntityFrameworkCore;
using VivaBev.Finance.Contracts;
using VivaBev.Finance.Domain;
using VivaBev.Finance.Infrastructure;
using VivaBev.Identity.Contracts;
using VivaBev.SharedKernel.Errors;
using VivaBev.SharedKernel.Time;

namespace VivaBev.Finance.Application;

internal sealed class BudgetService(FinanceDbContext db, IOrganisationQueries organisation, TimeProvider clock) : IBudgets
{
    public async Task<BudgetCheckDto> CheckAsync(Guid costCentreId, DateTime onDate, decimal amount,
        CancellationToken ct = default)
    {
        var fiscalYear = FiscalCalendar.FiscalYearOf(onDate);
        var budget = await FindAsync(costCentreId, fiscalYear, tracked: false, ct);

        if (budget is null)
            return new BudgetCheckDto(costCentreId, fiscalYear, HasBudget: false, 0m, 0m, 0m, 0m, amount, IsOverBudget: true);

        return new BudgetCheckDto(costCentreId, fiscalYear, HasBudget: true, budget.Amount, budget.Committed,
            budget.Consumed, budget.Available, amount, IsOverBudget: amount > budget.Available);
    }

    public async Task CommitAsync(BudgetCommitmentRequest request, CancellationToken ct = default)
    {
        var budget = await FindAsync(request.CostCentreId, request.FiscalYear, tracked: true, ct);

        if (budget is null)
        {
            // Unbudgeted spend is still tracked: a zero budget makes it show up as overspent.
            budget = new Budget(request.CostCentreId, request.FiscalYear, 0m);
            db.Budgets.Add(budget);
        }

        budget.Commit(request.DocumentType, request.DocumentId, request.DocumentNumber, request.Amount, Now);
        await db.SaveChangesAsync(ct);
    }

    public async Task ReleaseAsync(string documentType, Guid documentId, CancellationToken ct = default)
    {
        var budget = await db.Budgets
            .Include(b => b.Commitments)
            .SingleOrDefaultAsync(b => b.Commitments.Any(c =>
                c.DocumentType == documentType && c.DocumentId == documentId && c.Status == CommitmentStatus.Open), ct);

        if (budget is null)
            return;

        budget.Release(documentType, documentId, Now);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> SetBudgetAsync(SetBudgetRequest request, CancellationToken ct)
    {
        _ = await organisation.GetCostCentreAsync(request.CostCentreId, ct)
            ?? throw new NotFoundException($"Cost centre {request.CostCentreId} was not found.");

        var budget = await FindAsync(request.CostCentreId, request.FiscalYear, tracked: true, ct);
        if (budget is null)
        {
            budget = new Budget(request.CostCentreId, request.FiscalYear, request.Amount);
            db.Budgets.Add(budget);
        }
        else
        {
            budget.SetAmount(request.Amount);
        }

        await db.SaveChangesAsync(ct);
        return budget.Id;
    }

    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    private Task<Budget?> FindAsync(Guid costCentreId, int fiscalYear, bool tracked, CancellationToken ct)
    {
        var query = db.Budgets.Include(b => b.Commitments).AsQueryable();
        if (!tracked)
            query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(b => b.CostCentreId == costCentreId && b.FiscalYear == fiscalYear, ct);
    }
}
