using Microsoft.EntityFrameworkCore;
using VivaBev.Infrastructure.Persistence;
using VivaBev.Suppliers.Domain;

namespace VivaBev.Suppliers.Infrastructure;

public sealed class SuppliersDbContext(DbContextOptions<SuppliersDbContext> options)
    : ModuleDbContext(options)
{
    public const string SchemaName = "suppliers";

    protected override string Schema => SchemaName;

    public DbSet<Supplier> Suppliers => Set<Supplier>();
}