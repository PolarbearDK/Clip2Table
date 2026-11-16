using Clip2Table.Core;
using Clip2Table.Core.Parsing;

namespace Clip2Table.Core.Tests;

public class BoxTableParserTests
{
    private const string Sample = """
        ┌────────────────┬───────────────────────────────────────────────────┬──────────────────────────┐
        │     Event      │             Publisher (routing entry)             │     Learner handler      │
        ├────────────────┼───────────────────────────────────────────────────┼──────────────────────────┤
        │ ContactUpdated │ pbc-contact/src/Contact.Api/subscriptions.json:44 │ ContactEventsHandler: ...│
        ├────────────────┼───────────────────────────────────────────────────┼──────────────────────────┤
        │ ContactDeleted │ pbc-contact/src/Contact.Api/subscriptions.json:2  │ ContactEventsHandler: ...│
        └────────────────┴───────────────────────────────────────────────────┴──────────────────────────┘
        """;

    [Fact]
    public void Parses_user_sample()
    {
        var table = BoxTableParser.Parse(Sample, ParseMode.Strict);

        AssertUserSample(table);
    }

    [Fact]
    public void Joins_wrapped_rows()
    {
        const string text = """
            ┌───┬──────────────────┐
            │ H │ Text             │
            ├───┼──────────────────┤
            │ A │ Long text that   │
            │   │ wraps here       │
            ├───┼──────────────────┤
            │ B │ Short            │
            └───┴──────────────────┘
            """;

        var table = BoxTableParser.Parse(text, ParseMode.Strict);

        Assert.NotNull(table);
        Assert.Equal(new[] { "H", "Text" }, table.Header);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal(new[] { "A", "Long text that wraps here" }, table.Rows[0]);
        Assert.Equal(new[] { "B", "Short" }, table.Rows[1]);
    }

    [Fact]
    public void Each_line_is_a_row_without_row_separators()
    {
        const string text = """
            ┌────┬────┐
            │ a  │ b  │
            ├────┼────┤
            │ 1  │ 2  │
            │ 3  │ 4  │
            │ 5  │ 6  │
            └────┴────┘
            """;

        var table = BoxTableParser.Parse(text, ParseMode.Strict);

        Assert.NotNull(table);
        Assert.Equal(new[] { "a", "b" }, table.Header);
        Assert.Equal(3, table.Rows.Count);
        Assert.Equal(new[] { "1", "2" }, table.Rows[0]);
        Assert.Equal(new[] { "3", "4" }, table.Rows[1]);
        Assert.Equal(new[] { "5", "6" }, table.Rows[2]);
    }

    [Fact]
    public void Blank_content_line_is_not_a_border()
    {
        const string text = """
            ┌──────┬──────┐
            │ H1   │ H2   │
            ├──────┼──────┤
            │ one  │ two  │
            │      │      │
            │ more │ more │
            ├──────┼──────┤
            │ x    │ y    │
            └──────┴──────┘
            """;

        var table = BoxTableParser.Parse(text, ParseMode.Strict);

        Assert.NotNull(table);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal(new[] { "one more", "two more" }, table.Rows[0]);
        Assert.Equal(new[] { "x", "y" }, table.Rows[1]);
    }

    [Fact]
    public void Tolerates_indent_trailing_spaces_crlf()
    {
        var text = string.Join("\r\n", Sample.Split('\n').Select(line => "    " + line.TrimEnd('\r') + "  "));

        var table = BoxTableParser.Parse(text, ParseMode.Strict);

        AssertUserSample(table);
    }

    [Fact]
    public void Parses_double_line_variant()
    {
        const string text = """
            ╔═══╦═══╗
            ║ a ║ b ║
            ╠═══╬═══╣
            ║ 1 ║ 2 ║
            ╚═══╩═══╝
            """;

        var table = BoxTableParser.Parse(text, ParseMode.Strict);

        Assert.NotNull(table);
        Assert.Equal(new[] { "a", "b" }, table.Header);
        Assert.Equal(new[] { "1", "2" }, table.Rows[0]);
    }

    [Fact]
    public void Strips_inline_markers()
    {
        const string text = """
            ┌─────┬───┐
            │ id  │ b │
            ├─────┼───┤
            │ `a` │ 2 │
            └─────┴───┘
            """;

        var table = BoxTableParser.Parse(text, ParseMode.Strict);

        Assert.NotNull(table);
        Assert.Equal("a", table.Rows[0][0]);
    }

    public static TheoryData<string, ParseMode> RejectedInputs()
    {
        var withoutTopBorder = string.Join("\n", Sample.Split('\n').Skip(1));
        var data = new TheoryData<string, ParseMode>();
        foreach (var mode in new[] { ParseMode.Strict, ParseMode.Lenient })
        {
            data.Add("Here you go:\n" + Sample, mode);
            data.Add(Sample + "\nDone.", mode);
            data.Add(withoutTopBorder, mode);
            data.Add("│ a │ b │", mode);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(RejectedInputs))]
    public void Rejects(string text, ParseMode mode)
    {
        Assert.Null(BoxTableParser.Parse(text, mode));
    }

    private static void AssertUserSample(ParsedTable? table)
    {
        Assert.NotNull(table);
        Assert.Equal(new[] { "Event", "Publisher (routing entry)", "Learner handler" }, table.Header);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("pbc-contact/src/Contact.Api/subscriptions.json:44", table.Rows[0][1]);
        Assert.Equal("ContactEventsHandler: ...", table.Rows[1][2]);
    }
}
