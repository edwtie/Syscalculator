#nullable enable

// Copyright (c) 1995-2026 Edward Tie / Tiedragon. All rights reserved.
// Builds the logical Graph3D grid and projects it for rendering.
namespace Tiedragon.Graph.G3D;

/// <summary>
/// Identifies one of the three graph axes.
/// </summary>
public enum GraphAxis3D
{
    X,
    Y,
    Z
}

/// <summary>
/// Identifies the plane that owns a 3D grid line.
/// </summary>
public enum GraphGridPlane3D
{
    XY,
    XZ,
    YZ
}

/// <summary>
/// Logical grid line in graph coordinates before projection to the screen.
/// </summary>
public readonly record struct GraphGridLine3D(
    GraphPoint3D Start,
    GraphPoint3D End,
    GraphGridPlane3D Plane,
    GraphAxis3D Direction,
    double Value,
    bool IsAxis,
    bool IsMajor);

/// <summary>
/// Logical main axis line in graph coordinates.
/// </summary>
public readonly record struct GraphAxisLine3D(
    GraphPoint3D Start,
    GraphPoint3D End,
    GraphAxis3D Axis,
    string Label);

/// <summary>
/// Grid or axis line projected to screen coordinates, including depth for draw ordering.
/// </summary>
public readonly record struct GraphProjectedLine3D(
    PointF Start,
    PointF End,
    double Depth,
    GraphGridPlane3D Plane,
    GraphAxis3D Direction,
    bool IsAxis,
    bool IsMajor);

/// <summary>
/// Complete logical Graph3D grid scene: regular grid lines plus main axes.
/// </summary>
public readonly record struct GraphGridScene3D(
    IReadOnlyList<GraphGridLine3D> GridLines,
    IReadOnlyList<GraphAxisLine3D> Axes);

/// <summary>
/// Builds and projects the reusable Graph3D grid.
/// </summary>
public static class Graph3DGrid
{
    private const int MaxTicksPerAxis = 320;

    /// <summary>
    /// Creates a 3D grid scene for the supplied view and optional fixed grid step.
    /// </summary>
    public static GraphGridScene3D Create(GraphPlotView3D view, int targetTicksPerAxis = 10, double? requestedStep = null)
    {
        if (!GraphGeometry3D.IsValidView(view))
            return new GraphGridScene3D([], []);

        var step = NormalizeRequestedStep(requestedStep) ?? ChooseUniformStep(view, targetTicksPerAxis);
        var xTicks = BuildTicks(view.MinX, view.MaxX, step);
        var yTicks = BuildTicks(view.MinY, view.MaxY, step);
        var zTicks = BuildTicks(view.MinZ, view.MaxZ, step);
        var xMinorTicks = BuildMinorTicks(view.MinX, view.MaxX, step);
        var yMinorTicks = BuildMinorTicks(view.MinY, view.MaxY, step);
        var zMinorTicks = BuildMinorTicks(view.MinZ, view.MaxZ, step);

        var lines = new List<GraphGridLine3D>();
        foreach (var z in PlanePositions(view.MinZ, view.MaxZ))
            AddXYPlane(lines, view, xMinorTicks, yMinorTicks, z, isMajor: false);
        foreach (var y in PlanePositions(view.MinY, view.MaxY))
            AddXZPlane(lines, view, xMinorTicks, zMinorTicks, y, isMajor: false);
        foreach (var x in PlanePositions(view.MinX, view.MaxX))
            AddYZPlane(lines, view, yMinorTicks, zMinorTicks, x, isMajor: false);

        foreach (var z in PlanePositions(view.MinZ, view.MaxZ))
            AddXYPlane(lines, view, xTicks, yTicks, z, isMajor: true);
        foreach (var y in PlanePositions(view.MinY, view.MaxY))
            AddXZPlane(lines, view, xTicks, zTicks, y, isMajor: true);
        foreach (var x in PlanePositions(view.MinX, view.MaxX))
            AddYZPlane(lines, view, yTicks, zTicks, x, isMajor: true);

        var axes = new[]
        {
            new GraphAxisLine3D(new GraphPoint3D(view.MinX, 0, 0), new GraphPoint3D(view.MaxX, 0, 0), GraphAxis3D.X, "X"),
            new GraphAxisLine3D(new GraphPoint3D(0, view.MinY, 0), new GraphPoint3D(0, view.MaxY, 0), GraphAxis3D.Y, "Y"),
            new GraphAxisLine3D(new GraphPoint3D(0, 0, view.MinZ), new GraphPoint3D(0, 0, view.MaxZ), GraphAxis3D.Z, "Z")
        };

        return new GraphGridScene3D(lines, axes);
    }

