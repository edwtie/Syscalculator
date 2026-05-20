#nullable enable
using System.ComponentModel;
using System.Drawing.Drawing2D;
using Tiedragon.Graph;
using Tiedragon.Graph.G3D;

namespace Syscalculator.UI.WinForms;

internal sealed class Graph3DRotationDial : Control
{
    private static readonly string[] CardinalLabels = { "N", "E", "S", "W" };
    private static readonly string[] IntercardinalLabels = { "NE", "SE", "SW", "NW" };

    private readonly ToolTip _toolTip = new();
    private bool _dragging;
    private bool _hover;
    private Point _lastMouse;
    private GraphCamera3D _camera = Graph3DApi.DefaultCamera;

    public Graph3DRotationDial()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        Width = 82;
        Height = 98;
        BackColor = Color.White;
        Cursor = Cursors.SizeAll;
        TabStop = false;
        _toolTip.SetToolTip(this, "Drag to rotate like a compass. Double-click to reset.");
    }

    public event Action<double, double>? RotationDeltaRequested;
    public event Action? ResetRequested;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public GraphCamera3D Camera
    {
        get => _camera;
        set
        {
            _camera = value;
            Invalidate();
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        if (!_dragging)
            Cursor = Cursors.SizeAll;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _dragging = true;
            _lastMouse = e.Location;
            Capture = true;
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging)
        {
            var dx = e.X - _lastMouse.X;
            var dy = e.Y - _lastMouse.Y;
            _lastMouse = e.Location;
            RotationDeltaRequested?.Invoke(dx * 0.7d, -dy * 0.5d);
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _dragging = false;
        Capture = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnDoubleClick(EventArgs e)
    {
        ResetRequested?.Invoke();
        base.OnDoubleClick(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        var readoutHeight = Math.Max(14, (int)Math.Round(Height * 0.18d));
        var dialSize = Math.Max(1, Math.Min(Width, Height - readoutHeight));
        var dialX = (Width - dialSize) / 2;
        using var path = new GraphicsPath();
        path.AddEllipse(new Rectangle(dialX, 0, dialSize, dialSize));
        path.AddRectangle(new Rectangle(0, Height - readoutHeight - 1, Math.Max(1, Width), readoutHeight + 1));
        Region = new Region(path);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        // Keep the overlay from painting a rectangular block over the graph grid.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var readoutHeight = Math.Max(14f, Height * 0.18f);
        var dialSize = Math.Min(Width, Height - readoutHeight);
        var inset = Math.Max(4f, dialSize * 0.06f);
        var rect = new RectangleF((Width - dialSize) / 2f + inset, inset, dialSize - inset * 2f, dialSize - inset * 2f);
        using var shadow = new SolidBrush(Color.FromArgb(28, 15, 23, 42));
        g.FillEllipse(shadow, rect.X + 1.5f, rect.Y + 2f, rect.Width, rect.Height);

        var state = _dragging
            ? GraphOverlayVisualState.Pressed
            : _hover ? GraphOverlayVisualState.Hover : GraphOverlayVisualState.Normal;
        using var rimFill = new SolidBrush(GraphOverlayStyle.ButtonFill(state, translucent: false));
        using var faceFill = new LinearGradientBrush(rect, Color.FromArgb(15, 23, 42), Color.FromArgb(30, 41, 59), LinearGradientMode.ForwardDiagonal);
        using var border = new Pen(GraphOverlayStyle.ButtonBorder(state, translucent: false), 1f);
        g.FillEllipse(rimFill, rect);
        g.FillEllipse(faceFill, RectangleF.Inflate(rect, -3f, -3f));
        g.DrawEllipse(border, rect);

        var cx = Width / 2f;
        var cy = Height / 2f;
        var radius = Math.Min(rect.Width, rect.Height) / 2f;
        DrawCompassRose(g, cx, cy, radius);
        DrawCompassNeedle(g, cx, cy, radius);

        var hubRadius = Math.Max(5f, radius * 0.16f);
        using var centerFill = new SolidBrush(Color.FromArgb(226, 232, 240));
        using var centerBorder = new Pen(Color.FromArgb(248, 250, 252), 1f);
        g.FillEllipse(centerFill, cx - hubRadius, cy - hubRadius, hubRadius * 2f, hubRadius * 2f);
        g.DrawEllipse(centerBorder, cx - hubRadius, cy - hubRadius, hubRadius * 2f, hubRadius * 2f);

        using var textBrush = new SolidBrush(Color.FromArgb(226, 232, 240));
        using var smallFont = new Font("Segoe UI", Math.Max(5.8f, radius * 0.15f), FontStyle.Bold);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        var yaw = NormalizeDegrees(_camera.YawDegrees);
        using var readoutBrush = new SolidBrush(Color.FromArgb(15, 63, 143));
        var readoutRect = Rectangle.Round(new RectangleF(cx - radius * 0.50f, Height - readoutHeight + 1f, radius, readoutHeight - 2f));
        var readoutRadius = new Size(Math.Max(3, (int)Math.Round(readoutHeight * 0.25f)), Math.Max(3, (int)Math.Round(readoutHeight * 0.25f)));
        using var readoutFill = new SolidBrush(Color.FromArgb(235, 248, 251, 255));
        using var readoutBorder = new Pen(Color.FromArgb(180, 191, 219, 254), 1f);
        g.FillRoundedRectangle(readoutFill, readoutRect, readoutRadius);
        g.DrawRoundedRectangle(readoutBorder, readoutRect, readoutRadius);
        g.DrawString($"{yaw:0} deg", smallFont, readoutBrush, readoutRect, format);
    }

    private static void DrawCompassRose(Graphics g, float cx, float cy, float radius)
    {
        var faceRadius = radius - 3f;
        using var outer = new Pen(Color.FromArgb(226, 232, 240), Math.Max(1f, radius * 0.035f));
        using var inner = new Pen(Color.FromArgb(71, 85, 105), 1f);
        g.DrawEllipse(outer, cx - faceRadius, cy - faceRadius, faceRadius * 2f, faceRadius * 2f);
        g.DrawEllipse(inner, cx - radius * 0.74f, cy - radius * 0.74f, radius * 1.48f, radius * 1.48f);

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
            g.DrawLine(tickPen, cx + ux * tickInner, cy + uy * tickInner, cx + ux * tickOuter, cy + uy * tickOuter);
        }

        DrawDirectionLabels(g, cx, cy, radius);

        if (radius >= 58f)
            DrawDegreeLabels(g, cx, cy, radius);
    }

    private static void DrawDirectionLabels(Graphics g, float cx, float cy, float radius)
    {
        using var cardinalFont = new Font("Segoe UI", Math.Max(6.5f, radius * 0.18f), FontStyle.Bold);
        using var interFont = new Font("Segoe UI", Math.Max(5f, radius * 0.12f), FontStyle.Bold);
        using var cardinalBrush = new SolidBrush(Color.FromArgb(248, 250, 252));
        using var interBrush = new SolidBrush(Color.FromArgb(203, 213, 225));
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        for (var i = 0; i < CardinalLabels.Length; i++)
            DrawPolarText(g, CardinalLabels[i], cardinalFont, cardinalBrush, format, cx, cy, radius * 0.56f, i * 90d);

        if (radius < 48f)
            return;

        for (var i = 0; i < IntercardinalLabels.Length; i++)
            DrawPolarText(g, IntercardinalLabels[i], interFont, interBrush, format, cx, cy, radius * 0.52f, 45d + i * 90d);
    }

    private static void DrawDegreeLabels(Graphics g, float cx, float cy, float radius)
    {
        using var degreeFont = new Font("Segoe UI", Math.Max(5.5f, radius * 0.14f), FontStyle.Regular);
        using var degreeBrush = new SolidBrush(Color.FromArgb(203, 213, 225));
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        for (var degrees = 0; degrees < 360; degrees += 60)
            DrawPolarText(g, degrees.ToString("0"), degreeFont, degreeBrush, format, cx, cy, radius * 0.78f, degrees);
    }

    private static void DrawPolarText(Graphics g, string text, Font font, Brush brush, StringFormat format, float cx, float cy, float distance, double degrees)
    {
        var angle = (degrees - 90d) * Math.PI / 180d;
        var x = cx + (float)Math.Cos(angle) * distance;
        var y = cy + (float)Math.Sin(angle) * distance;
        var size = Math.Max(14f, font.Size * 3f);
        g.DrawString(text, font, brush, new RectangleF(x - size / 2f, y - size / 2f, size, size), format);
    }

    private void DrawCompassNeedle(Graphics g, float cx, float cy, float radius)
    {
        var angle = (_camera.YawDegrees - 90d) * Math.PI / 180d;
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
        g.FillPolygon(red, north);
        g.DrawPolygon(outline, north);
        g.FillPolygon(blue, south);
        g.DrawPolygon(outline, south);
    }

    private static double NormalizeDegrees(double degrees)
    {
        var normalized = degrees % 360d;
        return normalized < 0d ? normalized + 360d : normalized;
    }
}
