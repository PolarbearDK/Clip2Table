using System.Text;
using System.Text.RegularExpressions;

namespace Clip2Table.Core.Parsing;

/// <summary>
/// Conservative removal of inline Markdown markers. Only paired markers are stripped;
/// when in doubt the text is left unchanged.
/// </summary>
internal static class InlineMarkdown
{
    // Code spans are swapped for private-use placeholders while the other rules run,
    // so that markers may wrap a code span (**`x`**) but never act inside one.
    private const char PlaceholderStart = '\uE000';
    private const char PlaceholderIndexBase = '\uE100';

    private static readonly Regex CodeSpan = new("`([^`]+)`", RegexOptions.Compiled);
    private static readonly Regex Link = new(@"\[([^\[\]]+)\]\([^()\s]*\)", RegexOptions.Compiled);
    private static readonly Regex Bold = new(@"\*\*(?=\S)(.+?)(?<=\S)\*\*", RegexOptions.Compiled);
    private static readonly Regex BoldUnderscore = new(@"(?<!\w)__(?=\S)(.+?)(?<=\S)__(?!\w)", RegexOptions.Compiled);
    private static readonly Regex Strike = new(@"~~(?=\S)(.+?)(?<=\S)~~", RegexOptions.Compiled);
    private static readonly Regex Emphasis = new(@"(?<![\w*])\*(?=\S)([^*]+?)(?<=\S)\*(?![\w*])", RegexOptions.Compiled);
    private static readonly Regex EmphasisUnderscore = new(@"(?<!\w)_(?=\S)(.+?)(?<=\S)_(?!\w)", RegexOptions.Compiled);

    public static string Strip(string cell)
    {
        if (cell.Length == 0 || cell.Contains(PlaceholderStart))
        {
            return cell;
        }

        var spans = new List<string>();
        var text = CodeSpan.Replace(cell, m =>
        {
            spans.Add(m.Groups[1].Value);
            return new string([PlaceholderStart, (char)(PlaceholderIndexBase + spans.Count - 1)]);
        });

        text = Link.Replace(text, "$1");
        text = Bold.Replace(text, "$1");
        text = BoldUnderscore.Replace(text, "$1");
        text = Strike.Replace(text, "$1");
        text = Emphasis.Replace(text, "$1");
        text = EmphasisUnderscore.Replace(text, "$1");

        if (spans.Count == 0)
        {
            return text;
        }

        var sb = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == PlaceholderStart && i + 1 < text.Length)
            {
                sb.Append(spans[text[i + 1] - PlaceholderIndexBase]);
                i++;
            }
            else
            {
                sb.Append(text[i]);
            }
        }

        return sb.ToString();
    }
}
