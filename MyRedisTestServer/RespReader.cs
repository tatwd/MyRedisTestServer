using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyRedisTestServer
{
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

        public string Display()
        {
            switch (Type)
            {
                case RespResultType.String:
                    return Result.ToString();
                
                case RespResultType.Error:
                    return Result.ToString();

                case RespResultType.Number:
                    return Result.ToString();

                case RespResultType.Strings:
                    return string.Join(",", (ICollection)Result);

                case RespResultType.Array:
                    return string.Join(",", (ICollection)Result);

                default:
                    throw new ArgumentOutOfRangeException();
            }
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

        public object Read()
        {
            switch (reader.Read())
            {
                case '+':
                    return reader.ReadLine();
                case '-':
                    {
                        char[] sep = { ' ' };
                        string[] split = reader.ReadLine().Split(sep, 2);
                        return new Error(split[0], split[1]);
                    }
                case ':':
                    return int.Parse(reader.ReadLine());
                case '$':
                    {
                        int length = int.Parse(reader.ReadLine());
                        if (length < 0)
                        {
                            return null;
                        }

                        char[] buf = new char[length];
                        reader.ReadBlock(buf, 0, length);
                        reader.ReadLine();
                        return (new StringBuilder()).Append(buf).ToString();
                    }
                case '*':
                    {
                        int length = int.Parse(reader.ReadLine());
                        if (length < 0)
                        {
                            return null;
                        }

                        object[] arr = new object[length];
                        for (int i = 0; i < length; i++)
                        {
                            arr[i] = this.Read();
                        }
                        return arr;
                    }
                case -1:
                    // TODO(schoon) - Find a more useful EOF indicator.
                    throw new EndOfStreamException();
                default:
                    return this.Read();
            }
        }

        public async Task<RespResult> ReadAsync()
        {
            char[] next = new char[1];
            if (await reader.ReadAsync(next, 0, 1) == 0)
            {
                // TODO(schoon) - Find a more useful EOF indicator.
                throw new EndOfStreamException();
            }

            switch (next[0])
            {
                case '+':
                    var val =  await reader.ReadLineAsync();
                    return new RespResult(RespResultType.String, val);
                    
                case '-':
                    char[] sep = { ' ' };
                    string line = await reader.ReadLineAsync();
                    string[] split = line.Split(sep, 2);
                    var error = new Error(split[0], split[1]);
                    return new RespResult(RespResultType.Error, error);

                case ':':
                    var num = int.Parse(await reader.ReadLineAsync());
                    return new RespResult(RespResultType.Number, num);

                
                case '$':
                    int length1 = int.Parse(await reader.ReadLineAsync());
                    if (length1 < 0)
                    {
                        return new RespResult(RespResultType.Strings, null);
                    }

                    char[] buf = new char[length1];
                    await reader.ReadBlockAsync(buf, 0, length1);
                    await reader.ReadLineAsync();
                    var str = (new StringBuilder()).Append(buf).ToString();
                    return new RespResult(RespResultType.Strings, str);
                    
                case '*':
                    int length = int.Parse(await reader.ReadLineAsync());
                    if (length < 0)
                    {
                        return new RespResult(RespResultType.Array, null);
                    }

                    object[] arr = new object[length];
                    for (int i = 0; i < length; i++)
                    {
                        arr[i] = await this.ReadAsync();
                    }
                    return new RespResult(RespResultType.Array, arr);
                
                default:
                    return await this.ReadAsync();
            }
        }
    }
}