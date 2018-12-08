namespace Activout.DatabaseClient.Implementation
{
    public class DatabaseClientContext
    {
        private static readonly ITaskConverterFactory DefaultTaskConverterFactory = new TaskConverterFactory();

        public IDatabaseGateway Gateway { get; set; }
        public ITaskConverterFactory TaskConverterFactory { get; set; } = DefaultTaskConverterFactory;
    }
}