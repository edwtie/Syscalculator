#nullable enable
using System.Globalization;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using NodSystem.Core;

namespace Syscalculator.UI.WinForms;

public sealed class GraphPreviewForm : Form
{
    private readonly Func<string> _getNodText;
    private readonly LanguageCatalog? _language;
    private readonly NumericUpDown _xMin;
    private readonly NumericUpDown _xMax;
    private readonly NumericUpDown _step;
    private readonly TableLayoutPanel _root;
    private readonly Panel _canvas;
    private readonly Panel _pointsPanel;
    private readonly Panel _pointsTitleBar;
    private readonly Panel _statusPanel;
    private readonly DataGridView _pointsGrid;
    private readonly Label _status;
    private readonly Button _toggleTableButton;
    private readonly List<PointF> _graphPoints = new();
    private readonly List<PointF> _stepPoints = new();
    private string _disabledMessage = "";
    private string _pointerText = "";
    private NodDocument? _currentDocument;
    private int _lastSkipped;
    private float _viewMinX;
    private float _viewMaxX;
    private float _viewMinY;
    private float _viewMaxY;
    private bool _hasView;
    private bool _panning;
    private bool _draggingPointsPanel;
    private Point _panStart;
    private Point _pointsPanelDragStart;
    private Point _pointsPanelStartLocation;
    private float _panStartMinX;
    private float _panStartMaxX;
    private float _panStartMinY;
    private float _panStartMaxY;

    public GraphPreviewForm(Func<string> getNodText)
        : this(getNodText, null)
    {
    }

