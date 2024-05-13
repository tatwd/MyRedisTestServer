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
                new RedisTestServer(6379, TestContext.Out)
                    .StartAsync()
                    .Wait(_cancellationTokenSource.Token);
            }
            catch
            {
                // ignored
            }
        }).Start();
    }
    
    [TearDown]
    public void TearDown()
    {
        _cancellationTokenSource.Cancel();
    }
}