using RedisBus.Helpers;
using RedisBus.Messages;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RedisBus
{
    public class RedisSubscriber : IDisposable
    {
        #region Local members
        private readonly RedisConnection _connection;
        private readonly IRedisSerializer _serializer;
        private readonly string _publisherQueue;
        private readonly ConcurrentDictionary<string, Func<RedisRequest, RedisMessage>> _handlers;

        private long _receiveState;
        private volatile Task _receiveTask;

        private const int DelayTimeout = 1;
        #endregion

        #region Constructor
        public RedisSubscriber(RedisConnection connection, string publisherQueue, IRedisSerializer serializer = null)
        {
            this._connection = connection;
            this._publisherQueue = publisherQueue;
            this._serializer = serializer ?? new BinarySerializer();
            this._receiveState = 0;
            this._handlers = new ConcurrentDictionary<string, Func<RedisRequest, RedisMessage>>();
        }
        #endregion

        #region Public Methods
        public void Subscribe(string receiveQueue, Func<RedisRequest, RedisMessage> handler)
        {
            this._handlers[receiveQueue] = handler;
        }

        public bool Unsubscribe(string receiveQueue)
        {
            Func<RedisRequest, RedisMessage> handler;
            return this._handlers.TryRemove(receiveQueue, out handler);
        }

        public void UnsubscribeAll()
        {
            this._handlers.Clear();
        }

        public void StartListening()
        {
            this.EnsureIsReceiving();
        }

        public void StopListening()
        {
            this.EnsureIsNotReceiving();
        }
        #endregion

        #region Private Methods
        private void EnsureIsReceiving()
        {
            if (Interlocked.CompareExchange(ref this._receiveState, 1, 0) == 0)
            {
                this._receiveTask = Task.Factory.StartNew(() => this.BeginReceive(), TaskCreationOptions.LongRunning).Unwrap();
            }
        }

        private void EnsureIsNotReceiving()
        {
            if (Interlocked.CompareExchange(ref this._receiveState, 0, 1) == 1)
            {
                this._receiveTask.Wait();
            }
        }

        private async Task BeginReceive()
        {
            var redisDb = this._connection.GetDatabase();

            while (Interlocked.Read(ref this._receiveState) == 1)
            {
                byte[] rawRequest;
                try
                {
                    rawRequest = await redisDb.ListRightPopAsync(this._publisherQueue);
                }
                catch (RedisException)
                {
                    rawRequest = default(byte[]);
                }

                if (rawRequest == null)
                {
                    await Task.Delay(DelayTimeout);
                    continue;
                }

                await this.ExecuteRequest(rawRequest, redisDb);
            }
        }

        private async Task ExecuteRequest(byte[] redisRequestBytes, IDatabase redisDb)
        {
            await Task.Run(() =>
            {
                var redisRequest = this._serializer.Deserialize<RedisRequest>(redisRequestBytes);
                if (redisRequest == default(RedisRequest)) return;
                var handler = this._handlers[redisRequest.ReceiveQueue];
                if (handler != null)
                {
                    var response = handler(redisRequest);
                    var redisResponse = new RedisResponse(redisRequest.Id, response);
                    var redisResponseValue = this._serializer.Serialize(redisResponse);
                    redisDb.ListLeftPushAsync(redisRequest.ReceiveQueue, redisResponseValue);
                }
            });
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            this.EnsureIsNotReceiving();
        }
        #endregion
    }
}
