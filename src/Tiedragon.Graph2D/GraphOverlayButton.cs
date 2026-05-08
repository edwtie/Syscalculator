#nullable enable
using System.Drawing.Drawing2D;

namespace Tiedragon.Graph2D;

/// <summary>
/// Icon set for small graph overlay controls.
/// </summary>
public enum GraphOverlayButtonIcon
{
    /// <summary>Reset the graph view to its normal/home range.</summary>
    Home,

    /// <summary>Zoom the graph view in.</summary>
    ZoomIn,

    /// <summary>Zoom the graph view out.</summary>
    ZoomOut
}

/// <summary>
/// Visual size preset for graph overlay buttons.
/// </summary>
public enum GraphOverlayButtonDensity
{
    /// <summary>Small button for the embedded Graph Preview tab.</summary>
    Compact,

    /// <summary>Larger button for the standalone Graph Preview window.</summary>
    Normal
}

/// <summary>
/// Reusable icon-only overlay button for graph controls.
/// </summary>
/// <remarks>
/// Use <see cref="Create"/> from graph surfaces so icon drawing, sizing, hover states, and tooltips stay centralized.
/// </remarks>
public sealed class GraphOverlayButton : Control
{
    private readonly ToolTip _toolTip = new();
    private bool _hover;
    private bool _down;

    /// <summary>
    /// Creates a graph overlay button with the supplied icon, density, and tooltip.
    /// </summary>
    public GraphOverlayButton(GraphOverlayButtonIcon icon, GraphOverlayButtonDensity density, string tooltip)
    {
        Icon = icon;
        Density = density;
        Text = "";
        Width = density == GraphOverlayButtonDensity.Compact ? 26 : 36;
        Height = density == GraphOverlayButtonDensity.Compact ? 26 : 36;
        BackColor = Color.White;
        ForeColor = Color.FromArgb(31, 41, 55);
        Cursor = Cursors.Hand;
        TabStop = false;
        _toolTip.SetToolTip(this, tooltip);

        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
    }

    /// <summary>
    /// Creates a graph overlay button and attaches a click handler in one call.
    /// </summary>
    /// <param name="icon">The icon to draw inside the button.</param>
    /// <param name="density">The size preset to use.</param>
    /// <param name="tooltip">Tooltip text shown on hover.</param>
    /// <param name="click">Click handler for the button action.</param>
    public static GraphOverlayButton Create(GraphOverlayButtonIcon icon, GraphOverlayButtonDensity density, string tooltip, EventHandler click)
    {
        var button = new GraphOverlayButton(icon, density, tooltip);
        button.Click += click;
        return button;
    }

    /// <summary>
    /// Creates the standard graph navigation overlay: home, zoom in, and zoom out.
    /// </summary>
    /// <remarks>
    /// Use this when a graph surface needs the whole navigation group. This keeps button order, spacing,
    /// sizing, and panel chrome consistent between compact and normal graph views.
    /// </remarks>
    public static Panel CreateNavigationGroup(
        GraphOverlayButtonDensity density,
        string homeTooltip,
        EventHandler homeClick,
        string zoomInTooltip,
        EventHandler zoomInClick,
        string zoomOutTooltip,
        EventHandler zoomOutClick)
    {
        var compact = density == GraphOverlayButtonDensity.Compact;
        var panel = new GraphOverlayButtonGroupPanel(density)
        {
            Width = compact ? 32 : 44,
            Height = compact ? 94 : 128,
            BackColor = Color.White
        };

        var padding = compact ? 3 : 4;
        var step = compact ? 31 : 41;
        var buttons = new[]
        {
            Create(GraphOverlayButtonIcon.Home, density, homeTooltip, homeClick),
            Create(GraphOverlayButtonIcon.ZoomIn, density, zoomInTooltip, zoomInClick),
            Create(GraphOverlayButtonIcon.ZoomOut, density, zoomOutTooltip, zoomOutClick)
        };

        for (var i = 0; i < buttons.Length; i++)
        {
            buttons[i].Location = new Point(padding, padding + i * step);
            panel.Controls.Add(buttons[i]);
        }

        return panel;
    }

    /// <summary>
    /// Icon drawn by this button.
    /// </summary>
    public GraphOverlayButtonIcon Icon { get; }

    /// <summary>
    /// Visual size preset used by this button.
    /// </summary>
    public GraphOverlayButtonDensity Density { get; }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _down = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _down = true;
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        var fireClick = _down && ClientRectangle.Contains(e.Location);
        _down = false;
        Invalidate();
        if (fireClick)
            OnClick(EventArgs.Empty);
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(1, 1, Width - 3, Height - 3);
        using var path = RoundedRect(rect, Density == GraphOverlayButtonDensity.Compact ? 8 : 10);
        var fillColor = _down
            ? Color.FromArgb(219, 234, 254)
            : _hover ? Color.FromArgb(239, 246, 255) : Color.White;
        using var fill = new SolidBrush(fillColor);
        using var border = new Pen(Color.FromArgb(203, 216, 234), 1);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);

        var cx = rect.Left + rect.Width / 2f;
        var cy = rect.Top + rect.Height / 2f;
        switch (Icon)
        {
            case GraphOverlayButtonIcon.Home:
                DrawHome(e.Graphics, cx, cy);
                break;
            case GraphOverlayButtonIcon.ZoomIn:
                DrawZoom(e.Graphics, cx, cy, plus: true);
                break;
            case GraphOverlayButtonIcon.ZoomOut:
                DrawZoom(e.Graphics, cx, cy, plus: false);
                break;
        }
    }

    private void DrawHome(Graphics graphics, float cx, float cy)
    {
        var compact = Density == GraphOverlayButtonDensity.Compact;
        var scale = compact ? 0.75f : 1f;
        using var pen = new Pen(ForeColor, compact ? 1.7f : 1.8f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        PointF P(float x, float y) => new(cx + x * scale, cy + y * scale);
        var outline = new[]
        {
            P(-8f, -1f),
            P(0f, -8f),
            P(8f, -1f),
            P(8f, 8f),
            P(3f, 8f),
            P(3f, 2f),
            P(-3f, 2f),
            P(-3f, 8f),
            P(-8f, 8f),
            P(-8f, -1f)
        };
        graphics.DrawLines(pen, outline);
    }

    private void DrawZoom(Graphics graphics, float cx, float cy, bool plus)
    {
        var compact = Density == GraphOverlayButtonDensity.Compact;
        using var pen = new Pen(ForeColor, compact ? 2.6f : 2.6f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        var half = compact ? 5f : 7f;
        graphics.DrawLine(pen, cx - half, cy, cx + half, cy);
        if (plus)
            graphics.DrawLine(pen, cx, cy - half, cx, cy + half);
    }

    private static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private sealed class GraphOverlayButtonGroupPanel : Panel
    {
        private readonly GraphOverlayButtonDensity _density;

        public GraphOverlayButtonGroupPanel(GraphOverlayButtonDensity density)
        {
            _density = density;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var compact = _density == GraphOverlayButtonDensity.Compact;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedRect(rect, compact ? 12 : 9);
            using var fill = new SolidBrush(compact ? Color.White : Color.FromArgb(253, 254, 255));
            using var border = new Pen(compact ? Color.FromArgb(203, 216, 234) : Color.FromArgb(225, 234, 247), 1);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
        }
    }
}
