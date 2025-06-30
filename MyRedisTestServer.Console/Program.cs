using System.Collections.Concurrent;
using MyRedisTestServer;

var cmdHandler = new MyTestRedisCmdHandler();

var redisServer = new RedisTestServer(6379);

redisServer.AddRedisCmdHandler("GET", cmdHandler)
    .AddRedisCmdHandler("MGET", cmdHandler)
    .AddRedisCmdHandler("EVAL", cmdHandler)
    .AddRedisCmdHandler("DEL", cmdHandler);

redisServer.DebugMode(true).StartAsync().Wait();


internal class MyTestRedisCmdHandler : MyRedisTestServer.IRedisCmdHandler
{
    private readonly ConcurrentDictionary<string, string> _fakeKv = new ConcurrentDictionary<string, string>();

    private readonly ConcurrentDictionary<string, string> _evalKv = new ConcurrentDictionary<string, string>();

    public void SetupRedisGet(string key, string val)
    {
        _fakeKv[key] = val;
    }

    public void SetupRedisEval(string[] keys, string val)
    {
        _evalKv[$"{keys.Length},{string.Join(",", keys)}"] = val.ToString();
    }

    public string Handle(string[] clientRequestArgs)
    {
        var cmdType = clientRequestArgs[0];

        if (cmdType.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            var key = clientRequestArgs[1];
            return HandleGET(key);
        }

        if (cmdType.Equals("MGET", StringComparison.OrdinalIgnoreCase))
        {
            return HandleMGET(clientRequestArgs);
        }

        if (cmdType.Equals("EVAL", StringComparison.OrdinalIgnoreCase))
        {
            return HandleEVAL(clientRequestArgs);
        }

        if (cmdType.Equals("DEL", StringComparison.OrdinalIgnoreCase))
        {
            return RespTypeBuilder.Int(clientRequestArgs.Length - 1);
        }

        return MyRedisTestServer.RespTypeBuilder.Nil();
    }

    private string HandleGET(string key)
    {
        if (_fakeKv.TryGetValue(key, out var val))
        {
            return MyRedisTestServer.RespTypeBuilder.String(val);
        }

        return MyRedisTestServer.RespTypeBuilder.Nil();
    }

    private string HandleMGET(string[] reqArgs)
    {
        //string[] outValues = new string[reqArgs.Length - 1];

        //for (var i = 1; i < reqArgs.Length; i++)
        //{
        //    outValues[i] = HandleGET(reqArgs[i]);
        //}

        return MyRedisTestServer.RespTypeBuilder.NilList();
    }


    private string HandleEVAL(string[] clientRequestArgs)
    {
        var len = clientRequestArgs[2];
        var key = string.Join(",", clientRequestArgs.Skip(2).Take(int.Parse(len) + 1));
        
        if (_evalKv.TryGetValue(key, out var val))
        {
            return MyRedisTestServer.RespTypeBuilder.String(val);
        }

        return MyRedisTestServer.RespTypeBuilder.Nil();
    }

}