#nullable enable

// Copyright (c) 1995-2026 Edward Tie / Tiedragon. All rights reserved.
// Public Graph2D surface API for chrome, range controls, drawing, and view helpers.
using Tiedragon.Graph;

namespace Tiedragon.Graph.G2D;

/// <summary>
/// UI parts created for a graph surface.
/// </summary>
public readonly record struct GraphSurfaceChrome(
    Panel NavigationPanel,
    Panel PointTablePanel,
    Panel PointTableTitleBar,
    DataGridView PointTable);

/// <summary>
/// Numeric range controls used by graph preview surfaces.
/// </summary>
public readonly record struct GraphRangeControls(
    NumericUpDown XMin,
    NumericUpDown XMax,
    NumericUpDown YMin,
    NumericUpDown YMax);

/// <summary>
/// High-level graph surface API.
/// </summary>
/// <remarks>
/// This facade keeps graph screens from manually composing buttons, point tables, and renderer calls.
/// It delegates to the smaller internal APIs: <see cref="GraphOverlayButton"/>,
/// <see cref="GraphPointTableOverlay"/>, and <see cref="GraphPlotRenderer"/>.
/// </remarks>
public static class GraphSurfaceApi
{
    public const double MinimumViewSpan = GraphGeometry2D.MinimumViewSpan;

    /// <summary>
    /// Creates the standard graph UI chrome: navigation controls and point-table overlay.
    /// </summary>
    public static GraphSurfaceChrome CreateChrome(
        GraphOverlayButtonDensity density,
        string pointsTitle,
        Func<string> pointDetailProvider,
        Action closePointTable,
        MouseEventHandler pointTableMouseDown,
        MouseEventHandler pointTableMouseMove,
        MouseEventHandler pointTableMouseUp,
        string homeTooltip,
        EventHandler homeClick,
        string zoomInTooltip,
        EventHandler zoomInClick,
        string zoomOutTooltip,
        EventHandler zoomOutClick)
    {
        var pointTable = GraphPointTableOverlay.Create(
            density,
            pointsTitle,
            pointDetailProvider,
            closePointTable,
            pointTableMouseDown,
            pointTableMouseMove,
            pointTableMouseUp);

        var navigation = GraphOverlayButton.CreateNavigationGroup(
            density,
            homeTooltip,
            homeClick,
            zoomInTooltip,
            zoomInClick,
            zoomOutTooltip,
            zoomOutClick);

        return new GraphSurfaceChrome(navigation, pointTable.Overlay, pointTable.TitleBar, pointTable.Table);
    }

