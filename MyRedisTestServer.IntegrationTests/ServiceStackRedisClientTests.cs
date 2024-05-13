using System.Text;
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
    
    
    [Test]
    public void Set_Utf8Bytes_Ok()
    {
        var redis = new RedisClient("localhost");

        const string json = "{\"foo\":\"bar木桐单元测试公司\"}";
        var bytes = Encoding.UTF8.GetBytes(json);
        
        var isOk = redis.Set("hello", bytes, TimeSpan.FromSeconds(3600));
        
        Assert.That(isOk, Is.True);
    }
}