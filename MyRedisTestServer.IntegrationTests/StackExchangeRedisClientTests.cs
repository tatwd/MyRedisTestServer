using StackExchange.Redis;

namespace MyRedisTestServer.IntegrationTests;

public class StackExchangeRedisClientTests
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    [SetUp]
    public void SetUp()
    {
        new Thread(() =>
        {
            new RedisTestServer(TestContext.Out)
                .StartLocalAsync(6379)
                .Wait(_cancellationTokenSource.Token);
        }).Start();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _cancellationTokenSource.Cancel();
    }


    [Test]
    public void Connect_Ok()
    {
        var redis = ConnectionMultiplexer.Connect("localhost",
            TestContext.Out);

        Assert.Pass();
    }
    
    
    
}