using Clip2Table.Core;
using Clip2Table.Core.Parsing;

namespace Clip2Table.Core.Tests;

public class MarkdownTableParserTests
{
    [Fact]
    public void Parses_piped_table()
    {
        var table = MarkdownTableParser.Parse("| a | b |\n|---|---|\n| 1 | 2 |", ParseMode.Strict);

        Assert.NotNull(table);
        Assert.Equal(new[] { "a", "b" }, table.Header);
        Assert.Equal(new[] { "1", "2" }, table.Rows[0]);
    }

    [Fact]
    public void Parses_without_outer_pipes()
    {
        var table = MarkdownTableParser.Parse("a | b\n--- | ---\n1 | 2", ParseMode.Strict);

        Assert.NotNull(table);
        Assert.Equal(new[] { "a", "b" }, table.Header);
        Assert.Equal(new[] { "1", "2" }, table.Rows[0]);
    }

    [Fact]
    public void Tolerates_indentation_and_crlf()
    {
        var table = MarkdownTableParser.Parse("  | a | b |  \r\n  |---|---|\r\n  | 1 | 2 |\r\n", ParseMode.Strict);

        Assert.NotNull(table);
        Assert.Equal(new[] { "a", "b" }, table.Header);
        Assert.Equal(new[] { "1", "2" }, table.Rows[0]);
    }

    [Fact]
    public void Reads_alignment_markers()
    {
        var table = MarkdownTableParser.Parse("| L | C | R | N |\n|:---|:---:|---:|---|\n| x | y | z | 1 |", ParseMode.Strict);

        Assert.NotNull(table);
        Assert.Equal(
            new ColumnAlignment?[] { ColumnAlignment.Left, ColumnAlignment.Center, ColumnAlignment.Right, null },
            table.AlignmentOverrides);
    }

    [Fact]
    public void Honours_escaped_pipes()
    {
        var table = MarkdownTableParser.Parse("| a | b |\n|---|---|\n| x \\| y | 2 |", ParseMode.Strict);

        Assert.NotNull(table);
        Assert.Equal("x | y", table.Rows[0][0]);
    }

    [Fact]
    public void Strips_inline_markers()
    {
        var table = MarkdownTableParser.Parse("| `id` | **name** |\n|---|---|\n| [Doc](http://x) | snake_case |", ParseMode.Strict);

        Assert.NotNull(table);
        Assert.Equal(new[] { "id", "name" }, table.Header);
        Assert.Equal(new[] { "Doc", "snake_case" }, table.Rows[0]);
    }

    [Fact]
    public void Strict_rejects_ragged_lenient_pads()
    {
        const string text = "| a | b | c |\n|---|---|---|\n| 1 | 2 |";

        Assert.Null(MarkdownTableParser.Parse(text, ParseMode.Strict));

        var table = MarkdownTableParser.Parse(text, ParseMode.Lenient);
        Assert.NotNull(table);
        Assert.Equal(new[] { "1", "2", "" }, table.Rows[0]);
    }

    [Theory]
    [InlineData("| a | b |\n| 1 | 2 |", ParseMode.Strict)]
    [InlineData("| a | b |\n| 1 | 2 |", ParseMode.Lenient)]
    [InlineData("| a | b |\n|---|\n| 1 | 2 |", ParseMode.Strict)]
    [InlineData("| a | b |\n|---|\n| 1 | 2 |", ParseMode.Lenient)]
    [InlineData("| a | b |\n|---|---|\n| 1 | 2 |\nThat's all.", ParseMode.Strict)]
    [InlineData("| a | b |\n|---|---|\n| 1 | 2 |\nThat's all.", ParseMode.Lenient)]
    [InlineData("Intro\n| a | b |\n|---|---|\n| 1 | 2 |", ParseMode.Strict)]
    [InlineData("Intro\n| a | b |\n|---|---|\n| 1 | 2 |", ParseMode.Lenient)]
    [InlineData("if (a || b)\n---\nfoo", ParseMode.Strict)]
    [InlineData("if (a || b)\n---\nfoo", ParseMode.Lenient)]
    [InlineData("x | y", ParseMode.Strict)]
    [InlineData("x | y", ParseMode.Lenient)]
    public void Rejects(string text, ParseMode mode)
    {
        Assert.Null(MarkdownTableParser.Parse(text, mode));
    }
}
