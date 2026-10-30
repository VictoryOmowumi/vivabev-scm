using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VivaBev.Catalogue.Domain;

namespace VivaBev.Catalogue.Infrastructure.Configurations;

internal sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Code).HasMaxLength(Item.CodeMaxLength);
        builder.HasIndex(i => i.Code).IsUnique();
        builder.Property(i => i.Name).HasMaxLength(200);
        builder.Property(i => i.Description).HasMaxLength(1000);
        builder.Property(i => i.BaseUomCode).HasMaxLength(UnitOfMeasure.CodeMaxLength);
        builder.HasIndex(i => i.CategoryId);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(i => i.BaseUomCode)
            .HasPrincipalKey(u => u.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}