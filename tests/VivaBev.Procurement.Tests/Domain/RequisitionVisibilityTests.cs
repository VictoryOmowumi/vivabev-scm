using VivaBev.Procurement.Domain.Requisitions;
using VivaBev.SharedKernel.Security;
using Xunit;

namespace VivaBev.Procurement.Tests.Domain;

public class RequisitionVisibilityTests
{
    private const string Create = Permissions.Requisitions.Create;
    private const string ViewDepartment = Permissions.Requisitions.ViewDepartment;
    private const string ViewAll = Permissions.Requisitions.ViewAll;

    [Fact]
    public void Without_a_view_permission_you_see_only_your_own() =>
        Assert.Equal(RequisitionScope.Mine, RequisitionVisibility.Widest([Create]));

    [Fact]
    public void Department_managers_see_their_department() =>
        Assert.Equal(RequisitionScope.Department, RequisitionVisibility.Widest([Create, ViewDepartment]));

    [Fact]
    public void View_all_is_the_widest_scope() =>
        Assert.Equal(RequisitionScope.All, RequisitionVisibility.Widest([ViewDepartment, ViewAll]));

    [Theory]
    [InlineData(RequisitionScope.Mine, true)]
    [InlineData(RequisitionScope.Department, true)]
    [InlineData(RequisitionScope.All, false)]
    public void A_department_manager_can_narrow_but_not_widen(RequisitionScope scope, bool allowed) =>
        Assert.Equal(allowed, RequisitionVisibility.Allows([ViewDepartment], scope));
}
