using VivaBev.Finance.Domain;
using VivaBev.Finance.Domain.Invoices;
using VivaBev.SharedKernel.Domain;
using Xunit;

namespace VivaBev.Finance.Tests.Domain;

public class SupplierInvoiceTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now);
    private static readonly Guid LaptopLine = Guid.NewGuid();
    private static readonly Guid CostCentre = Guid.NewGuid();

    private static SupplierInvoice Invoice(decimal quantity, decimal unitPrice) =>
        new("INV-2027-00001", Guid.NewGuid(), "SDS-INV-9001", Guid.NewGuid(), "PO-2027-00001", Today, 30, 191_250m,
            Guid.NewGuid(), Now,
            [new InvoicedLine(LaptopLine, "Business laptop", "EA", quantity, unitPrice, 850_000m, CostCentre, 2027)]);

    private static Dictionary<Guid, LineFacts> Received(decimal received, decimal alreadyInvoiced = 0) =>
        new() { [LaptopLine] = new LineFacts(received, alreadyInvoiced) };

    [Fact]
    public void An_invoice_totals_its_lines_plus_vat_and_is_due_after_the_payment_terms()
    {
        var invoice = Invoice(3, 850_000m);

        Assert.Equal(2_550_000m, invoice.Subtotal);
        Assert.Equal(2_741_250m, invoice.Total);
        Assert.Equal(Today.AddDays(30), invoice.DueDate);
    }

    [Fact]
    public void Matching_order_price_and_received_quantity_approves_it_for_payment()
    {
        var invoice = Invoice(3, 850_000m);

        Assert.True(invoice.Match(Received(3), Now));
        Assert.Equal(InvoiceStatus.Matched, invoice.Status);
        Assert.True(invoice.ApprovedForPayment);
    }

    [Fact]
    public void A_price_within_two_percent_still_matches() =>
        Assert.True(Invoice(3, 867_000m).Match(Received(3), Now));

    [Fact]
    public void A_higher_price_is_held()
    {
        var invoice = Invoice(3, 900_000m);

        Assert.False(invoice.Match(Received(3), Now));
        Assert.Equal(InvoiceStatus.OnHold, invoice.Status);
        Assert.Contains("above the ordered", invoice.Lines.Single().Exception);
    }

    [Fact]
    public void Billing_more_than_was_received_is_held_until_the_rest_arrives()
    {
        var invoice = Invoice(3, 850_000m);
        Assert.False(invoice.Match(Received(2), Now));
        Assert.Contains("only 2 received", invoice.Lines.Single().Exception);

        Assert.True(invoice.Match(Received(3), Now)); // rematched after the last laptop arrived
    }

    [Fact]
    public void Quantities_already_billed_by_other_invoices_count()
    {
        var invoice = Invoice(2, 850_000m);
        Assert.False(invoice.Match(Received(3, alreadyInvoiced: 2), Now));
    }

    [Fact]
    public void A_held_invoice_can_be_approved_anyway_or_rejected_with_a_reason_then_paid()
    {
        var held = Invoice(3, 900_000m);
        held.Match(Received(3), Now);
        Assert.Throws<DomainException>(() => held.MarkPaid("TRF-001", Today, Now));

        held.ApproveAnyway("Supplier's price rise agreed by the Procurement manager", Guid.NewGuid(), Now);
        held.MarkPaid("TRF-001", Today, Now);
        Assert.Equal(InvoiceStatus.Paid, held.Status);

        var other = Invoice(3, 900_000m);
        other.Match(Received(3), Now);
        other.Reject("Wrong price; supplier to reissue", Guid.NewGuid(), Now);
        Assert.Equal(InvoiceStatus.Rejected, other.Status);
    }

    [Fact]
    public void Spend_moves_out_of_the_order_commitment()
    {
        var budget = new Budget(CostCentre, 2027, 20_000_000m);
        var orderId = Guid.NewGuid();
        budget.Commit("PurchaseOrder", orderId, "PO-2027-00001", 2_550_000m, Now);

        budget.Consume("PurchaseOrder", orderId, "SupplierInvoice", Guid.NewGuid(), "INV-2027-00001", 1_700_000m, Now);
        Assert.Equal(850_000m, budget.Committed);
        Assert.Equal(1_700_000m, budget.Consumed);

        // Over-invoicing beyond what was committed still counts as spend
        budget.Consume("PurchaseOrder", orderId, "SupplierInvoice", Guid.NewGuid(), "INV-2027-00002", 1_000_000m, Now);
        Assert.Equal(0m, budget.Committed);
        Assert.Equal(2_700_000m, budget.Consumed);
        Assert.Equal(17_300_000m, budget.Available);
    }
}
