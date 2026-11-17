using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace Activout.DatabaseClient.LibSql;

/// <summary>
/// An <see cref="IDatabaseGateway"/> for libSQL (Bunny Database, Turso) that also works with SQLite.
/// </summary>
/// <remarks>
/// Commands are built with plain ADO.NET, because Dapper removes the <c>@</c> from parameter names and the libSQL
/// provider needs it. Dapper only maps the rows. Values are stored in SQLite-friendly forms so that SQLite and libSQL
/// behave the same: <see cref="Guid"/> as text, <see cref="DateTime"/> as UTC text in <see cref="DateTimeFormat"/>,
/// <see cref="bool"/> as 0/1 and enums by name. The Guid and DateTime type handlers are registered globally with Dapper.
/// </remarks>
public class LibSqlGateway : IDatabaseGateway
{
    /// <summary>The format <see cref="DateTime"/> values are stored in, always in UTC.</summary>
    public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

    private readonly DbConnection _connection;

    static LibSqlGateway()
    {
        SqlMapper.AddTypeHandler(new GuidHandler());
        SqlMapper.AddTypeHandler(new NullableGuidHandler());
        SqlMapper.AddTypeHandler(new DateTimeHandler());
        SqlMapper.AddTypeHandler(new NullableDateTimeHandler());
    }

    /// <summary>Creates the gateway. The connection is opened when first used.</summary>
    /// <param name="connection">A libSQL or SQLite connection, for example from <see cref="LibSqlConnectionFactory"/>.</param>
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
    public async Task<IEnumerable<object>> QueryAsync(SqlStatement statement)
    {
        await using var command = await CreateCommand(statement).ConfigureAwait(false);
        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        // Rows are buffered into a DataTable and mapped from there: the libSQL provider's reader does not support
        // the schema calls Dapper makes while a result is being read.
        var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
        var rows = new List<object?[]>();
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            var row = new object?[names.Length];
            for (var i = 0; i < names.Length; i++) row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }

        using var table = new DataTable();
        for (var i = 0; i < names.Length; i++)
        {
            var sample = rows.Select(r => r[i]).FirstOrDefault(v => v is not null);
            table.Columns.Add(names[i], sample?.GetType() ?? typeof(string));
        }

        foreach (var row in rows) table.Rows.Add(row.Select(v => v ?? DBNull.Value).ToArray());
        using var buffered = table.CreateDataReader();
        return buffered.Parse(statement.EffectiveType).ToList();
    }

    /// <inheritdoc />
    public async Task<object?> QueryFirstOrDefaultAsync(SqlStatement statement)
    {
        return (await QueryAsync(statement).ConfigureAwait(false)).FirstOrDefault();
    }

    /// <summary>Converts a parameter value to the form it is stored in.</summary>
    /// <param name="value">The value.</param>
    public static object ToDatabase(object? value) => value switch
    {
        null => DBNull.Value,
        Guid g => g.ToString("D"),
        DateTime d => d.ToUniversalTime().ToString(DateTimeFormat, CultureInfo.InvariantCulture),
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

    private static DateTime ParseDateTime(object value) =>
        DateTime.Parse((string)value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    private class GuidHandler : SqlMapper.TypeHandler<Guid>
    {
        public override Guid Parse(object value) => Guid.Parse((string)value);
        public override void SetValue(IDbDataParameter parameter, Guid value) => parameter.Value = ToDatabase(value);
    }

    private class NullableGuidHandler : SqlMapper.TypeHandler<Guid?>
    {
        public override Guid? Parse(object value) => value is null or DBNull ? null : Guid.Parse((string)value);
        public override void SetValue(IDbDataParameter parameter, Guid? value) => parameter.Value = ToDatabase(value);
    }

    private class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
    {
        public override DateTime Parse(object value) => ParseDateTime(value);
        public override void SetValue(IDbDataParameter parameter, DateTime value) => parameter.Value = ToDatabase(value);
    }

    private class NullableDateTimeHandler : SqlMapper.TypeHandler<DateTime?>
    {
        public override DateTime? Parse(object value) => value is null or DBNull ? null : ParseDateTime(value);
        public override void SetValue(IDbDataParameter parameter, DateTime? value) => parameter.Value = ToDatabase(value);
    }
}
