#nullable enable
using System.Drawing.Drawing2D;

namespace Tiedragon.ToolEditor;

/// <summary>
/// Reusable API for editor toolbars, icon buttons, balloon tips, and colors.
/// </summary>
public static class ToolEditorApi
{
    /// <summary>
    /// Creates the standard editor toolbar.
    /// </summary>
    public static ToolStrip CreateToolbar(ToolEditorPalette? palette = null)
    {
        var colors = palette ?? ToolEditorPalette.Default;
        return new ToolStrip
        {
            GripStyle = ToolStripGripStyle.Hidden,
            BackColor = colors.ToolbarBack,
            ForeColor = colors.Text,
            Padding = new Padding(5, 3, 5, 3),
            Dock = DockStyle.Top,
            ImageScalingSize = new Size(20, 20),
            RenderMode = ToolStripRenderMode.System
        };
    }

    /// <summary>
    /// Creates a standard editor toolbar button with icon, text, tooltip, and click action.
    /// </summary>
    public static ToolStripButton CreateButton(
        string text,
        ToolEditorIcon icon,
        EventHandler click,
        string? tooltip = null,
        ToolEditorPalette? palette = null)
    {
        var colors = palette ?? ToolEditorPalette.Default;
        var button = new ToolStripButton(text)
        {
            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            AutoSize = true,
            Image = CreateIcon(icon, palette),
            ImageTransparentColor = Color.Transparent,
            Padding = new Padding(2, 1, 2, 1),
            ToolTipText = string.IsNullOrWhiteSpace(tooltip) ? text : tooltip,
            BackColor = colors.ToolbarBack,
            ForeColor = colors.Text,
            Tag = icon
        };

        button.Click += click;
        return button;
    }

    /// <summary>
    /// Creates a reusable tooltip. Use balloon mode for short help/tip messages.
    /// </summary>
    public static ToolTip CreateTipBalloon(string? title = null, ToolTipIcon icon = ToolTipIcon.Info)
    {
        var tip = new ToolTip
        {
            IsBalloon = true,
            ShowAlways = true,
            AutomaticDelay = 350,
            AutoPopDelay = 7000,
            InitialDelay = 350,
            ReshowDelay = 100
        };

        if (!string.IsNullOrWhiteSpace(title))
        {
            tip.ToolTipTitle = title;
            tip.ToolTipIcon = icon;
        }

        return tip;
    }

    /// <summary>
    /// Creates a bitmap icon for an editor toolbar action.
    /// </summary>
    public static Bitmap CreateIcon(ToolEditorIcon icon, ToolEditorPalette? palette = null)
    {
        var colors = palette ?? ToolEditorPalette.Default;
        var bmp = new Bitmap(20, 20);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        switch (icon)
        {
            case ToolEditorIcon.New: DrawNewIcon(g, colors); break;
            case ToolEditorIcon.Wizard: DrawWizardIcon(g, colors); break;
            case ToolEditorIcon.Open: DrawOpenIcon(g, colors); break;
            case ToolEditorIcon.Save: DrawSaveIcon(g, colors); break;
            case ToolEditorIcon.Undo: DrawUndoIcon(g, colors); break;
            case ToolEditorIcon.Redo: DrawRedoIcon(g, colors); break;
            case ToolEditorIcon.Find: DrawFindIcon(g, colors); break;
            case ToolEditorIcon.Validate: DrawValidateIcon(g, colors); break;
            case ToolEditorIcon.Test: DrawTestIcon(g, colors); break;
            case ToolEditorIcon.Solver: DrawSolverIcon(g, colors); break;
            case ToolEditorIcon.Repair: DrawRepairIcon(g, colors); break;
        }

        return bmp;
    }

    private static void DrawWizardIcon(Graphics g, ToolEditorPalette colors)
    {
        using var accent = new Pen(colors.Accent, 1.8f);
        using var dark = new Pen(colors.Text, 1.8f);
        using var fill = new SolidBrush(colors.SoftBlue);
        g.FillEllipse(fill, 2, 2, 16, 16);
        g.DrawEllipse(accent, 2, 2, 16, 16);
        g.DrawLine(dark, 6, 10, 14, 10);
        g.DrawLine(dark, 11, 7, 14, 10);
        g.DrawLine(dark, 11, 13, 14, 10);
        g.DrawLine(accent, 10, 5, 10, 15);
    }

    private static void DrawNewIcon(Graphics g, ToolEditorPalette colors)
    {
        using var pen = new Pen(colors.Text, 1.7f);
        using var plusPen = new Pen(colors.Accent, 1.9f);
        g.DrawRectangle(pen, 5, 3, 9, 13);
        g.DrawLine(plusPen, 4, 10, 10, 10);
        g.DrawLine(plusPen, 7, 7, 7, 13);
    }

