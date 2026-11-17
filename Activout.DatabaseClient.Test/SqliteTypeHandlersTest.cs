using System;
using System.Threading.Tasks;
using Activout.DatabaseClient.Dapper;
using Activout.DatabaseClient.Implementation;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Activout.DatabaseClient.Test;

public class SqliteTypeHandlersTest
{
    [Fact]
    public async Task DapperGatewayReadsGuidAndUtcDateTimeStoredAsText()
    {
        SqliteTypeHandlers.Register();
        var dao = new DatabaseClientBuilder()
            .With(new DapperGateway(new SqliteConnection("Data Source=:memory:")))
            .Build<IThingDao>();
        await dao.CreateTable();
        var id = Guid.NewGuid();
        var parent = Guid.NewGuid();
        var created = new DateTime(2026, 12, 1, 23, 59, 59, 123, DateTimeKind.Utc);
        await dao.Insert(id, "a", created, created, parent, true, Colour.Green);

        var thing = Assert.Single(await dao.All());

        Assert.Equal(id, thing.Id);
        Assert.Equal(created, thing.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, thing.CreatedAt.Kind);
        Assert.Equal(created, thing.DeletedAt);
        Assert.Equal(parent, thing.ParentId);
    }

    [Fact]
    public void ToTextTreatsUnspecifiedKindAsUtc() =>
        Assert.Equal("2026-01-01 12:00:00.000", SqliteTypeHandlers.ToText(new DateTime(2026, 1, 1, 12, 0, 0)));

    [Fact]
    public void ToTextDropsSubMillisecondTicks() =>
        Assert.Equal("2026-01-01 12:00:00.123",
            SqliteTypeHandlers.ToText(new DateTime(2026, 1, 1, 12, 0, 0, 123, DateTimeKind.Utc).AddTicks(4567)));
}
