namespace Activout.DatabaseClient;

/// <summary>
/// Builds an implementation of a DAO interface whose methods are annotated with
/// <see cref="Attributes.SqlQueryAttribute"/> or <see cref="Attributes.SqlUpdateAttribute"/>.
/// </summary>
public interface IDatabaseClientBuilder
{
    /// <summary>Sets the gateway that executes the SQL.</summary>
    /// <param name="gateway">The gateway, for example <c>DapperGateway</c>.</param>
    /// <returns>This builder.</returns>
    IDatabaseClientBuilder With(IDatabaseGateway gateway);

    /// <summary>Creates an implementation of the DAO interface <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The DAO interface.</typeparam>
    /// <returns>The DAO implementation.</returns>
    T Build<T>() where T : class;
}
