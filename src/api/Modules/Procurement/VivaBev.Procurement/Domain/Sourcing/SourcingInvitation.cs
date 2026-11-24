using VivaBev.SharedKernel.Domain;

namespace VivaBev.Procurement.Domain.Sourcing;

public sealed class SourcingInvitation : AuditableEntity
{
    private SourcingInvitation() { }

    internal SourcingInvitation(Guid sourcingEventId, Guid supplierId)
    {
        SourcingEventId = sourcingEventId;
        SupplierId = supplierId;
        Status = InvitationStatus.Invited;
    }

    public Guid SourcingEventId { get; private set; }
    public Guid SupplierId { get; private set; }
    public InvitationStatus Status { get; private set; }
    public DateTime? InvitedAt { get; private set; } // set when the event is published

    internal void MarkSent(DateTime now) => InvitedAt ??= now;
    internal void MarkDeclined() => Status = InvitationStatus.Declined;
    internal void MarkResponded() => Status = InvitationStatus.Responded;
    internal void MarkInvited() => Status = InvitationStatus.Invited;
}
