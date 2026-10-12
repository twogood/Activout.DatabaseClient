using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Activout.DatabaseClient.Attributes;
using Activout.DatabaseClient.Dapper;
using Activout.DatabaseClient.Implementation;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Activout.DatabaseClient.Test;

public interface IUserDaoWithTransactions : IWithTransactions
{
    [SqlUpdate("CREATE TABLE user (id INTEGER PRIMARY KEY, name VARCHAR(255))")]
    Task CreateTable();

    [SqlUpdate("INSERT INTO user(id, name) VALUES (@id, @name)")]
    Task InsertNamed(int id, string name, IDbTransaction? transaction);

    [SqlUpdate("INSERT INTO user(id, name) VALUES (@id, @Name)")]
    Task<int> InsertMany([BindMany] IEnumerable<User> users, IDbTransaction? transaction);

    [SqlUpdate("INSERT INTO user(id, name) VALUES (@id, @Name)")]
    Task<int> InsertManyWithShared(int id, [BindMany] IEnumerable<UserNameOnly> users, IDbTransaction? transaction);

    [SqlQuery("SELECT * FROM user ORDER BY name")]
    Task<IEnumerable<User>> ListUsers(IDbTransaction? transaction);
}

public class UserNameOnly
{
    public string Name { get; set; } = "";
}

public class DapperGatewayTransactionTest
{
    private readonly IUserDaoWithTransactions _userDao = new DatabaseClientBuilder()
        .With(new DapperGateway(new SqliteConnection("Data Source=:memory:")))
        .Build<IUserDaoWithTransactions>();

    [Fact]
    public async Task TestCommit()
    {
        await _userDao.CreateTable();

        using (var transaction = _userDao.BeginTransaction())
        {
            await _userDao.InsertNamed(1, "one", transaction);
            transaction.Commit();
        }

        Assert.Single(await _userDao.ListUsers(null));
    }

    [Fact]
    public async Task TestRollback()
    {
        await _userDao.CreateTable();

        using (var transaction = _userDao.BeginTransaction(IsolationLevel.Serializable))
        {
            await _userDao.InsertNamed(1, "one", transaction);
            Assert.Single(await _userDao.ListUsers(transaction));
            transaction.Rollback();
        }

        Assert.Empty((await _userDao.ListUsers(null)).ToList());
    }

    [Fact]
    public async Task TestBindManyRollback()
    {
        await _userDao.CreateTable();
        var users = new[] { new User { Id = 1, Name = "one" }, new User { Id = 2, Name = "two" } };

        using (var transaction = _userDao.BeginTransaction())
        {
            Assert.Equal(2, await _userDao.InsertMany(users, transaction));
            Assert.Equal(2, (await _userDao.ListUsers(transaction)).Count());
            transaction.Rollback();
        }

        Assert.Empty((await _userDao.ListUsers(null)).ToList());
    }

    [Fact]
    public async Task TestBindManyCommit()
    {
        await _userDao.CreateTable();
        var users = new[] { new User { Id = 1, Name = "one" }, new User { Id = 2, Name = "two" } };
        var count = await _userDao.InsertMany(users, null);
        Assert.Equal(2, count);
        Assert.Equal(2, (await _userDao.ListUsers(null)).Count());
    }

    [Fact]
    public async Task TestBindManyWithShared()
    {
        await _userDao.CreateTable();
        var users = new[] { new UserNameOnly { Name = "one" } };
        await _userDao.InsertManyWithShared(1, users, null);
        Assert.Single(await _userDao.ListUsers(null));
    }

    [Fact]
    public async Task TestBindManyEmpty()
    {
        await _userDao.CreateTable();
        Assert.Equal(0, await _userDao.InsertMany(Enumerable.Empty<User>(), null));
        Assert.Empty(await _userDao.ListUsers(null));
    }
}
