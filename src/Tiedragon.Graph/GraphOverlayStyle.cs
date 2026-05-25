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
    public static bool UseDarkTheme { get; set; }

    public static Color IconColor(GraphOverlayVisualState state)
    {
        if (UseDarkTheme)
        {
            return state == GraphOverlayVisualState.Disabled
                ? Color.FromArgb(100, 116, 139)
                : Color.FromArgb(147, 197, 253);
        }

        return state == GraphOverlayVisualState.Disabled ? Color.FromArgb(148, 163, 184) : Color.FromArgb(15, 63, 143);
    }

    public static Color TextColor(GraphOverlayVisualState state)
    {
        return IconColor(state);
    }

    public static Color ButtonFill(GraphOverlayVisualState state, bool translucent)
    {
        if (UseDarkTheme)
        {
            return state switch
            {
                GraphOverlayVisualState.Disabled => translucent ? Color.FromArgb(150, 31, 41, 55) : Color.FromArgb(31, 41, 55),
                GraphOverlayVisualState.Pressed => translucent ? Color.FromArgb(220, 30, 64, 175) : Color.FromArgb(30, 64, 175),
                GraphOverlayVisualState.Hover => translucent ? Color.FromArgb(210, 51, 65, 85) : Color.FromArgb(51, 65, 85),
                _ => translucent ? Color.FromArgb(210, 15, 23, 42) : Color.FromArgb(30, 41, 59)
            };
        }

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
        if (UseDarkTheme)
        {
            var darkAlpha = translucent
                ? state == GraphOverlayVisualState.Hover ? 230 : 170
                : 255;
            return state == GraphOverlayVisualState.Pressed
                ? Color.FromArgb(darkAlpha, 96, 165, 250)
                : Color.FromArgb(darkAlpha, 71, 85, 105);
        }

        var alpha = translucent
            ? state == GraphOverlayVisualState.Hover ? 210 : 150
            : 255;
        return Color.FromArgb(alpha, 203, 216, 234);
    }

    public static Color PanelFill(bool translucent)
    {
        if (UseDarkTheme)
            return translucent ? Color.FromArgb(225, 15, 23, 42) : Color.FromArgb(15, 23, 42);

        return translucent ? Color.FromArgb(210, 255, 255, 255) : Color.White;
    }

    public static Color PanelBorder(bool translucent)
    {
        if (UseDarkTheme)
            return translucent ? Color.FromArgb(190, 71, 85, 105) : Color.FromArgb(71, 85, 105);

        return translucent ? Color.FromArgb(150, 173, 196, 225) : Color.FromArgb(203, 216, 234);
    }

    public static Color TitleFill => UseDarkTheme ? Color.FromArgb(30, 41, 59) : Color.FromArgb(239, 246, 255);
    public static Color TitleText => UseDarkTheme ? Color.FromArgb(147, 197, 253) : Color.FromArgb(15, 63, 143);
    public static Color DetailText => UseDarkTheme ? Color.FromArgb(148, 163, 184) : Color.FromArgb(71, 85, 105);
    public static Color TableBack => UseDarkTheme ? Color.FromArgb(17, 24, 39) : Color.White;
    public static Color TableAlternateBack => UseDarkTheme ? Color.FromArgb(30, 41, 59) : Color.FromArgb(248, 251, 255);
    public static Color TableGrid => UseDarkTheme ? Color.FromArgb(51, 65, 85) : Color.FromArgb(225, 232, 242);
    public static Color TableText => UseDarkTheme ? Color.FromArgb(226, 232, 240) : Color.FromArgb(17, 24, 39);
    public static Color TableSelectionBack => UseDarkTheme ? Color.FromArgb(37, 99, 235) : Color.FromArgb(219, 234, 254);
    public static Color TableSelectionText => UseDarkTheme ? Color.White : Color.FromArgb(17, 24, 39);
    public static Color IconFill => UseDarkTheme ? Color.FromArgb(175, 30, 41, 59) : Color.FromArgb(170, 239, 246, 255);
    public static Color IconFrontFill => UseDarkTheme ? Color.FromArgb(185, 51, 65, 85) : Color.FromArgb(185, 255, 255, 255);

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
