using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Activout.DatabaseClient;

/// <summary>A named SQL parameter and its value.</summary>
/// <param name="Name">The parameter name, without prefix.</param>
/// <param name="Value">The parameter value.</param>
public record QueryParameter(string Name, object? Value);

/// <summary>An SQL statement to be executed by an <see cref="IDatabaseGateway"/>.</summary>
public class SqlStatement
{
    /// <summary>The SQL text.</summary>
    public required string Sql { get; init; }

    /// <summary>The parameters to bind.</summary>
    public IList<QueryParameter> Parameters { get; } = new List<QueryParameter>();

    /// <summary>The type to map each result row to.</summary>
    public required Type EffectiveType { get; init; }

    /// <summary>The transaction to run in, or <c>null</c> for none.</summary>
    public IDbTransaction? Transaction { get; init; }
}

/// <summary>
/// Executes SQL statements against a database. Implement this to plug in a data access library other than Dapper.
/// </summary>
public interface IDatabaseGateway
{
    /// <summary>Begins a transaction.</summary>
    /// <param name="isolationLevel">The isolation level.</param>
    /// <returns>The transaction.</returns>
    IDbTransaction BeginTransaction(IsolationLevel isolationLevel);

    /// <summary>Executes a statement that does not return rows.</summary>
    /// <param name="statement">The statement.</param>
    /// <returns>The number of affected rows.</returns>
    Task<int> ExecuteAsync(SqlStatement statement);

    /// <summary>Runs a query and maps all rows to <see cref="SqlStatement.EffectiveType"/>.</summary>
    /// <param name="statement">The statement.</param>
    /// <returns>The mapped rows.</returns>
    Task<IEnumerable<object>> QueryAsync(SqlStatement statement);

    /// <summary>Runs a query and maps the first row to <see cref="SqlStatement.EffectiveType"/>.</summary>
    /// <param name="statement">The statement.</param>
    /// <returns>The mapped row, or <c>null</c> if there are no rows.</returns>
    Task<object?> QueryFirstOrDefaultAsync(SqlStatement statement);
}
