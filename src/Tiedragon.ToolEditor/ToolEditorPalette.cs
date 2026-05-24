#nullable enable

namespace Tiedragon.ToolEditor;

/// <summary>
/// Color set used by ToolEditor buttons and tool strips.
/// </summary>
public readonly record struct ToolEditorPalette(
    Color ToolbarBack,
    Color Text,
    Color Accent,
    Color Success,
    Color Warning,
    Color SoftBlue,
    Color Folder,
    Color FolderAccent)
{
    /// <summary>
    /// Default calm editor palette.
    /// </summary>
    public static ToolEditorPalette Default { get; } = new(
        Color.FromArgb(245, 245, 245),
        Color.FromArgb(31, 41, 55),
        Color.FromArgb(37, 99, 235),
        Color.FromArgb(22, 163, 74),
        Color.FromArgb(234, 88, 12),
        Color.FromArgb(239, 246, 255),
        Color.FromArgb(254, 240, 138),
        Color.FromArgb(250, 204, 21));

    /// <summary>
    /// Dark editor palette used when WinForms runs in dark color mode.
    /// </summary>
    public static ToolEditorPalette Dark { get; } = new(
        Color.FromArgb(31, 41, 55),
        Color.FromArgb(226, 232, 240),
        Color.FromArgb(96, 165, 250),
        Color.FromArgb(34, 197, 94),
        Color.FromArgb(251, 191, 36),
        Color.FromArgb(30, 58, 138),
        Color.FromArgb(250, 204, 21),
        Color.FromArgb(234, 179, 8));
}
