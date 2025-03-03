using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace Activout.DatabaseClient.Dapper;

public class DapperGateway : IDatabaseGateway
{
    private readonly IDbConnection _dbConnection;
    private readonly string _parameterPrefix;

    public DapperGateway(IDbConnection dbConnection, string parameterPrefix = "@")
    {
        _dbConnection = dbConnection;
        _parameterPrefix = parameterPrefix;
        if (_dbConnection.State == ConnectionState.Closed)
        {
            _dbConnection.Open();
        }
    }

    public async Task<int> ExecuteAsync(SqlStatement statement)
    {
        return await _dbConnection.ExecuteAsync(statement.Sql,
                GetDynamicParameters(statement))
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<object>> QueryAsync(SqlStatement statement)
    {
        return await _dbConnection
            .QueryAsync(statement.EffectiveType, statement.Sql,
                GetDynamicParameters(statement))
            .ConfigureAwait(false);
    }

    public async Task<object> QueryFirstOrDefaultAsync(SqlStatement statement)
    {
        return await _dbConnection.QueryFirstOrDefaultAsync(statement.EffectiveType, statement.Sql,
                GetDynamicParameters(statement))
            .ConfigureAwait(false);
    }

    DynamicParameters GetDynamicParameters(SqlStatement statement)
    {
        var dynamicParameters = new DynamicParameters();
        foreach (var p in statement.Parameters)
        {
            dynamicParameters.Add(_parameterPrefix + p.Name, p.Value);
        }

        return dynamicParameters;
    }


}