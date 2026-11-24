using Microsoft.EntityFrameworkCore;
using VivaBev.Approvals.Contracts;
using VivaBev.Procurement.Domain.Sourcing;
using VivaBev.Procurement.Infrastructure;
using VivaBev.SharedKernel.Errors;

namespace VivaBev.Procurement.Application;

/// <summary>Applies the final decision on an RFQ's award recommendation.</summary>
internal sealed class SourcingAwardApprovalHandler(ProcurementDbContext db, TimeProvider clock) : IApprovalOutcomeHandler
{
    public string DocumentType => SourcingEvent.AwardDocumentType;

    public async Task ApplyApprovedAsync(Guid documentId, CancellationToken ct)
    {
        var sourcingEvent = await LoadAsync(documentId, ct);
        sourcingEvent.ApproveAward(clock.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Back to evaluation, with the reason shown to the buyer.</summary>
    public async Task ApplyRejectedAsync(Guid documentId, string reason, CancellationToken ct)
    {
        var sourcingEvent = await LoadAsync(documentId, ct);
        sourcingEvent.RejectAward(reason);
        await db.SaveChangesAsync(ct);
    }

    private async Task<SourcingEvent> LoadAsync(Guid id, CancellationToken ct) =>
        await db.SourcingEvents.SingleOrDefaultAsync(e => e.Id == id, ct)
        ?? throw new NotFoundException($"RFQ {id} was not found.");
}
