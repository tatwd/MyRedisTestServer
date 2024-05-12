namespace MyRedisTestServer.IntegrationTests;

using static RespReader;
using static RespTypeBuilder;

public class RespReaderTests
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
}