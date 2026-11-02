using Microsoft.EntityFrameworkCore;
using VivaBev.SharedKernel.Abstractions;
using VivaBev.SharedKernel.Time;

namespace VivaBev.Numbering;

internal sealed class DocumentNumberGenerator(NumberingDbContext db, TimeProvider clock) : IDocumentNumberGenerator
{
    public async Task<string> NextAsync(string prefix, CancellationToken ct = default)
    {
        var code = prefix.Trim().ToUpperInvariant();
        var fiscalYear = FiscalCalendar.FiscalYearOf(clock.GetUtcNow().UtcDateTime);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            // Read and increment in one statement: the database makes concurrent callers take turns.
            var taken = await db.Database.SqlQuery<int>($"""
                UPDATE numbering.document_sequences
                SET NextValue = NextValue + 1
                OUTPUT deleted.NextValue AS Value
                WHERE Prefix = {code} AND FiscalYear = {fiscalYear}
                """).ToListAsync(ct);

            if (taken.Count == 1)
                return $"{code}-{fiscalYear}-{taken[0]:00000}";

            // First number of this type this year: create the counter row, then loop to take from it.
            try
            {
                db.Add(new DocumentSequence(code, fiscalYear));
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear(); // another request created it first; just retry the update
            }
        }

        throw new InvalidOperationException($"Could not allocate a {code} number.");
    }
}
