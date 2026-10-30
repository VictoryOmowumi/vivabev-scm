using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using VivaBev.Catalogue.Contracts;
using VivaBev.Suppliers.Application;
using VivaBev.Suppliers.Contracts;
using VivaBev.Suppliers.Domain;
using VivaBev.Suppliers.Infrastructure;

namespace VivaBev.Suppliers.Endpoints;

internal static class SupplierEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/suppliers").WithTags("Suppliers");

        // ---------- queries ----------

        group.MapGet("/", async (string? status, SuppliersDbContext db, CancellationToken ct) =>
        {
            var query = db.Suppliers.AsNoTracking();

            if (Enum.TryParse<SupplierStatus>(status, ignoreCase: true, out var wanted))
                query = query.Where(s => s.Status == wanted);

            var rows = await query
                .OrderBy(s => s.Code)
                .Select(s => new
                {
                    s.Id, s.Code, s.LegalName, s.Status, s.CountryCode,
                    Approved = s.Categories.Count(c => c.Status == CategoryApprovalStatus.Approved)
                })
                .ToListAsync(ct);

            return rows.Select(r => new SupplierSummaryDto(
                r.Id, r.Code, r.LegalName, r.Status.ToString(), r.CountryCode, r.Approved));
        });

        group.MapGet("/{id:guid}", async (Guid id, SuppliersDbContext db, ICatalogueQueries catalogue,
            CancellationToken ct) =>
        {
            var s = await db.Suppliers.AsNoTracking()
                .Include(x => x.Contacts)
                .Include(x => x.Addresses)
                .Include(x => x.Categories)
                .AsSplitQuery()
                .SingleOrDefaultAsync(x => x.Id == id, ct);

            if (s is null)
                return Results.NotFound();

            var categoryIds = s.Categories.Select(c => c.CategoryId).ToList();
            var names = (await catalogue.GetCategoriesAsync(categoryIds, ct)).ToDictionary(c => c.Id);

            return Results.Ok(new SupplierDetailDto(
                s.Id, s.Code, s.LegalName, s.TradingName, s.TaxId, s.Status.ToString(), s.StatusReason,
                s.CountryCode, s.DefaultCurrencyCode, s.PaymentTermsDays,
                s.Contacts.OrderByDescending(c => c.IsPrimary).ThenBy(c => c.Name)
                    .Select(c => new SupplierContactDto(c.Id, c.Name, c.Email, c.Phone, c.Role, c.IsPrimary))
                    .ToList(),
                s.Addresses
                    .Select(a => new SupplierAddressDto(a.Id, a.AddressType.ToString(), a.Line1, a.Line2,
                        a.City, a.State, a.CountryCode))
                    .ToList(),
                s.Categories
                    .Select(c => new SupplierCategoryDto(
                        c.CategoryId,
                        names.TryGetValue(c.CategoryId, out var n) ? n.Code : "?",
                        names.TryGetValue(c.CategoryId, out var m) ? m.Name : "(unknown category)",
                        c.Status.ToString(),
                        c.ApprovedAt))
                    .ToList()));
        });

        // ---------- commands ----------

        group.MapPost("/", async (CreateSupplierRequest request, SupplierService service, CancellationToken ct) =>
        {
            var id = await service.CreateAsync(request, ct);
            return Results.Created($"/api/suppliers/{id}", new { id });
        });

        group.MapPost("/{id:guid}/categories", async (Guid id, RequestCategoryRequest request,
            SupplierService service, CancellationToken ct) =>
        {
            await service.RequestCategoryAsync(id, request.CategoryId, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/categories/{categoryId:guid}/approve", async (Guid id, Guid categoryId,
            SupplierService service, CancellationToken ct) =>
        {
            await service.ApproveCategoryAsync(id, categoryId, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/categories/{categoryId:guid}/revoke", async (Guid id, Guid categoryId,
            SupplierService service, CancellationToken ct) =>
        {
            await service.RevokeCategoryAsync(id, categoryId, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/submit", async (Guid id, SupplierService service, CancellationToken ct) =>
        {
            await service.SubmitAsync(id, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/approve", async (Guid id, SupplierService service, CancellationToken ct) =>
        {
            await service.ApproveAsync(id, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/reject", async (Guid id, ReasonRequest request, SupplierService service,
            CancellationToken ct) =>
        {
            await service.RejectAsync(id, request.Reason, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/suspend", async (Guid id, ReasonRequest request, SupplierService service,
            CancellationToken ct) =>
        {
            await service.SuspendAsync(id, request.Reason, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/reinstate", async (Guid id, SupplierService service, CancellationToken ct) =>
        {
            await service.ReinstateAsync(id, ct);
            return Results.NoContent();
        });
    }
}
