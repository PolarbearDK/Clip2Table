namespace Clip2Table.Core.Parsing;

internal static class TextLines
{
    /// <summary>
    /// Splits on CRLF, LF or CR and drops leading and trailing whitespace-only lines.
    /// </summary>
    public static IReadOnlyList<string> Split(string text)
    {
        var lines = text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);

        var start = 0;
        var end = lines.Length;
        while (start < end && string.IsNullOrWhiteSpace(lines[start]))
        {
            start++;
        }

        while (end > start && string.IsNullOrWhiteSpace(lines[end - 1]))
        {
            end--;
        }

        return lines[start..end];
    }
}
