using System.Linq;
using System.Threading.Tasks;
using Activout.DatabaseClient.Dapper;
using Activout.DatabaseClient.Implementation;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Activout.DatabaseClient.Test;

public class DapperGatewayTransactionTest
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DapperGateway _gateway;
    private readonly IUserDaoAsync _userDao;

    public DapperGatewayTransactionTest()
    {
        _gateway = new DapperGateway(_connection);
        _userDao = new DatabaseClientBuilder()
            .With(new TaskConverter3Factory())
            .With(_gateway)
            .Build<IUserDaoAsync>();
    }

    [Fact]
    public async Task TestCommit()
    {
        await _userDao.CreateTable();

        using (var transaction = _connection.BeginTransaction())
        {
            _gateway.Transaction = transaction;
            await _userDao.InsertNamed(1, "one");
            transaction.Commit();
            _gateway.Transaction = null;
        }

        Assert.Single(await _userDao.ListUsers());
    }

    [Fact]
    public async Task TestRollback()
    {
        await _userDao.CreateTable();

        using (var transaction = _connection.BeginTransaction())
        {
            _gateway.Transaction = transaction;
            await _userDao.InsertNamed(1, "one");
            Assert.Single(await _userDao.ListUsers());
            transaction.Rollback();
            _gateway.Transaction = null;
        }

        Assert.Empty((await _userDao.ListUsers()).ToList());
    }
}
