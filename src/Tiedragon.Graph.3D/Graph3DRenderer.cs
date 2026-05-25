#nullable enable

// Copyright (c) 1995-2026 Edward Tie / Tiedragon. All rights reserved.
// Renders Graph3D grids, axes, boundary fields, line data, and highlight points.
using System.Drawing.Drawing2D;

namespace Tiedragon.Graph.G3D;

/// <summary>
/// Shared renderer for Graph3D surfaces.
/// </summary>
public static class Graph3DRenderer
{
    private static Color CanvasBackColor => GraphOverlayStyle.UseDarkTheme ? Color.FromArgb(10, 18, 32) : Color.White;
    private static Color MessageTextColor => GraphOverlayStyle.UseDarkTheme ? Color.FromArgb(203, 213, 225) : Color.DimGray;
    private static Color AxisColor => GraphOverlayStyle.UseDarkTheme ? Color.FromArgb(203, 213, 225) : Color.FromArgb(48, 56, 68);
    private static Color QuietAxisColor => GraphOverlayStyle.UseDarkTheme ? Color.FromArgb(148, 163, 184) : Color.FromArgb(70, 82, 98);
    private static Color AxisGuideColor => GraphOverlayStyle.UseDarkTheme ? Color.FromArgb(30, 41, 59) : Color.FromArgb(220, 226, 235);
    private static Color AxisLabelColor => GraphOverlayStyle.UseDarkTheme ? Color.FromArgb(226, 232, 240) : Color.FromArgb(51, 65, 85);
    private static Color NumberLabelColor => GraphOverlayStyle.UseDarkTheme ? Color.FromArgb(203, 213, 225) : Color.FromArgb(71, 85, 105);
    private static Color NeutralMajorGridColor => GraphOverlayStyle.UseDarkTheme ? Color.FromArgb(51, 65, 85) : Color.FromArgb(215, 222, 232);
    private static Color NeutralMinorGridColor => GraphOverlayStyle.UseDarkTheme ? Color.FromArgb(30, 41, 59) : Color.FromArgb(240, 244, 248);
    private static Color XyMajorGridColor => GraphOverlayStyle.UseDarkTheme ? Color.FromArgb(49, 70, 104) : Color.FromArgb(198, 213, 234);
    private static Color XzMajorGridColor => GraphOverlayStyle.UseDarkTheme ? Color.FromArgb(68, 55, 100) : Color.FromArgb(220, 205, 238);
    private static Color YzMajorGridColor => GraphOverlayStyle.UseDarkTheme ? Color.FromArgb(43, 86, 70) : Color.FromArgb(198, 226, 211);
    private static Color XyMinorGridColor => GraphOverlayStyle.UseDarkTheme ? Color.FromArgb(24, 36, 58) : Color.FromArgb(233, 239, 248);
    private static Color XzMinorGridColor => GraphOverlayStyle.UseDarkTheme ? Color.FromArgb(36, 31, 56) : Color.FromArgb(240, 232, 248);
    private static Color YzMinorGridColor => GraphOverlayStyle.UseDarkTheme ? Color.FromArgb(22, 48, 43) : Color.FromArgb(231, 245, 237);
    private static Color PointBorderColor => GraphOverlayStyle.UseDarkTheme ? Color.FromArgb(15, 23, 42) : Color.White;

    public static void Draw(
        Graphics graphics,
        Control canvas,
        IReadOnlyList<PointF> linePoints,
        IReadOnlyList<PointF> highlightPoints,
        GraphPlotView3D view,
        GraphCamera3D camera,
        bool showGrid,
        bool showBoundaryFields,
        bool showLines,
        double? gridStep,
        string disabledMessage,
        string emptyMessage,
        GraphPlotDensity density)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(CanvasBackColor);

        var plot = GraphGeometry2D.GetPlotRectangle(canvas.ClientRectangle);
        if (plot.Width <= 0 || plot.Height <= 0)
            return;

