using RedisBus.Exceptions;
using StackExchange.Redis;

namespace RedisBus.Helpers
{
    public class RedisConnection
    {
        #region Local members
        private readonly ConnectionMultiplexer _connectionManager;
        private readonly ConfigurationOptions _options;
        #endregion

        #region Constructors
        public RedisConnection(string host, int port = 6379, string password = null)
        {
            _options = new ConfigurationOptions { Password = password };
            _options.EndPoints.Add(host, port);
            _options.AbortOnConnectFail = false;
            this._connectionManager = ConnectionMultiplexer.Connect(this._options);
        }

        public RedisConnection(ConnectionMultiplexer connectionMultiplexer)
        {
            this._connectionManager = connectionMultiplexer;
        }
        #endregion

        #region Public Method
        public IDatabase GetDatabase()
        {
            if (!this._connectionManager.IsConnected)
                throw new RedisConnectionClosedException();

            return this._connectionManager.GetDatabase();
        }

        public ISubscriber GetSubscriber()
        {
            if (!this._connectionManager.IsConnected)
                throw new RedisConnectionClosedException();

            return this._connectionManager.GetSubscriber();
        }

        public void CloseConnection()
        {
            this._connectionManager.Close();
        }
        #endregion
    }
}
