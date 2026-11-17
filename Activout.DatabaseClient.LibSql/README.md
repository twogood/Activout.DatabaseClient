# Activout Database Client - libSQL backend

An `IDatabaseGateway` for [libSQL](https://github.com/tursodatabase/libsql) databases such as
[Bunny Database](https://bunny.net/database/) and Turso, built on
[Nelknet.LibSQL.Data](https://www.nuget.org/packages/Nelknet.LibSQL.Data). It works the same against a local SQLite
file, so tests can run on SQLite and production on libSQL.

```C#
SqliteTypeHandlers.Register(); // once at startup, so Guid and DateTime can be read back
var connection = new LibSQLConnection(connectionString); // or a SqliteConnection in tests
var userDao = new DatabaseClientBuilder()
    .With(new LibSqlGateway(connection))
    .Build<IUserDao>();
```

`DapperGateway` cannot be used with the libSQL provider, because Dapper removes the `@` from parameter names and the
provider needs it. `LibSqlGateway` builds commands with plain ADO.NET and uses Dapper only to map rows.

Values are stored in SQLite-friendly forms: `Guid` as text, `DateTime` as UTC text (`yyyy-MM-dd HH:mm:ss.fff`),
`bool` as 0/1 and enums by name.

`SqliteTypeHandlers` (from `Activout.DatabaseClient.Dapper`) lets Dapper read `Guid` and `DateTime` values stored as
text. Dapper's type handlers are global, so it also applies to any `DapperGateway` in the process: values stored as
text are parsed, and native `Guid` and `DateTime` values from other databases are returned unchanged.

A `DapperGateway` on Microsoft.Data.Sqlite can read what `LibSqlGateway` writes, but writes `Guid` in upper case and
`DateTime` in the provider's own format. Let one gateway write a given table.
