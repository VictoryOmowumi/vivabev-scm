using VivaBev.SharedKernel.Security;

namespace VivaBev.Identity.Infrastructure;

internal static class RoleDefinitions
{
    public const string Requester = "Requester";
    public const string DepartmentManager = "DepartmentManager";
    public const string ProcurementOfficer = "ProcurementOfficer";
    public const string ProcurementManager = "ProcurementManager";
    public const string WarehouseOfficer = "WarehouseOfficer";
    public const string FinanceOfficer = "FinanceOfficer";
    public const string Administrator = "Administrator";

    private static readonly string[] Basic =
        [Permissions.Catalogue.View, Permissions.Suppliers.View, Permissions.Organisation.View];

    public static readonly IReadOnlyList<(string Name, string Description, string[] Grants)> All =
    [
        (Requester, "Raises requests for goods and services", Basic),
        (DepartmentManager, "Reviews and approves departmental requests", Basic),
        (ProcurementOfficer, "Sources, maintains suppliers and the catalogue",
            [.. Basic, Permissions.Suppliers.Manage, Permissions.Catalogue.Manage]),
        (ProcurementManager, "Procurement officer who also approves suppliers",
            [.. Basic, Permissions.Suppliers.Manage, Permissions.Catalogue.Manage, Permissions.Suppliers.Approve]),
        (WarehouseOfficer, "Receives, stores and issues stock", Basic),
        (FinanceOfficer, "Budgets, invoices and matching", Basic),
        (Administrator, "Configures the system; holds every permission", [.. Permissions.All]),
    ];
}
