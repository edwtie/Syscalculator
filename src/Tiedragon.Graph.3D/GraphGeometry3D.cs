#nullable enable

// Copyright (c) 1995-2026 Edward Tie / Tiedragon. All rights reserved.
// Defines Graph3D geometry types and projection math shared by renderer and UI.
namespace Tiedragon.Graph.G3D;

/// <summary>
/// A point in graph 3D coordinates.
/// </summary>
public readonly record struct GraphPoint3D(double X, double Y, double Z);

/// <summary>
/// A projected 3D point in canvas coordinates, including depth for painter-order sorting.
/// </summary>
public readonly record struct GraphProjectedPoint(PointF Screen, double Depth);

/// <summary>
/// Visible graph-coordinate range for X, Y, and Z.
/// </summary>
public readonly record struct GraphPlotView3D(
    double MinX,
    double MaxX,
    double MinY,
    double MaxY,
    double MinZ,
    double MaxZ);

/// <summary>
/// Camera angles and scale used by the basic Graph3D projection.
/// </summary>
/// <remarks>
/// Yaw and pitch are degrees. Zoom is a screen-space multiplier where 1 is the default fit.
/// </remarks>
public readonly record struct GraphCamera3D(double YawDegrees = 35d, double PitchDegrees = 28d, double Zoom = 1d);

/// <summary>
/// Render-independent 3D graph geometry and projection API.
/// </summary>
public static class GraphGeometry3D
{
    private const double DegreesToRadians = Math.PI / 180d;

    /// <summary>
    /// Places a 2D graph point on a Z plane.
    /// </summary>
    public static GraphPoint3D From2D(PointF point, double z = 0d)
    {
        return new GraphPoint3D(point.X, point.Y, z);
    }

    /// <summary>
    /// Places a list of 2D graph points on the same Z plane.
    /// </summary>
    public static IReadOnlyList<GraphPoint3D> From2D(IReadOnlyList<PointF> points, double z = 0d)
    {
        return points.Select(point => From2D(point, z)).ToArray();
    }

    /// <summary>
    /// Extends a 2D view with a usable Z range.
    /// </summary>
    public static GraphPlotView3D From2D(GraphPlotView view, double minZ = -1d, double maxZ = 1d)
    {
        if (minZ > maxZ)
            (minZ, maxZ) = (maxZ, minZ);

        if (Math.Abs(maxZ - minZ) < GraphGeometry2D.MinimumViewSpan)
        {
            var centerZ = (minZ + maxZ) / 2d;
            minZ = centerZ - 1d;
            maxZ = centerZ + 1d;
        }

        return new GraphPlotView3D(view.MinX, view.MaxX, view.MinY, view.MaxY, minZ, maxZ);
    }

    /// <summary>
    /// Fits a 3D view around 2D points while keeping the requested Z plane visible.
    /// </summary>
    public static GraphPlotView3D CreateFitViewFrom2D(
        IReadOnlyList<PointF> points,
        GraphPlotView? fallbackView = null,
        double z = 0d,
        double zHalfRange = 1d)
    {
        if (points.Count == 0 && fallbackView.HasValue)
            return From2D(fallbackView.Value, z - zHalfRange, z + zHalfRange);

        var points3D = From2D(points, z);
        var view = CreateFitView(points3D);
        return view with
        {
            MinZ = z - Math.Max(GraphGeometry2D.MinimumViewSpan, zHalfRange),
            MaxZ = z + Math.Max(GraphGeometry2D.MinimumViewSpan, zHalfRange)
        };
    }

    /// <summary>
    /// Checks whether a 3D view has finite, increasing X, Y, and Z ranges.
    /// </summary>
    public static bool IsValidView(GraphPlotView3D view)
    {
        return double.IsFinite(view.MinX) &&
               double.IsFinite(view.MaxX) &&
               double.IsFinite(view.MinY) &&
               double.IsFinite(view.MaxY) &&
               double.IsFinite(view.MinZ) &&
               double.IsFinite(view.MaxZ) &&
               view.MaxX > view.MinX &&
               view.MaxY > view.MinY &&
               view.MaxZ > view.MinZ;
    }

