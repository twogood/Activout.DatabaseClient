using System;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Nelknet.LibSQL.Data;

namespace Activout.DatabaseClient.LibSql;

/// <summary>Creates a libSQL or SQLite connection from a connection string.</summary>
public static class LibSqlConnectionFactory
{
    /// <summary>
    /// Returns a <see cref="LibSQLConnection"/> when the connection string names a remote database
    /// (<c>libsql://</c> or <c>https://</c>, such as Bunny Database or Turso), otherwise a <see cref="SqliteConnection"/>
    /// for a local file or in-memory database.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    public static DbConnection Create(string connectionString) =>
        IsRemote(connectionString) ? new LibSQLConnection(connectionString) : new SqliteConnection(connectionString);

    /// <summary>Whether the connection string names a remote libSQL database.</summary>
    /// <param name="connectionString">The connection string.</param>
    public static bool IsRemote(string connectionString) =>
        connectionString.Contains("libsql://", StringComparison.OrdinalIgnoreCase) ||
        connectionString.Contains("https://", StringComparison.OrdinalIgnoreCase);
}
