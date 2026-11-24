using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using VivaBev.Approvals.Contracts;
using VivaBev.Identity.Contracts;
using VivaBev.Procurement.Application;
using VivaBev.Procurement.Contracts;
using VivaBev.Procurement.Domain.Requisitions;
using VivaBev.Procurement.Domain.Sourcing;
using VivaBev.Procurement.Infrastructure;
using VivaBev.SharedKernel.Security;
using VivaBev.Suppliers.Contracts;

namespace VivaBev.Procurement.Endpoints;

internal static class SourcingEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        // Procurement runs RFQs; Finance and the MD can look.
        var group = app.MapGroup("/api/sourcing").WithTags("Sourcing")
            .RequireAuthorization(Permissions.Sourcing.View);

        // ---------- queries ----------

        group.MapGet("/events", async (string? status, ProcurementDbContext db, IUserDirectory users,
            CancellationToken ct) =>
        {
            var query = db.SourcingEvents.AsNoTracking()
                .Include(e => e.Lines)
                .Include(e => e.Invitations)
                .Include(e => e.Quotations).ThenInclude(q => q.Lines)
                .AsSplitQuery();

            if (Enum.TryParse<SourcingEventStatus>(status, ignoreCase: true, out var wanted))
                query = query.Where(e => e.Status == wanted);

            var events = await query.OrderByDescending(e => e.Number).Take(200).ToListAsync(ct);
            var buyers = await users.GetUsersAsync(events.Select(e => e.BuyerId).ToList(), ct);

            return events.Select(e =>
            {
                var received = e.Quotations.Where(q => q.Status == QuotationStatus.Received).ToList();
                // Only quotes that price every line compare fairly on total
                var complete = received.Where(q => q.Lines.Count == e.Lines.Count).ToList();
                return new SourcingEventSummaryDto(
                    e.Id, e.Number, e.Title, e.Status.ToString(),
                    buyers.GetValueOrDefault(e.BuyerId)?.DisplayName ?? "(unknown user)",
                    e.ResponseDeadline, e.Lines.Count,
                    e.Lines.Select(l => l.RequisitionNumber).Distinct().Order().ToList(),
                    e.Invitations.Count, received.Count, e.EstimatedTotal,
                    complete.Count == 0 ? null : complete.Min(q => q.Total),
                    e.PublishedAt);
            }).ToList();
        });

        group.MapGet("/events/{id:guid}", async (Guid id, ProcurementDbContext db, IUserDirectory users,
            ISupplierQueries suppliers, CancellationToken ct) =>
        {
            var e = await db.SourcingEvents.AsNoTracking().WithEverything().SingleOrDefaultAsync(x => x.Id == id, ct);

            if (e is null)
                return Results.NotFound();

            var buyer = await users.GetUserAsync(e.BuyerId, ct);
            var names = await suppliers.GetNamesAsync(
                e.Invitations.Select(i => i.SupplierId).Concat(e.Quotations.Select(q => q.SupplierId)).ToList(), ct);
            string Name(Guid supplierId) => names.GetValueOrDefault(supplierId) ?? "(unknown supplier)";
            var evaluation = e.Evaluate();

            return Results.Ok(new SourcingEventDetailDto(
                e.Id, e.Number, e.Title, e.Status.ToString(), e.BuyerId, buyer?.DisplayName ?? "(unknown user)",
                e.ResponseDeadline, e.CurrencyCode, e.PublishedAt, e.ResponsesClosedAt, e.CancellationReason,
                e.EstimatedTotal, e.NeedsCompetition, SourcingEvent.MinimumSuppliers, SourcingEvent.CompetitiveThreshold,
                e.Lines.OrderBy(l => l.LineNo)
                    .Select(l => new SourcingEventLineDto(l.Id, l.LineNo, l.RequisitionId, l.RequisitionNumber, l.ItemId,
                        l.ItemCode, l.Description, l.CategoryId, l.Quantity, l.UomCode, l.EstimatedUnitPrice,
                        l.EstimatedTotal))
                    .ToList(),
                e.Invitations.OrderBy(i => Name(i.SupplierId))
                    .Select(i => new SourcingInvitationDto(i.SupplierId, Name(i.SupplierId), i.Status.ToString(), i.InvitedAt))
                    .ToList(),
                e.Quotations.OrderBy(q => q.Status).ThenBy(q => q.Total)
                    .Select(q => new QuotationDto(q.Id, q.SupplierId, Name(q.SupplierId), q.SupplierReference,
                        q.ReceivedAt, q.ValidUntil, q.LeadTimeDays, q.PaymentTermsDays, q.Total, q.Status.ToString(),
                        q.WithdrawalReason,
                        q.Lines.Select(l => new QuotationLineDto(l.SourcingEventLineId, l.UnitPrice, l.Quantity,
                            l.LineTotal, l.Notes)).ToList()))
                    .ToList(),
                e.Criteria.OrderBy(c => c.SortOrder)
                    .Select(c => new EvaluationCriterionDto(c.Id, c.Name, c.Kind.ToString(), c.Weight))
                    .ToList(),
                evaluation.Select(v =>
                {
                    var q = e.Quotations.Single(x => x.Id == v.QuotationId);
                    return new QuotationEvaluationDto(v.QuotationId, v.SupplierId, Name(v.SupplierId), v.Total,
                        q.LeadTimeDays, q.PaymentTermsDays,
                        e.Criteria.OrderBy(c => c.SortOrder).Select(c => new CriterionScoreDto(c.Id, v.Scores[c.Id])).ToList(),
                        v.WeightedTotal, v.FullyScored, v.Rank);
                }).ToList(),
                e.AwardQuotation is { } award
                    ? new SourcingAwardDto(award.Id, award.SupplierId, Name(award.SupplierId), award.Total,
                        e.AwardJustification, e.AwardRecommendedAt!.Value, e.AwardedAt,
                        evaluation.FirstOrDefault(v => v.QuotationId == award.Id)?.Rank == 1)
                    : null,
                e.AwardRejectionReason));
        });

        // The award's approval chain, like a requisition's
        group.MapGet("/events/{id:guid}/award-approval", async (Guid id, IApprovals approvals, CancellationToken ct) =>
        {
            var approval = await approvals.GetLatestAsync(SourcingEvent.AwardDocumentType, id, ct);
            return approval is null ? Results.NoContent() : Results.Ok(approval);
        });

        // Approved requisitions not yet on an RFQ
        group.MapGet("/sourceable-requisitions", async (ProcurementDbContext db, IUserDirectory users,
            IOrganisationQueries organisation, CancellationToken ct) =>
        {
            var rows = await db.Requisitions.AsNoTracking()
                .Include(r => r.Lines)
                .Where(r => r.Status == RequisitionStatus.Approved)
                .OrderBy(r => r.ApprovedAt)
                .ToListAsync(ct);

            var requesters = await users.GetUsersAsync(rows.Select(r => r.RequesterId).ToList(), ct);
            var costCentres = (await organisation.GetCostCentresAsync(ct)).ToDictionary(c => c.Id, c => c.Code);

            return rows.Select(r => new SourceableRequisitionDto(
                r.Id, r.Number, r.Title, requesters.GetValueOrDefault(r.RequesterId)?.DisplayName ?? "(unknown user)",
                costCentres.GetValueOrDefault(r.CostCentreId) ?? "?", r.RequiredBy, r.EstimatedTotal, r.Lines.Count,
                r.ApprovedAt)).ToList();
        }).RequireAuthorization(Permissions.Sourcing.Manage);

        // Who may be invited: approved for at least one line's category
        group.MapGet("/events/{id:guid}/eligible-suppliers", async (Guid id, ProcurementDbContext db,
            ISupplierQueries suppliers, CancellationToken ct) =>
        {
            var e = await db.SourcingEvents.AsNoTracking()
                .Include(x => x.Lines)
                .Include(x => x.Invitations)
                .AsSplitQuery()
                .SingleOrDefaultAsync(x => x.Id == id, ct);
            if (e is null)
                return Results.NotFound();

            var eligible = await suppliers.GetApprovedForCategoriesAsync(e.Lines.Select(l => l.CategoryId).ToList(), ct);
            var invited = e.Invitations.Select(i => i.SupplierId).ToHashSet();

            return Results.Ok(eligible.Select(s => new EligibleSupplierDto(
                    s.Id, s.Code, s.Name, s.PaymentTermsDays,
                    e.Lines.Count(l => s.ApprovedCategoryIds.Contains(l.CategoryId)), e.Lines.Count,
                    invited.Contains(s.Id)))
                .OrderByDescending(s => s.LinesCovered).ThenBy(s => s.Name)
                .ToList());
        }).RequireAuthorization(Permissions.Sourcing.Manage);

        // ---------- commands (buyers only) ----------

        var manage = group.MapGroup("/events").RequireAuthorization(Permissions.Sourcing.Manage);

        manage.MapPost("/", async (CreateSourcingEventRequest request, SourcingService service, CancellationToken ct) =>
        {
            var (id, number) = await service.CreateAsync(request, ct);
            return Results.Created($"/api/sourcing/events/{id}", new { id, number });
        });

        manage.MapPost("/{id:guid}/requisitions", async (Guid id, AddRequisitionToEventRequest request,
            SourcingService service, CancellationToken ct) =>
        {
            await service.AddRequisitionAsync(id, request.RequisitionId, ct);
            return Results.NoContent();
        });

        manage.MapDelete("/{id:guid}/requisitions/{requisitionId:guid}", async (Guid id, Guid requisitionId,
            SourcingService service, CancellationToken ct) =>
        {
            await service.RemoveRequisitionAsync(id, requisitionId, ct);
            return Results.NoContent();
        });

        manage.MapPost("/{id:guid}/invitations", async (Guid id, InviteSupplierRequest request,
            SourcingService service, CancellationToken ct) =>
        {
            await service.InviteAsync(id, request.SupplierId, ct);
            return Results.NoContent();
        });

        manage.MapDelete("/{id:guid}/invitations/{supplierId:guid}", async (Guid id, Guid supplierId,
            SourcingService service, CancellationToken ct) =>
        {
            await service.UninviteAsync(id, supplierId, ct);
            return Results.NoContent();
        });

        manage.MapPost("/{id:guid}/invitations/{supplierId:guid}/decline", async (Guid id, Guid supplierId,
            SourcingService service, CancellationToken ct) =>
        {
            await service.RecordDeclineAsync(id, supplierId, ct);
            return Results.NoContent();
        });

        manage.MapPut("/{id:guid}/deadline", async (Guid id, ChangeDeadlineRequest request,
            SourcingService service, CancellationToken ct) =>
        {
            await service.ChangeDeadlineAsync(id, request.ResponseDeadline, ct);
            return Results.NoContent();
        });

        manage.MapPost("/{id:guid}/publish", async (Guid id, SourcingService service, CancellationToken ct) =>
        {
            await service.PublishAsync(id, ct);
            return Results.NoContent();
        });

        manage.MapPost("/{id:guid}/quotations", async (Guid id, RecordQuotationRequest request,
            SourcingService service, CancellationToken ct) =>
        {
            var quotationId = await service.RecordQuotationAsync(id, request, ct);
            return Results.Ok(new { id = quotationId });
        });

        manage.MapPost("/{id:guid}/quotations/{quotationId:guid}/withdraw", async (Guid id, Guid quotationId,
            SourcingReasonRequest request, SourcingService service, CancellationToken ct) =>
        {
            await service.WithdrawQuotationAsync(id, quotationId, request.Reason, ct);
            return Results.NoContent();
        });

        manage.MapPost("/{id:guid}/close", async (Guid id, SourcingService service, CancellationToken ct) =>
        {
            await service.CloseResponsesAsync(id, ct);
            return Results.NoContent();
        });

        manage.MapPut("/{id:guid}/criteria", async (Guid id, SetCriteriaRequest request,
            SourcingService service, CancellationToken ct) =>
        {
            await service.SetCriteriaAsync(id, request, ct);
            return Results.NoContent();
        });

        manage.MapPut("/{id:guid}/scores", async (Guid id, SetScoresRequest request,
            SourcingService service, CancellationToken ct) =>
        {
            await service.SetScoresAsync(id, request, ct);
            return Results.NoContent();
        });

        manage.MapPost("/{id:guid}/award", async (Guid id, RecommendAwardRequest request,
            SourcingService service, CancellationToken ct) =>
        {
            await service.RecommendAwardAsync(id, request, ct);
            return Results.NoContent();
        });

        manage.MapPost("/{id:guid}/award/withdraw", async (Guid id, SourcingService service, CancellationToken ct) =>
        {
            await service.WithdrawRecommendationAsync(id, ct);
            return Results.NoContent();
        });

        manage.MapPost("/{id:guid}/cancel", async (Guid id, SourcingReasonRequest request,
            SourcingService service, CancellationToken ct) =>
        {
            await service.CancelAsync(id, request.Reason, ct);
            return Results.NoContent();
        });
    }
}
