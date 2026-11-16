using Clip2Table.Core.Clipboard;
using Clip2Table.Core.Parsing;
using Clip2Table.Core.Rendering;

namespace Clip2Table.Core.Conversion;

public enum ConversionTrigger { Manual, Auto }

public enum SkipReason { NoText, HtmlTableAlreadyPresent, OfficeSourceHtml, NotTabular }

public sealed record ConversionOutcome(byte[]? CfHtml, SkipReason? Skipped)
{
    public bool Converted => CfHtml is not null;
}

public static class ClipboardConverter
{
    public static ConversionOutcome Convert(string? text, string? existingHtml, ConversionTrigger trigger, Layout layout)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ConversionOutcome(null, SkipReason.NoText);
        }

        var auto = trigger == ConversionTrigger.Auto;
        if (auto && HtmlFormatInspector.ContainsTable(existingHtml))
        {
            return new ConversionOutcome(null, SkipReason.HtmlTableAlreadyPresent);
        }

        if (auto && HtmlFormatInspector.IsOfficeGenerated(existingHtml))
        {
            return new ConversionOutcome(null, SkipReason.OfficeSourceHtml);
        }

        var table = TableDetector.Detect(text, auto ? ParseMode.Strict : ParseMode.Lenient);
        if (table is null)
        {
            return new ConversionOutcome(null, SkipReason.NotTabular);
        }

        return new ConversionOutcome(CfHtml.Build(HtmlTableRenderer.Render(table, layout)), null);
    }
}
