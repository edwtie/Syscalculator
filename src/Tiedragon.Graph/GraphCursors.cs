#nullable enable

using System.Windows.Forms;

namespace Tiedragon.Graph;

/// <summary>
/// Shared cursor choices for Tiedragon graph surfaces.
/// </summary>
public static class GraphCursors
{
    /// <summary>
    /// Cursor used when the pointer is over a graph canvas that can be panned or rotated.
    /// </summary>
    public static Cursor Pan => Cursors.Hand;

    /// <summary>
    /// Cursor used when the pointer leaves an interactive graph canvas.
    /// </summary>
    public static Cursor Default => Cursors.Default;
}
