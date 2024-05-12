using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MyRedisTestServer
{
    public class RedisTestServer
    {

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
                    Log("Connected! client: " + handler.Client.RemoteEndPoint.ToString());
                    
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
        
        private static async Task<string> ReadToEnd2(Stream stream)
        {
            var myReadBuffer = new byte[1024];
            var numberOfBytesRead = await stream.ReadAsync(myReadBuffer, 0, myReadBuffer.Length);

            var request = Encoding.UTF8.GetString(myReadBuffer, 0, numberOfBytesRead);
            Log("请求: " + request);

            if (request.Contains(Cmd("PING"))) 
                return "+PONG\r\n";

            if (request.Contains(Cmd("EXISTS")))
                return ":0\r\n";

            if (IsReadCmd(request))
                return "_\r\n"; // 读取指令 什么也不返回
           
            return "+OK\r\n";
        }

        private static bool IsReadCmd(string request) 
        {
            return _readCmds.Any(cmd => request.Contains(cmd));
        }

        private static readonly string[] _readCmds = new string[]
        {
            Cmd("GET"),
            Cmd("MGET"),
        };

        private static string Cmd(string cmdStr) 
        {
            return $"${cmdStr.Length}\r\n{cmdStr.ToUpper()}\r\n";
        }

        // TODO: need a ILogger
        private static void Log(string msg)
        {
            var log = new StringBuilder()
                .AppendFormat("{0} [{1,2}] ", DateTime.Now, Environment.CurrentManagedThreadId)
                .Append(msg.Replace("\r\n","\\r\\n"))
                .AppendLine()
                .ToString();

            Console.Write(log);
        }
    }
}