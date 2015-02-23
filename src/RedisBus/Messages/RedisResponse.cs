using System;

namespace RedisBus.Messages
{
    [Serializable]
    public class RedisResponse
    {
        public RedisResponse(string requestId, RedisMessage message)
        {
            this.RequestId = requestId;
            this.Message = message;
        }

        public string RequestId { get; private set; }
        public RedisMessage Message { get; private set; }
    }
}
