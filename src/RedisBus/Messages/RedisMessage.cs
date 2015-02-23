using System;

namespace RedisBus.Messages
{
    [Serializable]
    public class RedisMessage
    {
        #region Constructor
        public RedisMessage(object data)
        {
            this.Data = data;
            this.Exception = null;
        }

        public RedisMessage(Exception error)
        {
            this.Data = null;
            this.Exception = error;
        }
        #endregion

        public object Data { get; private set; }
        public Exception Exception { get; private set; }
    }
}
