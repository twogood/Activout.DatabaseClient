using System.Data;
using System.Threading.Tasks;
using Activout.DatabaseClient.Attributes;
using Activout.DatabaseClient.Dapper.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Activout.DatabaseClient.Test;

public interface IOrdersDao : IWithTransactions
{
    [SqlUpdate("CREATE TABLE orders (id INTEGER PRIMARY KEY)")]
    Task CreateTable();

    [SqlUpdate("INSERT INTO orders(id) VALUES (@id)")]
    Task Insert(int id, IDbTransaction? transaction);

    [SqlQuery("SELECT COUNT(*) FROM orders")]
    Task<long> Count(IDbTransaction? transaction);
}

public interface IBooksDao
{
    [SqlUpdate("INSERT INTO orders(id) VALUES (@id)")]
    Task InsertOrder(int id, IDbTransaction? transaction);
}

public class SharedConnectionDependencyInjectionTest
{
    private static ServiceProvider CreateProvider()
    {
        // One scoped connection per scope, shared by all DAOs resolved in it.
        var services = new ServiceCollection();
        services.AddScoped<IDbConnection>(_ => new SqliteConnection("Data Source=:memory:"));
        services.AddDapperDatabaseClient<IOrdersDao>();
        services.AddDapperDatabaseClient<IBooksDao>();
        return services.BuildServiceProvider(true);
    }

    [Fact]
    public async Task TestCommitAcrossDaos()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var orders = scope.ServiceProvider.GetRequiredService<IOrdersDao>();
        var books = scope.ServiceProvider.GetRequiredService<IBooksDao>();
        await orders.CreateTable();

        using (var transaction = orders.BeginTransaction())
        {
            await books.InsertOrder(1, transaction);
            transaction.Commit();
        }

        Assert.Equal(1L, await orders.Count(null));
    }

    [Fact]
    public async Task TestRollbackAcrossDaos()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var orders = scope.ServiceProvider.GetRequiredService<IOrdersDao>();
        var books = scope.ServiceProvider.GetRequiredService<IBooksDao>();
        await orders.CreateTable();

        using (var transaction = orders.BeginTransaction())
        {
            await books.InsertOrder(1, transaction);
            transaction.Rollback();
        }

        Assert.Equal(0L, await orders.Count(null));
    }
}
