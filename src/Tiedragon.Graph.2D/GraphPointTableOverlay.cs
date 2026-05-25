#nullable enable

// Copyright (c) 1995-2026 Edward Tie / Tiedragon. All rights reserved.
// Builds the draggable point-table overlay used by graph surfaces.
using System.Drawing.Drawing2D;

namespace Tiedragon.Graph.G2D;

/// <summary>
/// Parts returned by the graph point-table overlay factory.
/// </summary>
public readonly record struct GraphPointTableOverlayParts(Panel Overlay, Panel TitleBar, DataGridView Table);

/// <summary>
/// Describes one column in a graph point-table overlay.
/// </summary>
public readonly record struct GraphPointTableColumn(string Name, string Header, int Width);

/// <summary>
/// Shared factory/API for graph point table overlays.
/// </summary>
public static class GraphPointTableOverlay
{
    /// <summary>
    /// Creates a draggable point-table overlay with a title bar and close target.
    /// </summary>
    public static GraphPointTableOverlayParts Create(
        GraphOverlayButtonDensity density,
        string title,
        Func<string> detailProvider,
        Action close,
        MouseEventHandler dragMouseDown,
        MouseEventHandler dragMouseMove,
        MouseEventHandler dragMouseUp,
        IReadOnlyList<GraphPointTableColumn>? columns = null,
        int? tableWidthOverride = null,
        int? tableHeightOverride = null,
        Func<string>? titleProvider = null)
    {
        var compact = density == GraphOverlayButtonDensity.Compact;
        var tableWidth = tableWidthOverride ?? (compact ? 132 : 188);
        var tableHeight = tableHeightOverride ?? (compact ? 64 : 99);
        var padding = compact ? 5 : 6;
        var titleHeight = compact ? 18 : 21;

        var table = CreateTable(density, columns);
        var overlay = new GraphPointOverlayPanel
        {
            Width = tableWidth + padding * 2,
            Height = titleHeight + tableHeight + padding * 2,
            BackColor = GraphOverlayStyle.PanelFill(translucent: false),
            Padding = new Padding(padding)
        };
        overlay.Paint += PaintOverlay;
        overlay.MouseDown += dragMouseDown;
        overlay.MouseMove += dragMouseMove;
        overlay.MouseUp += dragMouseUp;

        var titleBar = new Panel
        {
            Location = new Point(padding, padding),
            Size = new Size(tableWidth, titleHeight),
            BackColor = GraphOverlayStyle.TitleFill,
            Cursor = Cursors.Hand
        };
        titleBar.Paint += (_, e) => DrawTitleBar(e.Graphics, titleBar.ClientRectangle, titleProvider?.Invoke() ?? title, detailProvider(), density);
        titleBar.MouseDown += (_, e) =>
        {
            if (GetCloseRect(titleBar.ClientRectangle, density).Contains(e.Location))
            {
                close();
                return;
            }

            dragMouseDown(titleBar, e);
        };
        titleBar.MouseMove += dragMouseMove;
        titleBar.MouseUp += dragMouseUp;

        table.Dock = DockStyle.None;
        table.Location = new Point(padding, padding + titleHeight);
        table.Size = new Size(tableWidth, tableHeight);
        table.MouseDown += dragMouseDown;
        table.MouseMove += dragMouseMove;
        table.MouseUp += dragMouseUp;

        overlay.Controls.Add(titleBar);
        overlay.Controls.Add(table);
        return new GraphPointTableOverlayParts(overlay, titleBar, table);
    }

    private static DataGridView CreateTable(GraphOverlayButtonDensity density, IReadOnlyList<GraphPointTableColumn>? columns)
    {
        var compact = density == GraphOverlayButtonDensity.Compact;
        var table = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = compact ? 18 : 21,
            ScrollBars = ScrollBars.Vertical,
            GridColor = GraphOverlayStyle.TableGrid,
            Font = new Font("Consolas", compact ? 7.5f : 8f),
            RowTemplate = { Height = compact ? 17 : 20 }
        };

