namespace Clip2Table.Core.Parsing;

internal static class RowShaper
{
    /// <summary>
    /// Trims cells and shapes raw rows into a table (first row is the header).
    /// Returns <c>null</c> when the input is not tabular.
    /// </summary>
    public static ParsedTable? Shape(
        IReadOnlyList<IReadOnlyList<string>> rawRows,
        ParseMode mode,
        IReadOnlyList<ColumnAlignment?>? alignments = null)
    {
        if (rawRows.Count < 2)
        {
            return null;
        }

        var trimmed = rawRows
            .Select(row => row.Select(cell => cell.Trim()).ToList())
            .ToList();

        var header = trimmed[0];
        if (header.Count < 2)
        {
            return null;
        }

        int width;
        if (mode == ParseMode.Strict)
        {
            if (header.Any(cell => cell.Length == 0) || trimmed.Any(row => row.Count != header.Count))
            {
                return null;
            }

            width = header.Count;
        }
        else
        {
            width = trimmed.Max(row => row.Count);
        }

        foreach (var row in trimmed)
        {
            while (row.Count < width)
            {
                row.Add("");
            }
        }

        var overrides = new List<ColumnAlignment?>(alignments ?? []);
        while (overrides.Count < width)
        {
            overrides.Add(null);
        }

        return new ParsedTable(
            trimmed[0],
            trimmed.Skip(1).Select(row => (IReadOnlyList<string>)row).ToList(),
            overrides);
    }
}
