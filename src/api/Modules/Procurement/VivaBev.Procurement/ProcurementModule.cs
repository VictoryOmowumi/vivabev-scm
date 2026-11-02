using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VivaBev.Infrastructure.Persistence;
using VivaBev.Procurement.Application;
using VivaBev.Procurement.Endpoints;
using VivaBev.Procurement.Infrastructure;

namespace VivaBev.Procurement;

public static class ProcurementModule
{
    public static IServiceCollection AddProcurementModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<ProcurementDbContext>(configuration, ProcurementDbContext.SchemaName);
        services.AddScoped<RequisitionService>();
        return services;
    }

    public static IEndpointRouteBuilder MapProcurementEndpoints(this IEndpointRouteBuilder app)
    {
        RequisitionEndpoints.Map(app);
        return app;
    }
}
