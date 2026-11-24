using VivaBev.Inventory.Domain.Stock;
using VivaBev.SharedKernel.Domain;
using Xunit;

namespace VivaBev.Inventory.Tests.Domain;

public class StockCountTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);
    private static readonly ExpectedStock Laptops = new(Guid.NewGuid(), "IT-LAPTOP", "Business laptop", "EA", 5, 850_000m);
    private static readonly ExpectedStock Toner = new(Guid.NewGuid(), "OF-TONER", "Toner cartridge", "EA", 12, 45_000m);

    private static StockCount Start() => new("SC-2027-00001", Guid.NewGuid(), Guid.NewGuid(), Now, [Toner, Laptops]);

    private static Dictionary<Guid, decimal?> Counts(StockCount count, decimal laptops, decimal toner) => new()
    {
        [count.Lines.Single(l => l.ItemCode == "IT-LAPTOP").Id] = laptops,
        [count.Lines.Single(l => l.ItemCode == "OF-TONER").Id] = toner,
    };

    [Fact]
    public void A_count_lists_the_ledger_in_item_order()
    {
        var count = Start();
        Assert.Equal(new[] { "IT-LAPTOP", "OF-TONER" }, count.Lines.Select(l => l.ItemCode));
        Assert.All(count.Lines, l => Assert.Null(l.Counted));
    }

    [Fact]
    public void Every_line_needs_a_count_before_posting()
    {
        var count = Start();
        count.RecordCounts(new Dictionary<Guid, decimal?> { [count.Lines.First().Id] = 5 });

        Assert.Throws<DomainException>(() => count.Post(new Dictionary<Guid, decimal>(), "x", Guid.NewGuid(), Now));
    }

    [Fact]
    public void Differences_need_a_note_and_are_returned_for_adjustment()
    {
        var count = Start();
        count.RecordCounts(Counts(count, laptops: 4, toner: 12));
        var onHand = new Dictionary<Guid, decimal> { [Laptops.ItemId] = 5, [Toner.ItemId] = 12 };

        Assert.Throws<DomainException>(() => count.Post(onHand, null, Guid.NewGuid(), Now));

        var differences = count.Post(onHand, "One laptop missing from cage B", Guid.NewGuid(), Now);
        var line = Assert.Single(differences);
        Assert.Equal(-1, line.Variance);
        Assert.Equal(-850_000m, line.VarianceValue);
        Assert.Equal(StockCountStatus.Posted, count.Status);
    }

    [Fact]
    public void Posting_compares_with_the_ledger_now_not_at_the_start()
    {
        var count = Start();
        count.RecordCounts(Counts(count, laptops: 3, toner: 12));

        // Two laptops were issued while counting: the ledger now says 3, so there's no difference
        var differences = count.Post(new Dictionary<Guid, decimal> { [Laptops.ItemId] = 3, [Toner.ItemId] = 12 },
            null, Guid.NewGuid(), Now);

        Assert.Empty(differences);
    }

    [Fact]
    public void A_posted_count_cannot_change()
    {
        var count = Start();
        count.RecordCounts(Counts(count, laptops: 5, toner: 12));
        count.Post(new Dictionary<Guid, decimal> { [Laptops.ItemId] = 5, [Toner.ItemId] = 12 }, null, Guid.NewGuid(), Now);

        Assert.Throws<DomainException>(() => count.RecordCounts(Counts(count, 1, 1)));
        Assert.Throws<DomainException>(count.Cancel);
    }
}
