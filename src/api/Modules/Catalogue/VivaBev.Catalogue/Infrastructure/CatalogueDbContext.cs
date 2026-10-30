using Microsoft.EntityFrameworkCore;
using VivaBev.Catalogue.Domain;
using VivaBev.Infrastructure.Persistence;

namespace VivaBev.Catalogue.Infrastructure;

public sealed class CatalogueDbContext(DbContextOptions<CatalogueDbContext> options)
    : ModuleDbContext(options)
{
    public const string SchemaName = "catalogue";

    protected override string Schema => SchemaName;

    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<UomConversion> UomConversions => Set<UomConversion>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Item> Items => Set<Item>();
}