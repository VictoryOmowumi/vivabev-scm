using Microsoft.EntityFrameworkCore;
using VivaBev.Catalogue.Contracts;
using VivaBev.Identity.Contracts;
using VivaBev.Inventory.Contracts;
using VivaBev.Inventory.Domain;
using VivaBev.Inventory.Domain.Stock;
using VivaBev.Inventory.Infrastructure;
using VivaBev.SharedKernel.Abstractions;
using VivaBev.SharedKernel.Domain;
using VivaBev.SharedKernel.Errors;

namespace VivaBev.Inventory.Application;

/// <summary>Raises stock requests and issues them. An issue and the stock it takes out are saved together.</summary>
internal sealed class StockRequestService(
    InventoryDbContext db,
    StockLedger ledger,
    ICatalogueQueries catalogue,
    IOrganisationQueries organisation,
    IUserDirectory users,
    IDocumentNumberGenerator numbers,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public async Task<(Guid Id, string Number)> CreateAsync(CreateStockRequestRequest request, CancellationToken ct)
    {
        var me = Me();
        var requester = await users.GetUserAsync(me, ct) ?? throw new ForbiddenException("Your account was not found.");

        var costCentre = await organisation.GetCostCentreAsync(request.CostCentreId, ct)
            ?? throw new DomainException("Choose a cost centre.");
        if (!costCentre.IsActive)
            throw new DomainException($"{costCentre.Code} is no longer active.");
        if (costCentre.DepartmentId != requester.DepartmentId)
            throw new DomainException("Charge the request to one of your own department's cost centres.");

        var site = await organisation.GetSiteAsync(request.SiteId, ct) ?? throw new DomainException("Choose a site.");
        var balances = await ledger.AtSiteAsync(site.Id, request.Lines.Select(l => l.ItemId).ToList(), ct);

        var items = new List<RequestedItem>();
        foreach (var line in request.Lines.Where(l => l.Quantity != 0))
        {
            var item = await catalogue.GetItemAsync(line.ItemId, ct) ?? throw new DomainException("An item was not found.");
            if (!item.IsStocked)
                throw new DomainException($"{item.Name} isn't a stocked item. Raise a requisition to buy it.");
            if (!balances.TryGetValue(item.Id, out var balance) || balance.OnHand <= 0)
                throw new DomainException($"There's no {item.Name} in stock at {site.Name}.");
            items.Add(new RequestedItem(item.Id, item.Code, item.Name, item.BaseUomCode, line.Quantity));
        }

        var now = Now();
        var stockRequest = new StockRequest(await numbers.NextAsync("SR", ct), site.Id, costCentre.Id, me,
            request.Purpose, request.NeededBy, now, items);
        db.StockRequests.Add(stockRequest);
        await db.SaveChangesAsync(ct);
        return (stockRequest.Id, stockRequest.Number);
    }

    public async Task IssueAsync(Guid id, IssueStockRequest request, CancellationToken ct)
    {
        var stockRequest = await LoadAsync(id, ct);
        if (request.Lines.Select(l => l.LineId).Distinct().Count() != request.Lines.Count)
            throw new DomainException("A line appears twice.");

        var issued = stockRequest.Issue(request.Lines.ToDictionary(l => l.LineId, l => l.Quantity), Now());

        // Stock can't go below zero: check each item against what's on hand at the site now
        var balances = await ledger.AtSiteAsync(stockRequest.SiteId, issued.Select(i => i.Line.ItemId).ToList(), ct);
        foreach (var (line, quantity) in issued)
        {
            var onHand = balances.GetValueOrDefault(line.ItemId)?.OnHand ?? 0;
            if (quantity > onHand)
                throw new DomainException($"Only {onHand:0.####} {line.UomCode} of {line.Description} is in stock.");

            db.StockMovements.Add(new StockMovement(stockRequest.SiteId, line.ItemId, line.ItemCode, line.Description,
                line.UomCode, -quantity, balances[line.ItemId].AverageCost, StockMovement.IssueDocumentType,
                stockRequest.Id, stockRequest.Number, stockRequest.LastIssuedAt!.Value));
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task RejectAsync(Guid id, string reason, CancellationToken ct)
    {
        var stockRequest = await LoadAsync(id, ct);
        stockRequest.Reject(reason, Me(), Now());
        await db.SaveChangesAsync(ct);
    }

    public async Task CloseAsync(Guid id, string reason, CancellationToken ct)
    {
        var stockRequest = await LoadAsync(id, ct);
        stockRequest.CloseShort(reason, Me(), Now());
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(Guid id, CancellationToken ct)
    {
        var stockRequest = await LoadAsync(id, ct);
        stockRequest.Cancel(Me(), Now());
        await db.SaveChangesAsync(ct);
    }

    private async Task<StockRequest> LoadAsync(Guid id, CancellationToken ct) =>
        await db.StockRequests.Include(r => r.Lines).SingleOrDefaultAsync(r => r.Id == id, ct)
        ?? throw new NotFoundException($"Stock request {id} was not found.");

    private Guid Me() => currentUser.UserId ?? throw new ForbiddenException("You must be signed in.");

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;
}
