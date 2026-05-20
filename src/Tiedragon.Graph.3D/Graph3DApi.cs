#nullable enable

using Tiedragon.Graph;

namespace Tiedragon.Graph.G3D;

/// <summary>
/// Public facade for Graph3D geometry, Z-range handling, and 3D-to-2D projection.
/// </summary>
public static class Graph3DApi
{
    public static GraphCamera3D DefaultCamera => GraphCameraNavigator3D.Default;

    public static GraphCamera3D CameraPreset(GraphCameraPreset3D preset)
    {
        return GraphCameraNavigator3D.Preset(preset);
    }

    public static GraphCamera3D RotateCamera(GraphCamera3D camera, double deltaYawDegrees, double deltaPitchDegrees)
    {
        return GraphCameraNavigator3D.Rotate(camera, deltaYawDegrees, deltaPitchDegrees);
    }

    public static GraphCamera3D ZoomCamera(GraphCamera3D camera, double factor)
    {
        return GraphCameraNavigator3D.Zoom(camera, factor);
    }

    public static GraphCamera3D ResetCamera(GraphCameraPreset3D preset = GraphCameraPreset3D.Isometric)
    {
        return GraphCameraNavigator3D.Reset(preset);
    }

    public static void DrawCompass(
        Graphics graphics,
        Rectangle bounds,
        GraphCamera3D camera,
        bool hover = false,
        bool pressed = false)
    {
        Graph3DCompass.DrawCompass(graphics, bounds, camera, hover, pressed);
    }

    public static void DrawCompassDegrees(Graphics graphics, Rectangle bounds, GraphCamera3D camera)
    {
        Graph3DCompass.DrawDegreeReadout(graphics, bounds, camera);
    }

    public static GraphGridScene3D CreateGrid(GraphPlotView3D view, int targetTicksPerAxis = 10, double? requestedStep = null)
    {
        return Graph3DGrid.Create(view, targetTicksPerAxis, requestedStep);
    }

    public static IReadOnlyList<GraphProjectedLine3D> ProjectGrid(
        GraphGridScene3D scene,
        Rectangle plot,
        GraphPlotView3D view,
        GraphCamera3D camera)
    {
        return Graph3DGrid.ProjectGrid(scene, plot, view, camera);
    }

    public static IReadOnlyList<GraphProjectedLine3D> ProjectAxes(
        GraphGridScene3D scene,
        Rectangle plot,
        GraphPlotView3D view,
        GraphCamera3D camera)
    {
        return Graph3DGrid.ProjectAxes(scene, plot, view, camera);
    }

    public static GraphPoint3D From2D(PointF point, double z = 0d)
    {
        return GraphGeometry3D.From2D(point, z);
    }

    public static IReadOnlyList<GraphPoint3D> From2D(IReadOnlyList<PointF> points, double z = 0d)
    {
        return GraphGeometry3D.From2D(points, z);
    }

    public static GraphPlotView3D From2D(GraphPlotView view, double minZ = -1d, double maxZ = 1d)
    {
        return GraphGeometry3D.From2D(view, minZ, maxZ);
    }

    public static GraphPlotView3D CreateFitViewFrom2D(
        IReadOnlyList<PointF> points,
        GraphPlotView? fallbackView = null,
        double z = 0d,
        double zHalfRange = 1d)
    {
        return GraphGeometry3D.CreateFitViewFrom2D(points, fallbackView, z, zHalfRange);
    }

    public static bool IsValidView(GraphPlotView3D view)
    {
        return GraphGeometry3D.IsValidView(view);
    }

    public static GraphPlotView3D CreateFitView(IReadOnlyList<GraphPoint3D> points, double fallbackHalfRange = 5d)
    {
        return GraphGeometry3D.CreateFitView(points, fallbackHalfRange);
    }

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

    public static GraphProjectedPoint ProjectToScreen(GraphPoint3D point, Rectangle plot, GraphPlotView3D view, GraphCamera3D camera)
    {
        return GraphGeometry3D.ProjectToScreen(point, plot, view, camera);
    }

    public static IReadOnlyList<GraphProjectedPoint> ProjectToScreen(
        IReadOnlyList<GraphPoint3D> points,
        Rectangle plot,
        GraphPlotView3D view,
        GraphCamera3D camera)
    {
        return GraphGeometry3D.ProjectToScreen(points, plot, view, camera);
    }

    public static GraphPoint3D Center(GraphPlotView3D view)
    {
        return GraphGeometry3D.Center(view);
    }

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
