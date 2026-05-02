#nullable enable
using System.Globalization;
using System.Drawing.Drawing2D;

namespace Syscalculator.UI.WinForms;

internal enum GraphPlotDensity
{
    Compact,
    Normal
}

internal readonly record struct GraphPlotView(float MinX, float MaxX, float MinY, float MaxY);
internal readonly record struct GraphLineSeries(IReadOnlyList<PointF> Points, Color Color, float Width);

internal static class GraphPlotRenderer
{
    public static Rectangle GetPlotRectangle(Control canvas)
    {
        return Rectangle.Inflate(canvas.ClientRectangle, -1, -1);
    }

    public static PointF ScreenToGraph(PointF screenPoint, Rectangle plot, GraphPlotView view)
    {
        var x = view.MinX + ((screenPoint.X - plot.Left) / plot.Width) * (view.MaxX - view.MinX);
        var y = view.MaxY - ((screenPoint.Y - plot.Top) / plot.Height) * (view.MaxY - view.MinY);
        return new PointF(x, y);
    }

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
        GraphPlotDensity density)
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
            density);
    }

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
        GraphPlotDensity density)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = canvas.ClientRectangle;
        if (rect.Width <= 20 || rect.Height <= 20)
            return;

        var plot = GetPlotRectangle(canvas);
        using var background = new SolidBrush(Color.FromArgb(248, 251, 255));
        g.FillRectangle(background, rect);

        DrawGrid(g, plot);

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

        DrawAxes(g, canvas.Font, plot, view, requestedMinX, requestedMaxX, requestedStep, density, Map);

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

    private static void DrawGrid(Graphics g, Rectangle plot)
    {
        using var borderPen = new Pen(Color.FromArgb(203, 216, 234), 1);
        g.DrawRectangle(borderPen, plot);
    }

    private static void DrawAxes(
        Graphics g,
        Font labelFont,
        Rectangle plot,
        GraphPlotView view,
        float requestedMinX,
        float requestedMaxX,
        float requestedStep,
        GraphPlotDensity density,
        Func<PointF, PointF> map)
    {
        using var axisPen = new Pen(Color.FromArgb(51, 65, 85), 1.4f);
        using var gridPen = new Pen(Color.FromArgb(219, 231, 247), 1f);
        using var tickPen = new Pen(Color.FromArgb(71, 85, 105), 1f);
        using var labelBrush = new SolidBrush(Color.FromArgb(15, 63, 143));
        using var tickFont = new Font("Segoe UI", density == GraphPlotDensity.Compact ? 7.5f : 8f);

        var xTicks = IsSameRange(view.MinX, view.MaxX, requestedMinX, requestedMaxX)
            ? BuildTicks(requestedMinX, requestedMaxX, requestedStep, density == GraphPlotDensity.Compact ? 9 : 11)
            : BuildNiceTicks(view.MinX, view.MaxX, maxTicks: 8);
        var yTicks = BuildNiceTicks(view.MinY, view.MaxY, density == GraphPlotDensity.Compact ? 6 : 7);

        foreach (var xValue in xTicks)
        {
            if (xValue < view.MinX || xValue > view.MaxX)
                continue;

            var xPoint = map(new PointF(xValue, view.MinY));
            g.DrawLine(gridPen, xPoint.X, plot.Top, xPoint.X, plot.Bottom);
        }

        foreach (var yValue in yTicks)
        {
            var yPoint = map(new PointF(view.MinX, yValue));
            g.DrawLine(gridPen, plot.Left, yPoint.Y, plot.Right, yPoint.Y);
        }

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
        var tickSize = density == GraphPlotDensity.Compact ? 4 : 5;
        var xLabelOffset = density == GraphPlotDensity.Compact ? 5 : 7;
        var yLabelOffset = density == GraphPlotDensity.Compact ? 6 : 7;

        foreach (var xValue in xTicks)
        {
            if (xValue < view.MinX || xValue > view.MaxX)
                continue;

            if (Math.Abs(xValue) < 0.000001f && yAxisVisible && xAxisVisible)
                continue;

            var xPoint = map(new PointF(xValue, view.MinY));
            g.DrawLine(tickPen, xPoint.X, xAxisY - tickSize, xPoint.X, xAxisY + tickSize);
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
            g.DrawLine(tickPen, yAxisX - tickSize, yPoint.Y, yAxisX + tickSize, yPoint.Y);
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

        g.DrawString("x", labelFont, labelBrush, plot.Right - 12, Math.Clamp(xAxisY - 14, plot.Top + 2, plot.Bottom - 16));
        g.DrawString("y", labelFont, labelBrush, Math.Clamp(yAxisX - 18, plot.Left + 2, plot.Right - 14), plot.Top + 2);
    }

    private static IReadOnlyList<float> BuildTicks(float min, float max, float step, int maxTicks)
    {
        if (step <= 0 || (max - min) / step > maxTicks)
        {
            return Enumerable.Range(0, maxTicks)
                .Select(i => min + ((max - min) * i / (maxTicks - 1)))
                .ToArray();
        }

        var ticks = new List<float>();
        for (var value = min; value <= max + (step / 1000f); value += step)
            ticks.Add(value);
        return ticks;
    }

    private static IReadOnlyList<float> BuildNiceTicks(float min, float max, int maxTicks)
    {
        if (max <= min)
            return [min];

        var range = NiceNumber(max - min, round: false);
        var spacing = NiceNumber(range / Math.Max(1, maxTicks - 1), round: true);
        var niceMin = MathF.Floor(min / spacing) * spacing;
        var niceMax = MathF.Ceiling(max / spacing) * spacing;

        var ticks = new List<float>();
        for (var value = niceMin; value <= niceMax + spacing * 0.5f; value += spacing)
        {
            if (value >= min - spacing * 0.25f && value <= max + spacing * 0.25f)
                ticks.Add(Math.Abs(value) < spacing / 1000f ? 0 : value);
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

    private static bool IsSameRange(float minA, float maxA, float minB, float maxB)
    {
        return Math.Abs(minA - minB) < 0.0001f && Math.Abs(maxA - maxB) < 0.0001f;
    }

    private static string FormatTick(float value)
    {
        return Math.Abs(value) < 0.000001f
            ? "0"
            : value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
