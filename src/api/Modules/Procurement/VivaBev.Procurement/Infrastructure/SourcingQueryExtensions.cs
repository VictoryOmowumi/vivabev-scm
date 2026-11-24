using Microsoft.EntityFrameworkCore;
using VivaBev.Procurement.Domain.Sourcing;

namespace VivaBev.Procurement.Infrastructure;

internal static class SourcingQueryExtensions
{
    /// <summary>An RFQ with everything it owns: lines, invitations, quotations and their lines, criteria and scores.</summary>
    public static IQueryable<SourcingEvent> WithEverything(this IQueryable<SourcingEvent> query) =>
        query
            .Include(e => e.Lines)
            .Include(e => e.Invitations)
            .Include(e => e.Quotations).ThenInclude(q => q.Lines)
            .Include(e => e.Criteria)
            .Include(e => e.Scores)
            .AsSplitQuery();
}
