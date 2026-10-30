using System.Reflection;
using Microsoft.EntityFrameworkCore;
using VivaBev.SharedKernel.Domain;

namespace VivaBev.Infrastructure.Persistence;

/// <summary>Model rules every module context applies, whatever its base class.</summary>
public static class ModuleModel
{
    public static void Apply(ModelBuilder modelBuilder, string schema, Assembly configurationsAssembly)
    {
        modelBuilder.HasDefaultSchema(schema);
        modelBuilder.ApplyConfigurationsFromAssembly(configurationsAssembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            var clrType = entityType.ClrType;

            if (typeof(Entity).IsAssignableFrom(clrType))
                modelBuilder.Entity(clrType).Property(nameof(Entity.Id)).ValueGeneratedNever();

            if (typeof(AggregateRoot).IsAssignableFrom(clrType))
                modelBuilder.Entity(clrType).Property(nameof(AggregateRoot.RowVersion)).IsRowVersion();
        }
    }

    public static void ApplyConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(19, 4);
        configurationBuilder.Properties<string>().HaveMaxLength(200);
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(30);
    }
}
