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

        Assert.Equal(RequisitionStatus.Submitted, requisition.Status);
        Assert.Throws<DomainException>(() =>
            requisition.AddLine(LineType.Goods, null, null, "Mouse", Category, 1, "EA", 15_000m));
    }

    [Theory]
    [InlineData(2026, 6, 30, 2026)]
    [InlineData(2026, 7, 1, 2027)]
    [InlineData(2026, 10, 30, 2027)]
    public void Fiscal_years_run_July_to_June(int year, int month, int day, int expected) =>
        Assert.Equal(expected, FiscalCalendar.FiscalYearOf(new DateTime(year, month, day)));
}
