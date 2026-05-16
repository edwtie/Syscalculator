#nullable enable
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;

namespace Tiedragon.Graph2D;

/// <summary>
/// Visual density preset for graph rendering.
/// </summary>
public enum GraphPlotDensity
{
    /// <summary>Reduced detail for the embedded Graph Preview tab.</summary>
    Compact,

    /// <summary>Full detail for the standalone Graph Preview window and solver graph.</summary>
    Normal
}

/// <summary>
/// Visible graph-coordinate range.
/// </summary>
/// <param name="MinX">Left graph-coordinate boundary.</param>
/// <param name="MaxX">Right graph-coordinate boundary.</param>
/// <param name="MinY">Bottom graph-coordinate boundary.</param>
/// <param name="MaxY">Top graph-coordinate boundary.</param>
public readonly record struct GraphPlotView(float MinX, float MaxX, float MinY, float MaxY);

/// <summary>
/// A polyline series drawn in graph coordinates.
/// </summary>
/// <param name="Points">Points in graph coordinates.</param>
/// <param name="Color">Line color.</param>
/// <param name="Width">Line width in pixels.</param>
public readonly record struct GraphLineSeries(IReadOnlyList<PointF> Points, Color Color, float Width);

/// <summary>
/// Shared renderer for all Syscalculator graph surfaces.
/// </summary>
/// <remarks>
/// The renderer owns grid, axis, label, culture-aware tick formatting, compact/normal density, and graph-to-screen mapping.
/// Data sampling stays with the caller.
/// </remarks>
public static class GraphPlotRenderer
{
    /// <summary>
    /// Returns the drawable graph rectangle inside a canvas control.
    /// </summary>
    public static Rectangle GetPlotRectangle(Control canvas)
    {
        return Rectangle.Inflate(canvas.ClientRectangle, -1, -1);
    }

    /// <summary>
    /// Converts a screen/canvas point into graph coordinates for the supplied plot rectangle and view.
    /// </summary>
    public static PointF ScreenToGraph(PointF screenPoint, Rectangle plot, GraphPlotView view)
    {
        var x = view.MinX + ((screenPoint.X - plot.Left) / plot.Width) * (view.MaxX - view.MinX);
        var y = view.MaxY - ((screenPoint.Y - plot.Top) / plot.Height) * (view.MaxY - view.MinY);
        return new PointF(x, y);
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
        float requestedMinX,
        float requestedMaxX,
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
            if (!float.IsFinite(fallbackMinX) || !float.IsFinite(fallbackMaxX))
            {
                fallbackMinX = -fallbackHalfYRange;
                fallbackMaxX = fallbackHalfYRange;
            }

            ExpandFlatRange(ref fallbackMinX, ref fallbackMaxX);
            return new GraphPlotView(fallbackMinX, fallbackMaxX, -fallbackHalfYRange, fallbackHalfYRange);
        }

        var minX = Math.Min(requestedMinX, finitePoints.Min(point => point.X));
        var maxX = Math.Max(requestedMaxX, finitePoints.Max(point => point.X));
        var minY = finitePoints.Min(point => point.Y);
        var maxY = finitePoints.Max(point => point.Y);

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
    /// Draws a single graph line with optional highlighted sample points.
    /// </summary>
    /// <remarks>
    /// Axis tick spacing is selected from the current zoom/view and is independent of <paramref name="requestedStep"/>.
    /// The requested range and step are kept in the signature for graph-preview callers that pass their input controls through one API.
    /// </remarks>
    public static void Draw(
        Graphics g,
        Control canvas,
        IReadOnlyList<PointF> linePoints,
        IReadOnlyList<PointF> highlightPoints,
        GraphPlotView view,
        float requestedMinX,
        float requestedMaxX,
        float requestedStep,
        string disabledMessage,
        string emptyMessage,
        GraphPlotDensity density,
        float? requestedMinY = null,
        float? requestedMaxY = null,
        bool showRangeMarkers = true)
    {
        DrawMulti(
            g,
            canvas,
            [new GraphLineSeries(linePoints, Color.FromArgb(15, 63, 143), density == GraphPlotDensity.Compact ? 2.2f : 2.4f)],
            highlightPoints,
            view,
            requestedMinX,
            requestedMaxX,
            requestedStep,
            disabledMessage,
            emptyMessage,
            density,
            requestedMinY,
            requestedMaxY,
            showRangeMarkers);
    }

