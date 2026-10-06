using ImpromptuInterface;

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
        return new DatabaseClient<T>(_gateway).ActLike<T>();
    }
}
