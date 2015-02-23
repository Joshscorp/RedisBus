using RedisBus.Helpers;
using RedisBus.Messages;
using Xunit;

namespace RedisBus.IntegrationTest
{
    public class EventBroadcast_IntegrationTest : IUseFixture<RedisFixture>
    {
        public void SetFixture(RedisFixture data)
        {

        }

        [Fact]
        public void RedisBroker_PublishToMultipleRedisObserver_Successful()
        {
            // ARRANGE
            var observersHitCount = 0;
            RedisEvent redisEvent = new RedisEvent("1", new RedisMessage("Testing"));
            RedisObserver observer = new RedisObserver(new RedisConnection("localhost"), "channel");
            observer.Subscribe((evnt) =>
            {
                observersHitCount++;
            });
            observer.Subscribe((evnt) =>
            {
                observersHitCount++;
            });
            observer.Subscribe((evnt) =>
            {
                observersHitCount++;
            });

            // ACT
            using (var broker = new RedisBroker(new RedisConnection("localhost"), "channel"))
            {
                broker.PublishAsync(redisEvent).Wait();
            }

            // ASSERT
            Assert.Equal(3, observersHitCount);
        }

        [Fact]
        public void RedisBroker_PublishToSingleRedisObserver_Successful()
        {
            // ARRANGE
            var observersHitCount = 0;
            RedisEvent redisEvent = new RedisEvent("1", new RedisMessage("Testing"));
            RedisObserver observer = new RedisObserver(new RedisConnection("localhost"), "channel");
            observer.Subscribe((evnt) =>
            {
                observersHitCount++;
            });

            // ACT
            using (var broker = new RedisBroker(new RedisConnection("localhost"), "channel"))
            {
                broker.Publish(redisEvent);
            }

            // ASSERT
            Assert.Equal(1, observersHitCount);
        }
    } 
}
