﻿using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MyRedisTestServer;

public class RedisTestServer
{
    private readonly TextWriter _log;

    public RedisTestServer(TextWriter? log = null)
    {
        log ??= Console.Out;
        _log = log;
    }
    
    public Task StartLocalAsync(int port)
    {
        var ipEndPoint = new IPEndPoint(IPAddress.Loopback, port);
        return StartAsync(ipEndPoint);
    }

    private async Task StartAsync(IPEndPoint ipEndPoint)
    {

        TcpListener listener = new TcpListener(ipEndPoint);

        try
        {
            listener.Start();
            Log("Hello, RedisTestServer!");

            while (true)
            {
                var handler = await listener.AcceptTcpClientAsync();
                Log("Connected! client: " + handler.Client.RemoteEndPoint);
                
                var task = Task.Factory.StartNew(() =>
                {
                    AcceptHandler2(handler);
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
            listener.Stop();
            Log("Stop finished!");
        }
    }

    private async void // 必须使用 async void 
        AcceptHandler2(TcpClient handler)
    {
        while (true)
        {
            try
            {
                var stream = handler.GetStream();
                var response = await ReadToEnd2(stream);
                Log("响应: " + response);

                var sendBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(sendBytes, 0, sendBytes.Length);

            }
            catch (Exception exception)
            {
                Log(exception.Message);
                return;
            }
        }
    }
    
    private async Task<string> ReadToEnd2(Stream stream)
    {
        var myReadBuffer = new byte[4096];
        var numberOfBytesRead = await stream.ReadAsync(myReadBuffer, 0, myReadBuffer.Length);

        var request = Encoding.Default.GetString(myReadBuffer, 0, numberOfBytesRead);
        Log("请求: " + request);
        
        var result = RespReadWriter.Parse(request);
        Log("请求Parsed:" + Output(result));

        // TODO: need a RESP writer

        if (request.Contains(Cmd("PING"))) 
            return RespTypeBuilder.Inline("PONG");

        if (request.Contains(Cmd("EXISTS")))
            return RespTypeBuilder.Int(0);

        // if (IsReadCmd(request))
        //     return "_\r\n"; // 读取指令 什么也不返回
        
        return RespTypeBuilder.Inline("OK");
    }

    // private static bool IsReadCmd(string request) 
    // {
    //     return ReadCmdList.Any(request.Contains);
    // }

    // private static readonly string[] ReadCmdList = new string[]
    // {
    //     Cmd("GET"),
    //     Cmd("MGET"),
    // };

    private static string Cmd(string cmdStr) 
    {
        return $"${cmdStr.Length}\r\n{cmdStr.ToUpper()}\r\n";
    }

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
                    sb.Append(",");
                }
                sb.Append(Output(item));
            }

            sb.Append("]");
            return sb.ToString();
        }

        if (obj is string s)
        {
            return $"\"{s}\"";
        }
        
        return obj.ToString();
    }
    
}