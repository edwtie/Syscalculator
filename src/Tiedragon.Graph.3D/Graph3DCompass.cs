#nullable enable

// Copyright (c) 1995-2026 Edward Tie / Tiedragon. All rights reserved.
// Draws the Graph3D compass UI and degree text used by graph overlays.
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Tiedragon.Graph.G3D;

/// <summary>
/// Paints the reusable Graph3D compass dial and the matching degree readout.
/// </summary>
public static class Graph3DCompass
{
    private static readonly string[] IntercardinalLabels = { "NE", "SE", "SW", "NW" };

    /// <summary>
    /// Draws a compact compass that visualizes the camera yaw.
    /// </summary>
    public static void DrawCompass(
        Graphics graphics,
        Rectangle bounds,
        GraphCamera3D camera,
        bool hover = false,
        bool pressed = false)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var dialSize = Math.Min(bounds.Width, bounds.Height);
        var inset = Math.Max(1f, dialSize * 0.015f);
        var rect = new RectangleF(
            bounds.Left + (bounds.Width - dialSize) / 2f + inset,
            bounds.Top + (bounds.Height - dialSize) / 2f + inset,
            dialSize - inset * 2f,
            dialSize - inset * 2f);
        var state = pressed
            ? GraphOverlayVisualState.Pressed
            : hover ? GraphOverlayVisualState.Hover : GraphOverlayVisualState.Normal;
        var faceStart = state == GraphOverlayVisualState.Pressed ? Color.FromArgb(8, 13, 26) : Color.FromArgb(15, 23, 42);
        var faceEnd = state == GraphOverlayVisualState.Hover ? Color.FromArgb(42, 56, 79) : Color.FromArgb(30, 41, 59);
        using var faceFill = new LinearGradientBrush(rect, faceStart, faceEnd, LinearGradientMode.ForwardDiagonal);
        using var border = new Pen(Color.FromArgb(148, 163, 184), 1f);
        graphics.FillEllipse(faceFill, rect);
        graphics.DrawEllipse(border, rect);

        var cx = bounds.Left + bounds.Width / 2f;
        var cy = bounds.Top + bounds.Height / 2f;
        var radius = Math.Min(rect.Width, rect.Height) / 2f;
        DrawCompassRose(graphics, cx, cy, radius);
        DrawCompassNeedle(graphics, cx, cy, radius, camera);

