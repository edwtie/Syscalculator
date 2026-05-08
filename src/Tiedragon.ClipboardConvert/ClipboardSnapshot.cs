#nullable enable

namespace Tiedragon.ClipboardConvert;

/// <summary>
/// Clipboard text plus debug metadata.
/// </summary>
public readonly record struct ClipboardSnapshot(string Text, string[] Formats, string Owner)
{
    public static ClipboardSnapshot Empty => new("", Array.Empty<string>(), "none");
}
