namespace Clip2Table.Core.Styling;

/// <summary>
/// Colour combinations that will be hard to read, either anywhere or specifically in new Outlook, which strips
/// text colours on paste so text arrives black. Thresholds are chosen so the built-in layout raises none.
/// </summary>
public static class ReadabilityCheck
{
    private const string White = "#ffffff";
    private const string Black = "#000000";
    private const double Text = 4.5;
    private const double MutedText = 2.0;
    private const double Border = 1.3;

    public static IReadOnlyList<string> Find(LayoutDefinition d)
    {
        var warnings = new List<string>();
        var stripe = d.StripeBackground;

        void Check(bool applies, string fg, string bg, double min, string message)
        {
            if (applies && Contrast.Ratio(fg, bg) < min)
            {
                warnings.Add(message);
            }
        }

        Check(true, d.TextColor, White, Text, $"Table text ({d.TextColor}) is hard to read on white.");
        Check(d.Stripes, d.TextColor, stripe, Text, $"Table text ({d.TextColor}) is hard to read on the stripe background ({stripe}).");
        Check(true, d.HeaderTextColor, d.HeaderBackground, Text,
            $"Header text ({d.HeaderTextColor}) is hard to read on the header background ({d.HeaderBackground}).");
        Check(true, d.NullTextColor, White, MutedText, $"NULL text ({d.NullTextColor}) is hard to read on white.");
        Check(d.Stripes, d.NullTextColor, stripe, MutedText,
            $"NULL text ({d.NullTextColor}) is hard to read on the stripe background ({stripe}).");
        Check(true, d.HeaderBorderColor, White, Border, $"Header border ({d.HeaderBorderColor}) is nearly invisible on white.");
        Check(true, d.CellBorderColor, White, Border, $"Cell border ({d.CellBorderColor}) is nearly invisible on white.");
        Check(true, Black, d.HeaderBackground, Text,
            $"In new Outlook, text in the header turns black and will be hard to read on {d.HeaderBackground}.");
        Check(d.Stripes, Black, stripe, Text,
            $"In new Outlook, text in striped rows turns black and will be hard to read on {stripe}.");
        return warnings;
    }
}
