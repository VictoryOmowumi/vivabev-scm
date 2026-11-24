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

    [Fact]
    public void An_issued_order_cannot_be_cancelled_yet()
    {
        var po = Draft();
        po.Issue(Now);
        Assert.Throws<DomainException>(() => po.Cancel("Supplier went bust"));
    }

    [Fact]
    public void A_draft_can_be_cancelled_with_a_reason()
    {
        var po = Draft();
        po.Cancel("Wrong delivery site; raising again");
        Assert.Equal(PurchaseOrderStatus.Cancelled, po.Status);
        Assert.Throws<DomainException>(() => po.Issue(Now));
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
