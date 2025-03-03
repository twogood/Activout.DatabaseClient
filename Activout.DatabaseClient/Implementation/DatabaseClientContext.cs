namespace Activout.DatabaseClient.Implementation
{
    public class DatabaseClientContext
    {
        private static readonly ITaskConverterFactory DefaultTaskConverterFactory = new TaskConverter3Factory();

        public IDatabaseGateway Gateway { get; set; } = null!;
        public ITaskConverterFactory TaskConverterFactory { get; set; } = DefaultTaskConverterFactory;
    }
}