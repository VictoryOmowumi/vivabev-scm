using Microsoft.EntityFrameworkCore;
using VivaBev.SharedKernel.Domain;

namespace VivaBev.Infrastructure.Persistence;

public abstract class ModuleDbContext(DbContextOptions options) : DbContext(options)
{
    protected abstract string Schema { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            var clrType = entityType.ClrType;

            if (typeof(Entity).IsAssignableFrom(clrType))
                modelBuilder.Entity(clrType).Property(nameof(Entity.Id)).ValueGeneratedNever();

            if (typeof(AggregateRoot).IsAssignableFrom(clrType))
                modelBuilder.Entity(clrType).Property(nameof(AggregateRoot.RowVersion)).IsRowVersion();
        }

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(19, 4);
        configurationBuilder.Properties<string>().HaveMaxLength(200);
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(30);
    }
}