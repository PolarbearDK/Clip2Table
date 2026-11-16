using System.Text;
using Clip2Table.Core.Clipboard;

namespace Clip2Table.Core.Tests;

public class CfHtmlTests
{
    private static int Offset(string header, string name)
    {
        var start = header.IndexOf(name + ":", StringComparison.Ordinal) + name.Length + 1;
        return int.Parse(header.Substring(start, 10));
    }

    [Fact]
    public void Offsets_are_utf8_byte_offsets()
    {
        const string fragment = "<table><tr><td>Malmö – ÅÆØ 😀</td></tr></table>";
        var bytes = CfHtml.Build(fragment);
        var header = Encoding.ASCII.GetString(bytes, 0, 120);

        var startHtml = Offset(header, "StartHTML");
        var endHtml = Offset(header, "EndHTML");
        var startFragment = Offset(header, "StartFragment");
        var endFragment = Offset(header, "EndFragment");

        Assert.Equal(fragment, Encoding.UTF8.GetString(bytes[startFragment..endFragment]));
        Assert.StartsWith("<html>", Encoding.UTF8.GetString(bytes[startHtml..]));
        Assert.Equal(bytes.Length, endHtml);
        Assert.Equal("<!--StartFragment-->", Encoding.UTF8.GetString(bytes[(startFragment - 20)..startFragment]));
    }

    [Fact]
    public void Header_starts_with_version()
    {
        var text = Encoding.UTF8.GetString(CfHtml.Build("<table></table>"));
        Assert.StartsWith("Version:0.9\r\nStartHTML:", text);
    }
}
