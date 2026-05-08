#nullable enable

namespace Tiedragon.ClipboardConvert;

/// <summary>
/// Small reusable icons for clipboard diagnostics.
/// </summary>
public static class ClipboardDoctorIconApi
{
    public static Bitmap CreateClipboardDoctorIcon(int size = 16)
    {
        size = Math.Max(12, size);
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        var scale = size / 16f;
        using var clipboardFill = new SolidBrush(Color.FromArgb(248, 250, 252));
        using var clipboardBorder = new Pen(Color.FromArgb(71, 85, 105), Math.Max(1f, scale));
        using var accentPen = new Pen(Color.FromArgb(15, 63, 143), Math.Max(1.4f, 1.8f * scale))
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };
        using var darkPen = new Pen(Color.FromArgb(31, 41, 55), Math.Max(1f, 1.2f * scale))
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };
        using var clipBrush = new SolidBrush(Color.FromArgb(226, 232, 240));

        var body = Rectangle.Round(new RectangleF(3f * scale, 2.8f * scale, 10f * scale, 11.8f * scale));
        graphics.FillRectangle(clipboardFill, body);
        graphics.DrawRectangle(clipboardBorder, body);

        var clip = Rectangle.Round(new RectangleF(5.2f * scale, 1.4f * scale, 5.6f * scale, 3f * scale));
        graphics.FillRectangle(clipBrush, clip);
        graphics.DrawRectangle(clipboardBorder, clip);

        graphics.DrawArc(accentPen, 5.3f * scale, 6.2f * scale, 5.4f * scale, 5.1f * scale, 15, 250);
        graphics.DrawLine(accentPen, 10.2f * scale, 10.5f * scale, 12.4f * scale, 12.9f * scale);
        graphics.DrawEllipse(darkPen, 11.7f * scale, 12.1f * scale, 1.7f * scale, 1.7f * scale);
        graphics.DrawLine(darkPen, 6.6f * scale, 5.8f * scale, 5.3f * scale, 4.8f * scale);
        graphics.DrawLine(darkPen, 9.4f * scale, 5.8f * scale, 10.7f * scale, 4.8f * scale);

        return bitmap;
    }
}
