using StackExchange.Redis;

namespace RedisBus
{
    public interface IRedisSerializer
    {
        RedisValue Serialize<T>(T obj);
        T Deserialize<T>(RedisValue obj);
    }
}
