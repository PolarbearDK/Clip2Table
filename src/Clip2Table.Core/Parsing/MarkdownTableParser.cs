using System.Text;
using System.Text.RegularExpressions;

namespace Clip2Table.Core.Parsing;

internal static partial class MarkdownTableParser
{
    [GeneratedRegex("^:?-+:?$")]
    private static partial Regex DelimiterCell();

    public static ParsedTable? Parse(string text, ParseMode mode)
    {
        var lines = TextLines.Split(text).Select(line => line.Trim()).ToList();
        if (lines.Count < 2 || lines.Any(line => !ContainsUnescapedPipe(line)))
        {
            return null;
        }

        var rows = lines.Select(SplitRow).ToList();

        var delimiter = rows[1];
        if (delimiter.Count != rows[0].Count)
        {
            return null;
        }

        var alignments = new List<ColumnAlignment?>();
        foreach (var raw in delimiter)
        {
            var cell = raw.Trim();
            if (!DelimiterCell().IsMatch(cell))
            {
                return null;
            }

            var left = cell[0] == ':';
            var right = cell[^1] == ':';
            alignments.Add(left && right ? ColumnAlignment.Center
                : left ? ColumnAlignment.Left
                : right ? ColumnAlignment.Right
                : null);
        }

        var dataRows = new List<IReadOnlyList<string>>();
        for (var i = 0; i < rows.Count; i++)
        {
            if (i == 1)
            {
                continue;
            }

            dataRows.Add(rows[i].Select(cell => InlineMarkdown.Strip(cell.Trim())).ToList());
        }

        return RowShaper.Shape(dataRows, mode, alignments);
    }

    private static bool ContainsUnescapedPipe(string line)
    {
        for (var i = 0; i < line.Length; i++)
        {
            if (line[i] == '|' && (i == 0 || line[i - 1] != '\\'))
            {
                return true;
            }
        }

        return false;
    }

    private static List<string> SplitRow(string line)
    {
        var start = line.StartsWith('|') ? 1 : 0;
        var end = line.Length;
        if (end > start && line[end - 1] == '|' && (end < 2 || line[end - 2] != '\\'))
        {
            end--;
        }

        var cells = new List<string>();
        var current = new StringBuilder();
        for (var i = start; i < end; i++)
        {
            var c = line[i];
            if (c == '\\' && i + 1 < end && line[i + 1] == '|')
            {
                current.Append('|');
                i++;
            }
            else if (c == '|')
            {
                cells.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        cells.Add(current.ToString());
        return cells;
    }
}
