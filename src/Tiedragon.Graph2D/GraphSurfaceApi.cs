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
/// High-level graph surface API.
/// </summary>
/// <remarks>
/// This facade keeps graph screens from manually composing buttons, point tables, and renderer calls.
/// It delegates to the smaller internal APIs: <see cref="GraphOverlayButton"/>,
/// <see cref="GraphPointTableOverlay"/>, and <see cref="GraphPlotRenderer"/>.
/// </remarks>
public static class GraphSurfaceApi
{
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
        float requestedMinX,
        float requestedMaxX,
        float requestedStep,
        string disabledMessage,
        string emptyMessage,
        GraphPlotDensity density)
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
            density);
    }

    /// <summary>
    /// Returns the drawable graph rectangle inside a canvas control.
    /// </summary>
    public static Rectangle GetPlotRectangle(Control canvas)
    {
        return GraphPlotRenderer.GetPlotRectangle(canvas);
    }

    /// <summary>
    /// Converts a screen/canvas point into graph coordinates.
    /// </summary>
    public static PointF ScreenToGraph(PointF screenPoint, Rectangle plot, GraphPlotView view)
    {
        return GraphPlotRenderer.ScreenToGraph(screenPoint, plot, view);
    }
}
