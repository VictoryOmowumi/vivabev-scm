using Microsoft.EntityFrameworkCore;
using VivaBev.Approvals.Domain;
using VivaBev.Infrastructure.Persistence;

namespace VivaBev.Approvals.Infrastructure;

public sealed class ApprovalsDbContext(DbContextOptions<ApprovalsDbContext> options) : ModuleDbContext(options)
{
    public const string SchemaName = "approvals";

    protected override string Schema => SchemaName;

    public DbSet<ApprovalPolicyStep> PolicySteps => Set<ApprovalPolicyStep>();
    public DbSet<ApprovalRequest> Requests => Set<ApprovalRequest>();
}
