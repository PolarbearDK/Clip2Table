namespace Clip2Table.Core.Styling;

/// <summary>Fonts that survive pasting into Outlook and Word, each with a fallback chain for recipients without it.</summary>
public static class LayoutFonts
{
    public const int MinSize = 9;
    public const int MaxSize = 16;

    private static readonly (string Family, string Css)[] Chains =
    [
        ("Segoe UI", "'Segoe UI',Arial,sans-serif"),
        ("Calibri", "Calibri,Arial,sans-serif"),
        ("Arial", "Arial,sans-serif"),
        ("Verdana", "Verdana,Arial,sans-serif"),
        ("Tahoma", "Tahoma,Arial,sans-serif"),
        ("Georgia", "Georgia,'Times New Roman',serif"),
        ("Times New Roman", "'Times New Roman',serif"),
        ("Consolas", "Consolas,'Courier New',monospace"),
    ];

    public static IReadOnlyList<string> Families { get; } = [.. Chains.Select(c => c.Family)];

    public static string CssFamily(string family)
    {
        foreach (var (name, css) in Chains)
        {
            if (name == family)
            {
                return css;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown layout font.");
    }
}
