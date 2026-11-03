using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VivaBev.Approvals.Application;
using VivaBev.Approvals.Contracts;
using VivaBev.Approvals.Endpoints;
using VivaBev.Approvals.Infrastructure;
using VivaBev.Infrastructure.Persistence;

namespace VivaBev.Approvals;

public static class ApprovalsModule
{
    public static IServiceCollection AddApprovals(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<ApprovalsDbContext>(configuration, ApprovalsDbContext.SchemaName);
        services.AddScoped<ApprovalService>();
        services.AddScoped<IApprovals>(sp => sp.GetRequiredService<ApprovalService>());
        return services;
    }

    public static IEndpointRouteBuilder MapApprovalsEndpoints(this IEndpointRouteBuilder app)
    {
        ApprovalEndpoints.Map(app);
        return app;
    }

    public static async Task SeedApprovalsAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        await ApprovalPolicySeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<ApprovalsDbContext>(), ct);
    }
}
