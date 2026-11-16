using System.Text.RegularExpressions;

namespace Clip2Table.Core.Rendering;

public static partial class ColumnAnalysis
{
    [GeneratedRegex(@"^[+-]?\d+([.,]\d+)?$")]
    private static partial Regex NumberPattern();

    public static bool IsNumeric(IEnumerable<string> cells)
    {
        var any = false;
        foreach (var cell in cells)
        {
            if (cell.Length == 0 || cell == "NULL")
            {
                continue;
            }

            if (!NumberPattern().IsMatch(cell))
            {
                return false;
            }

            any = true;
        }

        return any;
    }

    public static ColumnAlignment EffectiveAlignment(ParsedTable t, int column)
    {
        if (column < t.AlignmentOverrides.Count && t.AlignmentOverrides[column] is { } over)
        {
            return over;
        }

        var cells = t.Rows.Select(r => column < r.Count ? r[column] : "");
        return IsNumeric(cells) ? ColumnAlignment.Right : ColumnAlignment.Left;
    }
}
