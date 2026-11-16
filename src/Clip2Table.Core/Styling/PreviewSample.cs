using Clip2Table.Core.Parsing;
using Clip2Table.Core.Rendering;

namespace Clip2Table.Core.Styling;

/// <summary>What the layout dialog previews: the clipboard's table if it has one, otherwise a small sample.</summary>
public static class PreviewSample
{
    private const int MaxRows = 20;
    private const int MaxColumns = 20;
    private const int MaxCellLength = 200;

    /// <summary>Has a NULL cell, a right-aligned number column and enough rows to show striping.</summary>
    public static ParsedTable Sample { get; } = new(
        ["Name", "Qty", "Status"],
        [
            ["Widget", "12", "NULL"],
            ["Gadget", "7", "Shipped"],
            ["Gizmo", "1,250", "Pending"],
            ["Doohickey", "3", "Shipped"],
        ],
        [null, null, null]);

    public static ParsedTable For(string? clipboardText)
    {
        if (string.IsNullOrWhiteSpace(clipboardText) || TableDetector.Detect(clipboardText, ParseMode.Lenient) is not { } table)
        {
            return Sample;
        }

        // Keep the preview small: WebView2 rejects documents over about 2 MB, and nobody reads more than this.
        return new ParsedTable(
            [.. table.Header.Take(MaxColumns).Select(Trim)],
            [.. table.Rows.Take(MaxRows).Select(r => (IReadOnlyList<string>)[.. r.Take(MaxColumns).Select(Trim)])],
            [.. table.AlignmentOverrides.Take(MaxColumns)]);
    }

    private static string Trim(string cell) => cell.Length <= MaxCellLength ? cell : cell[..MaxCellLength] + "…";

    public static string Document(ParsedTable table, Layout layout) =>
        "<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head>" +
        $"<body style=\"margin:8px;background:#ffffff\">{HtmlTableRenderer.Render(table, layout)}</body></html>";
}
