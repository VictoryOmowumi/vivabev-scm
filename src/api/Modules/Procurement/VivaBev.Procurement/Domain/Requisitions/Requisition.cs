using VivaBev.SharedKernel.Domain;

namespace VivaBev.Procurement.Domain.Requisitions;

public sealed class Requisition : AggregateRoot
{
    private readonly List<RequisitionLine> _lines = [];

    private Requisition() { }

    public Requisition(string number, string title, string justification, Guid requesterId, Guid departmentId,
        Guid costCentreId, Guid deliverToSiteId, DateOnly requiredBy, DateOnly today)
    {
        if (requiredBy < today)
            throw new DomainException("The required-by date cannot be in the past.");

        Number = Guard.Code(number, nameof(number), 20);
        Title = Guard.Required(title, nameof(title), 200);
        Justification = Guard.Required(justification, nameof(justification), 1000);
        RequesterId = requesterId;
        DepartmentId = departmentId;
        CostCentreId = costCentreId;
        DeliverToSiteId = deliverToSiteId;
        RequiredBy = requiredBy;
        CurrencyCode = "NGN";
        Status = RequisitionStatus.Draft;
    }

    public string Number { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string Justification { get; private set; } = null!;
    public Guid RequesterId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public Guid CostCentreId { get; private set; }
    public Guid DeliverToSiteId { get; private set; }
    public DateOnly RequiredBy { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public decimal EstimatedTotal { get; private set; }
    public RequisitionStatus Status { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public string? CancellationReason { get; private set; }

    public IReadOnlyCollection<RequisitionLine> Lines => _lines.AsReadOnly();

    public RequisitionLine AddLine(LineType lineType, Guid? itemId, string? itemCode, string description,
        Guid categoryId, decimal quantity, string uomCode, decimal estimatedUnitPrice)
    {
        EnsureDraft("add lines to");

        var lineNo = _lines.Count == 0 ? 1 : _lines.Max(l => l.LineNo) + 1;
        var line = new RequisitionLine(Id, lineNo, lineType, itemId, itemCode, description, categoryId,
            quantity, uomCode, estimatedUnitPrice);

        _lines.Add(line);
        RecalculateTotal();
        return line;
    }

    public void RemoveLine(int lineNo)
    {
        EnsureDraft("remove lines from");

        var line = _lines.SingleOrDefault(l => l.LineNo == lineNo)
            ?? throw new DomainException($"Line {lineNo} does not exist.");

        _lines.Remove(line);
        RecalculateTotal();
    }

    public void Submit(DateTime now)
    {
        EnsureDraft("submit");

        if (_lines.Count == 0)
            throw new DomainException("Add at least one line before submitting.");
        if (RequiredBy < DateOnly.FromDateTime(now))
            throw new DomainException("The required-by date has passed.");

        Status = RequisitionStatus.Submitted;
        SubmittedAt = now;
    }

    public void Cancel(string reason)
    {
        if (Status == RequisitionStatus.Cancelled)
            throw new DomainException("This requisition is already cancelled.");

        CancellationReason = Guard.Required(reason, nameof(reason), 500);
        Status = RequisitionStatus.Cancelled;
    }

    private void EnsureDraft(string action)
    {
        if (Status != RequisitionStatus.Draft)
            throw new DomainException($"Cannot {action} a requisition that is {Status}.");
    }

    private void RecalculateTotal() => EstimatedTotal = _lines.Sum(l => l.LineTotal);
}
