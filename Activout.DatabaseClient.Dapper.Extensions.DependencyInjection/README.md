# Activout.DatabaseClient.Dapper.Extensions.DependencyInjection

ASP.NET Core / `Microsoft.Extensions.DependencyInjection` registration of DAOs backed by `DapperGateway`.

```C#
// One connection per scope (e.g. per HTTP request), shared by all DAOs.
services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(connectionString));
services.AddDapperDatabaseClient<IOrdersDao>();
services.AddDapperDatabaseClient<IBooksDao>();
```

## Cross-DAO transactions

`IWithTransactions.BeginTransaction()` begins the transaction on the gateway's connection, so a transaction can be
passed to another DAO only if both DAOs use the **same** connection. Register `IDbConnection` as **scoped**, and keep
DAOs scoped (the default). Don't register the connection as transient or create it separately per DAO.

```C#
using var transaction = ordersDao.BeginTransaction();
await ordersDao.InsertOrder(order, transaction);
await booksDao.ReserveBook(bookId, transaction);
transaction.Commit();
```

## Connection ownership

- The container creates and disposes the scoped connection; DAOs and gateways never dispose it.
- `DapperGateway` opens the connection when it is created, if it is closed.
