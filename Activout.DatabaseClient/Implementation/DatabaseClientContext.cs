namespace Activout.DatabaseClient.Implementation
{
    public class DatabaseClientContext
    {
        public static readonly ITaskConverterFactory DefaultTaskConverterFactory = new TaskConverterFactory();

        public IDatabaseGateway Gateway { get; set; }
        public IDatabaseConnection Connection { get; set; }
        public ITaskConverterFactory TaskConverterFactory { get; set; } = DefaultTaskConverterFactory;
    }
}