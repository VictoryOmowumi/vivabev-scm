using VivaBev.SharedKernel.Domain;

namespace VivaBev.Inventory.Domain.Stock;

public enum StockRequestStatus
{
    Open,             // waiting for the warehouse
    PartiallyIssued,  // some issued, the rest still to come
    Issued,           // everything issued
    Closed,           // partly issued, and the warehouse won't issue the rest
    Rejected,         // the warehouse turned it down before issuing anything
    Cancelled,        // the requester withdrew it before anything was issued
}

/// <summary>
/// A request for stock already held at a site, charged to a cost centre. The warehouse issues it, in one go or in
/// several; each issue takes stock out of the ledger. No approval step: the stock was approved when it was bought.
/// </summary>
public sealed class StockRequest : AggregateRoot
{
    private readonly List<StockRequestLine> _lines = [];

    private StockRequest() { }

    public StockRequest(string number, Guid siteId, Guid costCentreId, Guid requestedBy, string purpose,
        DateOnly? neededBy, DateTime now, IReadOnlyCollection<RequestedItem> items)
    {
        var wanted = items.Where(i => i.Quantity != 0).ToList();
        if (wanted.Count == 0)
            throw new DomainException("Ask for at least one item.");
        if (wanted.Any(i => i.Quantity < 0))
            throw new DomainException("Quantities can't be negative.");
        if (wanted.Select(i => i.ItemId).Distinct().Count() != wanted.Count)
            throw new DomainException("An item appears twice. Put the whole quantity on one line.");
        if (neededBy is { } date && date < DateOnly.FromDateTime(now))
            throw new DomainException("The needed-by date is in the past.");

        Number = Guard.Code(number, nameof(number), 20);
        SiteId = siteId;
        CostCentreId = costCentreId;
        RequestedBy = requestedBy;
        RequestedAt = now;
        Purpose = Guard.Required(purpose, "What it's for", 500);
        NeededBy = neededBy;
        Status = StockRequestStatus.Open;

        var lineNo = 1;
        foreach (var item in wanted)
            _lines.Add(new StockRequestLine(Id, lineNo++, item));
    }

    public string Number { get; private set; } = null!;
    public Guid SiteId { get; private set; }
    public Guid CostCentreId { get; private set; }
    public Guid RequestedBy { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public string Purpose { get; private set; } = null!;
    public DateOnly? NeededBy { get; private set; }
    public StockRequestStatus Status { get; private set; }
    public DateTime? LastIssuedAt { get; private set; }
    public Guid? DecidedBy { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? DecisionReason { get; private set; }

    public IReadOnlyCollection<StockRequestLine> Lines => _lines.AsReadOnly();

    public bool IsOutstanding => Status is StockRequestStatus.Open or StockRequestStatus.PartiallyIssued;

    /// <summary>
    /// Issue quantities against lines. Returns what was actually issued (lines with a positive quantity), so the
    /// caller can take it out of stock.
    /// </summary>
    public IReadOnlyList<(StockRequestLine Line, decimal Quantity)> Issue(IReadOnlyDictionary<Guid, decimal> quantities, DateTime now)
    {
        if (!IsOutstanding)
            throw new DomainException($"{Number} is {Status}; there's nothing left to issue.");
        if (quantities.Keys.Any(id => _lines.All(l => l.Id != id)))
            throw new DomainException("A line is not on this request.");

        var issued = new List<(StockRequestLine, decimal)>();
        foreach (var line in _lines)
        {
            if (!quantities.TryGetValue(line.Id, out var quantity) || quantity == 0)
                continue;
            line.RecordIssue(quantity);
            issued.Add((line, quantity));
        }
        if (issued.Count == 0)
            throw new DomainException("Enter a quantity to issue for at least one line.");

        LastIssuedAt = now;
        Status = _lines.All(l => l.Outstanding == 0) ? StockRequestStatus.Issued : StockRequestStatus.PartiallyIssued;
        return issued;
    }

    /// <summary>Turn it down before anything is issued, e.g. the stock is reserved for production.</summary>
    public void Reject(string reason, Guid by, DateTime now)
    {
        if (Status != StockRequestStatus.Open)
            throw new DomainException(Status == StockRequestStatus.PartiallyIssued
                ? $"Part of {Number} has been issued. Close it instead."
                : $"{Number} is {Status} and can't be rejected.");
        Decide(StockRequestStatus.Rejected, reason, by, now);
    }

    /// <summary>Stop issuing a partly issued request: the rest won't come.</summary>
    public void CloseShort(string reason, Guid by, DateTime now)
    {
        if (Status != StockRequestStatus.PartiallyIssued)
            throw new DomainException($"Only a partly issued request can be closed; {Number} is {Status}.");
        Decide(StockRequestStatus.Closed, reason, by, now);
    }

    /// <summary>The requester withdraws it. Only before anything is issued.</summary>
    public void Cancel(Guid by, DateTime now)
    {
        if (by != RequestedBy)
            throw new DomainException("Only the person who asked can cancel a request.");
        if (Status != StockRequestStatus.Open)
            throw new DomainException($"{Number} is {Status} and can't be cancelled.");
        Status = StockRequestStatus.Cancelled;
        DecidedBy = by;
        DecidedAt = now;
    }

    private void Decide(StockRequestStatus status, string reason, Guid by, DateTime now)
    {
        DecisionReason = Guard.Required(reason, "Reason", 500);
        Status = status;
        DecidedBy = by;
        DecidedAt = now;
    }
}

public sealed class StockRequestLine : AuditableEntity
{
    private StockRequestLine() { }

    internal StockRequestLine(Guid stockRequestId, int lineNo, RequestedItem item)
    {
        StockRequestId = stockRequestId;
        LineNo = lineNo;
        ItemId = item.ItemId;
        ItemCode = Guard.Code(item.ItemCode, "itemCode", 30);
        Description = Guard.Required(item.Description, "description", 500);
        UomCode = Guard.Code(item.UomCode, "uomCode", 10);
        QuantityRequested = item.Quantity;
    }

    public Guid StockRequestId { get; private set; }
    public int LineNo { get; private set; }
    public Guid ItemId { get; private set; }
    public string ItemCode { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string UomCode { get; private set; } = null!;
    public decimal QuantityRequested { get; private set; }
    public decimal QuantityIssued { get; private set; }

    public decimal Outstanding => QuantityRequested - QuantityIssued;

    internal void RecordIssue(decimal quantity)
    {
        if (quantity < 0)
            throw new DomainException("Quantities can't be negative.");
        if (quantity > Outstanding)
            throw new DomainException($"Line {LineNo} ({Description}) has only {Outstanding:0.####} {UomCode} left to issue.");
        QuantityIssued += quantity;
    }
}

/// <summary>One item as the requester asks for it, with its catalogue details.</summary>
public sealed record RequestedItem(Guid ItemId, string ItemCode, string Description, string UomCode, decimal Quantity);
