using VivaBev.SharedKernel.Domain;

namespace VivaBev.Finance.Domain;

public enum CommitmentStatus
{
    Open,
    Released,
    Consumed
}

public sealed class BudgetCommitment : AuditableEntity
{
    private BudgetCommitment() { }

    internal BudgetCommitment(Guid budgetId, string documentType, Guid documentId, string documentNumber,
        decimal amount, DateTime now)
    {
        if (amount <= 0)
            throw new DomainException("A commitment must be greater than zero.");

        BudgetId = budgetId;
        DocumentType = Guard.Required(documentType, nameof(documentType), 30);
        DocumentId = documentId;
        DocumentNumber = Guard.Required(documentNumber, nameof(documentNumber), 30);
        Amount = amount;
        Status = CommitmentStatus.Open;
        OpenedAt = now;
    }

    public Guid BudgetId { get; private set; }
    public string DocumentType { get; private set; } = null!;
    public Guid DocumentId { get; private set; }
    public string DocumentNumber { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public CommitmentStatus Status { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    internal void Release(DateTime now)
    {
        Status = CommitmentStatus.Released;
        ClosedAt = now;
    }
}
