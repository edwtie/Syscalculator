#nullable enable

// Copyright (c) 1995-2026 Edward Tie / Tiedragon. All rights reserved.
// Draws reusable screen-space vector arrows for Graph2D and Graph3D surfaces.
using System.Drawing.Drawing2D;

namespace Tiedragon.Graph;

/// <summary>
/// Shared helper for drawing vector arrows after graph coordinates have been projected to screen coordinates.
/// </summary>
public static class GraphArrowRenderer
{
    /// <summary>
    /// Draws an arrow from start to end when both screen points are finite and visibly separated.
    /// </summary>
    public static void DrawArrow(Graphics graphics, PointF start, PointF end, Color color, float width = 2.2f)
    {
        if (!IsFinite(start) || !IsFinite(end))
            return;

        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        if ((dx * dx) + (dy * dy) < 4f)
            return;

        using var cap = new AdjustableArrowCap(Math.Max(4f, width * 2.4f), Math.Max(5f, width * 3.2f), true);
        using var pen = new Pen(color, width)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Custom,
            CustomEndCap = cap,
            LineJoin = LineJoin.Round
        };

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.DrawLine(pen, start, end);
    }

    private static bool IsFinite(PointF point)
    {
        return float.IsFinite(point.X) && float.IsFinite(point.Y);
    }
}
