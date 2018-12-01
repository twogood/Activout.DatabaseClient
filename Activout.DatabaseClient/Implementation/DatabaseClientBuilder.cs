using System;

namespace Activout.DatabaseClient.Implementation
{
    public class DatabaseClientBuilder : IDatabaseClientBuilder
    {
        private IDuckTyping _duckTyping = new DuckTyping();
        private readonly DatabaseClientContext _context = new DatabaseClientContext();

        public IDatabaseClientBuilder With(IDatabaseConnection connection)
        {
            _context.Connection = connection;
            return this;
        }

        public IDatabaseClientBuilder With(IDatabaseGateway gateway)
        {
            _context.Gateway = gateway;
            return this;
        }

        public IDatabaseClientBuilder With(IDuckTyping duckTyping)
        {
            _duckTyping = duckTyping ?? throw new ArgumentNullException(nameof(duckTyping));
            return this;
        }

        public T Build<T>() where T : class
        {
            var client = new DatabaseClient<T>(_context);
            return _duckTyping.DuckType<T>(client);
        }
    }
}