using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Activout.DatabaseClient.Attributes;
using Activout.DatabaseClient.Dapper;
using Activout.DatabaseClient.Implementation;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Activout.DatabaseClient.Test
{
    public interface IUserDaoAsync
    {
        [SqlUpdate("CREATE TABLE user (id INTEGER PRIMARY KEY, name VARCHAR)")]
        Task CreateTable();

        [SqlUpdate("INSERT INTO user(id, name) VALUES (@id, @name)")]
        Task InsertNamed([Bind("id")] int id, [Bind] string name);

        [SqlUpdate("INSERT INTO user(id, name) VALUES (@id, @Name)")]
        Task InsertObject([BindProperties] User user);

        [SqlUpdate("INSERT INTO user(id, name) VALUES (@user_id, @user_Name)")]
        Task<int> InsertObjectFull([BindProperties] User user);

        [SqlQuery("SELECT * FROM user ORDER BY name")]
        Task<IEnumerable<User>> ListUsers();

        [SqlQuery("SELECT * FROM user WHERE id = @id")]
        Task<User> GetUserById(int id);

        [SqlQuery("syntax error")]
        Task<User> SqlQuerySyntaxError();

        [SqlUpdate("syntax error")]
        Task SqlUpdateSyntaxError();
    }


    public class DatabaseClientAsyncTest
    {
        private readonly IUserDaoAsync _userDao;

        public DatabaseClientAsyncTest()
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = ":memory:"
            }.ConnectionString;
            var sqliteConnection = new SqliteConnection(connectionString);

            _userDao = new DatabaseClientBuilder()
                .With(new TaskConverter3Factory())
                .With(new DapperGateway(sqliteConnection))
                .Build<IUserDaoAsync>();
        }

        [Fact]
        public async Task TestCreateTable()
        {
            // Arrange
            await _userDao.CreateTable();

            // Act
            var users = await _userDao.ListUsers();

            // Assert
            Assert.Empty(users);
        }

        [Fact]
        public async Task TestInsertObject()
        {
            // Arrange
            await _userDao.CreateTable();

            // Act
            await _userDao.InsertObject(new User
            {
                Id = 42,
                Name = "foobar"
            });

            var user = await _userDao.GetUserById(42);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(42, user.Id);
            Assert.Equal("foobar", user.Name);
        }

        [Fact]
        public async Task TestInsertObjectNull()
        {
            // Arrange
            await _userDao.CreateTable();

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _userDao.InsertObject(null));
        }

        [Fact]
        public async Task TestInsertObjectFull()
        {
            // Arrange
            await _userDao.CreateTable();

            // Act
            var affectedRowCount = await _userDao.InsertObjectFull(new User
            {
                Id = 42,
                Name = "foobar"
            });

            var user = await _userDao.GetUserById(42);

            // Assert
            Assert.Equal(1, affectedRowCount);
            Assert.NotNull(user);
            Assert.Equal(42, user.Id);
            Assert.Equal("foobar", user.Name);
        }

        [Fact]
        public async Task TestInsertNull()
        {
            // Arrange
            await _userDao.CreateTable();
            await _userDao.InsertNamed(42, null);

            // Act
            var user = await _userDao.GetUserById(42);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(42, user.Id);
            Assert.Null(user.Name);
        }

        [Fact]
        public async Task TestQuery()
        {
            // Arrange
            await _userDao.CreateTable();
            await _userDao.InsertNamed(42, "foobar");

            // Act
            var users = (await _userDao.ListUsers()).ToList();

            // Assert
            Assert.Single(users);

            var user = users.Single();
            Assert.NotNull(user);
            Assert.Equal(42, user.Id);
            Assert.Equal("foobar", user.Name);
        }

        [Fact]
        public async Task TestQueryScalar()
        {
            // Arrange
            await _userDao.CreateTable();
            await _userDao.InsertNamed(42, "foobar");

            // Act
            var user = await _userDao.GetUserById(42);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(42, user.Id);
            Assert.Equal("foobar", user.Name);
        }

        [Fact]
        public async Task TestSqlQuerySyntaxError()
        {
            // Arrange

            // Act + Assert
            await Assert.ThrowsAnyAsync<DbException>(() => _userDao.SqlQuerySyntaxError());
        }

        [Fact]
        public async Task TestSqlUpdateSyntaxError()
        {
            // Arrange

            // Act + Assert
            await Assert.ThrowsAnyAsync<DbException>(() => _userDao.SqlUpdateSyntaxError());
        }
    }
}