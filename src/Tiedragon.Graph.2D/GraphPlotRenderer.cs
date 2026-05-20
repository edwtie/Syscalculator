#nullable enable

// Copyright (c) 1995-2026 Edward Tie / Tiedragon. All rights reserved.
// Renders Graph2D grids, axes, labels, range markers, lines, and points.
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using Tiedragon.Graph;

namespace Tiedragon.Graph.G2D;

/// <summary>
/// Shared renderer for all Syscalculator graph surfaces.
/// </summary>
/// <remarks>
/// The renderer owns grid, axis, label, culture-aware tick formatting, compact/normal density, and graph-to-screen mapping.
/// Data sampling stays with the caller.
/// </remarks>
public static class GraphPlotRenderer
{
    private const int MaxGridLinesPerAxis = 500;
    private const int MaxTicksPerAxis = 240;
    private const double PicoMeter = 1e-12;
    private const double LightYearMeters = 9_460_730_472_580_800d;
    private const double PlanckLengthMeters = 1.616255e-35;

    /// <summary>
    /// Returns the drawable graph rectangle inside a canvas control.
    /// </summary>
    public static Rectangle GetPlotRectangle(Control canvas)
    {
        return GraphGeometry2D.GetPlotRectangle(canvas.ClientRectangle);
    }

