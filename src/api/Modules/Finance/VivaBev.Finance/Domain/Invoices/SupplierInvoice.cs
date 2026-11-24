using VivaBev.SharedKernel.Domain;

namespace VivaBev.Finance.Domain.Invoices;

public enum InvoiceStatus
{
    OnHold,   // didn't match the order and what was received; Finance decides
    Matched,  // matched within tolerance: approved for payment automatically
    Approved, // held, then approved anyway by Finance with a reason
    Rejected, // sent back to the supplier
    Paid
}

/// <summary>
/// A supplier's invoice against one purchase order. On entry it is three-way matched, line by line, against the
/// order (price) and the goods received (quantity). A clean match is approved for payment straight away; anything
/// else is held for Finance to approve with a reason, or reject.
/// </summary>
public sealed class SupplierInvoice : AggregateRoot
{
    /// <summary>How far above the ordered unit price an invoice may be and still match.</summary>
    public const decimal PriceTolerance = 0.02m;

    private readonly List<SupplierInvoiceLine> _lines = [];

    private SupplierInvoice() { }

    public SupplierInvoice(string number, Guid supplierId, string supplierInvoiceNumber, Guid purchaseOrderId,
        string purchaseOrderNumber, DateOnly invoiceDate, int paymentTermsDays, decimal vatAmount, Guid recordedBy,
        DateTime now, IReadOnlyCollection<InvoicedLine> lines)
    {
        var billed = lines.Where(l => l.Quantity > 0).ToList();
        if (billed.Count == 0)
            throw new DomainException("An invoice needs at least one line with a quantity.");
        if (vatAmount < 0)
            throw new DomainException("VAT can't be negative.");
        if (invoiceDate > DateOnly.FromDateTime(now))
            throw new DomainException("The invoice date can't be in the future.");

        Number = Guard.Code(number, nameof(number), 20);
        SupplierId = supplierId;
        SupplierInvoiceNumber = Guard.Required(supplierInvoiceNumber, nameof(supplierInvoiceNumber), 50);
        PurchaseOrderId = purchaseOrderId;
        PurchaseOrderNumber = Guard.Code(purchaseOrderNumber, nameof(purchaseOrderNumber), 20);
        InvoiceDate = invoiceDate;
        DueDate = invoiceDate.AddDays(paymentTermsDays);
        VatAmount = vatAmount;
        CurrencyCode = "NGN";
        RecordedBy = recordedBy;
        RecordedAt = now;
        Status = InvoiceStatus.OnHold; // until matched

        var lineNo = 1;
        foreach (var line in billed)
            _lines.Add(new SupplierInvoiceLine(Id, lineNo++, line));
    }

