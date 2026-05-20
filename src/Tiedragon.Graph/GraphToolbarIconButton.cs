#nullable enable
// Shared icon button control for graph overlay toolbars.
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Tiedragon.Graph;

/// <summary>
/// Icon choices for the shared graph toolbar buttons.
/// </summary>
public enum GraphToolbarIcon
{
    Copy,
    Open,
    TableVisible,
    TableHidden
}

/// <summary>
/// Shared WinForms toolbar button used by Graph2D and Graph3D overlays.
/// </summary>
public sealed class GraphToolbarIconButton : Button
{
    private GraphToolbarIcon _icon;
    private readonly ToolTip _toolTip = new();
    private bool _hover;
    private bool _pressed;

    /// <summary>
    /// Creates a graph toolbar button with the requested icon and tooltip text.
    /// </summary>
    public GraphToolbarIconButton(GraphToolbarIcon icon, string tooltip)
    {
        _icon = icon;
        Text = "";
        Width = 44;
        Height = 26;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        TabStop = false;
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
        BackColor = Color.White;
        _toolTip.SetToolTip(this, tooltip);
    }

    /// <summary>
    /// Gets or sets the icon rendered inside the button.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public GraphToolbarIcon Icon
    {
        get => _icon;
        set
        {
            if (_icon == value)
                return;

            _icon = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the tooltip shown when the button is hovered.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string TooltipText
    {
        get => _toolTip.GetToolTip(this) ?? "";
        set => _toolTip.SetToolTip(this, value);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var g = pevent.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new RectangleF(0.5f, 0.5f, ClientSize.Width - 1, ClientSize.Height - 1);
        var state = !Enabled
            ? GraphOverlayVisualState.Disabled
            : _pressed
                ? GraphOverlayVisualState.Pressed
                : _hover ? GraphOverlayVisualState.Hover : GraphOverlayVisualState.Normal;
        GraphOverlayStyle.PaintButtonChrome(g, rect, 6f, state, translucent: false);

        var iconColor = GraphOverlayStyle.IconColor(state);
        using var pen = new Pen(iconColor, 1.7f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        using var fill = new SolidBrush(Enabled ? Color.FromArgb(170, 239, 246, 255) : Color.FromArgb(120, 241, 245, 249));

        var cx = ClientSize.Width / 2f;
        var cy = ClientSize.Height / 2f;
        switch (_icon)
        {
            case GraphToolbarIcon.Copy:
                DrawCopy(g, pen, fill, cx, cy);
                break;
            case GraphToolbarIcon.Open:
                DrawOpen(g, pen, cx, cy);
                break;
            case GraphToolbarIcon.TableVisible:
                DrawTable(g, pen, fill, cx, cy, visible: true);
                break;
            case GraphToolbarIcon.TableHidden:
                DrawTable(g, pen, fill, cx, cy, visible: false);
                break;
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
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        if (mevent.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }

        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using var path = GraphOverlayStyle.RoundedRect(
            new RectangleF(0, 0, Math.Max(1, Width), Math.Max(1, Height)),
            6f);
        Region = new Region(path);
    }

    private static void DrawCopy(Graphics g, Pen pen, Brush fill, float cx, float cy)
    {
        var back = new RectangleF(cx - 8, cy - 7, 11, 13);
        var front = new RectangleF(cx - 4, cy - 4, 12, 14);
        g.FillRectangle(fill, back);
        g.DrawRectangle(pen, back.X, back.Y, back.Width, back.Height);
        using var frontFill = new SolidBrush(Color.FromArgb(185, 255, 255, 255));
        g.FillRectangle(frontFill, front);
        g.DrawRectangle(pen, front.X, front.Y, front.Width, front.Height);
    }

    private static void DrawOpen(Graphics g, Pen pen, float cx, float cy)
    {
        var box = new RectangleF(cx - 8, cy - 5, 12, 11);
        g.DrawRectangle(pen, box.X, box.Y, box.Width, box.Height);
        g.DrawLine(pen, cx - 1, cy + 1, cx + 8, cy - 8);
        g.DrawLine(pen, cx + 3, cy - 8, cx + 8, cy - 8);
        g.DrawLine(pen, cx + 8, cy - 8, cx + 8, cy - 3);
    }

    private static void DrawTable(Graphics g, Pen pen, Brush fill, float cx, float cy, bool visible)
    {
        var rect = new RectangleF(cx - 12, cy - 7, 14, 14);
        g.FillRectangle(fill, rect);
        g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        g.DrawLine(pen, rect.Left, rect.Top + 5, rect.Right, rect.Top + 5);
        g.DrawLine(pen, rect.Left + 4.7f, rect.Top, rect.Left + 4.7f, rect.Bottom);
        g.DrawLine(pen, rect.Left + 9.4f, rect.Top, rect.Left + 9.4f, rect.Bottom);

        var markCenter = new PointF(cx + 10.5f, cy);
        if (visible)
        {
            g.DrawLine(pen, markCenter.X - 4, markCenter.Y, markCenter.X + 4, markCenter.Y);
            g.DrawLine(pen, markCenter.X, markCenter.Y - 4, markCenter.X, markCenter.Y + 4);
        }
        else
        {
            g.DrawLine(pen, markCenter.X - 4, markCenter.Y, markCenter.X + 4, markCenter.Y);
        }
    }
}
