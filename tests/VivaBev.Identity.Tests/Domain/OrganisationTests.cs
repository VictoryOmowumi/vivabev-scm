using VivaBev.Identity.Domain.Organisation;
using VivaBev.SharedKernel.Domain;
using Xunit;

namespace VivaBev.Identity.Tests.Domain;

public class OrganisationTests
{
    private static readonly Guid UnitId = Guid.NewGuid();

    [Fact]
    public void A_cost_centre_belongs_to_its_department()
    {
        var it = new Department("it", "Information Technology", UnitId);

        var costCentre = it.AddCostCentre("cc-it-ops", "IT operations");

        Assert.Equal(it.Id, costCentre.DepartmentId);
        Assert.Equal("CC-IT-OPS", costCentre.Code);
        Assert.Single(it.CostCentres);
    }

    [Fact]
    public void A_department_cannot_have_the_same_cost_centre_code_twice()
    {
        var it = new Department("IT", "Information Technology", UnitId);
        it.AddCostCentre("CC-IT-OPS", "IT operations");

        Assert.Throws<DomainException>(() => it.AddCostCentre("cc-it-ops", "Duplicate"));
    }

    [Fact]
    public void Deactivating_a_department_deactivates_its_cost_centres()
    {
        var it = new Department("IT", "Information Technology", UnitId);
        it.AddCostCentre("CC-IT-OPS", "IT operations");
        it.AddCostCentre("CC-IT-INF", "IT infrastructure");

        it.Deactivate();

        Assert.False(it.IsActive);
        Assert.All(it.CostCentres, c => Assert.False(c.IsActive));
    }

    [Fact]
    public void An_inactive_department_cannot_get_new_cost_centres()
    {
        var it = new Department("IT", "Information Technology", UnitId);
        it.Deactivate();

        Assert.Throws<DomainException>(() => it.AddCostCentre("CC-IT-NEW", "New"));
    }

    [Theory]
    [InlineData(SiteType.Plant, true)]
    [InlineData(SiteType.Warehouse, true)]
    [InlineData(SiteType.Office, false)]
    public void Only_plants_and_warehouses_can_hold_stock(SiteType type, bool expected)
    {
        var site = new Site("S-01", "Test site", type, UnitId, "Ikeja", "Lagos");

        Assert.Equal(expected, site.CanHoldStock);
    }
}
