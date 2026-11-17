using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Activout.DatabaseClient.Attributes;
using Activout.DatabaseClient.Implementation;
using Activout.DatabaseClient.LibSql;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Activout.DatabaseClient.Test;

public enum Colour
{
    Red,
    Green
}

public class Thing
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? ParentId { get; set; }
    public bool Active { get; set; }
    public Colour Colour { get; set; }
}

public interface IThingDao : IWithTransactions
{
    [SqlUpdate("""
               CREATE TABLE thing (id TEXT PRIMARY KEY, name TEXT NOT NULL, created_at TEXT NOT NULL,
                   deleted_at TEXT, parent_id TEXT, active INTEGER NOT NULL, colour TEXT NOT NULL)
               """)]
    Task CreateTable();

    [SqlUpdate("""
               INSERT INTO thing (id, name, created_at, deleted_at, parent_id, active, colour)
               VALUES (@id, @name, @created_at, @deleted_at, @parent_id, @active, @colour)
               ON CONFLICT (id) DO NOTHING
               """)]
    Task<int> Insert([Bind("id")] Guid id, [Bind("name")] string name, [Bind("created_at")] DateTime createdAt,
        [Bind("deleted_at")] DateTime? deletedAt, [Bind("parent_id")] Guid? parentId, [Bind("active")] bool active,
        [Bind("colour")] Colour colour, IDbTransaction? transaction = null);

    [SqlQuery("""
              SELECT id, name, created_at AS CreatedAt, deleted_at AS DeletedAt, parent_id AS ParentId, active, colour
              FROM thing WHERE id = @id
              """)]
    Task<Thing?> Find([Bind("id")] Guid id);

    [SqlQuery("SELECT name FROM thing ORDER BY name")]
    Task<IEnumerable<string>> ListNames();

    [SqlQuery("SELECT COUNT(*) FROM thing")]
    Task<long> Count();
}

public class LibSqlGatewayTest
{
    private readonly IThingDao _dao = new DatabaseClientBuilder()
        .With(new LibSqlGateway(LibSqlConnectionFactory.Create("Data Source=:memory:")))
        .Build<IThingDao>();

    [Fact]
    public async Task RoundTripsGuidDateTimeBoolAndEnum()
    {
        await _dao.CreateTable();
        var id = Guid.NewGuid();
        var parent = Guid.NewGuid();
        var created = new DateTime(2026, 12, 1, 23, 59, 59, 123, DateTimeKind.Utc);

        await _dao.Insert(id, "a", created, null, parent, true, Colour.Green);
        var thing = await _dao.Find(id);

        Assert.NotNull(thing);
        Assert.Equal(id, thing.Id);
        Assert.Equal(created, thing.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, thing.CreatedAt.Kind);
        Assert.Null(thing.DeletedAt);
        Assert.Equal(parent, thing.ParentId);
        Assert.True(thing.Active);
        Assert.Equal(Colour.Green, thing.Colour);
    }

    [Fact]
    public async Task ReturnsAffectedRowsSoConflictsCanBeDetected()
    {
        await _dao.CreateTable();
        var id = Guid.NewGuid();

        Assert.Equal(1, await _dao.Insert(id, "a", DateTime.UtcNow, null, null, false, Colour.Red));
        Assert.Equal(0, await _dao.Insert(id, "a", DateTime.UtcNow, null, null, false, Colour.Red));
    }

    [Fact]
    public async Task MapsScalarsAndEmptyResults()
    {
        await _dao.CreateTable();
        Assert.Empty(await _dao.ListNames());
        Assert.Null(await _dao.Find(Guid.NewGuid()));

        await _dao.Insert(Guid.NewGuid(), "b", DateTime.UtcNow, null, null, false, Colour.Red);
        await _dao.Insert(Guid.NewGuid(), "a", DateTime.UtcNow, null, null, false, Colour.Red);

        Assert.Equal(["a", "b"], await _dao.ListNames());
        Assert.Equal(2, await _dao.Count());
    }

    [Fact]
    public async Task RollsBackTransactions()
    {
        await _dao.CreateTable();
        using (var transaction = _dao.BeginTransaction())
        {
            await _dao.Insert(Guid.NewGuid(), "a", DateTime.UtcNow, null, null, false, Colour.Red, transaction);
            transaction.Rollback();
        }

        Assert.Equal(0, await _dao.Count());
    }

    [Theory]
    [InlineData("libsql://db.example.com", true)]
    [InlineData("Data Source=https://db.example.com;Auth Token=x", true)]
    [InlineData("Data Source=game.db", false)]
    public void RecognisesRemoteConnectionStrings(string connectionString, bool remote)
    {
        Assert.Equal(remote, LibSqlConnectionFactory.IsRemote(connectionString));
    }

    [Fact]
    public void CreatesSqliteConnectionForLocalFiles()
    {
        Assert.IsType<SqliteConnection>(LibSqlConnectionFactory.Create("Data Source=:memory:"));
    }
}
