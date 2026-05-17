#nullable enable

namespace Tiedragon.Graph2D;

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
    public const double MinimumViewSpan = 1.616255e-35;

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
        return GraphPlotRenderer.CreateFitView(points, requestedMinX, requestedMaxX, canvasSize, fallbackHalfYRange);
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
        if (plot.Width <= 0 || plot.Height <= 0)
            return view;

        var targetAspect = plot.Width / (double)plot.Height;
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

    /// <summary>
    /// Builds a canvas-aspect-corrected view from the X range controls.
    /// </summary>
    public static GraphPlotView CreateViewFromXRangeControls(GraphPlotView currentView, NumericUpDown xMin, NumericUpDown xMax, Control canvas)
    {
        return MatchViewToCanvasAspect(currentView with
        {
            MinX = (double)xMin.Value,
            MaxX = (double)xMax.Value
        }, canvas);
    }

    /// <summary>
    /// Builds a canvas-aspect-corrected view from the Y range controls.
    /// </summary>
    public static GraphPlotView CreateViewFromYRangeControls(GraphPlotView currentView, NumericUpDown yMin, NumericUpDown yMax, Control canvas)
    {
        return MatchViewToCanvasAspect(currentView with
        {
            MinY = (double)yMin.Value,
            MaxY = (double)yMax.Value
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
        return GraphPlotRenderer.ScreenToGraph(screenPoint, plot, view);
    }

    private static void SetNumberBoxValue(NumericUpDown box, double value)
    {
        if (!double.IsFinite(value))
            return;

        if (value != 0d && Math.Abs(value) < Math.Pow(10d, -box.DecimalPlaces))
            return;

        decimal decimalValue;
        try
        {
            decimalValue = (decimal)Math.Round(value, box.DecimalPlaces);
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

    private static double MinimumHalfSpan(double center)
    {
        var relativeSpan = Math.Abs(center) * 1e-14;
        return Math.Max(MinimumViewSpan / 2d, relativeSpan);
    }
}
