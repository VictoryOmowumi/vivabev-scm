using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VivaBev.Inventory.Domain;

namespace VivaBev.Inventory.Infrastructure.Configurations;

internal sealed class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
{
    public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
    {
        builder.ToTable("goods_receipts");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Number).HasMaxLength(20);
        builder.HasIndex(r => r.Number).IsUnique();
        builder.Property(r => r.PurchaseOrderNumber).HasMaxLength(20);
        builder.HasIndex(r => r.PurchaseOrderId);
        builder.Property(r => r.DeliveryNoteNumber).HasMaxLength(50);
        builder.Property(r => r.Notes).HasMaxLength(1000);

        builder.HasMany(r => r.Lines).WithOne().HasForeignKey(l => l.GoodsReceiptId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(r => r.AcceptedValue);
        builder.Ignore(r => r.HasRejections);
    }
}

internal sealed class GoodsReceiptLineConfiguration : IEntityTypeConfiguration<GoodsReceiptLine>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptLine> builder)
    {
        builder.ToTable("goods_receipt_lines");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.GoodsReceiptId, l.LineNo }).IsUnique();
        builder.Property(l => l.ItemCode).HasMaxLength(30);
        builder.Property(l => l.Description).HasMaxLength(500);
        builder.Property(l => l.UomCode).HasMaxLength(10);
        builder.Property(l => l.QuantityAccepted).HasPrecision(18, 4);
        builder.Property(l => l.QuantityRejected).HasPrecision(18, 4);
        builder.Property(l => l.RejectionReason).HasMaxLength(500);
        builder.Ignore(l => l.AcceptedValue);
    }
}

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.SiteId, m.ItemId });
        builder.HasIndex(m => new { m.DocumentType, m.DocumentId });
        builder.Property(m => m.ItemCode).HasMaxLength(30);
        builder.Property(m => m.Description).HasMaxLength(500);
        builder.Property(m => m.UomCode).HasMaxLength(10);
        builder.Property(m => m.Quantity).HasPrecision(18, 4);
        builder.Property(m => m.DocumentType).HasMaxLength(30);
        builder.Property(m => m.DocumentNumber).HasMaxLength(20);
    }
}
