namespace VivaBev.Suppliers.Domain;

public enum SupplierStatus
{
    Prospect,
    PendingApproval,
    Approved,
    Suspended,
    Blocked
}

public enum CategoryApprovalStatus
{
    Pending,
    Approved,
    Revoked
}

public enum AddressType
{
    Billing,
    Shipping
}