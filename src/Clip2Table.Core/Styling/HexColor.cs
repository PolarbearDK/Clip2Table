using System.Globalization;

namespace Clip2Table.Core.Styling;

/// <summary>Parses and normalises colours to lowercase six-digit <c>#rrggbb</c>, the only form Outlook honours.</summary>
public static class HexColor
{
    /// <summary>Accepts <c>#rgb</c> or <c>#rrggbb</c>, with or without <c>#</c>, any case, surrounding whitespace ignored.</summary>
    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = "";
        if (input is null)
        {
            return false;
        }

        var digits = input.Trim();
        if (digits.StartsWith('#'))
        {
            digits = digits[1..];
        }

        if (!IsHex(digits) || (digits.Length != 3 && digits.Length != 6))
        {
            return false;
        }

        if (digits.Length == 3)
        {
            digits = string.Concat(digits.Select(c => new string(c, 2)));
        }

        normalized = "#" + digits.ToLowerInvariant();
        return true;
    }

    /// <summary>True only for <c>#</c> followed by exactly six hex digits — the stored and exchanged form.</summary>
    public static bool IsStrict(string? value) =>
        value is { Length: 7 } && value[0] == '#' && IsHex(value.AsSpan(1));

    internal static (int R, int G, int B) ToRgb(string hex) =>
        (Channel(hex, 1), Channel(hex, 3), Channel(hex, 5));

    private static int Channel(string hex, int start) =>
        int.Parse(hex.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    private static bool IsHex(ReadOnlySpan<char> s)
    {
        foreach (var c in s)
        {
            if (!char.IsAsciiHexDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}
