namespace VivaBev.SharedKernel.Security;

public static class Permissions
{
    /// <summary>The claim type that carries a permission on the signed-in user.</summary>
    public const string ClaimType = "permission";

    public static class Catalogue
    {
        public const string View = "catalogue.view";
        public const string Manage = "catalogue.manage";
    }

    public static class Suppliers
    {
        public const string View = "suppliers.view";
        public const string Manage = "suppliers.manage";
        public const string Approve = "suppliers.approve";
    }

    public static class Organisation
    {
        public const string View = "organisation.view";
        public const string Manage = "organisation.manage";
    }

    public static class Requisitions
    {
        public const string Create = "requisitions.create";                 // raise and edit your own
        public const string ViewDepartment = "requisitions.view-department"; // see your department's
        public const string ViewAll = "requisitions.view-all";              // see everyone's
    }

    public static class Sourcing
    {
        public const string View = "sourcing.view";     // see RFQs and quotations
        public const string Manage = "sourcing.manage"; // run RFQs: invite, record quotations, close
    }

    public static class PurchaseOrders
    {
        public const string View = "purchase-orders.view";     // see purchase orders
        public const string Manage = "purchase-orders.manage"; // raise, issue and cancel them
    }

    public static class Inventory
    {
        public const string View = "inventory.view";       // see receipts and stock
        public const string Receive = "inventory.receive"; // record goods received against purchase orders
    }

    public static class Finance
    {
        public const string ViewBudgets = "finance.budgets.view";                      // every cost centre
        public const string ViewDepartmentBudgets = "finance.budgets.view-department"; // your department's, read-only
        public const string ManageBudgets = "finance.budgets.manage";
    }

    public static readonly IReadOnlyList<string> All =
    [
        Catalogue.View, Catalogue.Manage,
        Suppliers.View, Suppliers.Manage, Suppliers.Approve,
        Organisation.View, Organisation.Manage,
        Requisitions.Create, Requisitions.ViewDepartment, Requisitions.ViewAll,
        Sourcing.View, Sourcing.Manage,
        PurchaseOrders.View, PurchaseOrders.Manage,
        Inventory.View, Inventory.Receive,
        Finance.ViewBudgets, Finance.ViewDepartmentBudgets, Finance.ManageBudgets,
    ];
}
