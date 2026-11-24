using VivaBev.SharedKernel.Domain;

namespace VivaBev.Inventory.Domain.Stock;

public enum StockCountStatus
{
    Open,      // counting in progress
    Posted,    // differences written to the ledger
    Cancelled,
}

/// <summary>
/// A physical count of stock at a site. It lists what the ledger says is there when the count starts; the counter
/// enters what they find. Posting compares the counts with the ledger at that moment and writes the differences
/// as adjustments, so stock that moved during the count isn't double-counted.
/// </summary>
public sealed class StockCount : AggregateRoot
{
    private readonly List<StockCountLine> _lines = [];

    private StockCount() { }

    public StockCount(string number, Guid siteId, Guid startedBy, DateTime now, IReadOnlyCollection<ExpectedStock> expected)
    {
        if (expected.Count == 0)
            throw new DomainException("There's no stock at this site to count.");

        Number = Guard.Code(number, nameof(number), 20);
        SiteId = siteId;
        StartedBy = startedBy;
        StartedAt = now;
        Status = StockCountStatus.Open;

        var lineNo = 1;
        foreach (var e in expected.OrderBy(e => e.ItemCode))
            _lines.Add(new StockCountLine(Id, lineNo++, e));
    }

    public string Number { get; private set; } = null!;
    public Guid SiteId { get; private set; }
    public Guid StartedBy { get; private set; }
    public DateTime StartedAt { get; private set; }
    public StockCountStatus Status { get; private set; }
    public Guid? PostedBy { get; private set; }
    public DateTime? PostedAt { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<StockCountLine> Lines => _lines.AsReadOnly();

    /// <summary>Save counts as they come in. A null count clears it.</summary>
    public void RecordCounts(IReadOnlyDictionary<Guid, decimal?> counts)
    {
        EnsureOpen();
        foreach (var (lineId, counted) in counts)
        {
            var line = _lines.SingleOrDefault(l => l.Id == lineId)
                ?? throw new DomainException("A line is not on this count.");
            line.SetCounted(counted);
        }
    }

    /// <summary>
    /// Compare every count with the ledger now and fix the ledger's figure on each line. Returns the lines that
    /// differ; the caller writes one adjustment for each. A difference needs a note saying why.
    /// </summary>
    public IReadOnlyList<StockCountLine> Post(IReadOnlyDictionary<Guid, decimal> onHandNow, string? notes, Guid by, DateTime now)
    {
        EnsureOpen();
        var missing = _lines.Count(l => l.Counted is null);
        if (missing > 0)
            throw new DomainException($"{missing} line{(missing == 1 ? " has" : "s have")} no count yet.");

        foreach (var line in _lines)
            line.SetExpected(onHandNow.GetValueOrDefault(line.ItemId));

        var differences = _lines.Where(l => l.Variance != 0).ToList();
        if (differences.Count > 0 && string.IsNullOrWhiteSpace(notes))
            throw new DomainException("Some counts differ from the system. Add a note saying why before posting.");
        if (notes?.Trim().Length > 1000)
            throw new DomainException("Keep the notes under 1,000 characters.");

        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Status = StockCountStatus.Posted;
        PostedBy = by;
        PostedAt = now;
        return differences;
    }

    public void Cancel()
    {
        EnsureOpen();
        Status = StockCountStatus.Cancelled;
    }

    private void EnsureOpen()
    {
        if (Status != StockCountStatus.Open)
            throw new DomainException($"{Number} is {Status}.");
    }
}

public sealed class StockCountLine : AuditableEntity
{
    private StockCountLine() { }

    internal StockCountLine(Guid stockCountId, int lineNo, ExpectedStock e)
    {
        StockCountId = stockCountId;
        LineNo = lineNo;
        ItemId = e.ItemId;
        ItemCode = Guard.Code(e.ItemCode, "itemCode", 30);
        Description = Guard.Required(e.Description, "description", 500);
        UomCode = Guard.Code(e.UomCode, "uomCode", 10);
        Expected = e.OnHand;
        UnitCost = e.UnitCost;
    }

    public Guid StockCountId { get; private set; }
    public int LineNo { get; private set; }
    public Guid ItemId { get; private set; }
    public string ItemCode { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string UomCode { get; private set; } = null!;
    public decimal Expected { get; private set; }  // the ledger's figure: at start, then at posting
    public decimal? Counted { get; private set; }
    public decimal UnitCost { get; private set; }  // average cost, to value the difference

    public decimal Variance => (Counted ?? Expected) - Expected;
    public decimal VarianceValue => decimal.Round(Variance * UnitCost, 2);

    internal void SetCounted(decimal? counted)
    {
        if (counted < 0)
            throw new DomainException("A count can't be negative.");
        Counted = counted;
    }

    internal void SetExpected(decimal onHand) => Expected = onHand;
}

/// <summary>What the ledger says is at the site for one item.</summary>
public sealed record ExpectedStock(Guid ItemId, string ItemCode, string Description, string UomCode, decimal OnHand, decimal UnitCost);
