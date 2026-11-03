using VivaBev.SharedKernel.Domain;

namespace VivaBev.Finance.Domain;

public sealed class Budget : AggregateRoot
{
    private readonly List<BudgetCommitment> _commitments = [];

    private Budget() { }

    public Budget(Guid costCentreId, int fiscalYear, decimal amount)
    {
        if (fiscalYear is < 2000 or > 2100)
            throw new DomainException("The fiscal year looks wrong.");

        CostCentreId = costCentreId;
        FiscalYear = fiscalYear;
        CurrencyCode = "NGN";
        SetAmount(amount);
    }

    public Guid CostCentreId { get; private set; }
    public int FiscalYear { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = null!;

    public IReadOnlyCollection<BudgetCommitment> Commitments => _commitments.AsReadOnly();

    public decimal Committed => _commitments.Where(c => c.Status == CommitmentStatus.Open).Sum(c => c.Amount);
    public decimal Consumed => _commitments.Where(c => c.Status == CommitmentStatus.Consumed).Sum(c => c.Amount);
    public decimal Available => Amount - Committed - Consumed;

    public void SetAmount(decimal amount)
    {
        if (amount < 0)
            throw new DomainException("A budget cannot be negative.");
        Amount = amount;
    }

    public BudgetCommitment Commit(string documentType, Guid documentId, string documentNumber, decimal amount,
        DateTime now)
    {
        if (_commitments.Any(c => c.DocumentType == documentType && c.DocumentId == documentId
                                  && c.Status == CommitmentStatus.Open))
            throw new DomainException($"{documentNumber} already has an open commitment on this budget.");

        var commitment = new BudgetCommitment(Id, documentType, documentId, documentNumber, amount, now);
        _commitments.Add(commitment);
        return commitment;
    }

    /// <summary>Releases the document's open commitment. Returns false if it had none.</summary>
    public bool Release(string documentType, Guid documentId, DateTime now)
    {
        var commitment = _commitments.SingleOrDefault(c =>
            c.DocumentType == documentType && c.DocumentId == documentId && c.Status == CommitmentStatus.Open);

        if (commitment is null)
            return false;

        commitment.Release(now);
        return true;
    }
}
