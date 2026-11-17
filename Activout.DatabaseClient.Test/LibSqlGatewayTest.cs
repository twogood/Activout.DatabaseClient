using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Activout.DatabaseClient.Attributes;
using Activout.DatabaseClient.Dapper;
using Activout.DatabaseClient.Implementation;
using Activout.DatabaseClient.LibSql;
using Microsoft.Data.Sqlite;
using Nelknet.LibSQL.Data;
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

    [SqlQuery("""
              SELECT id, name, created_at AS CreatedAt, deleted_at AS DeletedAt, parent_id AS ParentId, active, colour
              FROM thing
              """)]
    Task<IEnumerable<Thing>> All();

    [SqlQuery("SELECT name FROM thing ORDER BY name")]
    Task<IEnumerable<string>> ListNames();

    [SqlQuery("SELECT COUNT(*) FROM thing")]
    Task<long> Count();

    [SqlUpdate("INSERT INTO thing (id, name, created_at, active, colour) VALUES ('not-a-guid', 'z', '', 0, 'Red')")]
    Task InsertUnparsableRow();

    [SqlQuery("SELECT id FROM thing ORDER BY name")]
    Task<Guid?> FirstId();

    [SqlQuery("SELECT 1 AS value UNION ALL SELECT 1.5")]
    Task<IEnumerable<object>> MixedTypes();

    [SqlQuery("SELECT 'first' AS name, 'second' AS name")]
    Task<Thing?> DuplicateColumnNames();
}

/// <summary>Runs the tests on the native libSQL engine, the same provider as production.</summary>
public class LibSqlGatewayOnLibSqlTest() : LibSqlGatewayTest(new LibSQLConnection("Data Source=:memory:"));

/// <summary>Runs the tests on Microsoft.Data.Sqlite, which callers may use for their own tests.</summary>
public class LibSqlGatewayOnSqliteTest() : LibSqlGatewayTest(new SqliteConnection("Data Source=:memory:"))
{
    [Fact]
    public async Task DapperGatewayReadsWhatLibSqlGatewayWrote()
    {
        await Dao.CreateTable();
        var id = Guid.NewGuid();
        var parent = Guid.NewGuid();
        var created = new DateTime(2026, 12, 1, 23, 59, 59, 123, DateTimeKind.Utc);
        await Dao.Insert(id, "a", created, created, parent, true, Colour.Green);

        var dapperDao = new DatabaseClientBuilder().With(new DapperGateway(Connection)).Build<IThingDao>();
        var thing = Assert.Single(await dapperDao.All());

        Assert.Equal(id, thing.Id);
        Assert.Equal(created, thing.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, thing.CreatedAt.Kind);
        Assert.Equal(created, thing.DeletedAt);
        Assert.Equal(parent, thing.ParentId);
        Assert.True(thing.Active);
        Assert.Equal(Colour.Green, thing.Colour);
    }
}

public abstract class LibSqlGatewayTest
{
    protected readonly DbConnection Connection;
    protected readonly IThingDao Dao;

    protected LibSqlGatewayTest(DbConnection connection)
    {
        SqliteTypeHandlers.Register();
        Connection = connection;
        Dao = new DatabaseClientBuilder().With(new LibSqlGateway(connection)).Build<IThingDao>();
    }

    [Fact]
    public async Task RoundTripsGuidDateTimeBoolAndEnum()
    {
        await Dao.CreateTable();
        var id = Guid.NewGuid();
        var parent = Guid.NewGuid();
        var created = new DateTime(2026, 12, 1, 23, 59, 59, 123, DateTimeKind.Utc);

        await Dao.Insert(id, "a", created, null, parent, true, Colour.Green);
        var thing = await Dao.Find(id);

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
        await Dao.CreateTable();
        var id = Guid.NewGuid();

        Assert.Equal(1, await Dao.Insert(id, "a", DateTime.UtcNow, null, null, false, Colour.Red));
        Assert.Equal(0, await Dao.Insert(id, "a", DateTime.UtcNow, null, null, false, Colour.Red));
    }

    [Fact]
    public async Task MapsScalarsAndEmptyResults()
    {
        await Dao.CreateTable();
        Assert.Empty(await Dao.ListNames());
        Assert.Null(await Dao.Find(Guid.NewGuid()));

        await Dao.Insert(Guid.NewGuid(), "b", DateTime.UtcNow, null, null, false, Colour.Red);
        await Dao.Insert(Guid.NewGuid(), "a", DateTime.UtcNow, null, null, false, Colour.Red);

        Assert.Equal(["a", "b"], await Dao.ListNames());
        Assert.Equal(2, await Dao.Count());
    }

    [Fact]
    public async Task RollsBackTransactions()
    {
        await Dao.CreateTable();
        using (var transaction = Dao.BeginTransaction())
        {
            await Dao.Insert(Guid.NewGuid(), "a", DateTime.UtcNow, null, null, false, Colour.Red, transaction);
            transaction.Rollback();
        }

        Assert.Equal(0, await Dao.Count());
    }

    [Fact]
    public async Task KeepsEachValueWhenAColumnHasMixedTypes()
    {
        var values = (await Dao.MixedTypes()).Select(row => ((IDictionary<string, object>)row)["value"]);
        Assert.Equal(new object[] { 1L, 1.5 }, values);
    }

    [Fact]
    public async Task MapsDuplicateColumnNamesLikeDapperGateway()
    {
        var thing = await Dao.DuplicateColumnNames();
        Assert.Equal("second", thing?.Name);
    }

    [Fact]
    public async Task FirstOrDefaultReadsOnlyTheFirstRow()
    {
        await Dao.CreateTable();
        var id = Guid.NewGuid();
        await Dao.Insert(id, "a", DateTime.UtcNow, null, null, false, Colour.Red);
        await Dao.InsertUnparsableRow();

        Assert.Equal(id, await Dao.FirstId());
    }
}
