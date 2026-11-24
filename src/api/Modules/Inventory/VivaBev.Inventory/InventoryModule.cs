using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VivaBev.Infrastructure.Persistence;
using VivaBev.Inventory.Application;
using VivaBev.Inventory.Endpoints;
using VivaBev.Inventory.Infrastructure;

namespace VivaBev.Inventory;

public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<InventoryDbContext>(configuration, InventoryDbContext.SchemaName);
        services.AddScoped<GoodsReceiptService>();
        return services;
    }

    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        InventoryEndpoints.Map(app);
        return app;
    }
}
