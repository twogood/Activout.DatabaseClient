using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Activout.DatabaseClient.Dapper;
using Dapper;

namespace Activout.DatabaseClient.LibSql;

/// <summary>
/// An <see cref="IDatabaseGateway"/> for libSQL (Bunny Database, Turso) that also works with SQLite.
/// </summary>
/// <remarks>
/// Commands are built with plain ADO.NET, because Dapper removes the <c>@</c> from parameter names and the libSQL
/// provider needs it. Dapper only maps the rows. Values are stored in SQLite-friendly forms so that SQLite and libSQL
/// behave the same: <see cref="Guid"/> as text, <see cref="DateTime"/> as UTC text in
/// <see cref="SqliteTypeHandlers.DateTimeFormat"/>, <see cref="bool"/> as 0/1 and enums by name. To read Guid and
/// DateTime values back, call <see cref="SqliteTypeHandlers.Register"/> once at startup.
/// </remarks>
public class LibSqlGateway : IDatabaseGateway
{
    private readonly DbConnection _connection;

    /// <summary>Creates the gateway. The connection is opened when first used.</summary>
    /// <param name="connection">A libSQL connection, or any SQLite connection such as one from Microsoft.Data.Sqlite.</param>
    public LibSqlGateway(DbConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    public IDbTransaction BeginTransaction(IsolationLevel isolationLevel)
    {
        if (_connection.State != ConnectionState.Open) _connection.Open();
        return _connection.BeginTransaction(isolationLevel);
    }

    /// <inheritdoc />
    public async Task<int> ExecuteAsync(SqlStatement statement)
    {
        await using var command = await CreateCommand(statement).ConfigureAwait(false);
        return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<IEnumerable<object>> QueryAsync(SqlStatement statement) => QueryAsync(statement, int.MaxValue);

    /// <inheritdoc />
    public async Task<object?> QueryFirstOrDefaultAsync(SqlStatement statement)
    {
        return (await QueryAsync(statement, 1).ConfigureAwait(false)).FirstOrDefault();
    }

    /// <summary>Converts a parameter value to the form it is stored in.</summary>
    /// <param name="value">The value.</param>
    public static object ToDatabase(object? value) => value switch
    {
        null => DBNull.Value,
        Guid g => SqliteTypeHandlers.ToText(g),
        DateTime d => SqliteTypeHandlers.ToText(d),
        bool b => b ? 1L : 0L,
        Enum e => e.ToString(),
        _ => value,
    };

    private async Task<DbCommand> CreateCommand(SqlStatement statement)
    {
        if (_connection.State != ConnectionState.Open) await _connection.OpenAsync().ConfigureAwait(false);
        var command = _connection.CreateCommand();
        command.CommandText = statement.Sql;
        command.Transaction = (DbTransaction?)statement.Transaction;
        foreach (var parameter in statement.Parameters)
        {
            var p = command.CreateParameter();
            p.ParameterName = "@" + parameter.Name.TrimStart('@');
            p.Value = ToDatabase(parameter.Value);
            command.Parameters.Add(p);
        }

        return command;
    }

    private async Task<IEnumerable<object>> QueryAsync(SqlStatement statement, int maxRows)
    {
        await using var command = await CreateCommand(statement).ConfigureAwait(false);
        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        // Rows are buffered and mapped from there: the libSQL provider's reader does not support the schema calls
        // Dapper makes while a result is being read.
        var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
        var rows = new List<object[]>();
        while (rows.Count < maxRows && await reader.ReadAsync().ConfigureAwait(false))
        {
            var row = new object[names.Length];
            for (var i = 0; i < names.Length; i++) row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
            rows.Add(row);
        }

        await using var buffered = new BufferedDataReader(names, rows);
        return buffered.Parse(statement.EffectiveType).ToList();
    }
}
