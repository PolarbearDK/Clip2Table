namespace Clip2Table.Core.Parsing;

public static class TableDetector
{
    public static ParsedTable? Detect(string? text, ParseMode mode)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return BoxTableParser.Parse(text, mode)
            ?? MarkdownTableParser.Parse(text, mode)
            ?? TabSeparatedParser.Parse(text, mode);
    }
}
