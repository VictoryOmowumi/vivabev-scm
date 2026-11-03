using Microsoft.EntityFrameworkCore;
using VivaBev.Approvals.Contracts;
using VivaBev.Finance.Contracts;
using VivaBev.Procurement.Domain.Requisitions;
using VivaBev.Procurement.Infrastructure;
using VivaBev.SharedKernel.Errors;
using VivaBev.SharedKernel.Time;

namespace VivaBev.Procurement.Application;

internal sealed class RequisitionApprovalHandler(ProcurementDbContext db, IBudgets budgets, TimeProvider clock)
    : IApprovalOutcomeHandler
{
    public string DocumentType => Requisition.ApprovalDocumentType;

    public async Task ApplyApprovedAsync(Guid documentId, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var requisition = await LoadAsync(documentId, ct);
        requisition.MarkApproved(now);
        await db.SaveChangesAsync(ct);

        await budgets.CommitAsync(new BudgetCommitmentRequest(
            requisition.CostCentreId,
            requisition.BudgetFiscalYear ?? FiscalCalendar.FiscalYearOf(now),
            Requisition.ApprovalDocumentType,
            requisition.Id,
            requisition.Number,
            requisition.EstimatedTotal), ct);
    }

    public async Task ApplyRejectedAsync(Guid documentId, string reason, CancellationToken ct)
    {
        var requisition = await LoadAsync(documentId, ct);
        requisition.MarkRejected(reason);
        await db.SaveChangesAsync(ct);
    }

    private async Task<Requisition> LoadAsync(Guid id, CancellationToken ct) =>
        await db.Requisitions.SingleOrDefaultAsync(r => r.Id == id, ct)
        ?? throw new NotFoundException($"Requisition {id} was not found.");
}
