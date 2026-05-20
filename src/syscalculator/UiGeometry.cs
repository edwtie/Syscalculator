#nullable enable

using System.Drawing.Drawing2D;

namespace Syscalculator.UI.WinForms;

internal static class UiGeometry
{
    public static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        return CreateRoundedRectangle(new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height), radius);
    }

    public static GraphicsPath CreateRoundedRectangle(RectangleF bounds, float radius)
    {
        var diameter = Math.Max(1f, radius * 2f);
        var path = new GraphicsPath();

        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}
