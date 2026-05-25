#nullable enable
using Tiedragon.Graph;
using Tiedragon.Graph.G2D;
using Tiedragon.Graph.G3D;
using Tiedragon.ToolEditor;

namespace Syscalculator.UI.WinForms;

internal sealed class Graph3DPreviewForm : Form
{
    private const decimal GraphRangeLimit = 1_000_000_000_000_000_000_000_000m;
    private const decimal GraphStepMinimum = 0.0000000000000000000000000001m;

    private readonly ToolEditorUiTheme _uiTheme;
    private readonly Panel _canvas;
    private readonly Panel _navigationPanel;
    private readonly Panel _pointsPanel;
    private readonly Panel _pointsTitleBar;
    private readonly DataGridView _pointsGrid;
    private readonly Graph3DRotationDial _rotationDial;
    private readonly NumericUpDown _xMin;
    private readonly NumericUpDown _xMax;
    private readonly NumericUpDown _yMin;
    private readonly NumericUpDown _yMax;
    private readonly NumericUpDown _zMin;
    private readonly NumericUpDown _zMax;
    private readonly NumericUpDown _step;
    private readonly NumericUpDown _gridStepBox;
    private readonly CheckBox _grid;
    private readonly CheckBox _lines;
    private readonly Button _toggleTableButton;
    private readonly Button _flat2DButton;
    private readonly Button _isoButton;
    private readonly Button _topButton;
    private readonly List<PointF> _linePoints = new();
    private readonly List<PointF> _stepPoints = new();
    private GraphPlotView3D _view = Graph3DApi.From2D(new GraphPlotView(-100, 100, -100, 100), -100, 100);
    private GraphCamera3D _camera = Graph3DApi.DefaultCamera;
    private string _disabledMessage = "";
    private string _pointerText = "";
    private bool _dragging;
    private MouseButtons _dragButton;
    private bool _draggingPointsPanel;
    private bool _flat2DMode;
    private bool _pointTableRequestedVisible = true;
    private Point _lastMouse;
    private Point _pointsPanelDragStart;
    private Point _pointsPanelStartLocation;
    private double _panStartMinX;
    private double _panStartMaxX;
    private double _panStartMinY;
    private double _panStartMaxY;
    private double _gridStep = 10d;
    private bool _applyingData;
    private bool IsDarkTheme => _uiTheme == ToolEditorUiTheme.Dark;
    private Color WindowBackColor => IsDarkTheme ? Color.FromArgb(18, 24, 32) : SystemColors.Control;
    private Color PanelBackColor => IsDarkTheme ? Color.FromArgb(17, 24, 39) : Color.FromArgb(250, 250, 250);
    private Color ToolbarBackColor => IsDarkTheme ? Color.FromArgb(31, 41, 55) : Color.FromArgb(250, 250, 250);
    private Color GraphHostBackColor => IsDarkTheme ? Color.FromArgb(15, 23, 42) : Color.White;
    private Color TextColor => IsDarkTheme ? Color.FromArgb(226, 232, 240) : SystemColors.ControlText;
    private Color EditorBackColor => IsDarkTheme ? Color.FromArgb(39, 39, 39) : SystemColors.Window;
    private Color ButtonBackColor => IsDarkTheme ? Color.FromArgb(30, 41, 59) : Color.White;
    private Color ButtonActiveBackColor => IsDarkTheme ? Color.FromArgb(30, 64, 175) : Color.FromArgb(219, 234, 254);
    private Color ButtonBorderColor => IsDarkTheme ? Color.FromArgb(71, 85, 105) : Color.FromArgb(203, 216, 234);
    private Color ButtonActiveBorderColor => IsDarkTheme ? Color.FromArgb(96, 165, 250) : Color.FromArgb(59, 130, 246);

