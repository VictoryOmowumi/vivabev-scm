using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VivaBev.Procurement.Domain.Requisitions;

namespace VivaBev.Procurement.Infrastructure.Configurations;

internal sealed class RequisitionConfiguration : IEntityTypeConfiguration<Requisition>
{
    public void Configure(EntityTypeBuilder<Requisition> builder)
    {
        builder.ToTable("requisitions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Number).HasMaxLength(20);
        builder.HasIndex(r => r.Number).IsUnique();
        builder.Property(r => r.Title).HasMaxLength(200);
        builder.Property(r => r.Justification).HasMaxLength(1000);
        builder.Property(r => r.CurrencyCode).HasMaxLength(3).IsFixedLength();
        builder.Property(r => r.CancellationReason).HasMaxLength(500);
        builder.HasIndex(r => r.RequesterId);
        builder.HasIndex(r => r.Status);

        builder.HasMany(r => r.Lines)
            .WithOne()
            .HasForeignKey(l => l.RequisitionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class RequisitionLineConfiguration : IEntityTypeConfiguration<RequisitionLine>
{
    public void Configure(EntityTypeBuilder<RequisitionLine> builder)
    {
        builder.ToTable("requisition_lines");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.RequisitionId, l.LineNo }).IsUnique();
        builder.Property(l => l.ItemCode).HasMaxLength(30);
        builder.Property(l => l.Description).HasMaxLength(500);
        builder.Property(l => l.UomCode).HasMaxLength(10);
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
    }
}
