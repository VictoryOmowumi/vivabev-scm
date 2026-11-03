namespace VivaBev.Finance.Contracts;

public sealed record BudgetCheckDto(
    Guid CostCentreId, int FiscalYear, bool HasBudget, decimal Budget, decimal Committed, decimal Consumed,
    decimal Available, decimal Requested, bool IsOverBudget);

public sealed record BudgetCommitmentRequest(
    Guid CostCentreId, int FiscalYear, string DocumentType, Guid DocumentId, string DocumentNumber, decimal Amount);

public interface IBudgets
{
    /// <summary>Compares an amount with what's available in the cost centre's budget for the date's fiscal year.</summary>
    Task<BudgetCheckDto> CheckAsync(Guid costCentreId, DateTime onDate, decimal amount, CancellationToken ct = default);

    /// <summary>Reserves an amount against a budget. Call inside the caller's transaction.</summary>
    Task CommitAsync(BudgetCommitmentRequest request, CancellationToken ct = default);

    /// <summary>Releases a document's open commitment, if it has one.</summary>
    Task ReleaseAsync(string documentType, Guid documentId, CancellationToken ct = default);
}

// ---- API shapes ----

public sealed record SetBudgetRequest(Guid CostCentreId, int FiscalYear, decimal Amount);

public sealed record BudgetSummaryDto(
    Guid Id, Guid CostCentreId, string CostCentreCode, string CostCentreName, int FiscalYear, string CurrencyCode,
    decimal Amount, decimal Committed, decimal Consumed, decimal Available);

public sealed record BudgetCommitmentDto(
    string DocumentType, Guid DocumentId, string DocumentNumber, decimal Amount, string Status,
    DateTime OpenedAt, DateTime? ClosedAt);

public sealed record BudgetDetailDto(BudgetSummaryDto Budget, IReadOnlyList<BudgetCommitmentDto> Commitments);
