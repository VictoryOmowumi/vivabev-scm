using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VivaBev.Finance.Application;
using VivaBev.Finance.Contracts;
using VivaBev.Finance.Endpoints;
using VivaBev.Finance.Infrastructure;
using VivaBev.Identity.Contracts;
using VivaBev.Infrastructure.Persistence;

namespace VivaBev.Finance;

public static class FinanceModule
{
    public static IServiceCollection AddFinanceModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<FinanceDbContext>(configuration, FinanceDbContext.SchemaName);
        services.AddScoped<BudgetService>();
        services.AddScoped<IBudgets>(sp => sp.GetRequiredService<BudgetService>());
        return services;
    }

    public static IEndpointRouteBuilder MapFinanceEndpoints(this IEndpointRouteBuilder app)
    {
        BudgetEndpoints.Map(app);
        return app;
    }

    public static async Task SeedFinanceAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        await BudgetSeeder.SeedAsync(sp.GetRequiredService<FinanceDbContext>(), sp.GetRequiredService<IOrganisationQueries>(),
            sp.GetRequiredService<TimeProvider>(), ct);
    }
}
