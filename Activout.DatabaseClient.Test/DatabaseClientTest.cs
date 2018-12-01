using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Activout.DatabaseClient.Attributes;
using Activout.DatabaseClient.Dapper;
using Activout.DatabaseClient.Implementation;
using Microsoft.Data.Sqlite;
using Xunit;
using Xunit.Sdk;

namespace Activout.DatabaseClient.Test
{
    public class User
    {
        [Bind("id")] public int Id { get; set; }
        public string Name { get; set; }
    }

    public interface IUserDao
    {
        [SqlUpdate("CREATE TABLE user (id INTEGER PRIMARY KEY, name VARCHAR)")]
        void CreateTable();

        [SqlUpdate("INSERT INTO user(id, name) VALUES (?, ?)")]
        void InsertPositional(int id, string name);

        [SqlUpdate("INSERT INTO user(id, name) VALUES (:id, :name)")]
        void InsertNamed([Bind("id")] int id, [Bind("name")] string name);

        [SqlUpdate("INSERT INTO user(id, name) VALUES (:id, :Name)")]
        void InsertObject([BindProperties] User user);

        [SqlUpdate("INSERT INTO user(id, name) VALUES (:user_id, :user_Name)")]
        void InsertObjectFull([BindProperties] User user);

        [SqlQuery("SELECT * FROM user ORDER BY name")]
        IEnumerable<User> ListUsers();

        [SqlQuery("SELECT * FROM user WHERE id = :id")]
        User GetUserById(int id);
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class DatabaseFixture : IDisposable
    {
        public DatabaseFixture()
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = ":memory:"
            }.ConnectionString;
            SqliteConnection = new SqliteConnection(connectionString);
        }

        public void Dispose()
        {
            SqliteConnection.Close();
            SqliteConnection.Dispose();
        }

        public SqliteConnection SqliteConnection { get; private set; }
    }


    public class UnitTest1 : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _databaseFixture;
        private readonly IUserDao _userDao;

        public UnitTest1(DatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture;
            _userDao = new DatabaseClientBuilder()
                .With(new DapperDatabaseConnection(databaseFixture.SqliteConnection))
                .With(new DapperGateway())
                .Build<IUserDao>();
        }

        [Fact]
        public void TestCreateTable()
        {
            // Arrange
            _userDao.CreateTable();

            // Act
            var users = _userDao.ListUsers();

            // Assert
            Assert.Empty(users);
        }

        [Fact]
        public void TestInsertObject()
        {
            // Arrange
            _userDao.CreateTable();

            // Act
            _userDao.InsertObject(new User
            {
                Id = 42,
                Name = "foobar"
            });

            var user = _userDao.GetUserById(42);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(42, user.Id);
            Assert.Equal("foobar", user.Name);
        }

        [Fact]
        public void TestInsertObjectFull()
        {
            // Arrange
            _userDao.CreateTable();

            // Act
            _userDao.InsertObjectFull(new User
            {
                Id = 42,
                Name = "foobar"
            });

            var user = _userDao.GetUserById(42);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(42, user.Id);
            Assert.Equal("foobar", user.Name);
        }

        [Fact]
        public void TestQuery()
        {
            // Arrange
            _userDao.CreateTable();
            _userDao.InsertNamed(42, "foobar");

            // Act
            var users = _userDao.ListUsers().ToList();

            // Assert
            Assert.Single(users);

            var user = users.Single();
            Assert.NotNull(user);
            Assert.Equal(42, user.Id);
            Assert.Equal("foobar", user.Name);
        }

        [Fact]
        public void TestQueryScalar()
        {
            // Arrange
            _userDao.CreateTable();
            _userDao.InsertNamed(42, "foobar");

            // Act
            var user = _userDao.GetUserById(42);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(42, user.Id);
            Assert.Equal("foobar", user.Name);
        }
    }
}