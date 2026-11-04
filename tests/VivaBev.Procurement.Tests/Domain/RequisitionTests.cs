using VivaBev.Procurement.Domain.Requisitions;
using VivaBev.SharedKernel.Domain;
using VivaBev.SharedKernel.Time;
using Xunit;

namespace VivaBev.Procurement.Tests.Domain;

public class RequisitionTests
{
    private static readonly DateOnly Today = new(2026, 10, 30);
    private static readonly DateTime Now = new(2026, 10, 30, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Category = Guid.NewGuid();

    private static Requisition NewDraft() =>
        new("req-2027-00001", "Laptops for new analysts", "Three analysts join in November",
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Today.AddDays(30), Today);

    [Fact]
    public void A_new_requisition_is_a_draft_with_no_total()
    {
        var requisition = NewDraft();

        Assert.Equal(RequisitionStatus.Draft, requisition.Status);
        Assert.Equal("REQ-2027-00001", requisition.Number);
        Assert.Equal(0m, requisition.EstimatedTotal);
    }

    [Fact]
    public void The_required_by_date_cannot_be_in_the_past() =>
        Assert.Throws<DomainException>(() =>
            new Requisition("REQ-2027-00002", "Late", "Too late", Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(), Guid.NewGuid(), Today.AddDays(-1), Today));

    [Fact]
    public void Lines_are_numbered_and_the_total_follows_them()
    {
        var requisition = NewDraft();

        requisition.AddLine(LineType.Goods, null, null, "Laptop", Category, 3, "EA", 850_000m);
        requisition.AddLine(LineType.Service, null, null, "Set-up", Category, 1, "DAY", 120_000m);

        Assert.Equal(new[] { 1, 2 }, requisition.Lines.Select(l => l.LineNo));
        Assert.Equal(2_670_000m, requisition.EstimatedTotal);

        requisition.RemoveLine(1);

        Assert.Equal(120_000m, requisition.EstimatedTotal);
        Assert.Equal(2, requisition.Lines.Single().LineNo); // not renumbered
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(2, 0)]
    [InlineData(-1, 100)]
    public void Quantity_and_price_must_be_positive(int quantity, int price)
    {
        var requisition = NewDraft();

        Assert.Throws<DomainException>(() =>
            requisition.AddLine(LineType.Goods, null, null, "Item", Category, quantity, "EA", price));
    }

    [Fact]
    public void An_empty_requisition_cannot_be_submitted() =>
        Assert.Throws<DomainException>(() => NewDraft().Submit(Now));

    [Fact]
    public void A_submitted_requisition_cannot_be_edited()
    {
        var requisition = NewDraft();
        requisition.AddLine(LineType.Goods, null, null, "Laptop", Category, 1, "EA", 850_000m);
        requisition.Submit(Now);

        Assert.Equal(RequisitionStatus.PendingApproval, requisition.Status);
        Assert.Throws<DomainException>(() =>
            requisition.AddLine(LineType.Goods, null, null, "Mouse", Category, 1, "EA", 15_000m));
    }

    [Fact]
    public void A_rejected_requisition_returns_to_draft_with_the_reason()
    {
        var requisition = NewDraft();
        requisition.AddLine(LineType.Goods, null, null, "Laptop", Category, 1, "EA", 850_000m);
        requisition.Submit(Now);

        requisition.MarkRejected("Use the spare laptops in stores");

        Assert.Equal(RequisitionStatus.Draft, requisition.Status);
        Assert.Equal("Use the spare laptops in stores", requisition.RejectionReason);
        requisition.AddLine(LineType.Goods, null, null, "Docking station", Category, 1, "EA", 95_000m); // editable again
    }

    [Fact]
    public void Only_a_pending_requisition_can_be_marked_approved() =>
        Assert.Throws<DomainException>(() => NewDraft().MarkApproved(Now));

    [Fact]
    public void The_budget_check_is_recorded_only_while_drafting()
    {
        var requisition = NewDraft();
        requisition.AddLine(LineType.Goods, null, null, "Laptop", Category, 5, "EA", 850_000m);

        requisition.RecordBudgetCheck(2027, 3_500_000m, isOverBudget: true);
        requisition.Submit(Now);

        Assert.Equal(BudgetStatus.OverBudget, requisition.BudgetStatus);
        Assert.Equal(3_500_000m, requisition.BudgetAvailableAtSubmit);
        Assert.Throws<DomainException>(() => requisition.RecordBudgetCheck(2027, 0m, isOverBudget: true));
    }

    [Theory]
    [InlineData(2026, 6, 30, 2026)]
    [InlineData(2026, 7, 1, 2027)]
    [InlineData(2026, 10, 30, 2027)]
    public void Fiscal_years_run_July_to_June(int year, int month, int day, int expected) =>
        Assert.Equal(expected, FiscalCalendar.FiscalYearOf(new DateTime(year, month, day)));

    [Fact]
    public void A_draft_can_be_retargeted_and_renamed()
    {
        var requisition = NewDraft();
        var costCentre = Guid.NewGuid();
        var site = Guid.NewGuid();

        requisition.UpdateDetails("Laptops for the audit team", "Two auditors join in December", costCentre, site,
            Today.AddDays(45), Today);

        Assert.Equal("Laptops for the audit team", requisition.Title);
        Assert.Equal(costCentre, requisition.CostCentreId);
        Assert.Equal(site, requisition.DeliverToSiteId);
        Assert.Equal(Today.AddDays(45), requisition.RequiredBy);
    }

    [Fact]
    public void Only_a_draft_can_be_edited()
    {
        var requisition = NewDraft();
        requisition.AddLine(LineType.Goods, null, null, "Laptop", Category, 1, "EA", 850_000m);
        requisition.Submit(Now);

        Assert.Throws<DomainException>(() => requisition.UpdateDetails("New title", "Why", Guid.NewGuid(),
            Guid.NewGuid(), Today.AddDays(10), Today));
    }

    [Fact]
    public void An_edit_cannot_move_the_required_by_date_into_the_past() =>
        Assert.Throws<DomainException>(() => NewDraft().UpdateDetails("Title", "Why", Guid.NewGuid(),
            Guid.NewGuid(), Today.AddDays(-1), Today));
}
