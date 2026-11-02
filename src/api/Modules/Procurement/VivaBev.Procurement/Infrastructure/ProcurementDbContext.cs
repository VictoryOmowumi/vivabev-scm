using Microsoft.EntityFrameworkCore;
using VivaBev.Infrastructure.Persistence;
using VivaBev.Procurement.Domain.Requisitions;

namespace VivaBev.Procurement.Infrastructure;

public sealed class ProcurementDbContext(DbContextOptions<ProcurementDbContext> options) : ModuleDbContext(options)
{
    public const string SchemaName = "procurement";

    protected override string Schema => SchemaName;

    public DbSet<Requisition> Requisitions => Set<Requisition>();
}
