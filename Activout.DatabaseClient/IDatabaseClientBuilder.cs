namespace Activout.DatabaseClient;

public interface IDatabaseClientBuilder
{
    IDatabaseClientBuilder With(ITaskConverterFactory taskConverterFactory);
    IDatabaseClientBuilder With(IDatabaseGateway gateway);
    IDatabaseClientBuilder With(IDuckTyping duckTyping);
    T Build<T>() where T : class;
}