        var gridView = CreateSpaceGridView(view);
        var gridScene = Graph3DApi.CreateGrid(gridView, targetTicksPerAxis: density == GraphPlotDensity.Compact ? 18 : 24, gridStep);
        var labelScene = Graph3DApi.CreateGrid(gridView, targetTicksPerAxis: density == GraphPlotDensity.Compact ? 12 : 16, gridStep);
        if (showGrid)
        {
            DrawGrid(graphics, gridScene, plot, view, camera);
            DrawNumberLabels(graphics, labelScene, plot, view, camera);
        }
        else
        {
            DrawAxes(graphics, labelScene, plot, view, camera);
        }

        if (showBoundaryFields)
            DrawMinimumBoundaryFields(graphics, plot, view, camera, showGrid);
        DrawAxisLabels(graphics, labelScene, plot, view, camera);
        DrawData(graphics, linePoints, highlightPoints, plot, view, camera, showLines);

        if (!string.IsNullOrWhiteSpace(disabledMessage))
        {
            using var brush = new SolidBrush(MessageTextColor);
            graphics.DrawString(disabledMessage, canvas.Font, brush, plot.Left + 12, plot.Top + 12);
        }
        else if (linePoints.Count == 0)
        {
            using var brush = new SolidBrush(MessageTextColor);
            graphics.DrawString(emptyMessage, canvas.Font, brush, plot.Left + 12, plot.Top + 12);
        }
    }

    private static GraphPlotView3D CreateSpaceGridView(GraphPlotView3D view)
    {
        const double factor = 3.4d;

        static (double Min, double Max) Expand(double min, double max)
        {
            var center = (min + max) / 2d;
            var half = Math.Max(GraphGeometry2D.MinimumViewSpan, (max - min) / 2d) * factor;
            return (center - half, center + half);
        }

        var x = Expand(view.MinX, view.MaxX);
        var y = Expand(view.MinY, view.MaxY);
        var z = Expand(view.MinZ, view.MaxZ);
        return new GraphPlotView3D(x.Min, x.Max, y.Min, y.Max, z.Min, z.Max);
    }

    private static void DrawGrid(Graphics graphics, GraphGridScene3D gridScene, Rectangle plot, GraphPlotView3D view, GraphCamera3D camera)
    {
        using var xyMajorPen = new Pen(XyMajorGridColor, 0.95f);
        using var xzMajorPen = new Pen(XzMajorGridColor, 0.95f);
        using var yzMajorPen = new Pen(YzMajorGridColor, 0.95f);
        using var xyMinorPen = new Pen(XyMinorGridColor, 0.45f);
        using var xzMinorPen = new Pen(XzMinorGridColor, 0.45f);
        using var yzMinorPen = new Pen(YzMinorGridColor, 0.45f);
        using var axisGuidePen = CreateAxisGuidePen();
        using var axisPen = CreateAxisPen();

        foreach (var line in Graph3DApi.ProjectGrid(gridScene, plot, view, camera))
        {
            if (line.IsAxis)
                continue;

            var pen = line.Plane switch
            {
                GraphGridPlane3D.XZ => line.IsMajor ? xzMajorPen : xzMinorPen,
                GraphGridPlane3D.YZ => line.IsMajor ? yzMajorPen : yzMinorPen,
                _ => line.IsMajor ? xyMajorPen : xyMinorPen
            };
            graphics.DrawLine(pen, line.Start, line.End);
        }

        var axes = Graph3DApi.ProjectAxes(gridScene, plot, view, camera);
        foreach (var line in axes)
            graphics.DrawLine(axisGuidePen, line.Start, line.End);
        foreach (var line in axes)
            graphics.DrawLine(axisPen, line.Start, line.End);
    }

    private static void DrawAxes(Graphics graphics, GraphGridScene3D scene, Rectangle plot, GraphPlotView3D view, GraphCamera3D camera)
    {
        using var axisGuidePen = CreateAxisGuidePen();
        using var axisPen = CreateAxisPen();
        var axes = Graph3DApi.ProjectAxes(scene, plot, view, camera);
        foreach (var line in axes)
            graphics.DrawLine(axisGuidePen, line.Start, line.End);
        foreach (var line in axes)
            graphics.DrawLine(axisPen, line.Start, line.End);
    }

    private static void DrawNeutralGrid(Graphics graphics, GraphGridScene3D gridScene, Rectangle plot, GraphPlotView3D view, GraphCamera3D camera)
    {
        using var majorPen = new Pen(NeutralMajorGridColor, 0.85f);
        using var minorPen = new Pen(NeutralMinorGridColor, 0.4f);
        using var axisGuidePen = CreateAxisGuidePen();
        using var axisPen = CreateQuietAxisPen();

        foreach (var line in Graph3DApi.ProjectGrid(gridScene, plot, view, camera))
        {
            if (line.IsAxis)
                continue;

            graphics.DrawLine(line.IsMajor ? majorPen : minorPen, line.Start, line.End);
        }

        var axes = Graph3DApi.ProjectAxes(gridScene, plot, view, camera);
        foreach (var line in axes)
            graphics.DrawLine(axisGuidePen, line.Start, line.End);
        foreach (var line in axes)
            graphics.DrawLine(axisPen, line.Start, line.End);
    }

    private static Pen CreateAxisPen()
    {
        return new Pen(AxisColor, 1.65f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
    }

    private static Pen CreateQuietAxisPen()
    {
        return new Pen(QuietAxisColor, 1.5f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
    }

    private static Pen CreateAxisGuidePen()
    {
        return new Pen(AxisGuideColor, 3.3f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
    }

    private static void DrawMinimumBoundaryFields(Graphics graphics, Rectangle plot, GraphPlotView3D view, GraphCamera3D camera, bool coloredGrid)
    {
        var fields = new[]
        {
            CreateBoundaryField(
                [
                    new GraphPoint3D(view.MinX, view.MinY, view.MinZ),
                    new GraphPoint3D(view.MinX, view.MaxY, view.MinZ),
                    new GraphPoint3D(view.MinX, view.MaxY, view.MaxZ),
                    new GraphPoint3D(view.MinX, view.MinY, view.MaxZ)
                ],
                "X min",
                isMinimum: true),
            CreateBoundaryField(
                [
                    new GraphPoint3D(view.MaxX, view.MinY, view.MinZ),
                    new GraphPoint3D(view.MaxX, view.MaxY, view.MinZ),
                    new GraphPoint3D(view.MaxX, view.MaxY, view.MaxZ),
                    new GraphPoint3D(view.MaxX, view.MinY, view.MaxZ)
                ],
                "X max",
                isMinimum: false),
            CreateBoundaryField(
                [
                    new GraphPoint3D(view.MinX, view.MinY, view.MinZ),
                    new GraphPoint3D(view.MaxX, view.MinY, view.MinZ),
                    new GraphPoint3D(view.MaxX, view.MinY, view.MaxZ),
                    new GraphPoint3D(view.MinX, view.MinY, view.MaxZ)
                ],
                "Y min",
                isMinimum: true),
            CreateBoundaryField(
                [
                    new GraphPoint3D(view.MinX, view.MaxY, view.MinZ),
                    new GraphPoint3D(view.MaxX, view.MaxY, view.MinZ),
                    new GraphPoint3D(view.MaxX, view.MaxY, view.MaxZ),
                    new GraphPoint3D(view.MinX, view.MaxY, view.MaxZ)
                ],
                "Y max",
                isMinimum: false),
            CreateBoundaryField(
                [
                    new GraphPoint3D(view.MinX, view.MinY, view.MinZ),
                    new GraphPoint3D(view.MaxX, view.MinY, view.MinZ),
                    new GraphPoint3D(view.MaxX, view.MaxY, view.MinZ),
                    new GraphPoint3D(view.MinX, view.MaxY, view.MinZ)
                ],
                "Z min",
                isMinimum: true),
            CreateBoundaryField(
                [
                    new GraphPoint3D(view.MinX, view.MinY, view.MaxZ),
                    new GraphPoint3D(view.MaxX, view.MinY, view.MaxZ),
                    new GraphPoint3D(view.MaxX, view.MaxY, view.MaxZ),
                    new GraphPoint3D(view.MinX, view.MaxY, view.MaxZ)
                ],
                "Z max",
                isMinimum: false)
        };

        using var minFill = new SolidBrush(GraphOverlayStyle.UseDarkTheme
            ? Color.FromArgb(coloredGrid ? 36 : 52, 37, 99, 235)
            : Color.FromArgb(coloredGrid ? 25 : 36, 92, 106, 124));
        using var maxFill = new SolidBrush(GraphOverlayStyle.UseDarkTheme
            ? Color.FromArgb(coloredGrid ? 18 : 28, 148, 163, 184)
            : Color.FromArgb(coloredGrid ? 10 : 16, 148, 163, 184));
        using var minOutline = new Pen(GraphOverlayStyle.UseDarkTheme
            ? Color.FromArgb(coloredGrid ? 180 : 210, 96, 165, 250)
            : Color.FromArgb(coloredGrid ? 165 : 190, 82, 94, 111), coloredGrid ? 1.15f : 1.3f)
        {
            DashStyle = DashStyle.Dash
        };
        using var maxOutline = new Pen(GraphOverlayStyle.UseDarkTheme
            ? Color.FromArgb(coloredGrid ? 120 : 150, 148, 163, 184)
            : Color.FromArgb(coloredGrid ? 100 : 125, 148, 163, 184), 0.9f)
        {
            DashStyle = DashStyle.Dot
        };
        using var minLabelBrush = new SolidBrush(GraphOverlayStyle.UseDarkTheme ? Color.FromArgb(191, 219, 254) : Color.FromArgb(51, 65, 85));
        using var maxLabelBrush = new SolidBrush(GraphOverlayStyle.UseDarkTheme ? Color.FromArgb(203, 213, 225) : Color.FromArgb(100, 116, 139));
        using var labelFont = new Font("Segoe UI", 7f, FontStyle.Regular);

        foreach (var field in fields
            .Select(field => ProjectBoundaryField(field.Points, field.Label, field.IsMinimum, plot, view, camera))
            .Where(field => field.Points.Length >= 3)
            .OrderBy(field => field.Depth))
        {
            graphics.FillPolygon(field.IsMinimum ? minFill : maxFill, field.Points);
            graphics.DrawPolygon(field.IsMinimum ? minOutline : maxOutline, field.Points);

            var labelAnchor = field.Points
                .OrderBy(point => point.X + point.Y)
                .First();
            graphics.DrawString(field.Label, labelFont, field.IsMinimum ? minLabelBrush : maxLabelBrush, labelAnchor.X + 4f, labelAnchor.Y + 4f);
        }
    }

    private static (IReadOnlyList<GraphPoint3D> Points, string Label, bool IsMinimum) CreateBoundaryField(
        IReadOnlyList<GraphPoint3D> points,
        string label,
        bool isMinimum)
    {
        return (points, label, isMinimum);
    }

    private static (PointF[] Points, double Depth, string Label, bool IsMinimum) ProjectBoundaryField(
        IReadOnlyList<GraphPoint3D> points,
        string label,
        bool isMinimum,
        Rectangle plot,
        GraphPlotView3D view,
        GraphCamera3D camera)
    {
        var projected = points
            .Select(point => Graph3DApi.ProjectToScreen(point, plot, view, camera))
            .Where(point => float.IsFinite(point.Screen.X) && float.IsFinite(point.Screen.Y) && double.IsFinite(point.Depth))
            .ToArray();

        return (
            projected.Select(point => point.Screen).ToArray(),
            projected.Length == 0 ? 0d : projected.Average(point => point.Depth),
            label,
            isMinimum);
    }

    private static void DrawData(
        Graphics graphics,
        IReadOnlyList<PointF> linePoints,
        IReadOnlyList<PointF> highlightPoints,
        Rectangle plot,
        GraphPlotView3D view,
        GraphCamera3D camera,
        bool showLines)
    {
        if (showLines && linePoints.Count >= 2)
        {
            var points3D = Graph3DApi.From2D(linePoints, z: 0d);
            var projected = Graph3DApi.ProjectToScreen(points3D, plot, view, camera)
                .Select(point => point.Screen)
                .Where(point => float.IsFinite(point.X) && float.IsFinite(point.Y))
                .ToArray();
            if (projected.Length >= 2)
            {
                using var linePen = new Pen(Color.FromArgb(15, 63, 143), 2.2f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                };
                graphics.DrawLines(linePen, projected);
            }
        }

        using var pointBrush = new SolidBrush(Color.FromArgb(220, 38, 38));
        using var pointBorder = new Pen(PointBorderColor, 1.2f);
        foreach (var point in Graph3DApi.ProjectToScreen(Graph3DApi.From2D(highlightPoints, z: 0d), plot, view, camera).Select(p => p.Screen))
        {
            if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
                continue;

            graphics.FillEllipse(pointBrush, point.X - 3.4f, point.Y - 3.4f, 6.8f, 6.8f);
            graphics.DrawEllipse(pointBorder, point.X - 3.4f, point.Y - 3.4f, 6.8f, 6.8f);
        }
    }

    private static void DrawAxisLabels(Graphics graphics, GraphGridScene3D scene, Rectangle plot, GraphPlotView3D view, GraphCamera3D camera)
    {
        using var font = new Font("Segoe UI Semibold", 8f);
        using var brush = new SolidBrush(AxisLabelColor);
        foreach (var axis in scene.Axes)
        {
            var projected = Graph3DApi.ProjectToScreen(axis.End, plot, view, camera);
            if (!float.IsFinite(projected.Screen.X) || !float.IsFinite(projected.Screen.Y))
                continue;

            graphics.DrawString(axis.Label, font, brush, projected.Screen.X + 4f, projected.Screen.Y - 12f);
        }
    }

    private static void DrawNumberLabels(Graphics graphics, GraphGridScene3D scene, Rectangle plot, GraphPlotView3D view, GraphCamera3D camera)
    {
        using var font = new Font("Segoe UI", 7f);
        using var brush = new SolidBrush(NumberLabelColor);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        DrawAxisTickLabels(graphics, scene.GridLines.Where(line => line.Plane == GraphGridPlane3D.XY && line.Direction == GraphAxis3D.Y), value => new GraphPoint3D(value, 0d, 0d), plot, view, camera, font, brush, format, new PointF(0f, 12f));
        DrawAxisTickLabels(graphics, scene.GridLines.Where(line => line.Plane == GraphGridPlane3D.XY && line.Direction == GraphAxis3D.X), value => new GraphPoint3D(0d, value, 0d), plot, view, camera, font, brush, format, new PointF(-18f, 0f));
        DrawAxisTickLabels(graphics, scene.GridLines.Where(line => line.Plane == GraphGridPlane3D.XZ && line.Direction == GraphAxis3D.X), value => new GraphPoint3D(0d, 0d, value), plot, view, camera, font, brush, format, new PointF(-18f, -4f));
    }

    private static void DrawAxisTickLabels(
        Graphics graphics,
        IEnumerable<GraphGridLine3D> lines,
        Func<double, GraphPoint3D> anchorSelector,
        Rectangle plot,
        GraphPlotView3D view,
        GraphCamera3D camera,
        Font font,
        Brush brush,
        StringFormat format,
        PointF offset)
    {
        var ticks = lines
            .GroupBy(line => line.Value)
            .Select(group => group.First())
            .OrderBy(line => line.Value)
            .ToArray();
        if (ticks.Length == 0)
            return;

        var stride = Math.Max(1, (int)Math.Ceiling(ticks.Length / 9d));
        var lastRect = RectangleF.Empty;
        for (var index = 0; index < ticks.Length; index += stride)
        {
            var line = ticks[index];
            if (Math.Abs(line.Value) < 1e-10d)
                continue;

            var projected = Graph3DApi.ProjectToScreen(anchorSelector(line.Value), plot, view, camera);
            if (!float.IsFinite(projected.Screen.X) || !float.IsFinite(projected.Screen.Y))
                continue;

            var text = GraphNumberFormatter.FormatDisplayNumber(line.Value);
            var size = graphics.MeasureString(text, font);
            var rect = new RectangleF(
                projected.Screen.X + offset.X - size.Width / 2f,
                projected.Screen.Y + offset.Y - size.Height / 2f,
                size.Width,
                size.Height);
            rect.X = Math.Clamp(rect.X, plot.Left + 3f, plot.Right - rect.Width - 3f);
            rect.Y = Math.Clamp(rect.Y, plot.Top + 3f, plot.Bottom - rect.Height - 3f);
            if (!lastRect.IsEmpty && rect.IntersectsWith(RectangleF.Inflate(lastRect, 3f, 2f)))
                continue;

            graphics.DrawString(text, font, brush, rect, format);
            lastRect = rect;
        }
    }
}
