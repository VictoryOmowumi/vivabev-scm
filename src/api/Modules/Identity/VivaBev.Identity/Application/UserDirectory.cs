using Microsoft.EntityFrameworkCore;
using VivaBev.Identity.Contracts;
using VivaBev.Identity.Infrastructure;

namespace VivaBev.Identity.Application;

internal sealed class UserDirectory(IdentityModuleDbContext db) : IUserDirectory
{
    public async Task<UserSummaryDto?> GetUserAsync(Guid userId, CancellationToken ct = default) =>
        await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserSummaryDto(
                u.Id, u.DisplayName, u.Email!, u.DepartmentId, u.SiteId, u.ManagerId, u.IsActive))
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<UserSummaryDto>> GetUsersInRoleAsync(
        string roleName, Guid? departmentId = null, CancellationToken ct = default)
    {
        var roleId = await db.Roles
            .Where(r => r.Name == roleName)
            .Select(r => (Guid?)r.Id)
            .SingleOrDefaultAsync(ct);

        if (roleId is null)
            return [];

        var query = db.Users.AsNoTracking()
            .Where(u => u.IsActive && db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId));

        if (departmentId is { } department)
            query = query.Where(u => u.DepartmentId == department);

        return await query
            .OrderBy(u => u.DisplayName)
            .Select(u => new UserSummaryDto(u.Id, u.DisplayName, u.Email!, u.DepartmentId, u.SiteId, u.ManagerId, u.IsActive))
            .ToListAsync(ct);
    }
}
