using Microsoft.EntityFrameworkCore;
using VivaBev.Finance.Domain;
using VivaBev.Identity.Contracts;

namespace VivaBev.Finance.Infrastructure;

internal static class BudgetSeeder
{
    private const int CurrentFiscalYear = 2027;
    private const decimal DefaultBudget = 10_000_000m;

    private static readonly Dictionary<string, decimal> Amounts = new()
    {
        ["CC-IT-OPS"] = 12_000_000m,   // deliberately tight, to show the soft check
        ["CC-IT-INF"] = 60_000_000m,
        ["CC-MFG-PRD"] = 450_000_000m,
        ["CC-MFG-MNT"] = 80_000_000m,
        ["CC-QA-LAB"] = 30_000_000m,
        ["CC-SCM-PRC"] = 25_000_000m,
        ["CC-SCM-WHS"] = 40_000_000m,
        ["CC-SCM-LOG"] = 120_000_000m,
        ["CC-SAL-SW"] = 35_000_000m,
        ["CC-SAL-NTH"] = 35_000_000m,
        ["CC-MKT-BRD"] = 150_000_000m,
        ["CC-MKT-TRD"] = 90_000_000m,
        ["CC-HR-ADM"] = 15_000_000m,
        ["CC-HR-LND"] = 20_000_000m,
        ["CC-FIN-ACC"] = 18_000_000m,
        ["CC-FIN-TRY"] = 10_000_000m,
    };

    public static async Task SeedAsync(FinanceDbContext db, IOrganisationQueries organisation, CancellationToken ct = default)
    {
        var existing = await db.Budgets
            .Where(b => b.FiscalYear == CurrentFiscalYear)
            .Select(b => b.CostCentreId)
            .ToListAsync(ct);

        foreach (var costCentre in await organisation.GetCostCentresAsync(ct))
        {
            if (existing.Contains(costCentre.Id))
                continue;

            var amount = Amounts.GetValueOrDefault(costCentre.Code, DefaultBudget);
            db.Budgets.Add(new Budget(costCentre.Id, CurrentFiscalYear, amount));
        }

        await db.SaveChangesAsync(ct);
    }
}
