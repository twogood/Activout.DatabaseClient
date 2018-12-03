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

        internal static IDbConnection GetDbConnection(this SqlStatement statement)
        {
            return ((DapperDatabaseConnection) statement.Connection).DbConnection;
        }
    }

    public class DapperDatabaseConnection : IDatabaseConnection
    {
        public DapperDatabaseConnection(IDbConnection dbConnection)
        {
            DbConnection = dbConnection;
            DbConnection.Open();
        }

        public IDbConnection DbConnection { get; }
    }

    public class DapperGateway : IDatabaseGateway
    {
        public async Task<int> ExecuteAsync(SqlStatement statement)
        {
            var connection = statement.GetDbConnection();
            return await connection.ExecuteAsync(statement.Sql, statement.GetDynamicParameters()).ConfigureAwait(false);
        }

        public async Task<IEnumerable<object>> QueryAsync(SqlStatement statement)
        {
            using (var connection = statement.GetDbConnection())
            {
                return await connection
                    .QueryAsync(statement.EffectiveType, statement.Sql, statement.GetDynamicParameters())
                    .ConfigureAwait(false);
            }
        }

        public async Task<object> QueryFirstOrDefaultAsync(SqlStatement statement)
        {
            using (var connection = statement.GetDbConnection())
            {
                return await connection.QueryFirstOrDefaultAsync(statement.EffectiveType, statement.Sql,
                    statement.GetDynamicParameters()).ConfigureAwait(false);
            }
        }
    }
}