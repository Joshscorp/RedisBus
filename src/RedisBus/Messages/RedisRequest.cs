using System;

namespace RedisBus.Messages
{
    [Serializable]
    public class RedisRequest
    {
        public RedisRequest(string receiveQueue, string id, RedisMessage message)
        {
            this.ReceiveQueue = receiveQueue;
            this.Id = id;
            this.Message = message;
        }

        public string Id { get; private set; }
        public string ReceiveQueue { get; private set; }
        public RedisMessage Message { get; private set; }
    }
}
