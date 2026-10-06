namespace Activout.DatabaseClient;

public interface IDatabaseClientBuilder
{
    IDatabaseClientBuilder With(IDatabaseGateway gateway);
    T Build<T>() where T : class;
}