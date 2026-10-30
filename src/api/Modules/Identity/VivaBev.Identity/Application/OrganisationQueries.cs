using Microsoft.EntityFrameworkCore;
using VivaBev.Identity.Contracts;
using VivaBev.Identity.Domain.Organisation;
using VivaBev.Identity.Infrastructure;

namespace VivaBev.Identity.Application;

internal sealed class OrganisationQueries(IdentityModuleDbContext db) : IOrganisationQueries
{
    public async Task<CostCentreDto?> GetCostCentreAsync(Guid costCentreId, CancellationToken ct = default) =>
        await db.Departments.AsNoTracking()
            .SelectMany(d => d.CostCentres, (d, c) => new { Department = d, CostCentre = c })
            .Where(x => x.CostCentre.Id == costCentreId)
            .Select(x => new CostCentreDto(
                x.CostCentre.Id,
                x.CostCentre.Code,
                x.CostCentre.Name,
                x.Department.Id,
                x.Department.Code,
                x.CostCentre.IsActive && x.Department.IsActive))
            .SingleOrDefaultAsync(ct);

    public async Task<SiteDto?> GetSiteAsync(Guid siteId, CancellationToken ct = default)
    {
        var row = await db.Sites.AsNoTracking()
            .Where(s => s.Id == siteId)
            .Join(db.BusinessUnits, s => s.BusinessUnitId, b => b.Id, (s, b) => new { Site = s, BusinessUnitCode = b.Code })
            .SingleOrDefaultAsync(ct);

        return row is null ? null : ToDto(row.Site, row.BusinessUnitCode);
    }

    internal static SiteDto ToDto(Site s, string businessUnitCode) =>
        new(s.Id, s.Code, s.Name, s.SiteType.ToString(), businessUnitCode, s.City, s.State, s.CanHoldStock, s.IsActive);
}
