using Microsoft.Data.SqlClient;
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
        services.AddScoped<CrossModuleTransaction>();
        return services;
    }

    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services, IConfiguration configuration, string schema)
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString("VivaBev")
            ?? throw new InvalidOperationException("Connection string 'VivaBev' is not configured.");

        // One connection per request, shared by every module's DbContext.
        services.TryAddScoped(_ => new SqlConnection(connectionString));

        services.AddDbContext<TContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(serviceProvider.GetRequiredService<SqlConnection>(), sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", schema));
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditingInterceptor>());
        });

        return services;
    }
}
