namespace VivaBev.Procurement.Contracts;

/// <summary>What other modules may ask Procurement about requisitions.</summary>
public interface IRequisitionQueries
{
    /// <summary>
    /// Per cost centre: the total of requisitions submitted against a fiscal year's budget and still awaiting
    /// approval. Budgets only see money once it's approved (committed); this is the amount on its way.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetInApprovalTotalsAsync(int fiscalYear, CancellationToken ct = default);
}
