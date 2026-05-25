#nullable enable
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text.RegularExpressions;
using Tiedragon.Graph;
using Tiedragon.Graph.G2D;
using Tiedragon.NodSystem.Core;
using Tiedragon.ToolEditor;

namespace Syscalculator.UI.WinForms;

public sealed class GraphPreviewForm : Form
{
    private const float NormalHalfYRange = 5f;
    private const int MaxVisibleStepPoints = 700;
    private const int MaxLineSamplePoints = 500;
    private const decimal GraphRangeLimit = 1_000_000_000_000_000_000_000_000m;
    private const decimal GraphStepMinimum = 0.0000000000000000000000000001m;
    private const double NanoScaleSpan = 1e-9;
    private const double LocatorZoomRatio = 0.18d;

    private readonly Func<string> _getNodText;
    private readonly LanguageCatalog? _language;
    private readonly ToolEditorUiTheme _uiTheme;
    private readonly NumericUpDown _xMin;
    private readonly NumericUpDown _xMax;
    private readonly NumericUpDown _yMin;
    private readonly NumericUpDown _yMax;
    private readonly NumericUpDown _step;
    private readonly CheckBox _showRangeLines;
    private readonly TableLayoutPanel _root;
    private readonly Panel _canvas;
    private readonly Panel _pointsPanel;
    private readonly Panel _pointsTitleBar;
    private readonly Panel _statusPanel;
    private readonly DataGridView _pointsGrid;
    private readonly Label _status;
    private readonly Button _toggleTableButton;
    private readonly ToolTip _inputToolTip = new();
    private readonly HashSet<NumericUpDown> _editedNumberBoxes = new();
    private readonly List<PointF> _fitGraphPoints = new();
    private readonly List<PointF> _graphPoints = new();
    private readonly List<PointF> _stepPoints = new();
    private string _disabledMessage = "";
    private string _pointerText = "";
    private string _graphDataTextSignature = "";
    private NodDocument? _currentDocument;
    private int _lastSkipped;
    private double _viewMinX;
    private double _viewMaxX;
    private double _viewMinY;
    private double _viewMaxY;
    private GraphPlotView _markerView = new(-NormalHalfYRange, NormalHalfYRange, -NormalHalfYRange, NormalHalfYRange);
    private bool _hasView;
    private bool _updatingXRangeControls;
    private bool _updatingYRangeControls;
    private bool _applyingSyncState;
    private bool _panning;
    private bool _rectangleZooming;
    private bool _draggingPointsPanel;
    private bool _pointTableRequestedVisible = true;
    private Point _panStart;
    private Point _rectangleZoomStart;
    private Point _rectangleZoomCurrent;
    private Point _pointsPanelDragStart;
    private Point _pointsPanelStartLocation;
    private double _panStartMinX;
    private double _panStartMaxX;
    private double _panStartMinY;
    private double _panStartMaxY;
    private double _userStep = 1d;
    private bool _updatingStepDisplay;
    private bool IsDarkTheme => _uiTheme == ToolEditorUiTheme.Dark;
    private Color WindowBackColor => IsDarkTheme ? Color.FromArgb(18, 24, 32) : SystemColors.Control;
    private Color PanelBackColor => IsDarkTheme ? Color.FromArgb(17, 24, 39) : Color.FromArgb(250, 250, 250);
    private Color ToolbarBackColor => IsDarkTheme ? Color.FromArgb(31, 41, 55) : Color.FromArgb(250, 250, 250);
    private Color GraphHostBackColor => IsDarkTheme ? Color.FromArgb(15, 23, 42) : Color.FromArgb(240, 244, 249);
    private Color TextColor => IsDarkTheme ? Color.FromArgb(226, 232, 240) : SystemColors.ControlText;
    private Color EditorBackColor => IsDarkTheme ? Color.FromArgb(39, 39, 39) : SystemColors.Window;
    private Color BorderColor => IsDarkTheme ? Color.FromArgb(55, 65, 81) : Color.FromArgb(205, 212, 222);

    public GraphPreviewForm(Func<string> getNodText)
        : this(getNodText, null)
    {
    }

