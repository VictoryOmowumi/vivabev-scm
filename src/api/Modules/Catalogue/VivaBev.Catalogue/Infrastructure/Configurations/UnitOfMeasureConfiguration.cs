using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VivaBev.Catalogue.Domain;

namespace VivaBev.Catalogue.Infrastructure.Configurations;

internal sealed class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("units_of_measure");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Code).HasMaxLength(UnitOfMeasure.CodeMaxLength);
        builder.HasAlternateKey(u => u.Code);
        builder.Property(u => u.Name).HasMaxLength(50);
    }
}