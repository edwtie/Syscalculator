#nullable enable
using System.ComponentModel;
using System.Drawing.Drawing2D;
using Tiedragon.Graph;
using Tiedragon.Graph.G3D;

namespace Syscalculator.UI.WinForms;

internal sealed class Graph3DRotationDial : Control
{
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

        Width = 56;
        Height = 56;
        BackColor = Color.White;
        Cursor = Cursors.SizeAll;
        TabStop = false;
        _toolTip.SetToolTip(this, "Drag to rotate. Double-click to reset.");
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
        using var path = new GraphicsPath();
        path.AddEllipse(new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)));
        Region = new Region(path);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new RectangleF(4, 4, Width - 8, Height - 8);
        using var shadow = new SolidBrush(Color.FromArgb(28, 15, 23, 42));
        g.FillEllipse(shadow, rect.X + 1.5f, rect.Y + 2f, rect.Width, rect.Height);

        var state = _dragging
            ? GraphOverlayVisualState.Pressed
            : _hover ? GraphOverlayVisualState.Hover : GraphOverlayVisualState.Normal;
        using var fill = new SolidBrush(GraphOverlayStyle.ButtonFill(state, translucent: false));
        using var border = new Pen(GraphOverlayStyle.ButtonBorder(state, translucent: false), 1f);
        g.FillEllipse(fill, rect);
        g.DrawEllipse(border, rect);

        var cx = Width / 2f;
        var cy = Height / 2f;
        DrawCompassNeedle(g, cx, cy);

        using var centerFill = new SolidBrush(Color.FromArgb(219, 234, 254));
        using var centerBorder = new Pen(Color.FromArgb(59, 130, 246), 1f);
        g.FillEllipse(centerFill, cx - 10f, cy - 10f, 20f, 20f);
        g.DrawEllipse(centerBorder, cx - 10f, cy - 10f, 20f, 20f);

        using var textBrush = new SolidBrush(Color.FromArgb(15, 63, 143));
        using var smallFont = new Font("Segoe UI", 7f, FontStyle.Bold);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString($"{Math.Round(_camera.YawDegrees) % 360:0} deg", smallFont, textBrush, new RectangleF(cx - 22f, cy + 15f, 44f, 14f), format);
    }

    private void DrawCompassNeedle(Graphics g, float cx, float cy)
    {
        var angle = (_camera.YawDegrees - 90d) * Math.PI / 180d;
        var ux = (float)Math.Cos(angle);
        var uy = (float)Math.Sin(angle);
        var px = -uy;
        var py = ux;

        PointF P(float along, float side) => new(cx + ux * along + px * side, cy + uy * along + py * side);
        var north = new[] { P(-3f, -5f), P(23f, 0f), P(-3f, 5f) };
        var south = new[] { P(3f, -5f), P(-20f, 0f), P(3f, 5f) };

        using var red = new SolidBrush(Color.FromArgb(220, 38, 38));
        using var blue = new SolidBrush(Color.FromArgb(59, 130, 246));
        using var outline = new Pen(Color.White, 1.1f);
        g.FillPolygon(red, north);
        g.DrawPolygon(outline, north);
        g.FillPolygon(blue, south);
        g.DrawPolygon(outline, south);
    }
}
