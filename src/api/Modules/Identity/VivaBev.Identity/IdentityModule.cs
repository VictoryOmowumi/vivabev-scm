using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VivaBev.Identity.Application;
using VivaBev.Identity.Contracts;
using VivaBev.Identity.Domain.Users;
using VivaBev.Identity.Endpoints;
using VivaBev.Identity.Infrastructure;
using VivaBev.Infrastructure.Persistence;
using VivaBev.SharedKernel.Security;

namespace VivaBev.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<IdentityModuleDbContext>(configuration, IdentityModuleDbContext.SchemaName);
        services.AddScoped<OrganisationService>();
        services.AddScoped<IOrganisationQueries, OrganisationQueries>();

        // Users, roles and sign-in
        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 10;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<IdentityModuleDbContext>()
            .AddSignInManager();

        // The sign-in cookie
        services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "vivabev.auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;

            // An API answers with status codes, never redirects to a login page
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        // One policy per permission: "suppliers.approve" requires that claim
        services.AddAuthorization(options =>
        {
            foreach (var permission in Permissions.All)
                options.AddPolicy(permission, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(Permissions.ClaimType, permission));
        });

        return services;
    }

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        AuthEndpoints.Map(app);
        return app;
    }

    public static IEndpointRouteBuilder MapOrganisationEndpoints(this IEndpointRouteBuilder app)
    {
        OrganisationEndpoints.Map(app);
        return app;
    }

    public static async Task SeedIdentityAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<IdentityModuleDbContext>();

        await OrganisationSeeder.SeedAsync(db, ct); // departments and sites first...
        await UserSeeder.SeedAsync(db,              // ...because users belong to them
            sp.GetRequiredService<RoleManager<AppRole>>(),
            sp.GetRequiredService<UserManager<AppUser>>(),
            ct);
    }
}
