namespace VivaBev.Identity.Contracts;

public sealed record UserSummaryDto(
    Guid Id, string DisplayName, string Email, Guid DepartmentId, Guid SiteId, Guid? ManagerId, bool IsActive);

public interface IUserDirectory
{
    Task<UserSummaryDto?> GetUserAsync(Guid userId, CancellationToken ct = default);
}
