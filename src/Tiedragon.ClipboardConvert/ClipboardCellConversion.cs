#nullable enable

namespace Tiedragon.ClipboardConvert;

/// <summary>
/// Result for one cell in a clipboard conversion.
/// </summary>
public readonly record struct ClipboardCellConversion(string Text, bool HadError);
