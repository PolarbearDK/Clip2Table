using Clip2Table.Core.Clipboard;

namespace Clip2Table.Core.Tests;

public class HtmlFormatInspectorTests
{
    [Theory]
    [InlineData("<table>", true)]
    [InlineData("<TABLE border=1>", true)]
    [InlineData("<table\r\n>", true)]
    [InlineData("<table/>", true)]
    [InlineData("<tablex>", false)]
    [InlineData("<p>table</p>", false)]
    [InlineData("<div>&lt;table&gt;</div>", false)]
    [InlineData(null, false)]
    public void ContainsTable(string? html, bool expected) =>
        Assert.Equal(expected, HtmlFormatInspector.ContainsTable(html));

    [Theory]
    [InlineData("<meta name=Generator content=\"Microsoft Word 15\">", true)]
    [InlineData("<meta name=\"Generator\" content=\"Microsoft Word 15 (filtered medium)\">", true)]
    [InlineData("<html xmlns:w=\"urn:schemas-microsoft-com:office:word\">", true)]
    [InlineData(null, false)]
    [InlineData("<div style=\"color:#d4d4d4\">x</div>", false)]
    [InlineData("<meta name=Generator content=\"Microsoft Excel 15\">", false)]
    public void IsOfficeGenerated(string? html, bool expected) =>
        Assert.Equal(expected, HtmlFormatInspector.IsOfficeGenerated(html));
}
