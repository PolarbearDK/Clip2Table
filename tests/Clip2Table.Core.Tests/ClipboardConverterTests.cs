using System.Text;
using Clip2Table.Core.Conversion;
using Clip2Table.Core.Parsing;
using Clip2Table.Core.Rendering;
using Clip2Table.Core.Styling;

namespace Clip2Table.Core.Tests;

public class ClipboardConverterTests
{
    private static readonly Layout E2 = BuiltInLayout.Create().ToLayout();

    private const string Markdown = "| a | b |\n|---|---|\n| 1 | 2 |";

    [Fact]
    public void Manual_converts_even_with_html_table()
    {
        var outcome = ClipboardConverter.Convert(Markdown, "<table>", ConversionTrigger.Manual, E2);
        Assert.True(outcome.Converted);
    }

    [Fact]
    public void Auto_skips_existing_html_table()
    {
        var outcome = ClipboardConverter.Convert(Markdown, "<table>", ConversionTrigger.Auto, E2);
        Assert.False(outcome.Converted);
        Assert.Equal(SkipReason.HtmlTableAlreadyPresent, outcome.Skipped);
    }

    [Fact]
    public void Auto_converts_over_non_table_html()
    {
        var html = "<div style=\"color:#d4d4d4\"><span>| a | b |</span></div>";
        var outcome = ClipboardConverter.Convert(Markdown, html, ConversionTrigger.Auto, E2);
        Assert.True(outcome.Converted);
    }

    private const string TabText = "Name\tValue\nPhil\t42";
    private const string WordHtml = "<html xmlns:w=\"urn:schemas-microsoft-com:office:word\"><body><p>Name\tValue</p></body></html>";

    [Fact]
    public void Auto_skips_office_generated_html()
    {
        var outcome = ClipboardConverter.Convert(TabText, WordHtml, ConversionTrigger.Auto, E2);
        Assert.False(outcome.Converted);
        Assert.Equal(SkipReason.OfficeSourceHtml, outcome.Skipped);
    }

    [Fact]
    public void Manual_converts_office_generated_html()
    {
        var outcome = ClipboardConverter.Convert(TabText, WordHtml, ConversionTrigger.Manual, E2);
        Assert.True(outcome.Converted);
    }

    [Fact]
    public void Auto_is_strict_manual_is_lenient()
    {
        const string ragged = "a\tb\tc\n1\t2";
        var auto = ClipboardConverter.Convert(ragged, null, ConversionTrigger.Auto, E2);
        var manual = ClipboardConverter.Convert(ragged, null, ConversionTrigger.Manual, E2);
        Assert.Equal(SkipReason.NotTabular, auto.Skipped);
        Assert.True(manual.Converted);
    }

    [Theory]
    [InlineData(null, ConversionTrigger.Manual)]
    [InlineData("", ConversionTrigger.Manual)]
    [InlineData("  ", ConversionTrigger.Manual)]
    [InlineData(null, ConversionTrigger.Auto)]
    [InlineData("", ConversionTrigger.Auto)]
    [InlineData("  ", ConversionTrigger.Auto)]
    public void No_text(string? text, ConversionTrigger trigger)
    {
        var outcome = ClipboardConverter.Convert(text, null, trigger, E2);
        Assert.Equal(SkipReason.NoText, outcome.Skipped);
    }

    [Fact]
    public void Output_contains_rendered_fragment()
    {
        var outcome = ClipboardConverter.Convert(Markdown, null, ConversionTrigger.Auto, E2);
        var expected = HtmlTableRenderer.Render(TableDetector.Detect(Markdown, ParseMode.Strict)!, E2);
        Assert.Contains(expected, Encoding.UTF8.GetString(outcome.CfHtml!));
    }
}
