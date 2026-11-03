using Microsoft.EntityFrameworkCore;
using VivaBev.Approvals.Domain;

namespace VivaBev.Approvals.Infrastructure;

internal static class ApprovalPolicySeeder
{
    public static async Task SeedAsync(ApprovalsDbContext db, CancellationToken ct = default)
    {
        if (await db.PolicySteps.AnyAsync(ct))
            return;

        db.PolicySteps.AddRange(
            new ApprovalPolicyStep("Requisition", 1, "Line manager", 0m, ApproverRule.RequesterManager),
            new ApprovalPolicyStep("Requisition", 2, "Head of department", 5_000_000m,
                ApproverRule.DepartmentRole, "DepartmentManager"),
            new ApprovalPolicyStep("Requisition", 3, "Managing Director", 25_000_000m,
                ApproverRule.Role, "Executive"));

        await db.SaveChangesAsync(ct);
    }
}
