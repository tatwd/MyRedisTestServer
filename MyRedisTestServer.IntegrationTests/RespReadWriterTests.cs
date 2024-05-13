using System.Text;

namespace MyRedisTestServer.IntegrationTests;

using static RespReadWriter;
using static RespTypeBuilder;

public class RespReadWriterTests
{
    [Test]
    public void ReadArray_ok()
    {
        var actual = ReadArray(Strings("foo", "bar"));
        var expected = new[] { String("foo"), String("bar") };
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ReadString_ok()
    {
        var actual = ReadString(String("foo"));
        Assert.That(actual, Is.EqualTo("foo"));
    }
    
    [Test]
    public void ReadStrings_ok()
    {
        var actual = ReadStrings(Strings("foo", "bar"));
        var expected = new[] { "foo", "bar" };
        Assert.That(actual, Is.EqualTo(expected));
    }
    
    [Test]
    public void Read_ok()
    {
        // blob strings
        ReadTest("$11\r\nhello world\r\n");
        ReadTest("$0\r\n\r\n");
        
        // simple strings
        ReadTest("+abc\r\n");
        ReadTest("+\r\n");
        
        // simple errors
        ReadTest("-ERR wrong\r\n");
        
        // int
        ReadTest(":10\r\n");
        
        // float
        ReadTest(",10\r\n");
        ReadTest(",10.0\r\n");
        ReadTest(",10.123\r\n");
        ReadTest(",inf\r\n");
        ReadTest(",-inf\r\n");
        
        // null
        ReadTest("_\r\n");
        
        // array
        ReadTest("*0\r\n");
        ReadTest("*1\r\n-foo\r\n");
        ReadTest("*2\r\n-foo\r\n$3\r\nfoo\r\n");
        ReadTest("*-1\r\n");
        
        // push
        ReadTest(">0\r\n");
        ReadTest(">1\r\n-foo\r\n");
        ReadTest(">2\r\n-foo\r\n$3\r\nfoo\r\n");
        ReadTest(">-1\r\n");
        
        // nil
        ReadTest("$-1\r\n");
        
        // map
        ReadTest("%0\r\n");
        ReadTest("%1\r\n-foo\r\n-bar\r\n");
        ReadTest("%2\r\n-foo\r\n$3\r\nfoo\r\n-bar\r\n-bar\r\n");
        ReadTest("%-1\r\n");
        
        // set
        ReadTest("~0\r\n");
        ReadTest("~1\r\n-foo\r\n");
        ReadTest("~2\r\n-foo\r\n$3\r\nfoo\r\n");
        ReadTest("~-1\r\n");
        
    }

    private static void ReadTest(string payload)
    {
        const string ping = "+ping\r\n";
        using var r = new StringReader(payload + ping);
        
        var actual = Read(r);
        
        Assert.That(actual, Is.EqualTo(payload));

        var buff = new char[7];
        r.ReadBlock(buff, 0, 7);
        
        Assert.That(new string(buff), Is.EqualTo(ping));
    }


    [Test]
    public void Write_ok()
    {
        WriteTest("*0\r\n");
        WriteTest("*1\r\n$0\r\n\r\n", "");
        WriteTest("*1\r\n$4\r\nPING\r\n", "PING");
        WriteTest("*3\r\n$4\r\nPING\r\n$1\r\na\r\n$1\r\nb\r\n", "PING", "a", "b");
    }

    private static void WriteTest(string expected, params string[] cmd)
    {
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);

        Write(sw, cmd);

        var actual = sb.ToString();
        Assert.That(actual, Is.EqualTo(expected));
    }


    [Test]
    public void Parse_ok()
    {
        // int
        var n = Parse(Int(12));
        Assert.That(n, Is.EqualTo(12));
        
        // inline
        var inline = Parse(Inline("foo"));
        Assert.That(inline, Is.EqualTo("foo"));
        
        // string
        var str = Parse(String("foo"));
        Assert.That(str, Is.EqualTo("foo"));

        // strings
        var strList = Parse(Strings("foo", "bar"));
        Assert.That(strList, Is.EqualTo(new [] { "foo", "bar" }));
        
        var strList2 =
            Parse(
                "*3\r\n$6\r\nCLIENT\r\n$7\r\nSETNAME\r\n$39\r\nLAPTOP-HUSFAIKI(SE.Redis-v2.7.33.41805)\r\n*4\r\n$6\\r\nCLIENT\r\n$7\r\nSETINFO\r\n$8\r\nlib-name\r\n$8\r\nSE.Redis\r\n");
        
        Assert.That(strList2, Is.EqualTo(new [] { "CLIENT","SETNAME","LAPTOP-HUSFAIKI(SE.Redis-v2.7.33.41805)" }));

        // string map
        var map = Parse(StringMap("foo", "bar", "baz", "zap"));
        Assert.That(map, Is.EqualTo(new Dictionary<string, string> { ["foo"] = "bar", ["baz"] = "zap" }));


    }


    [Test]
    public void Read_withErrorRangeString()
    {
        ReadTest("*3\r\n-foo\r\n$50\r\nfoobar\r\n$2\r\nhi\r\n");
    }
}