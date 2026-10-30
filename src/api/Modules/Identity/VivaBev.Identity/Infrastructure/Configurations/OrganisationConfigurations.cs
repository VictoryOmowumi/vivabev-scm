using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VivaBev.Identity.Domain.Organisation;

namespace VivaBev.Identity.Infrastructure.Configurations;

internal sealed class BusinessUnitConfiguration : IEntityTypeConfiguration<BusinessUnit>
{
    public void Configure(EntityTypeBuilder<BusinessUnit> builder)
    {
        builder.ToTable("business_units");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Code).HasMaxLength(BusinessUnit.CodeMaxLength);
        builder.HasIndex(b => b.Code).IsUnique();
        builder.Property(b => b.Name).HasMaxLength(100);
    }
}

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Code).HasMaxLength(Department.CodeMaxLength);
        builder.HasIndex(d => d.Code).IsUnique();
        builder.Property(d => d.Name).HasMaxLength(100);

        builder.HasOne<BusinessUnit>()
            .WithMany()
            .HasForeignKey(d => d.BusinessUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.CostCentres)
            .WithOne()
            .HasForeignKey(c => c.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(d => d.CostCentres).HasField("_costCentres").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class CostCentreConfiguration : IEntityTypeConfiguration<CostCentre>
{
    public void Configure(EntityTypeBuilder<CostCentre> builder)
    {
        builder.ToTable("cost_centres");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).HasMaxLength(CostCentre.CodeMaxLength);
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.Name).HasMaxLength(100);
    }
}

internal sealed class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.ToTable("sites");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).HasMaxLength(Site.CodeMaxLength);
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.Name).HasMaxLength(100);
        builder.Property(s => s.City).HasMaxLength(100);
        builder.Property(s => s.State).HasMaxLength(100);
        builder.HasIndex(s => s.SiteType);
        builder.Ignore(s => s.CanHoldStock);

        builder.HasOne<BusinessUnit>()
            .WithMany()
            .HasForeignKey(s => s.BusinessUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
