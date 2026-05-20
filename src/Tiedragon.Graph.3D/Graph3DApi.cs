#nullable enable

// Central entry point for Graph3D: camera, grid, projection, and drawing helpers.
using Tiedragon.Graph;

namespace Tiedragon.Graph.G3D;

/// <summary>
/// Public facade for Graph3D geometry, Z-range handling, and 3D-to-2D projection.
/// </summary>
public static class Graph3DApi
{
    /// <summary>
    /// Gets the standard camera used when a Graph3D surface is first opened.
    /// </summary>
    public static GraphCamera3D DefaultCamera => GraphCameraNavigator3D.Default;

    /// <summary>
    /// Creates a camera for a named view such as 2D, top, or isometric.
    /// </summary>
    public static GraphCamera3D CameraPreset(GraphCameraPreset3D preset)
    {
        return GraphCameraNavigator3D.Preset(preset);
    }

    /// <summary>
    /// Rotates an existing camera by yaw and pitch deltas in degrees.
    /// </summary>
    public static GraphCamera3D RotateCamera(GraphCamera3D camera, double deltaYawDegrees, double deltaPitchDegrees)
    {
        return GraphCameraNavigator3D.Rotate(camera, deltaYawDegrees, deltaPitchDegrees);
    }

    /// <summary>
    /// Applies a zoom factor to the camera while keeping it inside the supported zoom range.
    /// </summary>
    public static GraphCamera3D ZoomCamera(GraphCamera3D camera, double factor)
    {
        return GraphCameraNavigator3D.Zoom(camera, factor);
    }

    /// <summary>
    /// Resets the camera to the requested preset.
    /// </summary>
    public static GraphCamera3D ResetCamera(GraphCameraPreset3D preset = GraphCameraPreset3D.Isometric)
    {
        return GraphCameraNavigator3D.Reset(preset);
    }

    /// <summary>
    /// Draws the circular compass dial that shows the current Graph3D camera heading.
    /// </summary>
    public static void DrawCompass(
        Graphics graphics,
        Rectangle bounds,
        GraphCamera3D camera,
        bool hover = false,
        bool pressed = false)
    {
        Graph3DCompass.DrawCompass(graphics, bounds, camera, hover, pressed);
    }

    /// <summary>
    /// Draws the degree text below a compass dial.
    /// </summary>
    public static void DrawCompassDegrees(Graphics graphics, Rectangle bounds, GraphCamera3D camera)
    {
        Graph3DCompass.DrawDegreeReadout(graphics, bounds, camera);
    }

    /// <summary>
    /// Builds the logical 3D grid and axis lines for the supplied view.
    /// </summary>
    public static GraphGridScene3D CreateGrid(GraphPlotView3D view, int targetTicksPerAxis = 10, double? requestedStep = null)
    {
        return Graph3DGrid.Create(view, targetTicksPerAxis, requestedStep);
    }

    /// <summary>
    /// Projects the scene grid lines to screen coordinates for the current camera.
    /// </summary>
    public static IReadOnlyList<GraphProjectedLine3D> ProjectGrid(
        GraphGridScene3D scene,
        Rectangle plot,
        GraphPlotView3D view,
        GraphCamera3D camera)
    {
        return Graph3DGrid.ProjectGrid(scene, plot, view, camera);
    }

    /// <summary>
    /// Projects the main X, Y, and Z axis lines to screen coordinates.
    /// </summary>
    public static IReadOnlyList<GraphProjectedLine3D> ProjectAxes(
        GraphGridScene3D scene,
        Rectangle plot,
        GraphPlotView3D view,
        GraphCamera3D camera)
    {
        return Graph3DGrid.ProjectAxes(scene, plot, view, camera);
    }

    /// <summary>
    /// Converts a 2D point to a 3D point by placing it on the supplied Z plane.
    /// </summary>
    public static GraphPoint3D From2D(PointF point, double z = 0d)
    {
        return GraphGeometry3D.From2D(point, z);
    }

