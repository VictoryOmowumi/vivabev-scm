using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VivaBev.Catalogue.Contracts;
using VivaBev.Infrastructure.Persistence;
using VivaBev.Suppliers.Application;
using VivaBev.Suppliers.Contracts;
using VivaBev.Suppliers.Endpoints;
using VivaBev.Suppliers.Infrastructure;

namespace VivaBev.Suppliers;

public static class SuppliersModule
{
    public static IServiceCollection AddSuppliersModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<SuppliersDbContext>(configuration, SuppliersDbContext.SchemaName);
        services.AddScoped<SupplierService>();
        services.AddScoped<ISupplierQueries, SupplierQueries>();
        return services;
    }

    public static IEndpointRouteBuilder MapSuppliersEndpoints(this IEndpointRouteBuilder app)
    {
        SupplierEndpoints.Map(app);
        return app;
    }

    public static async Task SeedSuppliersAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        await SuppliersSeeder.SeedAsync(
            sp.GetRequiredService<SuppliersDbContext>(),
            sp.GetRequiredService<ICatalogueQueries>(),
            sp.GetRequiredService<TimeProvider>(),
            ct);
    }
}
