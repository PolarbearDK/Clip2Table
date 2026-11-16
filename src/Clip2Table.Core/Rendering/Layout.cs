namespace Clip2Table.Core.Rendering;

/// <summary>
/// A table layout expressed as inline-CSS strings, the renderer's input. Every style is applied inline because
/// Outlook's Word engine ignores <c>&lt;style&gt;</c> blocks. Built from a
/// <see cref="Styling.LayoutDefinition"/>; a null <see cref="StripeBackground"/> means no striping.
/// </summary>
public sealed record Layout(
    string TableStyle,
    string HeaderCellStyle,
    string DataCellStyle,
    string? StripeBackground,
    string NullCellStyle);
