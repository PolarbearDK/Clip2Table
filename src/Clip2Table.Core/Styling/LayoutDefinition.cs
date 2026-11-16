using Clip2Table.Core.Rendering;

namespace Clip2Table.Core.Styling;

/// <summary>A named table layout as the user edits it; <see cref="ToLayout"/> turns it into inline CSS.</summary>
public sealed record LayoutDefinition(
    string Id,
    string Name,
    string FontFamily,
    int FontSize,
    string TextColor,
    string HeaderBackground,
    string HeaderTextColor,
    bool HeaderBold,
    string HeaderBorderColor,
    string CellBorderColor,
    bool Stripes,
    string StripeBackground,
    string NullTextColor,
    bool NullItalic,
    CellPadding Padding)
{
    public Layout ToLayout()
    {
        var padding = $"padding:{Padding.Vertical}px {Padding.Horizontal}px";
        return new Layout(
            TableStyle: $"border-collapse:collapse;font-family:{LayoutFonts.CssFamily(FontFamily)};font-size:{FontSize}px;color:{TextColor}",
            HeaderCellStyle: $"background:{HeaderBackground};color:{HeaderTextColor};font-weight:{(HeaderBold ? "bold" : "normal")};border:1px solid {HeaderBorderColor};{padding}",
            DataCellStyle: $"border:1px solid {CellBorderColor};{padding}",
            StripeBackground: Stripes ? StripeBackground : null,
            NullCellStyle: $"color:{NullTextColor};font-style:{(NullItalic ? "italic" : "normal")}");
    }

    /// <summary>Same look, regardless of id and name. A stripe colour hidden by stripes being off doesn't count.</summary>
    public bool HasSameStyle(LayoutDefinition other)
    {
        var a = this with { Id = "", Name = "" };
        var b = other with { Id = "", Name = "" };
        if (!a.Stripes && !b.Stripes)
        {
            b = b with { StripeBackground = a.StripeBackground };
        }

        return a == b;
    }
}