    private static void DrawOpenIcon(Graphics g, ToolEditorPalette colors)
    {
        using var outline = new Pen(colors.Text, 1.7f);
        using var fill = new SolidBrush(colors.Folder);
        using var accent = new SolidBrush(colors.FolderAccent);
        using var back = CreateFolderPath(3, 5, 14, 10);
        using var front = CreateFolderPath(3, 7, 14, 8);
        g.FillPath(accent, back);
        g.FillPath(fill, front);
        g.DrawPath(outline, front);
    }

    private static void DrawSaveIcon(Graphics g, ToolEditorPalette colors)
    {
        using var pen = new Pen(colors.Text, 1.7f);
        using var accent = new Pen(colors.Accent, 1.7f);
        g.DrawRectangle(pen, 4, 3, 12, 14);
        g.DrawLine(accent, 7, 3, 7, 8);
        g.DrawLine(accent, 7, 8, 13, 8);
        g.DrawLine(pen, 7, 14, 13, 14);
    }

    private static void DrawUndoIcon(Graphics g, ToolEditorPalette colors)
    {
        using var pen = new Pen(colors.Text, 1.9f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        using var accent = new Pen(colors.Accent, 2.1f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        g.DrawArc(pen, 5, 5, 11, 10, 205, 260);
        g.DrawLines(accent, new[] { new Point(6, 5), new Point(3, 8), new Point(7, 10) });
    }

    private static void DrawRedoIcon(Graphics g, ToolEditorPalette colors)
    {
        using var pen = new Pen(colors.Text, 1.9f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        using var accent = new Pen(colors.Accent, 2.1f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        g.DrawArc(pen, 4, 5, 11, 10, 75, 260);
        g.DrawLines(accent, new[] { new Point(14, 5), new Point(17, 8), new Point(13, 10) });
    }

    private static void DrawFindIcon(Graphics g, ToolEditorPalette colors)
    {
        using var pen = new Pen(colors.Text, 1.8f);
        g.DrawEllipse(pen, 4, 4, 8, 8);
        g.DrawLine(pen, 11, 11, 16, 16);
    }

    private static void DrawValidateIcon(Graphics g, ToolEditorPalette colors)
    {
        using var circlePen = new Pen(colors.Success, 1.8f);
        using var checkPen = new Pen(colors.Success, 2f);
        g.DrawEllipse(circlePen, 3, 3, 14, 14);
        g.DrawLines(checkPen, new[] { new Point(6, 10), new Point(9, 13), new Point(14, 7) });
    }

    private static void DrawTestIcon(Graphics g, ToolEditorPalette colors)
    {
        using var pen = new Pen(colors.Text, 1.7f);
        using var accent = new SolidBrush(colors.Accent);
        using var box = CreateRoundedRectanglePath(new Rectangle(4, 3, 12, 14), 3);
        g.DrawPath(pen, box);
        g.FillPolygon(accent, new[] { new Point(8, 7), new Point(13, 10), new Point(8, 13) });
    }

    private static void DrawSolverIcon(Graphics g, ToolEditorPalette colors)
    {
        using var pen = new Pen(colors.Text, 1.6f);
        using var accent = new Pen(colors.Accent, 1.9f);
        using var fill = new SolidBrush(colors.SoftBlue);
        g.FillRectangle(fill, 3, 3, 14, 14);
        g.DrawRectangle(pen, 3, 3, 14, 14);
        g.DrawLine(accent, 6, 7, 10, 7);
        g.DrawLine(accent, 6, 10, 14, 10);
        g.DrawLine(accent, 6, 13, 12, 13);
        using var font = new Font("Cambria Math", 7, FontStyle.Bold);
        using var brush = new SolidBrush(Color.DarkBlue);
        g.DrawString("Σ", font, brush, 10, 2);
    }

    private static void DrawRepairIcon(Graphics g, ToolEditorPalette colors)
    {
        using var pen = new Pen(colors.Warning, 1.8f);
        using var dark = new Pen(colors.Text, 1.7f);
        using var warningBrush = new SolidBrush(colors.Warning);
        g.DrawArc(pen, 4, 4, 10, 10, 35, 280);
        g.FillPolygon(warningBrush, new[] { new Point(14, 4), new Point(17, 5), new Point(15, 8) });
        g.DrawLine(dark, 11, 12, 16, 17);
    }

    private static GraphicsPath CreateRoundedRectanglePath(Rectangle rectangle, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static GraphicsPath CreateFolderPath(int x, int y, int width, int height)
    {
        var path = new GraphicsPath();
        path.AddLines(new[]
        {
            new Point(x, y + 2),
            new Point(x + 5, y + 2),
            new Point(x + 7, y),
            new Point(x + width, y),
            new Point(x + width, y + height),
            new Point(x, y + height)
        });
        path.CloseFigure();
        return path;
    }
}
