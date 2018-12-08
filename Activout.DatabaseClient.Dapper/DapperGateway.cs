using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace Activout.DatabaseClient.Dapper
{
    internal static class Extensions
    {
        internal static DynamicParameters GetDynamicParameters(this SqlStatement statement)
        {
            var dynamicParameters = new DynamicParameters();
            foreach (var p in statement.Parameters)
            {
                dynamicParameters.Add(":" + p.Name, p.Value);
            }

            return dynamicParameters;
        }
    }

    public class DapperGateway : IDatabaseGateway
    {
        private readonly IDbConnection _dbConnection;

        public DapperGateway(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
            _dbConnection.Open();
        }

        public async Task<int> ExecuteAsync(SqlStatement statement)
        {
            return await _dbConnection.ExecuteAsync(statement.Sql, statement.GetDynamicParameters())
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<object>> QueryAsync(SqlStatement statement)
        {
            return await _dbConnection
                .QueryAsync(statement.EffectiveType, statement.Sql, statement.GetDynamicParameters())
                .ConfigureAwait(false);
        }

        public async Task<object> QueryFirstOrDefaultAsync(SqlStatement statement)
        {
            return await _dbConnection.QueryFirstOrDefaultAsync(statement.EffectiveType, statement.Sql,
                statement.GetDynamicParameters()).ConfigureAwait(false);
        }
    }
}