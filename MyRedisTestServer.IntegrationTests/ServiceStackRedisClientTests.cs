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

    [Test]
    public void Eval_Utf8Bytes_Ok()
    {
        var redis = new RedisClient("localhost");

        const string luaScript = @"local key = KEYS[1]
local seconds = tonumber(ARGV[1])

local count = redis.call('incr', key)

if count == 1 then
  redis.call('expire', key, seconds)
end

return count";
        var key = Encoding.UTF8.GetBytes("foo");
        var expire = Encoding.UTF8.GetBytes("3600");
        
        var isOk = redis.Eval(luaScript, 2, new[]{ key, expire });
        
        Assert.That(isOk, Is.True);
    }

}