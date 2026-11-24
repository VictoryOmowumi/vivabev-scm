using VivaBev.Inventory.Domain;
using VivaBev.SharedKernel.Domain;
using Xunit;

namespace VivaBev.Inventory.Tests.Domain;

public class GoodsReceiptTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);

    private static ReceiptLine Line(decimal accepted, decimal rejected = 0, string? reason = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "IT-LAPTOP", "Business laptop", "EA", 850_000m, accepted, rejected, reason);

    private static GoodsReceipt Receive(params ReceiptLine[] lines) =>
        new("GRN-2027-00001", Guid.NewGuid(), "PO-2027-00001", Guid.NewGuid(), Guid.NewGuid(), " DN-4471 ", null,
            Guid.NewGuid(), Now, lines);

    [Fact]
    public void A_receipt_keeps_only_the_lines_that_were_counted()
    {
        var receipt = Receive(Line(2), Line(0));

        Assert.Single(receipt.Lines);
        Assert.Equal("DN-4471", receipt.DeliveryNoteNumber);
        Assert.Equal(1_700_000m, receipt.AcceptedValue);
    }

    [Fact]
    public void Nothing_counted_is_refused() =>
        Assert.Throws<DomainException>(() => Receive(Line(0)));

    [Fact]
    public void Rejected_goods_need_a_reason()
    {
        Assert.Throws<DomainException>(() => Receive(Line(2, rejected: 1)));

        var receipt = Receive(Line(2, rejected: 1, reason: "Cracked screen"));
        Assert.True(receipt.HasRejections);
        Assert.Equal(1_700_000m, receipt.AcceptedValue); // rejected goods aren't valued
    }

    [Fact]
    public void A_stock_movement_needs_a_quantity() =>
        Assert.Throws<DomainException>(() => new StockMovement(Guid.NewGuid(), Guid.NewGuid(), "IT-LAPTOP", "Laptop",
            "EA", 0, 850_000m, StockMovement.ReceiptDocumentType, Guid.NewGuid(), "GRN-2027-00001", Now));
}
