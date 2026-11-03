using Microsoft.EntityFrameworkCore;
using VivaBev.Approvals.Contracts;
using VivaBev.Procurement.Domain.Requisitions;
using VivaBev.Procurement.Infrastructure;
using VivaBev.SharedKernel.Errors;

namespace VivaBev.Procurement.Application;

internal sealed class RequisitionApprovalHandler(ProcurementDbContext db, TimeProvider clock) : IApprovalOutcomeHandler
{
    public string DocumentType => Requisition.ApprovalDocumentType;

    public async Task ApplyApprovedAsync(Guid documentId, CancellationToken ct)
    {
        var requisition = await LoadAsync(documentId, ct);
        requisition.MarkApproved(clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(ct);
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
