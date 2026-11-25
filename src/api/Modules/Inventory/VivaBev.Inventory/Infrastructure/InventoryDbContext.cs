using Microsoft.EntityFrameworkCore;
using VivaBev.Infrastructure.Persistence;
using VivaBev.Inventory.Domain;
using VivaBev.Inventory.Domain.Stock;

namespace VivaBev.Inventory.Infrastructure;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : ModuleDbContext(options)
{
    public const string SchemaName = "inventory";

    protected override string Schema => SchemaName;

    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockRequest> StockRequests => Set<StockRequest>();
    public DbSet<StockCount> StockCounts => Set<StockCount>();
}
