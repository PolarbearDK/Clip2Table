namespace Clip2Table.Core.Styling;

public static class LayoutValidator
{
    /// <summary>The camelCase JSON name of the first invalid field, or null when the layout is valid.</summary>
    public static string? FindInvalidField(LayoutDefinition d)
    {
        if (string.IsNullOrWhiteSpace(d.Name))
        {
            return "name";
        }

        if (!LayoutFonts.Families.Contains(d.FontFamily))
        {
            return "fontFamily";
        }

        if (d.FontSize is < LayoutFonts.MinSize or > LayoutFonts.MaxSize)
        {
            return "fontSize";
        }

        (string Field, string Value)[] colours =
        [
            ("textColor", d.TextColor),
            ("headerBackground", d.HeaderBackground),
            ("headerTextColor", d.HeaderTextColor),
            ("headerBorderColor", d.HeaderBorderColor),
            ("cellBorderColor", d.CellBorderColor),
            ("stripeBackground", d.StripeBackground),
            ("nullTextColor", d.NullTextColor),
        ];
        foreach (var (field, value) in colours)
        {
            if (!HexColor.IsStrict(value))
            {
                return field;
            }
        }

        return d.Padding.IsInRange ? null : "padding";
    }
}