    /// <summary>
    /// Draws multiple graph lines with optional highlighted sample points.
    /// </summary>
    /// <remarks>
    /// Use this for solver graphs or comparisons where more than one line should share the same axes and grid.
    /// Axis labels use <see cref="CultureInfo.CurrentCulture"/> for decimal formatting.
    /// </remarks>
    public static void DrawMulti(
        Graphics g,
        Control canvas,
        IReadOnlyList<GraphLineSeries> series,
        IReadOnlyList<PointF> highlightPoints,
        GraphPlotView view,
        float requestedMinX,
        float requestedMaxX,
        float requestedStep,
        string disabledMessage,
        string emptyMessage,
        GraphPlotDensity density,
        float? requestedMinY = null,
        float? requestedMaxY = null,
        bool showRangeMarkers = true)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = canvas.ClientRectangle;
        if (rect.Width <= 20 || rect.Height <= 20)
            return;

        var plot = GetPlotRectangle(canvas);
        using var background = new SolidBrush(Color.White);
        g.FillRectangle(background, rect);

        if (!string.IsNullOrWhiteSpace(disabledMessage))
        {
            using var brush = new SolidBrush(Color.DimGray);
            g.DrawString(disabledMessage, canvas.Font, brush, plot.Left + 12, plot.Top + 12);
            return;
        }

        if (series.Count == 0 || series.All(line => line.Points.Count == 0))
        {
            using var brush = new SolidBrush(Color.DimGray);
            g.DrawString(emptyMessage, canvas.Font, brush, plot.Left + 12, plot.Top + 12);
            return;
        }

        PointF Map(PointF point)
        {
            var px = plot.Left + ((point.X - view.MinX) / (view.MaxX - view.MinX)) * plot.Width;
            var py = plot.Bottom - ((point.Y - view.MinY) / (view.MaxY - view.MinY)) * plot.Height;
            return new PointF(px, py);
        }

        var finiteLinePoints = series
            .SelectMany(line => line.Points)
            .Where(point => float.IsFinite(point.X) && float.IsFinite(point.Y))
            .ToArray();
        float? dataMinY = finiteLinePoints.Length > 0 ? finiteLinePoints.Min(point => point.Y) : null;
        float? dataMaxY = finiteLinePoints.Length > 0 ? finiteLinePoints.Max(point => point.Y) : null;

        DrawAxes(g, canvas.Font, plot, view, requestedMinX, requestedMaxX, requestedStep, requestedMinY, requestedMaxY, dataMinY, dataMaxY, density, Map, showRangeMarkers);

        foreach (var line in series)
        {
            var mappedLine = line.Points.Select(Map).ToArray();
            using var linePen = new Pen(line.Color, line.Width);
            if (mappedLine.Length > 1)
                g.DrawLines(linePen, mappedLine);
        }

