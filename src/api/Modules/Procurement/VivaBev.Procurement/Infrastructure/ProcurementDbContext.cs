using Microsoft.EntityFrameworkCore;
using VivaBev.Infrastructure.Persistence;
using VivaBev.Procurement.Domain.PurchaseOrders;
using VivaBev.Procurement.Domain.Requisitions;
using VivaBev.Procurement.Domain.Sourcing;

namespace VivaBev.Procurement.Infrastructure;

public sealed class ProcurementDbContext(DbContextOptions<ProcurementDbContext> options) : ModuleDbContext(options)
{
    public const string SchemaName = "procurement";

    protected override string Schema => SchemaName;

    public DbSet<Requisition> Requisitions => Set<Requisition>();
    public DbSet<SourcingEvent> SourcingEvents => Set<SourcingEvent>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
}
