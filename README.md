# Activout Database Client
Create a database client only by defining the C# interface you want. Uses Dapper for object mapping.
[![Build Status](https://travis-ci.com/twogood/Activout.DatabaseClient.svg?branch=master)](https://travis-ci.com/twogood/Activout.DatabaseClient)
[![SonarCloud](https://sonarcloud.io/api/project_badges/measure?project=Activout.DatabaseClient&metric=sqale_rating)](https://sonarcloud.io/dashboard?id=Activout.DatabaseClient)
[![NuGet Badge](https://buildstats.info/nuget/Activout.DatabaseClient)](https://www.nuget.org/packages/Activout.DatabaseClient/)

Create a Database Access Object (DAO) defining the C# interface you want and writing the SQL query.
*Shamelessly inspired by [Jdbi Declarative API](http://jdbi.org/#_declarative_api).* 

## Rationale
The Activout Database Client provides a type-safe approach to make SQL requests to the database.

For the actual object mapping, [Dapper](https://github.com/StackExchange/Dapper) is used but another implementation can be configured.

## Example

### Synchronous

```C#
public interface IUserDao
{
    [SqlUpdate("CREATE TABLE user (id INTEGER PRIMARY KEY, name VARCHAR)")]
    void CreateTable();

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


_userDao = new DatabaseClientBuilder()
    .With(new DapperGateway(sqliteConnection))
    .Build<IUserDao>();

_userDao.CreateTable();
_userDao.InsertNamed(42, "foobar");
var user = _userDao.GetUserById(42);
```

### Asynchronous

```C#
public interface IUserDaoAsync
{
    [SqlUpdate("CREATE TABLE user (id INTEGER PRIMARY KEY, name VARCHAR)")]
    Task CreateTable();

    [SqlUpdate("INSERT INTO user(id, name) VALUES (:id, :name)")]
    Task InsertNamed([Bind("id")] int id, [Bind("name")] string name);

    [SqlUpdate("INSERT INTO user(id, name) VALUES (:id, :Name)")]
    Task InsertObject([BindProperties] User user);

    [SqlUpdate("INSERT INTO user(id, name) VALUES (:user_id, :user_Name)")]
    Task InsertObjectFull([BindProperties] User user);

    [SqlQuery("SELECT * FROM user ORDER BY name")]
    Task<IEnumerable<User>> ListUsers();

    [SqlQuery("SELECT * FROM user WHERE id = :id")]
    Task<User> GetUserById(int id);
}


_userDao = new DatabaseClientBuilder()
    .With(new DapperGateway(sqliteConnection))
    .Build<IUserDaoAsync>();

await _userDao.CreateTable();
await _userDao.InsertNamed(42, null);
var user = await _userDao.GetUserById(42);
```

## Public projects using Activout.DatabaseClient

- Your project here?

## TODO

- Positional parameters, if anyone cares?
- Make it configurable whether `:` or `@` is used to mark named parameters
- More real-life testing :)

## Collaborate
This project is still under development - participation welcome!

## About Activout
[Activout AB](http://activout.se) is a software company in Ronneby, Sweden.
