#nullable enable

namespace Tiedragon.Graph;

/// <summary>
/// Visual density preset for graph rendering.
/// </summary>
public enum GraphPlotDensity
{
    /// <summary>Reduced detail for embedded graph surfaces.</summary>
    Compact,

    /// <summary>Full detail for standalone graph windows and solver graphs.</summary>
    Normal
}

/// <summary>
/// A polyline series drawn in graph coordinates.
/// </summary>
/// <param name="Points">Points in graph coordinates.</param>
/// <param name="Color">Line color.</param>
/// <param name="Width">Line width in pixels.</param>
public readonly record struct GraphLineSeries(IReadOnlyList<PointF> Points, Color Color, float Width);
