using Microsoft.EntityFrameworkCore;
using VivaBev.Approvals.Domain;

namespace VivaBev.Approvals.Infrastructure;

internal static class ApprovalPolicySeeder
{
    public static async Task SeedAsync(ApprovalsDbContext db, CancellationToken ct = default)
    {
        ApprovalPolicyStep[] defaults =
        [
            new("Requisition", 1, "Line manager", 0m, ApproverRule.RequesterManager),
            new("Requisition", 2, "Head of department", 5_000_000m, ApproverRule.DepartmentRole, "DepartmentManager"),
            new("Requisition", 3, "Managing Director", 25_000_000m, ApproverRule.Role, "Executive"),
            new("Requisition", 4, "Finance controller", 0m, ApproverRule.Role, "FinanceOfficer", overBudgetOnly: true),
        ];

        var existing = await db.PolicySteps
            .Select(p => new { p.DocumentType, p.Sequence })
            .ToListAsync(ct);

        foreach (var step in defaults)
            if (!existing.Any(e => e.DocumentType == step.DocumentType && e.Sequence == step.Sequence))
                db.PolicySteps.Add(step);

        await db.SaveChangesAsync(ct);
    }
}
