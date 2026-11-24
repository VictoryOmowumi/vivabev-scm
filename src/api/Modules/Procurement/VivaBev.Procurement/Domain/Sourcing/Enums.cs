namespace VivaBev.Procurement.Domain.Sourcing;

public enum SourcingEventStatus
{
    Draft,           // the buyer is choosing lines and suppliers
    Published,       // suppliers have been invited; quotations are coming in
    ResponsesClosed, // no more quotations; ready for evaluation
    Awarded,         // a supplier was chosen and the award approved (next release step)
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
