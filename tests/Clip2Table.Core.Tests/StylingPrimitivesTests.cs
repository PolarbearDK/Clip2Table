using Clip2Table.Core.Styling;

namespace Clip2Table.Core.Tests;

public class StylingPrimitivesTests
{
    [Theory]
    [InlineData("#1F4E79", "#1f4e79")]
    [InlineData("1f4e79", "#1f4e79")]
    [InlineData("ABC", "#aabbcc")]
    [InlineData(" #AbCdEf ", "#abcdef")]
    public void HexColor_normalizes(string input, string expected)
    {
        Assert.True(HexColor.TryNormalize(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#abcd")]
    [InlineData("#ggg")]
    [InlineData("red")]
    public void HexColor_rejects(string? input) => Assert.False(HexColor.TryNormalize(input, out _));

    [Fact]
    public void HexColor_strict_requires_hash_and_six_digits()
    {
        Assert.True(HexColor.IsStrict("#AABBCC"));
        Assert.False(HexColor.IsStrict("#abc"));
        Assert.False(HexColor.IsStrict("aabbcc"));
        Assert.False(HexColor.IsStrict(null));
    }

    [Fact]
    public void Contrast_black_white_is_21() => Assert.Equal(21.0, Contrast.Ratio("#000000", "#ffffff"), 2);

    [Fact]
    public void Contrast_is_symmetric_and_known()
    {
        Assert.Equal(4.48, Contrast.Ratio("#777777", "#ffffff"), 2);
        Assert.Equal(Contrast.Ratio("#777777", "#ffffff"), Contrast.Ratio("#ffffff", "#777777"));
    }

    [Theory]
    [InlineData(1, 3, PaddingPreset.Minimal)]
    [InlineData(3, 7, PaddingPreset.Compact)]
    [InlineData(5, 10, PaddingPreset.Normal)]
    [InlineData(8, 14, PaddingPreset.Relaxed)]
    [InlineData(3, 8, PaddingPreset.Custom)]
    public void Padding_preset_detected_from_values(int v, int h, PaddingPreset expected) =>
        Assert.Equal(expected, PaddingPresets.Detect(new CellPadding(v, h)));

    [Fact]
    public void Padding_preset_values()
    {
        Assert.Equal(new CellPadding(3, 7), PaddingPresets.For(PaddingPreset.Compact));
        Assert.Throws<ArgumentOutOfRangeException>(() => PaddingPresets.For(PaddingPreset.Custom));
    }

    [Fact]
    public void Padding_range()
    {
        Assert.True(new CellPadding(20, 40).IsInRange);
        Assert.True(new CellPadding(0, 0).IsInRange);
        Assert.False(new CellPadding(21, 0).IsInRange);
        Assert.False(new CellPadding(0, -1).IsInRange);
        Assert.False(new CellPadding(0, 41).IsInRange);
    }

    [Theory]
    [InlineData("Segoe UI", "'Segoe UI',Arial,sans-serif")]
    [InlineData("Calibri", "Calibri,Arial,sans-serif")]
    [InlineData("Verdana", "Verdana,Arial,sans-serif")]
    [InlineData("Tahoma", "Tahoma,Arial,sans-serif")]
    [InlineData("Arial", "Arial,sans-serif")]
    [InlineData("Georgia", "Georgia,'Times New Roman',serif")]
    [InlineData("Times New Roman", "'Times New Roman',serif")]
    [InlineData("Consolas", "Consolas,'Courier New',monospace")]
    public void Font_css_chain(string family, string css) => Assert.Equal(css, LayoutFonts.CssFamily(family));

    [Fact]
    public void Font_list_in_dialog_order() =>
        Assert.Equal(
            ["Segoe UI", "Calibri", "Arial", "Verdana", "Tahoma", "Georgia", "Times New Roman", "Consolas"],
            LayoutFonts.Families);
}
