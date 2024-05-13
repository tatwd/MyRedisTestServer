namespace MyRedisTestServer.IntegrationTests;

public class TestBase
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    [SetUp]
    public void SetUp()
    {
        new Thread(() =>
        {
            try {
                new RedisTestServer(TestContext.Out)
                    .StartLocalAsync(6379)
                    .Wait(_cancellationTokenSource.Token);
            } catch {
            }
        }).Start();
    }
    
    [TearDown]
    public void TearDown()
    {
        _cancellationTokenSource.Cancel();
    }
}