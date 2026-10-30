using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using VivaBev.Identity.Contracts;
using VivaBev.Identity.Domain.Users;
using VivaBev.Identity.Infrastructure;
using VivaBev.SharedKernel.Security;

namespace VivaBev.Identity.Endpoints;

internal static class AuthEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest request, UserManager<AppUser> users,
            SignInManager<AppUser> signIn) =>
        {
            var user = await users.FindByEmailAsync(request.Email);
            if (user is null || !user.IsActive)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password");

            var result = await signIn.PasswordSignInAsync(user, request.Password, isPersistent: false,
                lockoutOnFailure: true);

            if (result.IsLockedOut)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized,
                    title: "Too many failed attempts. Try again in 5 minutes.");
            if (!result.Succeeded)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password");

            return Results.NoContent();
        }).AllowAnonymous();

        group.MapPost("/logout", async (SignInManager<AppUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapGet("/me", async (ClaimsPrincipal principal, UserManager<AppUser> users,
            IdentityModuleDbContext db, CancellationToken ct) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null)
                return Results.Unauthorized();

            var departmentCode = await db.Departments.Where(d => d.Id == user.DepartmentId)
                .Select(d => d.Code).SingleAsync(ct);
            var siteCode = await db.Sites.Where(s => s.Id == user.SiteId)
                .Select(s => s.Code).SingleAsync(ct);
            var roles = await users.GetRolesAsync(user);
            var permissions = principal.FindAll(Permissions.ClaimType)
                .Select(c => c.Value).Distinct().Order().ToList();

            return Results.Ok(new CurrentUserDto(user.Id, user.Email!, user.DisplayName,
                departmentCode, siteCode, roles.Order().ToList(), permissions));
        }).RequireAuthorization();
    }
}
