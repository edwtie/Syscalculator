#nullable enable
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text.RegularExpressions;
using Tiedragon.NodSystem.Core;
using Tiedragon.Graph2D;

namespace Syscalculator.UI.WinForms;

public sealed class GraphPreviewForm : Form
{
    private const float NormalHalfYRange = 5f;

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
        _xMin = MakeNumberBox(-5, -100000, 100000, 1);
        _xMin.ValueChanged += (_, _) => Generate();
        controls.Controls.Add(_xMin, 1, 0);
        controls.Controls.Add(new Label { Text = "X max", AutoSize = true, Anchor = AnchorStyles.Left }, 2, 0);
        _xMax = MakeNumberBox(5, -100000, 100000, 1);
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

        var chrome = GraphSurfaceApi.CreateChrome(
            GraphOverlayButtonDensity.Normal,
            T("editor.graph.points", "Points"),
            () => _pointerText,
            () => SetPointTableVisible(false),
            PointsPanel_MouseDown,
            PointsPanel_MouseMove,
            PointsPanel_MouseUp,
            T("editor.graph.reset_view", "Home / reset view"),
            (_, _) => ResetViewToData(),
            T("editor.graph.zoom_in", "Zoom in"),
            (_, _) => ZoomView(0.75f),
            T("editor.graph.zoom_out", "Zoom out"),
            (_, _) => ZoomView(1.35f));
        _pointsPanel = chrome.PointTablePanel;
        _pointsTitleBar = chrome.PointTableTitleBar;
        _pointsGrid = chrome.PointTable;
        graphHost.Controls.Add(_pointsPanel);
        _pointsPanel.BringToFront();

        var zoomPanel = chrome.NavigationPanel;
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
            if (_hasView)
            {
                MatchViewToCanvasAspect();
                ResampleVisibleView();
            }
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
        SetNormalView();
        _hasView = true;
        ResampleVisibleView();
        UpdatePointerStatus(null);

        if (invalidate)
            _canvas.Invalidate();
    }

    private void SetNormalView()
    {
        var plot = GetPlotRectangle();
        var aspect = plot.Width > 0 && plot.Height > 0
            ? Math.Max(0.1f, plot.Width / (float)plot.Height)
            : 1f;

        var halfY = NormalHalfYRange;
        var halfX = halfY * aspect;

        _viewMinX = -halfX;
        _viewMaxX = halfX;
        _viewMinY = -halfY;
        _viewMaxY = halfY;
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
        MatchViewToCanvasAspect();
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
        MatchViewToCanvasAspect();
        ResampleVisibleView();
        UpdatePointerStatus(e.Location);
        _canvas.Invalidate();
    }

    private void MatchViewToCanvasAspect()
    {
        var plot = GetPlotRectangle();
        if (plot.Width <= 0 || plot.Height <= 0)
            return;

        var targetAspect = plot.Width / (float)plot.Height;
        var centerX = (_viewMinX + _viewMaxX) / 2f;
        var centerY = (_viewMinY + _viewMaxY) / 2f;
        var halfX = Math.Max(0.0001f, (_viewMaxX - _viewMinX) / 2f);
        var halfY = Math.Max(0.0001f, (_viewMaxY - _viewMinY) / 2f);

        if (halfX / halfY < targetAspect)
            halfX = halfY * targetAspect;
        else
            halfY = halfX / targetAspect;

        _viewMinX = centerX - halfX;
        _viewMaxX = centerX + halfX;
        _viewMinY = centerY - halfY;
        _viewMaxY = centerY + halfY;
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

        GraphSurfaceApi.Draw(
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
        return GraphSurfaceApi.GetPlotRectangle(_canvas);
    }

    private PointF ScreenToGraph(PointF screenPoint, Rectangle plot)
    {
        return GraphSurfaceApi.ScreenToGraph(screenPoint, plot, new GraphPlotView(_viewMinX, _viewMaxX, _viewMinY, _viewMaxY));
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
            : value.ToString("0.0", CultureInfo.CurrentCulture);
    }

}

