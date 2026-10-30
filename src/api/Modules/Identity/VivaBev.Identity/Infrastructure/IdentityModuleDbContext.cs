using Microsoft.EntityFrameworkCore;
using VivaBev.Identity.Domain.Organisation;
using VivaBev.Infrastructure.Persistence;

namespace VivaBev.Identity.Infrastructure;

public sealed class IdentityModuleDbContext(DbContextOptions<IdentityModuleDbContext> options)
    : ModuleDbContext(options)
{
    public const string SchemaName = "identity";

    protected override string Schema => SchemaName;

    public DbSet<BusinessUnit> BusinessUnits => Set<BusinessUnit>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Site> Sites => Set<Site>();
}