        var hubRadius = Math.Max(5f, radius * 0.16f);
        using var centerFill = new SolidBrush(Color.FromArgb(226, 232, 240));
        using var centerBorder = new Pen(Color.FromArgb(248, 250, 252), 1f);
        graphics.FillEllipse(centerFill, cx - hubRadius, cy - hubRadius, hubRadius * 2f, hubRadius * 2f);
        graphics.DrawEllipse(centerBorder, cx - hubRadius, cy - hubRadius, hubRadius * 2f, hubRadius * 2f);
    }

    /// <summary>
    /// Draws the normalized yaw angle below the compass dial.
    /// </summary>
    public static void DrawDegreeReadout(Graphics graphics, Rectangle dialBounds, GraphCamera3D camera)
    {
        if (dialBounds.Width <= 0 || dialBounds.Height <= 0)
            return;

        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        var yaw = NormalizeDegrees(camera.YawDegrees);
        using var font = new Font("Segoe UI", 7f, FontStyle.Bold);
        using var shadow = new SolidBrush(Color.FromArgb(230, 255, 255, 255));
        using var text = new SolidBrush(Color.FromArgb(15, 63, 143));
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        var bounds = new RectangleF(dialBounds.Left - 4f, dialBounds.Bottom + 1f, dialBounds.Width + 8f, 15f);
        graphics.DrawString($"{yaw:0} deg", font, shadow, new RectangleF(bounds.X + 1f, bounds.Y + 1f, bounds.Width, bounds.Height), format);
        graphics.DrawString($"{yaw:0} deg", font, text, bounds, format);
    }

    /// <summary>
    /// Normalizes an angle to the 0-359 degree range used by the compass display.
    /// </summary>
    public static double NormalizeDegrees(double degrees)
    {
        if (!double.IsFinite(degrees))
            return 0d;

        var normalized = degrees % 360d;
        return normalized < 0d ? normalized + 360d : normalized;
    }

    private static void DrawCompassRose(Graphics graphics, float cx, float cy, float radius)
    {
        var faceRadius = radius - 3f;
        using var outer = new Pen(Color.FromArgb(226, 232, 240), Math.Max(1f, radius * 0.035f));
        using var inner = new Pen(Color.FromArgb(71, 85, 105), 1f);
        graphics.DrawEllipse(outer, cx - faceRadius, cy - faceRadius, faceRadius * 2f, faceRadius * 2f);
        graphics.DrawEllipse(inner, cx - radius * 0.74f, cy - radius * 0.74f, radius * 1.48f, radius * 1.48f);

        for (var degrees = 0; degrees < 360; degrees += 5)
        {
            var major = degrees % 30 == 0;
            var cardinal = degrees % 90 == 0;
            var tickOuter = radius * 0.88f;
            var tickInner = radius * (cardinal ? 0.58f : major ? 0.68f : 0.78f);
            using var tickPen = new Pen(
                cardinal ? Color.FromArgb(248, 250, 252) : major ? Color.FromArgb(203, 213, 225) : Color.FromArgb(148, 163, 184),
                cardinal ? Math.Max(1.6f, radius * 0.045f) : major ? Math.Max(1.1f, radius * 0.032f) : 1f);

            var angle = (degrees - 90d) * Math.PI / 180d;
            var ux = (float)Math.Cos(angle);
            var uy = (float)Math.Sin(angle);
            graphics.DrawLine(tickPen, cx + ux * tickInner, cy + uy * tickInner, cx + ux * tickOuter, cy + uy * tickOuter);
        }

        DrawDirectionLabels(graphics, cx, cy, radius);

        if (radius >= 58f)
            DrawDegreeLabels(graphics, cx, cy, radius);
    }

    private static void DrawDirectionLabels(Graphics graphics, float cx, float cy, float radius)
    {
        using var cardinalFont = new Font("Segoe UI", Math.Max(6.5f, radius * 0.18f), FontStyle.Bold);
        using var interFont = new Font("Segoe UI", Math.Max(5f, radius * 0.18f), FontStyle.Regular);
        using var cardinalBrush = new SolidBrush(Color.FromArgb(248, 250, 252));
        using var interBrush = new SolidBrush(Color.FromArgb(203, 213, 225));
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        DrawPolarText(graphics, "N", cardinalFont, cardinalBrush, format, cx, cy, radius * 0.56f, 0d);
        DrawPolarText(graphics, "E", cardinalFont, cardinalBrush, format, cx, cy, radius * 0.56f, 90d);
        DrawPolarText(graphics, "S", cardinalFont, cardinalBrush, format, cx, cy, radius * 0.56f, 180d);
        DrawPolarText(graphics, "W", cardinalFont, cardinalBrush, format, cx, cy, radius * 0.56f, 270d);

        if (radius < 28f)
            return;

        for (var i = 0; i < IntercardinalLabels.Length; i++)
            DrawPolarText(graphics, IntercardinalLabels[i], interFont, interBrush, format, cx, cy, radius * 0.47f, 45d + i * 90d);
    }

    private static void DrawDegreeLabels(Graphics graphics, float cx, float cy, float radius)
    {
        using var degreeFont = new Font("Segoe UI", Math.Max(5.5f, radius * 0.14f), FontStyle.Regular);
        using var degreeBrush = new SolidBrush(Color.FromArgb(203, 213, 225));
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        for (var degrees = 0; degrees < 360; degrees += 60)
            DrawPolarText(graphics, degrees.ToString("0"), degreeFont, degreeBrush, format, cx, cy, radius * 0.78f, degrees);
    }

    private static void DrawPolarText(Graphics graphics, string text, Font font, Brush brush, StringFormat format, float cx, float cy, float distance, double degrees)
    {
        var angle = (degrees - 90d) * Math.PI / 180d;
        var x = cx + (float)Math.Cos(angle) * distance;
        var y = cy + (float)Math.Sin(angle) * distance;
        var size = Math.Max(14f, font.Size * 3f);
        graphics.DrawString(text, font, brush, new RectangleF(x - size / 2f, y - size / 2f, size, size), format);
    }

    private static void DrawCompassNeedle(Graphics graphics, float cx, float cy, float radius, GraphCamera3D camera)
    {
        var angle = (camera.YawDegrees - 90d) * Math.PI / 180d;
        var ux = (float)Math.Cos(angle);
        var uy = (float)Math.Sin(angle);
        var px = -uy;
        var py = ux;

        PointF P(float along, float side) => new(cx + ux * along + px * side, cy + uy * along + py * side);
        var tip = radius * 0.72f;
        var tail = radius * 0.58f;
        var waist = radius * 0.10f;
        var halfWidth = Math.Max(3.6f, radius * 0.12f);
        var north = new[] { P(-waist, -halfWidth), P(tip, 0f), P(-waist, halfWidth) };
        var south = new[] { P(waist, -halfWidth), P(-tail, 0f), P(waist, halfWidth) };

        using var red = new SolidBrush(Color.FromArgb(220, 38, 38));
        using var blue = new SolidBrush(Color.FromArgb(37, 99, 235));
        using var outline = new Pen(Color.White, 1.1f);
        graphics.FillPolygon(red, north);
        graphics.DrawPolygon(outline, north);
        graphics.FillPolygon(blue, south);
        graphics.DrawPolygon(outline, south);
    }
}
