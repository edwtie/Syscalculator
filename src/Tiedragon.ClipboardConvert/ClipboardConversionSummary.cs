#nullable enable

namespace Tiedragon.ClipboardConvert;

/// <summary>
/// Counts from a clipboard table conversion.
/// </summary>
public readonly record struct ClipboardConversionSummary(
    int RowCount,
    int CellCount,
    int ConvertedCellCount,
    int ErrorCellCount)
{
    public static ClipboardConversionSummary Empty => new(0, 0, 0, 0);
}
