using VivaBev.Procurement.Domain.PurchaseOrders;
using VivaBev.Procurement.Domain.Requisitions;
using VivaBev.SharedKernel.Domain;
using Xunit;

namespace VivaBev.Procurement.Tests.Domain;

public class PurchaseOrderTests
{
    private static readonly DateOnly Today = new(2026, 11, 24);
    private static readonly DateTime Now = new(2026, 11, 24, 9, 0, 0, DateTimeKind.Utc);

    private static OrderedLine Line(decimal quantity, decimal price, Guid? costCentre = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "REQ-2027-00002", costCentre ?? Guid.NewGuid(), 2027,
            null, "IT-LAPTOP", "Business laptop", quantity, "EA", price);

    private static PurchaseOrder Draft(params OrderedLine[] lines) =>
        new("PO-2027-00001", Guid.NewGuid(), Guid.NewGuid(), "RFQ-2027-00001", Guid.NewGuid(), "Q-114", Guid.NewGuid(),
            30, Guid.NewGuid(), Today.AddDays(14), lines.Length == 0 ? [Line(3, 850_000m)] : lines);

    [Fact]
    public void A_new_order_is_a_draft_numbered_with_its_lines_totalled()
    {
        var po = Draft(Line(3, 850_000m), Line(1, 120_000m));

        Assert.Equal(PurchaseOrderStatus.Draft, po.Status);
        Assert.Equal(new[] { 1, 2 }, po.Lines.Select(l => l.LineNo));
        Assert.Equal(2_670_000m, po.Total);
    }

    [Fact]
    public void An_order_needs_lines() =>
        Assert.Throws<DomainException>(() => new PurchaseOrder("PO-2027-00002", Guid.NewGuid(), Guid.NewGuid(),
            "RFQ-2027-00001", Guid.NewGuid(), null, Guid.NewGuid(), 30, Guid.NewGuid(), Today, []));

    [Fact]
    public void Delivery_details_can_change_only_while_draft()
    {
        var po = Draft();
        var site = Guid.NewGuid();

        po.UpdateDelivery(site, Today.AddDays(20), "  Deliver to gate B  ", Today);
        Assert.Equal(site, po.DeliverToSiteId);
        Assert.Equal("Deliver to gate B", po.Notes);
        Assert.Throws<DomainException>(() => po.UpdateDelivery(site, Today.AddDays(-1), null, Today));

        po.Issue(Now);
        Assert.Equal(PurchaseOrderStatus.Issued, po.Status);
        Assert.Throws<DomainException>(() => po.UpdateDelivery(site, Today.AddDays(30), null, Today));
    }

    private static readonly Guid Buyer = Guid.NewGuid();

    [Fact]
    public void An_issued_order_can_be_cancelled_until_something_arrives()
    {
        var po = Draft(Line(3, 850_000m));
        po.Issue(Now);
        po.Cancel("Supplier can no longer deliver", Buyer, Now);

        Assert.Equal(PurchaseOrderStatus.Cancelled, po.Status);
        Assert.Equal(Buyer, po.ClosedBy);

        var partly = Draft(Line(3, 850_000m));
        partly.Issue(Now);
        partly.RecordReceipt([(partly.Lines.First().Id, 1m)]);
        Assert.Throws<DomainException>(() => partly.Cancel("Too late", Buyer, Now));
    }

    [Fact]
    public void A_draft_can_be_cancelled_with_a_reason()
    {
        var po = Draft();
        po.Cancel("Wrong delivery site; raising again", Buyer, Now);
        Assert.Equal(PurchaseOrderStatus.Cancelled, po.Status);
        Assert.Throws<DomainException>(() => po.Issue(Now));
    }

    [Fact]
    public void Only_a_partly_received_order_can_be_closed_short_and_the_rest_is_valued()
    {
        var po = Draft(Line(3, 850_000m), Line(2, 25_000m));
        Assert.Throws<DomainException>(() => po.CloseShort("x", Buyer, Now)); // draft

        po.Issue(Now);
        Assert.Throws<DomainException>(() => po.CloseShort("x", Buyer, Now)); // nothing received: cancel instead

        po.RecordReceipt([(po.Lines.First().Id, 2m), (po.Lines.Last().Id, 2m)]);
        po.CloseShort("Third laptop discontinued", Buyer, Now);

        Assert.Equal(PurchaseOrderStatus.Closed, po.Status);
        var (line, value) = Assert.Single(po.OutstandingValue());
        Assert.Equal(1, line.LineNo);
        Assert.Equal(850_000m, value);
        Assert.Throws<DomainException>(() => po.RecordReceipt([(line.Id, 1m)]));
    }

    [Fact]
    public void An_ordered_requisition_cannot_be_cancelled_by_its_requester()
    {
        var requisition = new Requisition("REQ-2027-00002", "Laptops", "New analysts", Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Today.AddDays(30), Today);
        requisition.AddLine(LineType.Goods, null, null, "Laptop", Guid.NewGuid(), 3, "EA", 850_000m);
        requisition.Submit(Now);
        requisition.MarkApproved(Now);

        Assert.Throws<DomainException>(() => requisition.MarkOrdered()); // must be sourced first
        requisition.MarkInSourcing();
        requisition.MarkOrdered();

        Assert.Equal(RequisitionStatus.Ordered, requisition.Status);
        Assert.Throws<DomainException>(() => requisition.Cancel("No longer needed"));
    }

    [Fact]
    public void When_its_order_is_cancelled_a_requisition_goes_back_for_sourcing_or_is_cancelled()
    {
        Requisition Ordered()
        {
            var r = new Requisition("REQ-2027-00003", "Laptops", "New analysts", Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(), Guid.NewGuid(), Today.AddDays(30), Today);
            r.AddLine(LineType.Goods, null, null, "Laptop", Guid.NewGuid(), 3, "EA", 850_000m);
            r.Submit(Now);
            r.MarkApproved(Now);
            r.MarkInSourcing();
            r.MarkOrdered();
            return r;
        }

        var back = Ordered();
        back.ReturnFromCancelledOrder();
        Assert.Equal(RequisitionStatus.Approved, back.Status);
        Assert.Throws<DomainException>(back.ReturnFromCancelledOrder);

        var gone = Ordered();
        gone.CancelWithOrder("Supplier failed; project shelved");
        Assert.Equal(RequisitionStatus.Cancelled, gone.Status);
    }

    [Fact]
    public void Receiving_moves_an_issued_order_to_partly_then_fully_received()
    {
        var po = Draft(Line(3, 850_000m), Line(1, 120_000m));
        var (first, second) = (po.Lines.First().Id, po.Lines.Last().Id);
        Assert.Throws<DomainException>(() => po.RecordReceipt([(first, 1m)])); // not issued yet

        po.Issue(Now);
        po.RecordReceipt([(first, 2m)]);
        Assert.Equal(PurchaseOrderStatus.PartiallyReceived, po.Status);

        Assert.Throws<DomainException>(() => po.RecordReceipt([(first, 2m)])); // only 1 left
        po.RecordReceipt([(first, 1m), (second, 1m)]);
        Assert.Equal(PurchaseOrderStatus.Received, po.Status);
        Assert.All(po.Lines, l => Assert.Equal(0m, l.Outstanding));
    }
}
