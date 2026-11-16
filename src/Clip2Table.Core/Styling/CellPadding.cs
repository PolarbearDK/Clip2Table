namespace Clip2Table.Core.Styling;

/// <summary>Cell padding in pixels, applied to header and data cells alike.</summary>
public readonly record struct CellPadding(int Vertical, int Horizontal)
{
    public const int MaxVertical = 20;
    public const int MaxHorizontal = 40;

    public bool IsInRange => Vertical is >= 0 and <= MaxVertical && Horizontal is >= 0 and <= MaxHorizontal;
}

/// <summary>Declaration order is the order of the dialog's radio buttons.</summary>
public enum PaddingPreset { Compact, Minimal, Normal, Relaxed, Custom }

public static class PaddingPresets
{
    public static CellPadding For(PaddingPreset preset) => preset switch
    {
        PaddingPreset.Minimal => new(1, 3),
        PaddingPreset.Compact => new(3, 7),
        PaddingPreset.Normal => new(5, 10),
        PaddingPreset.Relaxed => new(8, 14),
        _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Custom padding has no fixed values."),
    };

    /// <summary>The preset whose values match, otherwise <see cref="PaddingPreset.Custom"/>.</summary>
    public static PaddingPreset Detect(CellPadding padding)
    {
        foreach (var preset in Enum.GetValues<PaddingPreset>())
        {
            if (preset != PaddingPreset.Custom && For(preset) == padding)
            {
                return preset;
            }
        }

        return PaddingPreset.Custom;
    }
}