    internal GraphPreviewForm(Func<string> getNodText, LanguageCatalog? language)
    {
        _getNodText = getNodText;
        _language = language;
        _uiTheme = ToolEditorUiThemeSettings.Load();
        GraphOverlayStyle.UseDarkTheme = IsDarkTheme;

        Text = T("editor.graph.title", "Graph 2D");
        AppWindowIcon.ApplyTo(this);
        BackColor = WindowBackColor;
        ForeColor = TextColor;
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
            BackColor = PanelBackColor,
            ForeColor = TextColor
        };
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var controls = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 14,
            RowCount = 1,
            Padding = new Padding(0, 5, 0, 0),
            BackColor = ToolbarBackColor,
            ForeColor = TextColor
        };
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        controls.Controls.Add(MakeToolbarLabel("X min"), 0, 0);
        _xMin = MakeNumberBox(-5, -GraphRangeLimit, GraphRangeLimit, 1);
        _xMin.ValueChanged += (_, _) => { if (!IsEditingNumberBox(_xMin)) ApplyXRangeFromControls(); };
        controls.Controls.Add(_xMin, 1, 0);
        controls.Controls.Add(MakeToolbarLabel("X max"), 2, 0);
        _xMax = MakeNumberBox(5, -GraphRangeLimit, GraphRangeLimit, 1);
        _xMax.ValueChanged += (_, _) => { if (!IsEditingNumberBox(_xMax)) ApplyXRangeFromControls(); };
        controls.Controls.Add(_xMax, 3, 0);
        controls.Controls.Add(MakeToolbarLabel("Y min"), 4, 0);
        _yMin = MakeNumberBox(-5, -GraphRangeLimit, GraphRangeLimit, 1);
        _yMin.ValueChanged += (_, _) => { if (!IsEditingNumberBox(_yMin)) ApplyYRangeFromControls(); };
        controls.Controls.Add(_yMin, 5, 0);
        controls.Controls.Add(MakeToolbarLabel("Y max"), 6, 0);
        _yMax = MakeNumberBox(5, -GraphRangeLimit, GraphRangeLimit, 1);
        _yMax.ValueChanged += (_, _) => { if (!IsEditingNumberBox(_yMax)) ApplyYRangeFromControls(); };
        controls.Controls.Add(_yMax, 7, 0);
        controls.Controls.Add(MakeToolbarLabel(T("editor.graph.step", "Step")), 8, 0);
        _step = MakeNumberBox(1, GraphStepMinimum, GraphRangeLimit, 1);
        _step.ValueChanged += (_, _) =>
        {
            if (!_applyingSyncState && !_updatingStepDisplay && !IsEditingNumberBox(_step))
            {
                _userStep = GraphSurfaceApi.GetNumberBoxValue(_step);
                Generate();
            }
        };
        controls.Controls.Add(_step, 9, 0);
        ApplyInputTooltips(_xMin, _xMax, _yMin, _yMax, _step);
        AttachCommittedInput(_xMin, ApplyXRangeFromControls);
        AttachCommittedInput(_xMax, ApplyXRangeFromControls);
        AttachCommittedInput(_yMin, ApplyYRangeFromControls);
        AttachCommittedInput(_yMax, ApplyYRangeFromControls);
        AttachCommittedInput(_step, () =>
        {
            _userStep = GraphSurfaceApi.GetNumberBoxValue(_step);
            if (!_applyingSyncState)
                Generate();
        });
        AttachStepSpinner(_step, CommitStepSpinner);

        var copy = new GraphToolbarIconButton(GraphToolbarIcon.Copy, T("editor.graph.copy_points", "Copy points"))
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Height = 28,
            Margin = new Padding(4, 0, 4, 0)
        };
        copy.Click += (_, _) =>
        {
            var text = BuildPointsText();
            if (!string.IsNullOrWhiteSpace(text))
                Clipboard.SetText(text);
        };
        controls.Controls.Add(copy, 10, 0);

        _toggleTableButton = new GraphToolbarIconButton(GraphToolbarIcon.TableHidden, T("editor.graph.hide_table", "Hide table"))
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Height = 28,
            Margin = new Padding(4, 0, 4, 0)
        };
        _toggleTableButton.Click += (_, _) => TogglePointTable();
        controls.Controls.Add(_toggleTableButton, 11, 0);

        _showRangeLines = new CheckBox
        {
            Text = T("editor.graph.lines", "Lines"),
            Checked = true,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(6, 0, 0, 0),
            BackColor = ToolbarBackColor,
            ForeColor = TextColor
        };
        controls.Controls.Add(_showRangeLines, 12, 0);

        _root.Controls.Add(controls, 0, 0);

        var graphHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = GraphHostBackColor,
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
        _canvas.MouseEnter += (_, _) =>
        {
            _canvas.Focus();
            if (_hasView)
                _canvas.Cursor = GraphCursors.Pan;
        };
        _canvas.MouseLeave += (_, _) =>
        {
            if (!_panning)
                _canvas.Cursor = GraphCursors.Default;
        };
        _showRangeLines.CheckedChanged += (_, _) =>
        {
            ApplyRangeLineVisibility();
            UpdateRangeInputMode();
            NotifySyncStateChanged();
            _canvas.Invalidate();
        };
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
            ForeColor = GraphOverlayStyle.TitleText,
            Font = new Font("Segoe UI", 7.5f)
        };
        _statusPanel = new Panel
        {
            Width = 52,
            Height = 20,
            BackColor = GraphOverlayStyle.PanelFill(translucent: false)
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
                UpdateViewportRangeControlsIfNeeded();
                ResampleVisibleView();
            }
            _canvas.Invalidate();
        };
        PlaceFloatingPanels();

        _root.Controls.Add(graphHost, 0, 1);

        Controls.Add(_root);
        ApplyThemeToChildren(this);
        InitializeBaseView();
        UpdateRangeInputMode();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ToolEditorUiThemeSettings.ApplyNativeWindowTheme(this, _uiTheme);
    }

    internal event EventHandler<GraphPreviewSyncState>? SyncStateChanged;

    internal void ApplySyncState(GraphPreviewSyncState state)
    {
        if (_applyingSyncState)
            return;

        _applyingSyncState = true;
        try
        {
            SetStepValue(state.Step);
            _showRangeLines.Checked = state.ShowRangeLines;
            _markerView = state.MarkerView;
            SetView(CreateAspectViewFromSync(state.View));
            if (_showRangeLines.Checked)
                SetRangeControls(_markerView);
            else
                UpdateViewportRangeControlsIfNeeded();
            _hasView = true;
            UpdatePointerStatus(null);
            _canvas.Invalidate();
        }
        finally
        {
            _applyingSyncState = false;
        }
    }

    private void NotifySyncStateChanged()
    {
        if (_applyingSyncState || !_hasView)
            return;

        SyncStateChanged?.Invoke(this, new GraphPreviewSyncState(GetView(), _markerView, (decimal)_userStep, _showRangeLines.Checked));
    }

    private GraphPlotView CreateAspectViewFromSync(GraphPlotView sourceView)
    {
        return GraphSurfaceApi.MatchViewToCanvasAspect(sourceView, _canvas);
    }

    private void SetStepValue(decimal value)
    {
        var clamped = Math.Clamp(value, _step.Minimum, _step.Maximum);
        _userStep = (double)clamped;
        SetStepDisplay(_userStep);
    }

    private void InitializeBaseView()
    {
        var view = GraphSurfaceApi.MatchViewToCanvasAspect(
            new GraphPlotView(GraphSurfaceApi.GetNumberBoxValue(_xMin), GraphSurfaceApi.GetNumberBoxValue(_xMax), GraphSurfaceApi.GetNumberBoxValue(_yMin), GraphSurfaceApi.GetNumberBoxValue(_yMax)),
            _canvas);
        SetView(view);
        _markerView = view;
        SetRangeControls(view);
        _hasView = true;
        _status.Text = T("editor.graph.position_empty", "Position: (-, -)");
    }

    private string T(string key, string fallback) => _language?.Text(key, fallback) ?? fallback;

    private Label MakeToolbarLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0),
            BackColor = ToolbarBackColor,
            ForeColor = TextColor
        };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Generate();
    }

    private static NumericUpDown MakeNumberBox(decimal value, decimal minimum, decimal maximum, decimal increment)
    {
        return new NumericUpDown
        {
            DecimalPlaces = 1,
            Minimum = minimum,
            Maximum = maximum,
            Increment = increment,
            Tag = (double)value,
            Value = value,
            Anchor = AnchorStyles.Left,
            Width = 86,
            Margin = new Padding(0)
        };
    }

    private void ApplyThemeToChildren(Control control)
    {
        foreach (Control child in control.Controls)
        {
            switch (child)
            {
                case NumericUpDown numberBox:
                    numberBox.BackColor = EditorBackColor;
                    numberBox.ForeColor = TextColor;
                    break;
                case DataGridView grid:
                    ApplyGridTheme(grid);
                    break;
                case CheckBox checkBox:
                    checkBox.BackColor = ToolbarBackColor;
                    checkBox.ForeColor = TextColor;
                    break;
                case Label label when !ReferenceEquals(label, _status):
                    label.BackColor = ToolbarBackColor;
                    label.ForeColor = TextColor;
                    break;
                case TableLayoutPanel table:
                    table.BackColor = ReferenceEquals(table, _root) ? PanelBackColor : ToolbarBackColor;
                    table.ForeColor = TextColor;
                    break;
                case Panel panel when !ReferenceEquals(panel, _canvas):
                    panel.BackColor = ReferenceEquals(panel, _statusPanel) || ReferenceEquals(panel, _pointsPanel)
                        ? GraphOverlayStyle.PanelFill(translucent: false)
                        : GraphHostBackColor;
                    panel.ForeColor = TextColor;
                    break;
            }

            ApplyThemeToChildren(child);
        }

        if (_pointsTitleBar is not null)
        {
            _pointsTitleBar.BackColor = GraphOverlayStyle.TitleFill;
            _pointsTitleBar.Invalidate();
        }

        _status.ForeColor = GraphOverlayStyle.TitleText;
        _statusPanel.BackColor = GraphOverlayStyle.PanelFill(translucent: false);
    }

    private void ApplyGridTheme(DataGridView grid)
    {
        grid.BackgroundColor = GraphOverlayStyle.TableBack;
        grid.BackColor = GraphOverlayStyle.TableBack;
        grid.ForeColor = GraphOverlayStyle.TableText;
        grid.GridColor = GraphOverlayStyle.TableGrid;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = GraphOverlayStyle.TitleFill;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = GraphOverlayStyle.TitleText;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = GraphOverlayStyle.TitleFill;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = GraphOverlayStyle.TitleText;
        grid.DefaultCellStyle.BackColor = GraphOverlayStyle.TableBack;
        grid.DefaultCellStyle.ForeColor = GraphOverlayStyle.TableText;
        grid.DefaultCellStyle.SelectionBackColor = GraphOverlayStyle.TableSelectionBack;
        grid.DefaultCellStyle.SelectionForeColor = GraphOverlayStyle.TableSelectionText;
        grid.AlternatingRowsDefaultCellStyle.BackColor = GraphOverlayStyle.TableAlternateBack;
        grid.AlternatingRowsDefaultCellStyle.ForeColor = GraphOverlayStyle.TableText;
    }

    private void ApplyInputTooltips(params Control[] controls)
    {
        var text = T(
            "editor.graph.input_tooltip",
            "Voorbeelden:\r\n1,05\r\n0,5 = 5 x 10⁻¹\r\n0,0000005 = 500 n = 5 x 10⁻⁷\r\n10⁵ (ook: 10^5)\r\n10⁻⁵ (ook: 10^-5)\r\n1e-5\r\n10 n\r\n2 k");
        foreach (var control in controls)
            _inputToolTip.SetToolTip(control, text);
    }

    private void AttachCommittedInput(NumericUpDown box, Action commit)
    {
        box.TextChanged += (_, _) =>
        {
            if (box.Focused)
                _editedNumberBoxes.Add(box);
        };
        box.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;
            CommitEditedNumberBox(box, commit);
        };
        box.Leave += (_, _) => CommitEditedNumberBox(box, commit);
    }

    private bool IsEditingNumberBox(NumericUpDown box)
    {
        return _editedNumberBoxes.Contains(box);
    }

    private void CommitEditedNumberBox(NumericUpDown box, Action commit)
    {
        if (!_editedNumberBoxes.Remove(box))
            return;

        box.Validate();
        GraphSurfaceApi.CommitNumberBoxValue(box);
        commit();
    }

    private void AttachStepSpinner(NumericUpDown box, Action<decimal> commit)
    {
        box.UpDownAlign = LeftRightAlignment.Right;
        box.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Up && e.KeyCode != Keys.Down)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;
            commit(GetNextScaleAwareStep(GraphSurfaceApi.GetNumberBoxValue(box), e.KeyCode == Keys.Up));
        };
        box.MouseWheel += (_, e) =>
        {
            ((HandledMouseEventArgs)e).Handled = true;
            commit(GetNextScaleAwareStep(GraphSurfaceApi.GetNumberBoxValue(box), e.Delta > 0));
        };
    }

    private void CommitStepSpinner(decimal value)
    {
        var clamped = Math.Clamp(value, _step.Minimum, _step.Maximum);
        _userStep = (double)clamped;
        SetStepDisplay(_userStep);
        if (!_applyingSyncState)
            Generate();
    }

    private static decimal GetNextScaleAwareStep(double current, bool increase)
    {
        if (!double.IsFinite(current) || current <= 0d)
            current = 1d;

        var exponent = Math.Floor(Math.Log10(current));
        var scale = Math.Pow(10d, exponent);
        var normalized = current / scale;
        double next;
        if (increase)
        {
            next = normalized < 0.5d ? 0.5d :
                normalized < 0.75d ? 0.75d :
                normalized < 1d ? 1d :
                normalized < 1.25d ? 1.25d :
                normalized < 1.5d ? 1.5d :
                normalized < 2d ? 2d :
                normalized < 2.5d ? 2.5d :
                normalized < 5d ? 5d :
                normalized < 7.5d ? 7.5d :
                10d;
        }
        else
        {
            next = normalized > 7.5d ? 7.5d :
                normalized > 5d ? 5d :
                normalized > 2.5d ? 2.5d :
                normalized > 2d ? 2d :
                normalized > 1.5d ? 1.5d :
                normalized > 1.25d ? 1.25d :
                normalized > 1d ? 1d :
                normalized > 0.75d ? 0.75d :
                normalized > 0.5d ? 0.5d :
                0.1d;
        }

        var result = next * scale;
        if (next == 0.1d)
            result = scale / 10d;

        try
        {
            return (decimal)result;
        }
        catch (OverflowException)
        {
            return current < 1d ? GraphStepMinimum : GraphRangeLimit;
        }
    }

    internal void InvalidateGraphData()
    {
        _graphDataTextSignature = "";
        _currentDocument = null;
        _fitGraphPoints.Clear();
        _graphPoints.Clear();
        _stepPoints.Clear();
        FillPointsGrid([]);
        _canvas.Invalidate();
    }

    private void TogglePointTable()
    {
        SetPointTableVisible(!_pointsPanel.Visible);
    }

    private void SetPointTableVisible(bool visible)
    {
        _pointTableRequestedVisible = visible;
        ApplyPointTableVisibility();
    }

    private void ApplyPointTableVisibility()
    {
        var hasRows = _pointsGrid.Rows.Count > 0;
        var visible = _pointTableRequestedVisible && hasRows;
        _pointsPanel.Visible = visible;
        UpdateFloatingStatusVisibility();
        if (_toggleTableButton is GraphToolbarIconButton iconButton)
        {
            iconButton.Icon = visible ? GraphToolbarIcon.TableHidden : GraphToolbarIcon.TableVisible;
            iconButton.TooltipText = visible ? T("editor.graph.hide_table", "Hide table") : T("editor.graph.show_table", "Show table");
        }
        _canvas.Invalidate();
    }

    private void UpdatePointTableAvailability()
    {
        var hasRows = _pointsGrid.Rows.Count > 0;
        _toggleTableButton.Enabled = hasRows;
        ApplyPointTableVisibility();
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
        _pointsPanel.Cursor = Cursors.Hand;
        _pointsGrid.Cursor = Cursors.Hand;
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
            Cursor = GraphCursors.Pan;
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
        using var path = UiGeometry.CreateRoundedRectangle(rect, 9);
        using var fill = new SolidBrush(GraphOverlayStyle.PanelFill(translucent: false));
        using var border = new Pen(GraphOverlayStyle.PanelBorder(translucent: false), 1);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
    }

    private void Generate()
    {
        _fitGraphPoints.Clear();
        _graphPoints.Clear();
        _stepPoints.Clear();
        _pointsGrid.Rows.Clear();
        var graphTextSignature = _getNodText();

        try
        {
            var min = GraphSurfaceApi.GetNumberBoxValue(_xMin);
            var max = GraphSurfaceApi.GetNumberBoxValue(_xMax);
            var step = _userStep;

            if (min > max)
            {
                _status.Text = "X min moet kleiner of gelijk zijn aan X max.";
                _canvas.Invalidate();
                return;
            }

            _currentDocument = NodParser.Parse(graphTextSignature);
            if (!IsGraphCompatible(_currentDocument, out var disabledReason))
            {
                _disabledMessage = disabledReason;
                _fitGraphPoints.Clear();
                _graphPoints.Clear();
                _stepPoints.Clear();
                FillPointsGrid([]);
                SetPointTableVisible(false);
                _hasView = false;
                _status.Text = T("editor.graph.disabled", "Graph 2D disabled");
                _canvas.Invalidate();
                return;
            }

            _disabledMessage = "";
            var stepRows = new List<PointF>();
            var skipped = 0;
            var visibleMin = Math.Min(min, max);
            var visibleMax = Math.Max(min, max);
            var lineSamples = Math.Clamp(_canvas.ClientSize.Width / 6, 80, MaxLineSamplePoints);
            var lineStep = (visibleMax - visibleMin) / Math.Max(1, lineSamples);
            if (!double.IsFinite(lineStep) || lineStep <= 0)
                lineStep = Math.Max(1.0, step);

            for (var i = 0; i <= lineSamples; i++)
            {
                var x = visibleMin + lineStep * i;
                var input = FormatGraphInput(x);

                try
                {
                    var result = NodEngine.ConvertForward(_currentDocument, input);
                    if (TryGetNumber(result, out var y))
                    {
                        var point = new PointF((float)x, (float)y);
                        _fitGraphPoints.Add(point);
                        _graphPoints.Add(point);
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

            var displayStep = ChooseDisplayedStep(step, visibleMin, visibleMax, MaxVisibleStepPoints);
            SetStepDisplay(displayStep);
            var firstStep = Math.Ceiling(visibleMin / displayStep) * displayStep;
            for (var i = 0; i < MaxVisibleStepPoints; i++)
            {
                var x = firstStep + displayStep * i;
                if (!double.IsFinite(x) || x > visibleMax + displayStep / 1000.0)
                    break;

                var input = FormatGraphInput(x);
                try
                {
                    var result = NodEngine.ConvertForward(_currentDocument, input);
                    if (TryGetNumber(result, out var y))
                    {
                        var point = new PointF((float)x, (float)y);
                        _stepPoints.Add(point);
                        stepRows.Add(point);
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
            _graphDataTextSignature = graphTextSignature;
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
        var view = GraphSurfaceApi.CreateFitViewForCanvas(
            _fitGraphPoints,
            (float)GraphSurfaceApi.GetNumberBoxValue(_xMin),
            (float)GraphSurfaceApi.GetNumberBoxValue(_xMax),
            _canvas,
            NormalHalfYRange);
        SetView(view);
        _markerView = view;
        SetRangeControls(view);
        UpdateViewportRangeControlsIfNeeded();
        _hasView = true;
        ResampleVisibleView();
        UpdatePointerStatus(null);
        NotifySyncStateChanged();

        if (invalidate)
            _canvas.Invalidate();
    }

    private void ApplyXRangeFromControls()
    {
        if (_updatingXRangeControls || !_hasView)
            return;

        if (GraphSurfaceApi.GetNumberBoxValue(_xMin) >= GraphSurfaceApi.GetNumberBoxValue(_xMax))
        {
            _status.Text = "X min moet kleiner zijn dan X max.";
            return;
        }

        SetView(GraphSurfaceApi.CreateViewFromXRangeControls(GetView(), _xMin, _xMax, _canvas));
        if (_showRangeLines.Checked)
            _markerView = GetView();
        ResampleVisibleView();
        UpdatePointerStatus(null);
        NotifySyncStateChanged();
        _canvas.Invalidate();
    }

    private void ApplyYRangeFromControls()
    {
        if (_updatingYRangeControls || !_hasView)
            return;

        if (GraphSurfaceApi.GetNumberBoxValue(_yMin) >= GraphSurfaceApi.GetNumberBoxValue(_yMax))
        {
            _status.Text = "Y min moet kleiner zijn dan Y max.";
            return;
        }

        SetView(GraphSurfaceApi.CreateViewFromYRangeControls(GetView(), _yMin, _yMax, _canvas));
        if (_showRangeLines.Checked)
            _markerView = GetView();
        ResampleVisibleView();
        UpdatePointerStatus(null);
        NotifySyncStateChanged();
        _canvas.Invalidate();
    }

    private GraphPlotView GetView()
    {
        return new GraphPlotView(_viewMinX, _viewMaxX, _viewMinY, _viewMaxY);
    }

    private void SetView(GraphPlotView view)
    {
        _viewMinX = view.MinX;
        _viewMaxX = view.MaxX;
        _viewMinY = view.MinY;
        _viewMaxY = view.MaxY;
    }

    private void SetRangeControls(GraphPlotView view)
    {
        _updatingXRangeControls = true;
        _updatingYRangeControls = true;
        try
        {
            GraphSurfaceApi.SetRangeControlValues(new GraphRangeControls(_xMin, _xMax, _yMin, _yMax), view);
        }
        finally
        {
            _updatingYRangeControls = false;
            _updatingXRangeControls = false;
        }
    }

    private void UpdateViewportRangeControlsIfNeeded()
    {
        _updatingXRangeControls = true;
        _updatingYRangeControls = true;
        try
        {
            GraphSurfaceApi.SyncViewportRangeControls(
                new GraphRangeControls(_xMin, _xMax, _yMin, _yMax),
                GetView(),
                _showRangeLines.Checked);
        }
        finally
        {
            _updatingYRangeControls = false;
            _updatingXRangeControls = false;
        }
    }

    private void ApplyRangeLineVisibility()
    {
        if (_showRangeLines.Checked)
        {
            SetRangeControls(_markerView);
            UpdateRangeInputMode();
            return;
        }

        UpdateViewportRangeControlsIfNeeded();
        UpdateRangeInputMode();
    }

    private void UpdateRangeInputMode()
    {
        var editable = _showRangeLines.Checked;
        foreach (var box in new[] { _xMin, _xMax, _yMin, _yMax, _step })
        {
            box.Enabled = editable;
            box.ReadOnly = !editable;
        }
    }

    private void ZoomView(float factor)
    {
        if (!EnsureGraphGeneratedForInteraction())
            return;

        ZoomViewAt(factor, new PointF(_canvas.ClientSize.Width / 2f, _canvas.ClientSize.Height / 2f));
    }

    private void Canvas_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (EnsureGraphGeneratedForInteraction())
            ZoomViewAt(e.Delta > 0 ? 0.85f : 1.18f, e.Location);
    }

    private bool EnsureGraphGeneratedForInteraction()
    {
        if (_hasView && _currentDocument is not null)
            return true;

        if (_hasView)
        {
            try
            {
                _currentDocument = NodParser.Parse(_getNodText());
                if (!IsGraphCompatible(_currentDocument, out var disabledReason))
                {
                    _disabledMessage = disabledReason;
                    _currentDocument = null;
                    _hasView = false;
                    _canvas.Invalidate();
                    return false;
                }

                ResampleVisibleView();
                _canvas.Invalidate();
                return true;
            }
            catch
            {
                _currentDocument = null;
                return false;
            }
        }

        Generate();
        return _hasView && _currentDocument is not null;
    }

    private void ZoomViewAt(float factor, PointF screenPoint)
    {
        var plot = GetPlotRectangle();
        if (plot.Width <= 0 || plot.Height <= 0)
            return;

        var anchor = ScreenToGraph(screenPoint, plot);
        var newWidth = (_viewMaxX - _viewMinX) * factor;
        var newHeight = (_viewMaxY - _viewMinY) * factor;
        if (newWidth < GraphSurfaceApi.MinimumViewSpan || newHeight < GraphSurfaceApi.MinimumViewSpan)
            return;

        var xRatio = (anchor.X - _viewMinX) / (_viewMaxX - _viewMinX);
        var yRatio = (anchor.Y - _viewMinY) / (_viewMaxY - _viewMinY);
        _viewMinX = anchor.X - newWidth * xRatio;
        _viewMaxX = _viewMinX + newWidth;
        _viewMinY = anchor.Y - newHeight * yRatio;
        _viewMaxY = _viewMinY + newHeight;
        MatchViewToCanvasAspect();
        UpdateViewportRangeControlsIfNeeded();
        ResampleVisibleView();
        UpdatePointerStatus(screenPoint);
        NotifySyncStateChanged();
        _canvas.Invalidate();
    }

    private void Canvas_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            if (!EnsureGraphGeneratedForInteraction())
                return;

            var plot = GetPlotRectangle();
            if (!plot.Contains(e.Location))
                return;

            _rectangleZooming = true;
            _rectangleZoomStart = e.Location;
            _rectangleZoomCurrent = e.Location;
            _canvas.Cursor = GraphCursors.Pan;
            _canvas.Invalidate();
            return;
        }

        if (e.Button != MouseButtons.Left || !EnsureGraphGeneratedForInteraction())
            return;

        _panning = true;
        _panStart = e.Location;
        _panStartMinX = _viewMinX;
        _panStartMaxX = _viewMaxX;
        _panStartMinY = _viewMinY;
        _panStartMaxY = _viewMaxY;
        _canvas.Cursor = GraphCursors.Pan;
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_rectangleZooming)
        {
            _rectangleZoomCurrent = e.Location;
            UpdatePointerStatus(e.Location);
            _canvas.Invalidate();
            return;
        }

        if (!_panning)
        {
            if (_hasView)
                _canvas.Cursor = GraphCursors.Pan;
            UpdatePointerStatus(e.Location);
            return;
        }

        var plot = GetPlotRectangle();
        if (plot.Width <= 0 || plot.Height <= 0)
            return;

        var dx = e.X - _panStart.X;
        var dy = e.Y - _panStart.Y;
        var graphDx = dx / (double)plot.Width * (_panStartMaxX - _panStartMinX);
        var graphDy = dy / (double)plot.Height * (_panStartMaxY - _panStartMinY);

        _viewMinX = _panStartMinX - graphDx;
        _viewMaxX = _panStartMaxX - graphDx;
        _viewMinY = _panStartMinY + graphDy;
        _viewMaxY = _panStartMaxY + graphDy;
        MatchViewToCanvasAspect();
        UpdateViewportRangeControlsIfNeeded();
        UpdatePointerStatus(e.Location);
        NotifySyncStateChanged();
        _canvas.Invalidate();
    }

    private void Canvas_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right && _rectangleZooming)
        {
            CompleteRectangleZoom(e.Location);
            return;
        }

        _panning = false;
        ResampleVisibleView();
        _canvas.Cursor = _canvas.ClientRectangle.Contains(e.Location) && _hasView ? GraphCursors.Pan : GraphCursors.Default;
        _canvas.Invalidate();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape && _rectangleZooming)
        {
            CancelRectangleZoom();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void CompleteRectangleZoom(Point endPoint)
    {
        _rectangleZoomCurrent = endPoint;
        var plot = GetPlotRectangle();
        var rect = BuildAspectZoomRectangle(_rectangleZoomStart, _rectangleZoomCurrent, plot);
        _rectangleZooming = false;

        if (rect.Width < 10 || rect.Height < 10)
        {
            _canvas.Cursor = _canvas.ClientRectangle.Contains(endPoint) && _hasView ? GraphCursors.Pan : GraphCursors.Default;
            _canvas.Invalidate();
            return;
        }

        var topLeft = ScreenToGraph(new PointF(rect.Left, rect.Top), plot);
        var bottomRight = ScreenToGraph(new PointF(rect.Right, rect.Bottom), plot);
        SetView(new GraphPlotView(
            Math.Min(topLeft.X, bottomRight.X),
            Math.Max(topLeft.X, bottomRight.X),
            Math.Min(topLeft.Y, bottomRight.Y),
            Math.Max(topLeft.Y, bottomRight.Y)));
        MatchViewToCanvasAspect();
        UpdateViewportRangeControlsIfNeeded();
        ResampleVisibleView();
        UpdatePointerStatus(endPoint);
        NotifySyncStateChanged();
        _canvas.Cursor = _canvas.ClientRectangle.Contains(endPoint) && _hasView ? GraphCursors.Pan : GraphCursors.Default;
        _canvas.Invalidate();
    }

    private void CancelRectangleZoom()
    {
        _rectangleZooming = false;
        _canvas.Cursor = _hasView ? GraphCursors.Pan : GraphCursors.Default;
        _canvas.Invalidate();
    }

    private void MatchViewToCanvasAspect()
    {
        SetView(GraphSurfaceApi.MatchViewToCanvasAspect(GetView(), _canvas));
    }

    private void ResampleVisibleView()
    {
        if (_currentDocument is null || !_hasView)
            return;

        if (_getNodText() != _graphDataTextSignature)
        {
            InvalidateGraphData();
            return;
        }

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
            var input = FormatGraphInput(x);

            try
            {
                var result = NodEngine.ConvertForward(_currentDocument, input);
                if (TryGetNumber(result, out var y))
                    points.Add(new PointF((float)x, (float)y));
                else
                    skipped++;
            }
            catch
            {
                skipped++;
            }
        }

        var requestedStep = _userStep;
        if (requestedStep > 0)
        {
            var stepMin = visibleMin;
            var stepMax = visibleMax;
            var displayStep = ChooseDisplayedStep(requestedStep, stepMin, stepMax, MaxVisibleStepPoints);
            SetStepDisplay(displayStep);
            var firstStep = Math.Ceiling(stepMin / displayStep) * displayStep;
            for (var i = 0; i < MaxVisibleStepPoints; i++)
            {
                var x = firstStep + displayStep * i;
                if (!double.IsFinite(x) || x > stepMax + displayStep / 1000.0)
                    break;

                var input = FormatGraphInput(x);
                try
                {
                    var result = NodEngine.ConvertForward(_currentDocument, input);
                    if (TryGetNumber(result, out var y))
                    {
                        var point = new PointF((float)x, (float)y);
                        if (IsPointInsideCurrentView(point))
                            stepPoints.Add(point);
                    }
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

    private bool IsPointInsideCurrentView(PointF point)
    {
        return float.IsFinite(point.X) &&
               float.IsFinite(point.Y) &&
               point.X >= _viewMinX &&
               point.X <= _viewMaxX &&
               point.Y >= _viewMinY &&
               point.Y <= _viewMaxY;
    }

    private static double ChooseDisplayedStep(double requestedStep, double min, double max, int maxPoints)
    {
        if (!double.IsFinite(requestedStep) || requestedStep <= 0 || !double.IsFinite(min) || !double.IsFinite(max) || max <= min)
            return Math.Max(1.0, requestedStep);

        var targetRows = Math.Max(8, maxPoints / 2);
        var rawStep = (max - min) / targetRows;
        if (!double.IsFinite(rawStep) || rawStep <= 0)
            return requestedStep;

        return NiceStep(rawStep);
    }

    private static double NiceStep(double value)
    {
        if (!double.IsFinite(value) || value <= 0d)
            return 1d;

        var exponent = Math.Floor(Math.Log10(value));
        var baseValue = Math.Pow(10d, exponent);
        var fraction = value / baseValue;
        var niceFraction = ChooseStepMantissa(fraction);
        return niceFraction * baseValue;
    }

    private static double ChooseStepMantissa(double fraction)
    {
        return fraction <= 0.5d ? 0.5d :
            fraction <= 0.75d ? 0.75d :
            fraction <= 1d ? 1d :
            fraction <= 1.25d ? 1.25d :
            fraction <= 1.5d ? 1.5d :
            fraction <= 2d ? 2d :
            fraction <= 2.5d ? 2.5d :
            fraction <= 5d ? 5d :
            fraction <= 7.5d ? 7.5d :
            10d;
    }

    private void SetStepDisplay(double value)
    {
        _updatingStepDisplay = true;
        try
        {
            GraphSurfaceApi.SetNumberBoxValue(_step, value);
        }
        finally
        {
            _updatingStepDisplay = false;
        }
    }

    private static string FormatGraphInput(double value)
    {
        var abs = Math.Abs(value);
        return abs is > 0 and < 1e-12 || abs >= 1e12
            ? value.ToString("0.############E+0", CultureInfo.InvariantCulture)
            : value.ToString("0.############", CultureInfo.InvariantCulture);
    }

    private void FillPointsGrid(IEnumerable<PointF> points)
    {
        _pointsGrid.SuspendLayout();
        _pointsGrid.Rows.Clear();
        foreach (var point in points)
        {
            _pointsGrid.Rows.Add(
                FormatGraphDisplayNumber(point.X),
                FormatGraphDisplayNumber(point.Y));
        }
        _pointsGrid.ClearSelection();
        _pointsGrid.ResumeLayout();
        UpdatePointTableAvailability();
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
            reason = T("editor.graph.disabled_chg", "Graph 2D is disabled for chg converters.");
            return false;
        }

        if (document.TranslateRules.Count > 0)
        {
            reason = T("editor.graph.disabled_trans", "Graph 2D is disabled for trans converters.");
            return false;
        }

        if (document.LegacyMathSteps.Count == 0 &&
            document.MathExpressions20.Count == 0 &&
            document.CalculusSteps.Count == 0 &&
            document.Equation is null)
        {
            reason = T("editor.graph.disabled_math", "Graph 2D is only available for numeric math converters.");
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
            _showRangeLines.Checked ? _markerView.MinX : GraphSurfaceApi.GetNumberBoxValue(_xMin),
            _showRangeLines.Checked ? _markerView.MaxX : GraphSurfaceApi.GetNumberBoxValue(_xMax),
            GraphSurfaceApi.GetNumberBoxValue(_step),
            _disabledMessage,
            "Generate graph",
            GraphPlotDensity.Normal,
            (float)GraphSurfaceApi.GetNumberBoxValue(_yMin),
            (float)GraphSurfaceApi.GetNumberBoxValue(_yMax),
            _showRangeLines.Checked);

        DrawNanoScaleLocator(e.Graphics);
        DrawRectangleZoomOverlay(e.Graphics);
    }

    private void DrawNanoScaleLocator(Graphics graphics)
    {
        if (_fitGraphPoints.Count == 0 || !_hasView)
            return;

        var home = GraphSurfaceApi.CreateFitViewForCanvas(
            _fitGraphPoints,
            (float)GraphSurfaceApi.GetNumberBoxValue(_xMin),
            (float)GraphSurfaceApi.GetNumberBoxValue(_xMax),
            _canvas,
            NormalHalfYRange);
        home = ExpandView(home, 1.25d);
        if (home.MaxX <= home.MinX || home.MaxY <= home.MinY)
            return;

        var view = GetView();
        var viewSpan = Math.Max(Math.Abs(view.MaxX - view.MinX), Math.Abs(view.MaxY - view.MinY));
        var homeSpan = Math.Max(Math.Abs(home.MaxX - home.MinX), Math.Abs(home.MaxY - home.MinY));
        if (!ShouldShowLocator(viewSpan, homeSpan))
            return;

        var plot = GetPlotRectangle();
        var width = Math.Min(210, Math.Max(150, plot.Width / 4));
        var height = Math.Min(130, Math.Max(92, plot.Height / 4));
        var rect = new Rectangle(
            plot.Right - width - 14,
            plot.Bottom - height - 14,
            width,
            height);
        if (rect.Left < plot.Left + 8 || rect.Top < plot.Top + 8)
            return;

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(Color.FromArgb(238, 255, 255, 255));
        using var border = new Pen(Color.FromArgb(30, 64, 175), 1.2f);
        using var redFill = new SolidBrush(Color.FromArgb(78, 220, 38, 38));
        using var redPen = new Pen(Color.FromArgb(220, 38, 38), 2.0f);
        graphics.FillRectangle(fill, rect);
        graphics.DrawRectangle(border, rect);
        DrawLocatorAxes(graphics, rect, home);
        if (_showRangeLines.Checked)
            DrawLocatorRangeMarkers(graphics, rect, home);
        DrawLocatorGraphLine(graphics, rect, home);

        var current = MapViewToLocator(view, home, rect);
        current.Intersect(rect);
        DrawLocatorViewIndicator(graphics, current, redFill, redPen);
    }

    private static void DrawLocatorViewIndicator(Graphics graphics, Rectangle current, Brush fill, Pen pen)
    {
        if (current.Width <= 0 || current.Height <= 0)
            return;

        if (current.Width < 7 && current.Height < 7)
        {
            var cx = current.Left + current.Width / 2f;
            var cy = current.Top + current.Height / 2f;
            var diameter = Math.Clamp(Math.Max(current.Width, current.Height), 1, 4);
            var radius = diameter / 2f;
            graphics.FillEllipse(fill, cx - radius, cy - radius, radius * 2f, radius * 2f);
            graphics.DrawEllipse(pen, cx - radius, cy - radius, radius * 2f, radius * 2f);
            return;
        }

        graphics.FillRectangle(fill, current);
        graphics.DrawRectangle(pen, current);
    }

    private void DrawLocatorGraphLine(Graphics graphics, Rectangle rect, GraphPlotView reference)
    {
        if (_currentDocument is null)
            return;

        var plot = rect;
        if (plot.Width <= 2 || plot.Height <= 2)
            return;

        var referenceWidth = reference.MaxX - reference.MinX;
        var referenceHeight = reference.MaxY - reference.MinY;
        if (referenceWidth <= 0d || referenceHeight <= 0d)
            return;

        PointF Map(PointF point)
        {
            var x = plot.Left + (point.X - reference.MinX) / referenceWidth * plot.Width;
            var y = plot.Bottom - (point.Y - reference.MinY) / referenceHeight * plot.Height;
            return new PointF(
                (float)Math.Clamp(x, plot.Left, plot.Right),
                (float)Math.Clamp(y, plot.Top, plot.Bottom));
        }

        using var pen = new Pen(Color.FromArgb(15, 63, 143), 1.35f);
        var segment = new List<PointF>();
        var sampleCount = Math.Clamp(plot.Width / 2, 80, 180);
        var step = (reference.MaxX - reference.MinX) / Math.Max(1, sampleCount);
        for (var i = 0; i <= sampleCount; i++)
        {
            var sourceX = reference.MinX + step * i;
            var input = FormatGraphInput(sourceX);
            PointF source;
            try
            {
                var result = NodEngine.ConvertForward(_currentDocument, input);
                if (!TryGetNumber(result, out var y))
                {
                    Flush();
                    continue;
                }

                source = new PointF((float)sourceX, (float)y);
            }
            catch
            {
                Flush();
                continue;
            }

            var point = Map(source);
            segment.Add(point);
        }

        Flush();

        void Flush()
        {
            if (segment.Count > 1)
                graphics.DrawLines(pen, segment.ToArray());
            segment.Clear();
        }
    }

    private static void DrawLocatorAxes(Graphics graphics, Rectangle rect, GraphPlotView reference)
    {
        var plot = rect;
        var referenceWidth = reference.MaxX - reference.MinX;
        var referenceHeight = reference.MaxY - reference.MinY;
        if (referenceWidth <= 0d || referenceHeight <= 0d)
            return;

        using var axisPen = new Pen(Color.FromArgb(110, 71, 85, 105), 1f);
        if (reference.MinX <= 0d && reference.MaxX >= 0d)
        {
            var x = plot.Left + (0d - reference.MinX) / referenceWidth * plot.Width;
            graphics.DrawLine(axisPen, (float)x, plot.Top, (float)x, plot.Bottom);
        }

        if (reference.MinY <= 0d && reference.MaxY >= 0d)
        {
            var y = plot.Bottom - (0d - reference.MinY) / referenceHeight * plot.Height;
            graphics.DrawLine(axisPen, plot.Left, (float)y, plot.Right, (float)y);
        }
    }

    private void DrawLocatorRangeMarkers(Graphics graphics, Rectangle rect, GraphPlotView reference)
    {
        var referenceWidth = reference.MaxX - reference.MinX;
        var referenceHeight = reference.MaxY - reference.MinY;
        if (referenceWidth <= 0d || referenceHeight <= 0d)
            return;

        float MapX(double x)
        {
            var mapped = rect.Left + (x - reference.MinX) / referenceWidth * rect.Width;
            return (float)Math.Clamp(mapped, rect.Left, rect.Right);
        }

        float MapY(double y)
        {
            var mapped = rect.Bottom - (y - reference.MinY) / referenceHeight * rect.Height;
            return (float)Math.Clamp(mapped, rect.Top, rect.Bottom);
        }

        using var bluePen = new Pen(Color.FromArgb(46, 110, 210), 1f) { DashStyle = DashStyle.Dot };
        using var greenPen = new Pen(Color.FromArgb(20, 125, 82), 1f) { DashStyle = DashStyle.Dot };

        var minX = MapX(_markerView.MinX);
        var maxX = MapX(_markerView.MaxX);
        var minY = MapY(_markerView.MinY);
        var maxY = MapY(_markerView.MaxY);
        graphics.DrawLine(bluePen, minX, rect.Top, minX, rect.Bottom);
        graphics.DrawLine(bluePen, maxX, rect.Top, maxX, rect.Bottom);
        graphics.DrawLine(greenPen, rect.Left, minY, rect.Right, minY);
        graphics.DrawLine(greenPen, rect.Left, maxY, rect.Right, maxY);
    }

    private static bool ShouldShowLocator(double viewSpan, double homeSpan)
    {
        if (!double.IsFinite(viewSpan) || !double.IsFinite(homeSpan) || viewSpan <= 0d || homeSpan <= 0d)
            return false;

        return viewSpan <= NanoScaleSpan || viewSpan / homeSpan <= LocatorZoomRatio;
    }

    private static GraphPlotView ExpandView(GraphPlotView view, double factor)
    {
        if (!double.IsFinite(factor) || factor <= 1d)
            return view;

        var centerX = (view.MinX + view.MaxX) / 2d;
        var centerY = (view.MinY + view.MaxY) / 2d;
        var halfX = (view.MaxX - view.MinX) * factor / 2d;
        var halfY = (view.MaxY - view.MinY) * factor / 2d;
        return new GraphPlotView(centerX - halfX, centerX + halfX, centerY - halfY, centerY + halfY);
    }

    private static Rectangle MapViewToLocator(GraphPlotView view, GraphPlotView reference, Rectangle rect)
    {
        var referenceWidth = reference.MaxX - reference.MinX;
        var referenceHeight = reference.MaxY - reference.MinY;
        if (referenceWidth <= 0 || referenceHeight <= 0)
            return Rectangle.Empty;

        var left = rect.Left + (view.MinX - reference.MinX) / referenceWidth * rect.Width;
        var right = rect.Left + (view.MaxX - reference.MinX) / referenceWidth * rect.Width;
        var top = rect.Bottom - (view.MaxY - reference.MinY) / referenceHeight * rect.Height;
        var bottom = rect.Bottom - (view.MinY - reference.MinY) / referenceHeight * rect.Height;

        var mappedLeft = (int)Math.Round(Math.Min(left, right));
        var mappedTop = (int)Math.Round(Math.Min(top, bottom));
        var mappedRight = (int)Math.Round(Math.Max(left, right));
        var mappedBottom = (int)Math.Round(Math.Max(top, bottom));
        if (mappedRight <= mappedLeft)
            mappedRight = mappedLeft + 1;
        if (mappedBottom <= mappedTop)
            mappedBottom = mappedTop + 1;

        mappedLeft = Math.Clamp(mappedLeft, rect.Left, rect.Right - 1);
        mappedTop = Math.Clamp(mappedTop, rect.Top, rect.Bottom - 1);
        mappedRight = Math.Clamp(mappedRight, mappedLeft + 1, rect.Right);
        mappedBottom = Math.Clamp(mappedBottom, mappedTop + 1, rect.Bottom);

        return Rectangle.FromLTRB(mappedLeft, mappedTop, mappedRight, mappedBottom);
    }

    private void DrawRectangleZoomOverlay(Graphics graphics)
    {
        if (!_rectangleZooming)
            return;

        var rect = BuildAspectZoomRectangle(_rectangleZoomStart, _rectangleZoomCurrent, GetPlotRectangle());
        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(Color.FromArgb(32, 220, 38, 38));
        using var pen = new Pen(Color.FromArgb(220, 38, 38), 1.8f)
        {
            DashStyle = DashStyle.Dash
        };
        graphics.FillRectangle(fill, rect);
        graphics.DrawRectangle(pen, rect);
    }

    private static Rectangle BuildAspectZoomRectangle(Point start, Point current, Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return Rectangle.Empty;

        start.X = Math.Clamp(start.X, bounds.Left, bounds.Right);
        start.Y = Math.Clamp(start.Y, bounds.Top, bounds.Bottom);
        current.X = Math.Clamp(current.X, bounds.Left, bounds.Right);
        current.Y = Math.Clamp(current.Y, bounds.Top, bounds.Bottom);

        var dx = (double)(current.X - start.X);
        var dy = (double)(current.Y - start.Y);
        if (dx == 0 || dy == 0)
            return Rectangle.Empty;

        var signX = Math.Sign(dx);
        var signY = Math.Sign(dy);
        var aspect = bounds.Width / (double)bounds.Height;
        var width = Math.Abs(dx);
        var height = Math.Abs(dy);
        if (width / height > aspect)
            height = width / aspect;
        else
            width = height * aspect;

        var maxWidth = signX > 0 ? bounds.Right - start.X : start.X - bounds.Left;
        var maxHeight = signY > 0 ? bounds.Bottom - start.Y : start.Y - bounds.Top;
        var scale = Math.Min(1d, Math.Min(maxWidth / Math.Max(1d, width), maxHeight / Math.Max(1d, height)));
        width *= scale;
        height *= scale;

        var endX = start.X + signX * width;
        var endY = start.Y + signY * height;
        return Rectangle.FromLTRB(
            (int)Math.Round(Math.Min(start.X, endX)),
            (int)Math.Round(Math.Min(start.Y, endY)),
            (int)Math.Round(Math.Max(start.X, endX)),
            (int)Math.Round(Math.Max(start.Y, endY)));
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
        return FormatGraphDisplayNumber(value);
    }

    private static string FormatGraphDisplayNumber(double value)
    {
        return GraphSurfaceApi.FormatDisplayNumber(value);
    }

}
