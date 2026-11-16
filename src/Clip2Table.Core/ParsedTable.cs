namespace Clip2Table.Core;

public enum ColumnAlignment
{
    Left,
    Center,
    Right,
}

/// <summary>
/// A parsed table. <paramref name="AlignmentOverrides"/> has one entry per header cell;
/// a <c>null</c> entry means automatic alignment.
/// </summary>
public sealed record ParsedTable(
    IReadOnlyList<string> Header,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyList<ColumnAlignment?> AlignmentOverrides);
