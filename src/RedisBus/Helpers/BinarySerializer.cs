using StackExchange.Redis;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace RedisBus.Helpers
{
    public class BinarySerializer : IRedisSerializer
    {
        public RedisValue Serialize<T>(T obj)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, obj);
                return stream.GetBuffer();
            }
        }

        public T Deserialize<T>(RedisValue obj)
        {
            var objBytes = (byte[])obj;
            if (objBytes != null)
            {
                var formatter = new BinaryFormatter();
                using (var stream = new MemoryStream(objBytes))
                {
                    return (T)formatter.Deserialize(stream);
                }
            }
            return default(T);
        }
    }
}
