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

    /// <summary>The document type this aggregate's awards go through Approvals as.</summary>
    public const string AwardDocumentType = "SourcingAward";

    /// <summary>Choosing anyone but the top scorer needs at least this much explanation.</summary>
    public const int MinimumJustificationLength = 20;

    private readonly List<SourcingEventLine> _lines = [];
    private readonly List<SourcingInvitation> _invitations = [];
    private readonly List<Quotation> _quotations = [];
    private readonly List<EvaluationCriterion> _criteria = [];
    private readonly List<EvaluationScore> _scores = [];

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

    // The award: the recommended quotation, why, and what happened to it
    public Guid? AwardQuotationId { get; private set; }
    public string? AwardJustification { get; private set; }
    public DateTime? AwardRecommendedAt { get; private set; }
    public DateTime? AwardedAt { get; private set; }
    public string? AwardRejectionReason { get; private set; } // the last rejection, cleared on the next recommendation

    public IReadOnlyCollection<SourcingEventLine> Lines => _lines.AsReadOnly();
    public IReadOnlyCollection<SourcingInvitation> Invitations => _invitations.AsReadOnly();
    public IReadOnlyCollection<Quotation> Quotations => _quotations.AsReadOnly();
    public IReadOnlyCollection<EvaluationCriterion> Criteria => _criteria.AsReadOnly();
    public IReadOnlyCollection<EvaluationScore> Scores => _scores.AsReadOnly();

    public Quotation? AwardQuotation => _quotations.SingleOrDefault(q => q.Id == AwardQuotationId);

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

        if (_criteria.Count == 0)
        {
            AddCriterion("Price", CriterionKind.Price, 60);
            AddCriterion("Delivery time", CriterionKind.Delivery, 20);
            AddCriterion("Payment terms", CriterionKind.PaymentTerms, 10);
            AddCriterion("Quality and service", CriterionKind.Manual, 10);
        }
    }

    // ---------- evaluation ----------

    /// <summary>Replaces the criteria. Weights must add up to 100; manual scores of removed criteria go too.</summary>
    public void SetCriteria(IReadOnlyCollection<CriterionSpec> specs)
    {
        EnsureStatus(SourcingEventStatus.ResponsesClosed, "change the criteria of");
        if (specs.Count is 0 or > 8)
            throw new DomainException("Use between 1 and 8 criteria.");
        if (specs.Sum(s => s.Weight) != 100)
            throw new DomainException($"The weights add up to {specs.Sum(s => s.Weight)}; they must add up to 100.");
        if (specs.Select(s => s.Name.Trim().ToUpperInvariant()).Distinct().Count() != specs.Count)
            throw new DomainException("Two criteria have the same name.");
        if (specs.Where(s => s.Kind != CriterionKind.Manual).GroupBy(s => s.Kind).Any(g => g.Count() > 1))
            throw new DomainException("Price, delivery and payment terms can each be used once.");

        var keep = specs.Where(s => s.Id is not null).Select(s => s.Id!.Value).ToHashSet();
        if (keep.Any(id => _criteria.All(c => c.Id != id)))
            throw new DomainException("A criterion is not part of this RFQ.");

        _scores.RemoveAll(s => !keep.Contains(s.CriterionId));
        _criteria.RemoveAll(c => !keep.Contains(c.Id));

        var order = 1;
        foreach (var spec in specs)
        {
            var existing = spec.Id is null ? null : _criteria.Single(c => c.Id == spec.Id);
            if (existing is null)
            {
                _criteria.Add(new EvaluationCriterion(Id, order, spec.Name, spec.Kind, spec.Weight));
            }
            else
            {
                if (existing.Kind != spec.Kind)
                    throw new DomainException($"'{existing.Name}' can't change how it is scored; remove it and add a new one.");
                existing.Update(spec.Name, spec.Weight);
                existing.MoveTo(order);
            }
            order++;
        }
    }

    /// <summary>Sets (or, with null, clears) the buyer's score for a quotation on a manual criterion.</summary>
    public void Score(Guid criterionId, Guid quotationId, decimal? score)
    {
        EnsureStatus(SourcingEventStatus.ResponsesClosed, "score");
        var criterion = _criteria.SingleOrDefault(c => c.Id == criterionId)
            ?? throw new DomainException("That criterion is not part of this RFQ.");
        if (criterion.Kind != CriterionKind.Manual)
            throw new DomainException($"'{criterion.Name}' is scored automatically.");
        var quotation = _quotations.SingleOrDefault(q => q.Id == quotationId)
            ?? throw new DomainException("That quotation is not part of this RFQ.");
        if (!IsComparable(quotation))
            throw new DomainException("Only received quotations that price every line are scored.");

        var existing = _scores.SingleOrDefault(s => s.CriterionId == criterionId && s.QuotationId == quotationId);
        if (score is null)
        {
            if (existing is not null)
                _scores.Remove(existing);
        }
        else if (existing is null)
            _scores.Add(new EvaluationScore(Id, criterionId, quotationId, score.Value));
        else
            existing.Set(score.Value);
    }

    /// <summary>
    /// Scores every received quotation that prices every line. Price, delivery and payment terms are scored
    /// against the best offer (best = 10); manual criteria use the buyer's scores. Totals are out of 100.
    /// </summary>
    public IReadOnlyList<QuotationEvaluation> Evaluate()
    {
        var comparable = _quotations.Where(IsComparable).ToList();
        if (comparable.Count == 0)
            return [];

        var lowestTotal = comparable.Min(q => q.Total);
        var shortestLead = comparable.Min(q => q.LeadTimeDays);
        var longestTerms = comparable.Max(q => q.PaymentTermsDays);

        decimal? ScoreOf(EvaluationCriterion c, Quotation q) => c.Kind switch
        {
            CriterionKind.Price => q.Total == 0 ? 10m : Round(10m * lowestTotal / q.Total),
            // +1 so a same-day offer doesn't divide by zero
            CriterionKind.Delivery => Round(10m * (shortestLead + 1) / (q.LeadTimeDays + 1)),
            CriterionKind.PaymentTerms => longestTerms == 0 ? 10m : Round(10m * q.PaymentTermsDays / longestTerms),
            _ => _scores.SingleOrDefault(s => s.CriterionId == c.Id && s.QuotationId == q.Id)?.Score,
        };

        var rows = comparable.Select(q =>
        {
            var scores = _criteria.ToDictionary(c => c.Id, c => ScoreOf(c, q));
            var weighted = _criteria.Sum(c => (scores[c.Id] ?? 0m) * c.Weight / 10m);
            return (Quotation: q, Scores: scores, Weighted: Round(weighted), Full: scores.Values.All(v => v is not null));
        }).ToList();

        return rows
            .OrderByDescending(r => r.Weighted).ThenBy(r => r.Quotation.Total)
            .Select(r => new QuotationEvaluation(r.Quotation.Id, r.Quotation.SupplierId, r.Quotation.Total, r.Scores,
                r.Weighted, r.Full, 1 + rows.Count(o => o.Weighted > r.Weighted)))
            .ToList();
    }

    // ---------- award ----------

    /// <summary>
    /// The buyer's recommendation. Every comparable quotation must be fully scored, the chosen one must still be
    /// valid, and choosing anyone but the top scorer needs a written reason.
    /// </summary>
    public void RecommendAward(Guid quotationId, string? justification, DateTime now)
    {
        EnsureStatus(SourcingEventStatus.ResponsesClosed, "recommend an award on");

        var quotation = _quotations.SingleOrDefault(q => q.Id == quotationId)
            ?? throw new DomainException("That quotation is not part of this RFQ.");
        if (!IsComparable(quotation))
            throw new DomainException("Only a received quotation that prices every line can be awarded.");
        if (quotation.ValidUntil < DateOnly.FromDateTime(now))
            throw new DomainException("That quotation has expired. Ask the supplier to confirm or re-quote.");

        var evaluation = Evaluate();
        if (evaluation.Any(e => !e.FullyScored))
            throw new DomainException("Score every quotation on every criterion first.");

        var reason = string.IsNullOrWhiteSpace(justification) ? null : justification.Trim();
        var chosen = evaluation.Single(e => e.QuotationId == quotationId);
        if (chosen.Rank != 1 && (reason?.Length ?? 0) < MinimumJustificationLength)
            throw new DomainException(
                $"This isn't the top-scoring quotation, so explain why in at least {MinimumJustificationLength} characters.");
        if (reason?.Length > 1000)
            throw new DomainException("Keep the justification under 1,000 characters.");

        AwardQuotationId = quotationId;
        AwardJustification = reason;
        AwardRecommendedAt = now;
        AwardRejectionReason = null;
        Status = SourcingEventStatus.AwardPending;
    }

    /// <summary>The buyer takes the recommendation back, to rescore or choose again.</summary>
    public void WithdrawRecommendation()
    {
        EnsureStatus(SourcingEventStatus.AwardPending, "withdraw the recommendation of");
        ClearRecommendation();
    }

    public void ApproveAward(DateTime now)
    {
        EnsureStatus(SourcingEventStatus.AwardPending, "approve the award of");
        Status = SourcingEventStatus.Awarded;
        AwardedAt = now;
    }

    public void RejectAward(string reason)
    {
        EnsureStatus(SourcingEventStatus.AwardPending, "reject the award of");
        ClearRecommendation();
        AwardRejectionReason = Guard.Required(reason, nameof(reason), 500);
    }

    public void Cancel(string reason)
    {
        if (Status is SourcingEventStatus.Cancelled or SourcingEventStatus.Awarded)
            throw new DomainException($"Cannot cancel an RFQ that is {Status}.");
        if (Status == SourcingEventStatus.AwardPending)
            throw new DomainException("The award is waiting for approval. Withdraw the recommendation first.");

        CancellationReason = Guard.Required(reason, nameof(reason), 500);
        Status = SourcingEventStatus.Cancelled;
    }

    private void AddCriterion(string name, CriterionKind kind, int weight) =>
        _criteria.Add(new EvaluationCriterion(Id, _criteria.Count + 1, name, kind, weight));

    private void ClearRecommendation()
    {
        AwardQuotationId = null;
        AwardJustification = null;
        AwardRecommendedAt = null;
        Status = SourcingEventStatus.ResponsesClosed;
    }

    /// <summary>Received and pricing every line: the quotations that can be scored and awarded the whole RFQ.</summary>
    private bool IsComparable(Quotation q) =>
        q.Status == QuotationStatus.Received && _lines.All(l => q.Lines.Any(ql => ql.SourcingEventLineId == l.Id));

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private void EnsureStatus(SourcingEventStatus expected, string action)
    {
        if (Status != expected)
            throw new DomainException($"Cannot {action} an RFQ that is {Status}.");
    }
}

/// <summary>One priced line of a quotation, as entered by the buyer.</summary>
public sealed record QuotedLine(Guid SourcingEventLineId, decimal UnitPrice, decimal Quantity, string? Notes);
