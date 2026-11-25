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

    [Fact]
    public void Closing_an_order_short_gives_back_part_of_its_commitment()
    {
        var budget = new Budget(Guid.NewGuid(), 2027, 10_000_000m);
        var po = Guid.NewGuid();
        budget.Commit("PurchaseOrder", po, "PO-2027-00001", 2_550_000m, Now);

        Assert.True(budget.Reduce("PurchaseOrder", po, 850_000m, Now));
        Assert.Equal(1_700_000m, budget.Committed);

        Assert.True(budget.Reduce("PurchaseOrder", po, 5_000_000m, Now)); // never below zero; released when empty
        Assert.Equal(0m, budget.Committed);
        Assert.False(budget.Reduce("PurchaseOrder", po, 1m, Now));
    }
}
