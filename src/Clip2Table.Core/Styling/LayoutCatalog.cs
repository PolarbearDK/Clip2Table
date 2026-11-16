using Clip2Table.Core.Settings;

namespace Clip2Table.Core.Styling;

public static class LayoutCatalog
{
    /// <summary>The built-in layout first, then custom layouts by name, ignoring case.</summary>
    public static IReadOnlyList<LayoutDefinition> All(AppSettings s) =>
        [BuiltInLayout.Create(s.BuiltInName), .. s.Customs.OrderBy(l => l.Name, StringComparer.CurrentCultureIgnoreCase)];

    public static LayoutDefinition Active(AppSettings s) =>
        s.Customs.FirstOrDefault(l => l.Id == s.Layout) ?? BuiltInLayout.Create(s.BuiltInName);
}
