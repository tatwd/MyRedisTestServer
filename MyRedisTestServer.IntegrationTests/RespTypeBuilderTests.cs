namespace MyRedisTestServer.IntegrationTests;

using static RespTypeBuilder;

public class RespTypeBuilderTests
{
    [Test]
    public void BuildRespTypes_ok()
    {
        Assert.Multiple(() =>
        {
            Assert.That(String(null), Is.EqualTo("$0\r\n\r\n"));
            Assert.That(String(""), Is.EqualTo("$0\r\n\r\n"));
            Assert.That(String("foo"), Is.EqualTo("$3\r\nfoo\r\n"));

            Assert.That(Inline("Hi"), Is.EqualTo("+Hi\r\n"));

            Assert.That(Error("ERR wrong"), Is.EqualTo("-ERR wrong\r\n"));

            Assert.That(Int(12), Is.EqualTo(":12\r\n"));

            Assert.That(Float(12.13), Is.EqualTo(",12.13\r\n"));
            
            Assert.That(Array(Inline("hi"), Inline("ho")), Is.EqualTo("*2\r\n+hi\r\n+ho\r\n"));
            Assert.That(Strings("hi", "ho"), Is.EqualTo("*2\r\n$2\r\nhi\r\n$2\r\nho\r\n"));

            Assert.That(Push(Inline("hi"), Inline("ho")), Is.EqualTo(">2\r\n+hi\r\n+ho\r\n"));
            
            Assert.That(Map(String("hi"), String("ho")), Is.EqualTo("%1\r\n$2\r\nhi\r\n$2\r\nho\r\n"));
            Assert.That(StringMap("hi", "ho"), Is.EqualTo("%1\r\n$2\r\nhi\r\n$2\r\nho\r\n"));
            
            
            Assert.That(Set(String("hi"), String("ho")), Is.EqualTo("~2\r\n$2\r\nhi\r\n$2\r\nho\r\n"));
            Assert.That(StringSet("hi", "ho"), Is.EqualTo("~2\r\n$2\r\nhi\r\n$2\r\nho\r\n"));
            
        });
    }
    
}