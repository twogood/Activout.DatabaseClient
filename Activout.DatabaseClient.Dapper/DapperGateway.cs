using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace Activout.DatabaseClient.Dapper;

/// <summary>An <see cref="IDatabaseGateway"/> that uses Dapper.</summary>
public class DapperGateway : IDatabaseGateway
{
    private readonly IDbConnection _dbConnection;
    private readonly string _parameterPrefix;

    /// <summary>Creates the gateway, opening the connection if it is closed.</summary>
    /// <param name="dbConnection">The database connection.</param>
    /// <param name="parameterPrefix">The prefix for SQL parameter names, such as <c>@</c> or <c>:</c>.</param>
    public DapperGateway(IDbConnection dbConnection, string parameterPrefix = "@")
    {
        _dbConnection = dbConnection;
        _parameterPrefix = parameterPrefix;
        if (_dbConnection.State == ConnectionState.Closed)
        {
            _dbConnection.Open();
        }
    }

    /// <inheritdoc />
    public IDbTransaction BeginTransaction(IsolationLevel isolationLevel)
    {
        return _dbConnection.BeginTransaction(isolationLevel);
    }

    /// <inheritdoc />
    public async Task<int> ExecuteAsync(SqlStatement statement)
    {
        return await _dbConnection.ExecuteAsync(statement.Sql,
                GetDynamicParameters(statement), statement.Transaction)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<object>> QueryAsync(SqlStatement statement)
    {
        return await _dbConnection
            .QueryAsync(statement.EffectiveType, statement.Sql,
                GetDynamicParameters(statement), statement.Transaction)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<object?> QueryFirstOrDefaultAsync(SqlStatement statement)
    {
        return await _dbConnection.QueryFirstOrDefaultAsync(statement.EffectiveType, statement.Sql,
                GetDynamicParameters(statement), statement.Transaction)
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