    public string Number { get; private set; } = null!;
    public Guid SupplierId { get; private set; }
    public string SupplierInvoiceNumber { get; private set; } = null!;
    public Guid PurchaseOrderId { get; private set; }
    public string PurchaseOrderNumber { get; private set; } = null!;
    public DateOnly InvoiceDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public decimal VatAmount { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public InvoiceStatus Status { get; private set; }
    public Guid RecordedBy { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public DateTime? MatchedAt { get; private set; }
    public string? DecisionReason { get; private set; } // why it was approved anyway, or rejected
    public Guid? DecidedBy { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? PaymentReference { get; private set; }
    public DateOnly? PaidOn { get; private set; }

    public IReadOnlyCollection<SupplierInvoiceLine> Lines => _lines.AsReadOnly();

    public decimal Subtotal => _lines.Sum(l => l.LineTotal);
    public decimal Total => Subtotal + VatAmount;
    public bool ApprovedForPayment => Status is InvoiceStatus.Matched or InvoiceStatus.Approved;

    /// <summary>
    /// Checks every line against the order and the goods received. <paramref name="facts"/> holds, per order line,
    /// how much has been received and how much other live invoices already bill. Returns true when it matched.
    /// </summary>
    public bool Match(IReadOnlyDictionary<Guid, LineFacts> facts, DateTime now)
    {
        if (Status != InvoiceStatus.OnHold)
            throw new DomainException($"Only a held invoice can be matched; this one is {Status}.");

        foreach (var line in _lines)
        {
            if (!facts.TryGetValue(line.PurchaseOrderLineId, out var f))
                throw new DomainException($"Line {line.LineNo} is not on {PurchaseOrderNumber}.");
            line.Check(f, PriceTolerance);
        }

        MatchedAt = now;
        if (_lines.All(l => l.Exception is null))
        {
            Status = InvoiceStatus.Matched;
            return true;
        }
        return false;
    }

    public void ApproveAnyway(string reason, Guid by, DateTime now)
    {
        if (Status != InvoiceStatus.OnHold)
            throw new DomainException($"Only a held invoice can be approved anyway; this one is {Status}.");
        Decide(InvoiceStatus.Approved, reason, by, now);
    }

    public void Reject(string reason, Guid by, DateTime now)
    {
        if (Status != InvoiceStatus.OnHold)
            throw new DomainException($"Only a held invoice can be rejected; this one is {Status}.");
        Decide(InvoiceStatus.Rejected, reason, by, now);
    }

    public void MarkPaid(string paymentReference, DateOnly paidOn, DateTime now)
    {
        if (!ApprovedForPayment)
            throw new DomainException($"Only an invoice approved for payment can be paid; this one is {Status}.");
        if (paidOn > DateOnly.FromDateTime(now))
            throw new DomainException("The payment date can't be in the future.");
        if (paidOn < InvoiceDate)
            throw new DomainException("The payment date can't be before the invoice date.");

        PaymentReference = Guard.Required(paymentReference, nameof(paymentReference), 50);
        PaidOn = paidOn;
        Status = InvoiceStatus.Paid;
    }

    private void Decide(InvoiceStatus status, string reason, Guid by, DateTime now)
    {
        DecisionReason = Guard.Required(reason, nameof(reason), 500);
        DecidedBy = by;
        DecidedAt = now;
        Status = status;
    }
}

public sealed class SupplierInvoiceLine : AuditableEntity
{
    private SupplierInvoiceLine() { }

    internal SupplierInvoiceLine(Guid invoiceId, int lineNo, InvoicedLine source)
    {
        if (source.UnitPrice < 0)
            throw new DomainException("Prices can't be negative.");

        SupplierInvoiceId = invoiceId;
        LineNo = lineNo;
        PurchaseOrderLineId = source.PurchaseOrderLineId;
        Description = Guard.Required(source.Description, "description", 500);
        UomCode = source.UomCode;
        Quantity = source.Quantity;
        UnitPrice = source.UnitPrice;
        OrderedUnitPrice = source.OrderedUnitPrice;
        CostCentreId = source.CostCentreId;
        BudgetFiscalYear = source.BudgetFiscalYear;
    }

    public Guid SupplierInvoiceId { get; private set; }
    public int LineNo { get; private set; }
    public Guid PurchaseOrderLineId { get; private set; }
    public string Description { get; private set; } = null!;
    public string UomCode { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal OrderedUnitPrice { get; private set; } // from the order, when recorded
    public Guid CostCentreId { get; private set; }
    public int BudgetFiscalYear { get; private set; }

    // From the last match
    public decimal? ReceivedAtMatch { get; private set; }
    public decimal? InvoicedBeforeAtMatch { get; private set; }
    public string? Exception { get; private set; }

    public decimal LineTotal => decimal.Round(Quantity * UnitPrice, 2);

    internal void Check(LineFacts facts, decimal tolerance)
    {
        ReceivedAtMatch = facts.Received;
        InvoicedBeforeAtMatch = facts.AlreadyInvoiced;

        var problems = new List<string>();
        if (UnitPrice > decimal.Round(OrderedUnitPrice * (1 + tolerance), 2))
            problems.Add($"Price ₦{UnitPrice:N2} is above the ordered ₦{OrderedUnitPrice:N2}");
        if (facts.AlreadyInvoiced + Quantity > facts.Received)
            problems.Add(facts.AlreadyInvoiced > 0
                ? $"Bills {Quantity:0.####} {UomCode}, but only {facts.Received - facts.AlreadyInvoiced:0.####} received and not yet invoiced"
                : $"Bills {Quantity:0.####} {UomCode}, but only {facts.Received:0.####} received");

        Exception = problems.Count == 0 ? null : string.Join("; ", problems);
    }
}

/// <summary>One billed line, with what the order says about it.</summary>
public sealed record InvoicedLine(
    Guid PurchaseOrderLineId, string Description, string UomCode, decimal Quantity, decimal UnitPrice,
    decimal OrderedUnitPrice, Guid CostCentreId, int BudgetFiscalYear);

/// <summary>For one order line at match time: how much has arrived, and how much other live invoices already bill.</summary>
public sealed record LineFacts(decimal Received, decimal AlreadyInvoiced);
