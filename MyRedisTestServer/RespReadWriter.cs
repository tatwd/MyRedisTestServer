using System.Text;

namespace MyRedisTestServer;

public static class RespReadWriter
{
    private const string ErrProtocol = "unsupported protocol";
    private const string ErrUnexpected = "not what you asked for";
    
    public static string[] ReadArray(string str)
    {
        using var stringReader = new StringReader(str);

        var line = CheckAndReadLine(stringReader);

        int elems;
        
        switch (line[0])
        {
            case '*': // array
            case '>': // push data
            case '~': // set
                elems = ReadLength(line);
                break;
            case '%': // maps
                var length2 = ReadLength(line);
                elems = length2 * 2;
                break;
            
            default:
                throw new NotSupportedException(ErrProtocol);
        }

        var res = new string[elems];
        for (var i = 0; i < elems; i++)
        {
            var next = Read(stringReader);
            res[i] = next;
        }
        
        return res;
    }

    public static string ReadString(string str)
    {
        using var stringReader = new StringReader(str);
        var line = CheckAndReadLine(stringReader);
    
        switch (line[0])
        {
            case '$':
                var length1 = ReadLength(line);
                if (length1 < 0)
                {
                    // -1 is a nil response
                    return line;
                }

                var buff = stringReader.ReadLine();
                return buff!;

            default:
                throw new NotSupportedException(ErrProtocol);
        }
    }

    public static string[] ReadStrings(string str)
    {
        var items = ReadArray(str);
        
        var res = new string[items.Length];
        for (var i = 0; i < items.Length; i++)
        {
            res[i] = ReadString(items[i]);
        }

        return res;
    }
    
    // Read a single command, returning it raw. Used to read replies from redis.
    // Understands RESP3 proto.
    public static string Read(TextReader textReader)
    {
        var line = CheckAndReadLine(textReader);

        switch (line[0])
        {
            case '+': // inline string
            case '-': // error
            case ':': // int
            case ',': //float
            case '_': //null
                return line;

            case '$':
            {
                // bulk strings are: `$5\r\nhello\r\n`
                var length = ReadLength(line); // expect `\r\n`
                if (length < 0)
                {
                    // -1 is a nil response
                    return line;
                }

                var buff = ReadLineWithCrlf(textReader, length + 2); // 2 is `\r\n`
                return line + buff;
            }
            case '*': // arrays are: `*6\r\n...`
            case '>': // pushdata is: `>6\r\n...`
            case '~': // sets are: `~6\r\n...`
            {
                var length = ReadLength(line);

                var sb = new StringBuilder();
                sb.Append(line);

                for (var i = 0; i < length; i++)
                {
                    var next = Read(textReader);
                    sb.Append(next);
                }

                return sb.ToString();
            }
            case '%':
            {
                var length = ReadLength(line);
                var sb = new StringBuilder();
                sb.Append(line);
                
                for (var i = 0; i < length * 2; i++)
                {
                    var next = Read(textReader);
                    sb.Append(next);
                }
                return sb.ToString();
            }
            
            default:
                throw new NotSupportedException(ErrProtocol + "(" + line[0] +  ")");
        }
    }

    public static string ReadError(string str)
    {
        if (str.Length < 1 || str[0] != '-')
        {
            throw new ArgumentException(ErrUnexpected);
        }

        return ReadInline(str);
    }
    

    private static string ReadInline(string str)
    {
        if (str.Length < 3)
        {
            throw new ArgumentException(ErrUnexpected);
        }

        return str.Substring(1, str.Length - 3);
    }
    
    private static int ReadLength(string line)
    {
        return int.Parse(line.Substring(1, line.Length - 3));
    }
    
    private static string CheckAndReadLine(TextReader textReader)
    {
        var line = ReadLineWithCrlf(textReader);

        if (line.Length < 3)
        {
            throw new NotSupportedException(ErrProtocol);
        }

        return line.ToString();
    }
    
    private static StringBuilder ReadLineWithCrlf(TextReader textReader)
    {
        var line = new StringBuilder(128);

        var hasNext = TryReadNextChar(textReader, out var next);

        while (hasNext)
        {
            line.Append(next);

            if (next == '\n')
            {
                break;
            }

            hasNext = TryReadNextChar(textReader, out next);
        }

        return line;
    }

    private static StringBuilder ReadLineWithCrlf(TextReader textReader, int byteLength)
    {
        var line = new StringBuilder(128);

        var hasNext = TryReadNextChar(textReader, out var next);
        var i = 0;
        var tmp = new char[1];

        while (hasNext)
        {
            line.Append(next);
            tmp[0] = next; 
            i += Encoding.UTF8.GetByteCount(tmp); // char maybe UTF8

            // if (next == '\n')
            if (i == byteLength)
            {
                break;
            }

            hasNext = TryReadNextChar(textReader, out next);
        }

        return line;
    }

    private static bool TryReadNextChar(TextReader reader, out char next)
    {
        next = default;
        var c = reader.Read();

        if (c == -1)
        {
            return false;
        }

        next = (char)c;
        return true;

    }


    public static void Write(TextWriter writer, string[] cmdList)
    {
        writer.Write($"*{cmdList.Length}\r\n");

        foreach (var cmd in cmdList)
        {
            writer.Write($"${cmd.Length}\r\n{cmd}\r\n");
        }
    }

    // exactly a single command (which can be nested).
    public static object Parse(string? str)
    {
        if (str is null || str.Length < 1)
        {
            throw new ArgumentException(ErrUnexpected);
        }

        switch (str[0])
        {
            case '+':
                return ReadInline(str);
            case '-':
                var err = ReadInline(str);
                throw new Exception(err);
            case ':':
                var e = ReadInline(str);
                return int.Parse(e);
            case '$':
                return ReadString(str);
            case '*':
            {
                var elems = ReadArray(str);
                var res = new object[elems.Length];

                for (var i = 0; i < elems.Length; i++)
                {
                    res[i] = Parse(elems[i]);
                }

                return res;
            }
            case '%':
            {
                var elems = ReadArray(str);
                var res = new Dictionary<object, object>();

                for (var i = 0; i + 1 < elems.Length; i += 2)
                {
                    var key = Parse(elems[i]);
                    var val = Parse(elems[i + 1]);
                    res[key] = val;
                }

                return res;
            }
            default:
                throw new NotSupportedException(ErrProtocol);
        }
        
    }
    
    
    
}