    /// <summary>
    /// Converts a screen/canvas point into graph coordinates for the supplied plot rectangle and view.
    /// </summary>
    public static PointF ScreenToGraph(PointF screenPoint, Rectangle plot, GraphPlotView view)
    {
        return GraphGeometry2D.ScreenToGraph(screenPoint, plot, view);
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
        GraphGeometry2D.NormalizeRange(ref minX, ref maxX, ref minY, ref maxY, padX);
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
        return GraphGeometry2D.CreateFitView(points, requestedMinX, requestedMaxX, canvasSize, fallbackHalfYRange);
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
        double requestedMinX,
        double requestedMaxX,
        double requestedStep,
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
        double requestedMinX,
        double requestedMaxX,
        double requestedStep,
        string disabledMessage,
        string emptyMessage,
        GraphPlotDensity density,
        float? requestedMinY = null,
        float? requestedMaxY = null,
        bool showRangeMarkers = true)
    {
        DrawMultiInRectangle(
            g,
            canvas.Font,
            canvas.ClientRectangle,
            series,
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

    public static void DrawInRectangle(
        Graphics g,
        Font font,
        Rectangle bounds,
        IReadOnlyList<PointF> linePoints,
        IReadOnlyList<PointF> highlightPoints,
        GraphPlotView view,
        double requestedMinX,
        double requestedMaxX,
        double requestedStep,
        string disabledMessage,
        string emptyMessage,
        GraphPlotDensity density,
        float? requestedMinY = null,
        float? requestedMaxY = null,
        bool showRangeMarkers = true)
    {
        DrawMultiInRectangle(
            g,
            font,
            bounds,
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

    private static void DrawMultiInRectangle(
        Graphics g,
        Font font,
        Rectangle bounds,
        IReadOnlyList<GraphLineSeries> series,
        IReadOnlyList<PointF> highlightPoints,
        GraphPlotView view,
        double requestedMinX,
        double requestedMaxX,
        double requestedStep,
        string disabledMessage,
        string emptyMessage,
        GraphPlotDensity density,
        float? requestedMinY = null,
        float? requestedMaxY = null,
        bool showRangeMarkers = true)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = bounds;
        if (rect.Width <= 20 || rect.Height <= 20)
            return;

        if (!GraphGeometry2D.IsValidView(view))
            return;

        var plot = GraphGeometry2D.GetPlotRectangle(rect);
        using var background = new SolidBrush(Color.White);
        g.FillRectangle(background, rect);

        if (!string.IsNullOrWhiteSpace(disabledMessage))
        {
            using var brush = new SolidBrush(Color.DimGray);
            g.DrawString(disabledMessage, font, brush, plot.Left + 12, plot.Top + 12);
            return;
        }

        if (series.Count == 0 || series.All(line => line.Points.Count == 0))
        {
            using var brush = new SolidBrush(Color.DimGray);
            g.DrawString(emptyMessage, font, brush, plot.Left + 12, plot.Top + 12);
            return;
        }

        PointF Map(PointF point) => GraphGeometry2D.GraphToScreen(point, plot, view);

        var finiteLinePoints = series
            .SelectMany(line => line.Points)
            .Where(point => float.IsFinite(point.X) && float.IsFinite(point.Y))
            .ToArray();
        double? dataMinY = finiteLinePoints.Length > 0 ? finiteLinePoints.Min(point => point.Y) : null;
        double? dataMaxY = finiteLinePoints.Length > 0 ? finiteLinePoints.Max(point => point.Y) : null;

        var strokeMultiplier = SmallScaleStrokeMultiplier(view, density);
        DrawAxes(g, font, plot, view, requestedMinX, requestedMaxX, requestedStep, requestedMinY, requestedMaxY, dataMinY, dataMaxY, density, Map, showRangeMarkers);

        foreach (var line in series)
        {
            using var linePen = new Pen(line.Color, line.Width * strokeMultiplier);
            DrawSafePolyline(g, linePen, line.Points, Map, plot);
        }

        using var pointBrush = new SolidBrush(Color.FromArgb(220, 38, 38));
        var radius = density == GraphPlotDensity.Compact ? 2.7f : 3.0f;
        var minHighlightDistance = density == GraphPlotDensity.Compact ? 7f : 9f;
        var lastHighlight = new PointF(float.NaN, float.NaN);
        foreach (var point in highlightPoints.Select(Map))
        {
            if (!IsDrawablePoint(point, plot))
                continue;

            if (float.IsFinite(lastHighlight.X) &&
                Math.Abs(point.X - lastHighlight.X) < minHighlightDistance &&
                Math.Abs(point.Y - lastHighlight.Y) < minHighlightDistance)
            {
                continue;
            }

            g.FillEllipse(pointBrush, point.X - radius, point.Y - radius, radius * 2, radius * 2);
            lastHighlight = point;
        }
    }

    private static void DrawSafePolyline(Graphics g, Pen pen, IReadOnlyList<PointF> points, Func<PointF, PointF> map, Rectangle plot)
    {
        var segment = new List<PointF>(points.Count);
        foreach (var source in points)
        {
            var point = map(source);
            if (!IsDrawablePoint(point, plot))
            {
                FlushSegment();
                continue;
            }

            segment.Add(point);
        }

        FlushSegment();

        void FlushSegment()
        {
            if (segment.Count > 1)
                g.DrawLines(pen, segment.ToArray());
            segment.Clear();
        }
    }

    private static bool IsDrawablePoint(PointF point, Rectangle plot)
    {
        if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
            return false;

        var margin = Math.Max(plot.Width, plot.Height) * 4f;
        return point.X >= plot.Left - margin &&
               point.X <= plot.Right + margin &&
               point.Y >= plot.Top - margin &&
               point.Y <= plot.Bottom + margin;
    }

    private static PointF Point(double x, double y)
    {
        return new PointF((float)x, (float)y);
    }

    private static void DrawAxes(
        Graphics g,
        Font labelFont,
        Rectangle plot,
        GraphPlotView view,
        double requestedMinX,
        double requestedMaxX,
        double requestedStep,
        double? requestedMinY,
        double? requestedMaxY,
        double? dataMinY,
        double? dataMaxY,
        GraphPlotDensity density,
        Func<PointF, PointF> map,
        bool showRangeMarkers)
    {
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var xCoarseStep = ChooseCoarseStep(plot, view, density, vertical: true);
        var yCoarseStep = ChooseCoarseStep(plot, view, density, vertical: false);
        var strokeMultiplier = SmallScaleStrokeMultiplier(view, density);
        using var axisPen = new Pen(Color.FromArgb(38, 38, 38), 1f * strokeMultiplier);
        using var majorGridPen = new Pen(
            density == GraphPlotDensity.Compact ? Color.FromArgb(185, 194, 206) : Color.FromArgb(176, 186, 199),
            (density == GraphPlotDensity.Compact ? 0.9f : 1.05f) * strokeMultiplier);
        using var minorGridPen = new Pen(
            density == GraphPlotDensity.Compact ? Color.FromArgb(234, 240, 248) : Color.FromArgb(229, 236, 246),
            (density == GraphPlotDensity.Compact ? 0.45f : 0.55f) * strokeMultiplier);
        using var labelBrush = new SolidBrush(Color.FromArgb(31, 31, 31));
        using var axisLabelBack = new SolidBrush(Color.FromArgb(248, 252, 255));
        using var tickFont = new Font("Segoe UI", density == GraphPlotDensity.Compact ? 7.5f : 8f);

        var xFineStep = xCoarseStep / 10d;
        var yFineStep = yCoarseStep / 10d;
        var xLabelStep = ChooseLabelStep(plot, view, xCoarseStep, density, vertical: true);
        var yLabelStep = ChooseLabelStep(plot, view, yCoarseStep, density, vertical: false);
        var xTicks = BuildTicksByStep(view.MinX, view.MaxX, xCoarseStep);
        var yTicks = BuildTicksByStep(view.MinY, view.MaxY, yCoarseStep);
        var xLabelTicks = BuildTicksByStep(view.MinX, view.MaxX, xLabelStep);
        var yLabelTicks = BuildTicksByStep(view.MinY, view.MaxY, yLabelStep);

        DrawSmallScaleCue(g, plot, xCoarseStep, yCoarseStep, density);

        var minFineGridPixels = ChooseMinFineGridPixels(xCoarseStep, yCoarseStep, density);
        var showFineGrid = Math.Max(Math.Abs(xCoarseStep), Math.Abs(yCoarseStep)) > PicoMeter;
        if (showFineGrid && xFineStep >= PlanckLengthMeters && PixelsPerStep(plot, view, xFineStep, vertical: true) >= minFineGridPixels)
            DrawGridLines(g, plot, view, xFineStep, vertical: true, minorGridPen, map, skipStep: xCoarseStep);
        if (showFineGrid && yFineStep >= PlanckLengthMeters && PixelsPerStep(plot, view, yFineStep, vertical: false) >= minFineGridPixels)
            DrawGridLines(g, plot, view, yFineStep, vertical: false, minorGridPen, map, skipStep: yCoarseStep);

        var lastXLabelRight = float.NegativeInfinity;
        foreach (var xValue in xTicks)
        {
            if (xValue < view.MinX || xValue > view.MaxX)
                continue;

            var xPoint = map(Point(xValue, view.MinY));
            g.DrawLine(majorGridPen, xPoint.X, plot.Top, xPoint.X, plot.Bottom);
        }

        foreach (var yValue in yTicks)
        {
            var yPoint = map(Point(view.MinX, yValue));
            g.DrawLine(majorGridPen, plot.Left, yPoint.Y, plot.Right, yPoint.Y);
        }

        var xAxisVisible = view.MinY <= 0 && view.MaxY >= 0;
        var yAxisVisible = view.MinX <= 0 && view.MaxX >= 0;

        if (xAxisVisible)
        {
            var y0 = map(Point(view.MinX, 0)).Y;
            g.DrawLine(axisPen, plot.Left, y0, plot.Right, y0);
        }

        if (yAxisVisible)
        {
            var x0 = map(Point(0, view.MinY)).X;
            g.DrawLine(axisPen, x0, plot.Top, x0, plot.Bottom);
        }

        var xAxisY = xAxisVisible ? map(Point(view.MinX, 0)).Y : plot.Bottom;
        var yAxisX = yAxisVisible ? map(Point(0, view.MinY)).X : plot.Left;
        var xLabelOffset = density == GraphPlotDensity.Compact ? 8 : 5;
        var yLabelOffset = density == GraphPlotDensity.Compact ? 8 : 6;

        foreach (var xValue in xLabelTicks)
        {
            if (xValue < view.MinX || xValue > view.MaxX)
                continue;

            if (IsZeroTick(xValue, xLabelStep) && yAxisVisible && xAxisVisible)
                continue;

            var xPoint = map(Point(xValue, view.MinY));
            var text = FormatTick(xValue, xLabelStep);
            var size = g.MeasureString(text, tickFont);
            var labelX = Math.Clamp(xPoint.X - size.Width / 2, plot.Left + 2, plot.Right - size.Width - 2);
            var labelY = Math.Clamp(xAxisY + xLabelOffset, plot.Top + 2, plot.Bottom - size.Height - 2);
            if (labelX <= lastXLabelRight + 4)
                continue;
            DrawAxisLabel(g, text, tickFont, labelBrush, axisLabelBack, labelX, labelY, size);
            lastXLabelRight = labelX + size.Width;
        }

        var yLabels = new List<(string Text, float X, float Y, SizeF Size)>();
        foreach (var yValue in yLabelTicks)
        {
            if (IsZeroTick(yValue, yLabelStep) && yAxisVisible && xAxisVisible)
                continue;

            var yPoint = map(Point(view.MinX, yValue));
            var text = FormatTick(yValue, yLabelStep);
            var size = g.MeasureString(text, tickFont);
            var labelX = Math.Clamp(yAxisX - size.Width - yLabelOffset, plot.Left + 2, plot.Right - size.Width - 2);
            var labelY = Math.Clamp(yPoint.Y - size.Height / 2, plot.Top + 2, plot.Bottom - size.Height - 2);
            yLabels.Add((text, labelX, labelY, size));
        }

        var lastYLabelBottom = float.NegativeInfinity;
        foreach (var label in yLabels.OrderBy(label => label.Y))
        {
            if (label.Y <= lastYLabelBottom + 1)
                continue;

            DrawAxisLabel(g, label.Text, tickFont, labelBrush, axisLabelBack, label.X, label.Y, label.Size);
            lastYLabelBottom = label.Y + label.Size.Height;
        }

        if (xAxisVisible && yAxisVisible)
        {
            var origin = map(new PointF(0, 0));
            var text = "0";
            var size = g.MeasureString(text, tickFont);
            var labelX = Math.Clamp(origin.X - size.Width - yLabelOffset, plot.Left + 2, plot.Right - size.Width - 2);
            var labelY = Math.Clamp(origin.Y + xLabelOffset, plot.Top + 2, plot.Bottom - size.Height - 2);
            DrawAxisLabel(g, text, tickFont, labelBrush, axisLabelBack, labelX, labelY, size);
        }

        if (showRangeMarkers)
            DrawRequestedRangeMarkers(g, plot, view, requestedMinX, requestedMaxX, requestedStep, requestedMinY, requestedMaxY, dataMinY, dataMaxY, density, tickFont, map);
    }

    private static void DrawRequestedRangeMarkers(
        Graphics g,
        Rectangle plot,
        GraphPlotView view,
        double requestedMinX,
        double requestedMaxX,
        double requestedStep,
        double? requestedMinY,
        double? requestedMaxY,
        double? dataMinY,
        double? dataMaxY,
        GraphPlotDensity density,
        Font font,
        Func<PointF, PointF> map)
    {
        var strokeMultiplier = SmallScaleStrokeMultiplier(view, density);
        using var rangePen = new Pen(Color.FromArgb(46, 110, 210), (density == GraphPlotDensity.Compact ? 0.9f : 1.15f) * strokeMultiplier)
        {
            DashStyle = DashStyle.Dash
        };
        using var rangeBrush = new SolidBrush(Color.FromArgb(25, 78, 156));
        using var labelBack = new SolidBrush(Color.FromArgb(248, 252, 255));
        using var labelBorder = new Pen(Color.FromArgb(196, 216, 246), 1f);

        if (double.IsFinite(requestedMinX) && double.IsFinite(requestedMaxX))
        {
            var minX = Math.Min(requestedMinX, requestedMaxX);
            var maxX = Math.Max(requestedMinX, requestedMaxX);
            var labelY = plot.Bottom - g.MeasureString("X", font).Height - (density == GraphPlotDensity.Compact ? 4 : 6);
            var minLabelRect = RectangleF.Empty;
            var maxLabelRect = RectangleF.Empty;

            if (minX >= view.MinX && minX <= view.MaxX)
            {
                var point = map(Point(minX, view.MinY));
                g.DrawLine(rangePen, point.X, plot.Top, point.X, plot.Bottom);
                minLabelRect = DrawMarkerLabel(g, $"X min {FormatDisplayNumber(minX)}", font, rangeBrush, labelBack, labelBorder, point.X, labelY, plot);
            }

            if (maxX >= view.MinX && maxX <= view.MaxX)
            {
                var point = map(Point(maxX, view.MinY));
                g.DrawLine(rangePen, point.X, plot.Top, point.X, plot.Bottom);
                var maxLabelY = labelY;
                if (!minLabelRect.IsEmpty)
                {
                    var testSize = g.MeasureString($"X max {FormatDisplayNumber(maxX)}", font);
                    var testX = Math.Clamp(point.X - testSize.Width / 2 - 3, plot.Left + 2, plot.Right - testSize.Width - 8);
                    maxLabelRect = new RectangleF(testX, maxLabelY, testSize.Width + 6, testSize.Height + 2);
                    if (maxLabelRect.IntersectsWith(minLabelRect))
                        maxLabelY = Math.Max(plot.Top + 4, labelY - maxLabelRect.Height - 3);
                }

                DrawMarkerLabel(g, $"X max {FormatDisplayNumber(maxX)}", font, rangeBrush, labelBack, labelBorder, point.X, maxLabelY, plot);
            }
        }

        var markerMinY = requestedMinY ?? dataMinY;
        var markerMaxY = requestedMaxY ?? dataMaxY;
        if (markerMinY.HasValue && markerMaxY.HasValue &&
            double.IsFinite(markerMinY.Value) && double.IsFinite(markerMaxY.Value))
        {
            DrawDataYRangeMarkers(g, plot, view, markerMinY.Value, markerMaxY.Value, density, font, map);
        }

        if (double.IsFinite(requestedStep) && requestedStep > 0)
            DrawCornerLabel(g, $"Step {FormatDisplayNumber(requestedStep)}", font, rangeBrush, labelBack, labelBorder, plot);
    }

    private static void DrawDataYRangeMarkers(
        Graphics g,
        Rectangle plot,
        GraphPlotView view,
        double dataMinY,
        double dataMaxY,
        GraphPlotDensity density,
        Font font,
        Func<PointF, PointF> map)
    {
        var minY = Math.Min(dataMinY, dataMaxY);
        var maxY = Math.Max(dataMinY, dataMaxY);
        var strokeMultiplier = SmallScaleStrokeMultiplier(view, density);
        using var dataPen = new Pen(Color.FromArgb(20, 125, 82), (density == GraphPlotDensity.Compact ? 0.9f : 1.15f) * strokeMultiplier)
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

            var point = map(Point(view.MinX, minY));
            g.DrawLine(dataPen, plot.Left, point.Y, plot.Right, point.Y);
            DrawYMarkerLabel(g, $"Y min/max {FormatDisplayNumber(minY)}", font, dataBrush, labelBack, labelBorder, point.Y, plot, RectangleF.Empty, preferAbove: true);
            return;
        }

        var maxLabelRect = RectangleF.Empty;
        if (maxY >= view.MinY && maxY <= view.MaxY)
        {
            var point = map(Point(view.MinX, maxY));
            g.DrawLine(dataPen, plot.Left, point.Y, plot.Right, point.Y);
            maxLabelRect = DrawYMarkerLabel(g, $"Y max {FormatDisplayNumber(maxY)}", font, dataBrush, labelBack, labelBorder, point.Y, plot, RectangleF.Empty, preferAbove: true);
        }

        if (minY >= view.MinY && minY <= view.MaxY)
        {
            var point = map(Point(view.MinX, minY));
            g.DrawLine(dataPen, plot.Left, point.Y, plot.Right, point.Y);
            DrawYMarkerLabel(g, $"Y min {FormatDisplayNumber(minY)}", font, dataBrush, labelBack, labelBorder, point.Y, plot, maxLabelRect, preferAbove: false);
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

    private static void DrawGridLines(
        Graphics g,
        Rectangle plot,
        GraphPlotView view,
        double step,
        bool vertical,
        Pen pen,
        Func<PointF, PointF> map,
        double? skipStep = null)
    {
        step = Math.Abs(step);
        if (step <= 0)
            return;

        var min = vertical ? view.MinX : view.MinY;
        var max = vertical ? view.MaxX : view.MaxY;
        if (!double.IsFinite(min) || !double.IsFinite(max) || max <= min)
            return;

        var first = Math.Floor(min / step) * step;
        if (!double.IsFinite(first))
            return;

        for (var i = 0; i < MaxGridLinesPerAxis; i++)
        {
            var value = first + step * i;
            if (!double.IsFinite(value) || value > max + step * 0.5d)
                break;

            if (value < min || value > max || IsOnStep(value, skipStep, step))
                continue;

            if (vertical)
            {
                var point = map(Point(value, view.MinY));
                g.DrawLine(pen, point.X, plot.Top, point.X, plot.Bottom);
            }
            else
            {
                var point = map(Point(view.MinX, value));
                g.DrawLine(pen, plot.Left, point.Y, plot.Right, point.Y);
            }
        }
    }

    private static bool IsOnStep(double value, double? step, double fallbackTolerance)
    {
        if (step is not { } positiveStep || positiveStep <= 0)
            return false;

        var nearest = Math.Round(value / positiveStep) * positiveStep;
        var tolerance = Math.Max(1e-12, fallbackTolerance / 10d);
        return Math.Abs(value - nearest) <= tolerance;
    }

    private static bool IsZeroTick(double value, double step)
    {
        return Math.Abs(value) <= Math.Max(1e-300, Math.Abs(step) / 1000d);
    }

    private static void DrawSmallScaleCue(Graphics g, Rectangle plot, double xCoarseStep, double yCoarseStep, GraphPlotDensity density)
    {
        var scale = Math.Max(Math.Abs(xCoarseStep), Math.Abs(yCoarseStep));
        if (scale > PicoMeter)
            return;

        var planckScale = scale <= PlanckLengthMeters * 10f;
        using var brush = new SolidBrush(planckScale
            ? Color.FromArgb(density == GraphPlotDensity.Compact ? 34 : 42, 235, 240, 255)
            : Color.FromArgb(density == GraphPlotDensity.Compact ? 18 : 24, 238, 250, 255));
        g.FillRectangle(brush, plot);

        var text = planckScale ? "Planck-minimum 1 lP" : "Subatomaire schaal";
        using var textBrush = new SolidBrush(Color.FromArgb(45, 67, 95));
        using var backBrush = new SolidBrush(Color.FromArgb(238, 247, 255));
        using var borderPen = new Pen(Color.FromArgb(184, 207, 232), 1f);
        using var font = new Font("Segoe UI", density == GraphPlotDensity.Compact ? 7f : 8f, FontStyle.Regular);
        DrawCornerLabel(g, text, font, textBrush, backBrush, borderPen, plot);
    }

    private static float SmallScaleStrokeMultiplier(GraphPlotView view, GraphPlotDensity density)
    {
        var scale = Math.Max(Math.Abs(view.MaxX - view.MinX), Math.Abs(view.MaxY - view.MinY));
        return SmallScaleStrokeMultiplier(scale, density);
    }

    private static float SmallScaleStrokeMultiplier(double scale, GraphPlotDensity density)
    {
        if (!double.IsFinite(scale) || scale > PicoMeter)
            return 1f;

        if (scale <= PlanckLengthMeters * 10d)
            return density == GraphPlotDensity.Compact ? 3.2f : 3.8f;

        if (scale <= 1e-18d)
            return density == GraphPlotDensity.Compact ? 2.4f : 2.8f;

        return density == GraphPlotDensity.Compact ? 1.8f : 2.1f;
    }

    private static void DrawAxisLabel(
        Graphics g,
        string text,
        Font font,
        Brush textBrush,
        Brush backBrush,
        float x,
        float y,
        SizeF size)
    {
        var rect = new RectangleF(x - 1, y, size.Width + 2, size.Height);
        g.FillRectangle(backBrush, rect);
        g.DrawString(text, font, textBrush, x, y);
    }

    private static double PixelsPerStep(Rectangle plot, GraphPlotView view, double step, bool vertical)
    {
        var range = vertical
            ? Math.Max(1e-300, view.MaxX - view.MinX)
            : Math.Max(1e-300, view.MaxY - view.MinY);
        if (!double.IsFinite(range) || !double.IsFinite(step))
            return 0;

        var pixels = vertical ? plot.Width : plot.Height;
        return Math.Abs(step) * pixels / range;
    }

    private static void DrawUnitGridLines(
        Graphics g,
        Rectangle plot,
        GraphPlotView view,
        Pen pen,
        GraphPlotDensity density,
        Func<PointF, PointF> map)
    {
        var xPixelsPerUnit = plot.Width / Math.Max(1e-300, view.MaxX - view.MinX);
        var yPixelsPerUnit = plot.Height / Math.Max(1e-300, view.MaxY - view.MinY);
        var minPixelsPerUnit = density == GraphPlotDensity.Compact ? 18f : 10f;
        if (Math.Min(xPixelsPerUnit, yPixelsPerUnit) < minPixelsPerUnit)
            return;

        for (var x = Math.Ceiling(view.MinX); x <= view.MaxX; x += 1d)
        {
            var point = map(Point(x, view.MinY));
            g.DrawLine(pen, point.X, plot.Top, point.X, plot.Bottom);
        }

        for (var y = Math.Ceiling(view.MinY); y <= view.MaxY; y += 1d)
        {
            var point = map(Point(view.MinX, y));
            g.DrawLine(pen, plot.Left, point.Y, plot.Right, point.Y);
        }
    }

    private static double ChooseCoarseStep(Rectangle plot, GraphPlotView view, GraphPlotDensity density, bool vertical)
    {
        var range = vertical
            ? view.MaxX - view.MinX
            : view.MaxY - view.MinY;
        var pixels = vertical ? plot.Width : plot.Height;
        if (!double.IsFinite(range) || range <= 0 || pixels <= 0)
            return 1d;

        var minFinePixels = density == GraphPlotDensity.Compact ? 7d : 8d;
        var targetFineUnits = range * minFinePixels / pixels;
        if (!double.IsFinite(targetFineUnits) || targetFineUnits <= 0)
            targetFineUnits = range / 80d;

        var step = NiceNumber(targetFineUnits, round: false) * 10d;
        if (!double.IsFinite(step) || step <= 0)
            return 1d;

        return step;
    }

    private static float ChooseMinFineGridPixels(double xCoarseStep, double yCoarseStep, GraphPlotDensity density)
    {
        var scale = Math.Max(Math.Abs(xCoarseStep), Math.Abs(yCoarseStep));
        if (scale >= 1_000_000_000f)
            return density == GraphPlotDensity.Compact ? 18f : 24f;
        if (scale >= 1_000_000f)
            return density == GraphPlotDensity.Compact ? 14f : 18f;
        if (scale >= 1_000f)
            return density == GraphPlotDensity.Compact ? 9f : 11f;
        if (scale < 0.001d)
            return density == GraphPlotDensity.Compact ? 3f : 4f;

        return density == GraphPlotDensity.Compact ? 5f : 7f;
    }

    private static double ChooseLabelStep(Rectangle plot, GraphPlotView view, double majorStep, GraphPlotDensity density, bool vertical)
    {
        var range = vertical
            ? view.MaxX - view.MinX
            : view.MaxY - view.MinY;
        var pixels = vertical ? plot.Width : plot.Height;
        if (!double.IsFinite(range) || range <= 0 || pixels <= 0)
            return majorStep;

        var halfStep = majorStep / 2d;
        var minLabelPixels = vertical
            ? density == GraphPlotDensity.Compact ? 58f : 38f
            : density == GraphPlotDensity.Compact ? 24f : 22f;

        return halfStep * pixels / range >= minLabelPixels
            ? halfStep
            : majorStep;
    }

    private static IReadOnlyList<double> BuildTicksByStep(double min, double max, double step)
    {
        if (!double.IsFinite(min) || !double.IsFinite(max) || !double.IsFinite(step) || step <= 0 || max <= min)
            return [min];

        var ticks = new List<double>();
        var first = Math.Ceiling(min / step) * step;
        if (!double.IsFinite(first))
            return ticks;

        for (var i = 0; i < MaxTicksPerAxis; i++)
        {
            var value = first + step * i;
            if (!double.IsFinite(value) || value > max + step * 0.5d)
                break;

            if (value >= min - step * 0.25d && value <= max + step * 0.25d)
                ticks.Add(Math.Abs(value) < step / 1000d ? 0 : value);
        }

        return ticks;
    }

    private static double NiceNumber(double value, bool round)
    {
        if (value <= 0)
            return 1;

        var exponent = Math.Floor(Math.Log10(value));
        var fraction = value / Math.Pow(10, exponent);
        double niceFraction;

        if (round)
        {
            niceFraction = fraction < 1.5f ? 1 : fraction < 3 ? 2 : fraction < 7 ? 5 : 10;
        }
        else
        {
            niceFraction = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 5 ? 5 : 10;
        }

        return niceFraction * Math.Pow(10, exponent);
    }

    private static double PowerOfTenStep(double value)
    {
        if (value <= 0)
            return 1;

        return Math.Pow(10, Math.Ceiling(Math.Log10(value)));
    }

    public static string FormatDisplayNumber(double value)
    {
        return GraphNumberFormatter.FormatDisplayNumber(value);
    }

    private static string FormatTick(double value)
    {
        return GraphNumberFormatter.FormatDisplayNumber(value);
    }

    private static string FormatTick(double value, double step)
    {
        return GraphNumberFormatter.FormatTick(value, step);
    }

    private static bool TryFormatPlanckTick(double value, double step, double scaleValue, out string text)
    {
        if (scaleValue >= 1e-30f)
        {
            text = string.Empty;
            return false;
        }

        var planckValue = value / PlanckLengthMeters;
        var planckStep = step / PlanckLengthMeters;
        if (Math.Abs(planckValue) < Math.Max(0.001d, Math.Abs(planckStep) / 1000d))
        {
            text = "0";
            return true;
        }

        text = FormatCompactTick(planckValue, planckStep, " lP");
        return true;
    }

    private static bool TryFormatLightYearTick(double value, double step, double scaleValue, out string text)
    {
        if (scaleValue < LightYearMeters)
        {
            text = string.Empty;
            return false;
        }

        var lyValue = value / LightYearMeters;
        var lyStep = step / LightYearMeters;
        var lyScale = scaleValue / LightYearMeters;

        if (lyScale >= 1e30f)
        {
            text = FormatScientificTick(lyValue) + " lj";
            return true;
        }
        if (lyScale >= 1e27f)
        {
            text = FormatCompactTick(lyValue / 1e27f, lyStep / 1e27f, "R lj");
            return true;
        }
        if (lyScale >= 1e24f)
        {
            text = FormatCompactTick(lyValue / 1e24f, lyStep / 1e24f, "Y lj");
            return true;
        }
        if (lyScale >= 1e21f)
        {
            text = FormatCompactTick(lyValue / 1e21f, lyStep / 1e21f, "Z lj");
            return true;
        }
        if (lyScale >= 1e18f)
        {
            text = FormatCompactTick(lyValue / 1e18f, lyStep / 1e18f, "E lj");
            return true;
        }
        if (lyScale >= 1e15f)
        {
            text = FormatCompactTick(lyValue / 1e15f, lyStep / 1e15f, "P lj");
            return true;
        }
        if (lyScale >= 1e12f)
        {
            text = FormatCompactTick(lyValue / 1e12f, lyStep / 1e12f, "T lj");
            return true;
        }
        if (lyScale >= 1e9f)
        {
            text = FormatCompactTick(lyValue / 1e9f, lyStep / 1e9f, "G lj");
            return true;
        }
        if (lyScale >= 1e6f)
        {
            text = FormatCompactTick(lyValue / 1e6f, lyStep / 1e6f, "M lj");
            return true;
        }
        if (lyScale >= 1e3f)
        {
            text = FormatCompactTick(lyValue / 1e3f, lyStep / 1e3f, "K lj");
            return true;
        }

        text = FormatCompactTick(lyValue, lyStep, " lj");
        return true;
    }

    private static bool TryFormatLargeSiTick(double value, double step, double scaleValue, out string text)
    {
        if (scaleValue >= 1e33f)
        {
            text = FormatScientificTick(value);
            return true;
        }
        if (scaleValue >= 1e30f)
        {
            text = FormatCompactTick(value / 1e30f, step / 1e30f, "Q");
            return true;
        }
        if (scaleValue >= 1e27f)
        {
            text = FormatCompactTick(value / 1e27f, step / 1e27f, "R");
            return true;
        }
        if (scaleValue >= 1e24f)
        {
            text = FormatCompactTick(value / 1e24f, step / 1e24f, "Y");
            return true;
        }
        if (scaleValue >= 1e21f)
        {
            text = FormatCompactTick(value / 1e21f, step / 1e21f, "Z");
            return true;
        }
        if (scaleValue >= 1e18f)
        {
            text = FormatCompactTick(value / 1e18f, step / 1e18f, "E");
            return true;
        }
        if (scaleValue >= 1e15f)
        {
            text = FormatCompactTick(value / 1e15f, step / 1e15f, "P");
            return true;
        }
        if (scaleValue >= 1e12f)
        {
            text = FormatCompactTick(value / 1e12f, step / 1e12f, "T");
            return true;
        }
        if (scaleValue >= 1e9f)
        {
            text = FormatCompactTick(value / 1e9f, step / 1e9f, "G");
            return true;
        }
        if (scaleValue >= 1e6f)
        {
            text = FormatCompactTick(value / 1e6f, step / 1e6f, "M");
            return true;
        }
        if (scaleValue >= 1e3f)
        {
            text = FormatCompactTick(value / 1e3f, step / 1e3f, "K");
            return true;
        }

        text = string.Empty;
        return false;
    }

    private static bool TryFormatSmallSiTick(double value, double step, double scaleValue, out string text)
    {
        if (scaleValue < 1e-30f)
        {
            text = FormatScientificTick(value);
            return true;
        }
        if (scaleValue < 1e-27f)
        {
            text = FormatCompactTick(value * 1e30f, step * 1e30f, "q");
            return true;
        }
        if (scaleValue < 1e-24f)
        {
            text = FormatCompactTick(value * 1e27f, step * 1e27f, "r");
            return true;
        }
        if (scaleValue < 1e-21f)
        {
            text = FormatCompactTick(value * 1e24f, step * 1e24f, "y");
            return true;
        }
        if (scaleValue < 1e-18f)
        {
            text = FormatCompactTick(value * 1e21f, step * 1e21f, "z");
            return true;
        }
        if (scaleValue < 1e-15f)
        {
            text = FormatCompactTick(value * 1e18f, step * 1e18f, "a");
            return true;
        }
        if (scaleValue < 1e-12f)
        {
            text = FormatCompactTick(value * 1e15f, step * 1e15f, "f");
            return true;
        }
        if (scaleValue < 1e-9f)
        {
            text = FormatCompactTick(value * 1e12f, step * 1e12f, "p");
            return true;
        }
        if (scaleValue < 1e-6f)
        {
            text = FormatCompactTick(value * 1e9f, step * 1e9f, "n");
            return true;
        }
        if (scaleValue < 1e-3f)
        {
            text = FormatCompactTick(value * 1e6f, step * 1e6f, "u");
            return true;
        }
        if (scaleValue < 1f)
        {
            text = FormatCompactTick(value * 1e3f, step * 1e3f, "m");
            return true;
        }

        text = string.Empty;
        return false;
    }

    private static string FormatCompactTick(double scaledValue, double scaledStep, string suffix)
    {
        var format = FormatForStep(scaledStep, Math.Abs(scaledValue));
        return scaledValue.ToString(format, CultureInfo.CurrentCulture) + suffix;
    }

    private static string FormatScientificTick(double value)
    {
        return value.ToString("0.###E+0", CultureInfo.CurrentCulture);
    }

    private static string FormatForStep(double step, double absValue)
    {
        if (step > 0 && step < 1d)
        {
            var decimals = Math.Clamp((int)Math.Ceiling(-Math.Log10(step)) + 1, 1, 4);
            return "0." + new string('#', decimals);
        }

        return absValue >= 100d ? "0" : "0.##";
    }
}
