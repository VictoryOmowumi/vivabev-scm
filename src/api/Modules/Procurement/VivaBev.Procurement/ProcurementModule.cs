using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VivaBev.Approvals.Contracts;
using VivaBev.Infrastructure.Persistence;
using VivaBev.Procurement.Application;
using VivaBev.Procurement.Endpoints;
using VivaBev.Procurement.Contracts;
using VivaBev.Procurement.Infrastructure;

namespace VivaBev.Procurement;

public static class ProcurementModule
{
    public static IServiceCollection AddProcurementModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<ProcurementDbContext>(configuration, ProcurementDbContext.SchemaName);
        services.AddScoped<RequisitionService>();
        services.AddScoped<SourcingService>();
        services.AddScoped<PurchaseOrderService>();
        services.AddScoped<IRequisitionQueries, RequisitionQueries>();
        services.AddScoped<IApprovalOutcomeHandler, RequisitionApprovalHandler>();
        services.AddScoped<IApprovalOutcomeHandler, SourcingAwardApprovalHandler>();
        return services;
    }

    public static IEndpointRouteBuilder MapProcurementEndpoints(this IEndpointRouteBuilder app)
    {
        RequisitionEndpoints.Map(app);
        SourcingEndpoints.Map(app);
        PurchaseOrderEndpoints.Map(app);
        return app;
    }
}
