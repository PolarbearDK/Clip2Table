using Clip2Table.Core;
using Clip2Table.Core.Rendering;
using Clip2Table.Core.Styling;

namespace Clip2Table.Core.Tests;

public class HtmlTableRendererTests
{
    private static readonly Layout E2 = BuiltInLayout.Create().ToLayout();

    [Fact]
    public void Renders_e2_golden()
    {
        const string Th = "background:#dce6f1;color:#1f4e79;font-weight:bold;border:1px solid #9fb6cd;padding:3px 7px";
        const string Td = "border:1px solid #bbbbbb;padding:3px 7px";
        var expected =
            "<table style=\"border-collapse:collapse;font-family:'Segoe UI',Arial,sans-serif;font-size:12px;color:#222222\">" +
            $"<tr><th style=\"{Th};text-align:left\">Name</th><th style=\"{Th};text-align:right\">Qty</th></tr>" +
            $"<tr><td style=\"{Td};text-align:left\">Malmö</td><td style=\"{Td};text-align:right\">5</td></tr>" +
            $"<tr style=\"background:#eaf1f8\"><td style=\"{Td};text-align:left;background:#eaf1f8;color:#999999;font-style:italic\">NULL</td>" +
            $"<td style=\"{Td};text-align:right;background:#eaf1f8\">&nbsp;</td></tr>" +
            $"<tr><td style=\"{Td};text-align:left\">a&lt;b&amp;c</td><td style=\"{Td};text-align:right\">7</td></tr>" +
            "</table>";

        var table = new ParsedTable(
            ["Name", "Qty"],
            [["Malmö", "5"], ["NULL", ""], ["a<b&c", "7"]],
            [null, null]);

        Assert.Equal(expected, HtmlTableRenderer.Render(table, E2));
    }

    [Fact]
    public void Uses_only_six_digit_hex_colors_because_outlook_ignores_shorthand()
    {
        var table = new ParsedTable(["A", "B"], [["NULL", "1"], ["x", "2"]], [null, null]);

        var html = HtmlTableRenderer.Render(table, E2);

        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3}(?![0-9a-fA-F])", html);
    }

    [Fact]
    public void Applies_center_override()
    {
        var table = new ParsedTable(["A", "B"], [["x", "y"]], [ColumnAlignment.Center, null]);

        var html = HtmlTableRenderer.Render(table, E2);

        Assert.Contains(";text-align:center\">A</th>", html);
        Assert.Contains(";text-align:center\">x</td>", html);
    }

    [Fact]
    public void Escapes_quotes()
    {
        var table = new ParsedTable(["A"], [["say \"hi\""]], [null]);

        Assert.Contains("say &quot;hi&quot;", HtmlTableRenderer.Render(table, E2));
    }
}
