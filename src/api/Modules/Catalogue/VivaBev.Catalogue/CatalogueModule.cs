using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VivaBev.Catalogue.Endpoints;
using VivaBev.Catalogue.Infrastructure;
using VivaBev.Infrastructure.Persistence;
using VivaBev.Catalogue.Application;
using VivaBev.Catalogue.Contracts;

namespace VivaBev.Catalogue;

public static class CatalogueModule
{
    public static IServiceCollection AddCatalogueModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<CatalogueDbContext>(configuration, CatalogueDbContext.SchemaName);
        services.AddScoped<ICatalogueQueries, CatalogueQueries>();

        return services;
    }

    public static IEndpointRouteBuilder MapCatalogueEndpoints(this IEndpointRouteBuilder app)
    {
        CatalogueEndpoints.Map(app);
        return app;
    }

    public static async Task SeedCatalogueAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogueDbContext>();
        await CatalogueSeeder.SeedAsync(db, ct);
    }
}