    /// <summary>
    /// Converts a 2D point list to Graph3D points on a shared Z plane.
    /// </summary>
    public static IReadOnlyList<GraphPoint3D> From2D(IReadOnlyList<PointF> points, double z = 0d)
    {
        return GraphGeometry3D.From2D(points, z);
    }

    /// <summary>
    /// Extends a 2D graph view with a Z range so the same data can be drawn in Graph3D.
    /// </summary>
    public static GraphPlotView3D From2D(GraphPlotView view, double minZ = -1d, double maxZ = 1d)
    {
        return GraphGeometry3D.From2D(view, minZ, maxZ);
    }

    /// <summary>
    /// Creates a fitted Graph3D view from 2D points while preserving a controlled Z range.
    /// </summary>
    public static GraphPlotView3D CreateFitViewFrom2D(
        IReadOnlyList<PointF> points,
        GraphPlotView? fallbackView = null,
        double z = 0d,
        double zHalfRange = 1d)
    {
        return GraphGeometry3D.CreateFitViewFrom2D(points, fallbackView, z, zHalfRange);
    }

    /// <summary>
    /// Returns whether all ranges in the 3D view are finite and usable.
    /// </summary>
    public static bool IsValidView(GraphPlotView3D view)
    {
        return GraphGeometry3D.IsValidView(view);
    }

    /// <summary>
    /// Creates a Graph3D view that contains the supplied 3D points.
    /// </summary>
    public static GraphPlotView3D CreateFitView(IReadOnlyList<GraphPoint3D> points, double fallbackHalfRange = 5d)
    {
        return GraphGeometry3D.CreateFitView(points, fallbackHalfRange);
    }

    /// <summary>
    /// Repairs empty or invalid X, Y, and Z ranges and optionally pads flat axes.
    /// </summary>
    public static void NormalizeRange(
        ref float minX,
        ref float maxX,
        ref float minY,
        ref float maxY,
        ref float minZ,
        ref float maxZ,
        bool padX = true,
        bool padY = true,
        bool padZ = true)
    {
        GraphGeometry3D.NormalizeRange(ref minX, ref maxX, ref minY, ref maxY, ref minZ, ref maxZ, padX, padY, padZ);
    }

    /// <summary>
    /// Projects one graph-space 3D point to a screen point and depth value.
    /// </summary>
    public static GraphProjectedPoint ProjectToScreen(GraphPoint3D point, Rectangle plot, GraphPlotView3D view, GraphCamera3D camera)
    {
        return GraphGeometry3D.ProjectToScreen(point, plot, view, camera);
    }

    /// <summary>
    /// Projects a list of graph-space 3D points to screen points for drawing.
    /// </summary>
    public static IReadOnlyList<GraphProjectedPoint> ProjectToScreen(
        IReadOnlyList<GraphPoint3D> points,
        Rectangle plot,
        GraphPlotView3D view,
        GraphCamera3D camera)
    {
        return GraphGeometry3D.ProjectToScreen(points, plot, view, camera);
    }

    /// <summary>
    /// Calculates the center point of a 3D graph view.
    /// </summary>
    public static GraphPoint3D Center(GraphPlotView3D view)
    {
        return GraphGeometry3D.Center(view);
    }

    /// <summary>
    /// Draws the complete Graph3D surface: grid, axes, boundary markers, line, and red points.
    /// </summary>
    public static void Draw(
        Graphics graphics,
        Control canvas,
        IReadOnlyList<PointF> linePoints,
        IReadOnlyList<PointF> highlightPoints,
        GraphPlotView3D view,
        GraphCamera3D camera,
        bool showGrid,
        bool showBoundaryFields,
        double? gridStep,
        string disabledMessage,
        string emptyMessage,
        GraphPlotDensity density)
    {
        Graph3DRenderer.Draw(
            graphics,
            canvas,
            linePoints,
            highlightPoints,
            view,
            camera,
            showGrid,
            showBoundaryFields,
            gridStep,
            disabledMessage,
            emptyMessage,
            density);
    }
}
