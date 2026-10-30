using VivaBev.Identity.Domain.Users;
using VivaBev.Identity.Infrastructure;
using VivaBev.SharedKernel.Domain;
using VivaBev.SharedKernel.Security;
using Xunit;

namespace VivaBev.Identity.Tests.Domain;

public class UserAndRoleTests
{
    [Fact]
    public void A_new_user_signs_in_with_their_email_and_is_active()
    {
        var user = new AppUser(" Ada@VivaBev.example ", "Ada Obi", Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal("ada@vivabev.example", user.Email);
        Assert.Equal(user.Email, user.UserName);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void A_user_cannot_be_their_own_manager()
    {
        var user = new AppUser("ada@vivabev.example", "Ada Obi", Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<DomainException>(() => user.SetManager(user.Id));
    }

    [Fact]
    public void Every_role_only_grants_known_permissions()
    {
        foreach (var (name, _, grants) in RoleDefinitions.All)
            foreach (var permission in grants)
                Assert.True(Permissions.All.Contains(permission), $"{name} grants unknown permission '{permission}'");
    }

    [Fact]
    public void The_administrator_holds_every_permission()
    {
        var admin = RoleDefinitions.All.Single(r => r.Name == RoleDefinitions.Administrator);

        Assert.Equal(Permissions.All.Order(), admin.Grants.Order());
    }

    [Theory]
    [InlineData("Requester")]
    [InlineData("ProcurementOfficer")]
    [InlineData("FinanceOfficer")]
    public void Only_the_procurement_manager_and_administrator_approve_suppliers(string roleName)
    {
        var role = RoleDefinitions.All.Single(r => r.Name == roleName);

        Assert.DoesNotContain(Permissions.Suppliers.Approve, role.Grants);
    }
}
