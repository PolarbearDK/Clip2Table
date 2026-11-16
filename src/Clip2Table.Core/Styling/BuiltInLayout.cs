namespace Clip2Table.Core.Styling;

/// <summary>The layout that always exists. Its settings are fixed; only its name can change.</summary>
public static class BuiltInLayout
{
    public const string Id = "builtin";
    public const string DefaultName = "Compact, light blue header";

    public static LayoutDefinition Create(string? nameOverride = null) => new(
        Id: Id,
        Name: string.IsNullOrWhiteSpace(nameOverride) ? DefaultName : nameOverride.Trim(),
        FontFamily: "Segoe UI",
        FontSize: 12,
        TextColor: "#222222",
        // New Outlook strips every text colour on paste but keeps backgrounds, so the header
        // background must stay light enough to read with black text.
        HeaderBackground: "#dce6f1",
        HeaderTextColor: "#1f4e79",
        HeaderBold: true,
        HeaderBorderColor: "#9fb6cd",
        CellBorderColor: "#bbbbbb",
        Stripes: true,
        StripeBackground: "#eaf1f8",
        NullTextColor: "#999999",
        NullItalic: true,
        Padding: PaddingPresets.For(PaddingPreset.Compact));
}
