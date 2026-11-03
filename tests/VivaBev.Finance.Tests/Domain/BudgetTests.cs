using VivaBev.Finance.Domain;
using VivaBev.SharedKernel.Domain;
using Xunit;

namespace VivaBev.Finance.Tests.Domain;

public class BudgetTests
{
    private static readonly DateTime Now = new(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);

    private static Budget NewBudget(decimal amount = 12_000_000m) => new(Guid.NewGuid(), 2027, amount);

    [Fact]
    public void A_new_budget_is_fully_available()
    {
        var budget = NewBudget();

        Assert.Equal(0m, budget.Committed);
        Assert.Equal(12_000_000m, budget.Available);
    }

    [Fact]
    public void Commitments_reduce_what_is_available()
    {
        var budget = NewBudget();

        budget.Commit("Requisition", Guid.NewGuid(), "REQ-2027-00004", 8_500_000m, Now);

        Assert.Equal(8_500_000m, budget.Committed);
        Assert.Equal(3_500_000m, budget.Available);
    }

    [Fact]
    public void A_budget_can_be_over_committed()
    {
        var budget = NewBudget();

        budget.Commit("Requisition", Guid.NewGuid(), "REQ-2027-00004", 8_500_000m, Now);
        budget.Commit("Requisition", Guid.NewGuid(), "REQ-2027-00005", 4_250_000m, Now);

        Assert.Equal(-750_000m, budget.Available); // soft budget: allowed, and visible
    }

    [Fact]
    public void The_same_document_cannot_be_committed_twice()
    {
        var budget = NewBudget();
        var requisition = Guid.NewGuid();
        budget.Commit("Requisition", requisition, "REQ-2027-00004", 8_500_000m, Now);

        Assert.Throws<DomainException>(() =>
            budget.Commit("Requisition", requisition, "REQ-2027-00004", 8_500_000m, Now));
    }

    [Fact]
    public void Releasing_gives_the_money_back_and_keeps_the_history()
    {
        var budget = NewBudget();
        var requisition = Guid.NewGuid();
        budget.Commit("Requisition", requisition, "REQ-2027-00004", 8_500_000m, Now);

        Assert.True(budget.Release("Requisition", requisition, Now));

        Assert.Equal(12_000_000m, budget.Available);
        Assert.Equal(CommitmentStatus.Released, budget.Commitments.Single().Status);
        Assert.False(budget.Release("Requisition", requisition, Now)); // nothing open any more
    }

    [Fact]
    public void A_budget_cannot_be_negative() =>
        Assert.Throws<DomainException>(() => NewBudget(-1m));
}
