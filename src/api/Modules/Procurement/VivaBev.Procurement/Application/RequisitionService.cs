using Microsoft.EntityFrameworkCore;
using VivaBev.Approvals.Contracts;
using VivaBev.Catalogue.Contracts;
using VivaBev.Finance.Contracts;
using VivaBev.Identity.Contracts;
using VivaBev.Infrastructure.Persistence;
using VivaBev.Procurement.Contracts;
using VivaBev.Procurement.Domain.Requisitions;
using VivaBev.Procurement.Infrastructure;
using VivaBev.SharedKernel.Abstractions;
using VivaBev.SharedKernel.Domain;
using VivaBev.SharedKernel.Errors;

namespace VivaBev.Procurement.Application;

internal sealed class RequisitionService(
    ProcurementDbContext db,
    IDocumentNumberGenerator numbers,
    IOrganisationQueries organisation,
    ICatalogueQueries catalogue,
    IUserDirectory users,
    IApprovals approvals,
    IBudgets budgets,
    CrossModuleTransaction transaction,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public async Task<(Guid Id, string Number)> CreateAsync(CreateRequisitionRequest request, CancellationToken ct)
    {
        var requester = await CurrentRequesterAsync(ct);

        var costCentre = await organisation.GetCostCentreAsync(request.CostCentreId, ct)
            ?? throw new NotFoundException($"Cost centre {request.CostCentreId} was not found.");
        if (!costCentre.IsActive)
            throw new DomainException($"Cost centre {costCentre.Code} is inactive.");
        if (costCentre.DepartmentId != requester.DepartmentId)
            throw new DomainException($"Cost centre {costCentre.Code} belongs to another department.");

        var site = await organisation.GetSiteAsync(request.DeliverToSiteId, ct)
            ?? throw new NotFoundException($"Site {request.DeliverToSiteId} was not found.");
        if (!site.IsActive)
            throw new DomainException($"Site {site.Code} is inactive.");

        var number = await numbers.NextAsync("REQ", ct);
        var requisition = new Requisition(number, request.Title, request.Justification, requester.Id,
            requester.DepartmentId, costCentre.Id, site.Id, request.RequiredBy, Today);

        db.Requisitions.Add(requisition);
        await db.SaveChangesAsync(ct);
        return (requisition.Id, requisition.Number);
    }

    public async Task<int> AddLineAsync(Guid requisitionId, AddRequisitionLineRequest request, CancellationToken ct)
    {
        var requisition = await LoadOwnAsync(requisitionId, ct);
        RequisitionLine line;

        if (request.ItemId is { } itemId)
        {
            // Catalogue line: the item decides type, category and default unit
            var item = await catalogue.GetItemAsync(itemId, ct)
                ?? throw new NotFoundException($"Item {itemId} was not found.");
            if (!item.IsActive)
                throw new DomainException($"Item {item.Code} is inactive.");

            line = requisition.AddLine(
                item.IsService ? LineType.Service : LineType.Goods,
                item.Id,
                item.Code,
                string.IsNullOrWhiteSpace(request.Description) ? item.Name : request.Description,
                item.CategoryId,
                request.Quantity,
                request.UomCode ?? item.BaseUomCode,
                request.EstimatedUnitPrice);
        }
        else
        {
            // Free-text line: the requester supplies everything, and the category must be real
            if (request.CategoryId is not { } categoryId)
                throw new DomainException("A free-text line needs a category.");

            var category = await catalogue.GetCategoryAsync(categoryId, ct)
                ?? throw new NotFoundException($"Category {categoryId} was not found.");
            if (!category.IsActive)
                throw new DomainException($"Category {category.Code} is inactive.");

            var lineType = Enum.TryParse<LineType>(request.LineType, ignoreCase: true, out var parsed)
                ? parsed
                : LineType.Goods;

            line = requisition.AddLine(lineType, null, null, request.Description ?? string.Empty, category.Id,
                request.Quantity, request.UomCode ?? string.Empty, request.EstimatedUnitPrice);
        }

        await db.SaveChangesAsync(ct);
        return line.LineNo;
    }

    public async Task RemoveLineAsync(Guid requisitionId, int lineNo, CancellationToken ct)
    {
        var requisition = await LoadOwnAsync(requisitionId, ct);
        requisition.RemoveLine(lineNo);
        await db.SaveChangesAsync(ct);
    }

    public Task SubmitAsync(Guid requisitionId, CancellationToken ct) =>
        transaction.RunAsync(async () =>
        {
            var requisition = await LoadOwnAsync(requisitionId, ct);

            var check = await budgets.CheckAsync(requisition.CostCentreId, Now, requisition.EstimatedTotal, ct);
            requisition.RecordBudgetCheck(check.FiscalYear, check.Available, check.IsOverBudget);
            requisition.Submit(Now);
            await db.SaveChangesAsync(ct);

            await approvals.StartAsync(new ApprovalSubject(
                Requisition.ApprovalDocumentType, requisition.Id, requisition.Number, requisition.Title,
                requisition.EstimatedTotal, requisition.RequesterId, OverBudget: check.IsOverBudget), ct);
        }, ct);

    public Task CancelAsync(Guid requisitionId, string reason, CancellationToken ct) =>
        transaction.RunAsync(async () =>
        {
            var requisition = await LoadOwnAsync(requisitionId, ct);

            if (requisition.Status == RequisitionStatus.PendingApproval)
                await approvals.WithdrawAsync(Requisition.ApprovalDocumentType, requisition.Id, ct);

            if (requisition.Status == RequisitionStatus.Approved)
                await budgets.ReleaseAsync(Requisition.ApprovalDocumentType, requisition.Id, ct);

            requisition.Cancel(reason);
            await db.SaveChangesAsync(ct);
        }, ct);

    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private DateOnly Today => DateOnly.FromDateTime(Now);

    private async Task<UserSummaryDto> CurrentRequesterAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new ForbiddenException("You must be signed in.");
        var user = await users.GetUserAsync(userId, ct)
            ?? throw new NotFoundException($"User {userId} was not found.");
        if (!user.IsActive)
            throw new ForbiddenException("Your account is inactive.");
        return user;
    }

    private async Task<Requisition> LoadOwnAsync(Guid id, CancellationToken ct)
    {
        var requisition = await db.Requisitions
            .Include(r => r.Lines)
            .SingleOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException($"Requisition {id} was not found.");

        if (requisition.RequesterId != currentUser.UserId)
            throw new ForbiddenException("Only the requester can change this requisition.");

        return requisition;
    }
}
