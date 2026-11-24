using VivaBev.Catalogue.Contracts;
using VivaBev.Infrastructure.Persistence;
using VivaBev.Inventory.Contracts;
using VivaBev.Inventory.Domain;
using VivaBev.Inventory.Infrastructure;
using VivaBev.Procurement.Contracts;
using VivaBev.SharedKernel.Abstractions;
using VivaBev.SharedKernel.Domain;
using VivaBev.SharedKernel.Errors;

namespace VivaBev.Inventory.Application;

/// <summary>
/// Records a delivery against a purchase order. The receipt, the stock it adds and the order's received quantities
/// are saved in one transaction across the Inventory and Procurement schemas.
/// </summary>
internal sealed class GoodsReceiptService(
    InventoryDbContext db,
    IPurchaseOrderReceiving orders,
    ICatalogueQueries catalogue,
    IDocumentNumberGenerator numbers,
    CrossModuleTransaction transaction,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public async Task<(Guid Id, string Number)> ReceiveAsync(ReceiveGoodsRequest request, CancellationToken ct)
    {
        var receiverId = currentUser.UserId ?? throw new ForbiddenException("You must be signed in.");
        (Guid, string) result = default;

        await transaction.RunAsync(async () =>
        {
            var order = await orders.GetOrderAsync(request.PurchaseOrderId, ct)
                ?? throw new NotFoundException($"Purchase order {request.PurchaseOrderId} was not found.");
            if (order.Status is not ("Issued" or "PartiallyReceived"))
                throw new DomainException($"{order.Number} is {order.Status}; only issued orders can be received.");
            if (request.Lines.Select(l => l.PurchaseOrderLineId).Distinct().Count() != request.Lines.Count)
                throw new DomainException("A line appears twice.");

            var lines = request.Lines.Select(l =>
            {
                var line = order.Lines.SingleOrDefault(x => x.Id == l.PurchaseOrderLineId)
                    ?? throw new DomainException("A line is not on this purchase order.");
                // Rejected goods go back, so only accepted quantities count against what's outstanding
                if (l.QuantityAccepted > line.Outstanding)
                    throw new DomainException(
                        $"Line {line.LineNo} ({line.Description}) has only {line.Outstanding:0.####} {line.UomCode} left to receive.");
                return new ReceiptLine(line.Id, line.ItemId, line.ItemCode, line.Description, line.UomCode, line.UnitPrice,
                    l.QuantityAccepted, l.QuantityRejected, l.RejectionReason);
            }).ToList();

            var now = clock.GetUtcNow().UtcDateTime;
            var receipt = new GoodsReceipt(await numbers.NextAsync("GRN", ct), order.Id, order.Number, order.SupplierId,
                order.DeliverToSiteId, request.DeliveryNoteNumber, request.Notes, receiverId, now, lines);
            db.GoodsReceipts.Add(receipt);

            // Stocked catalogue items go into stock at the delivery site, at the order price
            foreach (var line in receipt.Lines.Where(l => l.ItemId is not null && l.QuantityAccepted > 0))
            {
                var item = await catalogue.GetItemAsync(line.ItemId!.Value, ct);
                if (item is { IsStocked: true })
                    db.StockMovements.Add(new StockMovement(receipt.SiteId, item.Id, item.Code, line.Description,
                        line.UomCode, line.QuantityAccepted, line.UnitPrice, StockMovement.ReceiptDocumentType,
                        receipt.Id, receipt.Number, now));
            }

            await db.SaveChangesAsync(ct);

            var accepted = receipt.Lines.Where(l => l.QuantityAccepted > 0)
                .Select(l => new ReceivedQuantity(l.PurchaseOrderLineId, l.QuantityAccepted))
                .ToList();
            if (accepted.Count > 0)
                await orders.RecordReceiptAsync(order.Id, accepted, ct);

            result = (receipt.Id, receipt.Number);
        }, ct);

        return result;
    }
}
