using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Activout.DatabaseClient
{
    public class QueryParameter
    {
        public QueryParameter(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public object Value { get; }
    }

    public class SqlStatement
    {
        public string Sql { get; set; }
        public IList<QueryParameter> Parameters { get; } = new List<QueryParameter>();
        public Type EffectiveType { get; set; }
    }

    public interface IDatabaseGateway
    {
        Task<int> ExecuteAsync(SqlStatement statement);
        Task<IEnumerable<object>> QueryAsync(SqlStatement statement);
        Task<object> QueryFirstAsync(SqlStatement statement);
        Task<object> QueryFirstOrDefaultAsync(SqlStatement statement);
    }
}