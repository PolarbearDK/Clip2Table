using Clip2Table.Core;
using Clip2Table.Core.Parsing;

namespace Clip2Table.Core.Tests;

public class TableDetectorTests
{
    private const string Box = """
        ┌────┬──────┐
        │ Id │ Name │
        ├────┼──────┤
        │ 1  │ A    │
        └────┴──────┘
        """;

    private const string Markdown = "| Id | Name |\n|---|---|\n| 1 | A |";

    private const string Tsv = "Id\tName\tAmount\r\n1\tMalmö\t18240.50\r\n";

    [Theory]
    [InlineData(ParseMode.Strict)]
    [InlineData(ParseMode.Lenient)]
    public void Routes_each_format(ParseMode mode)
    {
        var box = TableDetector.Detect(Box, mode);
        var markdown = TableDetector.Detect(Markdown, mode);
        var tsv = TableDetector.Detect(Tsv, mode);

        Assert.NotNull(box);
        Assert.Equal(new[] { "Id", "Name" }, box.Header);
        Assert.NotNull(markdown);
        Assert.Equal(new[] { "Id", "Name" }, markdown.Header);
        Assert.NotNull(tsv);
        Assert.Equal(new[] { "Id", "Name", "Amount" }, tsv.Header);
    }

    [Fact]
    public void Markdown_wins_over_tsv()
    {
        var table = TableDetector.Detect("| a\t| b |\n|---|---|\n| 1 | 2 |", ParseMode.Strict);

        Assert.NotNull(table);
        Assert.Equal(new[] { "a", "b" }, table.Header);
    }

    [Theory]
    [InlineData(null, ParseMode.Strict)]
    [InlineData(null, ParseMode.Lenient)]
    [InlineData("", ParseMode.Strict)]
    [InlineData("", ParseMode.Lenient)]
    [InlineData("   \r\n", ParseMode.Strict)]
    [InlineData("   \r\n", ParseMode.Lenient)]
    [InlineData("Hello there.\nHow are you?", ParseMode.Strict)]
    [InlineData("Hello there.\nHow are you?", ParseMode.Lenient)]
    [InlineData("a\tb", ParseMode.Strict)]
    [InlineData("a\tb", ParseMode.Lenient)]
    [InlineData("\tif (x)\n\t\treturn;", ParseMode.Strict)]
    [InlineData("\tif (x)\n\t\treturn;", ParseMode.Lenient)]
    [InlineData("Results:\nId\tName\n1\tA", ParseMode.Strict)]
    [InlineData("Results:\nId\tName\n1\tA", ParseMode.Lenient)]
    [InlineData("| Id | Name |\n|---|---|\n| 1 | A |\nHope this helps!", ParseMode.Strict)]
    [InlineData("| Id | Name |\n|---|---|\n| 1 | A |\nHope this helps!", ParseMode.Lenient)]
    public void Rejects(string? text, ParseMode mode)
    {
        Assert.Null(TableDetector.Detect(text, mode));
    }
}
