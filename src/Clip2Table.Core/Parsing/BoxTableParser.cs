namespace Clip2Table.Core.Parsing;

internal static class BoxTableParser
{
    private static readonly char[] Verticals = ['│', '┃', '║'];
    private static readonly char[] Horizontals = ['─', '━', '═'];

    public static ParsedTable? Parse(string text, ParseMode mode)
    {
        var lines = TextLines.Split(text).Select(line => line.Trim()).ToList();
        if (lines.Count == 0 || !IsBorder(lines[0]) || !IsBorder(lines[^1]))
        {
            return null;
        }

        var blocks = new List<List<List<string>>>();
        var current = new List<List<string>>();
        foreach (var line in lines)
        {
            if (IsBorder(line))
            {
                if (current.Count > 0)
                {
                    blocks.Add(current);
                    current = [];
                }
            }
            else if (IsContent(line))
            {
                var cells = line[1..^1].Split(Verticals).ToList();
                if (current.Count > 0 && current[0].Count != cells.Count)
                {
                    return null;
                }

                current.Add(cells);
            }
            else
            {
                return null;
            }
        }

        var rows = new List<IReadOnlyList<string>>();
        if (blocks.Count == 1)
        {
            rows.AddRange(blocks[0]);
        }
        else if (blocks.Count == 2)
        {
            rows.Add(Join(blocks[0]));
            rows.AddRange(blocks[1]);
        }
        else
        {
            rows.AddRange(blocks.Select(Join));
        }

        var stripped = rows
            .Select(row => (IReadOnlyList<string>)row.Select(cell => InlineMarkdown.Strip(cell.Trim())).ToList())
            .ToList();

        return RowShaper.Shape(stripped, mode);
    }

    private static bool IsBorder(string line) =>
        line.IndexOfAny(Horizontals) >= 0
        && line.All(c => char.IsWhiteSpace(c) || c is >= '─' and <= '╿');

    private static bool IsContent(string line) =>
        line.Length >= 2 && Verticals.Contains(line[0]) && Verticals.Contains(line[^1]);

    private static List<string> Join(List<List<string>> block)
    {
        var joined = new List<string>();
        for (var column = 0; column < block[0].Count; column++)
        {
            joined.Add(string.Join(' ', block
                .Select(cells => cells[column].Trim())
                .Where(part => part.Length > 0)));
        }

        return joined;
    }
}
