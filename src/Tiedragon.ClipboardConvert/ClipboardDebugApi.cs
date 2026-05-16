#nullable enable

namespace Tiedragon.ClipboardConvert;

/// <summary>
/// Debug helpers for clipboard based tools.
/// </summary>
public static class ClipboardDebugApi
{
    /// <summary>
    /// Builds a readable debug report from a clipboard snapshot and optional state comparison values.
    /// </summary>
    public static ClipboardDebugReport CreateReport(
        ClipboardSnapshot snapshot,
        string? lastSeenText = null,
        string? lastConvertedText = null,
        int previewLimit = 1200)
    {
        var text = snapshot.Text ?? "";
        var rows = ClipboardGridApi.ParseTable(text);
        var maxColumns = rows.Count == 0 ? 0 : rows.Max(row => row.Count);
        var preview = EscapePreview(text, previewLimit);

        return new ClipboardDebugReport(
            snapshot.Owner,
            text.Length,
            rows.Count,
            maxColumns,
            string.Equals(text, lastSeenText ?? "", StringComparison.Ordinal),
            !string.IsNullOrEmpty(lastConvertedText) && string.Equals(text, lastConvertedText, StringComparison.Ordinal),
            snapshot.Formats,
            preview);
    }

    /// <summary>
    /// Escapes control characters so tabs and line endings are visible in a debug view.
    /// </summary>
    public static string EscapePreview(string text, int limit = 1200)
    {
        var preview = text
            .Replace("\r", "\\r")
            .Replace("\n", "\\n" + Environment.NewLine)
            .Replace("\t", "\\t");

        if (preview.Length <= limit)
            return preview.Length == 0 ? "(empty)" : preview;

        return preview[..limit] + Environment.NewLine + "...";
    }
}
