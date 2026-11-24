using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VivaBev.Procurement.Domain.PurchaseOrders;

namespace VivaBev.Procurement.Infrastructure.Configurations;

internal sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("purchase_orders");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Number).HasMaxLength(20);
        builder.HasIndex(p => p.Number).IsUnique();
        builder.Property(p => p.SourcingEventNumber).HasMaxLength(20);
        builder.Property(p => p.SupplierReference).HasMaxLength(50);
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.CurrencyCode).HasMaxLength(3).IsFixedLength();
        builder.Property(p => p.CancellationReason).HasMaxLength(500);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.SupplierId);

        // One live order per award: a cancelled draft frees the RFQ for a new one
        builder.HasIndex(p => p.SourcingEventId).IsUnique().HasFilter("[Status] <> 'Cancelled'");

        builder.HasMany(p => p.Lines).WithOne().HasForeignKey(l => l.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(p => p.Total);
        builder.Ignore(p => p.RequisitionIds);
    }
}

internal sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("purchase_order_lines");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.PurchaseOrderId, l.LineNo }).IsUnique();
        builder.HasIndex(l => l.RequisitionId);
        builder.Property(l => l.RequisitionNumber).HasMaxLength(20);
        builder.Property(l => l.ItemCode).HasMaxLength(30);
        builder.Property(l => l.Description).HasMaxLength(500);
        builder.Property(l => l.UomCode).HasMaxLength(10);
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Ignore(l => l.LineTotal);
    }
}
