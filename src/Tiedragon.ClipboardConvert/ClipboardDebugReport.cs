#nullable enable

namespace Tiedragon.ClipboardConvert;

/// <summary>
/// Human-readable clipboard debug report.
/// </summary>
public readonly record struct ClipboardDebugReport(
    string Owner,
    int TextLength,
    int LineCount,
    int ColumnCount,
    bool SameAsLastSeen,
    bool SameAsLastConverted,
    string[] Formats,
    string Preview)
{
    public string ToDisplayText()
    {
        var formats = Formats.Length == 0
            ? "  (none)"
            : "  - " + string.Join(Environment.NewLine + "  - ", Formats);

        return
            "Clipboard Debug" + Environment.NewLine +
            "===============" + Environment.NewLine +
            $"Owner              : {Owner}" + Environment.NewLine +
            $"Text length        : {TextLength}" + Environment.NewLine +
            $"Rows               : {LineCount}" + Environment.NewLine +
            $"Max columns        : {ColumnCount}" + Environment.NewLine +
            $"Same as last seen  : {FormatBool(SameAsLastSeen)}" + Environment.NewLine +
            $"Same as converted  : {FormatBool(SameAsLastConverted)}" + Environment.NewLine +
            Environment.NewLine +
            "Formats" + Environment.NewLine +
            "-------" + Environment.NewLine +
            formats + Environment.NewLine +
            Environment.NewLine +
            "Preview" + Environment.NewLine +
            "-------" + Environment.NewLine +
            Preview;
    }

    private static string FormatBool(bool value) => value ? "yes" : "no";
}