        table.EnableHeadersVisualStyles = false;
        table.BackgroundColor = GraphOverlayStyle.TableBack;
        table.DefaultCellStyle.BackColor = GraphOverlayStyle.TableBack;
        table.DefaultCellStyle.ForeColor = GraphOverlayStyle.TableText;
        table.ColumnHeadersDefaultCellStyle.BackColor = GraphOverlayStyle.TitleFill;
        table.ColumnHeadersDefaultCellStyle.ForeColor = GraphOverlayStyle.TitleText;
        table.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", compact ? 7.5f : 8f, FontStyle.Bold);
        table.ColumnHeadersDefaultCellStyle.Padding = new Padding(compact ? 1 : 2, 0, compact ? 1 : 2, 0);
        table.DefaultCellStyle.Padding = new Padding(compact ? 1 : 2, 0, compact ? 1 : 2, 0);
        table.DefaultCellStyle.SelectionBackColor = GraphOverlayStyle.TableSelectionBack;
        table.DefaultCellStyle.SelectionForeColor = GraphOverlayStyle.TableSelectionText;
        if (compact)
        {
            table.AlternatingRowsDefaultCellStyle.BackColor = GraphOverlayStyle.TableAlternateBack;
            table.AlternatingRowsDefaultCellStyle.ForeColor = GraphOverlayStyle.TableText;
        }

        var tableColumns = columns is { Count: > 0 }
            ? columns
            : new[]
            {
                new GraphPointTableColumn("x", "x", compact ? 50 : 74),
                new GraphPointTableColumn("y", "y", compact ? 62 : 96)
            };

        foreach (var column in tableColumns)
        {
            table.Columns.Add(column.Name, column.Header);
            table.Columns[^1].Width = column.Width;
        }

        return table;
    }

    private static void DrawTitleBar(Graphics graphics, Rectangle rect, string title, string detail, GraphOverlayButtonDensity density)
    {
        var compact = density == GraphOverlayButtonDensity.Compact;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var titleBrush = new SolidBrush(GraphOverlayStyle.TitleText);
        using var detailBrush = new SolidBrush(GraphOverlayStyle.DetailText);
        using var font = new Font("Segoe UI", compact ? 7.5f : 8f, FontStyle.Bold);
        using var detailFont = new Font("Segoe UI", 7f);
        using var format = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        var left = rect.Left + (compact ? 4 : 5);
        var right = rect.Right - GetCloseRect(rect, density).Width - 2;
        var titleWidth = Math.Min(graphics.MeasureString(title, font).Width + 2f, Math.Max(0f, right - left));
        graphics.DrawString(title, font, titleBrush, new RectangleF(left, rect.Top, titleWidth, rect.Height), format);
        if (!string.IsNullOrWhiteSpace(detail))
        {
            var detailLeft = left + titleWidth + (compact ? 5f : 7f);
            var detailWidth = right - detailLeft;
            if (detailWidth > 10f)
                graphics.DrawString(detail, detailFont, detailBrush, new RectangleF(detailLeft, rect.Top, detailWidth, rect.Height), format);
        }

        var closeRect = GetCloseRect(rect, density);
        using var closePen = new Pen(GraphOverlayStyle.TitleText, compact ? 1.7f : 1.8f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        var cx = closeRect.Left + closeRect.Width / 2f;
        var cy = closeRect.Top + closeRect.Height / 2f;
        var half = compact ? 3.8f : 4.2f;
        graphics.DrawLine(closePen, cx - half, cy - half, cx + half, cy + half);
        graphics.DrawLine(closePen, cx + half, cy - half, cx - half, cy + half);
    }

    private static Rectangle GetCloseRect(Rectangle rect, GraphOverlayButtonDensity density)
    {
        var width = density == GraphOverlayButtonDensity.Compact ? 18 : 20;
        return new Rectangle(rect.Right - width, rect.Top, width, rect.Height);
    }

    private static void PaintOverlay(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel panel)
            return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
        GraphOverlayStyle.PaintPanelChrome(e.Graphics, rect, 9f, translucent: false);
    }

    private sealed class GraphPointOverlayPanel : Panel
    {
        public GraphPointOverlayPanel()
        {
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            using var path = GraphOverlayStyle.RoundedRect(
                new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)),
                9);
            Region = new Region(path);
        }
    }
}

