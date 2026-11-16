using Clip2Table.Core.Rendering;
using Clip2Table.Core.Styling;

namespace Clip2Table.Core.Tests;

public class PreviewSampleTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("just a sentence")]
    public void Falls_back_to_sample(string? text) => Assert.Same(PreviewSample.Sample, PreviewSample.For(text));

    [Fact]
    public void Uses_clipboard_table_capped_at_20_rows()
    {
        var big = "A\tB\n" + string.Join("\n", Enumerable.Range(1, 30).Select(i => $"{i}\tx"));

        var table = PreviewSample.For(big);

        Assert.Equal(["A", "B"], table.Header);
        Assert.Equal(20, table.Rows.Count);
    }

    [Fact]
    public void Long_cells_and_many_columns_are_trimmed_for_preview()
    {
        var header = string.Join("\t", Enumerable.Range(1, 40).Select(i => $"C{i}"));
        var row = string.Join("\t", Enumerable.Range(1, 40).Select(_ => new string('x', 5000)));

        var table = PreviewSample.For(header + "\n" + row);

        Assert.Equal(20, table.Header.Count);
        Assert.Equal(20, table.Rows[0].Count);
        Assert.Equal(201, table.Rows[0][0].Length); // 200 chars + ellipsis
        Assert.EndsWith("…", table.Rows[0][0]);
        Assert.Equal(20, table.AlignmentOverrides.Count);
    }

    [Fact]
    public void Sample_shows_null_numbers_and_stripes()
    {
        var sample = PreviewSample.Sample;
        Assert.Equal(["Name", "Qty", "Status"], sample.Header);
        Assert.Equal(ColumnAlignment.Right, ColumnAnalysis.EffectiveAlignment(sample, 1));
        Assert.Contains(sample.Rows, r => r.Contains("NULL"));
        Assert.True(sample.Rows.Count >= 3);
    }

    [Fact]
    public void Document_wraps_rendered_table()
    {
        var layout = BuiltInLayout.Create().ToLayout();

        var doc = PreviewSample.Document(PreviewSample.Sample, layout);

        Assert.StartsWith("<!DOCTYPE html>", doc);
        Assert.Contains("<meta charset=\"utf-8\">", doc);
        Assert.Contains(HtmlTableRenderer.Render(PreviewSample.Sample, layout), doc);
    }
}
