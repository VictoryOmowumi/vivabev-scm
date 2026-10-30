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
}
