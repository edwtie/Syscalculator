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

        Width = 82;
        Height = 82;
        BackColor = GraphOverlayStyle.PanelFill(translucent: false);
        Cursor = Cursors.Hand;
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
            Cursor = Cursors.Hand;
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
        var dialSize = Math.Max(1, Math.Min(Width, Height));
        var dialX = (Width - dialSize) / 2;
        var dialY = (Height - dialSize) / 2;
        using var path = new GraphicsPath();
        path.AddEllipse(new Rectangle(dialX, dialY, dialSize, dialSize));
        Region = new Region(path);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        // Keep the overlay from painting a rectangular block over the graph grid.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graph3DCompass.DrawCompass(e.Graphics, ClientRectangle, _camera, _hover, _dragging);
    }
}

