using System.Security.Claims;

namespace VivaBev.SharedKernel.Security;

public static class PermissionExtensions
{
    /// <summary>The permission claims on a signed-in user, e.g. "requisitions.view-all".</summary>
    public static IReadOnlySet<string> GetPermissions(this ClaimsPrincipal user) =>
        user.FindAll(Permissions.ClaimType).Select(c => c.Value).ToHashSet();
}
