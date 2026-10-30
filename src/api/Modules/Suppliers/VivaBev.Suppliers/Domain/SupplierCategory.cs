using VivaBev.SharedKernel.Domain;

namespace VivaBev.Suppliers.Domain;

public sealed class SupplierCategory : AuditableEntity
{
    private SupplierCategory() { }

    internal SupplierCategory(Guid supplierId, Guid categoryId)
    {
        SupplierId = supplierId;
        CategoryId = categoryId;
        Status = CategoryApprovalStatus.Pending;
    }

    public Guid SupplierId { get; private set; }
    public Guid CategoryId { get; private set; }
    public CategoryApprovalStatus Status { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }

    internal void Approve(Guid? approvedBy, DateTime approvedAt)
    {
        if (Status != CategoryApprovalStatus.Pending)
            throw new DomainException($"Only a pending category can be approved; this one is {Status}.");

        Status = CategoryApprovalStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = approvedAt;
    }

    internal void Revoke()
    {
        if (Status == CategoryApprovalStatus.Revoked)
            throw new DomainException("This category is already revoked.");

        Status = CategoryApprovalStatus.Revoked;
    }

    internal void Reopen()
    {
        Status = CategoryApprovalStatus.Pending;
        ApprovedBy = null;
        ApprovedAt = null;
    }
}