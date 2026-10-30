using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VivaBev.Catalogue.Domain;

namespace VivaBev.Catalogue.Infrastructure.Configurations;

internal sealed class UomConversionConfiguration : IEntityTypeConfiguration<UomConversion>
{
    public void Configure(EntityTypeBuilder<UomConversion> builder)
    {
        builder.ToTable("uom_conversions");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.FromUomCode).HasMaxLength(UnitOfMeasure.CodeMaxLength);
        builder.Property(c => c.ToUomCode).HasMaxLength(UnitOfMeasure.CodeMaxLength);
        builder.Property(c => c.Factor).HasPrecision(18, 6);
        builder.HasIndex(c => new { c.ItemId, c.FromUomCode, c.ToUomCode }).IsUnique().HasFilter(null);

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(c => c.FromUomCode)
            .HasPrincipalKey(u => u.Code)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(c => c.ToUomCode)
            .HasPrincipalKey(u => u.Code)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Item>()
            .WithMany()
            .HasForeignKey(c => c.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}