    public Graph3DPreviewForm()
    {
        _uiTheme = ToolEditorUiThemeSettings.Load();
        GraphOverlayStyle.UseDarkTheme = IsDarkTheme;
        Text = "Graph 3D";
        AppWindowIcon.ApplyTo(this);
        BackColor = WindowBackColor;
        ForeColor = TextColor;
        Width = 1020;
        Height = 620;
        MinimumSize = new Size(760, 440);
        ShowInTaskbar = false;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(10),
            BackColor = PanelBackColor,
            ForeColor = TextColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 12,
            RowCount = 3,
            Padding = new Padding(0, 4, 0, 4),
            BackColor = ToolbarBackColor,
            ForeColor = TextColor
        };
        for (var i = 0; i < 12; i++)
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, i % 2 == 0 ? 54 : 86));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        toolbar.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        toolbar.Controls.Add(MakeToolbarLabel("X min"), 0, 0);
        _xMin = MakeNumberBox(-100, -GraphRangeLimit, GraphRangeLimit, 1);
        _xMin.ValueChanged += (_, _) => ApplyRangeFromControls();
        toolbar.Controls.Add(_xMin, 1, 0);
        toolbar.Controls.Add(MakeToolbarLabel("X max"), 2, 0);
        _xMax = MakeNumberBox(100, -GraphRangeLimit, GraphRangeLimit, 1);
        _xMax.ValueChanged += (_, _) => ApplyRangeFromControls();
        toolbar.Controls.Add(_xMax, 3, 0);
        toolbar.Controls.Add(MakeToolbarLabel("Step"), 4, 0);
        _step = MakeNumberBox(1, GraphStepMinimum, GraphRangeLimit, 1);
        _step.ValueChanged += (_, _) =>
        {
            if (!_applyingData)
                NotifySyncStateChanged();
        };
        toolbar.Controls.Add(_step, 5, 0);
        toolbar.Controls.Add(MakeToolbarLabel("Z min"), 6, 0);
        _zMin = MakeNumberBox(-100, -GraphRangeLimit, GraphRangeLimit, 1);
        _zMin.ValueChanged += (_, _) => ApplyRangeFromControls();
        toolbar.Controls.Add(_zMin, 7, 0);
        toolbar.Controls.Add(MakeToolbarLabel("Z max"), 8, 0);
        _zMax = MakeNumberBox(100, -GraphRangeLimit, GraphRangeLimit, 1);
        _zMax.ValueChanged += (_, _) => ApplyRangeFromControls();
        toolbar.Controls.Add(_zMax, 9, 0);

        toolbar.Controls.Add(MakeToolbarLabel("Y min"), 0, 1);
        _yMin = MakeNumberBox(-100, -GraphRangeLimit, GraphRangeLimit, 1);
        _yMin.ValueChanged += (_, _) => ApplyRangeFromControls();
        toolbar.Controls.Add(_yMin, 1, 1);
        toolbar.Controls.Add(MakeToolbarLabel("Y max"), 2, 1);
        _yMax = MakeNumberBox(100, -GraphRangeLimit, GraphRangeLimit, 1);
        _yMax.ValueChanged += (_, _) => ApplyRangeFromControls();
        toolbar.Controls.Add(_yMax, 3, 1);
        toolbar.Controls.Add(MakeToolbarLabel("Grid step"), 4, 1);
        _gridStepBox = MakeNumberBox(10, GraphStepMinimum, GraphRangeLimit, 1);
        _gridStepBox.ValueChanged += (_, _) => ApplyGridStepFromControl();
        toolbar.Controls.Add(_gridStepBox, 5, 1);

        _flat2DButton = MakeModeButton("2D", ToggleFlat2DMode);
        toolbar.Controls.Add(_flat2DButton, 0, 2);
        _isoButton = MakeModeButton("3D", () => SetCamera(GraphCameraPreset3D.Isometric));
        toolbar.Controls.Add(_isoButton, 1, 2);
        _topButton = MakeModeButton("Top", () => SetCamera(GraphCameraPreset3D.Top));
        toolbar.Controls.Add(_topButton, 2, 2);
        _grid = new CheckBox { Text = "Grid", Checked = true, AutoSize = true, Margin = new Padding(12, 7, 6, 0), BackColor = ToolbarBackColor, ForeColor = TextColor };
        _grid.CheckedChanged += (_, _) =>
        {
            InvalidateCanvas();
            NotifySyncStateChanged();
        };
        toolbar.Controls.Add(_grid, 3, 2);
        _lines = new CheckBox { Text = "Lines", Checked = true, AutoSize = true, Margin = new Padding(6, 7, 6, 0), BackColor = ToolbarBackColor, ForeColor = TextColor };
        _lines.CheckedChanged += (_, _) =>
        {
            UpdateRangeInputMode();
            if (_lines.Checked)
                Restore3DRangeControls();
            else
                Update3DViewportRangeControlsIfNeeded();
            InvalidateCanvas();
            NotifySyncStateChanged();
        };
        toolbar.Controls.Add(_lines, 4, 2);

        var copy = new GraphToolbarIconButton(GraphToolbarIcon.Copy, "Copy 3D points")
        {
            Width = 44,
            Height = 28,
            Margin = new Padding(10, 1, 4, 0)
        };
        copy.Click += (_, _) => CopyPoints();
        toolbar.Controls.Add(copy, 5, 2);
        _toggleTableButton = new GraphToolbarIconButton(GraphToolbarIcon.TableHidden, "Hide table")
        {
            Width = 44,
            Height = 28,
            Margin = new Padding(4, 1, 4, 0)
        };
        _toggleTableButton.Click += (_, _) => TogglePointTable();
        toolbar.Controls.Add(_toggleTableButton, 6, 2);
        root.Controls.Add(toolbar, 0, 0);

        var host = new Panel { Dock = DockStyle.Fill, BackColor = GraphHostBackColor };
        _canvas = new GraphCanvasPanel { Dock = DockStyle.Fill, BackColor = GraphHostBackColor };
        _canvas.Paint += Canvas_Paint;
        _canvas.MouseDown += Canvas_MouseDown;
        _canvas.MouseMove += Canvas_MouseMove;
        _canvas.MouseUp += Canvas_MouseUp;
        _canvas.MouseWheel += Canvas_MouseWheel;
        _canvas.MouseEnter += (_, _) =>
        {
            _canvas.Focus();
            _canvas.Cursor = GraphCursors.Pan;
        };
        _canvas.MouseLeave += (_, _) =>
        {
            if (!_dragging)
                _canvas.Cursor = GraphCursors.Default;
        };
        host.Controls.Add(_canvas);

        _navigationPanel = GraphOverlayButton.CreateNavigationGroup(
            GraphOverlayButtonDensity.Normal,
            "Home / reset camera",
            (_, _) => SetCamera(GraphCameraPreset3D.Isometric),
            "Zoom in",
            (_, _) => ZoomCamera(1.15d),
            "Zoom out",
            (_, _) => ZoomCamera(1d / 1.15d));
        host.Controls.Add(_navigationPanel);
        _navigationPanel.BringToFront();

        _rotationDial = new Graph3DRotationDial { Width = 88, Height = 88, Camera = _camera };
        _rotationDial.RotationDeltaRequested += (yaw, pitch) => RotateCamera(yaw, pitch);
        _rotationDial.ResetRequested += () => SetCamera(GraphCameraPreset3D.Isometric);

        var overlay = GraphPointTableOverlay.Create(
            GraphOverlayButtonDensity.Normal,
            "3D Points",
            () => _pointerText,
            () => SetPointTableVisible(false),
            PointsPanel_MouseDown,
            PointsPanel_MouseMove,
            PointsPanel_MouseUp,
            new[]
            {
                new GraphPointTableColumn("x", "x", 60),
                new GraphPointTableColumn("y", "y", 60),
                new GraphPointTableColumn("z", "z", 60)
            },
            tableWidthOverride: 200,
            tableHeightOverride: 110,
            titleProvider: () => _flat2DMode ? "2D Points" : "3D Points");
        _pointsPanel = overlay.Overlay;
        _pointsTitleBar = overlay.TitleBar;
        _pointsGrid = overlay.Table;
        host.Controls.Add(_pointsPanel);
        _pointsPanel.BringToFront();
        host.Resize += (_, _) =>
        {
            PlaceOverlays(host);
            _canvas.Invalidate();
        };
        root.Controls.Add(host, 0, 1);
        Controls.Add(root);
        ApplyThemeToChildren(this);
        PlaceOverlays(host);
        UpdateModeButtons();
        UpdatePointTableAvailability();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ToolEditorUiThemeSettings.ApplyNativeWindowTheme(this, _uiTheme);
    }

    internal event EventHandler<Graph3DPreviewSyncState>? SyncStateChanged;

    public void SetData(
        IReadOnlyList<PointF> linePoints,
        IReadOnlyList<PointF> stepPoints,
        GraphPlotView3D view,
        GraphPlotView markerView,
        GraphCamera3D camera,
        double step,
        double gridStep,
        bool showGrid,
        bool showLines,
        string disabledMessage)
    {
        _linePoints.Clear();
        _linePoints.AddRange(linePoints);
        _stepPoints.Clear();
        _stepPoints.AddRange(stepPoints);
        _applyingData = true;
        _camera = camera;
        _flat2DMode = IsFlat2DCamera(camera);
        try
        {
            _view = view;
            var controlView = _flat2DMode ? markerView : new GraphPlotView(view.MinX, view.MaxX, view.MinY, view.MaxY);
            SetNumberBox(_xMin, controlView.MinX);
            SetNumberBox(_xMax, controlView.MaxX);
            SetNumberBox(_yMin, controlView.MinY);
            SetNumberBox(_yMax, controlView.MaxY);
            SetNumberBox(_zMin, view.MinZ);
            SetNumberBox(_zMax, view.MaxZ);
            SetNumberBox(_step, step);
            SetNumberBox(_gridStepBox, gridStep);
            _grid.Checked = showGrid;
            _lines.Checked = showLines;
        }
        finally
        {
            _applyingData = false;
        }
        _gridStep = gridStep;
        _disabledMessage = disabledMessage;
        _rotationDial.Camera = _camera;
        UpdateModeButtons();
        FillPoints();
        _canvas.Invalidate();
    }

    private void NotifySyncStateChanged()
    {
        if (_applyingData)
            return;

        SyncStateChanged?.Invoke(this, new Graph3DPreviewSyncState(
            _view,
            GetMarkerViewFromControls(),
            _flat2DMode ? Graph3DApi.CameraPreset(GraphCameraPreset3D.Front) : _camera,
            GetNumberBoxValue(_step),
            _gridStep,
            _grid.Checked,
            _lines.Checked));
    }

    public void InvalidateGraphData()
    {
        _linePoints.Clear();
        _stepPoints.Clear();
        _disabledMessage = "";
        FillPoints();
        _canvas.Invalidate();
    }

    private Button MakeModeButton(string text, Action action)
    {
        var button = new Button
        {
            Text = text,
            Width = 48,
            Height = 28,
            Margin = new Padding(0, 1, 4, 0),
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            BackColor = ButtonBackColor,
            ForeColor = GraphOverlayStyle.TitleText,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold)
        };
        button.FlatAppearance.BorderColor = ButtonBorderColor;
        button.FlatAppearance.MouseOverBackColor = IsDarkTheme ? Color.FromArgb(51, 65, 85) : Color.FromArgb(239, 246, 255);
        button.FlatAppearance.MouseDownBackColor = ButtonActiveBackColor;
        button.Click += (_, _) => action();
        return button;
    }

    private Label MakeToolbarLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(4, 6, 2, 0),
            BackColor = ToolbarBackColor,
            ForeColor = TextColor
        };
    }

    private static NumericUpDown MakeNumberBox(decimal value, decimal minimum, decimal maximum, decimal increment)
    {
        return new NumericUpDown
        {
            DecimalPlaces = 1,
            Minimum = minimum,
            Maximum = maximum,
            Increment = increment,
            Value = value,
            Anchor = AnchorStyles.Left,
            Width = 82,
            Margin = new Padding(0, 2, 4, 0)
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
                case Label label:
                    label.BackColor = ToolbarBackColor;
                    label.ForeColor = TextColor;
                    break;
                case TableLayoutPanel table:
                    table.BackColor = ReferenceEquals(table.Parent, this) ? PanelBackColor : ToolbarBackColor;
                    table.ForeColor = TextColor;
                    break;
                case Panel panel when !ReferenceEquals(panel, _canvas):
                    panel.BackColor = ReferenceEquals(panel, _pointsPanel) || ReferenceEquals(panel, _navigationPanel)
                        ? GraphOverlayStyle.PanelFill(translucent: false)
                        : GraphHostBackColor;
                    panel.ForeColor = TextColor;
                    break;
            }

            ApplyThemeToChildren(child);
        }

        _pointsTitleBar.BackColor = GraphOverlayStyle.TitleFill;
        _pointsTitleBar.Invalidate();
        _rotationDial.BackColor = GraphOverlayStyle.PanelFill(translucent: false);
    }

    private static void ApplyGridTheme(DataGridView grid)
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

    private void ApplyRangeFromControls()
    {
        if (_applyingData)
            return;

        var minX = GetNumberBoxValue(_xMin);
        var maxX = GetNumberBoxValue(_xMax);
        var minY = GetNumberBoxValue(_yMin);
        var maxY = GetNumberBoxValue(_yMax);
        var minZ = GetNumberBoxValue(_zMin);
        var maxZ = GetNumberBoxValue(_zMax);
        if (minX >= maxX || minY >= maxY)
            return;

        if (_flat2DMode)
        {
            _canvas.Invalidate();
            NotifySyncStateChanged();
            return;
        }
        else if (minZ > maxZ)
        {
            return;
        }
        else if (Math.Abs(maxZ - minZ) < GraphSurfaceApi.MinimumViewSpan)
        {
            var z = ExpandFlat3DRange(minZ, maxZ);
            minZ = z.Min;
            maxZ = z.Max;
            Set3DRangeControls(new GraphPlotView3D(minX, maxX, minY, maxY, minZ, maxZ));
        }

        _view = new GraphPlotView3D(minX, maxX, minY, maxY, minZ, maxZ);
        _canvas.Invalidate();
        NotifySyncStateChanged();
    }

    private void ApplyGridStepFromControl()
    {
        if (_applyingData)
            return;

        var value = GetNumberBoxValue(_gridStepBox);
        if (value <= 0d)
            return;

        _gridStep = value;
        _canvas.Invalidate();
        NotifySyncStateChanged();
    }

    private static double GetNumberBoxValue(NumericUpDown box)
    {
        return (double)box.Value;
    }

    private GraphPlotView GetMarkerViewFromControls()
    {
        var minX = GetNumberBoxValue(_xMin);
        var maxX = GetNumberBoxValue(_xMax);
        var minY = GetNumberBoxValue(_yMin);
        var maxY = GetNumberBoxValue(_yMax);
        if (minX >= maxX || minY >= maxY)
            return Get2DView();

        return new GraphPlotView(minX, maxX, minY, maxY);
    }

    private static void SetNumberBox(NumericUpDown box, double value)
    {
        if (!double.IsFinite(value))
            return;

        var clamped = Math.Clamp((decimal)value, box.Minimum, box.Maximum);
        if (box.Value != clamped)
            box.Value = clamped;
    }

    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.Clear(_canvas.BackColor);
        if (_flat2DMode)
        {
            var view = Get2DView();
            GraphSurfaceApi.Draw(
                e.Graphics,
                _canvas,
                _linePoints,
                _stepPoints,
                view,
                GetNumberBoxValue(_xMin),
                GetNumberBoxValue(_xMax),
                GetNumberBoxValue(_step),
                _disabledMessage,
                "Generate graph",
                GraphPlotDensity.Normal,
                (float)GetNumberBoxValue(_yMin),
                (float)GetNumberBoxValue(_yMax),
                _lines.Checked);
            return;
        }

        Graph3DApi.Draw(
            e.Graphics,
            _canvas,
            _linePoints,
            _stepPoints,
            _view,
            _camera,
            _grid.Checked,
            _lines.Checked,
            showLines: true,
            _gridStep,
            _disabledMessage,
            "Generate graph",
            GraphPlotDensity.Normal);
        Graph3DApi.DrawCompass(e.Graphics, _rotationDial.Bounds, _camera);
        Graph3DApi.DrawCompassDegrees(e.Graphics, _rotationDial.Bounds, _camera);
    }

    private void InvalidateCanvas()
    {
        if (!_canvas.IsDisposed)
            _canvas.Invalidate();
    }

    private void TogglePointTable()
    {
        SetPointTableVisible(!_pointsPanel.Visible);
    }

    private void Canvas_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button is not (MouseButtons.Left or MouseButtons.Right))
            return;

        _dragging = true;
        _dragButton = e.Button;
        _lastMouse = e.Location;
        if (_flat2DMode || e.Button == MouseButtons.Right)
        {
            var view = Get2DView();
            _panStartMinX = view.MinX;
            _panStartMaxX = view.MaxX;
            _panStartMinY = view.MinY;
            _panStartMaxY = view.MaxY;
        }
        _canvas.Cursor = GraphCursors.Pan;
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging)
            return;

        if (_flat2DMode)
        {
            PanFlat2D(e.Location);
            return;
        }

        if (_dragButton == MouseButtons.Right)
        {
            PanCamera(e.Location);
            return;
        }

        var dx = e.X - _lastMouse.X;
        var dy = e.Y - _lastMouse.Y;
        _lastMouse = e.Location;
        RotateCamera(dx * 0.6d, -dy * 0.45d);
    }

    private void Canvas_MouseUp(object? sender, MouseEventArgs e)
    {
        _dragging = false;
        _dragButton = MouseButtons.None;
        _canvas.Cursor = _canvas.ClientRectangle.Contains(e.Location) ? GraphCursors.Pan : GraphCursors.Default;
    }

    private void Canvas_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (_flat2DMode)
        {
            ZoomFlat2D(e.Delta > 0 ? 0.85d : 1.18d, e.Location);
            return;
        }

        ZoomCamera(e.Delta > 0 ? 1.12d : 1d / 1.12d);
    }

    private void SetCamera(GraphCameraPreset3D preset)
    {
        _flat2DMode = false;
        _camera = Graph3DApi.CameraPreset(preset);
        _rotationDial.Camera = _camera;
        UpdateModeButtons();
        _canvas.Invalidate();
        NotifySyncStateChanged();
    }

    private void ToggleFlat2DMode()
    {
        _flat2DMode = !_flat2DMode;
        if (!_flat2DMode)
        {
            _camera = Graph3DApi.CameraPreset(GraphCameraPreset3D.Isometric);
            _rotationDial.Camera = _camera;
        }

        UpdateModeButtons();
        _canvas.Invalidate();
        NotifySyncStateChanged();
    }

    private void UpdateModeButtons()
    {
        PaintModeButtonState(_flat2DButton, _flat2DMode);
        PaintModeButtonState(_isoButton, !_flat2DMode && Math.Abs(_camera.YawDegrees - Graph3DApi.CameraPreset(GraphCameraPreset3D.Isometric).YawDegrees) < 0.001d);
        PaintModeButtonState(_topButton, !_flat2DMode && Math.Abs(_camera.PitchDegrees - Graph3DApi.CameraPreset(GraphCameraPreset3D.Top).PitchDegrees) < 0.001d);
        _rotationDial.Visible = !_flat2DMode;
        UpdateRangeInputMode();
        UpdatePointTableMode();
    }

    private void UpdateRangeInputMode()
    {
        var editable = _lines.Checked;
        foreach (var box in new[] { _xMin, _xMax, _yMin, _yMax, _step })
        {
            box.Enabled = editable;
            box.ReadOnly = !editable;
        }

        var zEditable = editable && !_flat2DMode;
        foreach (var box in new[] { _zMin, _zMax, _gridStepBox })
        {
            box.Enabled = zEditable;
            box.ReadOnly = !zEditable;
        }

        _grid.Enabled = !_flat2DMode;
        if (_flat2DMode)
        {
            _applyingData = true;
            try
            {
                SetNumberBox(_zMin, 0d);
                SetNumberBox(_zMax, 0d);
            }
            finally
            {
                _applyingData = false;
            }
        }
    }

    private static bool IsFlat2DCamera(GraphCamera3D camera)
    {
        return Math.Abs(NormalizeAngle(camera.YawDegrees)) < 0.0001d &&
               Math.Abs(NormalizeAngle(camera.PitchDegrees)) < 0.0001d;
    }

    private static double NormalizeAngle(double angle)
    {
        var normalized = angle % 360d;
        if (normalized > 180d)
            normalized -= 360d;
        if (normalized < -180d)
            normalized += 360d;
        return normalized;
    }

    private void PaintModeButtonState(Button button, bool active)
    {
        button.BackColor = active ? ButtonActiveBackColor : ButtonBackColor;
        button.ForeColor = active && IsDarkTheme ? Color.White : GraphOverlayStyle.TitleText;
        button.FlatAppearance.BorderColor = active ? ButtonActiveBorderColor : ButtonBorderColor;
    }

    private void RotateCamera(double yaw, double pitch)
    {
        _camera = Graph3DApi.RotateCamera(_camera, yaw, pitch);
        _rotationDial.Camera = _camera;
        UpdateModeButtons();
        Update3DViewportRangeControlsIfNeeded();
        _canvas.Invalidate();
        NotifySyncStateChanged();
    }

    private void ZoomCamera(double factor)
    {
        _camera = Graph3DApi.ZoomCamera(_camera, factor);
        _rotationDial.Camera = _camera;
        Update3DViewportRangeControlsIfNeeded();
        _canvas.Invalidate();
        NotifySyncStateChanged();
    }

    private void Update3DViewportRangeControlsIfNeeded()
    {
        if (_lines.Checked)
            return;

        var plot = GraphSurfaceApi.GetPlotRectangle(_canvas);
        var view = Graph3DApi.CreateCameraAdjustedView(plot, _view, _camera);
        Set3DRangeControls(view);
    }

    private void Restore3DRangeControls()
    {
        Set3DRangeControls(_view);
    }

    private static (double Min, double Max) ExpandFlat3DRange(double min, double max)
    {
        var center = (min + max) / 2d;
        var half = Math.Max(100d, Math.Abs(center) * 0.1d);
        return (center - half, center + half);
    }

    private void Set3DRangeControls(GraphPlotView3D view)
    {
        _applyingData = true;
        try
        {
            SetNumberBox(_xMin, view.MinX);
            SetNumberBox(_xMax, view.MaxX);
            SetNumberBox(_yMin, view.MinY);
            SetNumberBox(_yMax, view.MaxY);
            SetNumberBox(_zMin, view.MinZ);
            SetNumberBox(_zMax, view.MaxZ);
        }
        finally
        {
            _applyingData = false;
        }
    }

    private GraphPlotView Get2DView()
    {
        return new GraphPlotView(_view.MinX, _view.MaxX, _view.MinY, _view.MaxY);
    }

    private void Set2DView(GraphPlotView view)
    {
        _view = new GraphPlotView3D(view.MinX, view.MaxX, view.MinY, view.MaxY, _view.MinZ, _view.MaxZ);
        if (_lines.Checked)
            return;

        _applyingData = true;
        try
        {
            SetNumberBox(_xMin, view.MinX);
            SetNumberBox(_xMax, view.MaxX);
            SetNumberBox(_yMin, view.MinY);
            SetNumberBox(_yMax, view.MaxY);
        }
        finally
        {
            _applyingData = false;
        }
    }

    private void ZoomFlat2D(double factor, PointF screenPoint)
    {
        var plot = GraphSurfaceApi.GetPlotRectangle(_canvas);
        if (plot.Width <= 0 || plot.Height <= 0)
            return;

        var view = Get2DView();
        if (!GraphSurfaceApi.IsValidView(view))
            return;

        var anchor = GraphSurfaceApi.ScreenToGraph(screenPoint, plot, view);
        var newWidth = (view.MaxX - view.MinX) * factor;
        var newHeight = (view.MaxY - view.MinY) * factor;
        if (newWidth < GraphSurfaceApi.MinimumViewSpan || newHeight < GraphSurfaceApi.MinimumViewSpan)
            return;

        var xRatio = (anchor.X - view.MinX) / (view.MaxX - view.MinX);
        var yRatio = (anchor.Y - view.MinY) / (view.MaxY - view.MinY);
        Set2DView(new GraphPlotView(
            anchor.X - newWidth * xRatio,
            anchor.X - newWidth * xRatio + newWidth,
            anchor.Y - newHeight * yRatio,
            anchor.Y - newHeight * yRatio + newHeight));
        _canvas.Invalidate();
        NotifySyncStateChanged();
    }

    private void PanFlat2D(Point location)
    {
        var plot = GraphSurfaceApi.GetPlotRectangle(_canvas);
        if (plot.Width <= 0 || plot.Height <= 0)
            return;

        var dx = location.X - _lastMouse.X;
        var dy = location.Y - _lastMouse.Y;
        var graphDx = dx / (double)plot.Width * (_panStartMaxX - _panStartMinX);
        var graphDy = dy / (double)plot.Height * (_panStartMaxY - _panStartMinY);
        Set2DView(new GraphPlotView(
            _panStartMinX - graphDx,
            _panStartMaxX - graphDx,
            _panStartMinY + graphDy,
            _panStartMaxY + graphDy));
        _canvas.Invalidate();
        NotifySyncStateChanged();
    }

    private void PanCamera(Point location)
    {
        var dx = location.X - _lastMouse.X;
        var dy = location.Y - _lastMouse.Y;
        _lastMouse = location;
        _camera = _camera with
        {
            PanX = _camera.PanX + dx,
            PanY = _camera.PanY + dy
        };
        _rotationDial.Camera = _camera;
        Update3DViewportRangeControlsIfNeeded();
        _canvas.Invalidate();
        NotifySyncStateChanged();
    }

    private void PlaceOverlays(Control host)
    {
        const int gap = 10;
        _navigationPanel.Left = Math.Max(gap, host.ClientSize.Width - _navigationPanel.Width - gap);
        _navigationPanel.Top = Math.Max(gap, host.ClientSize.Height - _navigationPanel.Height - gap);
        _rotationDial.Left = gap;
        _rotationDial.Top = Math.Max(gap, host.ClientSize.Height - _rotationDial.Height - gap - 16);
        _pointsPanel.Left = gap;
        _pointsPanel.Top = gap;
        _pointsPanel.BringToFront();
        _navigationPanel.BringToFront();
    }

    private void FillPoints()
    {
        UpdatePointTableMode();
        _pointsGrid.SuspendLayout();
        _pointsGrid.Rows.Clear();
        foreach (var point in _stepPoints)
            _pointsGrid.Rows.Add(Format(point.X), Format(point.Y), Format(0d));
        _pointsGrid.ClearSelection();
        _pointsGrid.ResumeLayout();
        _pointerText = "";
        _pointsTitleBar.Invalidate();
        UpdatePointTableAvailability();
    }

    private void UpdatePointTableAvailability()
    {
        var hasRows = _pointsGrid.Rows.Count > 0;
        _toggleTableButton.Enabled = hasRows;
        SetPointTableVisible(_pointTableRequestedVisible && hasRows);
    }

    private void SetPointTableVisible(bool visible)
    {
        _pointTableRequestedVisible = visible;
        var show = visible && _pointsGrid.Rows.Count > 0;
        _pointsPanel.Visible = show;
        if (_toggleTableButton is GraphToolbarIconButton iconButton)
        {
            iconButton.Icon = show ? GraphToolbarIcon.TableHidden : GraphToolbarIcon.TableVisible;
            iconButton.TooltipText = show ? "Hide table" : "Show table";
        }
        _canvas.Invalidate();
    }

    private void CopyPoints()
    {
        if (_stepPoints.Count == 0)
            return;

        var lines = _flat2DMode
            ? _stepPoints.Select(point => $"{Format(point.X)}\t{Format(point.Y)}")
            : _stepPoints.Select(point => $"{Format(point.X)}\t{Format(point.Y)}\t{Format(0d)}");
        Clipboard.SetText(string.Join(Environment.NewLine, lines));
    }

    private void UpdatePointTableMode()
    {
        if (_pointsGrid.Columns.Count < 3)
            return;

        var showZ = !_flat2DMode;
        _pointsGrid.Columns[2].Visible = showZ;
        _pointsPanel.Width = showZ ? 212 : 172;
        _pointsGrid.Width = showZ ? 200 : 160;
        _pointsGrid.Columns[0].Width = showZ ? 60 : 72;
        _pointsGrid.Columns[1].Width = showZ ? 60 : 72;
        _pointsGrid.Columns[2].Width = 60;
        _pointsTitleBar.Width = _pointsGrid.Width;
        _pointsTitleBar.Invalidate();
    }

    private void PointsPanel_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || sender is not Control control || _pointsPanel.Parent is null)
            return;

        _draggingPointsPanel = true;
        _pointsPanelDragStart = _pointsPanel.Parent.PointToClient(control.PointToScreen(e.Location));
        _pointsPanelStartLocation = _pointsPanel.Location;
        _pointsPanel.BringToFront();
    }

    private void PointsPanel_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_draggingPointsPanel || sender is not Control control || _pointsPanel.Parent is null)
            return;

        var current = _pointsPanel.Parent.PointToClient(control.PointToScreen(e.Location));
        _pointsPanel.Left = _pointsPanelStartLocation.X + current.X - _pointsPanelDragStart.X;
        _pointsPanel.Top = _pointsPanelStartLocation.Y + current.Y - _pointsPanelDragStart.Y;
    }

    private void PointsPanel_MouseUp(object? sender, MouseEventArgs e)
    {
        _draggingPointsPanel = false;
    }

    private static string Format(double value)
    {
        return GraphSurfaceApi.FormatDisplayNumber(value);
    }

    private sealed class GraphCanvasPanel : Panel
    {
        public GraphCanvasPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            Cursor = GraphCursors.Pan;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }
    }
}
