namespace Activout.DatabaseClient.Implementation;

/// <summary>The default <see cref="IDatabaseClientBuilder"/>.</summary>
public class DatabaseClientBuilder : IDatabaseClientBuilder
{
    private IDatabaseGateway _gateway = null!;

    /// <inheritdoc />
    public IDatabaseClientBuilder With(IDatabaseGateway gateway)
    {
        _gateway = gateway;
        return this;
    }

    /// <inheritdoc />
    public T Build<T>() where T : class
    {
        return DatabaseClient.Create<T>(_gateway);
    }
}
