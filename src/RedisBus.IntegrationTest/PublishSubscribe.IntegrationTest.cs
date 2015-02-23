using RedisBus.Helpers;
using RedisBus.Messages;
using Xunit;

namespace RedisBus.IntegrationTest
{
    public class RedisPublishSubscribe_IntegrationTest : IUseFixture<RedisFixture>
    {
        public void SetFixture(RedisFixture data)
        {
        }

        [Fact]
        public void RedisPublisher_PublishToSingleRedisSubscriber_Successful()
        {
            // ARRANGE
            var abc = new RedisMessage("ABC xyz");
            RedisResponse responseXyz = null;
            using (var sub = new RedisSubscriber(new RedisConnection("localhost"), "publisherQueue"))
            {
                sub.Subscribe("xyz", (request) => abc);
                sub.StartListening();

                // ACT
                using (var publisher = new RedisPublisher(new RedisConnection("localhost"), "publisherQueue"))
                {
                    responseXyz = publisher.PublishAsync("xyz", new RedisMessage("TESTMESSAGE")).Result;
                }
            }

            // ASSERT
            Assert.Equal(responseXyz.Message.Data, abc.Data);
        }

        [Fact]
        public void RedisPublisher_PublishToMultiRedisSubscribersOnDifferentChannel_Successful()
        {
            // ARRANGE
            var abc = new RedisMessage("ABC xyz");
            var def = new RedisMessage("DEF zzz");
            RedisResponse responseXyz = null;
            RedisResponse responseZzz = null;
            using (var sub = new RedisSubscriber(new RedisConnection("localhost"), "publisherQueue"))
            {
                sub.Subscribe("xyz", (request) => abc);
                sub.Subscribe("zzz", (request) => def);
                sub.StartListening();

                // ACT
                using (var publisher = new RedisPublisher(new RedisConnection("localhost"), "publisherQueue"))
                {
                    responseXyz = publisher.PublishAsync("xyz", new RedisMessage("TESTMESSAGE")).Result;
                    responseZzz = publisher.PublishAsync("zzz", new RedisMessage("TESTMESSAGE")).Result;
                }
            }

            // ASSERT
            Assert.Equal(responseXyz.Message.Data, abc.Data);
            Assert.Equal(responseZzz.Message.Data, def.Data);
        }

        [Fact]
        public void RedisSubscriber_SubscribesToSameChannel_WillOnlyBeSubscribedToLatestSubscriber()
        {
            // ARRANGE
            var abc = new RedisMessage("ABC zzz");
            var def = new RedisMessage("DEF zzz");
            RedisResponse response = null;
            using (var sub = new RedisSubscriber(new RedisConnection("localhost"), "publisherQueue"))
            {
                sub.Subscribe("zzz", (request) => abc);
                sub.Subscribe("zzz", (request) => def);
                sub.StartListening();

                // ACT
                using (var publisher = new RedisPublisher(new RedisConnection("localhost"), "publisherQueue"))
                {
                    response = publisher.PublishAsync("zzz", new RedisMessage("TESTMESSAGE")).Result;
                }
            }

            // ASSERT
            Assert.Equal(response.Message.Data, def.Data);
        }
    }
}
