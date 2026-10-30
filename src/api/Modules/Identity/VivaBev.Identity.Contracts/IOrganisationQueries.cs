namespace VivaBev.Identity.Contracts;

public interface IOrganisationQueries
{
    /// <summary>Null if not found. IsActive is false if the cost centre or its department is inactive.</summary>
    Task<CostCentreDto?> GetCostCentreAsync(Guid costCentreId, CancellationToken ct = default);

    Task<SiteDto?> GetSiteAsync(Guid siteId, CancellationToken ct = default);
}
