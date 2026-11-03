using System.Data;
using System.Transactions;
using Microsoft.Data.SqlClient;

namespace VivaBev.Infrastructure.Persistence;

/// <summary>Runs work from several modules in one database transaction.</summary>
public sealed class CrossModuleTransaction(SqlConnection connection)
{
    public async Task RunAsync(Func<Task> work, CancellationToken ct)
    {
        if (Transaction.Current is not null)
        {
            await work(); // already inside one: just take part in it
            return;
        }

        using var scope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);

        // The connection must be opened inside the scope to join the transaction,
        // and stay open so every module's DbContext uses this one connection.
        if (connection.State == ConnectionState.Open)
            await connection.CloseAsync();
        await connection.OpenAsync(ct);

        try
        {
            await work();
            scope.Complete();
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
