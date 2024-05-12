using ServiceStack.Redis;

namespace MyRedisTestServer.IntegrationTests;

public class ServiceStackRedisClientTests : TestBase
{
    [Test]
    public void Set_Ok()
    {
        var redis = new RedisClient("localhost");

        redis.Set("hello", 1);
        redis.Set("foo", "bar\r\nbaz");
        
        Assert.Pass();
    }
}