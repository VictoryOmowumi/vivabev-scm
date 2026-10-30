using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VivaBev.Infrastructure.Persistence;
using VivaBev.SharedKernel.Abstractions;

namespace VivaBev.Numbering;

public static class NumberingModule
{
    public static IServiceCollection AddDocumentNumbering(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<NumberingDbContext>(configuration, NumberingDbContext.SchemaName);
        services.AddScoped<IDocumentNumberGenerator, DocumentNumberGenerator>();
        return services;
    }
}
