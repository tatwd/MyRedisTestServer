namespace MyRedisTestServer;

public interface IRedisCmdHandler
{
    /// <summary>
    /// 处理客户端 redis 指令请求
    /// </summary>
    /// <param name="clientRequestArgs"></param>
    /// <returns></returns>
    string Handle(string[] clientRequestArgs);
}