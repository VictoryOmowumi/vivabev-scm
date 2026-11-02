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
        public const string Create = "requisitions.create";     // raise and edit your own
        public const string ViewAll = "requisitions.view-all";  // see everyone's
    }

    public static readonly IReadOnlyList<string> All =
    [
        Catalogue.View, Catalogue.Manage,
        Suppliers.View, Suppliers.Manage, Suppliers.Approve,
        Organisation.View, Organisation.Manage,
        Requisitions.Create, Requisitions.ViewAll,
    ];
}
