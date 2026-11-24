using VivaBev.SharedKernel.Domain;

namespace VivaBev.Procurement.Domain.Sourcing;

/// <summary>
/// A request for quotation (RFQ): lines taken from approved requisitions, sent to approved suppliers,
/// whose quotations the buyer records and compares.
/// </summary>
public sealed class SourcingEvent : AggregateRoot
{
    /// <summary>At or above this estimated value, an RFQ must invite at least <see cref="MinimumSuppliers"/>.</summary>
    public const decimal CompetitiveThreshold = 2_000_000m;
    public const int MinimumSuppliers = 3;

    private readonly List<SourcingEventLine> _lines = [];
    private readonly List<SourcingInvitation> _invitations = [];
    private readonly List<Quotation> _quotations = [];

    private SourcingEvent() { }

    public SourcingEvent(string number, string title, Guid buyerId, DateOnly responseDeadline, DateOnly today)
    {
        if (responseDeadline <= today)
            throw new DomainException("The response deadline must be after today.");

        Number = Guard.Code(number, nameof(number), 20);
        Title = Guard.Required(title, nameof(title), 200);
        BuyerId = buyerId;
        ResponseDeadline = responseDeadline;
        CurrencyCode = "NGN";
        Status = SourcingEventStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public Guid BuyerId { get; private set; }
    public SourcingEventStatus Status { get; private set; }
    public DateOnly ResponseDeadline { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public DateTime? PublishedAt { get; private set; }
    public DateTime? ResponsesClosedAt { get; private set; }
    public string? CancellationReason { get; private set; }

    public IReadOnlyCollection<SourcingEventLine> Lines => _lines.AsReadOnly();
    public IReadOnlyCollection<SourcingInvitation> Invitations => _invitations.AsReadOnly();
    public IReadOnlyCollection<Quotation> Quotations => _quotations.AsReadOnly();

    public decimal EstimatedTotal => _lines.Sum(l => l.EstimatedTotal);
    public IEnumerable<Guid> RequisitionIds => _lines.Select(l => l.RequisitionId).Distinct();
    public bool NeedsCompetition => EstimatedTotal >= CompetitiveThreshold;

    // ---------- while drafting ----------

    public void AddLines(IReadOnlyCollection<SourcedLine> lines)
    {
        EnsureStatus(SourcingEventStatus.Draft, "add lines to");
        if (lines.Count == 0)
            throw new DomainException("There are no lines to add.");

        foreach (var line in lines)
        {
            if (_lines.Any(l => l.RequisitionLineId == line.RequisitionLineId))
                throw new DomainException($"A line from {line.RequisitionNumber} is already on this RFQ.");

            var lineNo = _lines.Count == 0 ? 1 : _lines.Max(l => l.LineNo) + 1;
            _lines.Add(new SourcingEventLine(Id, lineNo, line));
        }
    }

    /// <summary>Takes every line of one requisition off the RFQ.</summary>
    public void RemoveRequisition(Guid requisitionId)
    {
        EnsureStatus(SourcingEventStatus.Draft, "remove lines from");
        if (_lines.RemoveAll(l => l.RequisitionId == requisitionId) == 0)
            throw new DomainException("That requisition is not on this RFQ.");
    }

    public void Invite(Guid supplierId, DateTime now)
    {
        if (Status is not (SourcingEventStatus.Draft or SourcingEventStatus.Published))
            throw new DomainException($"Cannot invite suppliers to an RFQ that is {Status}.");
        if (_invitations.Any(i => i.SupplierId == supplierId))
            throw new DomainException("That supplier is already invited.");

        var invitation = new SourcingInvitation(Id, supplierId);
        if (Status == SourcingEventStatus.Published)
            invitation.MarkSent(now); // a late invitation goes out straight away
        _invitations.Add(invitation);
    }

    public void Uninvite(Guid supplierId)
    {
        EnsureStatus(SourcingEventStatus.Draft, "remove suppliers from");
        var invitation = _invitations.SingleOrDefault(i => i.SupplierId == supplierId)
            ?? throw new DomainException("That supplier is not invited.");
        _invitations.Remove(invitation);
    }

    public void Publish(DateTime now)
    {
        EnsureStatus(SourcingEventStatus.Draft, "publish");
        if (_lines.Count == 0)
            throw new DomainException("Add at least one requisition before publishing.");
        if (_invitations.Count == 0)
            throw new DomainException("Invite at least one supplier before publishing.");
        if (NeedsCompetition && _invitations.Count < MinimumSuppliers)
            throw new DomainException(
                $"RFQs worth ₦{CompetitiveThreshold:N0} or more must invite at least {MinimumSuppliers} suppliers.");
        if (ResponseDeadline <= DateOnly.FromDateTime(now))
            throw new DomainException("The response deadline has passed. Change it before publishing.");

        foreach (var invitation in _invitations)
            invitation.MarkSent(now);

        Status = SourcingEventStatus.Published;
        PublishedAt = now;
    }

    public void ChangeDeadline(DateOnly responseDeadline, DateOnly today)
    {
        if (Status is not (SourcingEventStatus.Draft or SourcingEventStatus.Published))
            throw new DomainException($"Cannot change the deadline of an RFQ that is {Status}.");
        if (responseDeadline <= today)
            throw new DomainException("The response deadline must be after today.");
        ResponseDeadline = responseDeadline;
    }

    // ---------- while open ----------

    public Quotation RecordQuotation(Guid supplierId, string? supplierReference, DateOnly validUntil, int leadTimeDays,
        int paymentTermsDays, IReadOnlyCollection<QuotedLine> lines, DateTime now)
    {
        EnsureStatus(SourcingEventStatus.Published, "record quotations on");

        var invitation = _invitations.SingleOrDefault(i => i.SupplierId == supplierId)
            ?? throw new DomainException("Only invited suppliers can quote. Invite them first.");
        if (_quotations.Any(q => q.SupplierId == supplierId && q.Status == QuotationStatus.Received))
            throw new DomainException("This supplier already has a quotation. Withdraw it to record a new one.");
        if (validUntil < DateOnly.FromDateTime(now))
            throw new DomainException("This quotation has already expired.");
        if (lines.Count == 0)
            throw new DomainException("Price at least one line.");

        var quotation = new Quotation(Id, supplierId, supplierReference, validUntil, leadTimeDays, paymentTermsDays, now);
        foreach (var line in lines)
        {
            if (_lines.All(l => l.Id != line.SourcingEventLineId))
                throw new DomainException("A priced line is not part of this RFQ.");
            quotation.AddLine(line.SourcingEventLineId, line.UnitPrice, line.Quantity, line.Notes);
        }

        _quotations.Add(quotation);
        invitation.MarkResponded();
        return quotation;
    }

    public void WithdrawQuotation(Guid quotationId, string reason)
    {
        if (Status is not (SourcingEventStatus.Published or SourcingEventStatus.ResponsesClosed))
            throw new DomainException($"Cannot withdraw quotations on an RFQ that is {Status}.");

        var quotation = _quotations.SingleOrDefault(q => q.Id == quotationId)
            ?? throw new DomainException("That quotation is not on this RFQ.");
        quotation.Withdraw(reason);

        // Back to "Invited", so a corrected quotation can be recorded
        _invitations.Single(i => i.SupplierId == quotation.SupplierId).MarkInvited();
    }

    public void RecordDecline(Guid supplierId)
    {
        EnsureStatus(SourcingEventStatus.Published, "record a decline on");
        var invitation = _invitations.SingleOrDefault(i => i.SupplierId == supplierId)
            ?? throw new DomainException("That supplier is not invited.");
        if (invitation.Status == InvitationStatus.Responded)
            throw new DomainException("That supplier has already quoted. Withdraw the quotation first.");
        invitation.MarkDeclined();
    }

    public void CloseResponses(DateTime now)
    {
        EnsureStatus(SourcingEventStatus.Published, "close");
        if (!_quotations.Any(q => q.Status == QuotationStatus.Received))
            throw new DomainException("No quotations have been recorded yet.");

        Status = SourcingEventStatus.ResponsesClosed;
        ResponsesClosedAt = now;
    }

    public void Cancel(string reason)
    {
        if (Status is SourcingEventStatus.Cancelled or SourcingEventStatus.Awarded)
            throw new DomainException($"Cannot cancel an RFQ that is {Status}.");

        CancellationReason = Guard.Required(reason, nameof(reason), 500);
        Status = SourcingEventStatus.Cancelled;
    }

    private void EnsureStatus(SourcingEventStatus expected, string action)
    {
        if (Status != expected)
            throw new DomainException($"Cannot {action} an RFQ that is {Status}.");
    }
}

/// <summary>One priced line of a quotation, as entered by the buyer.</summary>
public sealed record QuotedLine(Guid SourcingEventLineId, decimal UnitPrice, decimal Quantity, string? Notes);
