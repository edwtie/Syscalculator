#nullable enable

// Copyright (c) 1995-2026 Edward Tie / Tiedragon. All rights reserved.
// Centralizes shared graph overlay colors, rounded shapes, and chrome painting.
using System.Drawing.Drawing2D;

namespace Tiedragon.Graph;

public enum GraphOverlayVisualState
{
    Normal,
    Hover,
    Pressed,
    Disabled
}

public static class GraphOverlayStyle
{
    public static Color IconColor(GraphOverlayVisualState state)
    {
        return state == GraphOverlayVisualState.Disabled
            ? Color.FromArgb(148, 163, 184)
            : Color.FromArgb(15, 63, 143);
    }

    public static Color TextColor(GraphOverlayVisualState state)
    {
        return IconColor(state);
    }

    public static Color ButtonFill(GraphOverlayVisualState state, bool translucent)
    {
        return state switch
        {
            GraphOverlayVisualState.Disabled => translucent ? Color.FromArgb(120, 246, 248, 252) : Color.FromArgb(246, 248, 252),
            GraphOverlayVisualState.Pressed => translucent ? Color.FromArgb(210, 219, 234, 254) : Color.FromArgb(219, 234, 254),
            GraphOverlayVisualState.Hover => translucent ? Color.FromArgb(190, 239, 246, 255) : Color.FromArgb(239, 246, 255),
            _ => translucent ? Color.FromArgb(145, 255, 255, 255) : Color.White
        };
    }

    public static Color ButtonBorder(GraphOverlayVisualState state, bool translucent)
    {
        var alpha = translucent
            ? state == GraphOverlayVisualState.Hover ? 210 : 150
            : 255;
        return Color.FromArgb(alpha, 203, 216, 234);
    }

    public static Color PanelFill(bool translucent)
    {
        return translucent ? Color.FromArgb(210, 255, 255, 255) : Color.White;
    }

    public static Color PanelBorder(bool translucent)
    {
        return translucent ? Color.FromArgb(150, 173, 196, 225) : Color.FromArgb(203, 216, 234);
    }

    public static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        return RoundedRect(new RectangleF(rect.X, rect.Y, rect.Width, rect.Height), radius);
    }

    public static GraphicsPath RoundedRect(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Max(0f, radius * 2f);
        if (diameter <= 0f)
        {
            path.AddRectangle(rect);
            return path;
        }

        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void PaintButtonChrome(Graphics graphics, RectangleF rect, float radius, GraphOverlayVisualState state, bool translucent)
    {
        using var path = RoundedRect(rect, radius);
        using var fill = new SolidBrush(ButtonFill(state, translucent));
        using var border = new Pen(ButtonBorder(state, translucent), 1f);
        graphics.FillPath(fill, path);
        graphics.DrawPath(border, path);
    }

    public static void PaintPanelChrome(Graphics graphics, RectangleF rect, float radius, bool translucent)
    {
        using var path = RoundedRect(rect, radius);
        using var fill = new SolidBrush(PanelFill(translucent));
        using var border = new Pen(PanelBorder(translucent), 1f);
        graphics.FillPath(fill, path);
        graphics.DrawPath(border, path);
    }
}
