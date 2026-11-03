using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Activout.DatabaseClient.Attributes;
using Activout.DatabaseClient.Dapper;
using Activout.DatabaseClient.Implementation;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Activout.DatabaseClient.Test;

public class User
{
    [Bind("id")] public int Id { get; set; }
    public string? Name { get; set; }
}

public interface IUserDao
{
    [SqlUpdate("CREATE TABLE user (id INTEGER PRIMARY KEY, name VARCHAR(255))")]
    void CreateTable();

    [SqlUpdate("INSERT INTO user(id, name) VALUES (@id, @name)")]
    void InsertNamed([Bind] int id, [Bind("name")] string? name);

    [SqlUpdate("INSERT INTO user(id, name) VALUES (@id, @Name)")]
    void InsertObject([BindProperties] User user);

    [SqlUpdate("INSERT INTO user(id, name) VALUES (@user_id, @user_Name)")]
    int InsertObjectFull([BindProperties] User user);

    [SqlUpdate("INSERT INTO user(id, name) VALUES (@id, @Name)")]
    void InsertObjectWithShared(int id, [BindProperties] User user);

    [SqlUpdate("INSERT INTO user(id, name) VALUES (@id, @Name)")]
    void InsertObjectWithSharedAfter([BindProperties] User user, int id);

    [SqlUpdate("INSERT INTO user(id, name) VALUES (@id, @Name)")]
    void InsertObjectWithAlias([Bind("user_Name")] string name, [BindProperties] User user);

    [SqlUpdate("INSERT INTO user(id, name) VALUES (@id, @Name)")]
    void InsertObjectWithCase([Bind("ID")] int id, [BindProperties] User user);

    [SqlUpdate("INSERT INTO user(id, name) VALUES (@id, @Name)")]
    void InsertObjects([BindProperties] User user, [BindProperties] User other);

    [SqlQuery("SELECT * FROM user ORDER BY name")]
    IEnumerable<User> ListUsers();

    [SqlQuery("SELECT * FROM user WHERE id = @id")]
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
            .With(new DapperGateway(sqliteConnection))
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
        Assert.Throws<ArgumentNullException>(() => _userDao.InsertObject(null!));

        // Assert
    }

    [Fact]
    public void TestBindPropertiesRejectsOverlappingNamesBeforeWriting()
    {
        _userDao.CreateTable();
        var user = new User { Id = 1, Name = "one" };

        var error = Assert.Throws<NotSupportedException>(() => _userDao.InsertObjectWithShared(99, user));
        Assert.Contains("id", error.Message);
        Assert.Throws<NotSupportedException>(() => _userDao.InsertObjectWithSharedAfter(user, 99));
        error = Assert.Throws<NotSupportedException>(() => _userDao.InsertObjectWithAlias("shared", user));
        Assert.Contains("user_Name", error.Message);
        Assert.Throws<NotSupportedException>(() => _userDao.InsertObjectWithCase(99, user));
        Assert.Throws<NotSupportedException>(() => _userDao.InsertObjects(user, user));
        Assert.Empty(_userDao.ListUsers());
    }

    [Fact]
    public void TestInsertObjectFull()
    {
        // Arrange
        _userDao.CreateTable();

        // Act
        var affectedRowCount = _userDao.InsertObjectFull(new User
        {
            Id = 42,
            Name = "foobar"
        });

        var user = _userDao.GetUserById(42);

        // Assert
        Assert.Equal(1, affectedRowCount);
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
