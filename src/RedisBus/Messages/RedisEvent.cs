using System;

namespace RedisBus.Messages
{
    [Serializable]
    public class RedisEvent
    {
        public RedisEvent(string eventId, RedisMessage message)
        {
            this.EventId = eventId;
            this.Message = message;
        }

        public string EventId { get; private set; }
        public RedisMessage Message { get; private set; }
    }
}
