namespace Activout.DatabaseClient.Implementation;

public class DatabaseClientBuilder : IDatabaseClientBuilder
{
    private IDatabaseGateway _gateway = null!;

    public IDatabaseClientBuilder With(IDatabaseGateway gateway)
    {
        _gateway = gateway;
        return this;
    }

    public T Build<T>() where T : class
    {
        return DatabaseClient.Create<T>(_gateway);
    }
}
