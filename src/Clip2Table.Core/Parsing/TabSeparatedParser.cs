namespace Clip2Table.Core.Parsing;

internal static class TabSeparatedParser
{
    public static ParsedTable? Parse(string text, ParseMode mode)
    {
        var lines = TextLines.Split(text);
        var rows = lines
            .Select(line => (IReadOnlyList<string>)line.Split('\t'))
            .ToList();

        if (mode == ParseMode.Lenient)
        {
            if (rows.Any(row => row.Count < 2) || rows.Count > 0 && rows[0].Any(cell => cell.Trim().Length == 0))
            {
                return null;
            }
        }

        return RowShaper.Shape(rows, mode);
    }
}
