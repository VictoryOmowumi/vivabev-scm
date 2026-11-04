using Microsoft.EntityFrameworkCore;
using VivaBev.Procurement.Contracts;
using VivaBev.Procurement.Domain.Requisitions;
using VivaBev.Procurement.Infrastructure;

namespace VivaBev.Procurement.Application;

internal sealed class RequisitionQueries(ProcurementDbContext db) : IRequisitionQueries
{
    public async Task<IReadOnlyDictionary<Guid, decimal>> GetInApprovalTotalsAsync(int fiscalYear,
        CancellationToken ct = default) =>
        await db.Requisitions.AsNoTracking()
            .Where(r => r.Status == RequisitionStatus.PendingApproval && r.BudgetFiscalYear == fiscalYear)
            .GroupBy(r => r.CostCentreId)
            .Select(g => new { CostCentreId = g.Key, Total = g.Sum(r => r.EstimatedTotal) })
            .ToDictionaryAsync(x => x.CostCentreId, x => x.Total, ct);
}
