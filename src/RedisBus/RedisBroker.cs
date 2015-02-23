using RedisBus.Exceptions;
using RedisBus.Helpers;
using RedisBus.Messages;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace RedisBus
{
    public class RedisBroker : IDisposable
    {
        #region Private members
        private readonly RedisConnection _connection;
        private readonly string _broadcastChannel;
        private readonly IRedisSerializer _serializer;
        #endregion

        #region Constructor
        public RedisBroker(RedisConnection connection, string broadcastChannel, IRedisSerializer serializer = null)
        {
            this._connection = connection;
            this._broadcastChannel = broadcastChannel;
            this._serializer = serializer ?? new BinarySerializer();
        }
        #endregion

        #region Public Methods
        public void Publish(RedisEvent redisEvent)
        {
            if (redisEvent == null)
                throw new ArgumentNullException("redisEvent");

            var redisValue = this._serializer.Serialize(redisEvent);

            var subscriber = this._connection.GetSubscriber();
            if (subscriber.IsConnected(this._broadcastChannel))
                subscriber.Publish(this._broadcastChannel, redisValue, CommandFlags.FireAndForget);
            else
            {
                throw new RedisConnectionClosedException();
            }
        }

        public async Task PublishAsync(RedisEvent redisEvent)
        {
            if (redisEvent == null)
                throw new ArgumentNullException("redisEvent");

            var redisValue = this._serializer.Serialize(redisEvent);

            var subscriber = this._connection.GetSubscriber();
            if (subscriber.IsConnected(this._broadcastChannel))
                await subscriber.PublishAsync(this._broadcastChannel, redisValue, CommandFlags.FireAndForget);
        }
        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            this._connection.CloseConnection();
        }
        #endregion
    }
}
