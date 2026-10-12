# Activout.DatabaseClient.Extensions.DependencyInjection

`Microsoft.Extensions.DependencyInjection` extensions for Activout.DatabaseClient.

```C#
services.AddDatabaseClient<IOrdersDao>(sp => new MyGateway(sp.GetRequiredService<MyConnection>()));
```

DAOs are registered as scoped by default. For cross-DAO transactions, every participating DAO must use a gateway over
the **same** connection, so register the connection as scoped. See the Dapper package
(`Activout.DatabaseClient.Dapper.Extensions.DependencyInjection`) for a ready-made `IDbConnection` based setup.
