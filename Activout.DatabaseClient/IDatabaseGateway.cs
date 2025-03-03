using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Activout.DatabaseClient
{
    public record QueryParameter(string Name, object? Value);

    public class SqlStatement
    {
        public required string Sql { get; init; }
        public IList<QueryParameter> Parameters { get; } = new List<QueryParameter>();
        public required Type EffectiveType { get; init; }
    }

    public interface IDatabaseGateway
    {
        Task<int> ExecuteAsync(SqlStatement statement);
        Task<IEnumerable<object>> QueryAsync(SqlStatement statement);
        Task<object?> QueryFirstOrDefaultAsync(SqlStatement statement);
    }
}