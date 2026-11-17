# Activout Database Client - libSQL backend

An `IDatabaseGateway` for [libSQL](https://github.com/tursodatabase/libsql) databases such as
[Bunny Database](https://bunny.net/database/) and Turso, built on
[Nelknet.LibSQL.Data](https://www.nuget.org/packages/Nelknet.LibSQL.Data). It works the same against a local SQLite
file, so tests can run on SQLite and production on libSQL.

```C#
var connection = LibSqlConnectionFactory.Create(connectionString); // libsql:// or https:// → libSQL, else SQLite
var userDao = new DatabaseClientBuilder()
    .With(new LibSqlGateway(connection))
    .Build<IUserDao>();
```

`DapperGateway` cannot be used with the libSQL provider, because Dapper removes the `@` from parameter names and the
provider needs it. `LibSqlGateway` builds commands with plain ADO.NET and uses Dapper only to map rows.

Values are stored in SQLite-friendly forms: `Guid` as text, `DateTime` as UTC text (`yyyy-MM-dd HH:mm:ss.fff`),
`bool` as 0/1 and enums by name. The `Guid` and `DateTime` type handlers are registered globally with Dapper when the
gateway is first used.
