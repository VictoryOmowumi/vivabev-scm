using VivaBev.SharedKernel.Domain;
using VivaBev.Suppliers.Domain;
using Xunit;

namespace VivaBev.Suppliers.Tests.Domain;

public class SupplierTests
{
    private static readonly Guid Packaging = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);

    private static Supplier NewProspect()
    {
        var supplier = new Supplier("sup-9001", "Test Supplies Ltd", "tin-9001");
        supplier.AddContact("Ada Test", "ada@test.example");
        supplier.RequestCategory(Packaging);
        return supplier;
    }

    private static Supplier NewApproved()
    {
        var supplier = NewProspect();
        supplier.SubmitForApproval();
        supplier.Approve(approvedBy: null, Now);
        return supplier;
    }

    [Fact]
    public void A_new_supplier_is_a_prospect_with_normalised_codes()
    {
        var supplier = new Supplier(" sup-9001 ", "Test Supplies Ltd", "tin-9001");

        Assert.Equal(SupplierStatus.Prospect, supplier.Status);
        Assert.Equal("SUP-9001", supplier.Code);
        Assert.Equal("TIN-9001", supplier.TaxId);
    }

    [Fact]
    public void The_first_contact_becomes_primary_and_a_new_primary_replaces_it()
    {
        var supplier = new Supplier("SUP-9001", "Test Supplies Ltd", "TIN-9001");
        var first = supplier.AddContact("First", "first@test.example");
        var second = supplier.AddContact("Second", "second@test.example", isPrimary: true);

        Assert.False(first.IsPrimary);
        Assert.True(second.IsPrimary);
    }

    [Fact]
    public void Cannot_submit_without_a_category()
    {
        var supplier = new Supplier("SUP-9001", "Test Supplies Ltd", "TIN-9001");
        supplier.AddContact("Ada Test", "ada@test.example");

        Assert.Throws<DomainException>(() => supplier.SubmitForApproval());
    }

    [Fact]
    public void Cannot_approve_a_prospect_that_was_not_submitted()
    {
        var supplier = NewProspect();

        Assert.Throws<DomainException>(() => supplier.Approve(null, Now));
    }

    [Fact]
    public void Approving_a_supplier_approves_its_pending_categories()
    {
        var supplier = NewApproved();

        Assert.Equal(SupplierStatus.Approved, supplier.Status);
        Assert.True(supplier.IsApprovedFor(Packaging));
        Assert.Equal(Now, supplier.Categories.Single().ApprovedAt);
    }

    [Fact]
    public void Rejection_returns_the_supplier_to_prospect_with_a_reason()
    {
        var supplier = NewProspect();
        supplier.SubmitForApproval();

        supplier.Reject("Tax certificate expired");

        Assert.Equal(SupplierStatus.Prospect, supplier.Status);
        Assert.Equal("Tax certificate expired", supplier.StatusReason);
    }

    [Fact]
    public void A_suspended_supplier_is_not_approved_for_any_category()
    {
        var supplier = NewApproved();

        supplier.Suspend("Quality issues");

        Assert.False(supplier.IsApprovedFor(Packaging));
    }

    [Fact]
    public void Reinstating_restores_category_approvals()
    {
        var supplier = NewApproved();
        supplier.Suspend("Quality issues");

        supplier.Reinstate();

        Assert.True(supplier.IsApprovedFor(Packaging));
    }

    [Fact]
    public void A_blocked_supplier_cannot_be_changed()
    {
        var supplier = NewApproved();
        supplier.Block("Fraud investigation");

        Assert.Throws<DomainException>(() => supplier.AddContact("Late", "late@test.example"));
        Assert.Throws<DomainException>(() => supplier.Reinstate());
    }

    [Fact]
    public void The_same_category_cannot_be_requested_twice_unless_revoked()
    {
        var supplier = NewApproved();

        Assert.Throws<DomainException>(() => supplier.RequestCategory(Packaging));

        supplier.RevokeCategory(Packaging);
        supplier.RequestCategory(Packaging); // allowed again after revocation

        Assert.Equal(CategoryApprovalStatus.Pending, supplier.Categories.Single().Status);
    }
}
