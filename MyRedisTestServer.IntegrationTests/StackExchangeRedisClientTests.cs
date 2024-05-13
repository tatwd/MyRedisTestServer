using StackExchange.Redis;
using StackExchange.Redis.Configuration;

namespace MyRedisTestServer.IntegrationTests;

public class StackExchangeRedisClientTests : TestBase
{
    [Test]
    public void Connect_Ok()
    {

        var options = ConfigurationOptions.Parse(
            "localhost,abortConnect=true,$PING=,$SENTINEL=,$CLUSTER=,$INFO=,$CLIENT=,$ECHO=,$SUBSCRIBE=");

        //LoggingTunnel.LogToDirectory(options, @"d:\tmp\RedisLog"); // <=== added!

        var redis = ConnectionMultiplexer.Connect(options,
            TestContext.Out);

        var db = redis.GetDatabase();

        db.StringSet("foo", "bar");

        Assert.Pass();
    }
}