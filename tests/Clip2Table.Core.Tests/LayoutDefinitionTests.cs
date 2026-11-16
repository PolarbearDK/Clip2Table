using Clip2Table.Core.Rendering;
using Clip2Table.Core.Styling;

namespace Clip2Table.Core.Tests;

public class LayoutDefinitionTests
{
    [Fact]
    public void Builtin_produces_todays_css()
    {
        var l = BuiltInLayout.Create().ToLayout();
        Assert.Equal("border-collapse:collapse;font-family:'Segoe UI',Arial,sans-serif;font-size:12px;color:#222222", l.TableStyle);
        Assert.Equal("background:#dce6f1;color:#1f4e79;font-weight:bold;border:1px solid #9fb6cd;padding:3px 7px", l.HeaderCellStyle);
        Assert.Equal("border:1px solid #bbbbbb;padding:3px 7px", l.DataCellStyle);
        Assert.Equal("#eaf1f8", l.StripeBackground);
        Assert.Equal("color:#999999;font-style:italic", l.NullCellStyle);
    }

    [Fact]
    public void Bold_italic_stripes_off()
    {
        var l = (BuiltInLayout.Create() with { HeaderBold = false, NullItalic = false, Stripes = false }).ToLayout();
        Assert.Contains("font-weight:normal", l.HeaderCellStyle);
        Assert.Equal("color:#999999;font-style:normal", l.NullCellStyle);
        Assert.Null(l.StripeBackground);
    }

    [Fact]
    public void Padding_and_font_flow_into_css()
    {
        var l = (BuiltInLayout.Create() with { Padding = new(0, 14), FontFamily = "Calibri", FontSize = 9 }).ToLayout();
        Assert.EndsWith("padding:0px 14px", l.DataCellStyle);
        Assert.EndsWith("padding:0px 14px", l.HeaderCellStyle);
        Assert.Contains("font-family:Calibri,Arial,sans-serif;font-size:9px", l.TableStyle);
    }

    [Fact]
    public void Renderer_without_stripes_emits_no_row_background()
    {
        var table = new ParsedTable(["A"], [["1"], ["2"], ["3"]], [null]);
        var html = HtmlTableRenderer.Render(table, (BuiltInLayout.Create() with { Stripes = false }).ToLayout());
        Assert.DoesNotContain("<tr style=", html);
        Assert.DoesNotContain("#eaf1f8", html);
    }

    [Fact]
    public void Same_style_ignores_id_name_and_hidden_stripe_colour()
    {
        var a = BuiltInLayout.Create() with { Stripes = false };
        var b = a with { Id = "x", Name = "Other", StripeBackground = "#000000" };
        Assert.True(a.HasSameStyle(b));
        Assert.False(a.HasSameStyle(b with { Padding = new(3, 8) }));
        Assert.False((a with { Stripes = true }).HasSameStyle(b with { Stripes = true }));
    }

    [Fact]
    public void Builtin_name_override()
    {
        Assert.Equal("House", BuiltInLayout.Create("House").Name);
        Assert.Equal(BuiltInLayout.DefaultName, BuiltInLayout.Create("  ").Name);
        Assert.Equal(BuiltInLayout.Id, BuiltInLayout.Create("House").Id);
    }
}
