using Microsoft.EntityFrameworkCore;
using VivaBev.Procurement.Contracts;
using VivaBev.Procurement.Domain.PurchaseOrders;
using VivaBev.Procurement.Infrastructure;
using VivaBev.SharedKernel.Errors;

namespace VivaBev.Procurement.Application;

internal sealed class PurchaseOrderReceiving(ProcurementDbContext db) : IPurchaseOrderReceiving
{
    public async Task<IReadOnlyList<ReceivableOrderDto>> GetReceivableAsync(CancellationToken ct = default)
    {
        var orders = await db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Lines)
            .Where(p => p.Status == PurchaseOrderStatus.Issued || p.Status == PurchaseOrderStatus.PartiallyReceived)
            .OrderBy(p => p.ExpectedDelivery).ThenBy(p => p.Number)
            .ToListAsync(ct);
        return orders.Select(ToDto).ToList();
    }

    public async Task<ReceivableOrderDto?> GetOrderAsync(Guid purchaseOrderId, CancellationToken ct = default)
    {
        var order = await db.PurchaseOrders.AsNoTracking()
            .Include(p => p.Lines)
            .SingleOrDefaultAsync(p => p.Id == purchaseOrderId, ct);
        return order is null ? null : ToDto(order);
    }

    public async Task RecordReceiptAsync(Guid purchaseOrderId, IReadOnlyCollection<ReceivedQuantity> lines,
        CancellationToken ct = default)
    {
        var order = await db.PurchaseOrders.Include(p => p.Lines).SingleOrDefaultAsync(p => p.Id == purchaseOrderId, ct)
            ?? throw new NotFoundException($"Purchase order {purchaseOrderId} was not found.");
        order.RecordReceipt(lines.Select(l => (l.PurchaseOrderLineId, l.Quantity)).ToList());
        await db.SaveChangesAsync(ct);
    }

    private static ReceivableOrderDto ToDto(PurchaseOrder p) =>
        new(p.Id, p.Number, p.Status.ToString(), p.SupplierId, p.DeliverToSiteId, p.ExpectedDelivery, p.IssuedAt, p.Total,
            p.Lines.OrderBy(l => l.LineNo)
                .Select(l => new ReceivableLineDto(l.Id, l.LineNo, l.ItemId, l.ItemCode, l.Description, l.UomCode,
                    l.Quantity, l.QuantityReceived, l.Outstanding, l.UnitPrice))
                .ToList());
}
