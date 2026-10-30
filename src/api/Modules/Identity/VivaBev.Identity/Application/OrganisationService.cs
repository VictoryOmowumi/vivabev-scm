using Microsoft.EntityFrameworkCore;
using VivaBev.Identity.Contracts;
using VivaBev.Identity.Domain.Organisation;
using VivaBev.Identity.Infrastructure;
using VivaBev.SharedKernel.Domain;
using VivaBev.SharedKernel.Errors;

namespace VivaBev.Identity.Application;

internal sealed class OrganisationService(IdentityModuleDbContext db)
{
    public async Task<Guid> AddCostCentreAsync(Guid departmentId, CreateCostCentreRequest request,
        CancellationToken ct)
    {
        var department = await LoadDepartmentAsync(departmentId, ct);
        var costCentre = department.AddCostCentre(request.Code, request.Name);

        var codeTaken = await db.Departments
            .AnyAsync(d => d.CostCentres.Any(c => c.Code == costCentre.Code), ct);
        if (codeTaken)
            throw new DomainException($"Cost centre code {costCentre.Code} is already used by another department.");

        await db.SaveChangesAsync(ct);
        return costCentre.Id;
    }

    public async Task DeactivateCostCentreAsync(Guid departmentId, Guid costCentreId, CancellationToken ct)
    {
        var department = await LoadDepartmentAsync(departmentId, ct);
        department.DeactivateCostCentre(costCentreId);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeactivateDepartmentAsync(Guid departmentId, CancellationToken ct)
    {
        var department = await LoadDepartmentAsync(departmentId, ct);
        department.Deactivate();
        await db.SaveChangesAsync(ct);
    }

    private async Task<Department> LoadDepartmentAsync(Guid id, CancellationToken ct) =>
        await db.Departments
            .Include(d => d.CostCentres)
            .SingleOrDefaultAsync(d => d.Id == id, ct)
        ?? throw new NotFoundException($"Department {id} was not found.");
}
