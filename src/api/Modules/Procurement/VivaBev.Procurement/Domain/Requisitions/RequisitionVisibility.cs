using VivaBev.SharedKernel.Security;

namespace VivaBev.Procurement.Domain.Requisitions;

/// <summary>Whose requisitions a list shows. Ordered narrowest to widest, so they can be compared.</summary>
public enum RequisitionScope
{
    Mine,
    Department,
    All
}

/// <summary>Turns a user's permissions into what they may see. Pure, so it is easy to test.</summary>
public static class RequisitionVisibility
{
    public static RequisitionScope Widest(IReadOnlyCollection<string> permissions) =>
        permissions.Contains(Permissions.Requisitions.ViewAll) ? RequisitionScope.All
        : permissions.Contains(Permissions.Requisitions.ViewDepartment) ? RequisitionScope.Department
        : RequisitionScope.Mine;

    public static bool Allows(IReadOnlyCollection<string> permissions, RequisitionScope scope) =>
        scope <= Widest(permissions);
}
