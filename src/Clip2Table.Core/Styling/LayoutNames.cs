namespace Clip2Table.Core.Styling;

/// <summary>Layout names are trimmed, non-empty and unique without regard to case.</summary>
public static class LayoutNames
{
    /// <summary>An error message for the user, or null when <paramref name="name"/> is acceptable.</summary>
    public static string? Validate(string name, IEnumerable<string> otherNames)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return "Enter a name.";
        }

        return IsTaken(trimmed, otherNames) ? $"A layout named '{trimmed}' already exists." : null;
    }

    /// <summary>The trimmed name if free, otherwise the first free "name (2)", "name (3)", ….</summary>
    public static string MakeUnique(string name, IEnumerable<string> takenNames)
    {
        var taken = takenNames.ToList();
        var trimmed = name.Trim();
        if (!IsTaken(trimmed, taken))
        {
            return trimmed;
        }

        for (var n = 2; ; n++)
        {
            var candidate = $"{trimmed} ({n})";
            if (!IsTaken(candidate, taken))
            {
                return candidate;
            }
        }
    }

    private static bool IsTaken(string name, IEnumerable<string> names) =>
        names.Any(n => string.Equals(n.Trim(), name, StringComparison.CurrentCultureIgnoreCase));
}
