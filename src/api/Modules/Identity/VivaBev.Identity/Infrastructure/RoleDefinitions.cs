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
    public const string Executive = "Executive";
    public const string Administrator = "Administrator";

    private static readonly string[] Basic =
        [Permissions.Catalogue.View, Permissions.Suppliers.View, Permissions.Organisation.View];

    public static readonly IReadOnlyList<(string Name, string Description, string[] Grants)> All =
    [
        (Requester, "Raises requests for goods and services",
            [.. Basic, Permissions.Requisitions.Create, Permissions.Inventory.RequestStock]),
        (DepartmentManager, "Reviews and approves departmental requests",
            [.. Basic, Permissions.Requisitions.Create, Permissions.Requisitions.ViewDepartment,
             Permissions.Finance.ViewDepartmentBudgets, Permissions.Inventory.RequestStock]),
        (ProcurementOfficer, "Sources, maintains suppliers and the catalogue",
            [.. Basic, Permissions.Suppliers.Manage, Permissions.Catalogue.Manage, Permissions.Requisitions.ViewAll,
             Permissions.Sourcing.View, Permissions.Sourcing.Manage,
             Permissions.PurchaseOrders.View, Permissions.PurchaseOrders.Manage, Permissions.Inventory.View,
             Permissions.Finance.ViewInvoices, Permissions.Inventory.RequestStock]),
        (ProcurementManager, "Procurement officer who also approves suppliers",
            [.. Basic, Permissions.Suppliers.Manage, Permissions.Catalogue.Manage, Permissions.Suppliers.Approve,
             Permissions.Requisitions.ViewAll, Permissions.Sourcing.View, Permissions.Sourcing.Manage,
             Permissions.PurchaseOrders.View, Permissions.PurchaseOrders.Manage, Permissions.Inventory.View,
             Permissions.Finance.ViewInvoices, Permissions.Inventory.RequestStock]),
        (WarehouseOfficer, "Receives, stores and issues stock",
            [.. Basic, Permissions.PurchaseOrders.View, Permissions.Inventory.View, Permissions.Inventory.Receive,
             Permissions.Inventory.IssueStock, Permissions.Inventory.CountStock]),
        (FinanceOfficer, "Budgets, invoices and matching",
            [.. Basic, Permissions.Requisitions.ViewAll, Permissions.Finance.ViewBudgets, Permissions.Finance.ManageBudgets,
             Permissions.Sourcing.View, Permissions.PurchaseOrders.View, Permissions.Inventory.View,
             Permissions.Finance.ViewInvoices, Permissions.Finance.ManageInvoices]),
        (Executive, "Managing Director; final approver for large spend",
            [.. Basic, Permissions.Requisitions.ViewAll, Permissions.Finance.ViewBudgets, Permissions.Sourcing.View,
             Permissions.PurchaseOrders.View, Permissions.Inventory.View, Permissions.Finance.ViewInvoices]),
        (Administrator, "Configures the system; holds every permission", [.. Permissions.All]),
    ];
}
