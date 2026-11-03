using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VivaBev.Approvals.Domain;

namespace VivaBev.Approvals.Infrastructure.Configurations;

internal sealed class ApprovalPolicyStepConfiguration : IEntityTypeConfiguration<ApprovalPolicyStep>
{
    public void Configure(EntityTypeBuilder<ApprovalPolicyStep> builder)
    {
        builder.ToTable("policy_steps");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.DocumentType).HasMaxLength(30);
        builder.Property(p => p.Name).HasMaxLength(100);
        builder.Property(p => p.RoleName).HasMaxLength(50);
        builder.HasIndex(p => new { p.DocumentType, p.Sequence }).IsUnique();
    }
}

internal sealed class ApprovalRequestConfiguration : IEntityTypeConfiguration<ApprovalRequest>
{
    public void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.ToTable("requests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.DocumentType).HasMaxLength(30);
        builder.Property(r => r.DocumentNumber).HasMaxLength(30);
        builder.Property(r => r.Title).HasMaxLength(200);
        builder.Ignore(r => r.CurrentStep);

        // At most one pending request per document; finished ones stay as history.
        builder.HasIndex(r => new { r.DocumentType, r.DocumentId })
            .IsUnique()
            .HasFilter("[Status] = 'Pending'");

        builder.HasMany(r => r.Steps)
            .WithOne()
            .HasForeignKey(s => s.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.Steps).HasField("_steps").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ApprovalStepConfiguration : IEntityTypeConfiguration<ApprovalStep>
{
    public void Configure(EntityTypeBuilder<ApprovalStep> builder)
    {
        builder.ToTable("request_steps");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(100);
        builder.Property(s => s.Comment).HasMaxLength(500);
        builder.HasIndex(s => new { s.RequestId, s.Sequence }).IsUnique();
        builder.HasIndex(s => new { s.ApproverId, s.Decision });
    }
}
