using System;
using System.Data;
using Activout.DatabaseClient.Dapper;
using Activout.DatabaseClient.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Activout.DatabaseClient.Dapper.Extensions.DependencyInjection;

/// <summary>Dependency injection extensions for Activout.DatabaseClient.Dapper.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the DAO interface <typeparamref name="TDao"/> over a <see cref="DapperGateway"/> using the
    /// <see cref="IDbConnection"/> registered in the container.
    /// </summary>
    /// <remarks>
    /// Register the <see cref="IDbConnection"/> as scoped. All DAOs resolved in the same scope then share one
    /// connection, which is required to pass a transaction from one DAO to another. The container owns and disposes
    /// the connection; <see cref="DapperGateway"/> opens it if it is closed.
    /// </remarks>
    /// <typeparam name="TDao">The DAO interface.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="parameterPrefix">The prefix for SQL parameter names, such as <c>@</c> or <c>:</c>.</param>
    /// <param name="lifetime">The DAO lifetime. Defaults to <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddDapperDatabaseClient<TDao>(this IServiceCollection services,
        string parameterPrefix = "@",
        ServiceLifetime lifetime = ServiceLifetime.Scoped) where TDao : class
    {
        return services.AddDatabaseClient<TDao>(
            sp => new DapperGateway(sp.GetRequiredService<IDbConnection>(), parameterPrefix),
            lifetime);
    }
}
