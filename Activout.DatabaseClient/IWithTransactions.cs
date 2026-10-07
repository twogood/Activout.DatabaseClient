using System.Data;

namespace Activout.DatabaseClient;

/// <summary>
/// Extend your DAO interface with this to begin transactions without access to the connection.
/// Pass the returned transaction as an <see cref="IDbTransaction"/> parameter to DAO methods.
/// </summary>
public interface IWithTransactions
{
    IDbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Unspecified);
}
