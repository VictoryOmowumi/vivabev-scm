using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using VivaBev.Identity.Contracts;
using VivaBev.Procurement.Application;
using VivaBev.Procurement.Contracts;
using VivaBev.Procurement.Infrastructure;
using VivaBev.SharedKernel.Abstractions;
using VivaBev.SharedKernel.Security;

namespace VivaBev.Procurement.Endpoints;

internal static class RequisitionEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // Any signed-in user may call these; what they see is filtered below.
        var group = app.MapGroup("/api/requisitions").WithTags("Requisitions").RequireAuthorization();

        // ---------- queries ----------

        group.MapGet("/", async (bool? mine, ClaimsPrincipal principal, ICurrentUser currentUser,
            ProcurementDbContext db, CancellationToken ct) =>
        {
            var canSeeAll = principal.HasClaim(Permissions.ClaimType, Permissions.Requisitions.ViewAll);
            var query = db.Requisitions.AsNoTracking();

            if (!canSeeAll || mine == true)
            {
                var userId = currentUser.UserId;
                query = query.Where(r => r.RequesterId == userId);
            }

            var rows = await query.OrderByDescending(r => r.Number).Take(100).ToListAsync(ct);

            return rows.Select(r => new RequisitionSummaryDto(
                r.Id, r.Number, r.Title, r.Status.ToString(), r.RequesterId,
                r.RequiredBy, r.EstimatedTotal, r.CurrencyCode, r.SubmittedAt));
        });

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal principal, ICurrentUser currentUser,
            ProcurementDbContext db, IOrganisationQueries organisation, IUserDirectory users, CancellationToken ct) =>
        {
            var r = await db.Requisitions.AsNoTracking()
                .Include(x => x.Lines)
                .SingleOrDefaultAsync(x => x.Id == id, ct);

            if (r is null)
                return Results.NotFound();

            var isOwner = r.RequesterId == currentUser.UserId;
            if (!isOwner && !principal.HasClaim(Permissions.ClaimType, Permissions.Requisitions.ViewAll))
                return Results.Forbid();

            var requester = await users.GetUserAsync(r.RequesterId, ct);
            var costCentre = await organisation.GetCostCentreAsync(r.CostCentreId, ct);
            var site = await organisation.GetSiteAsync(r.DeliverToSiteId, ct);

            return Results.Ok(new RequisitionDetailDto(
                r.Id, r.Number, r.Title, r.Justification, r.Status.ToString(),
                r.RequesterId, requester?.DisplayName ?? "(unknown user)",
                costCentre?.Code ?? "?", site?.Code ?? "?",
                r.RequiredBy, r.CurrencyCode, r.EstimatedTotal, r.SubmittedAt, r.CancellationReason,
                r.Lines.OrderBy(l => l.LineNo)
                    .Select(l => new RequisitionLineDto(
                        l.LineNo, l.LineType.ToString(), l.ItemId, l.ItemCode, l.Description, l.CategoryId,
                        l.Quantity, l.UomCode, l.EstimatedUnitPrice, l.LineTotal))
                    .ToList()));
        });

        // ---------- commands ----------

        group.MapPost("/", async (CreateRequisitionRequest request, RequisitionService service, CancellationToken ct) =>
        {
            var (id, number) = await service.CreateAsync(request, ct);
            return Results.Created($"/api/requisitions/{id}", new { id, number });
        }).RequireAuthorization(Permissions.Requisitions.Create);

        group.MapPost("/{id:guid}/lines", async (Guid id, AddRequisitionLineRequest request,
            RequisitionService service, CancellationToken ct) =>
        {
            var lineNo = await service.AddLineAsync(id, request, ct);
            return Results.Ok(new { lineNo });
        }).RequireAuthorization(Permissions.Requisitions.Create);

        group.MapDelete("/{id:guid}/lines/{lineNo:int}", async (Guid id, int lineNo,
            RequisitionService service, CancellationToken ct) =>
        {
            await service.RemoveLineAsync(id, lineNo, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Requisitions.Create);

        group.MapPost("/{id:guid}/submit", async (Guid id, RequisitionService service, CancellationToken ct) =>
        {
            await service.SubmitAsync(id, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Requisitions.Create);

        group.MapPost("/{id:guid}/cancel", async (Guid id, CancelRequisitionRequest request,
            RequisitionService service, CancellationToken ct) =>
        {
            await service.CancelAsync(id, request.Reason, ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.Requisitions.Create);
    }
}
