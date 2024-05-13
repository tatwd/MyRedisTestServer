using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MyRedisTestServer;

public class RedisTestServer
{
    private readonly TextWriter _log;

    private readonly ConcurrentDictionary<string, IRedisCmdHandler> _redisCmdHandlers;

    private readonly TcpListener _listener;

    public RedisTestServer(int port, TextWriter? log = null)
    {
        log ??= Console.Out;
        _log = log;

        _redisCmdHandlers = new ConcurrentDictionary<string, IRedisCmdHandler>(StringComparer.OrdinalIgnoreCase);
        
        var ipEndPoint = new IPEndPoint(IPAddress.Loopback, port);
        _listener = new TcpListener(ipEndPoint);
    }

    public RedisTestServer AddRedisCmdHandler(string cmdType, IRedisCmdHandler handler)
    {
        _redisCmdHandlers.TryAdd(cmdType, handler);
        return this;
    }

    public void Stop()
    {
        _listener.Stop();
    }
    
    public Task StartAsync()
    {
        return StartAsyncInternal();
    }

    private async Task StartAsyncInternal()
    {
        try
        {
            _listener.Start();
            Log("Hello, RedisTestServer!");

            while (true)
            {
                var handler = await _listener.AcceptTcpClientAsync();
                Log("Connected! client: " + handler.Client.RemoteEndPoint);
                
                var task = Task.Factory.StartNew(() =>
                {
                    AcceptHandler(handler);
                });

                await task;
            }
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
        finally
        {
            _listener.Stop();
            Log("Stop finished!");
        }
    }

    private async void // 必须使用 async void 
        AcceptHandler(TcpClient handler)
    {
        while (true)
        {
            try
            {
                var stream = handler.GetStream();
                var response = await Task.Factory.StartNew(() => ReadToEnd(stream));
                Log("响应: " + response);

                var sendBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(sendBytes, 0, sendBytes.Length);

            }
            catch (System.ArgumentException ex)
            {
                Log(ex.ToString());
                return;
            }
            catch (Exception exception)
            {
                Log(exception.GetType().FullName  + ": " + exception.Message);
                return;
            }
        }
    }
    
    private string ReadToEnd(Stream stream)
    {
        var sr = new StreamReader(stream, Encoding.UTF8);

        var request = RespReadWriter.Read(sr);
        Log("请求(raw): " + request);

        var result = (object[])RespReadWriter.Parse(request); // client request is always array
        var clientReqArgs = ToStringArray(result);
        
        Log("请求: " + Output(clientReqArgs));

        var cmdType = clientReqArgs[0];

        if (_redisCmdHandlers.TryGetValue(cmdType, out var handler))
        {
            return handler.Handle(clientReqArgs);
        }

        if (cmdType.Equals("TiME", StringComparison.OrdinalIgnoreCase))
        {
            return CmdTime();
        }

        if (cmdType.Equals("PING", StringComparison.OrdinalIgnoreCase))
        {
            return CmdPing(clientReqArgs);
        }

        if (cmdType.Equals("ECHO", StringComparison.OrdinalIgnoreCase)) 
        {
            return CmdEcho(clientReqArgs);
        }
        
        if (cmdType.Equals("CONFIG", StringComparison.OrdinalIgnoreCase)
            && "GET".Equals(clientReqArgs[1], StringComparison.OrdinalIgnoreCase)) 
        {
            return CmdConfigGet(clientReqArgs[2]);
        }

        if (FakeResponseForSpecialCmd.TryGetValue(cmdType, out var fakeResponse)) 
        {
            return fakeResponse;
        }

        return RespTypeBuilder.Inline("OK");
    }

    private string[] ToStringArray(object[] args)
    {
        var outArgs = new string[args.Length];

        for (var i = 0; i < args.Length; i++)
        {
            outArgs[i] = args[i].ToString()!;
        }
        
        return outArgs;
    }

    private static string CmdTime()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var timestamp = utcNow.ToUnixTimeSeconds().ToString();
        var msOfDay = utcNow.TimeOfDay.TotalMilliseconds.ToString("0.");
        return RespTypeBuilder.Strings(timestamp, msOfDay);
    }

    private static string CmdPing(string[] req)
    {
        if (req.Length == 1)
        {
            return RespTypeBuilder.Inline("PONG");
        }

        return RespTypeBuilder.String(req[1]);
    }

    private static string CmdEcho(string[] req) 
    {
        return RespTypeBuilder.String(req[1]);
    }

    private static string CmdConfigGet(string configName)
    {
        if (FakeRedisConfig.TryGetValue(configName, out var val))
        {
            return val;
        }

        return RespTypeBuilder.String("");
    }
    
    private static readonly Dictionary<string, string> FakeRedisConfig = new (StringComparer.OrdinalIgnoreCase)
    {
        ["slave-read-only"] = RespTypeBuilder.String("yes"),
        ["databases"] = RespTypeBuilder.String("2"),
    };
    

    private static readonly Dictionary<string, string> FakeResponseForSpecialCmd = new (StringComparer.OrdinalIgnoreCase)
    {
        ["EXISTS"] = RespTypeBuilder.Int(0),
        ["TTL"] = RespTypeBuilder.Int(-1),
        ["LLEN"] = RespTypeBuilder.Int(0),
        ["GET"] = RespTypeBuilder.Nil(),
        ["HGET"] = RespTypeBuilder.NilResp3(),
        ["LPOP"] = RespTypeBuilder.NilResp3(),
        ["INFO"] = RespTypeBuilder.String(""),
    };

    // TODO: need a ILogger
    private void Log(string msg)
    {
        var log = new StringBuilder()
            .AppendFormat("{0} [{1,2}] ", DateTime.Now, Environment.CurrentManagedThreadId)
            // .Append(msg)
            .Append(msg.Replace("\r\n",@"\r\n"))
            .AppendLine()
            .ToString();

        _log.Write(log);
    }

    private static string Output(object obj)
    {
        if (obj is IDictionary<object, object> dict)
        {
            var sb = new StringBuilder(128)
                .Append("{");

            foreach (var kv in dict)
            {
                if (sb.Length > 1)
                {
                    sb.Append(",");
                }
                
                sb.Append(Output(kv.Key));
                sb.Append(":");
                sb.Append(Output(kv.Value));
            }

            sb.Append("}");
            return sb.ToString();
        }

        if (obj is ICollection arr)
        {
            var sb = new StringBuilder(128)
                .Append("[");

            foreach (var item in arr)
            {
                if (sb.Length > 1)
                {
                    sb.Append(", ");
                }
                sb.Append(Output(item));
            }

            sb.Append("]");
            return sb.ToString();
        }

        if (obj is string s)
        {
            return $"`{s}`";
        }
        
        return obj?.ToString() ?? "null";
    }
    
}