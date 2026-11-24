using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using VivaBev.Identity.Contracts;
using VivaBev.Inventory.Application;
using VivaBev.Inventory.Contracts;
using VivaBev.Inventory.Domain;
using VivaBev.Inventory.Domain.Stock;
using VivaBev.Inventory.Infrastructure;
using VivaBev.SharedKernel.Abstractions;
using VivaBev.SharedKernel.Security;

namespace VivaBev.Inventory.Endpoints;

/// <summary>
/// Stock requests. Anyone who may request stock sees their own; department managers see their department's;
/// the warehouse (issue permission) sees all of them.
/// </summary>
internal static class StockRequestEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory/stock-requests").WithTags("Stock requests").RequireAuthorization();

        group.MapGet("/form-options", async (ICurrentUser currentUser, IUserDirectory users,
            IOrganisationQueries organisation, StockLedger ledger, CancellationToken ct) =>
        {
            var me = await users.GetUserAsync(currentUser.UserId!.Value, ct);
            var costCentres = (await organisation.GetCostCentresAsync(ct))
                .Where(c => c.IsActive && c.DepartmentId == me?.DepartmentId)
                .OrderBy(c => c.Code)
                .Select(c => new StockOptionDto(c.Id, c.Code, c.Name))
                .ToList();

            var siteIds = (await ledger.BalancesAsync(null, null, ct)).Where(b => b.OnHand > 0).Select(b => b.SiteId).Distinct();
            var sites = new List<StockOptionDto>();
            foreach (var id in siteIds)
                if (await organisation.GetSiteAsync(id, ct) is { IsActive: true } site)
                    sites.Add(new StockOptionDto(site.Id, site.Code, site.Name));

            var defaultSite = sites.Any(s => s.Id == me?.SiteId) ? me!.SiteId : sites.FirstOrDefault()?.Id;
            return new StockRequestFormDto(costCentres, sites.OrderBy(s => s.Code).ToList(), defaultSite);
        }).RequireAuthorization(Permissions.Inventory.RequestStock);

        // What's in stock at a site, less what open requests are already waiting for
        group.MapGet("/available", async (Guid siteId, StockLedger ledger, InventoryDbContext db, CancellationToken ct) =>
        {
            var balances = await ledger.BalancesAsync(siteId, null, ct);
            var reserved = await ReservedAsync(db, siteId, ct);
            return balances.Where(b => b.OnHand > 0).OrderBy(b => b.ItemCode)
                .Select(b => new AvailableStockDto(b.ItemId, b.ItemCode, b.Description, b.UomCode, b.OnHand,
                    reserved.GetValueOrDefault(b.ItemId)))
                .ToList();
        }).RequireAuthorization(Permissions.Inventory.RequestStock);

        group.MapGet("/", async (ClaimsPrincipal principal, ICurrentUser currentUser, InventoryDbContext db,
            IUserDirectory users, IOrganisationQueries organisation, CancellationToken ct) =>
        {
            var permissions = principal.GetPermissions();
            var me = currentUser.UserId!.Value;
            var query = db.StockRequests.AsNoTracking().Include(r => r.Lines).AsQueryable();

            if (!permissions.Contains(Permissions.Inventory.IssueStock))
            {
                if (permissions.Contains(Permissions.Requisitions.ViewDepartment))
                {
                    var department = (await users.GetUserAsync(me, ct))?.DepartmentId;
                    var costCentreIds = (await organisation.GetCostCentresAsync(ct))
                        .Where(c => c.DepartmentId == department).Select(c => c.Id).ToList();
                    query = query.Where(r => r.RequestedBy == me || costCentreIds.Contains(r.CostCentreId));
                }
                else
                {
                    query = query.Where(r => r.RequestedBy == me);
                }
            }

            var rows = await query.OrderByDescending(r => r.Number).Take(200).ToListAsync(ct);
            var requesters = await users.GetUsersAsync(rows.Select(r => r.RequestedBy).ToList(), ct);
            var costCentres = (await organisation.GetCostCentresAsync(ct)).ToDictionary(c => c.Id);
            var sites = await SitesAsync(organisation, rows.Select(r => r.SiteId), ct);

            return rows.Select(r => new StockRequestSummaryDto(
                r.Id, r.Number, r.Status.ToString(), sites.GetValueOrDefault(r.SiteId)?.Code ?? "?",
                costCentres.GetValueOrDefault(r.CostCentreId)?.Code ?? "?",
                costCentres.GetValueOrDefault(r.CostCentreId)?.DepartmentCode ?? "?",
                r.RequestedBy, requesters.GetValueOrDefault(r.RequestedBy)?.DisplayName ?? "(unknown user)",
                r.RequestedAt, r.NeededBy, r.Purpose, r.Lines.Count, r.Lines.Count(l => l.Outstanding > 0))).ToList();
        });

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal principal, ICurrentUser currentUser,
            InventoryDbContext db, StockLedger ledger, IUserDirectory users, IOrganisationQueries organisation,
            CancellationToken ct) =>
        {
            var r = await db.StockRequests.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (r is null)
                return Results.NotFound();

            var costCentre = await organisation.GetCostCentreAsync(r.CostCentreId, ct);
            if (!await CanViewAsync(r, costCentre, principal, currentUser, users, ct))
                return Results.NotFound();

            var site = await organisation.GetSiteAsync(r.SiteId, ct);
            var balances = await ledger.AtSiteAsync(r.SiteId, r.Lines.Select(l => l.ItemId).ToList(), ct);
            var movements = await db.StockMovements.AsNoTracking()
                .Where(m => m.DocumentType == StockMovement.IssueDocumentType && m.DocumentId == r.Id)
                .OrderBy(m => m.OccurredAt).ThenBy(m => m.ItemCode)
                .ToListAsync(ct);

            var people = movements.Where(m => m.CreatedBy is not null).Select(m => m.CreatedBy!.Value)
                .Append(r.RequestedBy);
            if (r.DecidedBy is { } decider)
                people = people.Append(decider);
            var names = await users.GetUsersAsync(people.Distinct().ToList(), ct);
            string NameOf(Guid? userId) =>
                userId is { } u ? names.GetValueOrDefault(u)?.DisplayName ?? "(unknown user)" : "(unknown user)";

            return Results.Ok(new StockRequestDetailDto(
                r.Id, r.Number, r.Status.ToString(), r.SiteId, site?.Code ?? "?", site?.Name ?? "(unknown site)",
                costCentre?.Code ?? "?", costCentre?.Name ?? "(unknown cost centre)", costCentre?.DepartmentCode ?? "?",
                r.RequestedBy, NameOf(r.RequestedBy), r.RequestedAt, r.NeededBy, r.Purpose, r.LastIssuedAt,
                r.DecidedBy is null ? null : NameOf(r.DecidedBy), r.DecidedAt, r.DecisionReason,
                r.Lines.OrderBy(l => l.LineNo)
                    .Select(l => new StockRequestLineDto(l.Id, l.LineNo, l.ItemId, l.ItemCode, l.Description, l.UomCode,
                        l.QuantityRequested, l.QuantityIssued, l.Outstanding, balances.GetValueOrDefault(l.ItemId)?.OnHand ?? 0))
                    .ToList(),
                movements.Select(m => new StockIssueDto(m.OccurredAt, NameOf(m.CreatedBy), m.ItemCode, m.Description,
                    m.UomCode, -m.Quantity, decimal.Round(-m.Quantity * m.UnitCost, 2))).ToList()));
        });

        group.MapPost("/", async (CreateStockRequestRequest request, StockRequestService service, CancellationToken ct) =>
        {
            var (id, number) = await service.CreateAsync(request, ct);
            return Results.Created($"/api/inventory/stock-requests/{id}", new { id, number });
        }).RequireAuthorization(Permissions.Inventory.RequestStock);

        group.MapPost("/{id:guid}/cancel", async (Guid id, StockRequestService service, CancellationToken ct) =>
        {
            await service.CancelAsync(id, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Inventory.RequestStock);

        group.MapPost("/{id:guid}/issue", async (Guid id, IssueStockRequest request, StockRequestService service,
            CancellationToken ct) =>
        {
            await service.IssueAsync(id, request, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Inventory.IssueStock);

        group.MapPost("/{id:guid}/reject", async (Guid id, StockDecisionRequest request, StockRequestService service,
            CancellationToken ct) =>
        {
            await service.RejectAsync(id, request.Reason, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Inventory.IssueStock);

        group.MapPost("/{id:guid}/close", async (Guid id, StockDecisionRequest request, StockRequestService service,
            CancellationToken ct) =>
        {
            await service.CloseAsync(id, request.Reason, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Inventory.IssueStock);
    }

    /// <summary>Quantities that open requests at the site are still waiting for, by item.</summary>
    internal static async Task<Dictionary<Guid, decimal>> ReservedAsync(InventoryDbContext db, Guid siteId, CancellationToken ct)
    {
        var open = await db.StockRequests.AsNoTracking().Include(r => r.Lines)
            .Where(r => r.SiteId == siteId &&
                        (r.Status == StockRequestStatus.Open || r.Status == StockRequestStatus.PartiallyIssued))
            .ToListAsync(ct);
        return open.SelectMany(r => r.Lines).GroupBy(l => l.ItemId).ToDictionary(g => g.Key, g => g.Sum(l => l.Outstanding));
    }

    private static async Task<bool> CanViewAsync(StockRequest r, CostCentreDto? costCentre, ClaimsPrincipal principal,
        ICurrentUser currentUser, IUserDirectory users, CancellationToken ct)
    {
        if (currentUser.UserId is not { } me)
            return false;
        if (r.RequestedBy == me)
            return true;

        var permissions = principal.GetPermissions();
        if (permissions.Contains(Permissions.Inventory.IssueStock))
            return true;
        return permissions.Contains(Permissions.Requisitions.ViewDepartment) && costCentre is not null &&
               costCentre.DepartmentId == (await users.GetUserAsync(me, ct))?.DepartmentId;
    }

    private static async Task<Dictionary<Guid, SiteDto>> SitesAsync(IOrganisationQueries organisation,
        IEnumerable<Guid> ids, CancellationToken ct)
    {
        var sites = new Dictionary<Guid, SiteDto>();
        foreach (var id in ids.Distinct())
            if (await organisation.GetSiteAsync(id, ct) is { } site)
                sites[id] = site;
        return sites;
    }
}
