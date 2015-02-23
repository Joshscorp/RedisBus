using RedisBus.Helpers;
using RedisBus.Messages;
using System;
using System.Threading.Tasks;

namespace RedisBus
{
    public class RedisObserver : IDisposable
    {
        #region Private members
        private readonly RedisConnection _connection;
        private readonly string _broadcastChannel;
        private readonly IRedisSerializer _serializer;
        #endregion

        #region Constructor
        public RedisObserver(RedisConnection connection, string broadcastChannel, IRedisSerializer serializer = null)
        {
            this._connection = connection;
            this._broadcastChannel = broadcastChannel;
            this._serializer = serializer ?? new BinarySerializer();
        }
        #endregion

        #region Subscribe
        public void Subscribe(Action<RedisEvent> eventHandler)
        {
            var sub = this._connection.GetSubscriber();
            sub.SubscribeAsync(this._broadcastChannel, (channel, value) =>
            {
                var redisEvent = this._serializer.Deserialize<RedisEvent>(value);
                eventHandler(redisEvent);
            });
        }

        public async Task SubscribeAsync(Action<RedisEvent> eventHandler)
        {
            var sub = this._connection.GetSubscriber();
            await sub.SubscribeAsync(this._broadcastChannel, (channel, value) =>
            {
                var redisEvent = this._serializer.Deserialize<RedisEvent>(value);
                eventHandler(redisEvent);
            });
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
