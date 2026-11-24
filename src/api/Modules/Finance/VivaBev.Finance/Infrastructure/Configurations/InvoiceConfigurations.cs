using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VivaBev.Finance.Domain.Invoices;

namespace VivaBev.Finance.Infrastructure.Configurations;

internal sealed class SupplierInvoiceConfiguration : IEntityTypeConfiguration<SupplierInvoice>
{
    public void Configure(EntityTypeBuilder<SupplierInvoice> builder)
    {
        builder.ToTable("supplier_invoices");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Number).HasMaxLength(20);
        builder.HasIndex(i => i.Number).IsUnique();
        builder.Property(i => i.SupplierInvoiceNumber).HasMaxLength(50);
        // A supplier's invoice number is entered once (a rejected one can be entered again, corrected)
        builder.HasIndex(i => new { i.SupplierId, i.SupplierInvoiceNumber }).IsUnique().HasFilter("[Status] <> 'Rejected'");
        builder.Property(i => i.PurchaseOrderNumber).HasMaxLength(20);
        builder.HasIndex(i => i.PurchaseOrderId);
        builder.HasIndex(i => i.Status);
        builder.Property(i => i.CurrencyCode).HasMaxLength(3).IsFixedLength();
        builder.Property(i => i.DecisionReason).HasMaxLength(500);
        builder.Property(i => i.PaymentReference).HasMaxLength(50);

        builder.HasMany(i => i.Lines).WithOne().HasForeignKey(l => l.SupplierInvoiceId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(i => i.Subtotal);
        builder.Ignore(i => i.Total);
        builder.Ignore(i => i.ApprovedForPayment);
    }
}

internal sealed class SupplierInvoiceLineConfiguration : IEntityTypeConfiguration<SupplierInvoiceLine>
{
    public void Configure(EntityTypeBuilder<SupplierInvoiceLine> builder)
    {
        builder.ToTable("supplier_invoice_lines");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.SupplierInvoiceId, l.LineNo }).IsUnique();
        builder.HasIndex(l => l.PurchaseOrderLineId);
        builder.Property(l => l.Description).HasMaxLength(500);
        builder.Property(l => l.UomCode).HasMaxLength(10);
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.ReceivedAtMatch).HasPrecision(18, 4);
        builder.Property(l => l.InvoicedBeforeAtMatch).HasPrecision(18, 4);
        builder.Property(l => l.Exception).HasMaxLength(500);
        builder.Ignore(l => l.LineTotal);
    }
}