        using var pointBrush = new SolidBrush(Color.FromArgb(220, 38, 38));
        var radius = density == GraphPlotDensity.Compact ? 2.7f : 3.0f;
        foreach (var point in highlightPoints.Select(Map))
            g.FillEllipse(pointBrush, point.X - radius, point.Y - radius, radius * 2, radius * 2);
    }

    private static void DrawAxes(
        Graphics g,
        Font labelFont,
        Rectangle plot,
        GraphPlotView view,
        float requestedMinX,
        float requestedMaxX,
        float requestedStep,
        float? requestedMinY,
        float? requestedMaxY,
        float? dataMinY,
        float? dataMaxY,
        GraphPlotDensity density,
        Func<PointF, PointF> map,
        bool showRangeMarkers)
    {
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        using var axisPen = new Pen(Color.FromArgb(38, 38, 38), 1f);
        using var majorGridPen = new Pen(Color.FromArgb(185, 190, 198), 1f);
        using var minorGridPen = new Pen(Color.FromArgb(226, 229, 234), density == GraphPlotDensity.Compact ? 0.7f : 1f);
        using var unitGridPen = new Pen(
            density == GraphPlotDensity.Compact ? Color.FromArgb(176, 182, 190) : Color.FromArgb(163, 169, 178),
            density == GraphPlotDensity.Compact ? 1f : 1.35f);
        using var labelBrush = new SolidBrush(Color.FromArgb(31, 31, 31));
        using var tickFont = new Font("Segoe UI", density == GraphPlotDensity.Compact ? 7.5f : 8f);

        var majorStep = ChooseMajorStep(plot, view);
        var xTicks = BuildTicksByStep(view.MinX, view.MaxX, majorStep);
        var yTicks = BuildTicksByStep(view.MinY, view.MaxY, majorStep);

        DrawMinorGridLines(g, plot, view, xTicks, vertical: true, minorGridPen, majorStep, density, map);
        DrawMinorGridLines(g, plot, view, yTicks, vertical: false, minorGridPen, majorStep, density, map);

        foreach (var xValue in xTicks)
        {
            if (xValue < view.MinX || xValue > view.MaxX)
                continue;

            var xPoint = map(new PointF(xValue, view.MinY));
            g.DrawLine(majorGridPen, xPoint.X, plot.Top, xPoint.X, plot.Bottom);
        }

        foreach (var yValue in yTicks)
        {
            var yPoint = map(new PointF(view.MinX, yValue));
            g.DrawLine(majorGridPen, plot.Left, yPoint.Y, plot.Right, yPoint.Y);
        }

        if (majorStep < 1f)
            DrawUnitGridLines(g, plot, view, unitGridPen, density, map);

        var xAxisVisible = view.MinY <= 0 && view.MaxY >= 0;
        var yAxisVisible = view.MinX <= 0 && view.MaxX >= 0;

        if (xAxisVisible)
        {
            var y0 = map(new PointF(view.MinX, 0)).Y;
            g.DrawLine(axisPen, plot.Left, y0, plot.Right, y0);
        }

        if (yAxisVisible)
        {
            var x0 = map(new PointF(0, view.MinY)).X;
            g.DrawLine(axisPen, x0, plot.Top, x0, plot.Bottom);
        }

        var xAxisY = xAxisVisible ? map(new PointF(view.MinX, 0)).Y : plot.Bottom;
        var yAxisX = yAxisVisible ? map(new PointF(0, view.MinY)).X : plot.Left;
        var xLabelOffset = density == GraphPlotDensity.Compact ? 4 : 5;
        var yLabelOffset = density == GraphPlotDensity.Compact ? 5 : 6;

        foreach (var xValue in xTicks)
        {
            if (xValue < view.MinX || xValue > view.MaxX)
                continue;

            if (Math.Abs(xValue) < 0.000001f && yAxisVisible && xAxisVisible)
                continue;

            var xPoint = map(new PointF(xValue, view.MinY));
            var text = FormatTick(xValue);
            var size = g.MeasureString(text, tickFont);
            var labelX = Math.Clamp(xPoint.X - size.Width / 2, plot.Left + 2, plot.Right - size.Width - 2);
            var labelY = Math.Clamp(xAxisY + xLabelOffset, plot.Top + 2, plot.Bottom - size.Height - 2);
            g.DrawString(text, tickFont, labelBrush, labelX, labelY);
        }

        foreach (var yValue in yTicks)
        {
            if (Math.Abs(yValue) < 0.000001f && yAxisVisible && xAxisVisible)
                continue;

            var yPoint = map(new PointF(view.MinX, yValue));
            var text = FormatTick(yValue);
            var size = g.MeasureString(text, tickFont);
            var labelX = Math.Clamp(yAxisX - size.Width - yLabelOffset, plot.Left + 2, plot.Right - size.Width - 2);
            var labelY = Math.Clamp(yPoint.Y - size.Height / 2, plot.Top + 2, plot.Bottom - size.Height - 2);
            g.DrawString(text, tickFont, labelBrush, labelX, labelY);
        }

        if (xAxisVisible && yAxisVisible)
        {
            var origin = map(new PointF(0, 0));
            var text = "0";
            var size = g.MeasureString(text, tickFont);
            var labelX = Math.Clamp(origin.X - size.Width - yLabelOffset, plot.Left + 2, plot.Right - size.Width - 2);
            var labelY = Math.Clamp(origin.Y + xLabelOffset, plot.Top + 2, plot.Bottom - size.Height - 2);
            g.DrawString(text, tickFont, labelBrush, labelX, labelY);
        }

        if (showRangeMarkers)
            DrawRequestedRangeMarkers(g, plot, view, requestedMinX, requestedMaxX, requestedStep, requestedMinY, requestedMaxY, dataMinY, dataMaxY, density, tickFont, map);
    }

    private static void DrawRequestedRangeMarkers(
        Graphics g,
        Rectangle plot,
        GraphPlotView view,
        float requestedMinX,
        float requestedMaxX,
        float requestedStep,
        float? requestedMinY,
        float? requestedMaxY,
        float? dataMinY,
        float? dataMaxY,
        GraphPlotDensity density,
        Font font,
        Func<PointF, PointF> map)
    {
        using var rangePen = new Pen(Color.FromArgb(46, 110, 210), density == GraphPlotDensity.Compact ? 0.9f : 1.15f)
        {
            DashStyle = DashStyle.Dash
        };
        using var rangeBrush = new SolidBrush(Color.FromArgb(25, 78, 156));
        using var labelBack = new SolidBrush(Color.FromArgb(248, 252, 255));
        using var labelBorder = new Pen(Color.FromArgb(196, 216, 246), 1f);

        if (float.IsFinite(requestedMinX) && float.IsFinite(requestedMaxX))
        {
            var minX = Math.Min(requestedMinX, requestedMaxX);
            var maxX = Math.Max(requestedMinX, requestedMaxX);
            var labelY = plot.Bottom - g.MeasureString("X", font).Height - (density == GraphPlotDensity.Compact ? 4 : 6);
            var minLabelRect = RectangleF.Empty;
            var maxLabelRect = RectangleF.Empty;

            if (minX >= view.MinX && minX <= view.MaxX)
            {
                var point = map(new PointF(minX, view.MinY));
                g.DrawLine(rangePen, point.X, plot.Top, point.X, plot.Bottom);
                minLabelRect = DrawMarkerLabel(g, $"X min {FormatTick(minX)}", font, rangeBrush, labelBack, labelBorder, point.X, labelY, plot);
            }

            if (maxX >= view.MinX && maxX <= view.MaxX)
            {
                var point = map(new PointF(maxX, view.MinY));
                g.DrawLine(rangePen, point.X, plot.Top, point.X, plot.Bottom);
                var maxLabelY = labelY;
                if (!minLabelRect.IsEmpty)
                {
                    var testSize = g.MeasureString($"X max {FormatTick(maxX)}", font);
                    var testX = Math.Clamp(point.X - testSize.Width / 2 - 3, plot.Left + 2, plot.Right - testSize.Width - 8);
                    maxLabelRect = new RectangleF(testX, maxLabelY, testSize.Width + 6, testSize.Height + 2);
                    if (maxLabelRect.IntersectsWith(minLabelRect))
                        maxLabelY = Math.Max(plot.Top + 4, labelY - maxLabelRect.Height - 3);
                }

                DrawMarkerLabel(g, $"X max {FormatTick(maxX)}", font, rangeBrush, labelBack, labelBorder, point.X, maxLabelY, plot);
            }
        }

        var markerMinY = requestedMinY ?? dataMinY;
        var markerMaxY = requestedMaxY ?? dataMaxY;
        if (markerMinY.HasValue && markerMaxY.HasValue &&
            float.IsFinite(markerMinY.Value) && float.IsFinite(markerMaxY.Value))
        {
            DrawDataYRangeMarkers(g, plot, view, markerMinY.Value, markerMaxY.Value, density, font, map);
        }

        if (float.IsFinite(requestedStep) && requestedStep > 0)
            DrawCornerLabel(g, $"Step {FormatTick(requestedStep)}", font, rangeBrush, labelBack, labelBorder, plot);
    }

    private static void DrawDataYRangeMarkers(
        Graphics g,
        Rectangle plot,
        GraphPlotView view,
        float dataMinY,
        float dataMaxY,
        GraphPlotDensity density,
        Font font,
        Func<PointF, PointF> map)
    {
        var minY = Math.Min(dataMinY, dataMaxY);
        var maxY = Math.Max(dataMinY, dataMaxY);
        using var dataPen = new Pen(Color.FromArgb(20, 125, 82), density == GraphPlotDensity.Compact ? 0.9f : 1.15f)
        {
            DashStyle = DashStyle.DashDot
        };
        using var dataBrush = new SolidBrush(Color.FromArgb(12, 103, 68));
        using var labelBack = new SolidBrush(Color.FromArgb(248, 255, 251));
        using var labelBorder = new Pen(Color.FromArgb(187, 226, 204), 1f);

        if (Math.Abs(maxY - minY) < 0.000001f)
        {
            if (minY < view.MinY || minY > view.MaxY)
                return;

            var point = map(new PointF(view.MinX, minY));
            g.DrawLine(dataPen, plot.Left, point.Y, plot.Right, point.Y);
            DrawYMarkerLabel(g, $"Y min/max {FormatTick(minY)}", font, dataBrush, labelBack, labelBorder, point.Y, plot, RectangleF.Empty, preferAbove: true);
            return;
        }

        var maxLabelRect = RectangleF.Empty;
        if (maxY >= view.MinY && maxY <= view.MaxY)
        {
            var point = map(new PointF(view.MinX, maxY));
            g.DrawLine(dataPen, plot.Left, point.Y, plot.Right, point.Y);
            maxLabelRect = DrawYMarkerLabel(g, $"Y max {FormatTick(maxY)}", font, dataBrush, labelBack, labelBorder, point.Y, plot, RectangleF.Empty, preferAbove: true);
        }

        if (minY >= view.MinY && minY <= view.MaxY)
        {
            var point = map(new PointF(view.MinX, minY));
            g.DrawLine(dataPen, plot.Left, point.Y, plot.Right, point.Y);
            DrawYMarkerLabel(g, $"Y min {FormatTick(minY)}", font, dataBrush, labelBack, labelBorder, point.Y, plot, maxLabelRect, preferAbove: false);
        }
    }

    private static RectangleF DrawMarkerLabel(
        Graphics g,
        string text,
        Font font,
        Brush textBrush,
        Brush backBrush,
        Pen borderPen,
        float centerX,
        float y,
        Rectangle plot)
    {
        var size = g.MeasureString(text, font);
        var rect = new RectangleF(
            Math.Clamp(centerX - size.Width / 2 - 3, plot.Left + 2, plot.Right - size.Width - 8),
            y,
            size.Width + 6,
            size.Height + 2);
        g.FillRectangle(backBrush, rect);
        g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width, rect.Height);
        g.DrawString(text, font, textBrush, rect.X + 3, rect.Y + 1);
        return rect;
    }

    private static RectangleF DrawYMarkerLabel(
        Graphics g,
        string text,
        Font font,
        Brush textBrush,
        Brush backBrush,
        Pen borderPen,
        float centerY,
        Rectangle plot,
        RectangleF avoidRect,
        bool preferAbove)
    {
        var rect = BuildYMarkerLabelRect(g, text, font, centerY, plot);
        if (!avoidRect.IsEmpty && rect.IntersectsWith(avoidRect))
        {
            var above = Math.Max(plot.Top + 2, avoidRect.Top - rect.Height - 3);
            var below = Math.Min(plot.Bottom - rect.Height - 2, avoidRect.Bottom + 3);
            rect.Y = preferAbove ? above : below;
            if (rect.IntersectsWith(avoidRect))
                rect.Y = preferAbove ? below : above;
        }

        g.FillRectangle(backBrush, rect);
        g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width, rect.Height);
        g.DrawString(text, font, textBrush, rect.X + 3, rect.Y + 1);
        return rect;
    }

    private static RectangleF BuildYMarkerLabelRect(Graphics g, string text, Font font, float centerY, Rectangle plot)
    {
        var size = g.MeasureString(text, font);
        return new RectangleF(
            plot.Right - size.Width - 10,
            Math.Clamp(centerY - size.Height / 2 - 1, plot.Top + 2, plot.Bottom - size.Height - 4),
            size.Width + 6,
            size.Height + 2);
    }

    private static void DrawCornerLabel(
        Graphics g,
        string text,
        Font font,
        Brush textBrush,
        Brush backBrush,
        Pen borderPen,
        Rectangle plot)
    {
        var size = g.MeasureString(text, font);
        var rect = new RectangleF(plot.Left + 6, plot.Top + 5, size.Width + 8, size.Height + 2);
        g.FillRectangle(backBrush, rect);
        g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width, rect.Height);
        g.DrawString(text, font, textBrush, rect.X + 4, rect.Y + 1);
    }

    private static void DrawMinorGridLines(
        Graphics g,
        Rectangle plot,
        GraphPlotView view,
        IReadOnlyList<float> majorTicks,
        bool vertical,
        Pen pen,
        float majorStep,
        GraphPlotDensity density,
        Func<PointF, PointF> map)
    {
        if (majorTicks.Count < 2)
            return;

        var step = Math.Abs(majorTicks[1] - majorTicks[0]);
        if (step <= 0)
            return;

        var minorDivisions = density == GraphPlotDensity.Compact
            ? 2f
            : majorStep > 1f ? majorStep : 5f;
        var minorStep = step / minorDivisions;
        var min = vertical ? view.MinX : view.MinY;
        var max = vertical ? view.MaxX : view.MaxY;
        var first = MathF.Floor(min / minorStep) * minorStep;

        for (var value = first; value <= max + minorStep * 0.5f; value += minorStep)
        {
            if (value < min || value > max || IsOnMajorTick(value, majorTicks, minorStep))
                continue;

            if (vertical)
            {
                var point = map(new PointF(value, view.MinY));
                g.DrawLine(pen, point.X, plot.Top, point.X, plot.Bottom);
            }
            else
            {
                var point = map(new PointF(view.MinX, value));
                g.DrawLine(pen, plot.Left, point.Y, plot.Right, point.Y);
            }
        }
    }

    private static bool IsOnMajorTick(float value, IReadOnlyList<float> majorTicks, float minorStep)
    {
        var tolerance = Math.Max(0.00001f, minorStep / 10f);
        return majorTicks.Any(tick => Math.Abs(value - tick) <= tolerance);
    }

    private static void DrawUnitGridLines(
        Graphics g,
        Rectangle plot,
        GraphPlotView view,
        Pen pen,
        GraphPlotDensity density,
        Func<PointF, PointF> map)
    {
        var xPixelsPerUnit = plot.Width / Math.Max(0.000001f, view.MaxX - view.MinX);
        var yPixelsPerUnit = plot.Height / Math.Max(0.000001f, view.MaxY - view.MinY);
        var minPixelsPerUnit = density == GraphPlotDensity.Compact ? 18f : 10f;
        if (Math.Min(xPixelsPerUnit, yPixelsPerUnit) < minPixelsPerUnit)
            return;

        for (var x = MathF.Ceiling(view.MinX); x <= view.MaxX; x += 1f)
        {
            var point = map(new PointF(x, view.MinY));
            g.DrawLine(pen, point.X, plot.Top, point.X, plot.Bottom);
        }

        for (var y = MathF.Ceiling(view.MinY); y <= view.MaxY; y += 1f)
        {
            var point = map(new PointF(view.MinX, y));
            g.DrawLine(pen, plot.Left, point.Y, plot.Right, point.Y);
        }
    }

    private static float ChooseMajorStep(Rectangle plot, GraphPlotView view)
    {
        var xPixelsPerUnit = plot.Width / Math.Max(0.000001f, view.MaxX - view.MinX);
        var yPixelsPerUnit = plot.Height / Math.Max(0.000001f, view.MaxY - view.MinY);
        var pixelsPerUnit = Math.Min(xPixelsPerUnit, yPixelsPerUnit);

        var targetUnits = 46f / Math.Max(1f, pixelsPerUnit);
        return NiceNumber(targetUnits, round: true);
    }

    private static IReadOnlyList<float> BuildTicksByStep(float min, float max, float step)
    {
        if (step <= 0 || max <= min)
            return [min];

        var ticks = new List<float>();
        var first = MathF.Ceiling(min / step) * step;
        for (var value = first; value <= max + step * 0.5f; value += step)
        {
            if (value >= min - step * 0.25f && value <= max + step * 0.25f)
                ticks.Add(Math.Abs(value) < step / 1000f ? 0 : value);
        }

        return ticks;
    }

    private static float NiceNumber(float value, bool round)
    {
        if (value <= 0)
            return 1;

        var exponent = MathF.Floor(MathF.Log10(value));
        var fraction = value / MathF.Pow(10, exponent);
        float niceFraction;

        if (round)
        {
            niceFraction = fraction < 1.5f ? 1 : fraction < 3 ? 2 : fraction < 7 ? 5 : 10;
        }
        else
        {
            niceFraction = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 5 ? 5 : 10;
        }

        return niceFraction * MathF.Pow(10, exponent);
    }

    private static void ExpandFlatRange(ref float min, ref float max)
    {
        if (Math.Abs(max - min) >= 0.0001f)
            return;

        var pad = Math.Max(1f, Math.Abs(min) * 0.1f);
        min -= pad;
        max += pad;
    }

    private static void IncludeZeroWhenClose(ref float minY, ref float maxY)
    {
        var range = Math.Max(1f, maxY - minY);
        if (minY > 0 && minY <= range * 3f)
            minY = 0;
        else if (maxY < 0 && Math.Abs(maxY) <= range * 3f)
            maxY = 0;
    }

    private static string FormatTick(float value)
    {
        return Math.Abs(value) < 0.000001f
            ? "0"
            : value.ToString("0.##", CultureInfo.CurrentCulture);
    }
}
