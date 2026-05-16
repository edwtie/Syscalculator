#nullable enable

namespace Tiedragon.ClipboardConvert;

/// <summary>
/// Result for a line/tab delimited clipboard conversion.
/// </summary>
public readonly record struct ClipboardTextConversionResult(
    string Text,
    bool HadError,
    ClipboardConversionSummary Summary);
