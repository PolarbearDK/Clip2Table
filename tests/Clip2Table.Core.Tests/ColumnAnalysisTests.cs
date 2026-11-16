using Clip2Table.Core;
using Clip2Table.Core.Rendering;

namespace Clip2Table.Core.Tests;

public class ColumnAnalysisTests
{
    public static TheoryData<string[], bool> Cases => new()
    {
        { new[] { "-12", "18240.50", "0,5", "NULL", "" }, true },
        { new[] { "+5" }, true },
        { new[] { "12", "abc" }, false },
        { new[] { "NULL", "" }, false },
        { new[] { "1.2.3" }, false },
        { new[] { "1 000" }, false },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void IsNumeric(string[] cells, bool expected) =>
        Assert.Equal(expected, ColumnAnalysis.IsNumeric(cells));

    [Fact]
    public void Override_beats_numeric()
    {
        var table = new ParsedTable(
            ["Qty"],
            [["1"], ["2"]],
            [ColumnAlignment.Left]);

        Assert.Equal(ColumnAlignment.Left, ColumnAnalysis.EffectiveAlignment(table, 0));
    }

    [Fact]
    public void Numeric_without_override_is_right_and_text_is_left()
    {
        var table = new ParsedTable(
            ["Name", "Qty"],
            [["a", "1"]],
            [null, null]);

        Assert.Equal(ColumnAlignment.Left, ColumnAnalysis.EffectiveAlignment(table, 0));
        Assert.Equal(ColumnAlignment.Right, ColumnAnalysis.EffectiveAlignment(table, 1));
    }
}