    /// <summary>
    /// Normalizes mutable X, Y, and Z ranges so flat or reversed ranges can still render.
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
        NormalizeAxis(ref minX, ref maxX, padX ? 0.04f : 0f);
        NormalizeAxis(ref minY, ref maxY, padY ? 0.08f : 0f);
        NormalizeAxis(ref minZ, ref maxZ, padZ ? 0.08f : 0f);
    }

    /// <summary>
    /// Creates a view that contains all finite 3D points, with fallback bounds for empty input.
    /// </summary>
    public static GraphPlotView3D CreateFitView(
        IReadOnlyList<GraphPoint3D> points,
        double fallbackHalfRange = 5d)
    {
        var finitePoints = points
            .Where(point => double.IsFinite(point.X) && double.IsFinite(point.Y) && double.IsFinite(point.Z))
            .ToArray();

        if (finitePoints.Length == 0)
        {
            return new GraphPlotView3D(
                -fallbackHalfRange,
                fallbackHalfRange,
                -fallbackHalfRange,
                fallbackHalfRange,
                -fallbackHalfRange,
                fallbackHalfRange);
        }

        var minX = finitePoints.Min(point => point.X);
        var maxX = finitePoints.Max(point => point.X);
        var minY = finitePoints.Min(point => point.Y);
        var maxY = finitePoints.Max(point => point.Y);
        var minZ = finitePoints.Min(point => point.Z);
        var maxZ = finitePoints.Max(point => point.Z);

        ExpandFlatRange(ref minX, ref maxX);
        ExpandFlatRange(ref minY, ref maxY);
        ExpandFlatRange(ref minZ, ref maxZ);

        PadAxis(ref minX, ref maxX, 0.04d);
        PadAxis(ref minY, ref maxY, 0.08d);
        PadAxis(ref minZ, ref maxZ, 0.08d);

        return new GraphPlotView3D(minX, maxX, minY, maxY, minZ, maxZ);
    }

    /// <summary>
    /// Projects one graph-space point into canvas coordinates using the supplied camera.
    /// </summary>
    public static GraphProjectedPoint ProjectToScreen(GraphPoint3D point, Rectangle plot, GraphPlotView3D view, GraphCamera3D camera)
    {
        if (!IsValidView(view) || plot.Width <= 0 || plot.Height <= 0)
            return new GraphProjectedPoint(new PointF(float.NaN, float.NaN), double.NaN);

        var centered = NormalizeToCenteredCube(point, view);
        var rotated = Rotate(centered, camera);
        var zoom = double.IsFinite(camera.Zoom) && camera.Zoom > 0d ? camera.Zoom : 1d;
        var scale = Math.Min(plot.Width, plot.Height) * 0.50d * zoom;
        var screenX = plot.Left + plot.Width / 2d + rotated.X * scale;
        var screenY = plot.Top + plot.Height / 2d - rotated.Y * scale;

        return new GraphProjectedPoint(new PointF((float)screenX, (float)screenY), rotated.Z);
    }

    /// <summary>
    /// Projects a point list into canvas coordinates using the supplied camera.
    /// </summary>
    public static IReadOnlyList<GraphProjectedPoint> ProjectToScreen(
        IReadOnlyList<GraphPoint3D> points,
        Rectangle plot,
        GraphPlotView3D view,
        GraphCamera3D camera)
    {
        return points.Select(point => ProjectToScreen(point, plot, view, camera)).ToArray();
    }

    /// <summary>
    /// Returns the center point of a 3D graph view.
    /// </summary>
    public static GraphPoint3D Center(GraphPlotView3D view)
    {
        return new GraphPoint3D(
            (view.MinX + view.MaxX) / 2d,
            (view.MinY + view.MaxY) / 2d,
            (view.MinZ + view.MaxZ) / 2d);
    }

    private static GraphPoint3D NormalizeToCenteredCube(GraphPoint3D point, GraphPlotView3D view)
    {
        var centerX = (view.MinX + view.MaxX) / 2d;
        var centerY = (view.MinY + view.MaxY) / 2d;
        var centerZ = (view.MinZ + view.MaxZ) / 2d;
        var span = Math.Max(view.MaxX - view.MinX, Math.Max(view.MaxY - view.MinY, view.MaxZ - view.MinZ));
        span = Math.Max(GraphGeometry2D.MinimumViewSpan, span);

        var x = (point.X - centerX) / span * 2d;
        var y = (point.Y - centerY) / span * 2d;
        var z = (point.Z - centerZ) / span * 2d;
        return new GraphPoint3D(x, y, z);
    }

    private static GraphPoint3D Rotate(GraphPoint3D point, GraphCamera3D camera)
    {
        var yaw = camera.YawDegrees * DegreesToRadians;
        var pitch = camera.PitchDegrees * DegreesToRadians;
        var cosYaw = Math.Cos(yaw);
        var sinYaw = Math.Sin(yaw);
        var cosPitch = Math.Cos(pitch);
        var sinPitch = Math.Sin(pitch);

        var x = point.X * cosYaw - point.Z * sinYaw;
        var z = point.X * sinYaw + point.Z * cosYaw;
        var y = point.Y * cosPitch - z * sinPitch;
        var depth = point.Y * sinPitch + z * cosPitch;

        return new GraphPoint3D(x, y, depth);
    }

    private static void NormalizeAxis(ref float min, ref float max, float padFraction)
    {
        if (min > max)
            (min, max) = (max, min);

        if (Math.Abs(max - min) < 0.0001f)
        {
            var pad = Math.Max(1f, Math.Abs(min) * 0.1f);
            min -= pad;
            max += pad;
        }

        if (padFraction <= 0f)
            return;

        var axisPad = Math.Max(0.5f, (max - min) * padFraction);
        min -= axisPad;
        max += axisPad;
    }

    private static void ExpandFlatRange(ref double min, ref double max)
    {
        if (min > max)
            (min, max) = (max, min);

        if (Math.Abs(max - min) >= 0.0001d)
            return;

        var pad = Math.Max(1d, Math.Abs(min) * 0.1d);
        min -= pad;
        max += pad;
    }

    private static void PadAxis(ref double min, ref double max, double fraction)
    {
        var pad = Math.Max(0.25d, (max - min) * fraction);
        min -= pad;
        max += pad;
    }
}
