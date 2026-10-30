using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VivaBev.Suppliers.Domain;

namespace VivaBev.Suppliers.Infrastructure.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code).HasMaxLength(Supplier.CodeMaxLength);
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.LegalName).HasMaxLength(200);
        builder.Property(s => s.TradingName).HasMaxLength(200);
        builder.Property(s => s.TaxId).HasMaxLength(20);
        builder.HasIndex(s => s.TaxId).IsUnique();
        builder.Property(s => s.CountryCode).HasMaxLength(2).IsFixedLength();
        builder.Property(s => s.DefaultCurrencyCode).HasMaxLength(3).IsFixedLength();
        builder.Property(s => s.StatusReason).HasMaxLength(500);
        builder.HasIndex(s => s.Status);

        builder.HasMany(s => s.Contacts)
            .WithOne()
            .HasForeignKey(c => c.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Contacts).HasField("_contacts").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(s => s.Addresses)
            .WithOne()
            .HasForeignKey(a => a.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Addresses).HasField("_addresses").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(s => s.Categories)
            .WithOne()
            .HasForeignKey(c => c.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Categories).HasField("_categories").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
