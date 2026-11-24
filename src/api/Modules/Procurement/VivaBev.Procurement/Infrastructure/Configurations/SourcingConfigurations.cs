using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VivaBev.Procurement.Domain.Sourcing;

namespace VivaBev.Procurement.Infrastructure.Configurations;

internal sealed class SourcingEventConfiguration : IEntityTypeConfiguration<SourcingEvent>
{
    public void Configure(EntityTypeBuilder<SourcingEvent> builder)
    {
        builder.ToTable("sourcing_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Number).HasMaxLength(20);
        builder.HasIndex(e => e.Number).IsUnique();
        builder.Property(e => e.CurrencyCode).HasMaxLength(3).IsFixedLength();
        builder.Property(e => e.CancellationReason).HasMaxLength(500);
        builder.HasIndex(e => e.Status);

        builder.HasMany(e => e.Lines).WithOne().HasForeignKey(l => l.SourcingEventId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.Invitations).WithOne().HasForeignKey(i => i.SourcingEventId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Invitations).HasField("_invitations").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.Quotations).WithOne().HasForeignKey(q => q.SourcingEventId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Quotations).HasField("_quotations").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(e => e.EstimatedTotal);
        builder.Ignore(e => e.RequisitionIds);
        builder.Ignore(e => e.NeedsCompetition);
    }
}

internal sealed class SourcingEventLineConfiguration : IEntityTypeConfiguration<SourcingEventLine>
{
    public void Configure(EntityTypeBuilder<SourcingEventLine> builder)
    {
        builder.ToTable("sourcing_event_lines");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.SourcingEventId, l.LineNo }).IsUnique();
        builder.HasIndex(l => l.RequisitionLineId);
        builder.Property(l => l.RequisitionNumber).HasMaxLength(20);
        builder.Property(l => l.ItemCode).HasMaxLength(30);
        builder.Property(l => l.Description).HasMaxLength(500);
        builder.Property(l => l.UomCode).HasMaxLength(10);
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Ignore(l => l.EstimatedTotal);
    }
}

internal sealed class SourcingInvitationConfiguration : IEntityTypeConfiguration<SourcingInvitation>
{
    public void Configure(EntityTypeBuilder<SourcingInvitation> builder)
    {
        builder.ToTable("sourcing_invitations");
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => new { i.SourcingEventId, i.SupplierId }).IsUnique();
    }
}

internal sealed class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> builder)
    {
        builder.ToTable("quotations");
        builder.HasKey(q => q.Id);
        builder.HasIndex(q => new { q.SourcingEventId, q.SupplierId });
        builder.Property(q => q.SupplierReference).HasMaxLength(50);
        builder.Property(q => q.CurrencyCode).HasMaxLength(3).IsFixedLength();
        builder.Property(q => q.WithdrawalReason).HasMaxLength(500);

        builder.HasMany(q => q.Lines).WithOne().HasForeignKey(l => l.QuotationId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(q => q.Lines).HasField("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class QuotationLineConfiguration : IEntityTypeConfiguration<QuotationLine>
{
    public void Configure(EntityTypeBuilder<QuotationLine> builder)
    {
        builder.ToTable("quotation_lines");
        builder.HasKey(l => l.Id);
        builder.HasIndex(l => new { l.QuotationId, l.SourcingEventLineId }).IsUnique();
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.Notes).HasMaxLength(500);
    }
}