    internal GraphPreviewForm(Func<string> getNodText, LanguageCatalog? language)
    {
        _getNodText = getNodText;
        _language = language;

        Text = T("editor.graph.title", "Graph Preview");
        Width = 920;
        Height = 560;
        MinimumSize = new Size(700, 420);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = true;
        ShowInTaskbar = false;

        _root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(10),
            BackColor = Color.FromArgb(250, 250, 250)
        };
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var controls = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 11,
            RowCount = 1,
            Padding = new Padding(0, 5, 0, 0)
        };
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1));
        controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        controls.Controls.Add(new Label { Text = "X min", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        _xMin = MakeNumberBox(-3, -100000, 100000, 1);
        _xMin.ValueChanged += (_, _) => Generate();
        controls.Controls.Add(_xMin, 1, 0);
        controls.Controls.Add(new Label { Text = "X max", AutoSize = true, Anchor = AnchorStyles.Left }, 2, 0);
        _xMax = MakeNumberBox(3, -100000, 100000, 1);
        _xMax.ValueChanged += (_, _) => Generate();
        controls.Controls.Add(_xMax, 3, 0);
        controls.Controls.Add(new Label { Text = T("editor.graph.step", "Step"), AutoSize = true, Anchor = AnchorStyles.Left }, 4, 0);
        _step = MakeNumberBox(1, 0.0001m, 100000, 1);
        _step.ValueChanged += (_, _) => Generate();
        controls.Controls.Add(_step, 5, 0);

        var copy = new GraphToolbarIconButton(GraphToolbarIcon.Copy, T("editor.graph.copy_points", "Copy points")) { Dock = DockStyle.Fill, Margin = new Padding(4, 2, 4, 2) };
        copy.Click += (_, _) =>
        {
            var text = BuildPointsText();
            if (!string.IsNullOrWhiteSpace(text))
                Clipboard.SetText(text);
        };
        controls.Controls.Add(copy, 6, 0);

        _toggleTableButton = new GraphToolbarIconButton(GraphToolbarIcon.TableHidden, T("editor.graph.hide_table", "Hide table")) { Dock = DockStyle.Fill, Margin = new Padding(4, 2, 4, 2) };
        _toggleTableButton.Click += (_, _) => TogglePointTable();
        controls.Controls.Add(_toggleTableButton, 7, 0);

        _root.Controls.Add(controls, 0, 0);

        var graphHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(240, 244, 249),
            Padding = new Padding(0)
        };

        _canvas = new GraphCanvasPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        _canvas.Paint += Canvas_Paint;
        _canvas.MouseWheel += Canvas_MouseWheel;
        _canvas.MouseDown += Canvas_MouseDown;
        _canvas.MouseMove += Canvas_MouseMove;
        _canvas.MouseUp += Canvas_MouseUp;
        _canvas.MouseEnter += (_, _) => _canvas.Focus();
        graphHost.Controls.Add(_canvas);

        _pointsGrid = BuildPointsGrid();
        const int pointsTableWidth = 188;
        const int pointsTableHeight = 99;
        const int pointsPadding = 6;
        const int pointsTitleHeight = 21;
        _pointsPanel = new Panel
        {
            Width = pointsTableWidth + pointsPadding * 2,
            Height = pointsTitleHeight + 99 + pointsPadding * 2,
            BackColor = Color.White,
            Padding = new Padding(pointsPadding)
        };
        _pointsPanel.Paint += RoundedOverlayPanel_Paint;
        _pointsPanel.MouseDown += PointsPanel_MouseDown;
        _pointsPanel.MouseMove += PointsPanel_MouseMove;
        _pointsPanel.MouseUp += PointsPanel_MouseUp;

        _pointsTitleBar = new Panel
        {
            Location = new Point(pointsPadding, pointsPadding),
            Size = new Size(pointsTableWidth, pointsTitleHeight),
            BackColor = Color.FromArgb(239, 246, 255),
            Cursor = Cursors.SizeAll
        };
        _pointsTitleBar.Paint += (_, e) => DrawPointTitleBar(e.Graphics, _pointsTitleBar.ClientRectangle, T("editor.graph.points", "Points"), _pointerText);
        _pointsTitleBar.MouseDown += (_, e) =>
        {
            if (GetPointTitleCloseRect(_pointsTitleBar.ClientRectangle).Contains(e.Location))
            {
                SetPointTableVisible(false);
                return;
            }

            PointsPanel_MouseDown(_pointsTitleBar, e);
        };
        _pointsTitleBar.MouseMove += PointsPanel_MouseMove;
        _pointsTitleBar.MouseUp += PointsPanel_MouseUp;

        _pointsGrid.Dock = DockStyle.None;
        _pointsGrid.Location = new Point(pointsPadding, pointsPadding + pointsTitleHeight);
        _pointsGrid.Size = new Size(pointsTableWidth, pointsTableHeight);
        _pointsGrid.MouseDown += PointsPanel_MouseDown;
        _pointsGrid.MouseMove += PointsPanel_MouseMove;
        _pointsGrid.MouseUp += PointsPanel_MouseUp;
        _pointsPanel.Controls.Add(_pointsTitleBar);
        _pointsPanel.Controls.Add(_pointsGrid);
        graphHost.Controls.Add(_pointsPanel);
        _pointsPanel.BringToFront();

        var zoomPanel = new Panel
        {
            Width = 44,
            Height = 128,
            BackColor = Color.White
        };
        zoomPanel.Paint += RoundedOverlayPanel_Paint;
        var resetView = MakeOverlayButton("⟳", T("editor.graph.reset_view", "Reset view"), (_, _) => ResetViewToData(), 18f);
        var zoomInView = MakeOverlayButton("+", T("editor.graph.zoom_in", "Zoom in"), (_, _) => ZoomView(0.75f), 18f);
        var zoomOutView = MakeOverlayButton("−", T("editor.graph.zoom_out", "Zoom out"), (_, _) => ZoomView(1.35f), 20f);
        resetView.Location = new Point(4, 4);
        zoomInView.Location = new Point(4, 45);
        zoomOutView.Location = new Point(4, 86);
        zoomPanel.Controls.Add(resetView);
        zoomPanel.Controls.Add(zoomInView);
        zoomPanel.Controls.Add(zoomOutView);
        graphHost.Controls.Add(zoomPanel);
        zoomPanel.BringToFront();

        _status = new Label
        {
            Dock = DockStyle.Fill,
            Text = "",
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Padding = new Padding(6, 0, 6, 1),
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 7.5f)
        };
        _statusPanel = new Panel
        {
            Width = 52,
            Height = 20,
            BackColor = Color.White
        };
        _statusPanel.Paint += CompactStatusPanel_Paint;
        _statusPanel.Controls.Add(_status);
        graphHost.Controls.Add(_statusPanel);
        _statusPanel.BringToFront();

        void PlaceFloatingPanels()
        {
            zoomPanel.Left = Math.Max(0, graphHost.ClientSize.Width - zoomPanel.Width - 12);
            zoomPanel.Top = 12;
            _statusPanel.Left = 5;
            _statusPanel.Top = Math.Max(5, graphHost.ClientSize.Height - _statusPanel.Height - 7);
            if (_pointsPanel.Left <= 0 && _pointsPanel.Top <= 0)
            {
                _pointsPanel.Left = 12;
                _pointsPanel.Top = 12;
            }
            KeepPointsPanelInBounds();
            _pointsPanel.BringToFront();
            zoomPanel.BringToFront();
            _statusPanel.BringToFront();
            UpdateFloatingStatusVisibility();
        }

        graphHost.Resize += (_, _) =>
        {
            PlaceFloatingPanels();
            _canvas.Invalidate();
        };
        PlaceFloatingPanels();

        _root.Controls.Add(graphHost, 0, 1);

        Controls.Add(_root);
    }

    private string T(string key, string fallback) => _language?.Text(key, fallback) ?? fallback;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Generate();
    }

    private static NumericUpDown MakeNumberBox(decimal value, decimal minimum, decimal maximum, decimal increment)
    {
        return new NumericUpDown
        {
            DecimalPlaces = 4,
            Minimum = minimum,
            Maximum = maximum,
            Increment = increment,
            Value = value,
            Width = 78,
            Margin = new Padding(0, 2, 0, 0)
        };
    }

    private static DataGridView BuildPointsGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            ScrollBars = ScrollBars.Vertical,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 21,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            Font = new Font("Consolas", 8f),
            RowTemplate = { Height = 20 },
            GridColor = Color.FromArgb(225, 232, 242)
        };
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(239, 246, 255);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(15, 63, 143);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(2, 0, 2, 0);
        grid.DefaultCellStyle.Padding = new Padding(2, 0, 2, 0);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(17, 24, 39);
        grid.Columns.Add("x", "x");
        grid.Columns.Add("y", "y");
        grid.Columns[0].Width = 74;
        grid.Columns[1].Width = 96;
        return grid;
    }

    private void TogglePointTable()
    {
        SetPointTableVisible(!_pointsPanel.Visible);
    }

    private void SetPointTableVisible(bool visible)
    {
        _pointsPanel.Visible = visible;
        UpdateFloatingStatusVisibility();
        if (_toggleTableButton is GraphToolbarIconButton iconButton)
        {
            iconButton.Icon = visible ? GraphToolbarIcon.TableHidden : GraphToolbarIcon.TableVisible;
            iconButton.TooltipText = visible ? T("editor.graph.hide_table", "Hide table") : T("editor.graph.show_table", "Show table");
        }
        _canvas.Invalidate();
    }

    private void UpdateFloatingStatusVisibility()
    {
        _statusPanel.Visible = !_pointsPanel.Visible && !string.IsNullOrWhiteSpace(_pointerText);
    }

    private static void DrawPointTitleBar(Graphics graphics, Rectangle rect, string title, string detail = "")
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var titleBrush = new SolidBrush(Color.FromArgb(15, 63, 143));
        using var detailBrush = new SolidBrush(Color.FromArgb(71, 85, 105));
        using var font = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var detailFont = new Font("Segoe UI", 7f);
        using var format = new StringFormat { LineAlignment = StringAlignment.Center };
        graphics.DrawString(title, font, titleBrush, new Rectangle(rect.Left + 5, rect.Top, rect.Width - 24, rect.Height), format);
        if (!string.IsNullOrWhiteSpace(detail))
            graphics.DrawString(detail, detailFont, detailBrush, new Rectangle(rect.Left + 62, rect.Top, rect.Width - 86, rect.Height), format);

        var closeRect = GetPointTitleCloseRect(rect);
        using var closePen = new Pen(Color.FromArgb(15, 63, 143), 1.8f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        var cx = closeRect.Left + closeRect.Width / 2f;
        var cy = closeRect.Top + closeRect.Height / 2f;
        const float half = 4.2f;
        graphics.DrawLine(closePen, cx - half, cy - half, cx + half, cy + half);
        graphics.DrawLine(closePen, cx + half, cy - half, cx - half, cy + half);
    }

    private static Rectangle GetPointTitleCloseRect(Rectangle rect)
    {
        return new Rectangle(rect.Right - 20, rect.Top, 20, rect.Height);
    }

    private void PointsPanel_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        _draggingPointsPanel = true;
        _pointsPanelDragStart = _pointsPanel.Parent!.PointToClient(((Control)sender!).PointToScreen(e.Location));
        _pointsPanelStartLocation = _pointsPanel.Location;
        _pointsPanel.Cursor = Cursors.SizeAll;
        _pointsGrid.Cursor = Cursors.SizeAll;
        _pointsPanel.BringToFront();
    }

    private void PointsPanel_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_draggingPointsPanel)
            return;

        var current = _pointsPanel.Parent!.PointToClient(((Control)sender!).PointToScreen(e.Location));
        _pointsPanel.Left = _pointsPanelStartLocation.X + current.X - _pointsPanelDragStart.X;
        _pointsPanel.Top = _pointsPanelStartLocation.Y + current.Y - _pointsPanelDragStart.Y;
        KeepPointsPanelInBounds();
    }

    private void PointsPanel_MouseUp(object? sender, MouseEventArgs e)
    {
        _draggingPointsPanel = false;
        _pointsPanel.Cursor = Cursors.Default;
        _pointsGrid.Cursor = Cursors.Default;
    }

    private void KeepPointsPanelInBounds()
    {
        if (_pointsPanel.Parent is null)
            return;

        var parent = _pointsPanel.Parent.ClientSize;
        _pointsPanel.Left = Math.Clamp(_pointsPanel.Left, 8, Math.Max(8, parent.Width - _pointsPanel.Width - 8));
        _pointsPanel.Top = Math.Clamp(_pointsPanel.Top, 8, Math.Max(8, parent.Height - _pointsPanel.Height - 8));
    }

    private static Control MakeOverlayButton(string text, string tooltip, EventHandler click, float fontSize)
    {
        var button = new RoundOverlayButton
        {
            Text = text,
            Width = 36,
            Height = 36,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(31, 41, 55),
            Font = new Font("Segoe UI", fontSize, FontStyle.Bold),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        button.Click += click;

        var tip = new ToolTip();
        tip.SetToolTip(button, tooltip);
        return button;
    }

    private sealed class RoundOverlayButton : Control
    {
        private bool _hover;
        private bool _down;

        public RoundOverlayButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
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
            _down = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _down = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            var fireClick = _down && ClientRectangle.Contains(mevent.Location);
            _down = false;
            Invalidate();
            if (fireClick)
                OnClick(EventArgs.Empty);
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(1, 1, Width - 3, Height - 3);
            using var path = RoundedRect(rect, 10);
            var fillColor = _down
                ? Color.FromArgb(219, 234, 254)
                : _hover ? Color.FromArgb(239, 246, 255) : Color.White;
            using var fill = new SolidBrush(fillColor);
            using var border = new Pen(Color.FromArgb(203, 216, 234), 1);
            pevent.Graphics.FillPath(fill, path);
            pevent.Graphics.DrawPath(border, path);

            if (Text is "+" or "−" or "⟳")
            {
                using var symbolPen = new Pen(ForeColor, 2.6f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                };
                var centerX = rect.Left + rect.Width / 2f;
                var centerY = rect.Top + rect.Height / 2f;
                if (Text == "⟳")
                {
                    var arcRect = new RectangleF(centerX - 8.5f, centerY - 8.5f, 17f, 17f);
                    pevent.Graphics.DrawArc(symbolPen, arcRect, 35, 285);
                    using var arrowBrush = new SolidBrush(ForeColor);
                    var arrow = new[]
                    {
                        new PointF(centerX - 2.2f, centerY - 10.5f),
                        new PointF(centerX + 5.6f, centerY - 9.1f),
                        new PointF(centerX + 1.2f, centerY - 2.7f)
                    };
                    pevent.Graphics.FillPolygon(arrowBrush, arrow);
                    return;
                }

                var half = 7f;
                pevent.Graphics.DrawLine(symbolPen, centerX - half, centerY, centerX + half, centerY);
                if (Text == "+")
                    pevent.Graphics.DrawLine(symbolPen, centerX, centerY - half, centerX, centerY + half);
                return;
            }

            var textRect = rect;
            if (Text == "⟳")
                textRect.Offset(0, -2);

            using var textBrush = new SolidBrush(ForeColor);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.None,
                FormatFlags = StringFormatFlags.NoClip
            };
            pevent.Graphics.DrawString(Text, Font, textBrush, textRect, format);
        }
    }

    private sealed class GraphCanvasPanel : Panel
    {
        public GraphCanvasPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
        }
    }

    private static void RoundedOverlayPanel_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel panel)
            return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
        using var path = RoundedRect(rect, 12);
        using var fill = new SolidBrush(Color.White);
        using var border = new Pen(Color.FromArgb(203, 216, 234), 1);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
    }

    private static void CompactStatusPanel_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel panel)
            return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
        using var path = RoundedRect(rect, 9);
        using var fill = new SolidBrush(Color.FromArgb(253, 254, 255));
        using var border = new Pen(Color.FromArgb(225, 234, 247), 1);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
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

    private void Generate()
    {
        _graphPoints.Clear();
        _stepPoints.Clear();
        _pointsGrid.Rows.Clear();

        try
        {
            var min = (double)_xMin.Value;
            var max = (double)_xMax.Value;
            var step = (double)_step.Value;

            if (min > max)
            {
                _status.Text = "X min moet kleiner of gelijk zijn aan X max.";
                _canvas.Invalidate();
                return;
            }

            _currentDocument = NodParser.Parse(_getNodText());
            if (!IsGraphCompatible(_currentDocument, out var disabledReason))
            {
                _disabledMessage = disabledReason;
                _graphPoints.Clear();
                _stepPoints.Clear();
                FillPointsGrid([]);
                SetPointTableVisible(false);
                _toggleTableButton.Enabled = false;
                _hasView = false;
                _status.Text = T("editor.graph.disabled", "Graph Preview disabled");
                _canvas.Invalidate();
                return;
            }

            _disabledMessage = "";
            _toggleTableButton.Enabled = true;
            var stepRows = new List<PointF>();
            var skipped = 0;
            var total = 0;
            const int maxSamples = 5000;

            for (var x = min; x <= max + (step / 1000.0) && total < maxSamples; x += step)
            {
                total++;
                var input = x.ToString("0.############", CultureInfo.InvariantCulture);

                try
                {
                    var result = NodEngine.ConvertForward(_currentDocument, input);
                    if (TryGetNumber(result, out var y))
                    {
                        _graphPoints.Add(new PointF((float)x, (float)y));
                        _stepPoints.Add(new PointF((float)x, (float)y));
                        stepRows.Add(new PointF((float)x, (float)y));
                    }
                    else
                    {
                        skipped++;
                    }
                }
                catch
                {
                    skipped++;
                }
            }

            FillPointsGrid(stepRows);
            _lastSkipped = skipped;
            ResetViewToData(invalidate: false);
            _status.Text = _graphPoints.Count == 0
                ? string.Format(T("editor.graph.no_numeric_points", "No numeric points. Skipped: {0}."), skipped)
                : T("editor.graph.position_empty", "Position: (-, -)");
            if (_graphPoints.Count > 0)
                UpdatePointerStatus(null);
            _canvas.Invalidate();
        }
        catch (Exception ex)
        {
            _status.Text = string.Format(T("editor.graph.error", "Graph error: {0}"), ex.Message);
            _canvas.Invalidate();
        }
    }

    private void ResetViewToData(bool invalidate = true)
    {
        if (_graphPoints.Count == 0)
        {
            _hasView = false;
            if (invalidate) _canvas.Invalidate();
            return;
        }

        var minX = (float)_xMin.Value;
        var maxX = (float)_xMax.Value;
        var minY = _graphPoints.Min(p => p.Y);
        var maxY = _graphPoints.Max(p => p.Y);

        if (Math.Abs(maxX - minX) < 0.000001f) { minX -= 1; maxX += 1; }
        if (Math.Abs(maxY - minY) < 0.000001f) { minY -= 1; maxY += 1; }
        GraphPlotRenderer.NormalizeRange(ref minX, ref maxX, ref minY, ref maxY, padX: false);

        _viewMinX = minX;
        _viewMaxX = maxX;
        _viewMinY = minY;
        _viewMaxY = maxY;
        _hasView = true;
        UpdatePointerStatus(null);

        if (invalidate)
            _canvas.Invalidate();
    }

    private void ZoomView(float factor)
    {
        if (!_hasView)
            return;

        ZoomViewAt(factor, new PointF(_canvas.ClientSize.Width / 2f, _canvas.ClientSize.Height / 2f));
    }

    private void Canvas_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (_hasView)
            ZoomViewAt(e.Delta > 0 ? 0.85f : 1.18f, e.Location);
    }

    private void ZoomViewAt(float factor, PointF screenPoint)
    {
        var plot = GetPlotRectangle();
        if (plot.Width <= 0 || plot.Height <= 0)
            return;

        var anchor = ScreenToGraph(screenPoint, plot);
        var newWidth = (_viewMaxX - _viewMinX) * factor;
        var newHeight = (_viewMaxY - _viewMinY) * factor;
        if (newWidth < 0.0001f || newHeight < 0.0001f)
            return;

        var xRatio = (anchor.X - _viewMinX) / (_viewMaxX - _viewMinX);
        var yRatio = (anchor.Y - _viewMinY) / (_viewMaxY - _viewMinY);
        _viewMinX = anchor.X - newWidth * xRatio;
        _viewMaxX = _viewMinX + newWidth;
        _viewMinY = anchor.Y - newHeight * yRatio;
        _viewMaxY = _viewMinY + newHeight;
        ResampleVisibleView();
        UpdatePointerStatus(screenPoint);
        _canvas.Invalidate();
    }

    private void Canvas_MouseDown(object? sender, MouseEventArgs e)
    {
        if (!_hasView || e.Button != MouseButtons.Left)
            return;

        _panning = true;
        _panStart = e.Location;
        _panStartMinX = _viewMinX;
        _panStartMaxX = _viewMaxX;
        _panStartMinY = _viewMinY;
        _panStartMaxY = _viewMaxY;
        _canvas.Cursor = Cursors.Hand;
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_panning)
        {
            UpdatePointerStatus(e.Location);
            return;
        }

        var plot = GetPlotRectangle();
        if (plot.Width <= 0 || plot.Height <= 0)
            return;

        var dx = e.X - _panStart.X;
        var dy = e.Y - _panStart.Y;
        var graphDx = dx / (float)plot.Width * (_panStartMaxX - _panStartMinX);
        var graphDy = dy / (float)plot.Height * (_panStartMaxY - _panStartMinY);

        _viewMinX = _panStartMinX - graphDx;
        _viewMaxX = _panStartMaxX - graphDx;
        _viewMinY = _panStartMinY + graphDy;
        _viewMaxY = _panStartMaxY + graphDy;
        ResampleVisibleView();
        UpdatePointerStatus(e.Location);
        _canvas.Invalidate();
    }

    private void Canvas_MouseUp(object? sender, MouseEventArgs e)
    {
        _panning = false;
        _canvas.Cursor = Cursors.Default;
    }

    private void ResampleVisibleView()
    {
        if (_currentDocument is null || !_hasView)
            return;

        var visibleMin = Math.Min(_viewMinX, _viewMaxX);
        var visibleMax = Math.Max(_viewMinX, _viewMaxX);
        var width = visibleMax - visibleMin;
        if (width <= 0)
            return;

        var desiredSamples = Math.Clamp(_canvas.ClientSize.Width / 6, 80, 500);
        var step = width / desiredSamples;
        var points = new List<PointF>();
        var stepPoints = new List<PointF>();
        var skipped = 0;

        for (var i = 0; i <= desiredSamples; i++)
        {
            var x = visibleMin + step * i;
            var input = x.ToString("0.############", CultureInfo.InvariantCulture);

            try
            {
                var result = NodEngine.ConvertForward(_currentDocument, input);
                if (TryGetNumber(result, out var y))
                    points.Add(new PointF(x, (float)y));
                else
                    skipped++;
            }
            catch
            {
                skipped++;
            }
        }

        var requestedStep = (float)_step.Value;
        if (requestedStep > 0)
        {
            var firstStep = MathF.Ceiling(visibleMin / requestedStep) * requestedStep;
            for (var x = firstStep; x <= visibleMax + requestedStep / 1000f; x += requestedStep)
            {
                var input = x.ToString("0.############", CultureInfo.InvariantCulture);
                try
                {
                    var result = NodEngine.ConvertForward(_currentDocument, input);
                    if (TryGetNumber(result, out var y))
                        stepPoints.Add(new PointF(x, (float)y));
                    else
                        skipped++;
                }
                catch
                {
                    skipped++;
                }
            }
        }

        _graphPoints.Clear();
        _graphPoints.AddRange(points);
        _stepPoints.Clear();
        _stepPoints.AddRange(stepPoints);
        _lastSkipped = skipped;

        FillPointsGrid(stepPoints);
    }

    private void FillPointsGrid(IEnumerable<PointF> points)
    {
        _pointsGrid.SuspendLayout();
        _pointsGrid.Rows.Clear();
        foreach (var point in points)
        {
            _pointsGrid.Rows.Add(
                point.X.ToString("0.############", CultureInfo.InvariantCulture),
                point.Y.ToString("0.############", CultureInfo.InvariantCulture));
        }
        _pointsGrid.ClearSelection();
        _pointsGrid.ResumeLayout();
    }

    private string BuildPointsText()
    {
        return string.Join(Environment.NewLine, _stepPoints.Select(p =>
            $"{p.X.ToString("0.############", CultureInfo.InvariantCulture)}\t{p.Y.ToString("0.############", CultureInfo.InvariantCulture)}"));
    }

    private static bool TryGetNumber(NodResult result, out double value)
    {
        if (result.NumericValue.HasValue)
        {
            value = (double)result.NumericValue.Value;
            return double.IsFinite(value);
        }

        var text = Regex.Replace(result.Text.Trim(), @"[^\d,\.\-\+Ee]", "");
        if (double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return double.IsFinite(value);

        value = 0;
        return false;
    }

    private bool IsGraphCompatible(NodDocument document, out string reason)
    {
        if (document.ChangeRules.Count > 0)
        {
            reason = T("editor.graph.disabled_chg", "Graph Preview is disabled for chg converters.");
            return false;
        }

        if (document.TranslateRules.Count > 0)
        {
            reason = T("editor.graph.disabled_trans", "Graph Preview is disabled for trans converters.");
            return false;
        }

        if (document.LegacyMathSteps.Count == 0 &&
            document.MathExpressions20.Count == 0 &&
            document.CalculusSteps.Count == 0 &&
            document.Equation is null)
        {
            reason = T("editor.graph.disabled_math", "Graph Preview is only available for numeric math converters.");
            return false;
        }

        reason = "";
        return true;
    }

    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        if (!_hasView)
            ResetViewToData(invalidate: false);

        GraphPlotRenderer.Draw(
            e.Graphics,
            _canvas,
            _graphPoints,
            _stepPoints,
            new GraphPlotView(_viewMinX, _viewMaxX, _viewMinY, _viewMaxY),
            (float)_xMin.Value,
            (float)_xMax.Value,
            (float)_step.Value,
            _disabledMessage,
            "Generate graph",
            GraphPlotDensity.Normal);
    }

    private Rectangle GetPlotRectangle()
    {
        return GraphPlotRenderer.GetPlotRectangle(_canvas);
    }

    private PointF ScreenToGraph(PointF screenPoint, Rectangle plot)
    {
        return GraphPlotRenderer.ScreenToGraph(screenPoint, plot, new GraphPlotView(_viewMinX, _viewMaxX, _viewMinY, _viewMaxY));
    }

    private static void DrawGrid(Graphics g, Rectangle plot)
    {
        using var borderPen = new Pen(Color.FromArgb(203, 216, 234), 1);
        g.DrawRectangle(borderPen, plot);
    }

    private void DrawAxes(Graphics g, Rectangle plot, float minX, float maxX, float minY, float maxY, float requestedMinX, float requestedMaxX, float requestedStep, Func<PointF, PointF> map)
    {
        using var axisPen = new Pen(Color.FromArgb(51, 65, 85), 1.4f);
        using var gridPen = new Pen(Color.FromArgb(219, 231, 247), 1f);
        using var tickPen = new Pen(Color.FromArgb(71, 85, 105), 1f);
        using var labelBrush = new SolidBrush(Color.FromArgb(15, 63, 143));
        using var tickFont = new Font("Segoe UI", 8f);
        var xTicks = IsSameRange(minX, maxX, requestedMinX, requestedMaxX)
            ? BuildTicks(requestedMinX, requestedMaxX, requestedStep, maxTicks: 11)
            : BuildNiceTicks(minX, maxX, maxTicks: 8);
        var yTicks = BuildNiceTicks(minY, maxY, maxTicks: 7);

        foreach (var xValue in xTicks)
        {
            if (xValue < minX || xValue > maxX)
                continue;

            var xPoint = map(new PointF(xValue, minY));
            g.DrawLine(gridPen, xPoint.X, plot.Top, xPoint.X, plot.Bottom);
        }

        foreach (var yValue in yTicks)
        {
            var yPoint = map(new PointF(minX, yValue));
            g.DrawLine(gridPen, plot.Left, yPoint.Y, plot.Right, yPoint.Y);
        }

        var xAxisVisible = minY <= 0 && maxY >= 0;
        var yAxisVisible = minX <= 0 && maxX >= 0;

        if (xAxisVisible)
        {
            var y0 = map(new PointF(minX, 0)).Y;
            g.DrawLine(axisPen, plot.Left, y0, plot.Right, y0);
        }

        if (yAxisVisible)
        {
            var x0 = map(new PointF(0, minY)).X;
            g.DrawLine(axisPen, x0, plot.Top, x0, plot.Bottom);
        }

        var xAxisY = xAxisVisible ? map(new PointF(minX, 0)).Y : float.NaN;
        var yAxisX = yAxisVisible ? map(new PointF(0, minY)).X : float.NaN;

        if (xAxisVisible)
        {
            foreach (var xValue in xTicks)
            {
                if (xValue < minX || xValue > maxX)
                    continue;

                if (Math.Abs(xValue) < 0.000001f && yAxisVisible)
                    continue;

                var xPoint = map(new PointF(xValue, minY));
                g.DrawLine(tickPen, xPoint.X, xAxisY - 5, xPoint.X, xAxisY + 5);
                var text = FormatTick(xValue);
                var size = g.MeasureString(text, tickFont);
                var labelX = Math.Clamp(xPoint.X - size.Width / 2, plot.Left + 2, plot.Right - size.Width - 2);
                var labelY = Math.Clamp(xAxisY + 7, plot.Top + 2, plot.Bottom - size.Height - 2);
                g.DrawString(text, tickFont, labelBrush, labelX, labelY);
            }
        }

        if (yAxisVisible)
        {
            foreach (var yValue in yTicks)
            {
                if (Math.Abs(yValue) < 0.000001f && xAxisVisible)
                    continue;

                var yPoint = map(new PointF(minX, yValue));
                g.DrawLine(tickPen, yAxisX - 5, yPoint.Y, yAxisX + 5, yPoint.Y);
                var text = FormatTick(yValue);
                var size = g.MeasureString(text, tickFont);
                var labelX = Math.Clamp(yAxisX - size.Width - 7, plot.Left + 2, plot.Right - size.Width - 2);
                var labelY = Math.Clamp(yPoint.Y - size.Height / 2, plot.Top + 2, plot.Bottom - size.Height - 2);
                g.DrawString(text, tickFont, labelBrush, labelX, labelY);
            }
        }

        if (xAxisVisible && yAxisVisible)
        {
            var origin = map(new PointF(0, 0));
            var text = "0";
            var size = g.MeasureString(text, tickFont);
            var labelX = Math.Clamp(origin.X - size.Width - 8, plot.Left + 2, plot.Right - size.Width - 2);
            var labelY = Math.Clamp(origin.Y + 6, plot.Top + 2, plot.Bottom - size.Height - 2);
            g.DrawString(text, tickFont, labelBrush, labelX, labelY);
        }

        if (xAxisVisible)
            g.DrawString("x", Font, labelBrush, plot.Right + 4, Math.Clamp(xAxisY - 13, plot.Top + 2, plot.Bottom - 16));
        if (yAxisVisible)
            g.DrawString("y", Font, labelBrush, Math.Clamp(yAxisX - 18, plot.Left + 2, plot.Right - 14), plot.Top + 2);
    }

    private static string FormatTick(float value)
    {
        return Math.Abs(value) < 0.000001f
            ? "0"
            : value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private void UpdatePointerStatus(PointF? screenPoint)
    {
        if (_graphPoints.Count == 0 || !_hasView || screenPoint is null)
        {
            _status.Text = "";
            SetFloatingPointerText("");
            UpdateFloatingStatusVisibility();
            return;
        }

        var plot = GetPlotRectangle();
        if (!plot.Contains(Point.Round(screenPoint.Value)))
        {
            _status.Text = "";
            SetFloatingPointerText("");
            UpdateFloatingStatusVisibility();
            return;
        }

        var point = ScreenToGraph(screenPoint.Value, plot);
        var text = $"({FormatStatusNumber(point.X)}, {FormatStatusNumber(point.Y)})";
        _status.Text = text;
        _statusPanel.Width = Math.Clamp(
            TextRenderer.MeasureText(_status.Text, _status.Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width + _status.Padding.Horizontal + 8,
            46,
            190);
        SetFloatingPointerText(text);
        UpdateFloatingStatusVisibility();
    }

    private void SetFloatingPointerText(string text)
    {
        if (_pointerText == text)
            return;

        _pointerText = text;
        _pointsTitleBar.Invalidate();
    }

    private static string FormatStatusNumber(float value)
    {
        return Math.Abs(value) < 0.0000001f
            ? "0"
            : value.ToString("0.0", CultureInfo.GetCultureInfo("nl-NL"));
    }

    private static bool IsSameRange(float minA, float maxA, float minB, float maxB)
    {
        return Math.Abs(minA - minB) < 0.0001f && Math.Abs(maxA - maxB) < 0.0001f;
    }

    private static void NormalizeRange(ref float minX, ref float maxX, ref float minY, ref float maxY, bool padX = true)
    {
        if (padX)
        {
            var xPad = Math.Max(0.5f, (maxX - minX) * 0.04f);
            minX -= xPad;
            maxX += xPad;
        }

        if (minY >= 0)
            minY = 0;
        else if (maxY <= 0)
            maxY = 0;

        var yPad = Math.Max(0.5f, (maxY - minY) * 0.08f);
        minY -= yPad;
        maxY += yPad;
    }

    private static IReadOnlyList<float> BuildTicks(float min, float max, float step, int maxTicks)
    {
        if (step <= 0 || (max - min) / step > maxTicks)
        {
            return Enumerable.Range(0, maxTicks)
                .Select(i => min + ((max - min) * i / (maxTicks - 1)))
                .ToArray();
        }

        var ticks = new List<float>();
        for (var value = min; value <= max + (step / 1000f); value += step)
            ticks.Add(value);
        return ticks;
    }

    private static IReadOnlyList<float> BuildNiceTicks(float min, float max, int maxTicks)
    {
        if (max <= min)
            return [min];

        var range = NiceNumber(max - min, round: false);
        var spacing = NiceNumber(range / Math.Max(1, maxTicks - 1), round: true);
        var niceMin = MathF.Floor(min / spacing) * spacing;
        var niceMax = MathF.Ceiling(max / spacing) * spacing;

        var ticks = new List<float>();
        for (var value = niceMin; value <= niceMax + spacing / 2; value += spacing)
        {
            if (value >= min - spacing / 2 && value <= max + spacing / 2)
                ticks.Add(value);
        }

        return ticks;
    }

    private static float NiceNumber(float value, bool round)
    {
        var exponent = MathF.Floor(MathF.Log10(MathF.Max(value, 0.000001f)));
        var fraction = value / MathF.Pow(10, exponent);
        float niceFraction;

        if (round)
        {
            niceFraction = fraction < 1.5f ? 1f :
                fraction < 3f ? 2f :
                fraction < 7f ? 5f : 10f;
        }
        else
        {
            niceFraction = fraction <= 1f ? 1f :
                fraction <= 2f ? 2f :
                fraction <= 5f ? 5f : 10f;
        }

        return niceFraction * MathF.Pow(10, exponent);
    }
}
