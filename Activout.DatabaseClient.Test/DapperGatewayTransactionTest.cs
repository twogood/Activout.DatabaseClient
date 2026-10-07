using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Activout.DatabaseClient.Attributes;
using Activout.DatabaseClient.Dapper;
using Activout.DatabaseClient.Implementation;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Activout.DatabaseClient.Test;

public interface IUserDaoWithTransactions : IWithTransactions
{
    [SqlUpdate("CREATE TABLE user (id INTEGER PRIMARY KEY, name VARCHAR(255))")]
    Task CreateTable();

    [SqlUpdate("INSERT INTO user(id, name) VALUES (@id, @name)")]
    Task InsertNamed(int id, string name, IDbTransaction? transaction);

    [SqlQuery("SELECT * FROM user ORDER BY name")]
    Task<IEnumerable<User>> ListUsers(IDbTransaction? transaction);
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
}
