namespace MyRedisTestServer.IntegrationTests;

public class TestBase
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
    
    [TearDown]
    public void TearDown()
    {
        _cancellationTokenSource.Cancel();
    }
}