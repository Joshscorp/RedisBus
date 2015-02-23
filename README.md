# RedisBus
Redis based simple bus messaging system in .NET

This was created to aid local debugging/integration tests that runs really fast in memory (REDIS) in place of an actual bus implementation.  It supports both Broadcast and Request/Response messaging styles as well as multichannel support to broadcast on different channels.  It was created to solve a distributed solution based on the CQRS architecture/pattern.  Many solutions implemented an in-memory bus instead on that very same thread, this is to solve that same thread issue for myself.

It uses StackExchange.Redis to interact with Redis.  Please check out https://github.com/Joshscorp/Redis2Go on how to spin up a Redis instance in .NET for ease of debugging/testing with no installation/knowledge of Redis required.

Check out the Included Tests for more implementation ideas.

## Broadcast

We have 2 different classes, **RedisObserver** and **RedisBroker**, you *broadcast* through the **RedisBroker**, you *subscribe* through the **RedisObserver**.

Connection to redis, default is localhost, and if no port is provided, the default 6379 will be used, it accepts password as well should you require to set one, specialChannelA is the name of the channel to listen to
```
RedisObserver observer = new RedisObserver(new RedisConnection("localhost"), 
                                          "specialchannelA");
                                          
observer.Subscribe((evnt) =>
{
    // React to event here
    // evnt will contain eventId and Message (Object)
});                                          

// Can have more subscribers
observer2.Subscribe((evnt) =>
{
    // React to event here
    // evnt will contain eventId and Message (Object)
}); 
```
EventId, can be any id you wish, RedisMessage can include any objects in here, but must be marked Serializable.
```
RedisEvent redisEvent = new RedisEvent("EventId1", 
                          new RedisMessage("This is a test message")); 
                          
using (var broker = new RedisBroker(new RedisConnection("localhost"), "specialchannelA"))
{
    await broker.PublishAsync(redisEvent); // This will broadcast to the channel to all subscribers
}
```

## Request / Response
Any object will do as the payload. request contains id, Message (RedisMessage), ReceiveQueueName

Can also return exception in redismessage, constructor overload, request should contain the "TestMessageToSend" payload
```
var message = new RedisMessage("This is a response message"); 
var sub = new RedisSubscriber(new RedisConnection("localhost"), "specialQueueNameToPublish");
sub.Subscribe("specialQueueNameToReceive", (request) => 
{
    return message;
});
sub.StartListening();
```
Publisher
```
var publisher = new RedisPublisher(new RedisConnection("localhost"), "specialQueueNameToPublish");
var responseMessageReceived = await publisher.PublishAsync("specialQueueNameToReceive", new RedisMessage("TestMessageToSend"));
```

By default, it is using binary serialisation, but you are welcome to extend the class as you see fit.

That's all folks!
