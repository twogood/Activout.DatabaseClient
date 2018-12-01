namespace Activout.DatabaseClient.Implementation
{
    public class DatabaseClientContext
    {
        public IDatabaseGateway Gateway { get; set; }
        public IDatabaseConnection Connection { get; set; }
    }
}