#nullable enable
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Tiedragon.ToolEditor;

/// <summary>
/// Reusable API for editor tab strips, tab headers, dirty markers, and close buttons.
/// </summary>
public static class ToolEditorTabsApi
{
    /// <summary>
    /// Creates the visual tab strip used above editor content.
    /// </summary>
    public static FlowLayoutPanel CreateStrip()
    {
        return new ToolEditorTabStripPanel();
    }

    /// <summary>
    /// Creates one visual tab header with title label and close button.
    /// </summary>
    public static ToolEditorTabHeaderParts CreateHeader(TabPage page, EventHandler select, EventHandler close)
    {
        var panel = new ToolEditorTabHeaderPanel
        {
            AutoSize = false,
            Width = 170,
            Height = 25,
            Margin = new Padding(0, 0, 3, 0),
            BackColor = Color.Transparent,
            Tag = page
        };

        var title = new Label
        {
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0),
            AutoEllipsis = true,
            BackColor = Color.Transparent,
            AutoSize = false,
            Location = new Point(8, 1),
            Size = new Size(panel.Width - 56, panel.Height - 4),
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom
        };

        var closeButton = new ToolEditorTabCloseButton
        {
            Text = string.Empty,
            Size = new Size(16, 16),
            Location = new Point(panel.Width - 22, 4),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        title.Click += select;
        panel.Click += select;
        closeButton.Click += close;

        panel.Controls.Add(title);
        panel.Controls.Add(closeButton);
        return new ToolEditorTabHeaderParts(panel, title, closeButton);
    }

    /// <summary>
    /// Creates the shared close glyph button used by tabs and lightweight panels.
    /// </summary>
    public static Button CreateCloseButton(EventHandler close)
    {
        var button = new ToolEditorTabCloseButton();
        button.Click += close;
        return button;
    }

    /// <summary>
    /// Creates the shared close glyph button with custom hover colors for non-tab surfaces.
    /// </summary>
    public static Button CreateCloseButton(
        EventHandler close,
        Color glyphColor,
        Color hoverGlyphColor,
        Color hoverBackColor,
        Color pressedBackColor)
    {
        var button = new ToolEditorTabCloseButton
        {
            GlyphColor = glyphColor,
            HoverGlyphColor = hoverGlyphColor,
            HoverBackColor = hoverBackColor,
            PressedBackColor = pressedBackColor
        };
        button.Click += close;
        return button;
    }

    /// <summary>
    /// Applies selected/dirty state and title text to a tab header.
    /// </summary>
    public static void SetHeaderState(Control headerPanel, Label title, string text, bool selected, bool dirty, Font baseFont)
    {
        if (headerPanel is ToolEditorTabHeaderPanel header)
        {
            header.Selected = selected;
            header.Dirty = dirty;
            header.Invalidate();
        }

        title.Text = text;
        title.ForeColor = selected ? Color.Black : Color.FromArgb(40, 55, 75);
        title.Font = new Font(baseFont, selected ? FontStyle.Bold : FontStyle.Regular);
    }

    /// <summary>
    /// Attaches a tab context menu that opens on right-click without selecting or opening the tab.
    /// </summary>
    public static void AttachContextMenu(ContextMenuStrip menu, params Control[] controls)
    {
        foreach (var control in controls)
        {
            control.ContextMenuStrip = null;
            control.MouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Right)
                    return;

                var screenLocation = control.PointToScreen(e.Location);
                control.BeginInvoke(() =>
                {
                    if (control.IsDisposed || menu.IsDisposed)
                        return;

                    var target = controls.FirstOrDefault(item => !item.IsDisposed && item.Visible) ?? control;
                    menu.Show(target, target.PointToClient(screenLocation));
                });
            };
        }
    }

    private sealed class ToolEditorTabHeaderPanel : Panel
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Selected { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Dirty { get; set; }

        public ToolEditorTabHeaderPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedTopRect(rect, 5);
            using var fill = new SolidBrush(Selected ? Color.White : Color.FromArgb(242, 246, 252));
            using var border = new Pen(Color.FromArgb(190, 200, 214));
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);

            if (Selected)
            {
                using var cover = new Pen(Color.White, 2);
                e.Graphics.DrawLine(cover, 1, Height - 2, Width - 2, Height - 2);
                e.Graphics.DrawLine(cover, 1, Height - 1, Width - 2, Height - 1);
            }

            if (Dirty)
            {
                using var dirtyBrush = new SolidBrush(Color.FromArgb(210, 32, 32));
                using var dirtyBorder = new Pen(Color.FromArgb(145, 20, 20));
                var marker = new Rectangle(Math.Max(4, Width - 39), 9, 8, 8);
                e.Graphics.FillEllipse(dirtyBrush, marker);
                e.Graphics.DrawEllipse(dirtyBorder, marker);
            }
        }
    }

    private sealed class ToolEditorTabStripPanel : FlowLayoutPanel
    {
        public ToolEditorTabStripPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var y = Height - 1;
            using var border = new Pen(Color.FromArgb(205, 212, 222));
            var x = 0;

            foreach (Control control in Controls)
            {
                if (control is not ToolEditorTabHeaderPanel { Selected: true })
                    continue;

                if (control.Left > x)
                    e.Graphics.DrawLine(border, x, y, control.Left, y);

                x = control.Right;
            }

            if (x < Width - 1)
                e.Graphics.DrawLine(border, x, y, Width - 1, y);
        }
    }

    private sealed class ToolEditorTabCloseButton : Button
    {
        private bool _hovered;
        private bool _pressed;

        public ToolEditorTabCloseButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.Transparent;
            TabStop = false;
            Cursor = Cursors.Hand;
            Padding = Padding.Empty;
            Margin = new Padding(0, 3, 5, 3);
            UseVisualStyleBackColor = false;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color GlyphColor { get; set; } = Color.FromArgb(45, 67, 98);

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color HoverGlyphColor { get; set; } = Color.FromArgb(0, 74, 173);

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color PressedGlyphColor { get; set; } = Color.White;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color HoverBackColor { get; set; } = Color.FromArgb(226, 238, 255);

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color PressedBackColor { get; set; } = Color.FromArgb(40, 94, 170);

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovered = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _pressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var tabBackColor = Parent switch
            {
                ToolEditorTabHeaderPanel { Selected: true } => Color.White,
                ToolEditorTabHeaderPanel => Color.FromArgb(242, 246, 252),
                { } parent => parent.BackColor,
                _ => BackColor
            };
            e.Graphics.Clear(tabBackColor);

            var glyphColor = GlyphColor;
            if (_hovered)
            {
                var back = _pressed ? PressedBackColor : HoverBackColor;
                using var fill = new SolidBrush(back);
                e.Graphics.FillRectangle(fill, new Rectangle(2, 3, Width - 4, Height - 6));
                glyphColor = _pressed ? PressedGlyphColor : HoverGlyphColor;
            }

            using var pen = new Pen(glyphColor, 2.1f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            var cx = Width / 2;
            var cy = Height / 2;
            var r = Math.Max(4, Math.Min(Width, Height) / 4);
            e.Graphics.DrawLine(pen, cx - r, cy - r, cx + r, cy + r);
            e.Graphics.DrawLine(pen, cx + r, cy - r, cx - r, cy + r);
        }
    }

    private static GraphicsPath RoundedTopRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddLine(rect.Right, rect.Bottom, rect.Left, rect.Bottom);
        path.CloseFigure();
        return path;
    }
}
