using VivaBev.Procurement.Domain.Requisitions;
using VivaBev.Procurement.Domain.Sourcing;
using VivaBev.SharedKernel.Domain;
using Xunit;

namespace VivaBev.Procurement.Tests.Domain;

public class SourcingEventTests
{
    private static readonly DateOnly Today = new(2026, 11, 5);
    private static readonly DateTime Now = new(2026, 11, 5, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Category = Guid.NewGuid();

    private static SourcedLine Line(decimal quantity, decimal price, Guid? requisitionId = null) =>
        new(requisitionId ?? Guid.NewGuid(), "REQ-2027-00001", Guid.NewGuid(), null, null, "UPS 120 kVA",
            Category, quantity, "EA", price);

    private static SourcingEvent Draft(params SourcedLine[] lines)
    {
        var e = new SourcingEvent("rfq-2027-00001", "Data centre UPS", Guid.NewGuid(), Today.AddDays(10), Today);
        e.AddLines(lines.Length == 0 ? [Line(1, 500_000m)] : lines);
        return e;
    }

    private static SourcingEvent Published(out Guid supplier)
    {
        var e = Draft();
        supplier = Guid.NewGuid();
        e.Invite(supplier, Now);
        e.Publish(Now);
        return e;
    }

    [Fact]
    public void A_new_rfq_is_a_draft_with_its_lines_numbered()
    {
        var e = Draft(Line(2, 100_000m), Line(1, 50_000m));

        Assert.Equal(SourcingEventStatus.Draft, e.Status);
        Assert.Equal("RFQ-2027-00001", e.Number);
        Assert.Equal(new[] { 1, 2 }, e.Lines.Select(l => l.LineNo));
        Assert.Equal(250_000m, e.EstimatedTotal);
    }

    [Fact]
    public void The_deadline_must_be_in_the_future() =>
        Assert.Throws<DomainException>(() =>
            new SourcingEvent("RFQ-2027-00002", "Late", Guid.NewGuid(), Today, Today));

    [Fact]
    public void Publishing_needs_at_least_one_supplier() =>
        Assert.Throws<DomainException>(() => Draft().Publish(Now));

    [Fact]
    public void Rfqs_at_or_over_two_million_need_three_suppliers()
    {
        var e = Draft(Line(1, 2_000_000m));
        e.Invite(Guid.NewGuid(), Now);
        e.Invite(Guid.NewGuid(), Now);

        Assert.True(e.NeedsCompetition);
        Assert.Throws<DomainException>(() => e.Publish(Now));

        e.Invite(Guid.NewGuid(), Now);
        e.Publish(Now);
        Assert.Equal(SourcingEventStatus.Published, e.Status);
        Assert.All(e.Invitations, i => Assert.Equal(Now, i.InvitedAt));
    }

    [Fact]
    public void Below_two_million_one_supplier_is_enough()
    {
        var e = Published(out _);
        Assert.False(e.NeedsCompetition);
        Assert.Equal(SourcingEventStatus.Published, e.Status);
    }

    [Fact]
    public void Lines_and_invitations_freeze_once_published()
    {
        var e = Published(out var supplier);

        Assert.Throws<DomainException>(() => e.AddLines([Line(1, 10_000m)]));
        Assert.Throws<DomainException>(() => e.Uninvite(supplier));
    }

    [Fact]
    public void Only_invited_suppliers_can_quote()
    {
        var e = Published(out _);
        var line = e.Lines.Single();

        Assert.Throws<DomainException>(() => e.RecordQuotation(Guid.NewGuid(), null, Today.AddDays(30), 14, 30,
            [new QuotedLine(line.Id, 480_000m, 1, null)], Now));
    }

    [Fact]
    public void A_quotation_totals_its_lines_and_marks_the_supplier_as_responded()
    {
        var e = Published(out var supplier);
        var line = e.Lines.Single();

        var q = e.RecordQuotation(supplier, "Q-881", Today.AddDays(30), 14, 30,
            [new QuotedLine(line.Id, 480_000m, 1, "Includes delivery")], Now);

        Assert.Equal(480_000m, q.Total);
        Assert.Equal(InvitationStatus.Responded, e.Invitations.Single().Status);
    }

    [Fact]
    public void A_supplier_has_one_live_quotation_until_it_is_withdrawn()
    {
        var e = Published(out var supplier);
        var line = e.Lines.Single();
        QuotedLine[] priced = [new QuotedLine(line.Id, 480_000m, 1, null)];
        var first = e.RecordQuotation(supplier, null, Today.AddDays(30), 14, 30, priced, Now);

        Assert.Throws<DomainException>(() => e.RecordQuotation(supplier, null, Today.AddDays(30), 14, 30, priced, Now));

        e.WithdrawQuotation(first.Id, "Typo in unit price");
        Assert.Equal(InvitationStatus.Invited, e.Invitations.Single().Status);

        var second = e.RecordQuotation(supplier, null, Today.AddDays(30), 14, 30, priced, Now);
        Assert.Equal(QuotationStatus.Received, second.Status);
    }

    [Fact]
    public void An_expired_quotation_is_refused()
    {
        var e = Published(out var supplier);
        Assert.Throws<DomainException>(() => e.RecordQuotation(supplier, null, Today.AddDays(-1), 14, 30,
            [new QuotedLine(e.Lines.Single().Id, 480_000m, 1, null)], Now));
    }

    [Fact]
    public void Closing_needs_at_least_one_quotation()
    {
        var e = Published(out var supplier);
        Assert.Throws<DomainException>(() => e.CloseResponses(Now));

        e.RecordQuotation(supplier, null, Today.AddDays(30), 14, 30,
            [new QuotedLine(e.Lines.Single().Id, 480_000m, 1, null)], Now);
        e.CloseResponses(Now);

        Assert.Equal(SourcingEventStatus.ResponsesClosed, e.Status);
    }

    [Fact]
    public void A_cancelled_rfq_cannot_be_cancelled_again()
    {
        var e = Draft();
        e.Cancel("Requirement changed");
        Assert.Throws<DomainException>(() => e.Cancel("Again"));
    }

    [Fact]
    public void Removing_a_requisition_takes_all_its_lines()
    {
        var requisition = Guid.NewGuid();
        var e = Draft(Line(1, 10_000m, requisition), Line(2, 10_000m, requisition), Line(1, 5_000m));

        e.RemoveRequisition(requisition);

        Assert.Single(e.Lines);
    }

    [Fact]
    public void A_requisition_in_sourcing_cannot_be_cancelled_by_its_requester()
    {
        var requisition = new Requisition("REQ-2027-00001", "UPS", "Failed load test", Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Today.AddDays(30), Today);
        requisition.AddLine(LineType.Goods, null, null, "UPS", Category, 1, "EA", 500_000m);
        requisition.Submit(Now);
        requisition.MarkApproved(Now);

        requisition.MarkInSourcing();
        Assert.Throws<DomainException>(() => requisition.Cancel("No longer needed"));

        requisition.ReturnFromSourcing();
        Assert.Equal(RequisitionStatus.Approved, requisition.Status);
    }
}
