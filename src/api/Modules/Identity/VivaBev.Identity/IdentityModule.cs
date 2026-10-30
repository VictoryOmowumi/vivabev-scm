using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VivaBev.Identity.Application;
using VivaBev.Identity.Contracts;
using VivaBev.Identity.Endpoints;
using VivaBev.Identity.Infrastructure;
using VivaBev.Infrastructure.Persistence;

namespace VivaBev.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<IdentityModuleDbContext>(configuration, IdentityModuleDbContext.SchemaName);
        services.AddScoped<OrganisationService>();
        services.AddScoped<IOrganisationQueries, OrganisationQueries>();
        return services;
    }

    public static IEndpointRouteBuilder MapOrganisationEndpoints(this IEndpointRouteBuilder app)
    {
        OrganisationEndpoints.Map(app);
        return app;
    }

    public static async Task SeedOrganisationAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        await OrganisationSeeder.SeedAsync(db, ct);
    }
}
