using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VivaBev.Identity.Domain.Organisation;
using VivaBev.Identity.Domain.Users;
using VivaBev.Infrastructure.Persistence;

namespace VivaBev.Identity.Infrastructure;

public sealed class IdentityModuleDbContext(DbContextOptions<IdentityModuleDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public const string SchemaName = "identity";

    public DbSet<BusinessUnit> BusinessUnits => Set<BusinessUnit>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Site> Sites => Set<Site>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Identity defines its tables first...
        ModuleModel.Apply(builder, SchemaName, typeof(IdentityModuleDbContext).Assembly); // ...then ours run
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        ModuleModel.ApplyConventions(configurationBuilder);
}
