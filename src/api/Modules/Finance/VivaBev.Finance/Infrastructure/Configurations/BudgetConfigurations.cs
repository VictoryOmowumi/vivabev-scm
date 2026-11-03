using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VivaBev.Finance.Domain;

namespace VivaBev.Finance.Infrastructure.Configurations;

internal sealed class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("budgets");
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => new { b.CostCentreId, b.FiscalYear }).IsUnique();
        builder.Property(b => b.CurrencyCode).HasMaxLength(3).IsFixedLength();

        builder.Ignore(b => b.Committed);
        builder.Ignore(b => b.Consumed);
        builder.Ignore(b => b.Available);

        builder.HasMany(b => b.Commitments)
            .WithOne()
            .HasForeignKey(c => c.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(b => b.Commitments).HasField("_commitments").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class BudgetCommitmentConfiguration : IEntityTypeConfiguration<BudgetCommitment>
{
    public void Configure(EntityTypeBuilder<BudgetCommitment> builder)
    {
        builder.ToTable("budget_commitments");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.DocumentType).HasMaxLength(30);
        builder.Property(c => c.DocumentNumber).HasMaxLength(30);
        builder.HasIndex(c => new { c.DocumentType, c.DocumentId });
    }
}
