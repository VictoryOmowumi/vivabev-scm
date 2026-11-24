using VivaBev.SharedKernel.Domain;

namespace VivaBev.Procurement.Domain.Sourcing;

public enum CriterionKind
{
    Price,        // scored automatically: lowest total gets 10
    Delivery,     // scored automatically: shortest lead time gets 10
    PaymentTerms, // scored automatically: longest credit period gets 10
    Manual        // the buyer scores each supplier from 0 to 10
}

/// <summary>One thing quotations are judged on, with its share of the total (weights add up to 100).</summary>
public sealed class EvaluationCriterion : AuditableEntity
{
    private EvaluationCriterion() { }

    internal EvaluationCriterion(Guid sourcingEventId, int sortOrder, string name, CriterionKind kind, int weight)
    {
        SourcingEventId = sourcingEventId;
        SortOrder = sortOrder;
        Kind = kind;
        Update(name, weight);
    }

    public Guid SourcingEventId { get; private set; }
    public int SortOrder { get; private set; }
    public string Name { get; private set; } = null!;
    public CriterionKind Kind { get; private set; }
    public int Weight { get; private set; }

    internal void Update(string name, int weight)
    {
        if (weight is < 1 or > 100)
            throw new DomainException("Each weight must be between 1 and 100.");
        Name = Guard.Required(name, nameof(name), 100);
        Weight = weight;
    }

    internal void MoveTo(int sortOrder) => SortOrder = sortOrder;
}

/// <summary>The buyer's 0–10 score for one supplier's quotation on one manual criterion.</summary>
public sealed class EvaluationScore : AuditableEntity
{
    private EvaluationScore() { }

    internal EvaluationScore(Guid sourcingEventId, Guid criterionId, Guid quotationId, decimal score)
    {
        SourcingEventId = sourcingEventId;
        CriterionId = criterionId;
        QuotationId = quotationId;
        Set(score);
    }

    public Guid SourcingEventId { get; private set; }
    public Guid CriterionId { get; private set; }
    public Guid QuotationId { get; private set; }
    public decimal Score { get; private set; }

    internal void Set(decimal score)
    {
        if (score is < 0 or > 10)
            throw new DomainException("Scores run from 0 to 10.");
        if (score * 2 != decimal.Truncate(score * 2))
            throw new DomainException("Score in steps of 0.5.");
        Score = score;
    }
}

/// <summary>A criterion as the buyer sets it; Id is null for a new one.</summary>
public sealed record CriterionSpec(Guid? Id, string Name, CriterionKind Kind, int Weight);

/// <summary>How one quotation scores: per criterion (null = not scored yet) and in total, out of 100.</summary>
public sealed record QuotationEvaluation(
    Guid QuotationId,
    Guid SupplierId,
    decimal Total,
    IReadOnlyDictionary<Guid, decimal?> Scores,
    decimal WeightedTotal,
    bool FullyScored,
    int Rank);
