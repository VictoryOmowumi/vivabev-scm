using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VivaBev.Inventory.Domain.Stock;

namespace VivaBev.Inventory.Infrastructure.Configurations;

internal sealed class StockRequestConfiguration : IEntityTypeConfiguration<StockRequest>
{
    public void Configure(EntityTypeBuilder<StockRequest> builder)
    {
        builder.ToTable("stock_requests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Number).HasMaxLength(20);
        builder.HasIndex(r => r.Number).IsUnique();
        builder.Property(r => r.Purpose).HasMaxLength(500);
        builder.Property(r => r.DecisionReason).HasMaxLength(500);
        builder.HasIndex(r => new { r.SiteId, r.Status });
        builder.HasIndex(r => r.RequestedBy);

        builder.HasMany(r => r.Lines).WithOne().HasForeignKey(l => l.StockRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(r => r.IsOutstanding);
    }
}

internal sealed class StockRequestLineConfiguration : IEntityTypeConfiguration<StockRequestLine>
{
    public void Configure(EntityTypeBuilder<StockRequestLine> builder)
    {
        builder.ToTable("stock_request_lines");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.StockRequestId, l.LineNo }).IsUnique();
        builder.Property(l => l.ItemCode).HasMaxLength(30);
        builder.Property(l => l.Description).HasMaxLength(500);
        builder.Property(l => l.UomCode).HasMaxLength(10);
        builder.Property(l => l.QuantityRequested).HasPrecision(18, 4);
        builder.Property(l => l.QuantityIssued).HasPrecision(18, 4);
        builder.Ignore(l => l.Outstanding);
    }
}

internal sealed class StockCountConfiguration : IEntityTypeConfiguration<StockCount>
{
    public void Configure(EntityTypeBuilder<StockCount> builder)
    {
        builder.ToTable("stock_counts");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Number).HasMaxLength(20);
        builder.HasIndex(c => c.Number).IsUnique();
        builder.Property(c => c.Notes).HasMaxLength(1000);
        // One open count per site at a time
        builder.HasIndex(c => c.SiteId).IsUnique().HasFilter("[Status] = 'Open'");

        builder.HasMany(c => c.Lines).WithOne().HasForeignKey(l => l.StockCountId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class StockCountLineConfiguration : IEntityTypeConfiguration<StockCountLine>
{
    public void Configure(EntityTypeBuilder<StockCountLine> builder)
    {
        builder.ToTable("stock_count_lines");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.StockCountId, l.LineNo }).IsUnique();
        builder.Property(l => l.ItemCode).HasMaxLength(30);
        builder.Property(l => l.Description).HasMaxLength(500);
        builder.Property(l => l.UomCode).HasMaxLength(10);
        builder.Property(l => l.Expected).HasPrecision(18, 4);
        builder.Property(l => l.Counted).HasPrecision(18, 4);
        builder.Ignore(l => l.Variance);
        builder.Ignore(l => l.VarianceValue);
    }
}
