using VivaBev.Inventory.Domain.Stock;
using VivaBev.SharedKernel.Domain;
using Xunit;

namespace VivaBev.Inventory.Tests.Domain;

public class StockRequestTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Requester = Guid.NewGuid();

    private static RequestedItem Item(string code, decimal quantity) =>
        new(Guid.NewGuid(), code, $"Item {code}", "EA", quantity);

    private static StockRequest Request(params RequestedItem[] items) =>
        new("SR-2027-00001", Guid.NewGuid(), Guid.NewGuid(), Requester, "Replacement for the Ikeja sales team", null, Now, items);

    [Fact]
    public void A_request_keeps_only_lines_with_a_quantity()
    {
        var request = Request(Item("A", 3), Item("B", 0));

        Assert.Single(request.Lines);
        Assert.Equal(StockRequestStatus.Open, request.Status);
    }

    [Fact]
    public void Empty_or_duplicate_requests_are_refused()
    {
        Assert.Throws<DomainException>(() => Request(Item("A", 0)));
        var item = Item("A", 1);
        Assert.Throws<DomainException>(() => Request(item, item with { Quantity = 2 }));
    }

    [Fact]
    public void Issuing_in_parts_moves_from_partly_issued_to_issued()
    {
        var request = Request(Item("A", 3), Item("B", 2));
        var a = request.Lines.First();
        var b = request.Lines.Last();

        var issued = request.Issue(new Dictionary<Guid, decimal> { [a.Id] = 2 }, Now);
        Assert.Single(issued);
        Assert.Equal(StockRequestStatus.PartiallyIssued, request.Status);
        Assert.Equal(1, a.Outstanding);

        request.Issue(new Dictionary<Guid, decimal> { [a.Id] = 1, [b.Id] = 2 }, Now);
        Assert.Equal(StockRequestStatus.Issued, request.Status);
        Assert.Throws<DomainException>(() => request.Issue(new Dictionary<Guid, decimal> { [a.Id] = 1 }, Now));
    }

    [Fact]
    public void You_cannot_issue_more_than_was_asked_for()
    {
        var request = Request(Item("A", 3));
        Assert.Throws<DomainException>(() => request.Issue(new Dictionary<Guid, decimal> { [request.Lines.First().Id] = 4 }, Now));
    }

    [Fact]
    public void Reject_only_before_issuing_and_close_only_after()
    {
        var request = Request(Item("A", 3));
        Assert.Throws<DomainException>(() => request.CloseShort("No more coming", Guid.NewGuid(), Now));

        request.Issue(new Dictionary<Guid, decimal> { [request.Lines.First().Id] = 1 }, Now);
        Assert.Throws<DomainException>(() => request.Reject("Reserved for production", Guid.NewGuid(), Now));

        request.CloseShort("Rest is reserved for production", Guid.NewGuid(), Now);
        Assert.Equal(StockRequestStatus.Closed, request.Status);
    }

    [Fact]
    public void Only_the_requester_can_cancel_and_only_while_open()
    {
        var request = Request(Item("A", 3));
        Assert.Throws<DomainException>(() => request.Cancel(Guid.NewGuid(), Now));

        request.Cancel(Requester, Now);
        Assert.Equal(StockRequestStatus.Cancelled, request.Status);
    }
}
