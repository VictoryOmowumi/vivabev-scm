using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VivaBev.Infrastructure.Persistence;

namespace VivaBev.Numbering;

public sealed class NumberingDbContext(DbContextOptions<NumberingDbContext> options) : ModuleDbContext(options)
{
    public const string SchemaName = "numbering";

    protected override string Schema => SchemaName;
}

internal sealed class DocumentSequenceConfiguration : IEntityTypeConfiguration<DocumentSequence>
{
    public void Configure(EntityTypeBuilder<DocumentSequence> builder)
    {
        builder.ToTable("document_sequences");
        builder.HasKey(s => new { s.Prefix, s.FiscalYear });
        builder.Property(s => s.Prefix).HasMaxLength(10);
    }
}
