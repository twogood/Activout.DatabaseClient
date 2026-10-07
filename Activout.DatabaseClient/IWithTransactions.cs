using System.Data;

namespace Activout.DatabaseClient;

/// <summary>
/// Extend your DAO interface with this to begin transactions without access to the connection.
/// Pass the returned transaction as an <see cref="IDbTransaction"/> parameter to DAO methods.
/// </summary>
public interface IWithTransactions
{
    /// <summary>Begins a transaction.</summary>
    /// <param name="isolationLevel">The isolation level.</param>
    /// <returns>The transaction. Commit or roll back and dispose it when done.</returns>
    IDbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Unspecified);
}
