using Clip2Table.Core;
using Clip2Table.Core.Parsing;

namespace Clip2Table.Core.Tests;

public class TabSeparatedParserTests
{
    [Fact]
    public void Parses_ssms_copy_with_headers()
    {
        var table = TabSeparatedParser.Parse("Id\tName\tAmount\tNote\r\n1\tMalmö\t18240.50\tNULL\r\n2\tÅrhus\t-12\t\r\n", ParseMode.Strict);

        Assert.NotNull(table);
        Assert.Equal(new[] { "Id", "Name", "Amount", "Note" }, table.Header);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal(new[] { "1", "Malmö", "18240.50", "NULL" }, table.Rows[0]);
        Assert.Equal("", table.Rows[1][3]);
        Assert.All(table.AlignmentOverrides, a => Assert.Null(a));
    }

    [Fact]
    public void Trims_cells()
    {
        var table = TabSeparatedParser.Parse(" a \t b \n 1\t2 ", ParseMode.Strict);

        Assert.NotNull(table);
        Assert.Equal(new[] { "a", "b" }, table.Header);
        Assert.Equal(new[] { "1", "2" }, table.Rows[0]);
    }

    [Fact]
    public void Keeps_markdown_markers()
    {
        var table = TabSeparatedParser.Parse("col_a\tcol_b\n**x**\tsnake_case_value", ParseMode.Strict);

        Assert.NotNull(table);
        Assert.Equal(new[] { "**x**", "snake_case_value" }, table.Rows[0]);
    }

    [Fact]
    public void Ignores_outer_blank_lines()
    {
        var table = TabSeparatedParser.Parse("\r\n  \r\na\tb\r\n1\t2\r\n\r\n   \r\n", ParseMode.Strict);

        Assert.NotNull(table);
        Assert.Single(table.Rows);
    }

    [Theory]
    [InlineData("a\tb\n1\t2\t3")]
    [InlineData("a\tb\n1")]
    public void Strict_rejects_ragged(string text)
    {
        Assert.Null(TabSeparatedParser.Parse(text, ParseMode.Strict));
    }

    [Fact]
    public void Strict_rejects_empty_header_cell()
    {
        Assert.Null(TabSeparatedParser.Parse("a\t\n1\t2", ParseMode.Strict));
    }

    [Fact]
    public void Lenient_pads_short_rows()
    {
        var table = TabSeparatedParser.Parse("a\tb\tc\n1\t2\tx\n1\t2", ParseMode.Lenient);

        Assert.NotNull(table);
        Assert.Equal(new[] { "1", "2", "" }, table.Rows[1]);
    }

    [Fact]
    public void Lenient_widens_header_for_long_rows()
    {
        var table = TabSeparatedParser.Parse("a\tb\n1\t2\t3", ParseMode.Lenient);

        Assert.NotNull(table);
        Assert.Equal(new[] { "a", "b", "" }, table.Header);
        Assert.Equal(new[] { "1", "2", "3" }, table.Rows[0]);
        Assert.Equal(3, table.AlignmentOverrides.Count);
    }

    [Theory]
    [InlineData("a\tb", ParseMode.Strict)]
    [InlineData("a\tb", ParseMode.Lenient)]
    [InlineData("a\nb", ParseMode.Strict)]
    [InlineData("a\nb", ParseMode.Lenient)]
    [InlineData("Name:\tPhil\nThanks for the help", ParseMode.Strict)]
    [InlineData("Name:\tPhil\nThanks for the help", ParseMode.Lenient)]
    [InlineData("Id\tName\n1\tA\nSee above.", ParseMode.Strict)]
    [InlineData("Id\tName\n1\tA\nSee above.", ParseMode.Lenient)]
    [InlineData("\tif (x)\n\t\treturn;", ParseMode.Strict)]
    [InlineData("\tif (x)\n\t\treturn;", ParseMode.Lenient)]
    [InlineData("if (x)\n\treturn;", ParseMode.Strict)]
    [InlineData("if (x)\n\treturn;", ParseMode.Lenient)]
    public void Rejects_non_tables(string text, ParseMode mode)
    {
        Assert.Null(TabSeparatedParser.Parse(text, mode));
    }
}
