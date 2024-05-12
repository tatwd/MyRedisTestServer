using StackExchange.Redis;

namespace MyRedisTestServer.IntegrationTests;

public class StackExchangeRedisClientTests : TestBase
{
    [Test]
    public void Connect_Ok()
    {
        var redis = ConnectionMultiplexer.Connect("localhost",
            TestContext.Out);

        Assert.Pass();
    }
}