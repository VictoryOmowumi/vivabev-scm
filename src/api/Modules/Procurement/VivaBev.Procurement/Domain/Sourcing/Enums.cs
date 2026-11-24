namespace VivaBev.Procurement.Domain.Sourcing;

public enum SourcingEventStatus
{
    Draft,           // the buyer is choosing lines and suppliers
    Published,       // suppliers have been invited; quotations are coming in
    ResponsesClosed, // no more quotations; the buyer scores them
    AwardPending,    // the buyer recommended a supplier; the award is going through approvals
    Awarded,         // the award was approved
    Cancelled
}

public enum InvitationStatus
{
    Invited,
    Declined,
    Responded
}

public enum QuotationStatus
{
    Received,
    Withdrawn
}