    /// <summary>
    /// Projects all regular grid lines to screen coordinates and sorts them back to front.
    /// </summary>
    public static IReadOnlyList<GraphProjectedLine3D> ProjectGrid(
        GraphGridScene3D scene,
        Rectangle plot,
        GraphPlotView3D view,
        GraphCamera3D camera)
    {
        return scene.GridLines
            .Select(line =>
            {
                var start = GraphGeometry3D.ProjectToScreen(line.Start, plot, view, camera);
                var end = GraphGeometry3D.ProjectToScreen(line.End, plot, view, camera);
                return new GraphProjectedLine3D(
                    start.Screen,
                    end.Screen,
                    (start.Depth + end.Depth) / 2d,
                    line.Plane,
                    line.Direction,
                    line.IsAxis,
                    line.IsMajor);
            })
            .Where(line => IsFinite(line.Start) && IsFinite(line.End) && double.IsFinite(line.Depth))
            .OrderBy(line => line.Depth)
            .ToArray();
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
        return scene.Axes
            .Select(axis =>
            {
                var start = GraphGeometry3D.ProjectToScreen(axis.Start, plot, view, camera);
                var end = GraphGeometry3D.ProjectToScreen(axis.End, plot, view, camera);
                return new GraphProjectedLine3D(
                    start.Screen,
                    end.Screen,
                    (start.Depth + end.Depth) / 2d,
                    AxisPlane(axis.Axis),
                    axis.Axis,
                    IsAxis: true,
                    IsMajor: true);
            })
            .Where(line => IsFinite(line.Start) && IsFinite(line.End) && double.IsFinite(line.Depth))
            .OrderBy(line => line.Depth)
            .ToArray();
    }

    private static void AddXYPlane(List<GraphGridLine3D> lines, GraphPlotView3D view, IReadOnlyList<double> xTicks, IReadOnlyList<double> yTicks, double z, bool isMajor)
    {
        foreach (var x in xTicks)
        {
            lines.Add(new GraphGridLine3D(
                new GraphPoint3D(x, view.MinY, z),
                new GraphPoint3D(x, view.MaxY, z),
                GraphGridPlane3D.XY,
                GraphAxis3D.Y,
                x,
                IsZero(x),
                isMajor));
        }

        foreach (var y in yTicks)
        {
            lines.Add(new GraphGridLine3D(
                new GraphPoint3D(view.MinX, y, z),
                new GraphPoint3D(view.MaxX, y, z),
                GraphGridPlane3D.XY,
                GraphAxis3D.X,
                y,
                IsZero(y),
                isMajor));
        }
    }

    private static void AddXZPlane(List<GraphGridLine3D> lines, GraphPlotView3D view, IReadOnlyList<double> xTicks, IReadOnlyList<double> zTicks, double y, bool isMajor)
    {
        foreach (var x in xTicks)
        {
            lines.Add(new GraphGridLine3D(
                new GraphPoint3D(x, y, view.MinZ),
                new GraphPoint3D(x, y, view.MaxZ),
                GraphGridPlane3D.XZ,
                GraphAxis3D.Z,
                x,
                IsZero(x),
                isMajor));
        }

        foreach (var z in zTicks)
        {
            lines.Add(new GraphGridLine3D(
                new GraphPoint3D(view.MinX, y, z),
                new GraphPoint3D(view.MaxX, y, z),
                GraphGridPlane3D.XZ,
                GraphAxis3D.X,
                z,
                IsZero(z),
                isMajor));
        }
    }

