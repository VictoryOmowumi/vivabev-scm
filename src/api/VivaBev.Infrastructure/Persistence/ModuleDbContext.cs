using Microsoft.EntityFrameworkCore;

namespace VivaBev.Infrastructure.Persistence;

public abstract class ModuleDbContext(DbContextOptions options) : DbContext(options)
{
    protected abstract string Schema { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ModuleModel.Apply(modelBuilder, Schema, GetType().Assembly);
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        ModuleModel.ApplyConventions(configurationBuilder);
}
