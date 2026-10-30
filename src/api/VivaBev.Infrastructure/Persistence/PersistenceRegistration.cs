using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace VivaBev.Infrastructure.Persistence;

public static class PersistenceRegistration
{
    public static IServiceCollection AddVivaBevPersistence(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<AuditingInterceptor>();
        return services;
    }

    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services, IConfiguration configuration, string schema)
        where TContext : ModuleDbContext
    {
        var connectionString = configuration.GetConnectionString("VivaBev")
            ?? throw new InvalidOperationException("Connection string 'VivaBev' is not configured.");

        services.AddDbContext<TContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", schema));
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditingInterceptor>());
        });

        return services;
    }
}