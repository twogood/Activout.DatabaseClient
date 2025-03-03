# Activout Database Client
Create a database client only by defining the C# interface you want. Uses Dapper for object mapping.

Create a Database Access Object (DAO) defining the C# interface you want and writing the SQL query.
*Shamelessly inspired by [Jdbi Declarative API](http://jdbi.org/#_declarative_api).* 

## Rationale
The Activout Database Client provides a type-safe approach to make SQL requests to the database.

For the actual object mapping, [Dapper](https://github.com/StackExchange/Dapper) is used but another implementation can be configured.

## Example

```C#
public interface IUserDaoAsync
{
    [SqlUpdate("CREATE TABLE user (id INTEGER PRIMARY KEY, name VARCHAR)")]
    Task CreateTable();

    [SqlUpdate("INSERT INTO user(id, name) VALUES (@id, @name)")]
    Task InsertNamed([Bind("id")] int id, [Bind("name")] string name);

    [SqlUpdate("INSERT INTO user(id, name) VALUES (@id, @name)")]
    Task InsertObject([BindProperties] User user);

    [SqlUpdate("INSERT INTO user(id, name) VALUES (@user_id, @user_name)")]
    Task InsertObjectFull([BindProperties] User user);

    [SqlQuery("SELECT * FROM user ORDER BY name")]
    Task<IEnumerable<User>> ListUsers();

    [SqlQuery("SELECT * FROM user WHERE id = @id")]
    Task<User> GetUserById(int id);
}


_userDao = new DatabaseClientBuilder()
    .With(new DapperGateway(sqliteConnection))
    .Build<IUserDaoAsync>();

await _userDao.CreateTable();
await _userDao.InsertNamed(42, null);
var user = await _userDao.GetUserById(42);
```

Full example code in 
[DatabaseClientAsyncTest.cs](https://github.com/twogood/Activout.DatabaseClient/blob/main/Activout.DatabaseClient.Test/DatabaseClientAsyncTest.cs).

## Example with dependency injection

```C#

// Connection string example for MySQL:
// "Server=example.com;Database=example;User=example;Password=example;AutoEnlist=True;Pooling=True;ConnectionReset=True;CharSet=utf8mb4;"
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddScoped<IDbConnection>(_ => new MySqlConnection(connectionString));

builder.Services.AddScoped<DatabaseClientBuilder>();
builder.Services.AddScoped<IDatabaseClient>(sp => sp
    .GetRequiredService<DatabaseClientBuilder>()
    .With(new DapperGateway(sp.GetRequiredService<IDbConnection>()))
    .Build<IDatabaseClient>());

builder.Services.AddTransient<SomeService>();

// ...

public class SomeService(IDatabaseClient databaseClient)
{
    // ...
}


```


## Collaborate
This project is still under development - participation welcome!

## Related projects

- [Activout.RestClient](https://github.com/twogood/Activout.RestClient) - Create a REST(ish) API client only by defining the C# interface you want.

## About Activout
[Activout AB](http://activout.se) is a software company in Ronneby, Sweden.
