namespace VivaBev.Identity.Contracts;

public sealed record LoginRequest(string Email, string Password);

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string DepartmentCode,
    string SiteCode,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
