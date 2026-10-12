using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Activout.DatabaseClient.Attributes;
using Activout.DatabaseClient.Dapper;
using Activout.DatabaseClient.Implementation;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Activout.DatabaseClient.Test;

public interface ICollectionDao
{
    [SqlUpdate("CREATE TABLE user (id INTEGER PRIMARY KEY, name VARCHAR(255))")]
    void CreateTable();

    [SqlUpdate("INSERT INTO user(id, name) VALUES (@id, @name)")]
    void Insert([Bind("id")] int id, [Bind("name")] string? name);

    [SqlQuery("SELECT * FROM user ORDER BY id")] IEnumerable<User> AsEnumerable();
    [SqlQuery("SELECT * FROM user ORDER BY id")] IReadOnlyCollection<User> AsReadOnlyCollection();
    [SqlQuery("SELECT * FROM user ORDER BY id")] IReadOnlyList<User> AsReadOnlyList();
    [SqlQuery("SELECT * FROM user ORDER BY id")] ICollection<User> AsCollection();
    [SqlQuery("SELECT * FROM user ORDER BY id")] IList<User> AsIList();
    [SqlQuery("SELECT * FROM user ORDER BY id")] List<User> AsList();
    [SqlQuery("SELECT * FROM user ORDER BY id")] User[] AsArray();

    [SqlQuery("SELECT * FROM user ORDER BY id")] Task<IEnumerable<User>> AsEnumerableAsync();
    [SqlQuery("SELECT * FROM user ORDER BY id")] Task<IReadOnlyCollection<User>> AsReadOnlyCollectionAsync();
    [SqlQuery("SELECT * FROM user ORDER BY id")] Task<IReadOnlyList<User>> AsReadOnlyListAsync();
    [SqlQuery("SELECT * FROM user ORDER BY id")] Task<ICollection<User>> AsCollectionAsync();
    [SqlQuery("SELECT * FROM user ORDER BY id")] Task<IList<User>> AsIListAsync();
    [SqlQuery("SELECT * FROM user ORDER BY id")] Task<List<User>> AsListAsync();
    [SqlQuery("SELECT * FROM user ORDER BY id")] Task<User[]> AsArrayAsync();

    [SqlQuery("SELECT id FROM user ORDER BY id")] List<long> Ids();
    [SqlQuery("SELECT name FROM user ORDER BY id")] Task<string?[]> NamesAsync();
    [SqlQuery("SELECT id FROM user ORDER BY id")] Task<IReadOnlyList<long?>> NullableIdsAsync();
}

public interface IUnsupportedCollectionDao
{
    [SqlQuery("SELECT * FROM user")] HashSet<User> AsHashSet();
}

public class CollectionReturnTypesTest
{
    private static ICollectionDao Create(bool seed)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        var dao = new DatabaseClientBuilder().With(new DapperGateway(connection)).Build<ICollectionDao>();
        dao.CreateTable();
        if (seed)
        {
            dao.Insert(1, "a");
            dao.Insert(2, null);
        }

        return dao;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AllCollectionTypes(bool seed)
    {
        var dao = Create(seed);
        var expected = seed ? new[] { 1, 2 } : [];
        var expectedIds = expected.Select(i => (long)i).ToArray();

        Assert.Equal(expected, dao.AsEnumerable().Select(u => u.Id));
        Assert.Equal(expected, dao.AsReadOnlyCollection().Select(u => u.Id));
        Assert.Equal(expected, dao.AsReadOnlyList().Select(u => u.Id));
        Assert.Equal(expected, dao.AsCollection().Select(u => u.Id));
        Assert.Equal(expected, dao.AsIList().Select(u => u.Id));
        Assert.Equal(expected, dao.AsList().Select(u => u.Id));
        Assert.Equal(expected, dao.AsArray().Select(u => u.Id));

        Assert.Equal(expected, (await dao.AsEnumerableAsync()).Select(u => u.Id));
        Assert.Equal(expected, (await dao.AsReadOnlyCollectionAsync()).Select(u => u.Id));
        Assert.Equal(expected, (await dao.AsReadOnlyListAsync()).Select(u => u.Id));
        Assert.Equal(expected, (await dao.AsCollectionAsync()).Select(u => u.Id));
        Assert.Equal(expected, (await dao.AsIListAsync()).Select(u => u.Id));
        Assert.Equal(expected, (await dao.AsListAsync()).Select(u => u.Id));
        Assert.Equal(expected, (await dao.AsArrayAsync()).Select(u => u.Id));
        Assert.Equal(expectedIds, dao.Ids());
    }

    [Fact]
    public async Task NullableElements()
    {
        var dao = Create(true);

        Assert.Equal(new string?[] { "a", null }, await dao.NamesAsync());
        Assert.Equal([1L, 2L], await dao.NullableIdsAsync());
    }

    [Fact]
    public void UnsupportedCollectionIsRejected()
    {
        var gateway = new DapperGateway(new SqliteConnection("Data Source=:memory:"));
        var dao = new DatabaseClientBuilder().With(gateway).Build<IUnsupportedCollectionDao>();
        Assert.Throws<NotSupportedException>(() => dao.AsHashSet());
    }
}
