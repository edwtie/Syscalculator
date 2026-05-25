#nullable enable

// Copyright (c) 1995-2026 Edward Tie / Tiedragon. All rights reserved.
// Shared text button control for graph overlay modes such as 2D, 3D, and Top.
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Tiedragon.Graph;

/// <summary>
/// Small text button for graph overlays, used for mode buttons such as 2D, 3D, and Top.
/// </summary>
public sealed class GraphTextOverlayButton : Control
{
    private readonly Action _action;
    private readonly ToolTip _toolTip = new();
    private bool _hover;
    private bool _pressed;
    private bool _active;

    /// <summary>
    /// Creates a graph overlay button with text, a tooltip, and a click action.
    /// </summary>
    public GraphTextOverlayButton(string text, string tooltip, Action action)
    {
        _action = action;
        Text = text;
        Width = 34;
        Height = 22;
        Margin = new Padding(0, 1, 2, 0);
        ForeColor = GraphOverlayStyle.TextColor(GraphOverlayVisualState.Normal);
        Cursor = Cursors.Hand;
        TabStop = false;
        Font = new Font("Segoe UI", 7f, FontStyle.Bold);
        _toolTip.SetToolTip(this, tooltip);
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        BackColor = GraphOverlayStyle.PanelFill(translucent: false);
    }

    /// <summary>
    /// Gets or sets whether this button represents the active graph mode.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Active
    {
        get => _active;
        set
        {
            if (_active == value)
                return;

            _active = value;
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

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        var state = !Enabled
            ? GraphOverlayVisualState.Disabled
            : _pressed
                ? GraphOverlayVisualState.Pressed
                : _active
                    ? GraphOverlayVisualState.Pressed
                    : _hover ? GraphOverlayVisualState.Hover : GraphOverlayVisualState.Normal;
        GraphOverlayStyle.PaintButtonChrome(e.Graphics, rect, 6f, state, translucent: false);

        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            ClientRectangle,
            GraphOverlayStyle.TextColor(state),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
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

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        var fireClick = _pressed && ClientRectangle.Contains(e.Location);
        _pressed = false;
        Invalidate();
        if (fireClick)
            _action();
        base.OnMouseUp(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using var path = GraphOverlayStyle.RoundedRect(
            new RectangleF(0, 0, Math.Max(1, Width), Math.Max(1, Height)),
            6f);
        Region = new Region(path);
    }
}
