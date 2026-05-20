#nullable enable

namespace Tiedragon.Graph;

/// <summary>
/// Visible graph-coordinate range.
/// </summary>
/// <param name="MinX">Left graph-coordinate boundary.</param>
/// <param name="MaxX">Right graph-coordinate boundary.</param>
/// <param name="MinY">Bottom graph-coordinate boundary.</param>
/// <param name="MaxY">Top graph-coordinate boundary.</param>
public readonly record struct GraphPlotView(double MinX, double MaxX, double MinY, double MaxY);

/// <summary>
/// Render-independent 2D graph geometry and projection API.
/// </summary>
/// <remarks>
/// This keeps coordinate math reusable for Graph2D renderers, UI surfaces, and future Graph3D projection code.
/// </remarks>
public static class GraphGeometry2D
{
    public const double MinimumViewSpan = 1.616255e-35;

    public static Rectangle GetPlotRectangle(Rectangle canvasBounds)
    {
        return Rectangle.Inflate(canvasBounds, -1, -1);
    }

    public static bool IsValidView(GraphPlotView view)
    {
        return double.IsFinite(view.MinX) &&
               double.IsFinite(view.MaxX) &&
               double.IsFinite(view.MinY) &&
               double.IsFinite(view.MaxY) &&
               view.MaxX > view.MinX &&
               view.MaxY > view.MinY;
    }

    public static PointF GraphToScreen(PointF graphPoint, Rectangle plot, GraphPlotView view)
    {
        var px = plot.Left + ((graphPoint.X - view.MinX) / (view.MaxX - view.MinX)) * plot.Width;
        var py = plot.Bottom - ((graphPoint.Y - view.MinY) / (view.MaxY - view.MinY)) * plot.Height;
        return new PointF((float)px, (float)py);
    }

    public static PointF ScreenToGraph(PointF screenPoint, Rectangle plot, GraphPlotView view)
    {
        var x = view.MinX + ((screenPoint.X - plot.Left) / plot.Width) * (view.MaxX - view.MinX);
        var y = view.MaxY - ((screenPoint.Y - plot.Top) / plot.Height) * (view.MaxY - view.MinY);
        return new PointF((float)x, (float)y);
    }

    /// <summary>
    /// Expands a data range into a graph-friendly view range.
    /// </summary>
    /// <remarks>
    /// The Y range is anchored through zero when all sampled data is on one side of the axis.
    /// </remarks>
    /// <param name="padX">When true, adds horizontal padding around the X range.</param>
    public static void NormalizeRange(ref float minX, ref float maxX, ref float minY, ref float maxY, bool padX = true)
    {
        if (padX)
        {
            var xPad = Math.Max(0.5f, (maxX - minX) * 0.04f);
            minX -= xPad;
            maxX += xPad;
        }

        if (minY >= 0)
            minY = 0;
        else if (maxY <= 0)
            maxY = 0;

        var yPad = Math.Max(0.5f, (maxY - minY) * 0.08f);
        minY -= yPad;
        maxY += yPad;
    }

    /// <summary>
    /// Creates a padded view that keeps sampled graph data visible.
    /// </summary>
    public static GraphPlotView CreateFitView(
        IReadOnlyList<PointF> points,
        double requestedMinX,
        double requestedMaxX,
        Size canvasSize,
        float fallbackHalfYRange = 5f)
    {
        var finitePoints = points
            .Where(point => float.IsFinite(point.X) && float.IsFinite(point.Y))
            .ToArray();

        if (finitePoints.Length == 0)
        {
            var fallbackMinX = Math.Min(requestedMinX, requestedMaxX);
            var fallbackMaxX = Math.Max(requestedMinX, requestedMaxX);
            if (!double.IsFinite(fallbackMinX) || !double.IsFinite(fallbackMaxX))
            {
                fallbackMinX = -fallbackHalfYRange;
                fallbackMaxX = fallbackHalfYRange;
            }

            ExpandFlatRange(ref fallbackMinX, ref fallbackMaxX);
            return new GraphPlotView(fallbackMinX, fallbackMaxX, -fallbackHalfYRange, fallbackHalfYRange);
        }

        var minX = Math.Min(requestedMinX, finitePoints.Min(point => point.X));
        var maxX = Math.Max(requestedMaxX, finitePoints.Max(point => point.X));
        var minY = (double)finitePoints.Min(point => point.Y);
        var maxY = (double)finitePoints.Max(point => point.Y);

        if (minX > maxX)
            (minX, maxX) = (maxX, minX);

        ExpandFlatRange(ref minX, ref maxX);
        ExpandFlatRange(ref minY, ref maxY);
        IncludeZeroWhenClose(ref minY, ref maxY);

        var xPad = Math.Max(0.25f, (maxX - minX) * 0.04f);
        var yPad = Math.Max(0.25f, (maxY - minY) * 0.08f);
        minX -= xPad;
        maxX += xPad;
        minY -= yPad;
        maxY += yPad;

        return new GraphPlotView(minX, maxX, minY, maxY);
    }

    /// <summary>
    /// Expands the shorter axis of a view so graph units match the plot rectangle aspect ratio.
    /// </summary>
    public static GraphPlotView MatchViewToPlotAspect(GraphPlotView view, Rectangle plot)
    {
        return MatchViewToAspect(view, plot.Size);
    }

    /// <summary>
    /// Expands the shorter axis of a view so graph units match the canvas aspect ratio.
    /// </summary>
    public static GraphPlotView MatchViewToAspect(GraphPlotView view, Size canvasSize)
    {
        if (canvasSize.Width <= 0 || canvasSize.Height <= 0)
            return view;

        var targetAspect = canvasSize.Width / (double)canvasSize.Height;
        var centerX = (view.MinX + view.MaxX) / 2d;
        var centerY = (view.MinY + view.MaxY) / 2d;
        var halfX = Math.Max(MinimumHalfSpan(centerX), (view.MaxX - view.MinX) / 2d);
        var halfY = Math.Max(MinimumHalfSpan(centerY), (view.MaxY - view.MinY) / 2d);

        if (halfX / halfY < targetAspect)
            halfX = halfY * targetAspect;
        else
            halfY = halfX / targetAspect;

        return new GraphPlotView(
            centerX - halfX,
            centerX + halfX,
            centerY - halfY,
            centerY + halfY);
    }

    public static double MinimumHalfSpan(double center)
    {
        var relativeSpan = Math.Abs(center) * 1e-14;
        return Math.Max(MinimumViewSpan / 2d, relativeSpan);
    }

    private static void ExpandFlatRange(ref double min, ref double max)
    {
        if (Math.Abs(max - min) >= 0.0001f)
            return;

        var pad = Math.Max(1f, Math.Abs(min) * 0.1f);
        min -= pad;
        max += pad;
    }

    private static void IncludeZeroWhenClose(ref double minY, ref double maxY)
    {
        var range = Math.Max(1f, maxY - minY);
        if (minY > 0 && minY <= range * 3f)
            minY = 0;
        else if (maxY < 0 && Math.Abs(maxY) <= range * 3f)
            maxY = 0;
    }
}
