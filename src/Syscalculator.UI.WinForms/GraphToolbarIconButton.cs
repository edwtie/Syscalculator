#nullable enable
using System.Drawing.Drawing2D;

namespace Syscalculator.UI.WinForms;

internal enum GraphToolbarIcon
{
    Copy,
    Open,
    TableVisible,
    TableHidden
}

internal sealed class GraphToolbarIconButton : Button
{
    private GraphToolbarIcon _icon;
    private readonly ToolTip _toolTip = new();
    private bool _hover;
    private bool _pressed;

    public GraphToolbarIconButton(GraphToolbarIcon icon, string tooltip)
    {
        _icon = icon;
        Text = "";
        Width = 44;
        Height = 26;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.White;
        Cursor = Cursors.Hand;
        TabStop = false;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        _toolTip.SetToolTip(this, tooltip);
    }

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

    public string TooltipText
    {
        get => _toolTip.GetToolTip(this) ?? "";
        set => _toolTip.SetToolTip(this, value);
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var g = pevent.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? SystemColors.Control);

        var rect = new RectangleF(0.5f, 0.5f, ClientSize.Width - 1, ClientSize.Height - 1);
        var backgroundColor = !Enabled
            ? Color.FromArgb(246, 248, 252)
            : _pressed
                ? Color.FromArgb(219, 234, 254)
                : _hover
                    ? Color.FromArgb(248, 251, 255)
                    : Color.White;
        using var background = new SolidBrush(backgroundColor);
        using var borderPen = new Pen(Color.FromArgb(199, 211, 226), 1f);
        using var path = RoundedRect(rect, 5f);
        g.FillPath(background, path);
        g.DrawPath(borderPen, path);

        var iconColor = Enabled ? Color.FromArgb(15, 63, 143) : Color.FromArgb(148, 163, 184);
        using var pen = new Pen(iconColor, 1.7f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        using var fill = new SolidBrush(Enabled ? Color.FromArgb(239, 246, 255) : Color.FromArgb(241, 245, 249));

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

    private static void DrawCopy(Graphics g, Pen pen, Brush fill, float cx, float cy)
    {
        var back = new RectangleF(cx - 8, cy - 7, 11, 13);
        var front = new RectangleF(cx - 4, cy - 4, 12, 14);
        g.FillRectangle(fill, back);
        g.DrawRectangle(pen, back.X, back.Y, back.Width, back.Height);
        g.FillRectangle(Brushes.White, front);
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

    private static GraphicsPath RoundedRect(RectangleF rect, float radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
