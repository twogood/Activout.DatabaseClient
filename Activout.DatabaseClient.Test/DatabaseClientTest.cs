using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Activout.DatabaseClient.Attributes;
using Activout.DatabaseClient.Dapper;
using Activout.DatabaseClient.Implementation;
using Microsoft.Data.Sqlite;
using Xunit;

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

        [SqlUpdate("syntax error")]
        void SyntaxError();
    }

    public class DatabaseClientTest
    {
        private readonly IUserDao _userDao;

        public DatabaseClientTest()
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = ":memory:"
            }.ConnectionString;
            var sqliteConnection = new SqliteConnection(connectionString);

            _userDao = new DatabaseClientBuilder()
                .With(new DuckTyping())
                .With(new DapperDatabaseConnection(sqliteConnection))
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
        public void TestInsertObjectNull()
        {
            // Arrange
            _userDao.CreateTable();

            // Act
            Assert.Throws<ArgumentNullException>(() => _userDao.InsertObject(null));

            // Assert
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
        public void TestInsertNull()
        {
            // Arrange
            _userDao.CreateTable();
            _userDao.InsertNamed(42, null);

            // Act
            var user = _userDao.GetUserById(42);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(42, user.Id);
            Assert.Null(user.Name);
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

        [Fact]
        public void TestSyntaxError()
        {
            // Arrange

            // Act + Assert
            Assert.ThrowsAny<DbException>(() => _userDao.SyntaxError());
        }
    }
}