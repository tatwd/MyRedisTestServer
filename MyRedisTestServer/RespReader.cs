using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyRedisTestServer;

public struct Error
{
    public readonly string type;
    public readonly string message;

    public Error(string type, string message)
    {
        this.type = type.ToUpper();
        this.message = message;
    }
}


public struct RespResult
{
    public RespResultType Type { get; set; }
    public object Result { get; set; }

    public RespResult(RespResultType type, object result)
    {
        Type = type;
        Result = result;
    }
}

public enum RespResultType
{
    String = 0,
    Error = 1,
    Number = 2,
    Strings = 3,
    Array = 4,
}



public class RespReader
{
    private TextReader reader;

    public RespReader(string str)
    {
        reader = new StringReader(str);
    }

    public RespReader(Stream stream)
    {
        reader = new StreamReader(stream);
    }

    public RespResult Read()
    {
        throw new NotImplementedException();
    }
}