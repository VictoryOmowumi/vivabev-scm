using Microsoft.EntityFrameworkCore;
using VivaBev.Finance.Domain;
using VivaBev.Infrastructure.Persistence;

namespace VivaBev.Finance.Infrastructure;

public sealed class FinanceDbContext(DbContextOptions<FinanceDbContext> options) : ModuleDbContext(options)
{
    public const string SchemaName = "finance";

    protected override string Schema => SchemaName;

    public DbSet<Budget> Budgets => Set<Budget>();
}
