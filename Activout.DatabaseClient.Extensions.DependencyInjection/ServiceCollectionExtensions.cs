using System;
using Activout.DatabaseClient.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Activout.DatabaseClient.Extensions.DependencyInjection;

/// <summary>Dependency injection extensions for Activout.DatabaseClient.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the DAO interface <typeparamref name="TDao"/>, implemented over the gateway created by
    /// <paramref name="gatewayFactory"/>.
    /// </summary>
    /// <remarks>
    /// All DAOs that participate in one transaction must use gateways over the same connection. Make the gateway
    /// factory resolve a scoped connection, and keep the default <see cref="ServiceLifetime.Scoped"/> lifetime.
    /// </remarks>
    /// <typeparam name="TDao">The DAO interface.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="gatewayFactory">Creates the gateway for the DAO.</param>
    /// <param name="lifetime">The DAO lifetime. Defaults to <see cref="ServiceLifetime.Scoped"/>.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddDatabaseClient<TDao>(this IServiceCollection services,
        Func<IServiceProvider, IDatabaseGateway> gatewayFactory,
        ServiceLifetime lifetime = ServiceLifetime.Scoped) where TDao : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(gatewayFactory);

        services.Add(new ServiceDescriptor(typeof(TDao),
            sp => new DatabaseClientBuilder().With(gatewayFactory(sp)).Build<TDao>(),
            lifetime));
        return services;
    }
}
