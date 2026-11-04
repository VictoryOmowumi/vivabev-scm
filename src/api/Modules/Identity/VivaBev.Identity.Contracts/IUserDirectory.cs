namespace VivaBev.Identity.Contracts;

public sealed record UserSummaryDto(
    Guid Id, string DisplayName, string Email, Guid DepartmentId, Guid SiteId, Guid? ManagerId, bool IsActive);

public interface IUserDirectory
{
    Task<UserSummaryDto?> GetUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Several users in one query, keyed by id. Ids that don't exist are left out.</summary>
    Task<IReadOnlyDictionary<Guid, UserSummaryDto>> GetUsersAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);

    /// <summary>Active users holding a role, optionally only in one department, ordered by name.</summary>
    Task<IReadOnlyList<UserSummaryDto>> GetUsersInRoleAsync(
        string roleName, Guid? departmentId = null, CancellationToken ct = default);
}
