using RedisBus.Helpers;
using RedisBus.Messages;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedisBus
{
    public class RedisPublisher : IDisposable
    {
        #region Local members
        private readonly RedisConnection _connection;
        private readonly string _publisherQueue;
        private readonly IRedisSerializer _serializer;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<RedisResponse>> _requestCallbacks;

        private long _nextId;
        private long _receiveState;
        private volatile Task _receiveTask;

        const int DelayTimeout = 1;
        #endregion

        #region Constructor
        public RedisPublisher(RedisConnection connection, string publisherQueue, IRedisSerializer serializer = null)
        {
            this._connection = connection;
            this._publisherQueue = publisherQueue;
            this._serializer = serializer ?? new BinarySerializer();
            this._nextId = 0;
            this._receiveState = 0;
            this._requestCallbacks = new ConcurrentDictionary<string, TaskCompletionSource<RedisResponse>>();
        }
        #endregion

        #region Public method
        public Task<RedisResponse> PublishAsync(string receiveQueue, RedisMessage requestMessage, CancellationToken token = default(CancellationToken))
        {
            this.EnsureIsNotReceiving();
            this.EnsureIsReceiving(receiveQueue);

            var requestId = this.NextId();
            var redisRequest = new RedisRequest(receiveQueue, requestId, requestMessage);
            var redisRequestBytes = this._serializer.Serialize(redisRequest);

            var redis = this._connection.GetDatabase();
            var lpushTask = redis.ListLeftPushAsync(this._publisherQueue, redisRequestBytes);

            var callback = new TaskCompletionSource<RedisResponse>();
            this._requestCallbacks[requestId] = callback;

            if (token != CancellationToken.None)
                token.Register(() => this.OnRequestCancelled(requestId));

            lpushTask.ContinueWith(t =>
            {
                if (lpushTask.Exception != null)
                    this.OnRequestError(requestId, lpushTask.Exception.InnerException);
            });

            return callback.Task;
        }
        #endregion

        #region Private Methods
        private async Task Receive(string receiveQueue)
        {
            var redis = this._connection.GetDatabase();
            while (Interlocked.Read(ref this._receiveState) == 1)
            {
                byte[] rawResponse;
                try
                {
                    rawResponse = await redis.ListRightPopAsync(receiveQueue);
                }
                catch (RedisException)
                {
                    rawResponse = default(byte[]);
                }

                if (rawResponse == null)
                {
                    await Task.Delay(DelayTimeout);
                    // Maybe increase delayTimeout a little bit?
                    continue;
                }

                var redisResponseBytes = rawResponse;

                await this.ReturnResponse(redisResponseBytes);
            }
        }

        private async Task ReturnResponse(byte[] redisResponseBytes)
        {
            await Task.Run(() =>
            {
                RedisResponse redisResponse = null;
                try
                {
                    redisResponse = this._serializer.Deserialize<RedisResponse>(redisResponseBytes);
                }
                catch (Exception ex)
                {
                    redisResponse = new RedisResponse("0", new RedisMessage(ex));
                }
                finally
                {
                    if (redisResponse != null)
                    {
                        TaskCompletionSource<RedisResponse> callback;
                        if (this._requestCallbacks.TryRemove(redisResponse.RequestId, out callback))
                        {
                            callback.SetResult(redisResponse);
                        }
                    }
                }
            });
        }

        private string NextId()
        {
            return Interlocked.Increment(ref this._nextId).ToString(CultureInfo.InvariantCulture);
        }

        private void OnRequestError(string requestId, Exception exception)
        {
            TaskCompletionSource<RedisResponse> callback;
            if (this._requestCallbacks.TryRemove(requestId, out callback))
            {
                callback.TrySetException(exception);
            }
        }

        private void OnRequestCancelled(string requestId)
        {
            TaskCompletionSource<RedisResponse> _;
            this._requestCallbacks.TryRemove(requestId, out _);
        }

        private void EnsureIsReceiving(string receiveQueue)
        {
            if (Interlocked.CompareExchange(ref this._receiveState, 1, 0) == 0)
            {
                this._receiveTask = Task.Factory.StartNew(() => this.Receive(receiveQueue), TaskCreationOptions.LongRunning).Unwrap();
            }
        }

        private void EnsureIsNotReceiving()
        {
            if (Interlocked.CompareExchange(ref this._receiveState, 0, 1) == 1)
            {
                this._receiveTask.Wait();
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            this.EnsureIsNotReceiving();
            this._requestCallbacks.Clear();
        }
        #endregion
    }
}
