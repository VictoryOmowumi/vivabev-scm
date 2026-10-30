using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using VivaBev.Identity.Application;
using VivaBev.Identity.Contracts;
using VivaBev.Identity.Domain.Organisation;
using VivaBev.Identity.Infrastructure;

namespace VivaBev.Identity.Endpoints;

internal static class OrganisationEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organisation").WithTags("Organisation");

        group.MapGet("/business-units", async (IdentityModuleDbContext db, CancellationToken ct) =>
        {
            var units = await db.BusinessUnits.AsNoTracking().OrderBy(b => b.Code).ToListAsync(ct);
            return units.Select(b => new BusinessUnitDto(b.Id, b.Code, b.Name, b.IsActive));
        });

        group.MapGet("/departments", async (IdentityModuleDbContext db, CancellationToken ct) =>
        {
            var unitCodes = await db.BusinessUnits.AsNoTracking().ToDictionaryAsync(b => b.Id, b => b.Code, ct);
            var departments = await db.Departments.AsNoTracking()
                .Include(d => d.CostCentres)
                .OrderBy(d => d.Code)
                .ToListAsync(ct);

            return departments.Select(d => new DepartmentDto(
                d.Id, d.Code, d.Name, unitCodes[d.BusinessUnitId], d.IsActive,
                d.CostCentres.OrderBy(c => c.Code)
                    .Select(c => new CostCentreSummaryDto(c.Id, c.Code, c.Name, c.IsActive))
                    .ToList()));
        });

        group.MapGet("/sites", async (string? type, IdentityModuleDbContext db, CancellationToken ct) =>
        {
            var query = db.Sites.AsNoTracking();
            if (Enum.TryParse<SiteType>(type, ignoreCase: true, out var wanted))
                query = query.Where(s => s.SiteType == wanted);

            var rows = await query
                .Join(db.BusinessUnits, s => s.BusinessUnitId, b => b.Id, (s, b) => new { Site = s, BusinessUnitCode = b.Code })
                .OrderBy(x => x.Site.Code)
                .ToListAsync(ct);

            return rows.Select(x => OrganisationQueries.ToDto(x.Site, x.BusinessUnitCode));
        });

        group.MapPost("/departments/{id:guid}/cost-centres", async (Guid id, CreateCostCentreRequest request,
            OrganisationService service, CancellationToken ct) =>
        {
            var costCentreId = await service.AddCostCentreAsync(id, request, ct);
            return Results.Created($"/api/organisation/departments/{id}", new { id = costCentreId });
        });

        group.MapPost("/departments/{id:guid}/cost-centres/{costCentreId:guid}/deactivate", async (Guid id,
            Guid costCentreId, OrganisationService service, CancellationToken ct) =>
        {
            await service.DeactivateCostCentreAsync(id, costCentreId, ct);
            return Results.NoContent();
        });

        group.MapPost("/departments/{id:guid}/deactivate", async (Guid id, OrganisationService service,
            CancellationToken ct) =>
        {
            await service.DeactivateDepartmentAsync(id, ct);
            return Results.NoContent();
        });
    }
}