    private static void AddYZPlane(List<GraphGridLine3D> lines, GraphPlotView3D view, IReadOnlyList<double> yTicks, IReadOnlyList<double> zTicks, double x, bool isMajor)
    {
        foreach (var y in yTicks)
        {
            lines.Add(new GraphGridLine3D(
                new GraphPoint3D(x, y, view.MinZ),
                new GraphPoint3D(x, y, view.MaxZ),
                GraphGridPlane3D.YZ,
                GraphAxis3D.Z,
                y,
                IsZero(y),
                isMajor));
        }

        foreach (var z in zTicks)
        {
            lines.Add(new GraphGridLine3D(
                new GraphPoint3D(x, view.MinY, z),
                new GraphPoint3D(x, view.MaxY, z),
                GraphGridPlane3D.YZ,
                GraphAxis3D.Y,
                z,
                IsZero(z),
                isMajor));
        }
    }

    private static IReadOnlyList<double> PlanePositions(double min, double max)
    {
        var positions = new List<double> { min, max };
        if (min < 0d && max > 0d)
            positions.Add(0d);

        return positions
            .Order()
            .DistinctBy(value => Math.Round(value, 10))
            .ToArray();
    }

    private static double ChooseStep(double min, double max, int targetTicks)
    {
        var range = Math.Abs(max - min);
        if (!double.IsFinite(range) || range <= 0d)
            return 1d;

        return NiceNumber(range / Math.Clamp(targetTicks, 2, MaxTicksPerAxis), round: false);
    }

    private static double ChooseUniformStep(GraphPlotView3D view, int targetTicks)
    {
        var span = Math.Max(view.MaxX - view.MinX, Math.Max(view.MaxY - view.MinY, view.MaxZ - view.MinZ));
        if (!double.IsFinite(span) || span <= 0d)
            return 1d;

        return NiceNumber(span / Math.Clamp(targetTicks, 2, MaxTicksPerAxis), round: false);
    }

    private static double? NormalizeRequestedStep(double? requestedStep)
    {
        if (!requestedStep.HasValue || !double.IsFinite(requestedStep.Value) || requestedStep.Value <= 0d)
            return null;

        return requestedStep.Value;
    }

    private static IReadOnlyList<double> BuildTicks(double min, double max, double step)
    {
        if (!double.IsFinite(min) || !double.IsFinite(max) || !double.IsFinite(step) || step <= 0d || max <= min)
            return [];

        var ticks = new List<double>();
        var first = Math.Ceiling(min / step) * step;
        for (var i = 0; i < MaxTicksPerAxis; i++)
        {
            var value = first + step * i;
            if (!double.IsFinite(value) || value > max + step * 0.5d)
                break;

            if (value >= min - step * 0.25d && value <= max + step * 0.25d)
                ticks.Add(Math.Abs(value) < step / 1000d ? 0d : value);
        }

        return ticks;
    }

    private static IReadOnlyList<double> BuildMinorTicks(double min, double max, double majorStep)
    {
        var minorStep = majorStep / 5d;
        if (!double.IsFinite(minorStep) || minorStep <= 0d)
            return [];

        return BuildTicks(min, max, minorStep)
            .Where(value => !IsMultipleOfStep(value, majorStep))
            .ToArray();
    }

    private static bool IsMultipleOfStep(double value, double step)
    {
        if (!double.IsFinite(value) || !double.IsFinite(step) || step <= 0d)
            return false;

        var nearest = Math.Round(value / step) * step;
        return Math.Abs(value - nearest) <= step * 1e-7d;
    }

    private static double NiceNumber(double value, bool round)
    {
        if (value <= 0d)
            return 1d;

        var exponent = Math.Floor(Math.Log10(value));
        var fraction = value / Math.Pow(10d, exponent);
        double niceFraction = round
            ? fraction < 1.5d ? 1d : fraction < 3d ? 2d : fraction < 7d ? 5d : 10d
            : fraction <= 1d ? 1d : fraction <= 2d ? 2d : fraction <= 5d ? 5d : 10d;

        return niceFraction * Math.Pow(10d, exponent);
    }

    private static GraphGridPlane3D AxisPlane(GraphAxis3D axis)
    {
        return axis switch
        {
            GraphAxis3D.X => GraphGridPlane3D.XY,
            GraphAxis3D.Y => GraphGridPlane3D.XY,
            GraphAxis3D.Z => GraphGridPlane3D.XZ,
            _ => GraphGridPlane3D.XY
        };
    }

    private static bool IsZero(double value) => Math.Abs(value) < 1e-12d;

    private static bool IsFinite(PointF point) => float.IsFinite(point.X) && float.IsFinite(point.Y);
}
