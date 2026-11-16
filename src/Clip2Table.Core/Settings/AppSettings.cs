using Clip2Table.Core.Styling;

namespace Clip2Table.Core.Settings;

/// <param name="Layout">Id of the active layout; an unknown id means the built-in layout.</param>
/// <param name="BuiltInName">The built-in layout's name after a rename, or null for its default name.</param>
public sealed record AppSettings(
    bool AutoConvert = false,
    string Layout = BuiltInLayout.Id,
    IReadOnlyList<LayoutDefinition>? CustomLayouts = null,
    string? BuiltInName = null)
{
    public IReadOnlyList<LayoutDefinition> Customs => CustomLayouts ?? [];
}