    /// <summary>
    /// Draws a single-line graph through the shared renderer.
    /// </summary>
    public static void Draw(
        Graphics graphics,
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
        GraphPlotRenderer.Draw(
            graphics,
            canvas,
            linePoints,
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
    /// Draws a graph into an explicit rectangle instead of the full canvas.
    /// </summary>
    public static void DrawInRectangle(
        Graphics graphics,
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
        GraphPlotRenderer.DrawInRectangle(
            graphics,
            font,
            bounds,
            linePoints,
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
    /// Draws multiple graph lines through the shared renderer.
    /// </summary>
    public static void DrawMulti(
        Graphics graphics,
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
        GraphPlotRenderer.DrawMulti(
            graphics,
            canvas,
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

    /// <summary>
    /// Formats a graph value with the same readable SI-aware rules used by axis labels.
    /// </summary>
    public static string FormatDisplayNumber(double value)
    {
        return GraphNumberFormatter.FormatDisplayNumber(value);
    }

    /// <summary>
    /// Returns the drawable graph rectangle inside a canvas control.
    /// </summary>
    public static Rectangle GetPlotRectangle(Control canvas)
    {
        return GraphPlotRenderer.GetPlotRectangle(canvas);
    }

    /// <summary>
    /// Creates a graph view that fits the sampled data and follows the canvas aspect ratio.
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
    /// Returns true when a graph view can be projected and rendered safely.
    /// </summary>
    public static bool IsValidView(GraphPlotView view)
    {
        return GraphGeometry2D.IsValidView(view);
    }

    /// <summary>
    /// Expands a data range into a graph-friendly view range.
    /// </summary>
    public static void NormalizeRange(ref float minX, ref float maxX, ref float minY, ref float maxY, bool padX = true)
    {
        GraphGeometry2D.NormalizeRange(ref minX, ref maxX, ref minY, ref maxY, padX);
    }

    /// <summary>
    /// Creates a fit view from sampled points and expands it to the current canvas aspect ratio.
    /// </summary>
    public static GraphPlotView CreateFitViewForCanvas(
        IReadOnlyList<PointF> points,
        float fallbackMinX,
        float fallbackMaxX,
        Control canvas,
        float fallbackHalfYRange = 5f)
    {
        var requestedMinX = points.Count > 0 ? points.Min(point => point.X) : fallbackMinX;
        var requestedMaxX = points.Count > 0 ? points.Max(point => point.X) : fallbackMaxX;
        var view = CreateFitView(points, requestedMinX, requestedMaxX, canvas.ClientSize, fallbackHalfYRange);
        return MatchViewToCanvasAspect(view, canvas);
    }

    /// <summary>
    /// Expands the shorter axis of a view so graph units match the canvas aspect ratio.
    /// </summary>
    public static GraphPlotView MatchViewToCanvasAspect(GraphPlotView view, Control canvas)
    {
        var plot = GetPlotRectangle(canvas);
        return GraphGeometry2D.MatchViewToPlotAspect(view, plot);
    }

    /// <summary>
    /// Expands the shorter axis of a view so graph units match a raw canvas size.
    /// </summary>
    public static GraphPlotView MatchViewToAspect(GraphPlotView view, Size canvasSize)
    {
        return GraphGeometry2D.MatchViewToAspect(view, canvasSize);
    }

    /// <summary>
    /// Builds a canvas-aspect-corrected view from the X range controls.
    /// </summary>
    public static GraphPlotView CreateViewFromXRangeControls(GraphPlotView currentView, NumericUpDown xMin, NumericUpDown xMax, Control canvas)
    {
        return MatchViewToCanvasAspect(currentView with
        {
            MinX = GetNumberBoxValue(xMin),
            MaxX = GetNumberBoxValue(xMax)
        }, canvas);
    }

    /// <summary>
    /// Builds a canvas-aspect-corrected view from the Y range controls.
    /// </summary>
    public static GraphPlotView CreateViewFromYRangeControls(GraphPlotView currentView, NumericUpDown yMin, NumericUpDown yMax, Control canvas)
    {
        return MatchViewToCanvasAspect(currentView with
        {
            MinY = GetNumberBoxValue(yMin),
            MaxY = GetNumberBoxValue(yMax)
        }, canvas);
    }

    /// <summary>
    /// Writes a full graph view into X/Y numeric range controls.
    /// </summary>
    public static void SetRangeControlValues(GraphRangeControls controls, GraphPlotView view)
    {
        SetXRangeControlValues(controls.XMin, controls.XMax, view);
        SetYRangeControlValues(controls.YMin, controls.YMax, view);
    }

    /// <summary>
    /// Keeps range controls synchronized with the viewport when separate requested-range markers are hidden.
    /// </summary>
    public static void SyncViewportRangeControls(GraphRangeControls controls, GraphPlotView view, bool showRangeMarkers)
    {
        if (showRangeMarkers)
            return;

        SetRangeControlValues(controls, view);
    }

    /// <summary>
    /// Writes the X part of a graph view into numeric range controls.
    /// </summary>
    public static void SetXRangeControlValues(NumericUpDown xMin, NumericUpDown xMax, GraphPlotView view)
    {
        SetNumberBoxValue(xMin, view.MinX);
        SetNumberBoxValue(xMax, view.MaxX);
    }

    /// <summary>
    /// Writes the Y part of a graph view into numeric range controls.
    /// </summary>
    public static void SetYRangeControlValues(NumericUpDown yMin, NumericUpDown yMax, GraphPlotView view)
    {
        SetNumberBoxValue(yMin, view.MinY);
        SetNumberBoxValue(yMax, view.MaxY);
    }

    /// <summary>
    /// Converts a screen/canvas point into graph coordinates.
    /// </summary>
    public static PointF ScreenToGraph(PointF screenPoint, Rectangle plot, GraphPlotView view)
    {
        return GraphGeometry2D.ScreenToGraph(screenPoint, plot, view);
    }

    /// <summary>
    /// Converts graph coordinates into screen/canvas coordinates.
    /// </summary>
    public static PointF GraphToScreen(PointF graphPoint, Rectangle plot, GraphPlotView view)
    {
        return GraphGeometry2D.GraphToScreen(graphPoint, plot, view);
    }

    /// <summary>
    /// Draws a vector arrow in Graph2D coordinates.
    /// </summary>
    public static void DrawVectorArrow(
        Graphics graphics,
        PointF start,
        PointF end,
        Rectangle plot,
        GraphPlotView view,
        Color color,
        float width = 2.2f)
    {
        GraphArrowRenderer.DrawArrow(
            graphics,
            GraphToScreen(start, plot, view),
            GraphToScreen(end, plot, view),
            color,
            width);
    }

    /// <summary>
    /// Projects a formula vector to the Graph2D plane.
    /// 2D vectors already live in the plane; 3D vectors use the basic perspective projection (x/z, y/z).
    /// If z is zero, Graph2D falls back to the plain X/Y plane so the vector remains drawable.
    /// </summary>
    public static PointF ProjectFormulaVectorTo2D(IReadOnlyList<double> vector)
    {
        if (vector.Count is not 2 and not 3)
            throw new ArgumentException("Graph2D formula vectors must have 2 or 3 components.", nameof(vector));

        var x = vector[0];
        var y = vector[1];

        if (vector.Count == 2 || vector[2] == 0d)
            return new PointF((float)x, (float)y);

        var z = vector[2];
        return new PointF((float)(x / z), (float)(y / z));
    }

    public static double GetNumberBoxValue(NumericUpDown box)
    {
        return box.Tag is double exact && double.IsFinite(exact)
            ? exact
            : (double)box.Value;
    }

    public static void SetNumberBoxValue(NumericUpDown box, double value)
    {
        if (!double.IsFinite(value))
            return;

        box.Tag = value;
        box.DecimalPlaces = ChooseDisplayDecimalPlaces(value);

        decimal decimalValue;
        try
        {
            decimalValue = decimal.Round((decimal)value, box.DecimalPlaces);
        }
        catch (OverflowException)
        {
            decimalValue = value < 0 ? box.Minimum : box.Maximum;
        }

        if (decimalValue < box.Minimum)
            decimalValue = box.Minimum;
        else if (decimalValue > box.Maximum)
            decimalValue = box.Maximum;

        box.Value = decimalValue;
    }

    public static void CommitNumberBoxValue(NumericUpDown box)
    {
        box.Tag = (double)box.Value;
    }

    private static int ChooseDisplayDecimalPlaces(double value)
    {
        var abs = Math.Abs(value);
        if (abs == 0d || abs >= 0.01d)
            return 1;

        var exponent = Math.Floor(Math.Log10(abs));
        return Math.Clamp((int)(-exponent + 1), 1, 12);
    }

}
