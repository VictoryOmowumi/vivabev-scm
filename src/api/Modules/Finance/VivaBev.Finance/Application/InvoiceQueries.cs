using Microsoft.EntityFrameworkCore;
using VivaBev.Finance.Contracts;
using VivaBev.Finance.Domain.Invoices;
using VivaBev.Finance.Infrastructure;

namespace VivaBev.Finance.Application;

internal sealed class InvoiceQueries(FinanceDbContext db) : IInvoiceQueries
{
    public Task<bool> HasLiveInvoicesAsync(Guid purchaseOrderId, CancellationToken ct = default) =>
        db.SupplierInvoices.AsNoTracking()
            .AnyAsync(i => i.PurchaseOrderId == purchaseOrderId && i.Status != InvoiceStatus.Rejected, ct);
}
