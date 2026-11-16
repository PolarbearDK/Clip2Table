using System.Text.RegularExpressions;

namespace Clip2Table.Core.Clipboard;

public static partial class HtmlFormatInspector
{
    public static bool ContainsTable(string? cfHtml) =>
        cfHtml is not null && TableTag().IsMatch(cfHtml);

    /// <summary>
    /// True when the HTML was produced by Word/Outlook. Their tab-aligned prose arrives as strict-valid TSV,
    /// so auto mode must leave such copies alone.
    /// </summary>
    public static bool IsOfficeGenerated(string? cfHtml) =>
        cfHtml is not null && OfficeMarker().IsMatch(cfHtml);

    [GeneratedRegex(@"<table[\s>/]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TableTag();

    [GeneratedRegex(
        @"<meta\s+name\s*=\s*[""']?Generator[""']?\s+content\s*=\s*[""']?Microsoft Word|urn:schemas-microsoft-com:office:word",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OfficeMarker();
}
