#nullable enable
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Win32;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Tiedragon.Graph;
using Tiedragon.Graph.G2D;
using Tiedragon.Graph.G3D;
using Tiedragon.Help;
using Tiedragon.NodSystem.Core;
using Tiedragon.ToolEditor;

namespace Syscalculator.UI.WinForms;

/// <summary>
/// NOD Editor Plus.
/// 
/// Toegevoegd:
/// - toolbar met iconen
/// - tabbladen voor meerdere .nod-bestanden
/// - syntax highlighting
/// - zoek/vervang-venster
/// - line-number gutter per tab
/// - statusbar zoals editor/Notepad++-achtig
/// </summary>
public sealed class NodEditorForm : Form
{
    private const int WmSetRedraw = 0x000B;

    private enum UnsavedChangesChoice { Save, Discard, Cancel }

    private const float GraphNormalHalfYRange = 5f;
    private const float Graph3DDefaultHalfRange = 100f;
    private const int DockedTestPanelWidth = 420;
    private const int MaxDockedLivePreviewWidth = 250;
    private const int DockedSimulatorWidth = 360;
    private const int NodHelpPopupMinWidth = 330;
    private const int NodHelpPopupMaxWidth = 520;
    private const int NodHelpPopupMinHeight = 72;
    private const int NodHelpPopupMaxHeight = 360;
    private const int RecentFilesLimit = 5;
    private const int PreviewUpdateDelayMs = 180;
    private const int GraphUpdateDelayMs = 360;
    private const int SyntaxHighlightDelayMs = 120;
    private const int PowerResumeQuietMs = 1600;
    private const int GraphPreviewMaxLineSamplePoints = 500;
    private const int GraphPreviewMaxVisibleStepPoints = 350;
    private const decimal GraphPreviewRangeLimit = 1_000_000_000_000_000_000_000_000m;
    private const decimal GraphPreviewStepMinimum = 0.0000000000000000000000000001m;
    private static readonly Color DarkWindowBackColor = Color.FromArgb(18, 24, 32);
    private static readonly Color DarkPanelBackColor = Color.FromArgb(31, 41, 55);
    private static readonly Color DarkEditorBackColor = Color.FromArgb(39, 39, 39);
    private static readonly Color DarkEditorTextColor = Color.FromArgb(226, 232, 240);
    private static readonly Color DarkMutedTextColor = Color.FromArgb(148, 163, 184);
    private static readonly Color DarkBorderColor = Color.FromArgb(55, 65, 81);
    private static readonly Color LightSyntaxCommandColor = Color.RoyalBlue;
    private static readonly Color LightSyntaxFunctionColor = Color.DarkCyan;
    private static readonly Color LightSyntaxNumberColor = Color.DarkOrange;
    private static readonly Color DarkSyntaxCommandColor = Color.FromArgb(96, 165, 250);
    private static readonly Color DarkSyntaxFunctionColor = Color.FromArgb(45, 212, 191);
    private static readonly Color DarkSyntaxNumberColor = Color.FromArgb(251, 191, 36);
    private static readonly Color DarkSyntaxCommentColor = Color.FromArgb(74, 222, 128);
    private static string RecentFilesConfigDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Tiedragon",
        "Syscalculator");
    private static string RecentFilesConfigPath => Path.Combine(RecentFilesConfigDirectory, "recent-nod-files.cfg");

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private sealed class EditorTab
    {
        public string? Path { get; set; }
        public bool Dirty { get; set; }
        public string CleanText { get; set; } = "";
        public TabPage Page { get; init; } = null!;
        public Panel HeaderPanel { get; init; } = null!;
        public Label HeaderTitle { get; init; } = null!;
        public Button HeaderCloseButton { get; init; } = null!;
        public Control Content { get; init; } = null!;
        public RichTextBox Editor { get; init; } = null!;
        public TextBox LineNumbers { get; init; } = null!;
        public string HistoryText { get; set; } = "";
        public int LastLineNumberCount { get; set; }
        public Stack<string> UndoTextStack { get; } = new();
        public Stack<string> RedoTextStack { get; } = new();
    }

    private sealed class NodHelpPopupPanel : Panel
    {
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool DarkMode { get; set; }

        public NodHelpPopupPanel()
        {
            DoubleBuffered = true;
            Padding = new Padding(24, 24, 8, 8);
            BackColor = Color.Transparent;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint |
                ControlStyles.ContainerControl |
                ControlStyles.SupportsTransparentBackColor,
                true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = CreateNodHelpBubblePath(Width, Height, inset: 2.2f);
            using var fill = new SolidBrush(DarkMode ? Color.FromArgb(17, 24, 39) : Color.FromArgb(247, 251, 255));
            using var border = new Pen(DarkMode ? Color.FromArgb(59, 130, 246) : Color.FromArgb(115, 164, 232), 2.3f);
            PaintNodHelpBubbleShadow(e.Graphics, path);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            using var path = CreateNodHelpBubblePath(Math.Max(1, Width), Math.Max(1, Height), inset: 0.5f);
            using var shadowPath = (GraphicsPath)path.Clone();
            using (var matrix = new Matrix())
            {
                matrix.Translate(3.2f, 3.6f);
                shadowPath.Transform(matrix);
            }

            var region = new Region(path);
            region.Union(shadowPath);
            Region = region;
        }

        private static void PaintNodHelpBubbleShadow(Graphics graphics, GraphicsPath path)
        {
            using var ambientShadowPath = (GraphicsPath)path.Clone();
            using var farShadowPath = (GraphicsPath)path.Clone();
            using var nearShadowPath = (GraphicsPath)path.Clone();
            using var ambientMatrix = new Matrix();
            using var farMatrix = new Matrix();
            using var nearMatrix = new Matrix();
            ambientMatrix.Translate(3.2f, 3.6f);
            farMatrix.Translate(2f, 2.2f);
            nearMatrix.Translate(0.9f, 1f);
            ambientShadowPath.Transform(ambientMatrix);
            farShadowPath.Transform(farMatrix);
            nearShadowPath.Transform(nearMatrix);

            using var ambientShadow = new SolidBrush(Color.FromArgb(7, 16, 38, 70));
            using var farShadow = new SolidBrush(Color.FromArgb(13, 10, 24, 45));
            using var nearShadow = new SolidBrush(Color.FromArgb(24, 0, 0, 0));
            graphics.FillPath(ambientShadow, ambientShadowPath);
            graphics.FillPath(farShadow, farShadowPath);
            graphics.FillPath(nearShadow, nearShadowPath);
        }

        private static GraphicsPath CreateNodHelpBubblePath(int width, int height, float inset)
        {
            var left = 20f + inset;
            var top = 18f + inset;
            var right = Math.Max(left + 80f, width - 4f - inset);
            var bottom = Math.Max(top + 60f, height - 4f - inset);
            var radius = 14f;
            var tailTip = new PointF(4f + inset, 5f + inset);
            var topStart = new PointF(left + radius, top);
            var leftJoin = new PointF(left, top + 42f);

            var path = new GraphicsPath();
            path.StartFigure();
            path.AddBezier(tailTip, new PointF(10f, 12f), new PointF(20f, 18f), topStart);
            path.AddLine(topStart.X, topStart.Y, right - radius, top);
            path.AddBezier(right - radius, top, right, top, right, top + radius, right, top + radius);
            path.AddLine(right, top + radius, right, bottom - radius);
            path.AddBezier(right, bottom - radius, right, bottom, right - radius, bottom, right - radius, bottom);
            path.AddLine(right - radius, bottom, left + radius, bottom);
            path.AddBezier(left + radius, bottom, left, bottom, left, bottom - radius, left, bottom - radius);
            path.AddLine(left, bottom - radius, left, leftJoin.Y);
            path.AddBezier(leftJoin, new PointF(left + 1f, top + 28f), new PointF(12f, 11f), tailTip);
            path.CloseFigure();
            return path;
        }
    }

    private sealed class EditorContentPanel : Panel
    {
        public EditorContentPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var border = new Pen(Color.FromArgb(205, 212, 222));
            e.Graphics.DrawLine(border, 0, 0, 0, Height - 1);
            e.Graphics.DrawLine(border, Width - 1, 0, Width - 1, Height - 1);
            e.Graphics.DrawLine(border, 0, Height - 1, Width - 1, Height - 1);
        }
    }

    private sealed class GraphCanvasPanel : Panel
    {
        public GraphCanvasPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            Cursor = GraphCursors.Pan;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
            UpdateStyles();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // The graph renderers clear the full canvas; skipping the default erase avoids zoom flicker.
        }
    }

    private sealed class Graph3DOverlayFlowPanel : FlowLayoutPanel
    {
        public Graph3DOverlayFlowPanel()
        {
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
            BackColor = Color.White;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            using var path = GraphOverlayStyle.RoundedRect(
                new RectangleF(0, 0, Math.Max(1, Width), Math.Max(1, Height)),
                10f);
            Region = new Region(path);
        }
    }

    private sealed class Graph3DOverlayTablePanel : TableLayoutPanel
    {
        public Graph3DOverlayTablePanel()
        {
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
            BackColor = Color.White;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            using var path = GraphOverlayStyle.RoundedRect(
                new RectangleF(0, 0, Math.Max(1, Width), Math.Max(1, Height)),
                10f);
            Region = new Region(path);
        }
    }

    private static GraphicsPath RoundedTopRect(Rectangle rectangle, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
        path.AddLine(rectangle.Right, rectangle.Top + radius, rectangle.Right, rectangle.Bottom);
        path.AddLine(rectangle.Right, rectangle.Bottom, rectangle.Left, rectangle.Bottom);
        path.AddLine(rectangle.Left, rectangle.Bottom, rectangle.Left, rectangle.Top + radius);
        path.CloseFigure();
        return path;
    }

    private readonly Dictionary<TabPage, EditorTab> _tabs = new();
    private FlowLayoutPanel _editorTabStrip = null!;
    private LanguageCatalog _language;
    private readonly LanguageCatalog _englishLanguage;
    private bool _highlighting;
    private bool _applyingTextHistory;
    private bool _suppressNodHelpUpdates;
    private readonly System.Windows.Forms.Timer _syntaxHighlightTimer = new() { Interval = SyntaxHighlightDelayMs };
    private readonly System.Windows.Forms.Timer _previewUpdateTimer = new() { Interval = PreviewUpdateDelayMs };
    private readonly System.Windows.Forms.Timer _graphUpdateTimer = new() { Interval = GraphUpdateDelayMs };
    private readonly System.Windows.Forms.Timer _powerResumeTimer = new() { Interval = PowerResumeQuietMs };
    private EditorTab? _pendingHighlightTab;
    private bool _pendingPreviewGraphUpdate;
    private bool _powerResumePreviewPending;
    private bool _powerResumeGraphPending;
    private DateTime _powerResumeQuietUntilUtc = DateTime.MinValue;
    private Panel _nodHelpPopup = null!;
    private WebView2 _nodHelpBrowser = null!;
    private RichTextBox? _nodHelpTargetEditor;
    private string? _lastNodHelpKeyword;
    private string? _pendingNodHelpHtml;
    private string? _lastRenderedNodHelpHtml;
    private int _pendingNodHelpWidth = NodHelpPopupMaxWidth;
    private bool _nodHelpBrowserFailed;

    private MenuStrip _menuStrip = null!;
    private ToolStrip _toolStrip = null!;
    private ToolStripButton _saveButton = null!;
    private ToolStripButton _undoButton = null!;
    private ToolStripButton _redoButton = null!;
    private ToolStripMenuItem _undoMenuItem = null!;
    private ToolStripMenuItem _redoMenuItem = null!;
    private ToolStripMenuItem _recentFilesMenuItem = null!;
    private ToolStripMenuItem _viewTestPanelItem = null!;
    private ToolStripMenuItem _viewLivePreviewItem = null!;
    private ToolStripMenuItem _viewSimulatorItem = null!;
    private ToolStripMenuItem _viewRestorePanelsItem = null!;
    private TableLayoutPanel _rootLayout = null!;
    private Panel _topStripPanel = null!;
    private TabControl _tabControl = null!;
    private Panel _editorContentPanel = null!;
    private TabPage? _selectedEditorPage;
    private SplitContainer _mainSplit = null!;
    private TextBox _testInput = null!;
    private TextBox _testOutput = null!;
    private Label _testInputLabel = null!;
    private Label _testOutputLabel = null!;
    private Label _testFormulaLabel = null!;
    private Label _testCalculationLabel = null!;
    private Button _testButton = null!;
    private Button _validateButton = null!;
    private TabPage _converterTestPage = null!;
    private TabPage _graphPreviewPage = null!;
    private TabPage _graph3DPage = null!;
    private HtmlMathPreviewControl _testFormulaView = null!;
    private HtmlMathPreviewControl _testCalculationView = null!;
    private TabControl _testTabs = null!;
    private NumericUpDown _graphXMin = null!;
    private NumericUpDown _graphXMax = null!;
    private NumericUpDown _graphYMin = null!;
    private NumericUpDown _graphYMax = null!;
    private NumericUpDown _graph3DXMin = null!;
    private NumericUpDown _graph3DXMax = null!;
    private NumericUpDown _graph3DYMin = null!;
    private NumericUpDown _graph3DYMax = null!;
    private NumericUpDown _graph3DStep = null!;
    private NumericUpDown _graphZMin = null!;
    private NumericUpDown _graphZMax = null!;
    private NumericUpDown _graphZStep = null!;
    private NumericUpDown _graphStep = null!;
    private CheckBox _graphShowRangeLines = null!;
    private CheckBox _graph3DShowRangeLines = null!;
    private Panel _graphCanvas = null!;
    private Panel _graph3DCanvas = null!;
    private Panel _graph3DNavigationPanel = null!;
    private Panel _graph3DCommandPanel = null!;
    private Panel _graph3DRangePanel = null!;
    private GraphTextOverlayButton _graph3DFlat2DButton = null!;
    private GraphTextOverlayButton _graph3DIsoButton = null!;
    private GraphTextOverlayButton _graph3DTopButton = null!;
    private Panel _graphPointPanel = null!;
    private Panel _graph3DPointPanel = null!;
    private DataGridView _graphPointTable = null!;
    private DataGridView _graph3DPointTable = null!;
    private Panel _graphPointTitleBar = null!;
    private Panel _graph3DPointTitleBar = null!;
    private Panel _graphPointerStatusPanel = null!;
    private Label _graphPointerStatusLabel = null!;
    private Label _graphStatus = null!;
    private Button _graphToggleTableButton = null!;
    private Button _graph3DToggleTableButton = null!;
    private CheckBox _graph3DGridToggle = null!;
    private readonly ToolTip _graphInputToolTip = new();
    private readonly HashSet<NumericUpDown> _editedGraphNumberBoxes = new();
    private readonly List<PointF> _graphPreviewFitPoints = new();
    private readonly List<PointF> _graphPreviewPoints = new();
    private readonly List<PointF> _graphPreviewStepPoints = new();
    private string _graphPointClipboardText = "";
    private string _graph3DPointClipboardText = "";
    private string _graphDisabledMessage = "";
    private string _graphDataTextSignature = "";
    private NodDocument? _graphPreviewDocument;
    private double _graphSampleMinX = -GraphNormalHalfYRange;
    private double _graphSampleMaxX = GraphNormalHalfYRange;
    private GraphPlotView _graphMarkerView = new(-GraphNormalHalfYRange, GraphNormalHalfYRange, -GraphNormalHalfYRange, GraphNormalHalfYRange);
    private string _graphPointerText = "";
    private string _graph3DPointerText = "";
    private bool _draggingGraphPointPanel;
    private bool _draggingGraph3DPointPanel;
    private bool _graphPointTableRequestedVisible = true;
    private bool _graph3DPointTableRequestedVisible = true;
    private bool _graph3DGridVisible = true;
    private bool _updatingGraphXRangeControls;
    private bool _updatingGraphYRangeControls;
    private bool _updatingGraphZRangeControls;
    private bool _updatingGraphZStepDisplay;
    private bool _applyingGraph3DRangeControls;
    private bool _applyingGraph3DPreviewSyncState;
    private bool _applyingGraphSyncState;
    private Point _graphPointPanelDragStart;
    private Point _graphPointPanelStartLocation;
    private Point _graph3DPointPanelDragStart;
    private Point _graph3DPointPanelStartLocation;
    private bool _graphHasView;
    private bool _graphPanning;
    private Point _graphPanStart;
    private double _graphViewMinX;
    private double _graphViewMaxX;
    private double _graphViewMinY;
    private double _graphViewMaxY;
    private double _graphPanStartMinX;
    private double _graphPanStartMaxX;
    private double _graphPanStartMinY;
    private double _graphPanStartMaxY;
    private double _graphUserStep = 1d;
    private double _graph3DViewMinX = -Graph3DDefaultHalfRange;
    private double _graph3DViewMaxX = Graph3DDefaultHalfRange;
    private double _graph3DViewMinY = -Graph3DDefaultHalfRange;
    private double _graph3DViewMaxY = Graph3DDefaultHalfRange;
    private bool _updatingGraphStepDisplay;
    private string _pendingFormulaMathMarkup = "";
    private string _pendingCalculationMathMarkup = "";
    private Label _previewDialogName = null!;
    private Label _previewInputName = null!;
    private Label _previewOutputName = null!;
    private Label _previewInputSample = null!;
    private Label _previewOutputSample = null!;
    private Label _previewFormat = null!;
    private Label _previewIntroLabel = null!;
    private TextBox _previewIntro = null!;
    private RowStyle _previewIntroLabelRow = null!;
    private RowStyle _previewIntroRow = null!;
    private SplitContainer _previewSplit = null!;
    private TableLayoutPanel _bottomPanel = null!;
    private GroupBox _testGroup = null!;
    private GroupBox _metadataPreviewGroup = null!;
    private GroupBox _simulatorPreviewGroup = null!;
    private FloatingToolForm? _floatingTestForm;
    private FloatingToolForm? _floatingPreviewForm;
    private FloatingToolForm? _floatingSimulatorForm;
    private GraphPreviewForm? _graphPreviewForm;
    private Graph3DPreviewForm? _graph3DPreviewForm;
    private GraphCamera3D _graph3DCamera = Graph3DApi.DefaultCamera;
    private Graph3DRotationDial _graph3DRotationDial = null!;
    private bool _graph3DDragging;
    private MouseButtons _graph3DDragButton;
    private Point _graph3DDragStart;
    private bool _solverPreviewActive;
    private bool _panelsHiddenForSolver;
    private bool _testPanelWasVisibleBeforeSolver;
    private bool _simulatorPanelWasVisibleBeforeSolver;
    private bool _simulatorIsFloating;
    private bool _simulatorDragStarted;
    private bool _suppressFloatingCloseHandler;
    private Point _simulatorDragStartScreen;
    private Panel _simWindow = null!;
    private MenuStrip _simMenuStrip = null!;
    private Label _simFileLabel = null!;
    private ComboBox _simFileBox = null!;
    private Label _simTitle = null!;
    private Label _simInputLabel = null!;
    private Label _simOutputLabel = null!;
    private TextBox _simInputBox = null!;
    private TextBox _simOutputBox = null!;
    private CheckBox _simDigitGroup = null!;
    private CheckBox _simDecimals = null!;
    private NumericUpDown _simDecimalCount = null!;

    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _statusLabel = null!;
    private ToolStripStatusLabel _positionLabel = null!;
    private ToolStripStatusLabel _lineCountLabel = null!;
    private ToolStripStatusLabel _encodingLabel = null!;
    private ToolStripStatusLabel _lineEndingLabel = null!;
    private ToolStripMenuItem? _darkThemeMenuItem;
    private ToolStripMenuItem? _classicThemeMenuItem;
    private ToolEditorUiTheme _uiTheme;
    private ToolEditorPalette _palette;

    private bool IsDarkTheme => _uiTheme == ToolEditorUiTheme.Dark;
    private Color WindowBackColor => IsDarkTheme ? DarkWindowBackColor : SystemColors.Control;
    private Color PanelBackColor => IsDarkTheme ? DarkPanelBackColor : SystemColors.Control;
    private Color ContentBackColor => IsDarkTheme ? Color.FromArgb(15, 23, 42) : Color.White;
    private Color EditorBackColor => IsDarkTheme ? DarkEditorBackColor : Color.White;
    private Color EditorTextColor => IsDarkTheme ? DarkEditorTextColor : Color.Black;
    private Color MutedTextColor => IsDarkTheme ? DarkMutedTextColor : Color.FromArgb(120, 120, 120);
    private Color BorderColor => IsDarkTheme ? DarkBorderColor : Color.FromArgb(205, 212, 222);
    private Color SoftPanelBackColor => IsDarkTheme ? Color.FromArgb(17, 24, 39) : Color.FromArgb(250, 250, 250);
    private Color PreviewCardBackColor => IsDarkTheme ? Color.FromArgb(30, 41, 59) : Color.FromArgb(239, 246, 255);
    private Color PreviewCardBorderColor => IsDarkTheme ? Color.FromArgb(51, 65, 85) : Color.FromArgb(199, 219, 248);
    private Color SyntaxCommandColor => IsDarkTheme ? DarkSyntaxCommandColor : LightSyntaxCommandColor;
    private Color SyntaxFunctionColor => IsDarkTheme ? DarkSyntaxFunctionColor : LightSyntaxFunctionColor;
    private Color SyntaxNumberColor => IsDarkTheme ? DarkSyntaxNumberColor : LightSyntaxNumberColor;
    private Color SyntaxCommentColor => IsDarkTheme ? DarkSyntaxCommentColor : Color.ForestGreen;
    private CoreWebView2PreferredColorScheme PreferredWebViewColorScheme =>
        IsDarkTheme ? CoreWebView2PreferredColorScheme.Dark : CoreWebView2PreferredColorScheme.Light;

    private static readonly string[] NodKeywords =
    [
        "Name", "URLN", "input1", "input2", "input", "inputr", "Result", "Resfou", "Symb1", "Symb2", "Symb3", "Symb4",
        "format", "mode", "math", "chg", "trans", "reverse", "field", "table",
        "output", "phoneformat", "lookup", "match", "given", "equation", "solve",
        "constraint", "preview", "backup", "indoprint", "indoend", "end"
    ];

    private static readonly HashSet<string> RepeatableNodKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "input", "math", "chg", "trans", "field", "match", "given", "constraint", "preview"
    };

    private readonly record struct NodCommandGroup(
        string Id,
        string TitleKey,
        string FallbackTitle,
        string ContentKey,
        string ContentFile,
        string[] Keywords);

    private static readonly NodCommandGroup[] NodCommandGroups =
    [
        new(
            "commands-basic",
            "editor.nod_help.page.commands.basic",
            "Basis en velden",
            "editor.nod_help.full.commands.basic",
            "nod/full/commands-basic.html",
            ["Name", "input", "inputr", "input1", "input2", "Result", "Resfou", "Symb1", "Symb2", "Symb3", "Symb4", "format"]),
        new(
            "commands-math",
            "editor.nod_help.page.commands.math",
            "Rekenen",
            "editor.nod_help.full.commands.math",
            "nod/full/commands-math.html",
            ["math", "reverse"]),
        new(
            "commands-text",
            "editor.nod_help.page.commands.text",
            "Tekst en vertaling",
            "editor.nod_help.full.commands.text",
            "nod/full/commands-text.html",
            ["trans", "chg"]),
        new(
            "commands-data",
            "editor.nod_help.page.commands.data",
            "Data",
            "editor.nod_help.full.commands.data",
            "nod/full/commands-data.html",
            ["table", "field", "output", "phoneformat", "lookup", "match"]),
        new(
            "commands-equation",
            "editor.nod_help.page.commands.equation",
            "Vergelijkingen",
            "editor.nod_help.full.commands.equation",
            "nod/full/commands-equation.html",
            ["given", "equation", "solve", "constraint"]),
        new(
            "commands-system",
            "editor.nod_help.page.commands.system",
            "NOD-systeem",
            "editor.nod_help.full.commands.system",
            "nod/full/commands-system.html",
            ["mode", "URLN", "preview", "backup", "indoprint", "indoend", "end"])
    ];

    // Startpunt van het editorvenster: bouwt de UI en opent eventueel direct een .nod-bestand.
    public NodEditorForm(string? path = null, bool openTemplateWizard = false)
    {
        _uiTheme = ToolEditorUiThemeSettings.Load();
        _palette = _uiTheme == ToolEditorUiTheme.Dark ? ToolEditorPalette.Dark : ToolEditorPalette.Default;
        GraphOverlayStyle.UseDarkTheme = IsDarkTheme;
        ToolEditorTabsApi.ConfigureTheme(_uiTheme);
        _englishLanguage = LanguageCatalog.Load(AppContext.BaseDirectory, "eng.lng");
        _language = LanguageCatalog.LoadConfigured(AppContext.BaseDirectory);

        Text = EditorWindowTitle();
        AppWindowIcon.ApplyTo(this);
        Width = 1100;
        Height = 780;
        MinimumSize = new Size(860, 580);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = WindowBackColor;
        _syntaxHighlightTimer.Tick += (_, _) =>
        {
            _syntaxHighlightTimer.Stop();
            var tab = _pendingHighlightTab;
            _pendingHighlightTab = null;
            if (tab is not null && !tab.Page.IsDisposed)
                HighlightSyntax(tab);
        };
        _previewUpdateTimer.Tick += (_, _) =>
        {
            _previewUpdateTimer.Stop();
            var updateGraph = _pendingPreviewGraphUpdate;
            _pendingPreviewGraphUpdate = false;
            UpdatePreviewFromCurrentText(updateGraph);
        };
        _graphUpdateTimer.Tick += (_, _) =>
        {
            _graphUpdateTimer.Stop();
            GenerateGraphPreview();
        };
        _powerResumeTimer.Tick += (_, _) =>
        {
            _powerResumeTimer.Stop();
            FlushPowerResumeWork();
        };
        SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

        BuildRootLayout();
        BuildTopStripPanel();
        BuildToolbar();
        BuildMenu();
        ArrangeTopStrips();
        BuildLayout();
        BuildStatusbar();
        BuildNodHelpPopup();
        ApplyNodEditorTheme();
        Deactivate += (_, _) => HideNodHelpPopup();
        Move += (_, _) => HideNodHelpPopup();
        Resize += (_, _) => HideNodHelpPopup();

        if (path is not null && File.Exists(path))
            OpenFileInNewTab(path);
        else
            AddBlankNewTab();

        UpdateUiState();

        if (openTemplateWizard)
            Shown += (_, _) => BeginInvoke(new Action(() => TemplateWizard_Click(this, EventArgs.Empty)));
    }

    private EditorTab? CurrentTab => _selectedEditorPage is not null && _tabs.TryGetValue(_selectedEditorPage, out var tab)
        ? tab
        : null;

    private RichTextBox? CurrentEditor => CurrentTab?.Editor;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        RefreshDirtyStateBeforeClose();
        if (HasUnsavedTabs())
        {
            var choice = ShowUnsavedChangesDialog(
                T("editor.dialog.close_editor_title", "Close NOD Editor"),
                T("editor.dialog.close_editor_dirty", "There are unsaved NOD documents."));

            if (choice == UnsavedChangesChoice.Cancel ||
                (choice == UnsavedChangesChoice.Save && !SaveDirtyTabsBeforeClose()))
            {
                e.Cancel = true;
                return;
            }
        }

        CloseDetachedEditorWindows();
        _syntaxHighlightTimer.Stop();
        _previewUpdateTimer.Stop();
        _graphUpdateTimer.Stop();
        _powerResumeTimer.Stop();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            _syntaxHighlightTimer.Dispose();
            _previewUpdateTimer.Dispose();
            _graphUpdateTimer.Dispose();
            _powerResumeTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Shift | Keys.Z))
        {
            RedoCurrentEditor();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ToolEditorUiThemeSettings.ApplyNativeWindowTheme(this, _uiTheme);
    }

    private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (IsDisposed)
            return;

        if (e.Mode == PowerModes.Suspend)
        {
            _syntaxHighlightTimer.Stop();
            _previewUpdateTimer.Stop();
            _graphUpdateTimer.Stop();
            _powerResumeTimer.Stop();
            HideNodHelpPopup();
            return;
        }

        if (e.Mode != PowerModes.Resume)
            return;

        _powerResumeQuietUntilUtc = DateTime.UtcNow.AddMilliseconds(PowerResumeQuietMs);
        _pendingHighlightTab = null;
        _pendingPreviewGraphUpdate = false;
        _powerResumePreviewPending = CurrentEditor is not null;
        _powerResumeGraphPending = ShouldGenerateGraphPreview();
        _syntaxHighlightTimer.Stop();
        _previewUpdateTimer.Stop();
        _graphUpdateTimer.Stop();
        _powerResumeTimer.Stop();
        _powerResumeTimer.Start();
    }

    private bool IsPowerResumeQuietPeriod()
    {
        return DateTime.UtcNow < _powerResumeQuietUntilUtc;
    }

    private void FlushPowerResumeWork()
    {
        if (IsDisposed)
            return;

        if (IsPowerResumeQuietPeriod())
        {
            _powerResumeTimer.Start();
            return;
        }

        var updatePreview = _powerResumePreviewPending;
        var updateGraph = _powerResumeGraphPending;
        _powerResumePreviewPending = false;
        _powerResumeGraphPending = false;

        if (updatePreview)
            UpdatePreviewFromCurrentText(updateGraph);
        else if (updateGraph)
            ScheduleGraphPreviewUpdate();
    }

    private bool HasUnsavedTabs()
    {
        return _tabs.Values.Any(tab => tab.Dirty);
    }

    private void RefreshDirtyStateBeforeClose()
    {
        foreach (var tab in _tabs.Values)
            SetTabDirty(tab, !IsCleanEditorText(tab));
    }

    private bool SaveDirtyTabsBeforeClose()
    {
        foreach (var tab in _tabs.Values.Where(tab => tab.Dirty).ToList())
        {
            SelectEditorTab(tab.Page);
            if (!TrySaveTab(tab))
                return false;
        }

        return true;
    }

    private UnsavedChangesChoice ShowUnsavedChangesDialog(string title, string message)
    {
        using var dialog = new Form
        {
            Text = title,
            Width = 500,
            Height = 178,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            TopMost = TopMost
        };
        AppWindowIcon.ApplyTo(dialog);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(14, 14, 14, 10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        root.Controls.Add(new Label
        {
            Text = message,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };

        UnsavedChangesChoice choice = UnsavedChangesChoice.Cancel;

        Button MakeChoiceButton(string text, UnsavedChangesChoice buttonChoice)
        {
            var button = new Button
            {
                Text = text,
                Width = 120,
                Height = 30,
                Margin = new Padding(6, 4, 0, 0)
            };
            button.Click += (_, _) =>
            {
                choice = buttonChoice;
                dialog.DialogResult = DialogResult.OK;
            };
            return button;
        }

        var cancel = MakeChoiceButton(T("dialog.cancel", "Annuleren"), UnsavedChangesChoice.Cancel);
        var discard = MakeChoiceButton(T("editor.dialog.dont_save", "Niet opslaan"), UnsavedChangesChoice.Discard);
        var save = MakeChoiceButton(T("editor.dialog.save", "Opslaan"), UnsavedChangesChoice.Save);
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(discard);
        buttons.Controls.Add(save);
        root.Controls.Add(buttons, 0, 1);

        dialog.Controls.Add(root);
        dialog.AcceptButton = save;
        dialog.CancelButton = cancel;

        return dialog.ShowDialog(this) == DialogResult.OK ? choice : UnsavedChangesChoice.Cancel;
    }

    private void CloseDetachedEditorWindows()
    {
        _graphPreviewForm?.Close();
        _graphPreviewForm = null;

        DockTestPanel(showPanel: false);
        DockLivePreview(showPanel: false);
        DockSimulatorPreview(showPanel: false);
    }

    private void BuildRootLayout()
    {
        _rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(_rootLayout);
    }

    private void BuildTopStripPanel()
    {
        _topStripPanel = new Panel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = PanelBackColor,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        _rootLayout.Controls.Add(_topStripPanel, 0, 0);
    }

    // Bouwt het hoofdmenu: File, Edit, Tools en View.
    private void BuildMenu()
    {
        _menuStrip = new MenuStrip
        {
            BackColor = PanelBackColor,
            ForeColor = EditorTextColor,
            Dock = DockStyle.Top
        };
        _menuStrip.MenuActivate += (_, _) => HideNodHelpPopup();

        var file = new ToolStripMenuItem(T("editor.menu.file", "File"));
        AddMenuItem(file, T("editor.menu.file.new_tab", "New tab"), (_, _) => AddBlankNewTab(), Keys.Control | Keys.N);
        AddMenuItem(file, T("editor.menu.file.open", "Open..."), Open_Click, Keys.Control | Keys.O);
        AddMenuItem(file, T("editor.menu.file.save", "Save"), Save_Click, Keys.Control | Keys.S);
        AddMenuItem(file, T("editor.menu.file.save_as", "Save as..."), SaveAs_Click);
        AddMenuItem(file, T("editor.menu.file.close_tab", "Close tab"), (_, _) => CloseCurrentTab(), Keys.Control | Keys.W);
        file.DropDownItems.Add(new ToolStripSeparator());
        _recentFilesMenuItem = new ToolStripMenuItem(T("editor.menu.file.recent_files", "Recent files"));
        file.DropDownItems.Add(_recentFilesMenuItem);
        file.DropDownOpening += (_, _) => PopulateRecentFilesMenu();
        file.DropDownItems.Add(new ToolStripSeparator());
        AddMenuItem(file, T("editor.menu.file.close", "Close"), (_, _) => Close());

        var edit = new ToolStripMenuItem(T("editor.menu.edit", "Edit"));
        _undoMenuItem = AddMenuItem(edit, T("editor.menu.edit.undo", "Ongedaan maken"), (_, _) => UndoCurrentEditor(), Keys.Control | Keys.Z);
        _redoMenuItem = AddMenuItem(edit, T("editor.menu.edit.redo", "Opnieuw uitvoeren"), (_, _) => RedoCurrentEditor(), Keys.Control | Keys.Y);
        edit.DropDownOpening += (_, _) => UpdateUndoRedoState();
        edit.DropDownItems.Add(new ToolStripSeparator());
        AddMenuItem(edit, T("editor.menu.edit.cut", "Cut"), (_, _) => CurrentEditor?.Cut(), Keys.Control | Keys.X);
        AddMenuItem(edit, T("editor.menu.edit.copy", "Copy"), (_, _) => CurrentEditor?.Copy(), Keys.Control | Keys.C);
        AddMenuItem(edit, T("editor.menu.edit.paste", "Paste"), (_, _) => CurrentEditor?.Paste(), Keys.Control | Keys.V);
        AddMenuItem(edit, T("editor.menu.edit.select_all", "Select all"), (_, _) => CurrentEditor?.SelectAll(), Keys.Control | Keys.A);
        edit.DropDownItems.Add(new ToolStripSeparator());
        AddMenuItem(edit, T("editor.menu.edit.find_replace", "Find / replace"), (_, _) => ShowFindReplace(), Keys.Control | Keys.F);

        var tools = new ToolStripMenuItem(T("editor.menu.tools", "Tools"));
        AddMenuItem(tools, T("editor.menu.tools.validate", "Validate"), Validate_Click, Keys.F5);
        AddMenuItem(tools, T("editor.menu.tools.test", "Run test"), Test_Click, Keys.F6);
        AddMenuItem(tools, T("editor.menu.tools.solver_steps", "Solver stappen..."), SolverSteps_Click, Keys.F7);
        AddMenuItem(tools, T("editor.menu.tools.restore_original", "Herstel oorspronkelijk"), RestoreOriginal_Click);
        AddMenuItem(tools, T("editor.menu.tools.repair_lines", "Regels repareren"), RepairLines_Click);
        AddMenuItem(tools, T("editor.menu.tools.recolor", "Recolor syntax"), (_, _) => HighlightCurrent());
        tools.DropDownItems.Add(new ToolStripSeparator());
        AddMenuItem(tools, T("editor.menu.tools.template_wizard", "Template wizard..."), TemplateWizard_Click, Keys.Control | Keys.Shift | Keys.N);
        var view = new ToolStripMenuItem(T("editor.menu.view", "View"));
        _viewTestPanelItem = AddMenuItem(view, T("editor.menu.view.test_panel", "Show/hide test panel"), (_, _) => ToggleTestPanel());
        _viewLivePreviewItem = AddMenuItem(view, T("editor.menu.view.live_preview", "Show/hide live preview"), (_, _) => ToggleLivePreview());
        _viewSimulatorItem = AddMenuItem(view, T("editor.menu.view.simulator", "Show/hide simulator"), (_, _) => ToggleSimulatorPreview());
        ConfigureViewToggleItem(_viewTestPanelItem);
        ConfigureViewToggleItem(_viewLivePreviewItem);
        ConfigureViewToggleItem(_viewSimulatorItem);
        _viewRestorePanelsItem = AddMenuItem(view, T("editor.menu.view.restore_panels", "Restore panels"), (_, _) => RestoreDefaultPanels());
        view.DropDownOpening += (_, _) => UpdateViewMenuChecks();
        AddMenuItem(view, T("editor.menu.view.graph_preview", "Graph 2D..."), (_, _) => ShowGraphPreviewWindow());
        AddMenuItem(view, T("editor.menu.view.graph3d_preview", "Graph 3D..."), (_, _) => ShowGraph3DPreviewWindow());
        AddMenuItem(view, T("editor.menu.view.intro_preview", "Introduction preview"), (_, _) => ShowIntroPreview());
        view.DropDownItems.Add(new ToolStripSeparator());
        AddMenuItem(view, T("editor.menu.view.undock_test_panel", "Undock test panel"), (_, _) => UndockTestPanel());
        AddMenuItem(view, T("editor.menu.view.dock_test_panel", "Dock test panel"), (_, _) => DockTestPanel(showPanel: true));
        AddMenuItem(view, T("editor.menu.view.undock_live_preview", "Undock live preview"), (_, _) => UndockLivePreview());
        AddMenuItem(view, T("editor.menu.view.dock_live_preview", "Dock live preview"), (_, _) => DockLivePreview(showPanel: true));
        AddMenuItem(view, T("editor.menu.view.undock_simulator", "Undock simulator"), (_, _) => UndockSimulatorPreview());
        AddMenuItem(view, T("editor.menu.view.dock_simulator", "Dock simulator"), (_, _) => DockSimulatorPreview(showPanel: true));
        view.DropDownItems.Add(new ToolStripSeparator());
        var theme = new ToolStripMenuItem(T("tool_editor.menu.extra.theme", "Theme"));
        _darkThemeMenuItem = CreateThemeMenuItem(T("tool_editor.menu.extra.theme.dark", "Dark"), ToolEditorUiTheme.Dark);
        _classicThemeMenuItem = CreateThemeMenuItem(T("tool_editor.menu.extra.theme.classic", "Classic"), ToolEditorUiTheme.Classic);
        theme.DropDownItems.Add(_darkThemeMenuItem);
        theme.DropDownItems.Add(_classicThemeMenuItem);
        view.DropDownItems.Add(theme);

        var help = new ToolStripMenuItem(T("editor.menu.help", "Help"));
        AddMenuItem(help, T("editor.menu.help.nod", "NOD help"), (_, _) => ShowNodHelp(), Keys.F1);
        AddMenuItem(help, T("editor.menu.formula_card", "Formula card"), FormulaCard_Click);
        help.DropDownItems.Add(new ToolStripSeparator());
        AddMenuItem(help, T("editor.menu.help.about", "About NOD Editor"), (_, _) => ShowAbout());

        _menuStrip.Items.Add(file);
        _menuStrip.Items.Add(edit);
        _menuStrip.Items.Add(tools);
        _menuStrip.Items.Add(view);
        _menuStrip.Items.Add(help);

        MainMenuStrip = _menuStrip;
        _topStripPanel.Controls.Add(_menuStrip);
        PopulateRecentFilesMenu();
        UpdateViewMenuChecks();
    }

    private ToolStripMenuItem CreateThemeMenuItem(string text, ToolEditorUiTheme theme)
    {
        var item = new ToolStripMenuItem(text)
        {
            Checked = _uiTheme == theme,
            CheckOnClick = false
        };
        item.Click += (_, _) => ApplyAndSaveTheme(theme);
        return item;
    }

    private void ApplyAndSaveTheme(ToolEditorUiTheme theme)
    {
        if (_uiTheme == theme)
            return;

        _uiTheme = theme;
        _palette = theme == ToolEditorUiTheme.Dark ? ToolEditorPalette.Dark : ToolEditorPalette.Default;
        ToolEditorUiThemeSettings.Save(theme);
        ToolEditorUiThemeSettings.ApplyApplicationColorMode(theme);
        ToolEditorTabsApi.ConfigureTheme(theme);
        ToolEditorUiThemeSettings.ApplyNativeWindowTheme(this, theme);
        ApplyNodEditorTheme();
        ApplySharedThemeToOpenMainForms(theme);
        UpdateThemeMenuChecks();
        SetStatus(T("tool_editor.theme.saved", "Theme saved."));
    }

    private static void ApplySharedThemeToOpenMainForms(ToolEditorUiTheme theme)
    {
        foreach (Form form in Application.OpenForms)
        {
            if (form is MainForm mainForm)
                mainForm.ApplySharedTheme(theme);
        }
    }

    private void UpdateThemeMenuChecks()
    {
        if (_darkThemeMenuItem is not null)
            _darkThemeMenuItem.Checked = _uiTheme == ToolEditorUiTheme.Dark;
        if (_classicThemeMenuItem is not null)
            _classicThemeMenuItem.Checked = _uiTheme == ToolEditorUiTheme.Classic;
    }

    private void ApplyNodEditorTheme()
    {
        GraphOverlayStyle.UseDarkTheme = IsDarkTheme;
        BackColor = WindowBackColor;
        _rootLayout.BackColor = WindowBackColor;
        _topStripPanel.BackColor = PanelBackColor;
        _mainSplit.BackColor = BorderColor;
        _mainSplit.Panel1.BackColor = WindowBackColor;
        _mainSplit.Panel2.BackColor = WindowBackColor;
        _bottomPanel.BackColor = SoftPanelBackColor;
        _editorTabStrip.BackColor = PanelBackColor;
        _editorContentPanel.BackColor = BorderColor;
        _testTabs.BackColor = SoftPanelBackColor;
        _statusStrip.BackColor = PanelBackColor;
        _statusStrip.ForeColor = EditorTextColor;
        if (_nodHelpPopup is NodHelpPopupPanel popup)
        {
            popup.DarkMode = IsDarkTheme;
            popup.Invalidate();
        }

        ApplyThemeToControl(this);
        RefreshToolbarThemes(this);
        RefreshGraphOverlayThemes();
        RefreshSimulatorTheme();
        RefreshMathPreviewThemes();
        RefreshNodHelpBrowserTheme();

        foreach (var tab in _tabs.Values)
        {
            ApplyEditorTheme(tab);
            ScheduleSyntaxHighlight(tab);
        }

        RefreshEditorTabStrip();
        _graphCanvas?.Invalidate();
        _graph3DCanvas?.Invalidate();
    }

    private void ApplyThemeToControl(Control control)
    {
        if (control is RichTextBox or TextBox or NumericUpDown or ComboBox or ListBox)
        {
            control.BackColor = EditorBackColor;
            control.ForeColor = EditorTextColor;
        }
        else if (control is DataGridView grid)
        {
            ApplyDataGridViewTheme(grid);
        }
        else if (control is Label or CheckBox or RadioButton or GroupBox)
        {
            control.ForeColor = EditorTextColor;
            if (control.BackColor != Color.Transparent)
                control.BackColor = SoftPanelBackColor;
        }
        else if (control is TabPage or TableLayoutPanel or Panel or SplitContainer or TabControl)
        {
            if (control.BackColor != Color.Transparent)
                control.BackColor = control == _editorContentPanel ? BorderColor : SoftPanelBackColor;
            control.ForeColor = EditorTextColor;
        }

        foreach (Control child in control.Controls)
            ApplyThemeToControl(child);
    }

    private void ApplyDataGridViewTheme(DataGridView grid)
    {
        grid.BackgroundColor = EditorBackColor;
        grid.BackColor = EditorBackColor;
        grid.ForeColor = EditorTextColor;
        grid.GridColor = BorderColor;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = PreviewCardBackColor;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = EditorTextColor;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = PreviewCardBackColor;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = EditorTextColor;
        grid.DefaultCellStyle.BackColor = EditorBackColor;
        grid.DefaultCellStyle.ForeColor = EditorTextColor;
        grid.DefaultCellStyle.SelectionBackColor = IsDarkTheme ? Color.FromArgb(37, 99, 235) : SystemColors.Highlight;
        grid.DefaultCellStyle.SelectionForeColor = IsDarkTheme ? Color.White : SystemColors.HighlightText;
        grid.AlternatingRowsDefaultCellStyle.BackColor = IsDarkTheme ? Color.FromArgb(30, 41, 59) : Color.FromArgb(248, 251, 255);
        grid.AlternatingRowsDefaultCellStyle.ForeColor = EditorTextColor;
    }

    private void RefreshGraphOverlayThemes()
    {
        ApplyGraphOverlayPanelTheme(_graphPointPanel);
        ApplyGraphOverlayPanelTheme(_graph3DPointPanel);
        ApplyGraphOverlayPanelTheme(_graph3DCommandPanel);
        ApplyGraphOverlayPanelTheme(_graph3DNavigationPanel);
        ApplyGraphOverlayPanelTheme(_graph3DRotationDial);

        if (_graphPointTitleBar is not null)
        {
            _graphPointTitleBar.BackColor = GraphOverlayStyle.TitleFill;
            _graphPointTitleBar.Invalidate();
        }
        if (_graph3DPointTitleBar is not null)
        {
            _graph3DPointTitleBar.BackColor = GraphOverlayStyle.TitleFill;
            _graph3DPointTitleBar.Invalidate();
        }
        if (_graphPointTable is not null)
            ApplyDataGridViewTheme(_graphPointTable);
        if (_graph3DPointTable is not null)
            ApplyDataGridViewTheme(_graph3DPointTable);
    }

    private static void ApplyGraphOverlayPanelTheme(Control? control)
    {
        if (control is null)
            return;

        control.BackColor = GraphOverlayStyle.PanelFill(translucent: false);
        control.ForeColor = GraphOverlayStyle.TitleText;
        control.Invalidate();
        foreach (Control child in control.Controls)
            ApplyGraphOverlayPanelTheme(child);
    }

    private void RefreshSimulatorTheme()
    {
        if (_simulatorPreviewGroup is null || _simWindow is null)
            return;

        _simulatorPreviewGroup.BackColor = SoftPanelBackColor;
        _simulatorPreviewGroup.ForeColor = EditorTextColor;
        ApplySimulatorTheme(_simulatorPreviewGroup);

        _simTitle.ForeColor = EditorTextColor;
        _simFileLabel.ForeColor = EditorTextColor;
        _simInputLabel.ForeColor = EditorTextColor;
        _simOutputLabel.ForeColor = EditorTextColor;
        _simDigitGroup.ForeColor = MutedTextColor;
        _simDecimals.ForeColor = MutedTextColor;
        _simDecimalCount.ForeColor = MutedTextColor;
        _simDecimalCount.BackColor = EditorBackColor;
        _simFileBox.BackColor = EditorBackColor;
        _simFileBox.ForeColor = EditorTextColor;
    }

    private void ApplySimulatorTheme(Control control)
    {
        switch (control)
        {
            case PictureBox picture when picture.Tag is ToolbarIcon icon:
                picture.BackColor = PanelBackColor;
                picture.Image?.Dispose();
                picture.Image = CreateSimulatorToolbarImage(icon);
                break;
            case MenuStrip menu:
                menu.BackColor = PanelBackColor;
                menu.ForeColor = EditorTextColor;
                break;
            case ToolStrip toolStrip:
                toolStrip.BackColor = PanelBackColor;
                toolStrip.ForeColor = EditorTextColor;
                break;
            case TextBox textBox:
                textBox.BackColor = EditorBackColor;
                textBox.ForeColor = EditorTextColor;
                break;
            case ComboBox comboBox:
                comboBox.BackColor = EditorBackColor;
                comboBox.ForeColor = EditorTextColor;
                break;
            case NumericUpDown numberBox:
                numberBox.BackColor = EditorBackColor;
                numberBox.ForeColor = MutedTextColor;
                break;
            case CheckBox checkBox:
                checkBox.BackColor = SoftPanelBackColor;
                checkBox.ForeColor = checkBox.AutoCheck ? EditorTextColor : MutedTextColor;
                break;
            case RadioButton radio:
                radio.BackColor = SoftPanelBackColor;
                radio.ForeColor = MutedTextColor;
                break;
            case Label label:
                label.BackColor = SoftPanelBackColor;
                label.ForeColor = EditorTextColor;
                break;
            case TableLayoutPanel table:
                table.BackColor = SoftPanelBackColor;
                table.ForeColor = EditorTextColor;
                break;
            case FlowLayoutPanel flow:
                flow.BackColor = ReferenceEquals(flow.Parent, _simWindow) ? PanelBackColor : SoftPanelBackColor;
                flow.ForeColor = EditorTextColor;
                break;
            case Panel panel:
                panel.BackColor = ReferenceEquals(panel, _simWindow) ? SoftPanelBackColor : PanelBackColor;
                panel.ForeColor = EditorTextColor;
                break;
        }

        foreach (Control child in control.Controls)
            ApplySimulatorTheme(child);
    }

    private void RefreshToolbarThemes(Control control)
    {
        if (control is ToolStrip toolStrip)
        {
            toolStrip.BackColor = PanelBackColor;
            toolStrip.ForeColor = EditorTextColor;
            toolStrip.RenderMode = IsDarkTheme ? ToolStripRenderMode.ManagerRenderMode : ToolStripRenderMode.System;
            foreach (ToolStripItem item in toolStrip.Items)
                RefreshToolStripItemTheme(item);
        }

        foreach (Control child in control.Controls)
            RefreshToolbarThemes(child);
    }

    private void RefreshToolStripItemTheme(ToolStripItem item)
    {
        item.BackColor = PanelBackColor;
        item.ForeColor = EditorTextColor;
        if (item is ToolStripButton button && button.Tag is ToolEditorIcon icon)
            button.Image = ToolEditorApi.CreateIcon(icon, _palette);
        if (item is ToolStripDropDownItem dropDown)
        {
            dropDown.DropDown.BackColor = PanelBackColor;
            dropDown.DropDown.ForeColor = EditorTextColor;
            foreach (ToolStripItem child in dropDown.DropDownItems)
                RefreshToolStripItemTheme(child);
        }
    }

    private void RefreshMathPreviewThemes()
    {
        ApplyMathPreviewTheme(_testFormulaView);
        ApplyMathPreviewTheme(_testCalculationView);
    }

    private void ApplyMathPreviewTheme(HtmlMathPreviewControl? view)
    {
        if (view is null)
            return;

        if (view.Parent is Panel card)
        {
            card.BackColor = PreviewCardBackColor;
            card.Invalidate();
        }
        view.BackColor = PreviewCardBackColor;
        view.ForeColor = IsDarkTheme ? Color.FromArgb(191, 219, 254) : Color.FromArgb(0, 74, 173);
        view.PreferredColorScheme = PreferredWebViewColorScheme;
    }

    private void RefreshNodHelpBrowserTheme()
    {
        if (_nodHelpBrowser?.CoreWebView2 is not null)
            _nodHelpBrowser.CoreWebView2.Profile.PreferredColorScheme = PreferredWebViewColorScheme;

        if (_lastRenderedNodHelpHtml is not null)
        {
            _pendingNodHelpHtml = _lastRenderedNodHelpHtml;
            _lastRenderedNodHelpHtml = null;
            ShowPendingNodHelpHtmlIfReady();
        }
    }

    // Helper om snel menu-items met click-handler en sneltoets toe te voegen.
    private static ToolStripMenuItem AddMenuItem(ToolStripMenuItem parent, string text, EventHandler handler, Keys shortcutKeys = Keys.None)
    {
        var item = new ToolStripMenuItem(text);
        if (shortcutKeys != Keys.None) item.ShortcutKeys = shortcutKeys;
        item.Click += handler;
        parent.DropDownItems.Add(item);
        return item;
    }

    private static void ConfigureViewToggleItem(ToolStripMenuItem item)
    {
        item.CheckOnClick = false;
        item.ShowShortcutKeys = false;
    }

    private void UpdateViewMenuChecks()
    {
        if (_viewTestPanelItem is null || _viewLivePreviewItem is null || _viewSimulatorItem is null)
            return;

        _viewTestPanelItem.Checked = _floatingTestForm is not null || _testGroup?.Visible == true;
        _viewLivePreviewItem.Checked = _floatingPreviewForm is not null || _metadataPreviewGroup?.Visible == true;
        _viewSimulatorItem.Checked = _floatingSimulatorForm is not null || _simulatorPreviewGroup?.Visible == true;
        _viewTestPanelItem.Enabled = !_solverPreviewActive;
        _viewSimulatorItem.Enabled = !_solverPreviewActive;
        _viewRestorePanelsItem.Visible = !_viewTestPanelItem.Checked && !_viewLivePreviewItem.Checked && !_viewSimulatorItem.Checked;
    }

    // Bouwt de toolbar met knoppen voor nieuw, openen, opslaan, zoeken, valideren, testen en herstellen.
    private void BuildToolbar()
    {
        _toolStrip = ToolEditorApi.CreateToolbar(_palette);

        _toolStrip.Items.Add(MakeButton(T("editor.toolbar.new", "New"), (_, _) => AddBlankNewTab(), ToolEditorIcon.New));
        _toolStrip.Items.Add(MakeButton(T("editor.toolbar.open", "Open"), Open_Click, ToolEditorIcon.Open));
        _saveButton = MakeButton(T("editor.toolbar.save", "Save"), Save_Click, ToolEditorIcon.Save);
        _toolStrip.Items.Add(_saveButton);
        _toolStrip.Items.Add(new ToolStripSeparator());
        _undoButton = MakeIconButton(T("editor.toolbar.undo", "Ongedaan maken"), (_, _) => UndoCurrentEditor(), ToolEditorIcon.Undo);
        _redoButton = MakeIconButton(T("editor.toolbar.redo", "Opnieuw uitvoeren"), (_, _) => RedoCurrentEditor(), ToolEditorIcon.Redo);
        _toolStrip.Items.Add(_undoButton);
        _toolStrip.Items.Add(_redoButton);
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(MakeButton(T("editor.toolbar.template_wizard", "Templates"), TemplateWizard_Click, ToolEditorIcon.Wizard));
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(MakeButton(T("editor.toolbar.find", "Find"), (_, _) => ShowFindReplace(), ToolEditorIcon.Find));
        _toolStrip.Items.Add(MakeButton(T("editor.toolbar.validate", "Validate"), Validate_Click, ToolEditorIcon.Validate));
        _toolStrip.Items.Add(MakeButton(T("editor.toolbar.test", "Test"), Test_Click, ToolEditorIcon.Test));
        _toolStrip.Items.Add(MakeButton(T("editor.toolbar.solver_steps", "Solver"), SolverSteps_Click, ToolEditorIcon.Solver));
        _toolStrip.Items.Add(MakeButton(T("editor.toolbar.restore_original", "Herstel"), RestoreOriginal_Click, ToolEditorIcon.Repair));

        _topStripPanel.Controls.Add(_toolStrip);
        UpdateSaveButtonState();
    }

    // Maakt een toolbar-knop met tekst, icoon en click-handler.
    private ToolStripButton MakeButton(string text, EventHandler handler, ToolEditorIcon icon)
    {
        return ToolEditorApi.CreateButton(text, icon, handler, tooltip: text, palette: _palette);
    }

    private ToolStripButton MakeIconButton(string tooltip, EventHandler handler, ToolEditorIcon icon)
    {
        var button = ToolEditorApi.CreateButton("", icon, handler, tooltip: tooltip, palette: _palette);
        button.DisplayStyle = ToolStripItemDisplayStyle.Image;
        button.AutoSize = false;
        button.Size = new Size(28, 26);
        button.Padding = new Padding(1);
        return button;
    }

    // Bouwt de hoofdindeling: editor-tabs bovenaan en test/live-preview/simulator onderaan.
    private void BuildLayout()
    {
        _mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            Panel1MinSize = 1,
            Panel2MinSize = 1,
            BackColor = Color.FromArgb(230, 230, 230)
        };

        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Appearance = TabAppearance.FlatButtons,
            ItemSize = new Size(0, 1),
            SizeMode = TabSizeMode.Fixed
        };
        _tabControl.SelectedIndexChanged += (_, _) => { HideNodHelpPopup(); UpdateUiState(); UpdatePreviewFromCurrentText(); RefreshEditorTabStrip(); };
        _tabControl.Visible = false;

        _mainSplit.Panel1.Padding = new Padding(4, 4, 4, 0);
        var editorHost = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        editorHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editorHost.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        editorHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _editorTabStrip = ToolEditorTabsApi.CreateStrip();
        _editorTabStrip.Dock = DockStyle.Fill;
        _editorTabStrip.WrapContents = false;
        _editorTabStrip.AutoScroll = true;
        _editorTabStrip.BackColor = Color.White;
        _editorTabStrip.Padding = new Padding(0, 2, 0, 0);
        _editorTabStrip.Margin = Padding.Empty;
        _editorContentPanel = new EditorContentPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = Padding.Empty,
            Padding = new Padding(1, 0, 1, 1)
        };

        editorHost.Controls.Add(_editorTabStrip, 0, 0);
        editorHost.Controls.Add(_editorContentPanel, 0, 1);
        _mainSplit.Panel1.Controls.Add(editorHost);

        _bottomPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 2,
            Padding = new Padding(10, 8, 10, 8),
            BackColor = Color.FromArgb(250, 250, 250)
        };

        _bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DockedTestPanelWidth + 10));
        _bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _testGroup = new GroupBox
        {
            Text = T("editor.test.title", "Converter test") + "  \u00D7",
            Dock = DockStyle.Left,
            Width = DockedTestPanelWidth,
        };
        _testGroup.DoubleClick += (_, _) => UndockTestPanel();

        var testPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 3,
            Padding = new Padding(6, 7, 6, 5),
            BackColor = Color.FromArgb(250, 250, 250)
        };

        testPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        testPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        testPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        testPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        testPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        testPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        testPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));

        _testInputLabel = new Label { Text = T("editor.test.input", "Test input"), AutoSize = true, Anchor = AnchorStyles.Left };
        testPanel.Controls.Add(_testInputLabel, 0, 0);
        _testInput = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _testInput.TextChanged += (_, _) => SchedulePreviewUpdate(updateGraph: false);
        testPanel.Controls.Add(_testInput, 1, 0);

        _testButton = new Button { Text = T("editor.toolbar.test", "Test"), Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _testButton.Click += Test_Click;
        testPanel.Controls.Add(_testButton, 2, 0);

        _testOutputLabel = new Label { Text = T("editor.test.output", "Output"), AutoSize = true, Anchor = AnchorStyles.Left };
        testPanel.Controls.Add(_testOutputLabel, 0, 1);
        _testOutput = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        testPanel.Controls.Add(_testOutput, 1, 1);

        _validateButton = new Button { Text = T("editor.toolbar.validate", "Validate"), Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _validateButton.Click += Validate_Click;
        testPanel.Controls.Add(_validateButton, 2, 1);

        _testFormulaLabel = new Label { Text = T("editor.test.formula", "Formula"), AutoSize = true, Anchor = AnchorStyles.Left };
        testPanel.Controls.Add(_testFormulaLabel, 0, 2);
        var formulaCard = CreateMathPreviewCard(out _testFormulaView);
        testPanel.Controls.Add(formulaCard, 1, 2);
        testPanel.SetColumnSpan(formulaCard, 2);

        _testCalculationLabel = new Label { Text = T("editor.test.calculation", "Calculation"), AutoSize = true, Anchor = AnchorStyles.Left };
        testPanel.Controls.Add(_testCalculationLabel, 0, 3);
        var calculationCard = CreateMathPreviewCard(out _testCalculationView);
        testPanel.Controls.Add(calculationCard, 1, 3);
        testPanel.SetColumnSpan(calculationCard, 2);
        _testTabs = new TabControl
        {
            Dock = DockStyle.Fill
        };

        _converterTestPage = new TabPage(T("editor.test.title", "Converter test"));
        _converterTestPage.Controls.Add(testPanel);
        _graphPreviewPage = new TabPage(T("editor.graph.title", "Graph 2D"));
        _graphPreviewPage.Controls.Add(BuildGraphPreviewPanel());
        _graph3DPage = new TabPage(T("editor.graph3d.title", "Graph 3D"));
        _graph3DPage.Controls.Add(BuildGraph3DPanel());
        _testTabs.SelectedIndexChanged += (_, _) =>
        {
            if (_testTabs.SelectedTab == _graphPreviewPage || _testTabs.SelectedTab == _graph3DPage)
                GenerateGraphPreview();
            ApplyGraph3DFocusLayout();
            if (_testTabs.SelectedTab == _graph3DPage)
                _graph3DCanvas.Invalidate();
        };

        _testTabs.TabPages.Add(_converterTestPage);
        _testTabs.TabPages.Add(_graphPreviewPage);
        _testTabs.TabPages.Add(_graph3DPage);
        _testGroup.Controls.Add(_testTabs);
        ApplyGraph3DFocusLayout();

        _previewSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            FixedPanel = FixedPanel.Panel2,
            IsSplitterFixed = true,
            Panel1MinSize = 1,
            Panel2MinSize = 1
        };

        _metadataPreviewGroup = BuildPreviewBox();
        _metadataPreviewGroup.Dock = DockStyle.None;
        _metadataPreviewGroup.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _metadataPreviewGroup.Height = 168;
        _metadataPreviewGroup.Width = MaxDockedLivePreviewWidth;
        _simulatorPreviewGroup = BuildSimulatorPreviewBox();

        _previewSplit.Panel1.Controls.Add(_metadataPreviewGroup);
        _previewSplit.Panel1.SizeChanged += (_, _) => AdjustLivePreviewWidth();
        _previewSplit.Panel2.Controls.Add(_simulatorPreviewGroup);

        _bottomPanel.Controls.Add(_testGroup, 0, 0);
        _bottomPanel.Controls.Add(_previewSplit, 1, 0);
        ApplyGraph3DFocusLayout();
        _previewSplit.SizeChanged += (_, _) => AdjustPreviewSplitter();

        _mainSplit.Panel2.Controls.Add(_bottomPanel);
        _rootLayout.Controls.Add(_mainSplit, 0, 1);
        _mainSplit.SizeChanged += (_, _) => AdjustMainSplitter();
    }

    // Bouwt een grafiek-paneel dat de huidige converter als y=f(x) samplet.
    private Control BuildGraphPreviewPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(8, 8, 8, 6),
            BackColor = Color.FromArgb(250, 250, 250)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var inputGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 3
        };
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38));
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        inputGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        inputGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        inputGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 29));

        inputGrid.Controls.Add(MakeGraphToolbarLabel("X min"), 0, 0);
        _graphXMin = MakeGraphNumberBox(-5, -GraphPreviewRangeLimit, GraphPreviewRangeLimit, 1);
        _graphXMin.ValueChanged += (_, _) => { if (!IsEditingGraphNumberBox(_graphXMin)) ApplyGraphPreviewXRangeFromControls(); };
        inputGrid.Controls.Add(_graphXMin, 1, 0);

        inputGrid.Controls.Add(MakeGraphToolbarLabel("X max"), 2, 0);
        _graphXMax = MakeGraphNumberBox(5, -GraphPreviewRangeLimit, GraphPreviewRangeLimit, 1);
        _graphXMax.ValueChanged += (_, _) => { if (!IsEditingGraphNumberBox(_graphXMax)) ApplyGraphPreviewXRangeFromControls(); };
        inputGrid.Controls.Add(_graphXMax, 3, 0);

        inputGrid.Controls.Add(MakeGraphToolbarLabel(T("editor.graph.step", "Step")), 4, 0);
        _graphStep = MakeGraphNumberBox(1, GraphPreviewStepMinimum, GraphPreviewRangeLimit, 1);
        _graphStep.ValueChanged += (_, _) =>
        {
            if (!_applyingGraphSyncState && !_updatingGraphStepDisplay && !IsEditingGraphNumberBox(_graphStep))
            {
                _graphUserStep = GraphSurfaceApi.GetNumberBoxValue(_graphStep);
                GenerateGraphPreview();
            }
        };
        inputGrid.Controls.Add(_graphStep, 5, 0);

        inputGrid.Controls.Add(MakeGraphToolbarLabel("Y min"), 0, 1);
        _graphYMin = MakeGraphNumberBox(-5, -GraphPreviewRangeLimit, GraphPreviewRangeLimit, 1);
        _graphYMin.ValueChanged += (_, _) => { if (!IsEditingGraphNumberBox(_graphYMin)) ApplyGraphPreviewYRangeFromControls(); };
        inputGrid.Controls.Add(_graphYMin, 1, 1);

        inputGrid.Controls.Add(MakeGraphToolbarLabel("Y max"), 2, 1);
        _graphYMax = MakeGraphNumberBox(5, -GraphPreviewRangeLimit, GraphPreviewRangeLimit, 1);
        _graphYMax.ValueChanged += (_, _) => { if (!IsEditingGraphNumberBox(_graphYMax)) ApplyGraphPreviewYRangeFromControls(); };
        inputGrid.Controls.Add(_graphYMax, 3, 1);

        ApplyGraphInputTooltips(_graphXMin, _graphXMax, _graphYMin, _graphYMax, _graphStep);
        AttachGraphCommittedInput(_graphXMin, ApplyGraphPreviewXRangeFromControls);
        AttachGraphCommittedInput(_graphXMax, ApplyGraphPreviewXRangeFromControls);
        AttachGraphCommittedInput(_graphYMin, ApplyGraphPreviewYRangeFromControls);
        AttachGraphCommittedInput(_graphYMax, ApplyGraphPreviewYRangeFromControls);
        AttachGraphCommittedInput(_graphStep, () =>
        {
            _graphUserStep = GraphSurfaceApi.GetNumberBoxValue(_graphStep);
            if (!_applyingGraphSyncState)
                GenerateGraphPreview();
        });
        AttachGraphStepSpinner(_graphStep, CommitGraphStepSpinner);

        _graphShowRangeLines = new CheckBox
        {
            Text = T("editor.graph.lines", "Lines"),
            Checked = true,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(4, 0, 0, 0)
        };
        _graphShowRangeLines.CheckedChanged += (_, _) =>
        {
            ApplyGraphRangeLineVisibility();
            UpdateGraphRangeInputMode();
            NotifyGraphPreviewSyncStateChanged();
            _graphCanvas.Invalidate();
            _graph3DCanvas?.Invalidate();
        };
        inputGrid.Controls.Add(_graphShowRangeLines, 4, 1);
        inputGrid.SetColumnSpan(_graphShowRangeLines, 2);

        var copyButton = new GraphToolbarIconButton(GraphToolbarIcon.Copy, T("editor.graph.copy_points", "Copy points")) { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        copyButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_graphPointClipboardText))
                Clipboard.SetText(_graphPointClipboardText);
        };
        inputGrid.Controls.Add(copyButton, 0, 2);
        inputGrid.SetColumnSpan(copyButton, 2);

        var openLargeButton = new GraphToolbarIconButton(GraphToolbarIcon.Open, T("editor.graph.open_large", "Open large graph")) { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        openLargeButton.Click += (_, _) => ShowGraphPreviewWindow();
        inputGrid.Controls.Add(openLargeButton, 2, 2);
        inputGrid.SetColumnSpan(openLargeButton, 2);

        _graphToggleTableButton = new GraphToolbarIconButton(GraphToolbarIcon.TableHidden, T("editor.graph.hide_table", "Hide table")) { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _graphToggleTableButton.Click += (_, _) => SetGraphPointTableVisible(!_graphPointPanel.Visible);
        inputGrid.Controls.Add(_graphToggleTableButton, 4, 2);
        inputGrid.SetColumnSpan(_graphToggleTableButton, 2);

        panel.Controls.Add(inputGrid, 0, 0);

        var graphHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(240, 244, 249)
        };

        _graphCanvas = new GraphCanvasPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            MinimumSize = new Size(120, 120)
        };
        _graphCanvas.Paint += GraphCanvas_Paint;
        _graphCanvas.MouseWheel += GraphCanvas_MouseWheel;
        _graphCanvas.MouseDown += GraphCanvas_MouseDown;
        _graphCanvas.MouseMove += GraphCanvas_MouseMove;
        _graphCanvas.MouseUp += GraphCanvas_MouseUp;
        _graphCanvas.MouseEnter += (_, _) =>
        {
            _graphCanvas.Focus();
            if (_graphHasView)
                _graphCanvas.Cursor = GraphCursors.Pan;
        };
        _graphCanvas.MouseLeave += (_, _) =>
        {
            if (!_graphPanning)
                _graphCanvas.Cursor = GraphCursors.Default;
        };
        graphHost.Controls.Add(_graphCanvas);

        var chrome = GraphSurfaceApi.CreateChrome(
            GraphOverlayButtonDensity.Compact,
            T("editor.graph.points", "Points"),
            () => _graphPointerText,
            () => SetGraphPointTableVisible(false),
            GraphPointPanel_MouseDown,
            GraphPointPanel_MouseMove,
            GraphPointPanel_MouseUp,
            T("editor.graph.reset_view", "Home / reset view"),
            (_, _) => ResetGraphPreviewView(),
            T("editor.graph.zoom_in", "Zoom in"),
            (_, _) => ZoomGraphPreview(0.75f),
            T("editor.graph.zoom_out", "Zoom out"),
            (_, _) => ZoomGraphPreview(1.35f));
        var graphZoomPanel = chrome.NavigationPanel;
        graphHost.Controls.Add(graphZoomPanel);
        graphZoomPanel.BringToFront();

        _graphPointerStatusLabel = new Label
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
        _graphPointerStatusPanel = new Panel
        {
            Width = 52,
            Height = 20,
            BackColor = Color.White,
            Visible = false
        };
        _graphPointerStatusPanel.Paint += CompactGraphStatusPanel_Paint;
        _graphPointerStatusPanel.Controls.Add(_graphPointerStatusLabel);
        graphHost.Controls.Add(_graphPointerStatusPanel);
        _graphPointerStatusPanel.BringToFront();

        _graphPointPanel = chrome.PointTablePanel;
        _graphPointTitleBar = chrome.PointTableTitleBar;
        _graphPointTable = chrome.PointTable;
        graphHost.Controls.Add(_graphPointPanel);
        _graphPointPanel.BringToFront();
        graphHost.Resize += (_, _) => PlaceGraphPointOverlay();
        graphHost.Resize += (_, _) =>
        {
            graphZoomPanel.Left = Math.Max(0, graphHost.ClientSize.Width - graphZoomPanel.Width - 8);
            graphZoomPanel.Top = 8;
            _graphPointerStatusPanel.Left = 8;
            _graphPointerStatusPanel.Top = Math.Max(8, graphHost.ClientSize.Height - _graphPointerStatusPanel.Height - 4);
            graphZoomPanel.BringToFront();
            _graphPointerStatusPanel.BringToFront();
            UpdateGraphPointerStatusVisibility();
            if (_graphHasView)
            {
                MatchGraphPreviewViewToCanvasAspect();
                UpdateGraphPreviewViewportRangeControlsIfNeeded();
                ResampleGraphPreviewVisibleView();
            }
            _graphCanvas.Invalidate();
        };
        graphZoomPanel.Left = Math.Max(0, graphHost.ClientSize.Width - graphZoomPanel.Width - 8);
        graphZoomPanel.Top = 8;
        _graphPointerStatusPanel.Left = 8;
        _graphPointerStatusPanel.Top = Math.Max(8, graphHost.ClientSize.Height - _graphPointerStatusPanel.Height - 4);
        PlaceGraphPointOverlay();
        panel.Controls.Add(graphHost, 0, 1);

        _graphStatus = new Label { Text = "", Visible = false };
        InitializeGraphPreviewBaseView();
        UpdateGraphRangeInputMode();

        return panel;
    }

    private Control BuildGraph3DPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(8, 8, 8, 6),
            Margin = Padding.Empty,
            BackColor = Color.FromArgb(250, 250, 250)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var graphHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(240, 244, 249),
            Margin = Padding.Empty
        };

        _graph3DCanvas = new GraphCanvasPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            MinimumSize = new Size(120, 120)
        };
        _graph3DCanvas.Paint += Graph3DCanvas_Paint;
        _graph3DCanvas.MouseDown += Graph3DCanvas_MouseDown;
        _graph3DCanvas.MouseMove += Graph3DCanvas_MouseMove;
        _graph3DCanvas.MouseUp += Graph3DCanvas_MouseUp;
        _graph3DCanvas.MouseWheel += Graph3DCanvas_MouseWheel;
        _graph3DCanvas.MouseEnter += (_, _) =>
        {
            _graph3DCanvas.Focus();
            _graph3DCanvas.Cursor = GraphCursors.Pan;
        };
        _graph3DCanvas.MouseLeave += (_, _) =>
        {
            if (!_graph3DDragging)
                _graph3DCanvas.Cursor = GraphCursors.Default;
        };
        graphHost.Controls.Add(_graph3DCanvas);

        _graph3DNavigationPanel = GraphOverlayButton.CreateNavigationGroup(
            GraphOverlayButtonDensity.Compact,
            T("editor.graph3d.reset_view", "Home / reset camera"),
            (_, _) => SetGraph3DCamera(GraphCameraPreset3D.Isometric),
            T("editor.graph3d.zoom_in", "Zoom in"),
            (_, _) => ZoomGraph3DCamera(1.15),
            T("editor.graph3d.zoom_out", "Zoom out"),
            (_, _) => ZoomGraph3DCamera(1d / 1.15d));
        graphHost.Controls.Add(_graph3DNavigationPanel);
        _graph3DNavigationPanel.BringToFront();

        _graph3DRotationDial = new Graph3DRotationDial { Camera = _graph3DCamera };
        _graph3DRotationDial.RotationDeltaRequested += (yaw, pitch) => RotateGraph3DCamera(yaw, pitch);
        _graph3DRotationDial.ResetRequested += () => SetGraph3DCamera(GraphCameraPreset3D.Isometric);

        _graph3DCommandPanel = new Graph3DOverlayFlowPanel
        {
            Width = 258,
            Height = 28,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = new Padding(2)
        };
        _graph3DCommandPanel.Paint += Graph3DOverlayPanel_Paint;
        _graph3DFlat2DButton = MakeGraph3DButton("2D", T("editor.graph3d.front", "Front"), ToggleGraph3DFlat2DMode);
        _graph3DCommandPanel.Controls.Add(_graph3DFlat2DButton);
        _graph3DIsoButton = MakeGraph3DButton("3D", T("editor.graph3d.3d", "3D view"), () => SetGraph3DCamera(GraphCameraPreset3D.Isometric));
        _graph3DCommandPanel.Controls.Add(_graph3DIsoButton);
        _graph3DTopButton = MakeGraph3DButton("Top", T("editor.graph3d.top", "Top"), () => SetGraph3DCamera(GraphCameraPreset3D.Top));
        _graph3DCommandPanel.Controls.Add(_graph3DTopButton);
        _graph3DGridToggle = new CheckBox
        {
            Text = "Grid",
            Checked = true,
            AutoSize = true,
            Height = 22,
            Margin = new Padding(2, 3, 1, 0),
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(15, 63, 143),
            Font = new Font("Segoe UI", 7f, FontStyle.Bold)
        };
        _graph3DGridToggle.CheckedChanged += (_, _) =>
        {
            _graph3DGridVisible = _graph3DGridToggle.Checked;
            _graph3DCanvas?.Invalidate();
        };
        _graphInputToolTip.SetToolTip(_graph3DGridToggle, T("editor.graph3d.grid", "Show 3D grid"));
        _graph3DCommandPanel.Controls.Add(_graph3DGridToggle);
        var copyButton = new GraphToolbarIconButton(GraphToolbarIcon.Copy, T("editor.graph3d.copy_points", "Copy 3D points"))
        {
            Width = 30,
            Height = 22,
            Margin = new Padding(2, 1, 0, 0)
        };
        copyButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_graph3DPointClipboardText))
                Clipboard.SetText(_graph3DPointClipboardText);
        };
        _graph3DCommandPanel.Controls.Add(copyButton);

        var openButton = new GraphToolbarIconButton(GraphToolbarIcon.Open, T("editor.graph3d.open_large", "Open large graph"))
        {
            Width = 30,
            Height = 22,
            Margin = new Padding(2, 1, 0, 0)
        };
        openButton.Click += (_, _) => ShowGraph3DPreviewWindow();
        _graph3DCommandPanel.Controls.Add(openButton);

        _graph3DToggleTableButton = new GraphToolbarIconButton(GraphToolbarIcon.TableHidden, T("editor.graph3d.hide_table", "Hide 3D table"))
        {
            Width = 30,
            Height = 22,
            Margin = new Padding(2, 1, 0, 0)
        };
        _graph3DToggleTableButton.Click += (_, _) => SetGraph3DPointTableVisible(!_graph3DPointPanel.Visible);
        _graph3DCommandPanel.Controls.Add(_graph3DToggleTableButton);
        graphHost.Controls.Add(_graph3DCommandPanel);
        _graph3DCommandPanel.BringToFront();

        var graph3DRangePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = 382,
            Height = 75,
            ColumnCount = 6,
            RowCount = 3,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            BackColor = Color.Transparent
        };

        graph3DRangePanel.Controls.Add(MakeGraphToolbarLabel("X min"), 0, 0);
        _graph3DXMin = MakeGraphNumberBox(-100, -GraphPreviewRangeLimit, GraphPreviewRangeLimit, 1);
        _graph3DXMin.ValueChanged += (_, _) => { if (!IsEditingGraphNumberBox(_graph3DXMin)) ApplyGraph3DXRangeFromControls(); };
        graph3DRangePanel.Controls.Add(_graph3DXMin, 1, 0);
        graph3DRangePanel.Controls.Add(MakeGraphToolbarLabel("X max"), 2, 0);
        _graph3DXMax = MakeGraphNumberBox(100, -GraphPreviewRangeLimit, GraphPreviewRangeLimit, 1);
        _graph3DXMax.ValueChanged += (_, _) => { if (!IsEditingGraphNumberBox(_graph3DXMax)) ApplyGraph3DXRangeFromControls(); };
        graph3DRangePanel.Controls.Add(_graph3DXMax, 3, 0);
        graph3DRangePanel.Controls.Add(MakeGraphToolbarLabel(T("editor.graph.step", "Step")), 4, 0);
        _graph3DStep = MakeGraphNumberBox((decimal)GraphSurfaceApi.GetNumberBoxValue(_graphStep), GraphPreviewStepMinimum, GraphPreviewRangeLimit, 1);
        _graph3DStep.ValueChanged += (_, _) =>
        {
            if (!_applyingGraphSyncState && !_updatingGraphStepDisplay && !IsEditingGraphNumberBox(_graph3DStep))
                ApplyGraph3DStepFromControl();
        };
        graph3DRangePanel.Controls.Add(_graph3DStep, 5, 0);

        graph3DRangePanel.Controls.Add(MakeGraphToolbarLabel("Y min"), 0, 1);
        _graph3DYMin = MakeGraphNumberBox(-100, -GraphPreviewRangeLimit, GraphPreviewRangeLimit, 1);
        _graph3DYMin.ValueChanged += (_, _) => { if (!IsEditingGraphNumberBox(_graph3DYMin)) ApplyGraph3DYRangeFromControls(); };
        graph3DRangePanel.Controls.Add(_graph3DYMin, 1, 1);
        graph3DRangePanel.Controls.Add(MakeGraphToolbarLabel("Y max"), 2, 1);
        _graph3DYMax = MakeGraphNumberBox(100, -GraphPreviewRangeLimit, GraphPreviewRangeLimit, 1);
        _graph3DYMax.ValueChanged += (_, _) => { if (!IsEditingGraphNumberBox(_graph3DYMax)) ApplyGraph3DYRangeFromControls(); };
        graph3DRangePanel.Controls.Add(_graph3DYMax, 3, 1);
        graph3DRangePanel.Controls.Add(MakeGraphToolbarLabel("Grid step"), 4, 1);
        _graphZStep = MakeGraphNumberBox(10, GraphPreviewStepMinimum, GraphPreviewRangeLimit, 1);
        _graphZStep.ValueChanged += (_, _) =>
        {
            if (!_updatingGraphZStepDisplay && !IsEditingGraphNumberBox(_graphZStep))
                ApplyGraphPreviewZRangeFromControls();
        };
        graph3DRangePanel.Controls.Add(_graphZStep, 5, 1);

        graph3DRangePanel.Controls.Add(MakeGraphToolbarLabel("Z min"), 0, 2);
        _graphZMin = MakeGraphNumberBox(-100, -GraphPreviewRangeLimit, GraphPreviewRangeLimit, 1);
        _graphZMin.ValueChanged += (_, _) => { if (!IsEditingGraphNumberBox(_graphZMin)) ApplyGraphPreviewZRangeFromControls(); };
        graph3DRangePanel.Controls.Add(_graphZMin, 1, 2);
        graph3DRangePanel.Controls.Add(MakeGraphToolbarLabel("Z max"), 2, 2);
        _graphZMax = MakeGraphNumberBox(100, -GraphPreviewRangeLimit, GraphPreviewRangeLimit, 1);
        _graphZMax.ValueChanged += (_, _) => { if (!IsEditingGraphNumberBox(_graphZMax)) ApplyGraphPreviewZRangeFromControls(); };
        graph3DRangePanel.Controls.Add(_graphZMax, 3, 2);
        _graph3DShowRangeLines = new CheckBox
        {
            Text = T("editor.graph.lines", "Lines"),
            Checked = true,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(15, 63, 143),
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            Margin = new Padding(0, 3, 0, 0)
        };
        _graph3DShowRangeLines.CheckedChanged += (_, _) =>
        {
            UpdateGraph3DRangeInputMode();
            _graph3DCanvas?.Invalidate();
        };
        graph3DRangePanel.Controls.Add(_graph3DShowRangeLines, 4, 2);
        graph3DRangePanel.SetColumnSpan(_graph3DShowRangeLines, 2);
        graph3DRangePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
        graph3DRangePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68));
        graph3DRangePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
        graph3DRangePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68));
        graph3DRangePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        graph3DRangePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        graph3DRangePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        graph3DRangePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        graph3DRangePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        ApplyGraphInputTooltips(_graph3DXMin, _graph3DXMax, _graph3DYMin, _graph3DYMax, _graph3DStep, _graphZMin, _graphZMax, _graphZStep);
        AttachGraphCommittedInput(_graph3DXMin, ApplyGraph3DXRangeFromControls);
        AttachGraphCommittedInput(_graph3DXMax, ApplyGraph3DXRangeFromControls);
        AttachGraphCommittedInput(_graph3DYMin, ApplyGraph3DYRangeFromControls);
        AttachGraphCommittedInput(_graph3DYMax, ApplyGraph3DYRangeFromControls);
        AttachGraphCommittedInput(_graph3DStep, ApplyGraph3DStepFromControl);
        AttachGraphStepSpinner(_graph3DStep, CommitGraph3DStepSpinner);
        AttachGraphCommittedInput(_graphZMin, ApplyGraphPreviewZRangeFromControls);
        AttachGraphCommittedInput(_graphZMax, ApplyGraphPreviewZRangeFromControls);
        AttachGraphCommittedInput(_graphZStep, ApplyGraphPreviewZRangeFromControls);
        AttachGraphStepSpinner(_graphZStep, CommitGraphZStepSpinner);
        _graph3DRangePanel = graph3DRangePanel;
        UpdateGraph3DRangeInputMode();
        panel.Controls.Add(_graph3DRangePanel, 0, 0);

        var overlay = GraphPointTableOverlay.Create(
            GraphOverlayButtonDensity.Compact,
            T("editor.graph3d.points", "3D Points"),
            () => _graph3DPointerText,
            () => SetGraph3DPointTableVisible(false),
            Graph3DPointPanel_MouseDown,
            Graph3DPointPanel_MouseMove,
            Graph3DPointPanel_MouseUp,
            new[]
            {
                new GraphPointTableColumn("x", "x", 44),
                new GraphPointTableColumn("y", "y", 44),
                new GraphPointTableColumn("z", "z", 44)
            },
            tableWidthOverride: 148,
            tableHeightOverride: 64,
            titleProvider: () => IsGraph3DFlat2DView()
                ? T("editor.graph3d.points_2d", "2D Points")
                : T("editor.graph3d.points", "3D Points"));
        _graph3DPointPanel = overlay.Overlay;
        _graph3DPointTitleBar = overlay.TitleBar;
        _graph3DPointTable = overlay.Table;
        graphHost.Controls.Add(_graph3DPointPanel);
        _graph3DPointPanel.Visible = false;
        _graph3DPointPanel.BringToFront();
        graphHost.Resize += (_, _) =>
        {
            PlaceGraph3DOverlays();
            PlaceGraph3DPointOverlay();
            _graph3DCanvas.Invalidate();
        };
        PlaceGraph3DOverlays();
        PlaceGraph3DPointOverlay();
        UpdateGraph3DModeButtons();
        UpdateGraph3DPointTableAvailability();
        panel.Controls.Add(graphHost, 0, 1);

        return panel;
    }

    private GraphTextOverlayButton MakeGraph3DButton(string text, string tooltip, Action action)
    {
        return new GraphTextOverlayButton(text, tooltip, action);
    }

    private void PlaceGraph3DOverlays()
    {
        if (_graph3DCanvas.Parent is not Panel host)
            return;

        const int gap = 8;
        var surface = GetGraph3DCompactSurfaceBounds(host);
        if (_graph3DNavigationPanel is not null)
        {
            _graph3DNavigationPanel.Left = Math.Max(surface.Left + gap, surface.Right - _graph3DNavigationPanel.Width - gap);
            _graph3DNavigationPanel.Top = Math.Max(surface.Top + gap, surface.Bottom - _graph3DNavigationPanel.Height - gap);
            _graph3DNavigationPanel.BringToFront();
        }

        if (_graph3DRangePanel is not null && ReferenceEquals(_graph3DRangePanel.Parent, host))
        {
            _graph3DRangePanel.Left = Math.Max(surface.Left + gap, surface.Right - _graph3DRangePanel.Width - gap);
            _graph3DRangePanel.Top = surface.Top + gap;
            _graph3DRangePanel.BringToFront();
        }

        if (_graph3DRotationDial is { Visible: true })
        {
            _graph3DRotationDial.Left = surface.Left + gap;
            _graph3DRotationDial.Top = Math.Max(surface.Top + gap, surface.Bottom - _graph3DRotationDial.Height - gap - 16);
        }

        if (_graph3DCommandPanel is not null)
        {
            var left = surface.Left + gap;
            var navLeft = _graph3DNavigationPanel?.Left ?? surface.Right;
            var bottomControlsNeed = left + _graph3DCommandPanel.Width + gap + (_graph3DNavigationPanel?.Width ?? 0) + gap;
            if (bottomControlsNeed <= surface.Right)
            {
                var maxLeft = Math.Max(surface.Left + gap, navLeft - _graph3DCommandPanel.Width - gap);
                _graph3DCommandPanel.Left = Math.Min(left, maxLeft);
                _graph3DCommandPanel.Top = surface.Top + gap;
            }
            else
            {
                _graph3DCommandPanel.Left = surface.Left + gap;
                _graph3DCommandPanel.Top = surface.Top + gap;
            }
            _graph3DCommandPanel.BringToFront();
        }

        _graph3DPointPanel?.BringToFront();
    }

    private static void Graph3DOverlayPanel_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel panel)
            return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(0.5f, 0.5f, panel.Width - 1f, panel.Height - 1f);
        GraphOverlayStyle.PaintPanelChrome(e.Graphics, rect, 10f, translucent: false);
    }

    private void ToggleGraph3DFlat2DMode()
    {
        SetGraph3DCamera(IsGraph3DFlat2DView()
            ? GraphCameraPreset3D.Isometric
            : GraphCameraPreset3D.Front);
    }

    private void SetGraph3DCamera(GraphCameraPreset3D preset)
    {
        _graph3DCamera = Graph3DApi.CameraPreset(preset);
        if (IsGraph3DFlat2DView())
            SyncGraph3DFlatViewFromGraph2D();
        else
            ResetGraph3DSpaceView();
        if (_graph3DRotationDial is not null)
            _graph3DRotationDial.Camera = _graph3DCamera;
        UpdateGraph3DModeButtons();
        UpdateGraph3DRangeInputMode();
        _graph3DCanvas?.Invalidate();
        UpdateGraph3DPreviewWindowData();
    }

    private void RotateGraph3DCamera(double yaw, double pitch)
    {
        _graph3DCamera = Graph3DApi.RotateCamera(_graph3DCamera, yaw, pitch);
        if (_graph3DRotationDial is not null)
            _graph3DRotationDial.Camera = _graph3DCamera;
        UpdateGraph3DModeButtons();
        UpdateGraph3DRangeInputMode();
        _graph3DCanvas?.Invalidate();
        UpdateGraph3DPreviewWindowData();
    }

    private void ZoomGraph3DCamera(double factor)
    {
        if (IsGraph3DFlat2DView())
        {
            ZoomGraph3DFlatViewAt((float)(1d / factor), new PointF(_graph3DCanvas.ClientSize.Width / 2f, _graph3DCanvas.ClientSize.Height / 2f));
            return;
        }

        _graph3DCamera = Graph3DApi.ZoomCamera(_graph3DCamera, factor);
        if (_graph3DRotationDial is not null)
            _graph3DRotationDial.Camera = _graph3DCamera;
        UpdateGraph3DModeButtons();
        _graph3DCanvas?.Invalidate();
        UpdateGraph3DPreviewWindowData();
    }

    private void UpdateGraph3DModeButtons()
    {
        var flat2D = IsGraph3DFlat2DView();
        if (_graph3DFlat2DButton is not null)
            _graph3DFlat2DButton.Active = flat2D;
        if (_graph3DIsoButton is not null)
            _graph3DIsoButton.Active = !flat2D && Graph3DCameraMatches(GraphCameraPreset3D.Isometric);
        if (_graph3DTopButton is not null)
            _graph3DTopButton.Active = !flat2D && Graph3DCameraMatches(GraphCameraPreset3D.Top);
        if (_graph3DRotationDial is not null)
            _graph3DRotationDial.Visible = !flat2D;
        UpdateGraph3DPointTableMode();
        PlaceGraph3DOverlays();
    }

    private void UpdateGraph3DPointTableMode()
    {
        if (_graph3DPointTable is null || _graph3DPointTable.Columns.Count < 3)
            return;

        var showZ = !IsGraph3DFlat2DView();
        _graph3DPointTable.Columns[2].Visible = showZ;
        _graph3DPointPanel.Width = showZ ? 158 : 132;
        _graph3DPointTable.Width = showZ ? 148 : 122;
        _graph3DPointTable.Columns[0].Width = showZ ? 44 : 54;
        _graph3DPointTable.Columns[1].Width = showZ ? 44 : 54;
        _graph3DPointTable.Columns[2].Width = 44;
        _graph3DPointTitleBar.Width = _graph3DPointTable.Width;
        _graph3DPointTitleBar.Invalidate();
        KeepGraph3DPointOverlayInBounds();
    }

    private bool Graph3DCameraMatches(GraphCameraPreset3D preset)
    {
        var camera = Graph3DApi.CameraPreset(preset);
        return Math.Abs(NormalizeGraph3DAngle(_graph3DCamera.YawDegrees - camera.YawDegrees)) < 0.0001d &&
               Math.Abs(NormalizeGraph3DAngle(_graph3DCamera.PitchDegrees - camera.PitchDegrees)) < 0.0001d;
    }

    private static double NormalizeGraph3DAngle(double angle)
    {
        var normalized = angle % 360d;
        if (normalized > 180d)
            normalized -= 360d;
        if (normalized < -180d)
            normalized += 360d;
        return normalized;
    }

    private GraphPlotView3D GetGraph3DView()
    {
        var view = GetGraph3DXYView();
        var minZ = GraphSurfaceApi.GetNumberBoxValue(_graphZMin);
        var maxZ = GraphSurfaceApi.GetNumberBoxValue(_graphZMax);
        if (minZ >= maxZ)
        {
            minZ = -Graph3DDefaultHalfRange;
            maxZ = Graph3DDefaultHalfRange;
        }

        return Graph3DApi.From2D(view, minZ, maxZ);
    }

    private GraphPlotView GetGraph3DXYView()
    {
        return GetStoredGraph3DXYView();
    }

    private GraphPlotView GetStoredGraph3DXYView()
    {
        var view = new GraphPlotView(_graph3DViewMinX, _graph3DViewMaxX, _graph3DViewMinY, _graph3DViewMaxY);
        return GraphSurfaceApi.IsValidView(view)
            ? view
            : new GraphPlotView(-Graph3DDefaultHalfRange, Graph3DDefaultHalfRange, -Graph3DDefaultHalfRange, Graph3DDefaultHalfRange);
    }

    private void SetGraph3DXYView(GraphPlotView view)
    {
        _graph3DViewMinX = view.MinX;
        _graph3DViewMaxX = view.MaxX;
        _graph3DViewMinY = view.MinY;
        _graph3DViewMaxY = view.MaxY;
    }

    private void ResetGraph3DSpaceView()
    {
        var view = new GraphPlotView(
            -Graph3DDefaultHalfRange,
            Graph3DDefaultHalfRange,
            -Graph3DDefaultHalfRange,
            Graph3DDefaultHalfRange);
        SetGraph3DXYView(view);
        SetGraph3DXYRangeControls(view);
    }

    private void SyncGraph3DFlatViewFromGraph2D()
    {
        var view = GetGraphPreviewView();
        SetGraph3DXYView(view);
        SetGraph3DXYRangeControls(view);
    }

    private void SetGraph3DXYRangeControls(GraphPlotView view)
    {
        if (_graph3DXMin is null || _graph3DXMax is null || _graph3DYMin is null || _graph3DYMax is null)
            return;

        _updatingGraphXRangeControls = true;
        _updatingGraphYRangeControls = true;
        try
        {
            GraphSurfaceApi.SetNumberBoxValue(_graph3DXMin, view.MinX);
            GraphSurfaceApi.SetNumberBoxValue(_graph3DXMax, view.MaxX);
            GraphSurfaceApi.SetNumberBoxValue(_graph3DYMin, view.MinY);
            GraphSurfaceApi.SetNumberBoxValue(_graph3DYMax, view.MaxY);
        }
        finally
        {
            _updatingGraphYRangeControls = false;
            _updatingGraphXRangeControls = false;
        }
    }

    private void Graph3DCanvas_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.Clear(_graph3DCanvas.BackColor);
        var surface = GetGraph3DCompactSurfaceBounds(_graph3DCanvas);
        var state = e.Graphics.Save();
        e.Graphics.SetClip(surface);
        using var renderSurface = CreateGraph3DRenderSurface(_graph3DCanvas, surface);

        if (IsGraph3DFlat2DView())
        {
            DrawGraph3DAsGraph2D(e.Graphics, renderSurface);
            e.Graphics.Restore(state);
            return;
        }

        Graph3DApi.Draw(
            e.Graphics,
            renderSurface,
            _graphPreviewPoints,
            _graphPreviewStepPoints,
            GetGraph3DView(),
            _graph3DCamera,
            _graph3DGridVisible,
            showBoundaryFields: true,
            _graph3DShowRangeLines?.Checked ?? true,
            GraphSurfaceApi.GetNumberBoxValue(_graphZStep),
            _graphDisabledMessage,
            "Generate graph",
            GraphPlotDensity.Compact);
        if (_graph3DRotationDial is { Visible: true })
        {
            Graph3DApi.DrawCompass(e.Graphics, _graph3DRotationDial.Bounds, _graph3DCamera);
            Graph3DApi.DrawCompassDegrees(e.Graphics, _graph3DRotationDial.Bounds, _graph3DCamera);
        }
        e.Graphics.Restore(state);
    }

    private static Rectangle GetGraph3DCompactSurfaceBounds(Control host)
    {
        var height = host.ClientSize.Height;
        var width = host.ClientSize.Width;
        if (height <= 0 || width <= 0)
            return host.ClientRectangle;

        var surfaceWidth = Math.Min(width, Math.Max(420, height * 2));
        return new Rectangle(0, 0, surfaceWidth, height);
    }

    private static Control CreateGraph3DRenderSurface(Control canvas, Rectangle bounds)
    {
        return new GraphRenderSurface(bounds.Size, canvas.Font);
    }

    private sealed class GraphRenderSurface : Control
    {
        public GraphRenderSurface(Size clientSize, Font font)
        {
            ClientSize = clientSize;
            Font = font;
        }
    }

    private bool IsGraph3DFlat2DView()
    {
        static double AngleDistance(double angle, double target)
        {
            var delta = Math.Abs((angle - target) % 360d);
            return delta > 180d ? 360d - delta : delta;
        }

        return AngleDistance(_graph3DCamera.YawDegrees, 0d) < 0.0001d &&
               Math.Abs(_graph3DCamera.PitchDegrees) < 0.0001d;
    }

    private void UpdateGraph3DZRangeMode()
    {
        if (_graphZMin is null || _graphZMax is null)
            return;

        var flat2D = IsGraph3DFlat2DView();
        var linesEditable = _graph3DShowRangeLines?.Checked ?? true;
        if (flat2D)
        {
            _updatingGraphZRangeControls = true;
            try
            {
                GraphSurfaceApi.SetNumberBoxValue(_graphZMin, 0d);
                GraphSurfaceApi.SetNumberBoxValue(_graphZMax, 0d);
            }
            finally
            {
                _updatingGraphZRangeControls = false;
            }
        }

        var zEditable = linesEditable && !flat2D;
        _graphZMin.Enabled = zEditable;
        _graphZMax.Enabled = zEditable;
        _graphZMin.ReadOnly = !zEditable;
        _graphZMax.ReadOnly = !zEditable;
    }

    private void UpdateGraph3DRangeInputMode()
    {
        if (_graph3DXMin is null || _graph3DXMax is null || _graph3DYMin is null || _graph3DYMax is null)
            return;

        var editable = _graph3DShowRangeLines?.Checked ?? true;
        var flat2D = IsGraph3DFlat2DView();
        if (!editable)
            SetGraph3DXYRangeControls(GetGraph3DXYView());

        foreach (var box in new[] { _graph3DXMin, _graph3DXMax, _graph3DYMin, _graph3DYMax, _graph3DStep })
        {
            if (box is null)
                continue;

            box.Enabled = editable;
            box.ReadOnly = !editable;
        }

        if (_graphZStep is not null)
        {
            _graphZStep.Enabled = editable && !flat2D;
            _graphZStep.ReadOnly = !editable || flat2D;
        }

        if (_graph3DGridToggle is not null)
            _graph3DGridToggle.Enabled = !flat2D;

        UpdateGraph3DZRangeMode();
    }

    private void DrawGraph3DAsGraph2D(Graphics graphics, Control canvas)
    {
        if (!_graphHasView && _graphPreviewDocument is not null)
            ResetGraphPreviewView(invalidate: false);

        var view = GetGraph3DXYView();
        GraphSurfaceApi.Draw(
            graphics,
            canvas,
            _graphPreviewPoints,
            _graphPreviewStepPoints,
            view,
            GraphSurfaceApi.GetNumberBoxValue(_graph3DXMin),
            GraphSurfaceApi.GetNumberBoxValue(_graph3DXMax),
            GraphSurfaceApi.GetNumberBoxValue(_graph3DStep),
            _graphDisabledMessage,
            "Generate graph",
            GraphPlotDensity.Compact,
            (float)GraphSurfaceApi.GetNumberBoxValue(_graph3DYMin),
            (float)GraphSurfaceApi.GetNumberBoxValue(_graph3DYMax),
            _graph3DShowRangeLines?.Checked ?? true);
    }

    private void Graph3DCanvas_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button is not (MouseButtons.Left or MouseButtons.Right))
            return;

        _graph3DDragging = true;
        _graph3DDragButton = e.Button;
        _graph3DDragStart = e.Location;
        if (IsGraph3DFlat2DView() || e.Button == MouseButtons.Right)
        {
            if (IsGraph3DFlat2DView() && !EnsureGraphPreviewGeneratedForInteraction())
            {
                _graph3DDragging = false;
                return;
            }

            var view = GetGraph3DXYView();
            _graphPanStartMinX = view.MinX;
            _graphPanStartMaxX = view.MaxX;
            _graphPanStartMinY = view.MinY;
            _graphPanStartMaxY = view.MaxY;
        }

        _graph3DCanvas.Cursor = GraphCursors.Pan;
    }

    private void Graph3DCanvas_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_graph3DDragging)
            return;

        if (IsGraph3DFlat2DView())
        {
            PanGraph3DFlatView(e.Location);
            return;
        }

        if (_graph3DDragButton == MouseButtons.Right)
        {
            PanGraph3DCamera(e.Location);
            return;
        }

        var dx = e.X - _graph3DDragStart.X;
        var dy = e.Y - _graph3DDragStart.Y;
        _graph3DDragStart = e.Location;
        RotateGraph3DCamera(dx * 0.6d, -dy * 0.45d);
    }

    private void Graph3DCanvas_MouseUp(object? sender, MouseEventArgs e)
    {
        _graph3DDragging = false;
        _graph3DDragButton = MouseButtons.None;
        _graph3DCanvas.Cursor = _graph3DCanvas.ClientRectangle.Contains(e.Location) ? GraphCursors.Pan : GraphCursors.Default;
    }

    private void Graph3DCanvas_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (IsGraph3DFlat2DView())
        {
            if (EnsureGraphPreviewGeneratedForInteraction())
                ZoomGraph3DFlatViewAt(e.Delta > 0 ? 0.85f : 1.18f, e.Location);
            return;
        }

        ZoomGraph3DCamera(e.Delta > 0 ? 1.12d : 1d / 1.12d);
    }

    private void ZoomGraph3DFlatViewAt(float factor, PointF screenPoint)
    {
        var plot = GraphSurfaceApi.GetPlotRectangle(_graph3DCanvas);
        if (plot.Width <= 0 || plot.Height <= 0)
            return;

        var view = GetGraph3DXYView();
        if (!GraphSurfaceApi.IsValidView(view))
            return;

        var anchor = GraphSurfaceApi.ScreenToGraph(screenPoint, plot, view);
        var newWidth = (view.MaxX - view.MinX) * factor;
        var newHeight = (view.MaxY - view.MinY) * factor;
        if (newWidth < GraphSurfaceApi.MinimumViewSpan || newHeight < GraphSurfaceApi.MinimumViewSpan)
            return;

        var xRatio = (anchor.X - view.MinX) / (view.MaxX - view.MinX);
        var yRatio = (anchor.Y - view.MinY) / (view.MaxY - view.MinY);
        var nextView = new GraphPlotView(
            anchor.X - newWidth * xRatio,
            anchor.X - newWidth * xRatio + newWidth,
            anchor.Y - newHeight * yRatio,
            anchor.Y - newHeight * yRatio + newHeight);

        ApplyGraph3DXYView(nextView, regenerate: false);
    }

    private void PanGraph3DFlatView(Point location)
    {
        var plot = GraphSurfaceApi.GetPlotRectangle(_graph3DCanvas);
        if (plot.Width <= 0 || plot.Height <= 0)
            return;

        var dx = location.X - _graph3DDragStart.X;
        var dy = location.Y - _graph3DDragStart.Y;
        var graphDx = dx / (double)plot.Width * (_graphPanStartMaxX - _graphPanStartMinX);
        var graphDy = dy / (double)plot.Height * (_graphPanStartMaxY - _graphPanStartMinY);
        var nextView = new GraphPlotView(
            _graphPanStartMinX - graphDx,
            _graphPanStartMaxX - graphDx,
            _graphPanStartMinY + graphDy,
            _graphPanStartMaxY + graphDy);

        ApplyGraph3DXYView(nextView, regenerate: false);
    }

    private void PanGraph3DCamera(Point location)
    {
        var dx = location.X - _graph3DDragStart.X;
        var dy = location.Y - _graph3DDragStart.Y;
        _graph3DDragStart = location;
        _graph3DCamera = _graph3DCamera with
        {
            PanX = _graph3DCamera.PanX + dx,
            PanY = _graph3DCamera.PanY + dy
        };
        _graph3DRotationDial.Camera = _graph3DCamera;
        _graph3DCanvas?.Invalidate();
        UpdateGraph3DPreviewWindowData();
    }

    private void InitializeGraphPreviewBaseView()
    {
        var view = GraphSurfaceApi.MatchViewToCanvasAspect(
            new GraphPlotView(GraphSurfaceApi.GetNumberBoxValue(_graphXMin), GraphSurfaceApi.GetNumberBoxValue(_graphXMax), GraphSurfaceApi.GetNumberBoxValue(_graphYMin), GraphSurfaceApi.GetNumberBoxValue(_graphYMax)),
            _graphCanvas);
        SetGraphPreviewView(view);
        _graphMarkerView = view;
        SetGraphPreviewRangeControls(view);
        _graphHasView = true;
        _graphDisabledMessage = "";
    }

    private void ApplyGraphInputTooltips(params Control[] controls)
    {
        var text = T(
            "editor.graph.input_tooltip",
            "Voorbeelden:\r\n1,05\r\n0,5 = 5 x 10⁻¹\r\n0,0000005 = 500 n = 5 x 10⁻⁷\r\n10⁵ (ook: 10^5)\r\n10⁻⁵ (ook: 10^-5)\r\n1e-5\r\n10 n\r\n2 k");
        foreach (var control in controls)
            _graphInputToolTip.SetToolTip(control, text);
    }

    private void AttachGraphCommittedInput(NumericUpDown box, Action commit)
    {
        box.TextChanged += (_, _) =>
        {
            if (box.Focused)
                _editedGraphNumberBoxes.Add(box);
        };
        box.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;
            CommitEditedGraphNumberBox(box, commit);
        };
        box.Leave += (_, _) => CommitEditedGraphNumberBox(box, commit);
    }

    private bool IsEditingGraphNumberBox(NumericUpDown box)
    {
        return _editedGraphNumberBoxes.Contains(box);
    }

    private void CommitEditedGraphNumberBox(NumericUpDown box, Action commit)
    {
        if (!_editedGraphNumberBoxes.Remove(box))
            return;

        box.Validate();
        GraphSurfaceApi.CommitNumberBoxValue(box);
        commit();
    }

    private void AttachGraphStepSpinner(NumericUpDown box, Action<decimal> commit)
    {
        box.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Up && e.KeyCode != Keys.Down)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;
            commit(GetNextGraphScaleAwareStep(GraphSurfaceApi.GetNumberBoxValue(box), e.KeyCode == Keys.Up));
        };
        box.MouseWheel += (_, e) =>
        {
            ((HandledMouseEventArgs)e).Handled = true;
            commit(GetNextGraphScaleAwareStep(GraphSurfaceApi.GetNumberBoxValue(box), e.Delta > 0));
        };
    }

    private void CommitGraphStepSpinner(decimal value)
    {
        var clamped = Math.Clamp(value, _graphStep.Minimum, _graphStep.Maximum);
        _graphUserStep = (double)clamped;
        SetGraphStepDisplay(_graphUserStep);
        if (!_applyingGraphSyncState)
            GenerateGraphPreview();
    }

    private void CommitGraph3DStepSpinner(decimal value)
    {
        var clamped = Math.Clamp(value, _graph3DStep.Minimum, _graph3DStep.Maximum);
        _applyingGraph3DRangeControls = true;
        try
        {
            _graphUserStep = (double)clamped;
            SetGraphStepDisplay(_graphUserStep);
            if (!_applyingGraphSyncState)
                GenerateGraphPreview();
        }
        finally
        {
            _applyingGraph3DRangeControls = false;
        }
    }

    private void CommitGraphZStepSpinner(decimal value)
    {
        var clamped = Math.Clamp(value, _graphZStep.Minimum, _graphZStep.Maximum);
        SetGraphZStepDisplay((double)clamped);
        ApplyGraphPreviewZRangeFromControls();
    }

    private void SetGraphZStepDisplay(double value)
    {
        if (!double.IsFinite(value) || value <= 0d)
            return;

        _updatingGraphZStepDisplay = true;
        try
        {
            GraphSurfaceApi.SetNumberBoxValue(_graphZStep, value);
        }
        finally
        {
            _updatingGraphZStepDisplay = false;
        }
    }

    private static decimal GetNextGraphScaleAwareStep(double current, bool increase)
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
            return current < 1d ? GraphPreviewStepMinimum : GraphPreviewRangeLimit;
        }
    }

    private void SetGraphPointTableVisible(bool visible)
    {
        _graphPointTableRequestedVisible = visible;
        ApplyGraphPointTableVisibility();
    }

    private void ApplyGraphPointTableVisibility()
    {
        var hasRows = _graphPointTable.Rows.Count > 0;
        var visible = _graphPointTableRequestedVisible && hasRows;
        _graphPointPanel.Visible = visible;
        UpdateGraphPointerStatusVisibility();
        if (_graphToggleTableButton is GraphToolbarIconButton iconButton)
        {
            iconButton.Icon = visible ? GraphToolbarIcon.TableHidden : GraphToolbarIcon.TableVisible;
            iconButton.TooltipText = visible ? T("editor.graph.hide_table", "Hide table") : T("editor.graph.show_table", "Show table");
        }
        _graphCanvas.Invalidate();
    }

    private void UpdateGraphPointTableAvailability()
    {
        var hasRows = _graphPointTable.Rows.Count > 0;
        _graphToggleTableButton.Enabled = hasRows;
        ApplyGraphPointTableVisibility();
    }

    private void UpdateGraphPointerStatusVisibility()
    {
        if (_graphPointerStatusPanel is null)
            return;

        _graphPointerStatusPanel.Visible = false;
    }

    private void UpdateGraphControlTooltips()
    {
        if (_graphPointPanel is not null)
            SetGraphPointTableVisible(_graphPointPanel.Visible);
        if (_graph3DPointPanel is not null)
            SetGraph3DPointTableVisible(_graph3DPointPanel.Visible);
    }

    private void PlaceGraphPointOverlay()
    {
        if (_graphPointPanel.Parent is null)
            return;

        if (_graphPointPanel.Left <= 0 && _graphPointPanel.Top <= 0)
        {
            _graphPointPanel.Left = 10;
            _graphPointPanel.Top = 10;
        }

        KeepGraphPointOverlayInBounds();
        _graphPointPanel.BringToFront();
        _graphPointerStatusPanel?.BringToFront();
        UpdateGraphPointerStatusVisibility();
    }

    private void GraphPointPanel_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || sender is not Control control || _graphPointPanel.Parent is null)
            return;

        _draggingGraphPointPanel = true;
        _graphPointPanelDragStart = _graphPointPanel.Parent.PointToClient(control.PointToScreen(e.Location));
        _graphPointPanelStartLocation = _graphPointPanel.Location;
        _graphPointPanel.Cursor = Cursors.Hand;
        _graphPointTable.Cursor = Cursors.Hand;
        _graphPointPanel.BringToFront();
    }

    private void GraphPointPanel_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_draggingGraphPointPanel || sender is not Control control || _graphPointPanel.Parent is null)
            return;

        var current = _graphPointPanel.Parent.PointToClient(control.PointToScreen(e.Location));
        _graphPointPanel.Left = _graphPointPanelStartLocation.X + current.X - _graphPointPanelDragStart.X;
        _graphPointPanel.Top = _graphPointPanelStartLocation.Y + current.Y - _graphPointPanelDragStart.Y;
        KeepGraphPointOverlayInBounds();
    }

    private void GraphPointPanel_MouseUp(object? sender, MouseEventArgs e)
    {
        _draggingGraphPointPanel = false;
        _graphPointPanel.Cursor = Cursors.Default;
        _graphPointTable.Cursor = Cursors.Default;
    }

    private void KeepGraphPointOverlayInBounds()
    {
        if (_graphPointPanel.Parent is null)
            return;

        var parent = _graphPointPanel.Parent.ClientSize;
        _graphPointPanel.Left = Math.Clamp(_graphPointPanel.Left, 6, Math.Max(6, parent.Width - _graphPointPanel.Width - 6));
        _graphPointPanel.Top = Math.Clamp(_graphPointPanel.Top, 6, Math.Max(6, parent.Height - _graphPointPanel.Height - 6));
    }

    private void SetGraph3DPointTableVisible(bool visible)
    {
        _graph3DPointTableRequestedVisible = visible;
        ApplyGraph3DPointTableVisibility();
    }

    private void ApplyGraph3DPointTableVisibility()
    {
        if (_graph3DPointTable is null || _graph3DPointPanel is null)
            return;

        var hasRows = _graph3DPointTable.Rows.Count > 0;
        var visible = _graph3DPointTableRequestedVisible && hasRows;
        _graph3DPointPanel.Visible = visible;
        if (_graph3DToggleTableButton is not null)
        {
            _graph3DToggleTableButton.Enabled = hasRows;
            _graphInputToolTip.SetToolTip(
                _graph3DToggleTableButton,
                visible ? T("editor.graph3d.hide_table", "Hide 3D table") : T("editor.graph3d.show_table", "Show 3D table"));
            if (_graph3DToggleTableButton is GraphToolbarIconButton iconButton)
            {
                iconButton.Icon = visible ? GraphToolbarIcon.TableHidden : GraphToolbarIcon.TableVisible;
                iconButton.TooltipText = visible ? T("editor.graph3d.hide_table", "Hide 3D table") : T("editor.graph3d.show_table", "Show 3D table");
            }
        }
        _graph3DCanvas?.Invalidate();
    }

    private void UpdateGraph3DPointTableAvailability()
    {
        if (_graph3DPointTable is null)
            return;

        ApplyGraph3DPointTableVisibility();
    }

    private void PlaceGraph3DPointOverlay()
    {
        if (_graph3DPointPanel is null || _graph3DPointPanel.Parent is null)
            return;

        if (_graph3DPointPanel.Left <= 0 && _graph3DPointPanel.Top <= 0)
        {
            _graph3DPointPanel.Left = 10;
            _graph3DPointPanel.Top = 10;
        }

        KeepGraph3DPointOverlayInBounds();
        _graph3DPointPanel.BringToFront();
    }

    private void Graph3DPointPanel_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || sender is not Control control || _graph3DPointPanel.Parent is null)
            return;

        _draggingGraph3DPointPanel = true;
        _graph3DPointPanelDragStart = _graph3DPointPanel.Parent.PointToClient(control.PointToScreen(e.Location));
        _graph3DPointPanelStartLocation = _graph3DPointPanel.Location;
        _graph3DPointPanel.Cursor = Cursors.Hand;
        _graph3DPointTable.Cursor = Cursors.Hand;
        _graph3DPointPanel.BringToFront();
    }

    private void Graph3DPointPanel_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_draggingGraph3DPointPanel || sender is not Control control || _graph3DPointPanel.Parent is null)
            return;

        var current = _graph3DPointPanel.Parent.PointToClient(control.PointToScreen(e.Location));
        _graph3DPointPanel.Left = _graph3DPointPanelStartLocation.X + current.X - _graph3DPointPanelDragStart.X;
        _graph3DPointPanel.Top = _graph3DPointPanelStartLocation.Y + current.Y - _graph3DPointPanelDragStart.Y;
        KeepGraph3DPointOverlayInBounds();
    }

    private void Graph3DPointPanel_MouseUp(object? sender, MouseEventArgs e)
    {
        _draggingGraph3DPointPanel = false;
        _graph3DPointPanel.Cursor = Cursors.Default;
        _graph3DPointTable.Cursor = Cursors.Default;
    }

    private void KeepGraph3DPointOverlayInBounds()
    {
        if (_graph3DPointPanel.Parent is null)
            return;

        var bounds = _graph3DCanvas is not null
            ? _graph3DCanvas.Bounds
            : new Rectangle(Point.Empty, _graph3DPointPanel.Parent.ClientSize);
        _graph3DPointPanel.Left = Math.Clamp(
            _graph3DPointPanel.Left,
            bounds.Left + 6,
            Math.Max(bounds.Left + 6, bounds.Right - _graph3DPointPanel.Width - 6));
        _graph3DPointPanel.Top = Math.Clamp(
            _graph3DPointPanel.Top,
            bounds.Top + 6,
            Math.Max(bounds.Top + 6, bounds.Bottom - _graph3DPointPanel.Height - 6));
    }

    private static void CompactGraphStatusPanel_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel panel)
            return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
        using var path = CreateRoundedRectanglePath(rect, 9);
        using var fill = new SolidBrush(Color.FromArgb(248, 252, 255));
        using var border = new Pen(Color.FromArgb(232, 240, 250), 1);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
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

    private void ShowGraphPreviewWindow()
    {
        if (_graphPreviewForm is { IsDisposed: false })
        {
            _graphPreviewForm.BringToFront();
            _graphPreviewForm.Focus();
            return;
        }

        _graphPreviewForm = new GraphPreviewForm(() => CurrentEditor?.Text ?? "", _language)
        {
            StartPosition = FormStartPosition.Manual,
            Location = PointToScreen(new Point(Math.Max(60, Width - 760), 110)),
            TopMost = TopMost
        };
        _graphPreviewForm.SyncStateChanged += GraphPreviewForm_SyncStateChanged;
        _graphPreviewForm.FormClosed += (_, _) =>
        {
            if (_graphPreviewForm is not null)
                _graphPreviewForm.SyncStateChanged -= GraphPreviewForm_SyncStateChanged;
            _graphPreviewForm = null;
        };
        _graphPreviewForm.Show(this);
        if (_graphHasView)
            _graphPreviewForm.ApplySyncState(CreateGraphPreviewSyncState());
    }

    private void ShowGraph3DPreviewWindow()
    {
        if (_graph3DPreviewForm is { IsDisposed: false })
        {
            _graph3DPreviewForm.BringToFront();
            _graph3DPreviewForm.Focus();
            UpdateGraph3DPreviewWindowData();
            return;
        }

        if (!_graphHasView && ShouldGenerateGraphPreview())
            GenerateGraphPreview();

        _graph3DPreviewForm = new Graph3DPreviewForm
        {
            StartPosition = FormStartPosition.Manual,
            Location = PointToScreen(new Point(Math.Max(80, Width - 860), 130)),
            TopMost = TopMost
        };
        _graph3DPreviewForm.SyncStateChanged += Graph3DPreviewForm_SyncStateChanged;
        _graph3DPreviewForm.FormClosed += (_, _) =>
        {
            if (_graph3DPreviewForm is not null)
                _graph3DPreviewForm.SyncStateChanged -= Graph3DPreviewForm_SyncStateChanged;
            _graph3DPreviewForm = null;
        };
        _graph3DPreviewForm.Show(this);
        UpdateGraph3DPreviewWindowData();
    }

    private void UpdateGraph3DPreviewWindowData()
    {
        if (_graph3DPreviewForm is null || _graph3DPreviewForm.IsDisposed)
            return;
        if (_applyingGraph3DPreviewSyncState)
            return;

        var flat2D = IsGraph3DFlat2DView();
        var view = flat2D
            ? Graph3DApi.From2D(GetGraphPreviewView(), -Graph3DDefaultHalfRange, Graph3DDefaultHalfRange)
            : GetGraph3DView();
        var markerView = flat2D
            ? _graphMarkerView
            : new GraphPlotView(view.MinX, view.MaxX, view.MinY, view.MaxY);

        _graph3DPreviewForm.SetData(
            _graphPreviewPoints,
            _graphPreviewStepPoints,
            view,
            markerView,
            _graph3DCamera,
            _graphUserStep,
            GraphSurfaceApi.GetNumberBoxValue(_graphZStep),
            _graph3DGridVisible,
            _graph3DShowRangeLines?.Checked ?? true,
            _graphDisabledMessage);
    }

    private void Graph3DPreviewForm_SyncStateChanged(object? sender, Graph3DPreviewSyncState state)
    {
        ApplyGraph3DPreviewSyncState(state);
    }

    private void GraphPreviewForm_SyncStateChanged(object? sender, GraphPreviewSyncState state)
    {
        ApplyGraphPreviewSyncState(state);
    }

    private void InvalidateGraphPreviewData()
    {
        _graphDataTextSignature = "";
        _graphPreviewDocument = null;
        _graphPreviewFitPoints.Clear();
        _graphPreviewPoints.Clear();
        _graphPreviewStepPoints.Clear();
        _graphPointClipboardText = "";
        _graph3DPointClipboardText = "";
        if (_graphPointTable is not null)
            FillGraphPointTable([]);
        _graphCanvas?.Invalidate();
        _graph3DCanvas?.Invalidate();
        _graphPreviewForm?.InvalidateGraphData();
        _graph3DPreviewForm?.InvalidateGraphData();
    }

    private void ShowAbout()
    {
        using var form = new AboutForm(
            _language,
            LanguageCatalog.ListAvailable(AppContext.BaseDirectory),
            _language.FileName,
            AppVersionInfo.ReleaseChannel,
            _language.PackageId)
        {
            TopMost = TopMost
        };

        if (form.ShowDialog(this) == DialogResult.OK && form.SelectedLanguage is not null)
        {
            if (form.SelectedLanguage.Matches(_language.FileName, _language.PackageId))
                return;

            LanguageCatalog.SaveConfigured(AppContext.BaseDirectory, form.SelectedLanguage);
            _language = LanguageCatalog.Load(
                AppContext.BaseDirectory,
                form.SelectedLanguage.FileName,
                form.SelectedLanguage.PackageId);
            ApplyLanguageImmediately();
        }
    }

    private void ApplyLanguageImmediately()
    {
        RebuildMenuAndToolbar();
        ApplyPanelLanguage();
        _statusLabel.Text = T("status.language_changed", "Language changed.");
        UpdateGraphControlTooltips();
        UpdateUiState();
        _graphPreviewForm?.Close();
    }

    private void RebuildMenuAndToolbar()
    {
        _topStripPanel.Controls.Remove(_menuStrip);
        _menuStrip.Dispose();
        _topStripPanel.Controls.Remove(_toolStrip);
        _toolStrip.Dispose();

        BuildToolbar();
        BuildMenu();
        ArrangeTopStrips();
    }

    private void ArrangeTopStrips()
    {
        _topStripPanel.Controls.SetChildIndex(_toolStrip, 0);
        _topStripPanel.Controls.SetChildIndex(_menuStrip, 1);
        _topStripPanel.Height = _menuStrip.Height + _toolStrip.Height;
    }

    private void ApplyGraph3DFocusLayout()
    {
        _graph3DCanvas?.Invalidate();
    }

    private void ApplyPanelLanguage()
    {
        _testGroup.Text = T("editor.test.title", "Converter test") + "  \u00D7";
        _converterTestPage.Text = T("editor.test.title", "Converter test");
        _graphPreviewPage.Text = T("editor.graph.title", "Graph 2D");
        _testInputLabel.Text = T("editor.test.input", "Test input");
        _testOutputLabel.Text = T("editor.test.output", "Output");
        _testFormulaLabel.Text = T("editor.test.formula", "Formula");
        _testCalculationLabel.Text = T("editor.test.calculation", "Calculation");
        _testButton.Text = T("editor.toolbar.test", "Test");
        _validateButton.Text = T("editor.toolbar.validate", "Validate");
        _metadataPreviewGroup.Text = T("editor.preview.title", "Live preview") + "  \u00D7";
        _simulatorPreviewGroup.Text = T("editor.sim.title", "Converter simulator") + "  \u00D7";
        _simTitle.Text = T("editor.sim.window_title", "Converter preview");
        _simFileLabel.Text = T("editor.sim.file", "File:");
        _simDigitGroup.Text = T("option.digit_group", "Digit group");
        _simDecimals.Text = T("option.decimals", "Decimals");

        if (_simMenuStrip.Items.Count >= 5)
        {
            _simMenuStrip.Items[0].Text = T("editor.menu.file", "File");
            _simMenuStrip.Items[1].Text = T("editor.menu.edit", "Edit");
            _simMenuStrip.Items[2].Text = SimConfigMenuText();
            _simMenuStrip.Items[3].Text = T("editor.menu.tools", "Tools");
            _simMenuStrip.Items[4].Text = SimAboutMenuText();
        }
    }

    private string SimConfigMenuText()
        => HelpLanguageCode() switch
        {
            "ned" => "Config",
            "deu" => "Config",
            "fra" => "Configuration",
            "ita" => "Config",
            "spa" => "Config",
            "por" => "Config",
            "ind" => "Config",
            "zho" => "配置",
            _ => "Config"
        };

    private string SimAboutMenuText()
        => HelpLanguageCode() switch
        {
            "ned" => "Over",
            "deu" => "Info",
            "fra" => "A propos",
            "ita" => "Informazioni",
            "spa" => "Acerca de",
            "por" => "Sobre",
            "ind" => "Tentang",
            "zho" => "关于",
            _ => "About"
        };

    private static Label MakeGraphToolbarLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0)
        };
    }

    private static NumericUpDown MakeGraphNumberBox(decimal value, decimal minimum, decimal maximum, decimal increment)
    {
        return new NumericUpDown
        {
            DecimalPlaces = 1,
            Minimum = minimum,
            Maximum = maximum,
            Increment = increment,
            Tag = (double)value,
            Value = value,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Width = 80,
            Margin = new Padding(0)
        };
    }

    // Genereert grafiekpunten door huidige NOD als y=f(x) uit te voeren.
    private void GenerateGraphPreview()
    {
        if (IsPowerResumeQuietPeriod())
        {
            _powerResumeGraphPending = ShouldGenerateGraphPreview();
            _powerResumeTimer.Stop();
            _powerResumeTimer.Start();
            return;
        }

        if (!ShouldGenerateGraphPreview())
            return;

        var editor = CurrentEditor;
        if (editor is null || _graphCanvas is null || _graphPointTable is null)
            return;

        _graphPreviewFitPoints.Clear();
        _graphPreviewPoints.Clear();
        _graphPreviewStepPoints.Clear();
        FillGraphPointTable([]);
        var graphTextSignature = editor.Text;

        try
        {
            var min = _graphSampleMinX;
            var max = _graphSampleMaxX;
            var step = _graphUserStep;

            if (min > max)
            {
                _graphStatus.Text = "X min moet kleiner of gelijk zijn aan X max.";
                _graphCanvas.Invalidate();
                return;
            }

            if (step <= 0)
            {
                _graphStatus.Text = T("editor.graph.step_positive", "Step must be greater than 0.");
                _graphCanvas.Invalidate();
                return;
            }

            _graphPreviewDocument = NodParser.Parse(editor.Text);
            if (!IsGraphPreviewCompatible(_graphPreviewDocument, out var disabledReason))
            {
                _graphDisabledMessage = disabledReason;
                _graphPreviewDocument = null;
                _graphHasView = false;
                _graphPreviewFitPoints.Clear();
                _graphPreviewPoints.Clear();
                _graphPreviewStepPoints.Clear();
                FillGraphPointTable([]);
                SetGraphPointTableVisible(false);
                _graphCanvas.Invalidate();
                _graph3DCanvas?.Invalidate();
                return;
            }

            _graphDisabledMessage = "";
            var skipped = 0;
            var visibleMin = Math.Min(min, max);
            var visibleMax = Math.Max(min, max);
            var lineSamples = Math.Clamp(_graphCanvas.ClientSize.Width / 6, 80, GraphPreviewMaxLineSamplePoints);
            var lineStep = (visibleMax - visibleMin) / Math.Max(1, lineSamples);
            if (!double.IsFinite(lineStep) || lineStep <= 0)
                lineStep = Math.Max(1.0, step);

            for (var i = 0; i <= lineSamples; i++)
            {
                var x = visibleMin + lineStep * i;
                var input = FormatGraphPreviewInput(x);

                try
                {
                    var result = NodEngine.ConvertForward(_graphPreviewDocument, input);
                    if (TryGetGraphNumber(result, out var y))
                    {
                        var point = new PointF((float)x, (float)y);
                        _graphPreviewFitPoints.Add(point);
                        _graphPreviewPoints.Add(point);
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

            var displayStep = ChooseGraphDisplayedStep(step, visibleMin, visibleMax, GraphPreviewMaxVisibleStepPoints);
            SetGraphStepDisplay(displayStep);
            var firstStep = Math.Ceiling(visibleMin / displayStep) * displayStep;
            for (var i = 0; i < GraphPreviewMaxVisibleStepPoints; i++)
            {
                var x = firstStep + displayStep * i;
                if (!double.IsFinite(x) || x > visibleMax + displayStep / 1000.0)
                    break;

                var input = FormatGraphPreviewInput(x);
                try
                {
                    var result = NodEngine.ConvertForward(_graphPreviewDocument, input);
                    if (TryGetGraphNumber(result, out var y))
                        _graphPreviewStepPoints.Add(new PointF((float)x, (float)y));
                    else
                        skipped++;
                }
                catch
                {
                    skipped++;
                }
            }

            FillGraphPointTable(_graphPreviewStepPoints);
            _graphDataTextSignature = graphTextSignature;
            _graphStatus.Text = _graphPreviewPoints.Count == 0
                ? string.Format(T("editor.graph.no_numeric_points", "No numeric points. Skipped: {0}."), skipped)
                : string.Format(T("editor.graph.points_status", "Points: {0}. Skipped: {1}. Range: {2:0.####} to {3:0.####}."), _graphPreviewPoints.Count, skipped, min, max);
            ResetGraphPreviewView(invalidate: false);
            _graphCanvas.Invalidate();
            _graph3DCanvas?.Invalidate();
        }
        catch (Exception ex)
        {
            _graphStatus.Text = string.Format(T("editor.graph.error", "Graph error: {0}"), ex.Message);
            _graphCanvas.Invalidate();
            _graph3DCanvas?.Invalidate();
        }
    }

    // Vult de tabel en bewaart tegelijk een tab-gescheiden kopieertekst.
    private void FillGraphPointTable(IEnumerable<PointF> points)
    {
        var clipboardLines = new List<string>();

        _graphPointTable.SuspendLayout();
        _graphPointTable.Rows.Clear();
        foreach (var point in points)
        {
            var x = FormatGraphDisplayNumber(point.X);
            var y = FormatGraphDisplayNumber(point.Y);
            _graphPointTable.Rows.Add(x, y);
            clipboardLines.Add($"{x}\t{y}");
        }
        _graphPointTable.ClearSelection();
        _graphPointTable.ResumeLayout();
        UpdateGraphPointTableAvailability();

        _graphPointClipboardText = string.Join(Environment.NewLine, clipboardLines);
        FillGraph3DPointTable(points);
    }

    private void FillGraph3DPointTable(IEnumerable<PointF> points)
    {
        if (_graph3DPointTable is null)
            return;

        var clipboardLines = new List<string>();
        var flat2D = IsGraph3DFlat2DView();
        _graph3DPointTable.SuspendLayout();
        _graph3DPointTable.Rows.Clear();
        foreach (var point in points)
        {
            var x = FormatGraphDisplayNumber(point.X);
            var y = FormatGraphDisplayNumber(point.Y);
            var z = FormatGraphDisplayNumber(0d);
            _graph3DPointTable.Rows.Add(x, y, z);
            clipboardLines.Add(flat2D ? $"{x}\t{y}" : $"{x}\t{y}\t{z}");
        }
        _graph3DPointTable.ClearSelection();
        _graph3DPointTable.ResumeLayout();
        _graph3DPointClipboardText = string.Join(Environment.NewLine, clipboardLines);
        _graph3DPointerText = "";
        _graph3DPointTitleBar?.Invalidate();
        UpdateGraph3DPointTableAvailability();
    }

    private static bool TryGetGraphNumber(NodResult result, out double value)
    {
        if (result.NumericValue.HasValue)
        {
            value = (double)result.NumericValue.Value;
            return double.IsFinite(value);
        }

        var text = result.Text.Trim();
        text = Regex.Replace(text, @"[^\d,\.\-\+Ee]", "");

        if (double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return double.IsFinite(value);

        value = 0;
        return false;
    }

    private void UpdateGraphPointerStatus(PointF? screenPoint)
    {
        var text = "";
        if (_graphPreviewPoints.Count > 0 && _graphHasView && screenPoint is not null)
        {
            var plot = GetGraphPlotRectangle();
            if (plot.Contains(Point.Round(screenPoint.Value)))
            {
                var point = ScreenToGraphPoint(screenPoint.Value, plot);
                text = $"({FormatGraphStatusNumber(point.X)}, {FormatGraphStatusNumber(point.Y)})";
            }
        }

        if (_graphPointerText == text)
            return;

        _graphPointerText = text;
        if (_graphPointerStatusLabel is not null)
        {
            _graphPointerStatusLabel.Text = text;
            _graphPointerStatusPanel.Width = string.IsNullOrWhiteSpace(text)
                ? 52
                : Math.Clamp(
                    TextRenderer.MeasureText(text, _graphPointerStatusLabel.Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width + _graphPointerStatusLabel.Padding.Horizontal + 8,
                    46,
                    190);
        }

        UpdateGraphPointerStatusVisibility();
        _graphPointTitleBar?.Invalidate();
    }

    private static string FormatGraphStatusNumber(float value)
    {
        return FormatGraphDisplayNumber(value);
    }

    private static string FormatGraphDisplayNumber(double value)
    {
        return GraphSurfaceApi.FormatDisplayNumber(value);
    }

    private bool IsGraphPreviewCompatible(NodDocument document, out string reason)
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

    private void ResetGraphPreviewView(bool invalidate = true)
    {
        if (_graphPreviewDocument is null)
        {
            _graphHasView = false;
            return;
        }

        var view = GraphSurfaceApi.CreateFitViewForCanvas(
            _graphPreviewFitPoints,
            (float)_graphSampleMinX,
            (float)_graphSampleMaxX,
            _graphCanvas,
            GraphNormalHalfYRange);
        SetGraphPreviewView(view);
        _graphMarkerView = view;
        SetGraphPreviewRangeControls(view);
        UpdateGraphPreviewViewportRangeControlsIfNeeded();
        _graphHasView = true;
        ResampleGraphPreviewVisibleView();
        UpdateGraphPointerStatus(null);
        NotifyGraphPreviewSyncStateChanged();

        if (invalidate)
        {
            _graphCanvas.Invalidate();
            _graph3DCanvas?.Invalidate();
        }
    }

    private void ApplyGraphPreviewXRangeFromControls()
    {
        if (_updatingGraphXRangeControls)
            return;

        if (GraphSurfaceApi.GetNumberBoxValue(_graphXMin) >= GraphSurfaceApi.GetNumberBoxValue(_graphXMax))
        {
            _graphStatus.Text = "X min moet kleiner zijn dan X max.";
            return;
        }

        _graphSampleMinX = GraphSurfaceApi.GetNumberBoxValue(_graphXMin);
        _graphSampleMaxX = GraphSurfaceApi.GetNumberBoxValue(_graphXMax);
        SyncGraph3DRangeControlsFromGraphControls();
        GenerateGraphPreview();
    }

    private void ApplyGraphPreviewYRangeFromControls()
    {
        if (_updatingGraphYRangeControls || !_graphHasView)
            return;

        if (GraphSurfaceApi.GetNumberBoxValue(_graphYMin) >= GraphSurfaceApi.GetNumberBoxValue(_graphYMax))
        {
            _graphStatus.Text = "Y min moet kleiner zijn dan Y max.";
            return;
        }

        SetGraphPreviewView(GraphSurfaceApi.CreateViewFromYRangeControls(GetGraphPreviewView(), _graphYMin, _graphYMax, _graphCanvas));
        if (_graphShowRangeLines.Checked)
            _graphMarkerView = GetGraphPreviewView();
        SyncGraph3DRangeControlsFromGraphControls();
        ResampleGraphPreviewVisibleView();
        UpdateGraphPointerStatus(null);
        NotifyGraphPreviewSyncStateChanged();
        _graphCanvas.Invalidate();
    }

    private void ApplyGraph3DXRangeFromControls()
    {
        if (_updatingGraphXRangeControls)
            return;

        if (GraphSurfaceApi.GetNumberBoxValue(_graph3DXMin) >= GraphSurfaceApi.GetNumberBoxValue(_graph3DXMax))
        {
            _graphStatus.Text = "X min moet kleiner zijn dan X max.";
            return;
        }

        _applyingGraph3DRangeControls = true;
        _updatingGraphXRangeControls = true;
        try
        {
            _graphSampleMinX = GraphSurfaceApi.GetNumberBoxValue(_graph3DXMin);
            _graphSampleMaxX = GraphSurfaceApi.GetNumberBoxValue(_graph3DXMax);
            var view = new GraphPlotView(
                GraphSurfaceApi.GetNumberBoxValue(_graph3DXMin),
                GraphSurfaceApi.GetNumberBoxValue(_graph3DXMax),
                GraphSurfaceApi.GetNumberBoxValue(_graph3DYMin),
                GraphSurfaceApi.GetNumberBoxValue(_graph3DYMax));
            SetGraph3DXYView(view);
            if (IsGraph3DFlat2DView())
            {
                GraphSurfaceApi.SetNumberBoxValue(_graphXMin, _graphSampleMinX);
                GraphSurfaceApi.SetNumberBoxValue(_graphXMax, _graphSampleMaxX);
            }
            GenerateGraphPreview();
        }
        finally
        {
            _updatingGraphXRangeControls = false;
            _applyingGraph3DRangeControls = false;
        }
    }

    private void ApplyGraph3DYRangeFromControls()
    {
        if (_updatingGraphYRangeControls)
            return;

        if (GraphSurfaceApi.GetNumberBoxValue(_graph3DYMin) >= GraphSurfaceApi.GetNumberBoxValue(_graph3DYMax))
        {
            _graphStatus.Text = "Y min moet kleiner zijn dan Y max.";
            return;
        }

        _applyingGraph3DRangeControls = true;
        _updatingGraphYRangeControls = true;
        try
        {
            var view = new GraphPlotView(
                GraphSurfaceApi.GetNumberBoxValue(_graph3DXMin),
                GraphSurfaceApi.GetNumberBoxValue(_graph3DXMax),
                GraphSurfaceApi.GetNumberBoxValue(_graph3DYMin),
                GraphSurfaceApi.GetNumberBoxValue(_graph3DYMax));
            SetGraph3DXYView(view);
            if (IsGraph3DFlat2DView())
            {
                GraphSurfaceApi.SetNumberBoxValue(_graphYMin, GraphSurfaceApi.GetNumberBoxValue(_graph3DYMin));
                GraphSurfaceApi.SetNumberBoxValue(_graphYMax, GraphSurfaceApi.GetNumberBoxValue(_graph3DYMax));
            }
            _graph3DCanvas?.Invalidate();
        }
        finally
        {
            _updatingGraphYRangeControls = false;
            _applyingGraph3DRangeControls = false;
        }
    }

    private void ApplyGraph3DStepFromControl()
    {
        if (_updatingGraphStepDisplay)
            return;

        _applyingGraph3DRangeControls = true;
        try
        {
            _graphUserStep = GraphSurfaceApi.GetNumberBoxValue(_graph3DStep);
            SetGraphStepDisplay(_graphUserStep);
            if (!_applyingGraphSyncState)
                GenerateGraphPreview();
        }
        finally
        {
            _applyingGraph3DRangeControls = false;
        }
    }

    private void ApplyGraph3DXYView(GraphPlotView view, bool regenerate)
    {
        _applyingGraph3DRangeControls = true;
        _updatingGraphXRangeControls = true;
        _updatingGraphYRangeControls = true;
        try
        {
            SetGraph3DXYView(view);

            if (regenerate && !_applyingGraphSyncState)
            {
                _graphSampleMinX = view.MinX;
                _graphSampleMaxX = view.MaxX;
                GraphSurfaceApi.SetNumberBoxValue(_graphXMin, view.MinX);
                GraphSurfaceApi.SetNumberBoxValue(_graphXMax, view.MaxX);
                GraphSurfaceApi.SetNumberBoxValue(_graphYMin, view.MinY);
                GraphSurfaceApi.SetNumberBoxValue(_graphYMax, view.MaxY);
                GenerateGraphPreview();
            }
            else
            {
                if (_graph3DShowRangeLines?.Checked == false)
                    SetGraph3DXYRangeControls(view);
                _graph3DCanvas?.Invalidate();
            }
        }
        finally
        {
            _updatingGraphYRangeControls = false;
            _updatingGraphXRangeControls = false;
            _applyingGraph3DRangeControls = false;
        }
    }

    private void ApplyGraphPreviewZRangeFromControls()
    {
        if (_updatingGraphZRangeControls)
            return;

        if (IsGraph3DFlat2DView())
        {
            UpdateGraph3DZRangeMode();
            _graph3DCanvas?.Invalidate();
            return;
        }

        if (GraphSurfaceApi.GetNumberBoxValue(_graphZMin) >= GraphSurfaceApi.GetNumberBoxValue(_graphZMax))
        {
            _graphStatus.Text = "Z min moet kleiner zijn dan Z max.";
            return;
        }

        if (GraphSurfaceApi.GetNumberBoxValue(_graphZStep) <= 0d)
        {
            _graphStatus.Text = "Z step moet groter zijn dan 0.";
            return;
        }

        _graph3DCanvas?.Invalidate();
    }

    private GraphPlotView GetGraphPreviewView()
    {
        return new GraphPlotView(_graphViewMinX, _graphViewMaxX, _graphViewMinY, _graphViewMaxY);
    }

    private void SetGraphPreviewView(GraphPlotView view)
    {
        _graphViewMinX = view.MinX;
        _graphViewMaxX = view.MaxX;
        _graphViewMinY = view.MinY;
        _graphViewMaxY = view.MaxY;
    }

    private GraphPreviewSyncState CreateGraphPreviewSyncState()
    {
        return new GraphPreviewSyncState(GetGraphPreviewView(), _graphMarkerView, (decimal)_graphUserStep, _graphShowRangeLines.Checked);
    }

    private void NotifyGraphPreviewSyncStateChanged()
    {
        if (_applyingGraphSyncState || !_graphHasView || _graphPreviewForm is not { IsDisposed: false })
            return;

        _graphPreviewForm.ApplySyncState(CreateGraphPreviewSyncState());
    }

    private void ApplyGraphPreviewSyncState(GraphPreviewSyncState state)
    {
        if (_applyingGraphSyncState)
            return;

        _applyingGraphSyncState = true;
        try
        {
            SetGraphStepValue(state.Step);
            _graphShowRangeLines.Checked = state.ShowRangeLines;
            _graphMarkerView = state.MarkerView;
            SetGraphPreviewView(CreateGraphAspectViewFromSync(state.View));
            if (_graphShowRangeLines.Checked)
                SetGraphPreviewRangeControls(_graphMarkerView);
            else
                UpdateGraphPreviewViewportRangeControlsIfNeeded();
            _graphHasView = true;
            if (_graphPreviewDocument is not null)
                ResampleGraphPreviewVisibleView();
            UpdateGraphPointerStatus(null);
            _graphCanvas.Invalidate();
        }
        finally
        {
            _applyingGraphSyncState = false;
        }
    }

    private void ApplyGraph3DPreviewSyncState(Graph3DPreviewSyncState state)
    {
        if (_applyingGraph3DPreviewSyncState)
            return;

        _applyingGraph3DPreviewSyncState = true;
        _applyingGraph3DRangeControls = true;
        _updatingGraphXRangeControls = true;
        _updatingGraphYRangeControls = true;
        _updatingGraphZRangeControls = true;
        _updatingGraphZStepDisplay = true;
        try
        {
            _graph3DCamera = state.Camera;
            if (_graph3DRotationDial is not null)
                _graph3DRotationDial.Camera = _graph3DCamera;

            SetGraph3DXYView(new GraphPlotView(state.View.MinX, state.View.MaxX, state.View.MinY, state.View.MaxY));
            GraphSurfaceApi.SetNumberBoxValue(_graph3DXMin, state.View.MinX);
            GraphSurfaceApi.SetNumberBoxValue(_graph3DXMax, state.View.MaxX);
            GraphSurfaceApi.SetNumberBoxValue(_graph3DYMin, state.View.MinY);
            GraphSurfaceApi.SetNumberBoxValue(_graph3DYMax, state.View.MaxY);
            GraphSurfaceApi.SetNumberBoxValue(_graphZMin, state.View.MinZ);
            GraphSurfaceApi.SetNumberBoxValue(_graphZMax, state.View.MaxZ);
            GraphSurfaceApi.SetNumberBoxValue(_graphZStep, state.GridStep);

            _graph3DGridVisible = state.ShowGrid;
            if (_graph3DGridToggle is not null)
                _graph3DGridToggle.Checked = state.ShowGrid;
            if (_graph3DShowRangeLines is not null)
                _graph3DShowRangeLines.Checked = state.ShowLines;

            var syncGraphPreviewWindow = false;
            if (IsGraph3DFlat2DView())
            {
                _applyingGraphSyncState = true;
                try
                {
                    SetGraphStepValue((decimal)state.Step);
                    _graphShowRangeLines.Checked = state.ShowLines;
                    _graphMarkerView = state.MarkerView;
                    SetGraphPreviewView(CreateGraphAspectViewFromSync(new GraphPlotView(
                        state.View.MinX,
                        state.View.MaxX,
                        state.View.MinY,
                        state.View.MaxY)));
                    if (_graphShowRangeLines.Checked)
                        SetGraphPreviewRangeControls(_graphMarkerView);
                    else
                        UpdateGraphPreviewViewportRangeControlsIfNeeded();
                    ResampleGraphPreviewVisibleView();
                    _graphCanvas?.Invalidate();
                    syncGraphPreviewWindow = true;
                }
                finally
                {
                    _applyingGraphSyncState = false;
                }
            }
            if (syncGraphPreviewWindow)
                NotifyGraphPreviewSyncStateChanged();

            UpdateGraph3DModeButtons();
            UpdateGraph3DRangeInputMode();
            _graph3DCanvas?.Invalidate();
        }
        finally
        {
            _updatingGraphZStepDisplay = false;
            _updatingGraphZRangeControls = false;
            _updatingGraphYRangeControls = false;
            _updatingGraphXRangeControls = false;
            _applyingGraph3DRangeControls = false;
            _applyingGraph3DPreviewSyncState = false;
        }
    }

    private GraphPlotView CreateGraphAspectViewFromSync(GraphPlotView sourceView)
    {
        return GraphSurfaceApi.MatchViewToCanvasAspect(sourceView, _graphCanvas);
    }

    private void SetGraphStepValue(decimal value)
    {
        var clamped = Math.Clamp(value, _graphStep.Minimum, _graphStep.Maximum);
        _graphUserStep = (double)clamped;
        SetGraphStepDisplay(_graphUserStep);
    }

    private void SetGraphPreviewRangeControls(GraphPlotView view)
    {
        _updatingGraphXRangeControls = true;
        _updatingGraphYRangeControls = true;
        _updatingGraphZRangeControls = true;
        try
        {
            GraphSurfaceApi.SetRangeControlValues(new GraphRangeControls(_graphXMin, _graphXMax, _graphYMin, _graphYMax), view);
            if (_graphZMin is not null && _graphZMax is not null)
            {
                GraphSurfaceApi.SetNumberBoxValue(_graphZMin, -Graph3DDefaultHalfRange);
                GraphSurfaceApi.SetNumberBoxValue(_graphZMax, Graph3DDefaultHalfRange);
            }
            SyncGraph3DRangeControlsFromGraphControls();
        }
        finally
        {
            _updatingGraphZRangeControls = false;
            _updatingGraphYRangeControls = false;
            _updatingGraphXRangeControls = false;
        }
    }

    private void UpdateGraphPreviewViewportRangeControlsIfNeeded()
    {
        _updatingGraphXRangeControls = true;
        _updatingGraphYRangeControls = true;
        _updatingGraphZRangeControls = true;
        try
        {
            GraphSurfaceApi.SyncViewportRangeControls(
                new GraphRangeControls(_graphXMin, _graphXMax, _graphYMin, _graphYMax),
                GetGraphPreviewView(),
                _graphShowRangeLines.Checked);
            SyncGraph3DRangeControlsFromGraphControls();
        }
        finally
        {
            _updatingGraphZRangeControls = false;
            _updatingGraphYRangeControls = false;
            _updatingGraphXRangeControls = false;
        }
    }

    private void ApplyGraphRangeLineVisibility()
    {
        if (_graphShowRangeLines.Checked)
        {
            SetGraphPreviewRangeControls(_graphMarkerView);
            UpdateGraphRangeInputMode();
            return;
        }

        UpdateGraphPreviewViewportRangeControlsIfNeeded();
        UpdateGraphRangeInputMode();
    }

    private void UpdateGraphRangeInputMode()
    {
        var editable = _graphShowRangeLines.Checked;
        foreach (var box in new[] { _graphXMin, _graphXMax, _graphYMin, _graphYMax, _graphStep })
        {
            if (box is null)
                continue;

            box.Enabled = editable;
            box.ReadOnly = !editable;
        }
        UpdateGraph3DRangeInputMode();
    }

    private void ZoomGraphPreview(float factor)
    {
        if (!EnsureGraphPreviewGeneratedForInteraction())
            return;

        ZoomGraphPreviewAt(_graphCanvas, factor, new PointF(_graphCanvas.ClientSize.Width / 2f, _graphCanvas.ClientSize.Height / 2f));
    }

    private void GraphCanvas_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (EnsureGraphPreviewGeneratedForInteraction())
            ZoomGraphPreviewAt(_graphCanvas, e.Delta > 0 ? 0.85f : 1.18f, e.Location);
    }

    private bool EnsureGraphPreviewGeneratedForInteraction()
    {
        if (_graphHasView && _graphPreviewDocument is not null)
            return true;

        if (_graphHasView)
        {
            var editor = CurrentEditor;
            if (editor is null)
                return false;

            try
            {
                _graphPreviewDocument = NodParser.Parse(editor.Text);
                if (!IsGraphPreviewCompatible(_graphPreviewDocument, out var disabledReason))
                {
                    _graphDisabledMessage = disabledReason;
                    _graphPreviewDocument = null;
                    _graphHasView = false;
                    _graphCanvas.Invalidate();
                    return false;
                }

                ResampleGraphPreviewVisibleView();
                _graphCanvas.Invalidate();
                return true;
            }
            catch
            {
                _graphPreviewDocument = null;
                return false;
            }
        }

        GenerateGraphPreview();
        return _graphHasView && _graphPreviewDocument is not null;
    }

    private void ZoomGraphPreviewAt(Control canvas, float factor, PointF screenPoint)
    {
        var plot = GraphSurfaceApi.GetPlotRectangle(canvas);
        if (plot.Width <= 0 || plot.Height <= 0)
            return;

        var anchor = GraphSurfaceApi.ScreenToGraph(screenPoint, plot, GetGraphPreviewView());
        var newWidth = (_graphViewMaxX - _graphViewMinX) * factor;
        var newHeight = (_graphViewMaxY - _graphViewMinY) * factor;
        if (newWidth < GraphSurfaceApi.MinimumViewSpan || newHeight < GraphSurfaceApi.MinimumViewSpan)
            return;

        var xRatio = (anchor.X - _graphViewMinX) / (_graphViewMaxX - _graphViewMinX);
        var yRatio = (anchor.Y - _graphViewMinY) / (_graphViewMaxY - _graphViewMinY);
        _graphViewMinX = anchor.X - newWidth * xRatio;
        _graphViewMaxX = _graphViewMinX + newWidth;
        _graphViewMinY = anchor.Y - newHeight * yRatio;
        _graphViewMaxY = _graphViewMinY + newHeight;
        MatchGraphPreviewViewToCanvasAspect();
        UpdateGraphPreviewViewportRangeControlsIfNeeded();
        ResampleGraphPreviewVisibleView();
        NotifyGraphPreviewSyncStateChanged();
        _graphCanvas.Invalidate();
        _graph3DCanvas?.Invalidate();
    }

    private void GraphCanvas_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || !EnsureGraphPreviewGeneratedForInteraction())
            return;

        _graphPanning = true;
        _graphPanStart = e.Location;
        _graphPanStartMinX = _graphViewMinX;
        _graphPanStartMaxX = _graphViewMaxX;
        _graphPanStartMinY = _graphViewMinY;
        _graphPanStartMaxY = _graphViewMaxY;
        _graphCanvas.Cursor = GraphCursors.Pan;
    }

    private void GraphCanvas_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_graphPanning)
        {
            if (_graphHasView)
                _graphCanvas.Cursor = GraphCursors.Pan;
            UpdateGraphPointerStatus(e.Location);
            return;
        }

        var plot = GetGraphPlotRectangle();
        if (plot.Width <= 0 || plot.Height <= 0)
            return;

        var dx = e.X - _graphPanStart.X;
        var dy = e.Y - _graphPanStart.Y;
        var graphDx = dx / (double)plot.Width * (_graphPanStartMaxX - _graphPanStartMinX);
        var graphDy = dy / (double)plot.Height * (_graphPanStartMaxY - _graphPanStartMinY);

        _graphViewMinX = _graphPanStartMinX - graphDx;
        _graphViewMaxX = _graphPanStartMaxX - graphDx;
        _graphViewMinY = _graphPanStartMinY + graphDy;
        _graphViewMaxY = _graphPanStartMaxY + graphDy;
        MatchGraphPreviewViewToCanvasAspect();
        UpdateGraphPreviewViewportRangeControlsIfNeeded();
        ResampleGraphPreviewVisibleView();
        UpdateGraphPointerStatus(e.Location);
        NotifyGraphPreviewSyncStateChanged();
        _graphCanvas.Invalidate();
    }

    private void GraphCanvas_MouseUp(object? sender, MouseEventArgs e)
    {
        _graphPanning = false;
        _graphCanvas.Cursor = _graphCanvas.ClientRectangle.Contains(e.Location) && _graphHasView ? GraphCursors.Pan : GraphCursors.Default;
    }

    private void MatchGraphPreviewViewToCanvasAspect()
    {
        SetGraphPreviewView(GraphSurfaceApi.MatchViewToCanvasAspect(GetGraphPreviewView(), _graphCanvas));
    }

    private void ResampleGraphPreviewVisibleView()
    {
        if (_graphPreviewDocument is null || !_graphHasView)
            return;

        if (CurrentEditor?.Text != _graphDataTextSignature)
        {
            InvalidateGraphPreviewData();
            return;
        }

        var visibleMin = Math.Min(_graphViewMinX, _graphViewMaxX);
        var visibleMax = Math.Max(_graphViewMinX, _graphViewMaxX);
        var width = visibleMax - visibleMin;
        if (width <= 0)
            return;

        var desiredSamples = Math.Clamp(_graphCanvas.ClientSize.Width / 6, 80, GraphPreviewMaxLineSamplePoints);
        var step = width / desiredSamples;
        var linePoints = new List<PointF>();
        var stepPoints = new List<PointF>();

        for (var i = 0; i <= desiredSamples; i++)
        {
            var x = visibleMin + step * i;
            var input = FormatGraphPreviewInput(x);

            try
            {
                var result = NodEngine.ConvertForward(_graphPreviewDocument, input);
                if (TryGetGraphNumber(result, out var y))
                    linePoints.Add(new PointF((float)x, (float)y));
            }
            catch
            {
                // Niet-numerieke punten horen niet in de grafiek.
            }
        }

        var requestedStep = _graphUserStep;
        if (requestedStep > 0)
        {
            var stepMin = visibleMin;
            var stepMax = visibleMax;
            var displayStep = ChooseGraphDisplayedStep(requestedStep, stepMin, stepMax, GraphPreviewMaxVisibleStepPoints);
            SetGraphStepDisplay(displayStep);
            var firstStep = Math.Ceiling(stepMin / displayStep) * displayStep;
            for (var i = 0; i < GraphPreviewMaxVisibleStepPoints; i++)
            {
                var x = firstStep + displayStep * i;
                if (!double.IsFinite(x) || x > stepMax + displayStep / 1000.0)
                    break;

                var input = FormatGraphPreviewInput(x);
                try
                {
                    var result = NodEngine.ConvertForward(_graphPreviewDocument, input);
                    if (TryGetGraphNumber(result, out var y))
                    {
                        var point = new PointF((float)x, (float)y);
                        if (IsGraphPreviewPointInsideCurrentView(point))
                            stepPoints.Add(point);
                    }
                }
                catch
                {
                    // Niet-numerieke punten horen niet in de tabel.
                }
            }
        }

        _graphPreviewPoints.Clear();
        _graphPreviewPoints.AddRange(linePoints);
        _graphPreviewStepPoints.Clear();
        _graphPreviewStepPoints.AddRange(stepPoints);
        FillGraphPointTable(stepPoints);
        _graph3DCanvas?.Invalidate();
        UpdateGraph3DPreviewWindowData();
    }

    private bool IsGraphPreviewPointInsideCurrentView(PointF point)
    {
        return float.IsFinite(point.X) &&
               float.IsFinite(point.Y) &&
               point.X >= _graphViewMinX &&
               point.X <= _graphViewMaxX &&
               point.Y >= _graphViewMinY &&
               point.Y <= _graphViewMaxY;
    }

    private static double ChooseGraphDisplayedStep(double requestedStep, double min, double max, int maxPoints)
    {
        if (!double.IsFinite(requestedStep) || requestedStep <= 0 || !double.IsFinite(min) || !double.IsFinite(max) || max <= min)
            return Math.Max(1.0, requestedStep);

        var targetRows = Math.Max(8, maxPoints / 2);
        var rawStep = (max - min) / targetRows;
        if (!double.IsFinite(rawStep) || rawStep <= 0)
            return requestedStep;

        return NiceGraphStep(rawStep);
    }

    private static double NiceGraphStep(double value)
    {
        if (!double.IsFinite(value) || value <= 0d)
            return 1d;

        var exponent = Math.Floor(Math.Log10(value));
        var baseValue = Math.Pow(10d, exponent);
        var fraction = value / baseValue;
        var niceFraction = ChooseGraphStepMantissa(fraction);
        return niceFraction * baseValue;
    }

    private static double ChooseGraphStepMantissa(double fraction)
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

    private void SetGraphStepDisplay(double value)
    {
        _updatingGraphStepDisplay = true;
        try
        {
            GraphSurfaceApi.SetNumberBoxValue(_graphStep, value);
            if (_graph3DStep is not null)
                GraphSurfaceApi.SetNumberBoxValue(_graph3DStep, value);
        }
        finally
        {
            _updatingGraphStepDisplay = false;
        }
    }

    private void SyncGraph3DRangeControlsFromGraphControls()
    {
        if (_applyingGraph3DRangeControls)
            return;

        if (_graph3DXMin is null || _graph3DXMax is null || _graph3DYMin is null || _graph3DYMax is null)
            return;

        if (_graph3DXMin.Focused || _graph3DXMax.Focused || _graph3DYMin.Focused || _graph3DYMax.Focused || _graph3DStep?.Focused == true)
            return;

        _updatingGraphStepDisplay = true;
        try
        {
            if (IsGraph3DFlat2DView())
                SyncGraph3DFlatViewFromGraph2D();
            if (_graph3DStep is not null)
                GraphSurfaceApi.SetNumberBoxValue(_graph3DStep, _graphUserStep);
        }
        finally
        {
            _updatingGraphStepDisplay = false;
        }
    }

    private static string FormatGraphPreviewInput(double value)
    {
        var abs = Math.Abs(value);
        return abs is > 0 and < 1e-12 || abs >= 1e12
            ? value.ToString("0.############E+0", CultureInfo.InvariantCulture)
            : value.ToString("0.############", CultureInfo.InvariantCulture);
    }

    private Rectangle GetGraphPlotRectangle()
    {
        return GraphSurfaceApi.GetPlotRectangle(_graphCanvas);
    }

    private PointF ScreenToGraphPoint(PointF screenPoint, Rectangle plot)
    {
        return GraphSurfaceApi.ScreenToGraph(screenPoint, plot, new GraphPlotView(_graphViewMinX, _graphViewMaxX, _graphViewMinY, _graphViewMaxY));
    }

    private void GraphCanvas_Paint(object? sender, PaintEventArgs e)
    {
        if (!_graphHasView && _graphPreviewDocument is not null)
            ResetGraphPreviewView(invalidate: false);

        GraphSurfaceApi.Draw(
            e.Graphics,
            _graphCanvas,
            _graphPreviewPoints,
            _graphPreviewStepPoints,
            new GraphPlotView(_graphViewMinX, _graphViewMaxX, _graphViewMinY, _graphViewMaxY),
            _graphShowRangeLines.Checked ? _graphMarkerView.MinX : GraphSurfaceApi.GetNumberBoxValue(_graphXMin),
            _graphShowRangeLines.Checked ? _graphMarkerView.MaxX : GraphSurfaceApi.GetNumberBoxValue(_graphXMax),
            GraphSurfaceApi.GetNumberBoxValue(_graphStep),
            _graphDisabledMessage,
            "Generate graph",
            GraphPlotDensity.Compact,
            (float)GraphSurfaceApi.GetNumberBoxValue(_graphYMin),
            (float)GraphSurfaceApi.GetNumberBoxValue(_graphYMax),
            _graphShowRangeLines.Checked);
    }

    // Wordt aangeroepen zodra het venster zichtbaar is; zet paneelgroottes en formuleweergave goed.
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        AdjustMainSplitter();
        AdjustPreviewSplitter();
        RenderPendingMath();
    }

    // Rendert de laatst opgebouwde formule en berekening in de MathML-previewvelden.
    private void RenderPendingMath()
    {
        if (_testFormulaView is not null)
            RenderMath(_testFormulaView, string.IsNullOrWhiteSpace(_pendingFormulaMathMarkup) ? PlainMathMarkup("") : _pendingFormulaMathMarkup);

        if (_testCalculationView is not null)
            RenderMath(_testCalculationView, string.IsNullOrWhiteSpace(_pendingCalculationMathMarkup) ? PlainMathMarkup("") : _pendingCalculationMathMarkup);
    }

    private void ApplySolverPanelVisibility(bool solverActive)
    {
        if (_testGroup is null || _simulatorPreviewGroup is null)
            return;

        _solverPreviewActive = solverActive;

        if (solverActive)
        {
            if (!_panelsHiddenForSolver)
            {
                _testPanelWasVisibleBeforeSolver = _floatingTestForm is not null || _testGroup.Visible;
                _simulatorPanelWasVisibleBeforeSolver = _floatingSimulatorForm is not null || _simulatorPreviewGroup.Visible;
            }

            _panelsHiddenForSolver = true;
            DockTestPanel(showPanel: false);
            DockSimulatorPreview(showPanel: false);
            UpdateViewMenuChecks();
            return;
        }

        if (_panelsHiddenForSolver)
        {
            _panelsHiddenForSolver = false;
            DockTestPanel(_testPanelWasVisibleBeforeSolver);
            DockSimulatorPreview(_simulatorPanelWasVisibleBeforeSolver);
        }

        UpdateViewMenuChecks();
    }

    // Houdt de hoofd-splitter op een bruikbare hoogte voor editor en onderste panelen.
    private void AdjustMainSplitter()
    {
        if (_mainSplit.Height <= 0 || _mainSplit.Panel1Collapsed || _mainSplit.Panel2Collapsed)
            return;

        const int desiredPanel1Min = 280;
        const int desiredPanel2Height = 330;

        var available = _mainSplit.Height - _mainSplit.SplitterWidth;
        if (available < desiredPanel1Min + desiredPanel2Height)
            return;

        var splitterDistance = available - desiredPanel2Height;
        var maxValid = _mainSplit.Height - _mainSplit.Panel2MinSize;
        if (splitterDistance >= _mainSplit.Panel1MinSize && splitterDistance <= maxValid)
            _mainSplit.SplitterDistance = splitterDistance;
    }

    private bool TrySetPreviewSplitterDistance(int distance)
    {
        if (_previewSplit is null || _previewSplit.Width <= 0 || _previewSplit.IsDisposed)
            return false;

        var min = Math.Max(1, _previewSplit.Panel1MinSize);
        var max = _previewSplit.Width - Math.Max(1, _previewSplit.Panel2MinSize);
        if (max < min)
            return false;

        var clamped = Math.Clamp(distance, min, max);
        try
        {
            if (_previewSplit.SplitterDistance != clamped)
                _previewSplit.SplitterDistance = clamped;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    // Houdt de preview-splitter netjes verdeeld tussen live preview en simulator.
    private void AdjustPreviewSplitter()
    {
        if (_previewSplit.Width <= 0)
            return;

        var available = _previewSplit.Width - _previewSplit.SplitterWidth;
        const int desiredPanel1Min = 160;
        if (available < desiredPanel1Min + DockedSimulatorWidth)
            return;

        var min = _previewSplit.Panel1MinSize;
        var maxValid = _previewSplit.Width - _previewSplit.Panel2MinSize;
        if (maxValid < min)
            return;

        var splitterDistance = Math.Clamp(available - DockedSimulatorWidth, min, maxValid);
        if (TrySetPreviewSplitterDistance(splitterDistance))
            AdjustLivePreviewWidth();
    }

    // Beperkt de breedte van de live preview zodat deze niet te breed wordt.
    private void AdjustLivePreviewWidth()
    {
        if (_metadataPreviewGroup is null || _previewSplit is null)
            return;

        _metadataPreviewGroup.Width = Math.Min(MaxDockedLivePreviewWidth, _previewSplit.Panel1.ClientSize.Width);
        _metadataPreviewGroup.Height = _previewIntro.Visible ? _previewSplit.Panel1.ClientSize.Height : 168;
    }


    // Tekent de tabbladen handmatig, inclusief het sluitkruisje.
    private void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _tabControl.TabPages.Count)
            return;

        var page = _tabControl.TabPages[e.Index];
        var rect = _tabControl.GetTabRect(e.Index);
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

        using var backBrush = new SolidBrush(selected ? Color.White : Color.FromArgb(240, 240, 240));
        using var borderPen = new Pen(Color.FromArgb(190, 190, 190));
        e.Graphics.FillRectangle(backBrush, rect);
        e.Graphics.DrawRectangle(borderPen, rect);

        var closeRect = GetTabCloseRect(e.Index);
        var textRect = new Rectangle(rect.X + 8, rect.Y + 4, Math.Max(10, rect.Width - 26), rect.Height - 8);

        TextRenderer.DrawText(
            e.Graphics,
            page.Text,
            Font,
            textRect,
            Color.Black,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        TextRenderer.DrawText(
            e.Graphics,
            "×",
            Font,
            closeRect,
            Color.DimGray,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    // Berekent het klikgebied van het sluitkruisje op een tab.
    private Rectangle GetTabCloseRect(int index)
    {
        var rect = _tabControl.GetTabRect(index);
        return new Rectangle(rect.Right - 18, rect.Top + 4, 14, Math.Max(12, rect.Height - 8));
    }

    // Sluit een tab wanneer de gebruiker op het sluitkruisje klikt.
    private void TabControl_MouseDown(object? sender, MouseEventArgs e)
    {
        for (var i = 0; i < _tabControl.TabPages.Count; i++)
        {
            if (GetTabCloseRect(i).Contains(e.Location))
            {
                if (_tabs.TryGetValue(_tabControl.TabPages[i], out var tab))
                {
                    CloseTab(tab);
                    return;
                }
            }
        }
    }

    // Maakt een HTML/WebView2-previewcontrol voor formule en uitwerking.
    private ToolEditorTabHeaderParts CreateEditorTabHeader(TabPage page)
    {
        return ToolEditorTabsApi.CreateHeader(
            page,
            (_, _) => SelectEditorTab(page),
            (_, _) =>
            {
                if (_tabs.TryGetValue(page, out var closeTab))
                    CloseTab(closeTab);
            });
    }

    private void SelectEditorTab(TabPage page)
    {
        if (!_tabs.TryGetValue(page, out var tab))
            return;

        _selectedEditorPage = page;
        _editorContentPanel.SuspendLayout();
        _editorContentPanel.Controls.Clear();
        tab.Content.Dock = DockStyle.Fill;
        _editorContentPanel.Controls.Add(tab.Content);
        _editorContentPanel.ResumeLayout();
        RefreshEditorTabStrip();
        UpdateUiState();
        UpdatePreviewFromCurrentText();
    }

    private void RefreshEditorTabStrip()
    {
        foreach (var tab in _tabs.Values)
        {
            var selected = _selectedEditorPage == tab.Page;
            ToolEditorTabsApi.SetHeaderState(tab.HeaderPanel, tab.HeaderTitle, tab.Page.Text, selected, tab.Dirty, Font);
        }

        _editorTabStrip.Invalidate();
    }

    private static HtmlMathPreviewControl CreateMathView()
    {
        return new HtmlMathPreviewControl
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(239, 246, 255),
            Margin = new Padding(0),
        };
    }

    // Geeft de formule-preview dezelfde rustige kaartstijl als de help, maar compact genoeg voor het testpaneel.
    private static Panel CreateMathPreviewCard(out HtmlMathPreviewControl mathView)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 6, 10, 6),
            Margin = new Padding(0, 4, 0, 4),
            BackColor = Color.FromArgb(239, 246, 255),
        };
        card.Paint += MathPreviewCard_Paint;

        mathView = CreateMathView();
        card.Controls.Add(mathView);
        return card;
    }

    // Tekent de zachte rand van de formulekaart.
    private static void MathPreviewCard_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel panel)
            return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
        using var path = CreateRoundedRectanglePath(rect, 8);
        var borderColor = panel.BackColor.GetBrightness() < 0.4f
            ? Color.FromArgb(51, 65, 85)
            : Color.FromArgb(199, 219, 248);
        using var fill = new SolidBrush(panel.BackColor);
        using var border = new Pen(borderColor, 1);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
    }

    // Stuurt MathML-markup naar de eigen formule-renderer.
    private static void RenderMath(HtmlMathPreviewControl view, string mathMarkup)
    {
        // WebView2 gebruikt dezelfde HTML/MathML-laag als de NOD-help.
        // NOD blijft de bron; MathML is alleen de weergave.
        view.MathMarkup = mathMarkup;
    }


    // Bouwt de live preview met naam, inputs, output, format en introductietekst.
    private GroupBox BuildPreviewBox()
    {
        var group = new GroupBox
        {
            Text = T("editor.preview.title", "Live preview") + "  \u00D7",
            Dock = DockStyle.Fill
        };

        group.DoubleClick += (_, _) => UndockLivePreview();

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 8,
            ColumnCount = 2,
            Padding = new Padding(6, 7, 6, 5)
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        for (var i = 0; i < 6; i++)
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
        _previewIntroLabelRow = new RowStyle(SizeType.Absolute, 18);
        _previewIntroRow = new RowStyle(SizeType.Percent, 100);
        panel.RowStyles.Add(_previewIntroLabelRow);
        panel.RowStyles.Add(_previewIntroRow);

        panel.Controls.Add(new Label { Text = "Dialoog", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        _previewDialogName = CreatePreviewValueLabel();
        panel.Controls.Add(_previewDialogName, 1, 0);

        panel.Controls.Add(new Label { Text = "Input1", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        _previewInputName = CreatePreviewValueLabel();
        panel.Controls.Add(_previewInputName, 1, 1);

        panel.Controls.Add(new Label { Text = "Input2", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        _previewOutputName = CreatePreviewValueLabel();
        panel.Controls.Add(_previewOutputName, 1, 2);

        panel.Controls.Add(new Label { Text = "Voorbeeld", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
        _previewInputSample = CreatePreviewValueLabel();
        panel.Controls.Add(_previewInputSample, 1, 3);

        panel.Controls.Add(new Label { Text = "Resultaat", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 4);
        _previewOutputSample = CreatePreviewValueLabel();
        panel.Controls.Add(_previewOutputSample, 1, 4);

        panel.Controls.Add(new Label { Text = "Format", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 5);
        _previewFormat = CreatePreviewValueLabel();
        panel.Controls.Add(_previewFormat, 1, 5);

        _previewIntroLabel = new Label { Text = "Introductie", AutoSize = true, Anchor = AnchorStyles.Left };
        panel.Controls.Add(_previewIntroLabel, 0, 6);
        panel.SetColumnSpan(_previewIntroLabel, 2);

        _previewIntro = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            Text = "-"
        };
        panel.Controls.Add(_previewIntro, 0, 7);
        panel.SetColumnSpan(_previewIntro, 2);

        group.Controls.Add(panel);
        return group;
    }

    // Maakt previewwaarden leesbaar binnen smalle docked panelen.
    private static Label CreatePreviewValueLabel()
    {
        return new Label
        {
            Text = "-",
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
    }

    // Verbergt het introductievak wanneer er geen introductie is; anders gebruikt het de resterende hoogte.
    private void SetPreviewIntro(IReadOnlyList<string> introLines)
    {
        var hasIntro = introLines.Count > 0;
        _previewIntroLabel.Visible = hasIntro;
        _previewIntro.Visible = hasIntro;
        _previewIntroLabelRow.SizeType = SizeType.Absolute;
        _previewIntroLabelRow.Height = hasIntro ? 18 : 0;
        _previewIntroRow.SizeType = hasIntro ? SizeType.Percent : SizeType.Absolute;
        _previewIntroRow.Height = hasIntro ? 100 : 0;
        _previewIntro.Text = hasIntro ? string.Join(Environment.NewLine, introLines) : "";
        if (_metadataPreviewGroup is not null && _previewSplit is not null)
            _metadataPreviewGroup.Height = hasIntro ? _previewSplit.Panel1.ClientSize.Height : 168;
    }

    private void SchedulePreviewUpdate(bool updateGraph)
    {
        _pendingPreviewGraphUpdate |= updateGraph;

        if (IsPowerResumeQuietPeriod())
        {
            _powerResumePreviewPending = true;
            _powerResumeGraphPending |= updateGraph && ShouldGenerateGraphPreview();
            _previewUpdateTimer.Stop();
            _powerResumeTimer.Stop();
            _powerResumeTimer.Start();
            return;
        }

        _previewUpdateTimer.Stop();
        _previewUpdateTimer.Start();
    }

    private void ScheduleGraphPreviewUpdate()
    {
        if (IsPowerResumeQuietPeriod())
        {
            _graphUpdateTimer.Stop();
            _powerResumeGraphPending |= ShouldGenerateGraphPreview();
            _powerResumeTimer.Stop();
            _powerResumeTimer.Start();
            return;
        }

        if (!ShouldGenerateGraphPreview())
        {
            _graphUpdateTimer.Stop();
            return;
        }

        _graphUpdateTimer.Stop();
        _graphUpdateTimer.Start();
    }

    private bool ShouldGenerateGraphPreview()
    {
        var floatingGraphVisible = _graphPreviewForm is { IsDisposed: false, Visible: true };
        var floatingGraph3DVisible = _graph3DPreviewForm is { IsDisposed: false, Visible: true };
        var miniGraphVisible = _testTabs is not null
            && !_testTabs.IsDisposed
            && (_testTabs.SelectedTab == _graphPreviewPage || _testTabs.SelectedTab == _graph3DPage)
            && _graphPreviewPage is { IsDisposed: false }
            && _testGroup is { Visible: true };

        return floatingGraphVisible || floatingGraph3DVisible || miniGraphVisible;
    }

    // Leest de huidige editorinhoud en werkt live preview en simulator bij.
    private void UpdatePreviewFromCurrentText(bool updateGraph = true)
    {
        if (IsPowerResumeQuietPeriod())
        {
            _powerResumePreviewPending = true;
            _powerResumeGraphPending |= updateGraph && ShouldGenerateGraphPreview();
            _previewUpdateTimer.Stop();
            _powerResumeTimer.Stop();
            _powerResumeTimer.Start();
            return;
        }

        if (_previewDialogName is null)
            return;

        var editor = CurrentEditor;
        if (editor is null)
            return;

        try
        {
            var normalized = NodTextNormalizer.Normalize(editor.Text, repairConcatenated: true);
            var meta = NodUiMetadata.Parse(normalized);

            var dialogName = string.IsNullOrWhiteSpace(meta.Urln) ? meta.Name : meta.Urln;
            var input1 = string.IsNullOrWhiteSpace(meta.Input1) ? "Input" : meta.Input1;
            var input2 = string.IsNullOrWhiteSpace(meta.Input2) ? "Output" : meta.Input2;

            _previewDialogName.Text = dialogName;
            _previewInputName.Text = input1;
            _previewOutputName.Text = input2;
            _previewFormat.Text = string.IsNullOrWhiteSpace(meta.Format) ? "-" : meta.Format;
            SetPreviewIntro(meta.IntroLines);

            var sampleInput = string.IsNullOrWhiteSpace(_testInput.Text) ? "22" : _testInput.Text;
            _previewInputSample.Text = FormatSimValue(meta.SymbolBeforeInput, sampleInput, meta.SymbolAfterInput);

            try
            {
                var doc = NodParser.Parse(normalized);
                _pendingFormulaMathMarkup = BuildFormulaMathMarkup(doc, meta);

                var isSolverDocument = IsSolverDocument(normalized, doc);
                ApplySolverPanelVisibility(isSolverDocument);

                if (isSolverDocument)
                {
                    var report = SolverStepBuilder.Build(doc, sampleInput);
                    var solverOutputText = FormatResultTextForCurrentCulture(report.ResultText);
                    _previewOutputSample.Text = solverOutputText;
                    _testOutput.Text = solverOutputText;
                    _pendingCalculationMathMarkup = PlainMathMarkup(T("editor.solver_steps.preview_hint", "Gebruik Solver stappen (F7) voor de echte grafiek en stap-voor-stap uitleg."));
                    RenderPendingMath();
                    UpdateSimulatorPreview(meta, T("editor.solver_steps.short", "Solver"), T("editor.solver_steps.open_hint", "Open Solver stappen (F7)"));
                    _testButton.Text = T("editor.toolbar.solver_steps", "Solver");
                    return;
                }

                var result = NodEngine.ConvertForward(doc, sampleInput);
                var outputText = FormatResultTextForCurrentCulture(result.Text);
                _previewOutputSample.Text = FormatSimValue(meta.SymbolBeforeOutput, outputText, meta.SymbolAfterOutput);
                _pendingCalculationMathMarkup = BuildCalculationMathMarkup(doc, sampleInput, outputText);
                RenderPendingMath();
                UpdateSimulatorPreview(meta, sampleInput, outputText);
                _testButton.Text = T("editor.toolbar.test", "Test");
            }
            catch
            {
                ApplySolverPanelVisibility(false);
                _previewOutputSample.Text = "-";
                _pendingFormulaMathMarkup = PlainMathMarkup("");
                _pendingCalculationMathMarkup = PlainMathMarkup("");
                RenderPendingMath();
                UpdateSimulatorPreview(meta, sampleInput, "-");
                _testButton.Text = T("editor.toolbar.test", "Test");
            }
        }
        catch
        {
            _previewDialogName.Text = "-";
            _previewInputName.Text = "-";
            _previewOutputName.Text = "-";
            _previewInputSample.Text = "-";
            _previewOutputSample.Text = "-";
            _previewFormat.Text = "-";
            SetPreviewIntro(Array.Empty<string>());
            ApplySolverPanelVisibility(false);
            _pendingFormulaMathMarkup = PlainMathMarkup("");
            _pendingCalculationMathMarkup = PlainMathMarkup("");
            RenderPendingMath();
            UpdateSimulatorPreview(new NodUiMetadata(), "22", "-");
            _testButton.Text = T("editor.toolbar.test", "Test");
        }

        if (updateGraph)
            ScheduleGraphPreviewUpdate();
    }


    // Bouwt de converter-simulator die lijkt op de oude converter-interface.
    private GroupBox BuildSimulatorPreviewBox()
    {
        var group = new GroupBox
        {
            Text = T("editor.sim.title", "Converter simulator") + "  \u00D7",
            Dock = DockStyle.Fill
        };

        group.DoubleClick += (_, _) => UndockSimulatorPreview();
        AttachSimulatorDrag(group);

        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 1,
            Padding = new Padding(6)
        };

        _simWindow = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SoftPanelBackColor,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(0)
        };

        var titlePanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 26,
            BackColor = PanelBackColor
        };

        _simTitle = new Label
        {
            Text = T("editor.sim.window_title", "Converter preview"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = EditorTextColor
        };

        titlePanel.Controls.Add(_simTitle);
        AttachSimulatorDrag(titlePanel);
        AttachSimulatorDrag(_simTitle);

        _simMenuStrip = new MenuStrip
        {
            Dock = DockStyle.Top,
            BackColor = PanelBackColor,
            ForeColor = EditorTextColor,
            Height = 24,
            CanOverflow = false
        };
        _simMenuStrip.Items.Add(T("editor.menu.file", "File"));
        _simMenuStrip.Items.Add(T("editor.menu.edit", "Edit"));
        _simMenuStrip.Items.Add(SimConfigMenuText());
        _simMenuStrip.Items.Add(T("editor.menu.tools", "Tools"));
        _simMenuStrip.Items.Add(SimAboutMenuText());

        var toolbarPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 32,
            WrapContents = false,
            BackColor = PanelBackColor,
            Padding = new Padding(6, 4, 6, 3)
        };

        toolbarPanel.Controls.Add(MakeSimIconButton("WizardExpress", ToolbarIcon.Wizard));
        toolbarPanel.Controls.Add(MakeSimIconButton("Calculator", ToolbarIcon.Test));
        toolbarPanel.Controls.Add(MakeSimIconButton("Open NOD", ToolbarIcon.Open));
        _simFileLabel = new Label
        {
            Text = T("editor.sim.file", "File:"),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(6, 5, 3, 0),
            ForeColor = EditorTextColor
        };
        toolbarPanel.Controls.Add(_simFileLabel);

        _simFileBox = new ComboBox
        {
            Width = 198,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 1, 0, 0),
            BackColor = EditorBackColor,
            ForeColor = EditorTextColor
        };
        _simFileBox.Items.Add(T("editor.sim.current_converter", "Current converter"));
        _simFileBox.SelectedIndex = 0;
        toolbarPanel.Controls.Add(_simFileBox);
        toolbarPanel.SizeChanged += (_, _) => ResizeSimulatorFileBox(toolbarPanel);
        AttachSimulatorDrag(toolbarPanel);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 5,
            Padding = new Padding(12, 8, 12, 8),
            BackColor = SoftPanelBackColor,
            ForeColor = EditorTextColor
        };

        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 26));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));

        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _simInputLabel = new Label
        {
            Text = "Input1",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = EditorTextColor
        };
        body.Controls.Add(_simInputLabel, 3, 0);

        body.Controls.Add(new RadioButton { Enabled = false, Checked = true, Anchor = AnchorStyles.Left, BackColor = SoftPanelBackColor, ForeColor = EditorTextColor }, 0, 1);

        _simInputBox = new TextBox
        {
            Dock = DockStyle.Fill,
            BackColor = EditorBackColor,
            ForeColor = EditorTextColor
        };
        body.Controls.Add(_simInputBox, 3, 1);

        var inputSuffix = new Label
        {
            Name = "SimInputSuffix",
            Text = "",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = EditorTextColor
        };
        body.Controls.Add(inputSuffix, 4, 1);

        _simOutputLabel = new Label
        {
            Text = "Input2",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = EditorTextColor
        };
        body.Controls.Add(_simOutputLabel, 3, 2);

        body.Controls.Add(new RadioButton { Enabled = false, Checked = false, Anchor = AnchorStyles.Left, BackColor = SoftPanelBackColor, ForeColor = EditorTextColor }, 0, 3);

        _simOutputBox = new TextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = EditorBackColor,
            ForeColor = EditorTextColor
        };
        body.Controls.Add(_simOutputBox, 3, 3);

        var outputSuffix = new Label
        {
            Name = "SimOutputSuffix",
            Text = "",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = EditorTextColor
        };
        body.Controls.Add(outputSuffix, 4, 3);

        var optionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = SoftPanelBackColor,
            ForeColor = EditorTextColor
        };

        _simDigitGroup = new CheckBox { Text = T("option.digit_group", "Digit group"), AutoSize = true, AutoCheck = false, Margin = new Padding(0, 6, 14, 0), BackColor = SoftPanelBackColor, ForeColor = MutedTextColor, TabStop = false };
        _simDecimals = new CheckBox { Text = T("option.decimals", "Decimals"), AutoSize = true, AutoCheck = false, Margin = new Padding(0, 6, 6, 0), BackColor = SoftPanelBackColor, ForeColor = MutedTextColor, TabStop = false };
        _simDecimalCount = new NumericUpDown { Width = 50, Minimum = 0, Maximum = 8, ReadOnly = true, InterceptArrowKeys = false, Margin = new Padding(0, 3, 0, 0), BackColor = EditorBackColor, ForeColor = MutedTextColor, TabStop = false };

        optionsPanel.Controls.Add(_simDigitGroup);
        optionsPanel.Controls.Add(_simDecimals);
        optionsPanel.Controls.Add(_simDecimalCount);

        body.SetColumnSpan(optionsPanel, 5);
        body.Controls.Add(optionsPanel, 0, 4);

        _simWindow.Controls.Add(body);
        _simWindow.Controls.Add(toolbarPanel);
        _simWindow.Controls.Add(_simMenuStrip);
        _simWindow.Controls.Add(titlePanel);

        outer.Controls.Add(_simWindow, 0, 0);
        group.Controls.Add(outer);
        return group;
    }

    // Maakt een kleine icon-knop voor de simulator-toolbar.
    private Control MakeSimIconButton(string tooltip, ToolbarIcon icon)
    {
        var box = new PictureBox
        {
            Width = 28,
            Height = 24,
            Margin = new Padding(2, 0, 2, 0),
            BackColor = PanelBackColor,
            Cursor = Cursors.Hand,
            Image = CreateSimulatorToolbarImage(icon),
            Tag = icon,
            SizeMode = PictureBoxSizeMode.CenterImage
        };

        var tip = new ToolTip();
        tip.SetToolTip(box, tooltip);

        return box;
    }

    private Bitmap CreateSimulatorToolbarImage(ToolbarIcon icon)
    {
        var toolEditorIcon = icon switch
        {
            ToolbarIcon.Wizard => ToolEditorIcon.Wizard,
            ToolbarIcon.Open => ToolEditorIcon.Open,
            ToolbarIcon.Test => ToolEditorIcon.Test,
            ToolbarIcon.Save => ToolEditorIcon.Save,
            ToolbarIcon.Find => ToolEditorIcon.Find,
            ToolbarIcon.Validate => ToolEditorIcon.Validate,
            ToolbarIcon.Solver => ToolEditorIcon.Solver,
            ToolbarIcon.Repair => ToolEditorIcon.Repair,
            _ => ToolEditorIcon.New
        };
        return ToolEditorApi.CreateIcon(toolEditorIcon, _palette);
    }

    // Plakt symbool vóór/achter netjes om een simulatorwaarde heen.
    private static string FormatSimValue(string before, string value, string after)
    {
        before = before?.Trim() ?? "";
        value = value?.Trim() ?? "";
        after = after?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(before) && !string.IsNullOrWhiteSpace(after))
            return $"{before} {value} {after}";
        if (!string.IsNullOrWhiteSpace(before))
            return $"{before} {value}".Trim();
        if (!string.IsNullOrWhiteSpace(after))
            return $"{value} {after}".Trim();
        return value;
    }

    // Vult de simulator met metadata, testinput, output en format-instellingen.
    private void UpdateSimulatorPreview(NodUiMetadata meta, string inputValue, string outputValue)
    {
        if (_simTitle is null)
            return;

        _simTitle.Text = string.IsNullOrWhiteSpace(meta.Urln)
            ? (string.IsNullOrWhiteSpace(meta.Name) ? T("editor.sim.window_title", "Converter preview") : meta.Name)
            : meta.Urln;

        var displayName = !string.IsNullOrWhiteSpace(meta.Urln)
            ? meta.Urln
            : !string.IsNullOrWhiteSpace(meta.Name)
                ? meta.Name
                : CurrentTab?.Page.Text ?? T("editor.sim.current_converter", "Current converter");
        if (_simFileBox is not null)
        {
            _simFileBox.Items.Clear();
            _simFileBox.Items.Add(displayName);
            _simFileBox.DropDownWidth = Math.Max(_simFileBox.Width, TextRenderer.MeasureText(displayName, _simFileBox.Font).Width + 28);
            _simFileBox.SelectedIndex = 0;
            ResizeSimulatorFileBox(_simFileBox.Parent);
        }

        _simInputLabel.Text = string.IsNullOrWhiteSpace(meta.Input1) ? "Input 1" : meta.Input1;
        _simOutputLabel.Text = string.IsNullOrWhiteSpace(meta.Input2) ? "Input 2" : meta.Input2;

        _simInputBox.Text = FormatSimValue(meta.SymbolBeforeInput, inputValue, "");
        _simOutputBox.Text = FormatSimValue(meta.SymbolBeforeOutput, outputValue, "");

        var suffix1 = _simWindow.Controls.Find("SimInputSuffix", true).FirstOrDefault() as Label;
        var suffix2 = _simWindow.Controls.Find("SimOutputSuffix", true).FirstOrDefault() as Label;

        if (suffix1 is not null)
            suffix1.Text = string.IsNullOrWhiteSpace(meta.SymbolAfterInput) ? "" : meta.SymbolAfterInput;

        if (suffix2 is not null)
            suffix2.Text = string.IsNullOrWhiteSpace(meta.SymbolAfterOutput) ? "" : meta.SymbolAfterOutput;

        _simDecimals.Checked = !string.IsNullOrWhiteSpace(meta.Format) && meta.Format.Contains(".");
        _simDigitGroup.Checked = false;

        var decimals = 0;
        if (!string.IsNullOrWhiteSpace(meta.Format) && meta.Format.Contains('.'))
            decimals = meta.Format[(meta.Format.IndexOf('.') + 1)..].Count(ch => ch == '0' || ch == '#');
        _simDecimalCount.Value = Math.Min(_simDecimalCount.Maximum, Math.Max(_simDecimalCount.Minimum, decimals));
    }

    // Bouwt de statusbar onderin met status, cursorpositie, regels, encoding en line endings.
    private void BuildStatusbar()
    {
        _statusStrip = new StatusStrip { BackColor = Color.FromArgb(238, 238, 238), Dock = DockStyle.Fill };

        _statusLabel = new ToolStripStatusLabel("Gereed") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _positionLabel = new ToolStripStatusLabel(FormatPositionStatus(1, 1));
        _lineCountLabel = new ToolStripStatusLabel(FormatLineCountStatus(1));
        _encodingLabel = new ToolStripStatusLabel("UTF-8");
        _lineEndingLabel = new ToolStripStatusLabel("CRLF");

        _statusStrip.Items.Add(_statusLabel);
        _statusStrip.Items.Add(_positionLabel);
        _statusStrip.Items.Add(_lineCountLabel);
        _statusStrip.Items.Add(_encodingLabel);
        _statusStrip.Items.Add(_lineEndingLabel);

        _rootLayout.Controls.Add(_statusStrip, 0, 2);
    }

    // Bouwt het kleine HTML-helpvenster voor NOD-keyword suggesties.
    private void BuildNodHelpPopup()
    {
        _nodHelpBrowser = new WebView2
        {
            Dock = DockStyle.Fill,
            AllowExternalDrop = false,
            DefaultBackgroundColor = Color.Transparent,
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = WebView2UserDataFolder.GetPath()
            }
        };
        _nodHelpBrowser.NavigationStarting += NodHelpBrowser_NavigationStarting;
        _nodHelpBrowser.CoreWebView2InitializationCompleted += (_, e) =>
        {
            if (!e.IsSuccess || _nodHelpBrowser.CoreWebView2 is null)
                return;

            _nodHelpBrowser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _nodHelpBrowser.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            _nodHelpBrowser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            _nodHelpBrowser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _nodHelpBrowser.CoreWebView2.Profile.PreferredColorScheme = PreferredWebViewColorScheme;
            _nodHelpBrowser.CoreWebView2.WebMessageReceived += NodHelpBrowser_WebMessageReceived;
            ShowPendingNodHelpHtmlIfReady();
        };
        _ = InitializeNodHelpBrowserAsync();

        _nodHelpPopup = new NodHelpPopupPanel
        {
            Width = NodHelpPopupMaxWidth,
            Height = NodHelpPopupMinHeight,
            DarkMode = IsDarkTheme,
            Visible = false
        };
        _nodHelpPopup.Controls.Add(_nodHelpBrowser);
        Controls.Add(_nodHelpPopup);
        _nodHelpPopup.BringToFront();
    }

    // Past de kleine help-popup aan de hoogte van de HTML-inhoud aan.
    private void NodHelpBrowser_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var message = e.TryGetWebMessageAsString();
        const string prefix = "nodhelp-height:";
        if (string.IsNullOrWhiteSpace(message) || !message.StartsWith(prefix, StringComparison.Ordinal))
            return;

        if (!int.TryParse(message[prefix.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var contentHeight))
            return;

        var newHeight = Math.Clamp(contentHeight + _nodHelpPopup.Padding.Vertical + 4, NodHelpPopupMinHeight, NodHelpPopupMaxHeight);
        if (_nodHelpPopup.Height == newHeight)
            return;

        _nodHelpPopup.Height = newHeight;
        if (_nodHelpPopup.Visible)
            KeepNodHelpPopupInBounds();
    }

    // Zoek/commentaar: Start de moderne Edge/WebView2 engine voor de kleine NOD-help popup.
    private async Task InitializeNodHelpBrowserAsync()
    {
        try
        {
            await _nodHelpBrowser.EnsureCoreWebView2Async();
            ShowPendingNodHelpHtmlIfReady();
        }
        catch (COMException)
        {
            _nodHelpBrowserFailed = true;
        }
        catch (ObjectDisposedException)
        {
            _nodHelpBrowserFailed = true;
        }
        catch (Exception ex)
        {
            _nodHelpBrowserFailed = true;
            _nodHelpBrowser.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8),
                Text = "WebView2 kon niet starten.\r\n" + ex.Message
            });
        }
    }

    private void AddBlankNewTab(EditorTab? afterTab = null)
    {
        AddNewTab(T("editor.tab.new", "new"), "", null, dirty: false, afterTab);
    }

    private void ApplyEditorTheme(EditorTab tab)
    {
        tab.Content.BackColor = BorderColor;
        tab.Editor.BackColor = EditorBackColor;
        tab.Editor.ForeColor = EditorTextColor;
        tab.LineNumbers.BackColor = IsDarkTheme ? Color.FromArgb(30, 41, 59) : Color.White;
        tab.LineNumbers.ForeColor = MutedTextColor;
        tab.Page.BackColor = ContentBackColor;
        tab.Page.ForeColor = EditorTextColor;
    }

    // Maakt een nieuwe editor-tab met regelnummers, syntax highlighting en preview-updates.
    private void AddNewTab(string title, string text, string? path, bool dirty, EditorTab? afterTab = null)
    {
        var page = new TabPage(title);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.White,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var lineNumbers = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.None,
            Font = new Font("Consolas", 10),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(120, 120, 120),
            BorderStyle = BorderStyle.None,
            TextAlign = HorizontalAlignment.Right,
            Margin = Padding.Empty,
            TabStop = false,
            WordWrap = false,
            ShortcutsEnabled = false
        };

        var editor = new RichTextBox
        {
            Multiline = true,
            AcceptsTab = true,
            WordWrap = false,
            ScrollBars = RichTextBoxScrollBars.Both,
            Font = new Font("Consolas", 10),
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            ForeColor = Color.Black,
            DetectUrls = false,
            Margin = Padding.Empty
        };
        editor.ContextMenuStrip = CreateEditorContextMenu(editor);

        var header = CreateEditorTabHeader(page);
        var normalizedText = NodTextNormalizer.NormalizeForEditor(text, repairConcatenated: true);
        var tab = new EditorTab
        {
            Page = page,
            HeaderPanel = header.Panel,
            HeaderTitle = header.Title,
            HeaderCloseButton = header.CloseButton,
            Content = layout,
            Editor = editor,
            LineNumbers = lineNumbers,
            Path = path,
            CleanText = dirty ? "" : normalizedText,
            Dirty = dirty,
            HistoryText = normalizedText
        };
        ApplyEditorTheme(tab);

        editor.TextChanged += (_, _) =>
        {
            if (_highlighting || _applyingTextHistory) return;
            InvalidateGraphPreviewData();
            TrackEditorTextChange(tab);
            SetTabDirty(tab, !IsCleanEditorText(tab));
            UpdateLineNumbers(tab);
            ScheduleSyntaxHighlight(tab);
            UpdateUiState();
            SchedulePreviewUpdate(updateGraph: true);
            UpdateNodKeywordTip(editor);
        };

        editor.KeyUp += (_, _) =>
        {
            UpdateUiState();
            UpdateNodKeywordTip(editor);
        };
        editor.KeyDown += (_, e) =>
        {
            if (e.Control && !e.Shift && e.KeyCode == Keys.Z)
            {
                UndoEditor(editor);
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
            else if ((e.Control && e.KeyCode == Keys.Y) || (e.Control && e.Shift && e.KeyCode == Keys.Z))
            {
                RedoEditor(editor);
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        };
        editor.MouseDown += (_, e) =>
        {
            HideNodHelpPopup();
            if (e.Button == MouseButtons.Right)
                MoveEditorCaretForContextMenu(editor, e.Location);
        };
        editor.MouseUp += (_, e) =>
        {
            UpdateUiState();
            if (e.Button != MouseButtons.Right)
                UpdateNodKeywordTip(editor);
        };
        editor.SelectionChanged += (_, _) =>
        {
            if (!_highlighting && !_applyingTextHistory && !_suppressNodHelpUpdates)
                UpdateNodKeywordTip(editor);
        };
        editor.Leave += (_, _) => HideNodHelpPopup();

        layout.Controls.Add(lineNumbers, 0, 0);
        layout.Controls.Add(editor, 1, 0);

        _tabs[page] = tab;
        AttachEditorTabContextMenu(tab);
        _editorTabStrip.Controls.Add(tab.HeaderPanel);
        if (afterTab is not null && _tabs.ContainsKey(afterTab.Page))
        {
            var tabIndex = _tabControl.TabPages.IndexOf(afterTab.Page);
            var headerIndex = _editorTabStrip.Controls.IndexOf(afterTab.HeaderPanel);
            _tabControl.TabPages.Insert(Math.Min(tabIndex + 1, _tabControl.TabPages.Count), page);
            _editorTabStrip.Controls.SetChildIndex(tab.HeaderPanel, Math.Min(headerIndex + 1, _editorTabStrip.Controls.Count - 1));
        }
        else
        {
            _tabControl.TabPages.Add(page);
        }

        SelectEditorTab(page);

        editor.Text = normalizedText;
        if (!dirty)
            tab.CleanText = editor.Text;

        SetTabDirty(tab, dirty || !IsCleanEditorText(tab));
        UpdateLineNumbers(tab);
        HighlightSyntax(tab);
        editor.ClearUndo();
        UpdateTabTitle(tab);
        RefreshEditorTabStrip();
        UpdatePreviewFromCurrentText();
        UpdateUndoRedoState();
    }

    // Bouwt een normaal rechtermuisknopmenu voor tekstbewerking in de NOD-editor.
    private ContextMenuStrip CreateEditorContextMenu(RichTextBox editor)
    {
        var menu = new ContextMenuStrip();
        var undo = new ToolStripMenuItem(T("editor.menu.edit.undo", "Ongedaan maken"), null, (_, _) => UndoEditor(editor));
        var redo = new ToolStripMenuItem(T("editor.menu.edit.redo", "Opnieuw uitvoeren"), null, (_, _) => RedoEditor(editor));
        var cut = new ToolStripMenuItem(T("editor.menu.edit.cut", "Cut"), null, (_, _) => editor.Cut());
        var copy = new ToolStripMenuItem(T("editor.menu.edit.copy", "Copy"), null, (_, _) => editor.Copy());
        var paste = new ToolStripMenuItem(T("editor.menu.edit.paste", "Paste"), null, (_, _) => editor.Paste());
        var selectAll = new ToolStripMenuItem(T("editor.menu.edit.select_all", "Select all"), null, (_, _) => editor.SelectAll());
        var help = new ToolStripMenuItem(T("editor.context.help_command", "Help over command"), null, (_, _) => ShowNodKeywordHelpAtCaret(editor));

        menu.Items.AddRange(
        [
            undo,
            redo,
            new ToolStripSeparator(),
            cut,
            copy,
            paste,
            new ToolStripSeparator(),
            selectAll,
            new ToolStripSeparator(),
            help
        ]);

        menu.Opening += (_, _) =>
        {
            HideNodHelpPopup();
            var hasSelection = editor.SelectionLength > 0;
            var tab = FindTab(editor);
            undo.Enabled = tab?.UndoTextStack.Count > 0;
            redo.Enabled = tab?.RedoTextStack.Count > 0;
            cut.Enabled = hasSelection;
            copy.Enabled = hasSelection;
            paste.Enabled = Clipboard.ContainsText();
            selectAll.Enabled = editor.TextLength > 0;
            help.Enabled = TryGetKeywordAtCaret(editor, out var keyword) &&
                NodKeywords.Any(key => key.Equals(keyword, StringComparison.OrdinalIgnoreCase));
        };

        return menu;
    }

    // Bij rechterklik buiten de selectie hoort het menu bij de aangeklikte plek.
    private static void MoveEditorCaretForContextMenu(RichTextBox editor, Point location)
    {
        var index = editor.GetCharIndexFromPosition(location);
        var selectionStart = editor.SelectionStart;
        var selectionEnd = selectionStart + editor.SelectionLength;
        if (editor.SelectionLength > 0 && index >= selectionStart && index <= selectionEnd)
            return;

        editor.SelectionStart = index;
        editor.SelectionLength = 0;
    }

    private List<string> LoadRecentFiles()
    {
        try
        {
            if (!File.Exists(RecentFilesConfigPath))
                return new List<string>();

            return File.ReadAllLines(RecentFilesConfigPath)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith("#") && !line.StartsWith(";"))
                .Select(Path.GetFullPath)
                .Where(IsRecentNodFile)
                .DistinctBy(RecentFileIdentity, StringComparer.OrdinalIgnoreCase)
                .Take(RecentFilesLimit)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private void SaveRecentFiles(IEnumerable<string> paths)
    {
        try
        {
            var cleanPaths = paths
                .Select(Path.GetFullPath)
                .Where(IsRecentNodFile)
                .DistinctBy(RecentFileIdentity, StringComparer.OrdinalIgnoreCase)
                .Take(RecentFilesLimit)
                .ToList();

            var lines = new List<string> { "# Syscalculator NOD Editor recent files." };
            lines.AddRange(cleanPaths);
            Directory.CreateDirectory(RecentFilesConfigDirectory);
            File.WriteAllLines(RecentFilesConfigPath, lines);
        }
        catch
        {
            // Recent files are convenience state; opening/saving a NOD file must remain the main action.
        }
    }

    private void AddRecentFile(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!IsRecentNodFile(fullPath))
            return;

        var recentFiles = new List<string> { fullPath };
        recentFiles.AddRange(LoadRecentFiles().Where(item => !string.Equals(item, fullPath, StringComparison.OrdinalIgnoreCase)));
        SaveRecentFiles(recentFiles);
        WindowsShellIntegration.AddRecentDocument(fullPath);
        PopulateRecentFilesMenu();
    }

    private void RemoveRecentFile(string path)
    {
        var fullPath = Path.GetFullPath(path);
        SaveRecentFiles(LoadRecentFiles().Where(item => !string.Equals(item, fullPath, StringComparison.OrdinalIgnoreCase)));
        PopulateRecentFilesMenu();
    }

    private void PopulateRecentFilesMenu()
    {
        if (_recentFilesMenuItem is null)
            return;

        _recentFilesMenuItem.DropDownItems.Clear();
        var recentFiles = LoadRecentFiles();
        SaveRecentFiles(recentFiles);
        _recentFilesMenuItem.Enabled = recentFiles.Count > 0;

        if (recentFiles.Count == 0)
        {
            _recentFilesMenuItem.DropDownItems.Add(new ToolStripMenuItem(T("editor.menu.file.recent_empty", "No recent files"))
            {
                Enabled = false
            });
            return;
        }

        for (var index = 0; index < recentFiles.Count; index++)
        {
            var path = recentFiles[index];
            var item = new ToolStripMenuItem($"{index + 1}. {Path.GetFileName(path)}")
            {
                ToolTipText = path
            };
            item.Click += (_, _) => OpenRecentFile(path);
            _recentFilesMenuItem.DropDownItems.Add(item);
        }
    }

    private static string RecentFileIdentity(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var marker = $"{Path.DirectorySeparatorChar}Converters{Path.DirectorySeparatorChar}";
        var index = fullPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? fullPath[(index + 1)..] : fullPath;
    }

    private static bool IsRecentNodFile(string path)
    {
        return File.Exists(path) &&
               Path.GetExtension(path).Equals(".nod", StringComparison.OrdinalIgnoreCase);
    }

    private void OpenRecentFile(string path)
    {
        if (!File.Exists(path))
        {
            RemoveRecentFile(path);
            MessageBox.Show(
                this,
                string.Format(T("dialog.open_nod.file_not_found", "File not found: {0}"), path),
                T("dialog.open_nod.failed_title", "NOD load failed"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            OpenFileInNewTab(path);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, T("dialog.open_nod.failed_title", "NOD load failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Opent een bestand vanaf schijf en zet het in een nieuwe tab.
    private void OpenFileInNewTab(string path)
    {
        var raw = File.ReadAllText(path);
        var text = NodTextNormalizer.NormalizeForEditor(raw, repairConcatenated: true);
        AddNewTab(Path.GetFileName(path), text, path, dirty: false);
        AddRecentFile(path);
        SetStatus(T("editor.status.ready", "Gereed"));
    }

    // Menu/toolbar actie: laat de gebruiker een .nod-bestand openen.
    private void Open_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Filter = T("editor.dialog.filter", "NOD files (*.nod)|*.nod|All files (*.*)|*.*"),
            Title = T("editor.dialog.open_title", "Open NOD"),
            Multiselect = true
        };

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        foreach (var file in dlg.FileNames)
            OpenFileInNewTab(file);
    }

    private void TemplateWizard_Click(object? sender, EventArgs e)
    {
        using var form = new NodTemplateWizardForm(_language)
        {
            TopMost = TopMost
        };

        if (form.ShowDialog(this) != DialogResult.OK || form.SelectedTemplate is null)
            return;

        AddNewTab(form.SelectedTemplate.FileName, form.SelectedTemplate.Text, null, dirty: true);
        SetStatus(string.Format(T("editor.status.template_created", "Template created: {0}"), form.SelectedTemplate.Title));
    }

    // Menu/toolbar actie: slaat de huidige tab op.
    private void Save_Click(object? sender, EventArgs e)
    {
        var tab = CurrentTab;
        if (tab is null) return;

        TrySaveTab(tab);
    }

    private void UndoCurrentEditor()
    {
        if (CurrentEditor is { } editor)
            UndoEditor(editor);
    }

    private void RedoCurrentEditor()
    {
        if (CurrentEditor is { } editor)
            RedoEditor(editor);
    }

    private void UndoEditor(RichTextBox editor)
    {
        var tab = FindTab(editor);
        if (tab is null || tab.UndoTextStack.Count == 0)
            return;

        tab.RedoTextStack.Push(editor.Text);
        ApplyEditorHistoryText(tab, tab.UndoTextStack.Pop());
    }

    private void RedoEditor(RichTextBox editor)
    {
        var tab = FindTab(editor);
        if (tab is null || tab.RedoTextStack.Count == 0)
            return;

        tab.UndoTextStack.Push(editor.Text);
        ApplyEditorHistoryText(tab, tab.RedoTextStack.Pop());
    }

    private EditorTab? FindTab(RichTextBox editor)
    {
        return _tabs.Values.FirstOrDefault(tab => ReferenceEquals(tab.Editor, editor));
    }

    private void ApplyEditorHistoryText(EditorTab tab, string text)
    {
        var editor = tab.Editor;
        var selectionStart = Math.Min(editor.SelectionStart, text.Length);

        RunEditorAntiShock(editor, suppressTextHistory: true, hideTip: true, () =>
        {
            editor.Text = text;
            editor.Select(selectionStart, 0);
            tab.HistoryText = text;
            HighlightSyntax(tab);
        });

        SetTabDirty(tab, !IsCleanEditorText(tab));
        UpdateLineNumbers(tab);
        UpdatePreviewFromCurrentText();
        UpdateUiState();
    }

    private void HighlightSyntaxForEdit(RichTextBox editor)
    {
        var tab = FindTab(editor);
        if (tab is not null)
            HighlightSyntax(tab);
    }

    private void TrackEditorTextChange(EditorTab tab)
    {
        if (_applyingTextHistory)
            return;

        var text = tab.Editor.Text;
        if (string.Equals(text, tab.HistoryText, StringComparison.Ordinal))
            return;

        tab.UndoTextStack.Push(tab.HistoryText);
        tab.RedoTextStack.Clear();
        tab.HistoryText = text;
    }

    private void RunEditorAntiShock(Control control, bool suppressTextHistory, bool hideTip, Action action)
    {
        var previousTextHistorySuppression = _applyingTextHistory;
        var previousTipSuppression = _suppressNodHelpUpdates;

        _applyingTextHistory = _applyingTextHistory || suppressTextHistory;
        _suppressNodHelpUpdates = true;

        if (hideTip)
            HideNodHelpPopup();

        SetControlRedraw(control, enabled: false);
        try
        {
            action();
        }
        finally
        {
            _applyingTextHistory = previousTextHistorySuppression;
            _suppressNodHelpUpdates = previousTipSuppression;
            SetControlRedraw(control, enabled: true);
            control.Refresh();
        }
    }

    private static void SetControlRedraw(Control control, bool enabled)
    {
        if (!control.IsHandleCreated)
            return;

        SendMessage(control.Handle, WmSetRedraw, enabled ? 1 : 0, 0);
        if (enabled)
            control.Invalidate();
    }

    // Menu actie: slaat de huidige tab op onder een gekozen bestandsnaam.
    private void SaveAs_Click(object? sender, EventArgs e)
    {
        var tab = CurrentTab;
        if (tab is null) return;

        TrySaveTabAs(tab);
    }

    // Schrijft een tab naar schijf en markeert hem als opgeslagen.
    private bool TrySaveTab(EditorTab tab)
    {
        if (tab.Path is null)
            return TrySaveTabAs(tab);

        try
        {
            SaveTab(tab, tab.Path);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, T("editor.dialog.save_failed_title", "Save failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private bool TrySaveTabAs(EditorTab tab)
    {
        using var dlg = new SaveFileDialog
        {
            Filter = T("editor.dialog.filter", "NOD files (*.nod)|*.nod|All files (*.*)|*.*"),
            Title = T("editor.dialog.save_title", "Save NOD as"),
            DefaultExt = "nod",
            AddExtension = true,
            FileName = Path.GetFileNameWithoutExtension(tab.Page.Text.Replace("*", "").Trim()) + ".nod"
        };

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return false;

        try
        {
            SaveTab(tab, dlg.FileName);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, T("editor.dialog.save_failed_title", "Save failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void SaveTab(EditorTab tab, string path)
    {
        var text = NodTextNormalizer.NormalizeForEditor(tab.Editor.Text, repairConcatenated: false);
        File.WriteAllText(path, text);
        tab.Path = path;
        if (!string.Equals(tab.Editor.Text, text, StringComparison.Ordinal))
        {
            RunEditorAntiShock(tab.Editor, suppressTextHistory: true, hideTip: true, () =>
            {
                tab.Editor.Text = text;
                tab.HistoryText = text;
                HighlightSyntax(tab);
                tab.Editor.ClearUndo();
            });
        }

        tab.CleanText = tab.Editor.Text;
        SetTabDirty(tab, false);
        AddRecentFile(path);
        SetStatus(T("editor.status.saved_short", "Opgeslagen"));
    }

    private void NormalizeEditorText(EditorTab tab, bool repairConcatenated)
    {
        var editor = tab.Editor;
        var normalized = NodTextNormalizer.NormalizeForEditor(editor.Text, repairConcatenated);
        if (string.Equals(editor.Text, normalized, StringComparison.Ordinal))
            return;

        var selectionStart = Math.Min(editor.SelectionStart, normalized.Length);
        var selectionLength = Math.Min(editor.SelectionLength, normalized.Length - selectionStart);
        RunEditorAntiShock(editor, suppressTextHistory: true, hideTip: true, () =>
        {
            editor.Text = normalized;
            editor.Select(selectionStart, Math.Max(0, selectionLength));
            HighlightSyntax(tab);
        });

        UpdateUndoRedoState();
    }

    // Sluit de actieve tab.
    private void CloseCurrentTab()
    {
        var tab = CurrentTab;
        if (tab is not null)
            CloseTab(tab);
    }

    private void AttachEditorTabContextMenu(EditorTab tab)
    {
        var menu = CreateEditorTabContextMenu(tab);
        ToolEditorTabsApi.AttachContextMenu(menu, tab.HeaderPanel, tab.HeaderTitle, tab.HeaderCloseButton);
    }

    private ContextMenuStrip CreateEditorTabContextMenu(EditorTab tab)
    {
        var menu = new ContextMenuStrip();
        var newRight = menu.Items.Add(T("editor.tabs.new_right", "New tab to the right"));
        menu.Items.Add(new ToolStripSeparator());
        var closeAll = menu.Items.Add(T("editor.tabs.close_all", "Close all tabs"));
        var closeRight = menu.Items.Add(T("editor.tabs.close_right", "Close tabs to the right"));
        var closeLeft = menu.Items.Add(T("editor.tabs.close_left", "Close tabs to the left"));

        menu.Opening += (_, _) =>
        {
            var tabs = GetEditorTabsInHeaderOrder();
            var index = tabs.IndexOf(tab);
            closeAll.Enabled = tabs.Count > 0;
            closeLeft.Enabled = index > 0;
            closeRight.Enabled = index >= 0 && index < tabs.Count - 1;
        };

        newRight.Click += (_, _) => AddBlankNewTab(tab);
        closeAll.Click += (_, _) => CloseEditorTabs(GetEditorTabsInHeaderOrder());
        closeRight.Click += (_, _) =>
        {
            var tabs = GetEditorTabsInHeaderOrder();
            var index = tabs.IndexOf(tab);
            if (index >= 0)
                CloseEditorTabs(tabs.Skip(index + 1));
        };
        closeLeft.Click += (_, _) =>
        {
            var tabs = GetEditorTabsInHeaderOrder();
            var index = tabs.IndexOf(tab);
            if (index > 0)
                CloseEditorTabs(tabs.Take(index));
        };

        return menu;
    }

    private List<EditorTab> GetEditorTabsInHeaderOrder()
    {
        var result = new List<EditorTab>();
        foreach (Control control in _editorTabStrip.Controls)
        {
            var tab = _tabs.Values.FirstOrDefault(item => ReferenceEquals(item.HeaderPanel, control));
            if (tab is not null)
                result.Add(tab);
        }

        return result;
    }

    private bool CloseEditorTabs(IEnumerable<EditorTab> tabs)
    {
        foreach (var tab in tabs.ToList())
        {
            if (_tabs.ContainsKey(tab.Page) && !CloseTab(tab))
                return false;
        }

        return true;
    }

    // Sluit een specifieke tab en vraagt eerst om opslaan bij wijzigingen.
    private bool CloseTab(EditorTab tab)
    {
        if (tab.Dirty)
        {
            var choice = ShowUnsavedChangesDialog(
                T("editor.dialog.close_tab_title", "Close tab"),
                T("editor.dialog.close_dirty", "Tab has unsaved changes."));

            if (choice == UnsavedChangesChoice.Cancel ||
                (choice == UnsavedChangesChoice.Save && !TrySaveTab(tab)))
                return false;
        }

        var removedIndex = _tabControl.TabPages.IndexOf(tab.Page);
        _tabs.Remove(tab.Page);
        _editorTabStrip.Controls.Remove(tab.HeaderPanel);
        tab.HeaderPanel.Dispose();
        if (_editorContentPanel.Controls.Contains(tab.Content))
            _editorContentPanel.Controls.Remove(tab.Content);
        _tabControl.TabPages.Remove(tab.Page);
        tab.Content.Dispose();
        tab.Page.Dispose();

        if (_tabControl.TabPages.Count == 0)
            ClearActiveDocument();
        else
            SelectEditorTab(_tabControl.TabPages[Math.Min(Math.Max(removedIndex, 0), _tabControl.TabPages.Count - 1)]);

        UpdateUiState();
        return true;
    }

    private void ClearActiveDocument()
    {
        _selectedEditorPage = null;
        _editorContentPanel.Controls.Clear();
        _previewDialogName.Text = "-";
        _previewInputName.Text = "-";
        _previewOutputName.Text = "-";
        _previewInputSample.Text = "-";
        _previewOutputSample.Text = "-";
        _previewFormat.Text = "-";
        SetPreviewIntro(Array.Empty<string>());
        ApplySolverPanelVisibility(false);
        _pendingFormulaMathMarkup = PlainMathMarkup("");
        _pendingCalculationMathMarkup = PlainMathMarkup("");
        RenderPendingMath();
        UpdateSimulatorPreview(new NodUiMetadata(), "22", "-");
        _testOutput.Text = "";
        _testButton.Text = T("editor.toolbar.test", "Test");
        _graphPreviewFitPoints.Clear();
        _graphPreviewPoints.Clear();
        _graphPreviewStepPoints.Clear();
        _graphPreviewDocument = null;
        _graphHasView = false;
        _graphDisabledMessage = "";
        _graphCanvas?.Invalidate();
        _graph3DCanvas?.Invalidate();
        _graphPointTable?.Rows.Clear();
        _graph3DPointTable?.Rows.Clear();
        RefreshEditorTabStrip();
    }

    // Menu/toolbar actie: controleert of de huidige NOD geldig geparsed kan worden.
    private void Validate_Click(object? sender, EventArgs e)
    {
        var tab = CurrentTab;
        if (tab is null) return;

        try
        {
            NormalizeEditorText(tab, repairConcatenated: true);
            NodParser.Parse(tab.Editor.Text);
            SetStatus(T("editor.status.valid", "NOD valid."));
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(T("status.error", "Error: {0}"), ex.Message));
            MessageBox.Show(this, ex.Message, T("editor.dialog.validation_error", "Validation error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // Menu/toolbar actie: voert de huidige NOD uit met de testinput.
    private void Test_Click(object? sender, EventArgs e)
    {
        var tab = CurrentTab;
        if (tab is null) return;

        try
        {
            NormalizeEditorText(tab, repairConcatenated: true);
            var normalized = NodTextNormalizer.Normalize(tab.Editor.Text, repairConcatenated: true);
            var doc = NodParser.Parse(normalized);
            var meta = NodUiMetadata.Parse(normalized);

            if (IsSolverDocument(normalized, doc))
            {
                ShowSolverStepsDialog(tab.Editor.Text);
                SetStatus(T("editor.status.solver_steps_opened", "Solver steps opened."));
                return;
            }

            var result = NodEngine.ConvertForward(doc, _testInput.Text);
            var outputText = FormatResultTextForCurrentCulture(result.Text);
            _testOutput.Text = outputText;
            _pendingFormulaMathMarkup = BuildFormulaMathMarkup(doc, meta);
            _pendingCalculationMathMarkup = BuildCalculationMathMarkup(doc, _testInput.Text, outputText);
            RenderPendingMath();
            SetStatus(T("editor.status.test_done", "Test completed."));
        }
        catch (Exception ex)
        {
            _pendingFormulaMathMarkup = PlainMathMarkup("");
            _pendingCalculationMathMarkup = PlainMathMarkup("");
            RenderPendingMath();
            SetStatus(string.Format(T("status.error", "Error: {0}"), ex.Message));
            MessageBox.Show(this, ex.Message, T("editor.dialog.test_error", "Test error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // Bouwt de MathML-weergave van de formule zelf.
    private static string BuildFormulaMathMarkup(NodDocument doc, NodUiMetadata? meta = null)
    {
        if (doc.LegacyMathSteps.Count > 0)
            return BuildLegacyFormulaMathMarkup(doc, meta);

        if (doc.MathExpressions20.Count > 0)
            return InlineMathSequence(doc.MathExpressions20.Select(ExpressionToMathMl));

        if (doc.CalculusSteps.Count > 0)
            return InlineMathSequence(doc.CalculusSteps.Select(step => CalculusToMathMl(step.Operation.ToString(), step.Expression)));

        if (doc.Equation is not null && !string.IsNullOrWhiteSpace(doc.Equation.EquationText))
            return InlineMath(ExpressionToMathMl(doc.Equation.EquationText));

        if (doc.ChangeRules.Count > 0)
            return PlainMathMarkup(string.Join(" \u21D2 ", doc.ChangeRules.Select(rule => $"chg {rule.OldPrefix} \u2192 {rule.NewPrefix}")));

        if (doc.TranslateRules.Count > 0)
            return PlainMathMarkup(string.Join(" \u21D2 ", doc.TranslateRules.Select(rule => $"trans {rule.OldValue} \u2192 {rule.NewValue}")));

        return PlainMathMarkup("Direct");
    }

    // Bouwt MathML voor oude legacy math-stappen zoals "math ans * 1,8".
    private static string BuildLegacyFormulaMathMarkup(NodDocument doc, NodUiMetadata? meta)
    {
        var inputSymbol = FormulaSymbolOrDefault(meta?.SymbolAfterInput, "x");
        var outputSymbol = FormulaSymbolOrDefault(meta?.SymbolAfterOutput, "y");

        var expression = IdentifierToMathMl(inputSymbol);
        foreach (var step in doc.LegacyMathSteps)
            expression = ApplyLegacyStepToMathMl(expression, step.Operator.ToString(), FormatTraceValue(step.Number));

        return InlineMath(MathRow(IdentifierToMathMl(outputSymbol), "<mo>=</mo>", expression));
    }

    // Kiest een kort formulasymbool of gebruikt een fallback zoals x/y.
    private static string FormulaSymbolOrDefault(string? symbol, string fallback)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return fallback;

        // Symb3/Symb4 kan soms een eenheid zijn. Voor formuleweergave is één kort symbool het mooist.
        var clean = symbol.Trim();
        return clean.Length <= 4 ? clean : fallback;
    }

    // Past één legacy rekenstap toe op bestaande MathML.
    private static string ApplyLegacyStepToMathMl(string left, string op, string number)
    {
        var right = NumberToMathMl(number);
        return op.Trim() switch
        {
            "+" => MathRow(left, "<mo>+</mo>", right),
            "-" => MathRow(left, "<mo>-</mo>", right),
            "*" or "×" or "x" => MathRow(left, "<mo>&#xD7;</mo>", right),
            "/" or "÷" => $"<mfrac>{left}{right}</mfrac>",
            "^" => $"<msup>{left}{right}</msup>",
            _ => MathRow(left, OperatorToMathMl(op), right),
        };
    }

    // Bouwt de MathML/tekstweergave van de berekeningsstappen.
    private static string BuildCalculationMathMarkup(NodDocument doc, string input, string output)
    {
        if (doc.LegacyMathSteps.Count > 0 || doc.MathExpressions20.Count > 0 || doc.CalculusSteps.Count > 0)
        {
            var trace = NodEngine.ConvertForwardWithTrace(doc, input);
            return InlineMathSequence(trace.Trace.Steps.Select(step =>
            {
                var expression = Regex.Replace(
                    step.Expression,
                    @"\bans\b",
                    FormatTraceValue(step.InputValue),
                    RegexOptions.IgnoreCase);

                return MathRow(ExpressionToMathMl(expression), "<mo>=</mo>", NumberToMathMl(FormatTraceValue(step.OutputValue)));
            }));
        }

        if (doc.ChangeRules.Count > 0)
            return PlainMathMarkup(DescribeChangeCalculation(doc.ChangeRules, input, output));

        if (doc.TranslateRules.Count > 0)
            return PlainMathMarkup(DescribeTranslateCalculation(doc.TranslateRules, input, output));

        return InlineMath(MathRow(TextToken(input), "<mo>=</mo>", TextToken(output)));
    }

    // Wikkelt MathML-inhoud in een math-element met vaste blauwe kleur.
    private static string InlineMath(string content) => $"<math mathcolor=\"#004AAD\">{content}</math>";

    // Zet meerdere MathML-stappen achter elkaar met een dubbele pijl ertussen.
    private static string InlineMathSequence(IEnumerable<string> parts)
    {
        var items = parts.Select(InlineMath).ToList();
        if (items.Count == 0)
            return InlineMath("");

        var rows = items.Select((item, index) =>
            $"<span class=\"math-step-row\">{item}{(index < items.Count - 1 ? "<span class=\"math-step-arrow\">&#x2192;</span>" : "")}</span>");
        var joined = string.Concat(rows);

        return $"<span class=\"math-step-sequence\">{joined}</span>";
    }

    // Maakt veilige platte tekst voor de formule-preview.
    private static string PlainMathMarkup(string text) => $"<span class=\"plain\" style=\"color:#004AAD\">{WebUtility.HtmlEncode(text)}</span>";

    // Combineert MathML-fragmenten in één rij.
    private static string MathRow(params string[] parts) => $"<mrow>{string.Concat(parts)}</mrow>";

    // Maakt een tekst-token voor MathML.
    private static string TextToken(string text) => $"<mtext>{WebUtility.HtmlEncode(text)}</mtext>";

    // Maakt een variabele/identifier-token voor MathML.
    private static string IdentifierToMathMl(string text) => $"<mi>{WebUtility.HtmlEncode(text)}</mi>";

    // Maakt een getal-token voor MathML.
    private static string NumberToMathMl(string text) => $"<mn>{WebUtility.HtmlEncode(text)}</mn>";

    // Maakt een operator-token voor MathML.
    private static string OperatorToMathMl(string text) => $"<mo>{WebUtility.HtmlEncode(text)}</mo>";

    // Maakt een functienaam-token voor MathML.
    private static string FunctionNameToMathMl(string text) => $"<mo>{WebUtility.HtmlEncode(text.ToLowerInvariant())}</mo>";

    // Controleert of een naam een bekende wiskundige functie is.
    private static bool IsKnownFunctionName(string text)
        => text.Equals("sin", StringComparison.OrdinalIgnoreCase)
            || text.Equals("cos", StringComparison.OrdinalIgnoreCase)
            || text.Equals("tan", StringComparison.OrdinalIgnoreCase)
            || text.Equals("asin", StringComparison.OrdinalIgnoreCase)
            || text.Equals("acos", StringComparison.OrdinalIgnoreCase)
            || text.Equals("atan", StringComparison.OrdinalIgnoreCase)
            || text.Equals("log", StringComparison.OrdinalIgnoreCase)
            || text.Equals("ln", StringComparison.OrdinalIgnoreCase)
            || text.Equals("abs", StringComparison.OrdinalIgnoreCase)
            || text.Equals("exp", StringComparison.OrdinalIgnoreCase);

    // Zet calculus-operaties zoals integral, limit en diff om naar MathML.
    private static string CalculusToMathMl(string operation, string expression)
    {
        var body = ExpressionToMathMl(expression);
        return operation.ToLowerInvariant() switch
        {
            "integral" => MathRow("<msubsup><mo>&#x222B;</mo><mn>0</mn><mn>1</mn></msubsup>", body, "<mtext>dx</mtext>"),
            "limit" => MathRow("<munder><mo>lim</mo><mrow><mi>x</mi><mo>&#x2192;</mo><mn>0</mn></mrow></munder>", body),
            "diff" or "derivative" => MathRow("<mfrac><mi>d</mi><mtext>dx</mtext></mfrac>", body),
            _ => MathRow(IdentifierToMathMl(operation), body),
        };
    }

    // Zet een simpele wiskundige expressie recursief om naar MathML.
    private static string ExpressionToMathMl(string expression)
    {
        expression = expression.Trim();
        if (expression.Length == 0)
            return TextToken("");

        var equalsIndex = FindTopLevelOperator(expression, '=');
        if (equalsIndex > 0)
        {
            return MathRow(
                ExpressionToMathMl(expression[..equalsIndex]),
                "<mo>=</mo>",
                ExpressionToMathMl(expression[(equalsIndex + 1)..]));
        }

        foreach (var op in new[] { '+', '-' })
        {
            var index = FindTopLevelOperator(expression, op);
            if (index > 0)
                return MathRow(ExpressionToMathMl(expression[..index]), OperatorToMathMl(op.ToString()), ExpressionToMathMl(expression[(index + 1)..]));
        }

        foreach (var op in new[] { '*', '/' })
        {
            var index = FindTopLevelOperator(expression, op);
            if (index > 0)
            {
                var left = ExpressionToMathMl(expression[..index]);
                var right = ExpressionToMathMl(expression[(index + 1)..]);
                return op == '/'
                    ? $"<mfrac>{left}{right}</mfrac>"
                    : MathRow(left, "<mo>&#xD7;</mo>", right);
            }
        }

        var powerIndex = FindTopLevelOperator(expression, '^');
        if (powerIndex > 0)
            return $"<msup>{ExpressionToMathMl(expression[..powerIndex])}{ExpressionToMathMl(expression[(powerIndex + 1)..])}</msup>";

        var functionMatch = Regex.Match(expression, @"^(?<name>[A-Za-z]+)\((?<arg>.*)\)$");
        if (functionMatch.Success)
        {
            var name = functionMatch.Groups["name"].Value;
            var arg = functionMatch.Groups["arg"].Value;
            var args = SplitTopLevelArguments(arg);

            if (IsCombinationFunctionName(name) && args.Count == 2)
                return CombinationToMathMl(args[0], args[1]);

            if (IsPermutationFunctionName(name) && args.Count == 2)
                return PermutationToMathMl(args[0], args[1]);

            if (IsExpectedFunctionName(name) && args.Count >= 2 && args.Count % 2 == 0)
                return ExpectedValueToMathMl(args);

            if (name.Equals("sqrt", StringComparison.OrdinalIgnoreCase) || name.Equals("sqr", StringComparison.OrdinalIgnoreCase))
                return $"<msqrt>{ExpressionToMathMl(arg)}</msqrt>";

            var functionName = IsKnownFunctionName(name) ? FunctionNameToMathMl(name) : IdentifierToMathMl(name);
            return MathRow(functionName, "<mo>(</mo>", ExpressionToMathMl(arg), "<mo>)</mo>");
        }

        if (IsWrappedInParentheses(expression))
            return MathRow("<mo>(</mo>", ExpressionToMathMl(expression[1..^1]), "<mo>)</mo>");

        if (decimal.TryParse(expression.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
            return NumberToMathMl(expression);

        if (expression.Equals("ans", StringComparison.OrdinalIgnoreCase))
            return IdentifierToMathMl("x");

        if (expression.Equals("pi", StringComparison.OrdinalIgnoreCase))
            return IdentifierToMathMl("\u03C0");

        return IdentifierToMathMl(expression);
    }

    // Zet comb/ncr/choose om naar de herkenbare nCr-notatie met de faculteitformule.
    private static string CombinationToMathMl(string nExpression, string rExpression)
    {
        var n = ExpressionToMathMl(nExpression);
        var r = ExpressionToMathMl(rExpression);
        var nMinusR = DifferenceToMathMl(nExpression, rExpression);

        return MathRow(
            BinomialCoefficientToMathMl(n, r),
            "<mo>=</mo>",
            $"<mfrac>{MathRow(n, "<mo>!</mo>")}{MathRow(r, "<mo>!</mo>", "<mo>(</mo>", nMinusR, "<mo>)</mo>", "<mo>!</mo>")}</mfrac>");
    }

    // Zet perm/npr om naar P(n,r) met de faculteitformule.
    private static string PermutationToMathMl(string nExpression, string rExpression)
    {
        var n = ExpressionToMathMl(nExpression);
        var r = ExpressionToMathMl(rExpression);
        var nMinusR = DifferenceToMathMl(nExpression, rExpression);

        return MathRow(
            "<mi>P</mi>",
            "<mo>(</mo>",
            n,
            "<mo>,</mo>",
            r,
            "<mo>)</mo>",
            "<mo>=</mo>",
            $"<mfrac>{MathRow(n, "<mo>!</mo>")}{MathRow("<mo>(</mo>", nMinusR, "<mo>)</mo>", "<mo>!</mo>")}</mfrac>");
    }

    // Zet expected(x,p,...) om naar verwachtingswaarde met uitgewerkte waarde-kans-paren.
    private static string ExpectedValueToMathMl(IReadOnlyList<string> args)
    {
        var terms = new List<string>();
        for (var i = 0; i < args.Count; i += 2)
        {
            terms.Add(MathRow(
                ExpressionToMathMl(args[i]),
                "<mo>&#xD7;</mo>",
                ExpressionToMathMl(args[i + 1])));
        }

        return MathRow(
            "<mi>E</mi>",
            "<mo>(</mo>",
            "<mi>X</mi>",
            "<mo>)</mo>",
            "<mo>=</mo>",
            "<munderover><mo>&#x2211;</mo><mi>i</mi><mi>n</mi></munderover>",
            "<msub><mi>x</mi><mi>i</mi></msub>",
            "<mo>&#xD7;</mo>",
            "<msub><mi>p</mi><mi>i</mi></msub>",
            "<mo>=</mo>",
            JoinMathMlTerms(terms, "<mo>+</mo>"));
    }

    private static string JoinMathMlTerms(IReadOnlyList<string> terms, string separator)
    {
        if (terms.Count == 0)
            return "";

        var parts = new List<string>();
        for (var i = 0; i < terms.Count; i++)
        {
            if (i > 0)
                parts.Add(separator);

            parts.Add(terms[i]);
        }

        return MathRow(parts.ToArray());
    }

    // Bouwt de compacte (n boven r)-notatie.
    private static string BinomialCoefficientToMathMl(string n, string r)
        => MathRow("<mo>(</mo>", $"<mfrac linethickness=\"0\">{n}{r}</mfrac>", "<mo>)</mo>");

    // Laat n-r numeriek vereenvoudigd zien wanneer beide argumenten getallen zijn.
    private static string DifferenceToMathMl(string leftExpression, string rightExpression)
    {
        if (TryParseInvariantDecimal(leftExpression, out var left) &&
            TryParseInvariantDecimal(rightExpression, out var right))
        {
            return NumberToMathMl(FormatTraceValue(left - right));
        }

        return MathRow(ExpressionToMathMl(leftExpression), "<mo>-</mo>", ExpressionToMathMl(rightExpression));
    }

    private static bool TryParseInvariantDecimal(string expression, out decimal value)
        => decimal.TryParse(
            expression.Trim().Replace(',', '.'),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out value);

    private static bool IsCombinationFunctionName(string text)
        => text.Equals("comb", StringComparison.OrdinalIgnoreCase)
            || text.Equals("ncr", StringComparison.OrdinalIgnoreCase)
            || text.Equals("choose", StringComparison.OrdinalIgnoreCase);

    private static bool IsPermutationFunctionName(string text)
        => text.Equals("perm", StringComparison.OrdinalIgnoreCase)
            || text.Equals("npr", StringComparison.OrdinalIgnoreCase);

    private static bool IsExpectedFunctionName(string text)
        => text.Equals("expected", StringComparison.OrdinalIgnoreCase)
            || text.Equals("expect", StringComparison.OrdinalIgnoreCase);

    // Splitst functie-argumenten zonder komma's binnen haakjes mee te nemen.
    private static IReadOnlyList<string> SplitTopLevelArguments(string text)
    {
        var result = new List<string>();
        var depth = 0;
        var start = 0;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '(')
            {
                depth++;
            }
            else if (ch == ')')
            {
                depth--;
            }
            else if (ch == ',' && depth == 0)
            {
                result.Add(text[start..i].Trim());
                start = i + 1;
            }
        }

        result.Add(text[start..].Trim());
        return result;
    }

    // Zoekt een operator op het hoogste niveau, dus niet binnen haakjes.
    private static int FindTopLevelOperator(string expression, char op)
    {
        var depth = 0;
        for (var i = expression.Length - 1; i >= 0; i--)
        {
            var ch = expression[i];
            if (ch == ')') depth++;
            else if (ch == '(') depth--;
            else if (depth == 0 && ch == op)
            {
                if ((op == '+' || op == '-') && i == 0)
                    continue;

                return i;
            }
        }

        return -1;
    }

    // Controleert of de volledige expressie tussen één buitenste haakjespaar staat.
    private static bool IsWrappedInParentheses(string expression)
    {
        if (expression.Length < 2 || expression[0] != '(' || expression[^1] != ')')
            return false;

        var depth = 0;
        for (var i = 0; i < expression.Length; i++)
        {
            if (expression[i] == '(') depth++;
            if (expression[i] == ')') depth--;
            if (depth == 0 && i < expression.Length - 1)
                return false;
        }

        return true;
    }

    // Maakt een leesbare tekstbeschrijving van de formule.
    private static string DescribeFormula(NodDocument doc)
    {
        if (doc.LegacyMathSteps.Count > 0)
            return string.Join(" \u21D2 ", doc.LegacyMathSteps.Select(step => PrettyFormula($"ans {step.Operator} {step.Number}")));

        if (doc.MathExpressions20.Count > 0)
            return string.Join(" \u21D2 ", doc.MathExpressions20.Select(PrettyFormula));

        if (doc.CalculusSteps.Count > 0)
            return string.Join(" \u21D2 ", doc.CalculusSteps.Select(step => PrettyFormula($"{step.Operation} {step.Expression}")));

        if (doc.Equation is not null && !string.IsNullOrWhiteSpace(doc.Equation.EquationText))
            return string.IsNullOrWhiteSpace(doc.Equation.SolveVariable)
                ? PrettyFormula(doc.Equation.EquationText)
                : $"{PrettyFormula(doc.Equation.EquationText)}; solve {doc.Equation.SolveVariable}";

        if (doc.ChangeRules.Count > 0)
            return string.Join(" \u21D2 ", doc.ChangeRules.Select(rule => $"chg {rule.OldPrefix} \u2192 {rule.NewPrefix}"));

        if (doc.TranslateRules.Count > 0)
            return string.Join(" \u21D2 ", doc.TranslateRules.Select(rule => $"trans {rule.OldValue} \u2192 {rule.NewValue}"));

        return "Direct";
    }

    // Maakt een leesbare tekstbeschrijving van de berekening.
    private static string DescribeCalculation(NodDocument doc, string input, string output)
    {
        if (doc.LegacyMathSteps.Count > 0 || doc.MathExpressions20.Count > 0 || doc.CalculusSteps.Count > 0)
        {
            var trace = NodEngine.ConvertForwardWithTrace(doc, input);
            return string.Join(" \u21D2 ", trace.Trace.Steps.Select(DescribeTraceStep));
        }

        if (doc.ChangeRules.Count > 0)
            return DescribeChangeCalculation(doc.ChangeRules, input, output);

        if (doc.TranslateRules.Count > 0)
            return DescribeTranslateCalculation(doc.TranslateRules, input, output);

        if (doc.Equation is not null && !string.IsNullOrWhiteSpace(doc.Equation.EquationText))
            return output;

        return PrettyFormula($"{input} = {output}");
    }

    // Maakt een leesbare beschrijving van één rekenstap uit de trace.
    private static string DescribeTraceStep(CalculationTraceStep step)
    {
        var expression = Regex.Replace(
            step.Expression,
            @"\bans\b",
            FormatTraceValue(step.InputValue),
            RegexOptions.IgnoreCase);

        return PrettyFormula($"{expression} = {FormatTraceValue(step.OutputValue)}");
    }

    // Format een trace-getal volgens de huidige Windows/regio-instelling.
    private static string FormatTraceValue(decimal value)
    {
        // Regionale notatie volgen:
        // nl-NL: 1,8 / 37,8 / 69,8
        // en-US: 1.8 / 37.8 / 69.8
        return value.ToString("0.############################", System.Globalization.CultureInfo.CurrentCulture);
    }
    // Zet getallen in resultaattekst om naar de huidige regio-instelling.
    private static string FormatResultTextForCurrentCulture(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text ?? string.Empty;

        return System.Text.RegularExpressions.Regex.Replace(
            text,
            @"[-+]?\d+(?:[.,]\d+)?",
            match =>
            {
                var raw = match.Value;

                var separatorIndex = Math.Max(raw.LastIndexOf('.'), raw.LastIndexOf(','));
                var decimals = separatorIndex >= 0
                    ? raw.Length - separatorIndex - 1
                    : 0;

                var normalized = raw.Replace(',', '.');

                if (!decimal.TryParse(
                        normalized,
                        System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var value))
                {
                    return raw;
                }

                var format = decimals > 0
                    ? "0." + new string('0', decimals)
                    : "0";

                return value.ToString(
                    format,
                    System.Globalization.CultureInfo.CurrentCulture);
            });
    }
    // Maakt formuletekst mooier met pijlen, vermenigvuldiging, deling, pi en superscript.
    private static string PrettyFormula(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var pretty = text
            .Replace("->", "\u2192")
            .Replace("*", "\u00D7")
            .Replace("/", "\u00F7");

        pretty = Regex.Replace(pretty, @"\bpi\b", "\u03C0", RegexOptions.IgnoreCase);
        pretty = Regex.Replace(pretty, @"\bsqrt\s*\(([^()]*)\)", "\u221A($1)", RegexOptions.IgnoreCase);
        pretty = Regex.Replace(pretty, @"\bans\b", "x", RegexOptions.IgnoreCase);
        pretty = Regex.Replace(pretty, @"\^(-?\d+)", match => ToSuperscript(match.Groups[1].Value));

        return pretty;
    }

    // Zet gewone cijfers om naar superscript-cijfers.
    private static string ToSuperscript(string value)
    {
        return string.Concat(value.Select(ch => ch switch
        {
            '-' => '\u207B',
            '0' => '\u2070',
            '1' => '\u00B9',
            '2' => '\u00B2',
            '3' => '\u00B3',
            '4' => '\u2074',
            '5' => '\u2075',
            '6' => '\u2076',
            '7' => '\u2077',
            '8' => '\u2078',
            '9' => '\u2079',
            _ => ch,
        }));
    }

    // Beschrijft welke chg-regel is toegepast.
    private static string DescribeChangeCalculation(IReadOnlyList<ChangeRule> rules, string input, string output)
    {
        foreach (var rule in rules)
        {
            if (input.StartsWith(rule.OldPrefix, StringComparison.OrdinalIgnoreCase))
                return $"{input} \u2192 {output} (found: {rule.OldPrefix} \u2192 {rule.NewPrefix})";
        }

        return $"{input} \u2192 {output} (chg not found)";
    }

    // Beschrijft welke trans-regel is toegepast.
    private static string DescribeTranslateCalculation(IReadOnlyList<TranslateRule> rules, string input, string output)
    {
        foreach (var rule in rules)
        {
            if (input.Equals(rule.OldValue, StringComparison.OrdinalIgnoreCase))
                return $"{input} \u2192 {output} (found: {rule.OldValue} \u2192 {rule.NewValue})";
        }

        return $"{input} \u2192 {output} (trans not found)";
    }

    // Menu/toolbar actie: zet de huidige tab terug naar de laatst opgeslagen/geopende tekst.
    private void RestoreOriginal_Click(object? sender, EventArgs e)
    {
        var tab = CurrentTab;
        if (tab is null || string.Equals(tab.Editor.Text, tab.CleanText, StringComparison.Ordinal))
            return;

        RestoreEditorText(tab, tab.CleanText, markClean: true);
        SetStatus(T("editor.status.original_restored", "Oorspronkelijke tekst hersteld."));
    }

    private void RestoreEditorText(EditorTab tab, string text, bool markClean)
    {
        var editor = tab.Editor;
        var selectionStart = Math.Min(editor.SelectionStart, text.Length);

        RunEditorAntiShock(editor, suppressTextHistory: false, hideTip: true, () =>
        {
            editor.Text = text;
            editor.Select(selectionStart, 0);
            tab.HistoryText = text;
            HighlightSyntax(tab);
        });

        tab.UndoTextStack.Clear();
        tab.RedoTextStack.Clear();
        if (markClean)
            tab.CleanText = text;

        SetTabDirty(tab, !markClean && !IsCleanEditorText(tab));
        UpdateLineNumbers(tab);
        UpdatePreviewFromCurrentText();
        UpdateUiState();
    }

    // Menu actie: repareert samengeplakte of rommelige NOD-regels.
    private void RepairLines_Click(object? sender, EventArgs e)
    {
        var tab = CurrentTab;
        if (tab is null) return;

        NormalizeEditorText(tab, repairConcatenated: true);
        SetTabDirty(tab, !IsCleanEditorText(tab));
        SetStatus(T("editor.status.lines_repaired", "Lines repaired."));
    }

    // Menu/toolbar actie: opent een los solver-stappenvenster voor equation/diff/integral.
    private void SolverSteps_Click(object? sender, EventArgs e)
    {
        var tab = CurrentTab;
        if (tab is null) return;

        try
        {
            NormalizeEditorText(tab, repairConcatenated: true);
            var normalized = NodTextNormalizer.Normalize(tab.Editor.Text, repairConcatenated: true);
            var doc = NodParser.Parse(normalized);
            if (!IsSolverDocument(normalized, doc))
                throw new InvalidOperationException(T(
                    "editor.solver_steps.only_solve",
                    "Solver stappen zijn alleen voor 'solve diff', 'solve integral' of een equation met solve. Gebruik gewone math-regels via Converter test."));

            ShowSolverStepsDialog(tab.Editor.Text);
            SetStatus(T("editor.status.solver_steps_opened", "Solver steps opened."));
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(T("status.error", "Error: {0}"), ex.Message));
            MessageBox.Show(this, ex.Message, T("editor.dialog.solver_steps_error", "Solver steps error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowSolverStepsDialog(string nodText)
    {
        using var form = new SolverStepsForm(nodText, _testInput.Text, _language)
        {
            TopMost = TopMost
        };
        form.ShowDialog(this);
    }

    private static bool IsSolverDocument(string normalizedText, NodDocument doc)
    {
        if (doc.ChangeRules.Count > 0 || doc.TranslateRules.Count > 0)
            return false;

        return HasExplicitSolverCommand(normalizedText) ||
               (doc.Equation is not null && !string.IsNullOrWhiteSpace(doc.Equation.SolveVariable));
    }

    private static bool HasExplicitSolverCommand(string normalizedText)
    {
        foreach (var rawLine in normalizedText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("'", StringComparison.Ordinal))
                continue;

            var space = line.IndexOfAny([' ', '\t']);
            if (space <= 0)
                continue;

            var keyword = line[..space].Trim();
            if (!keyword.Equals("solve", StringComparison.OrdinalIgnoreCase))
                continue;

            var value = line[(space + 1)..].TrimStart();
            return value.StartsWith("diff ", StringComparison.OrdinalIgnoreCase) ||
                   value.StartsWith("derivative ", StringComparison.OrdinalIgnoreCase) ||
                   value.StartsWith("integral ", StringComparison.OrdinalIgnoreCase) ||
                   value.StartsWith("limit ", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    // Opent het zoek/vervang-venster voor de actieve editor.
    private void ShowFindReplace()
    {
        var form = new FindReplaceForm(() => CurrentEditor);
        form.TopMost = TopMost;
        form.Show(this);
    }



    // Koppelt drag-events zodat de simulator losgesleept kan worden.
    private void AttachSimulatorDrag(Control control)
    {
        control.MouseDown += SimulatorDrag_MouseDown;
        control.MouseMove += SimulatorDrag_MouseMove;
        control.MouseUp += SimulatorDrag_MouseUp;
    }

    // Start het slepen van de gedockte simulator.
    private void SimulatorDrag_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _simulatorIsFloating)
            return;

        _simulatorDragStarted = true;
        _simulatorDragStartScreen = ((Control)sender!).PointToScreen(e.Location);
    }

    // Maakt de simulator floating zodra er ver genoeg gesleept is.
    private void SimulatorDrag_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_simulatorDragStarted || sender is not Control control)
            return;

        var current = control.PointToScreen(e.Location);
        var dx = Math.Abs(current.X - _simulatorDragStartScreen.X);
        var dy = Math.Abs(current.Y - _simulatorDragStartScreen.Y);

        if (dx > 14 || dy > 14)
        {
            _simulatorDragStarted = false;
            UndockSimulatorPreview(current);
        }
    }

    // Stopt de drag-status van de simulator.
    private void SimulatorDrag_MouseUp(object? sender, MouseEventArgs e)
    {
        _simulatorDragStarted = false;
    }

    // Maakt de simulator los op een standaardpositie.
    private void UndockSimulatorPreview()
    {
        var location = PointToScreen(new Point(Math.Max(80, Width - 560), 120));
        UndockSimulatorPreview(location);
    }

    private void UndockTestPanel()
    {
        if (_floatingTestForm is { IsDisposed: false })
        {
            _floatingTestForm.BringToFront();
            return;
        }

        if (_testGroup is null || _bottomPanel is null)
            return;

        _testGroup.Parent?.Controls.Remove(_testGroup);
        _testGroup.Dock = DockStyle.Fill;
        _testGroup.Visible = true;

        _floatingTestForm = new FloatingToolForm(
            T("editor.test.title", "Converter test"),
            _testGroup,
            onClose: () =>
            {
                if (_suppressFloatingCloseHandler)
                    return;

                _floatingTestForm = null;
                DockTestPanel(showPanel: false);
            },
            onMinimize: () =>
            {
                _floatingTestForm = null;
                DockTestPanel(showPanel: true);
            })
        {
            Width = 470,
            Height = 330,
            Location = PointToScreen(new Point(80, 120)),
            TopMost = TopMost
        };

        _floatingTestForm.Show(this);
        UpdateBottomPanelVisibility();
        UpdateViewMenuChecks();
    }

    private void DockTestPanel(bool showPanel)
    {
        if (_testGroup is null || _bottomPanel is null)
            return;

        if (_solverPreviewActive && showPanel)
        {
            SetStatus(T("editor.status.solver_hides_converter_panels", "Solver gebruikt de echte stapweergave; Converter test en Simulator zijn verborgen."));
            return;
        }

        if (_floatingTestForm is not null)
        {
            var form = _floatingTestForm;
            _floatingTestForm = null;

            _suppressFloatingCloseHandler = true;
            form.Controls.Remove(_testGroup);
            form.Close();
            _suppressFloatingCloseHandler = false;
        }

        if (!_bottomPanel.Controls.Contains(_testGroup))
            _bottomPanel.Controls.Add(_testGroup, 0, 0);

        _testGroup.Dock = DockStyle.Left;
        _testGroup.Width = DockedTestPanelWidth;
        _testGroup.Visible = showPanel;
        ApplyGraph3DFocusLayout();
        UpdateBottomPanelVisibility();
        UpdateViewMenuChecks();
    }

    private void UndockLivePreview()
    {
        if (_floatingPreviewForm is { IsDisposed: false })
        {
            _floatingPreviewForm.BringToFront();
            return;
        }

        if (_metadataPreviewGroup is null || _previewSplit is null)
            return;

        _metadataPreviewGroup.Parent?.Controls.Remove(_metadataPreviewGroup);
        _metadataPreviewGroup.Dock = DockStyle.Fill;
        _metadataPreviewGroup.Visible = true;

        _floatingPreviewForm = new FloatingToolForm(
            T("editor.preview.title", "Live preview"),
            _metadataPreviewGroup,
            onClose: () =>
            {
                if (_suppressFloatingCloseHandler)
                    return;

                _floatingPreviewForm = null;
                DockLivePreview(showPanel: false);
            },
            onMinimize: () =>
            {
                _floatingPreviewForm = null;
                DockLivePreview(showPanel: true);
            })
        {
            Width = 340,
            Height = 360,
            Location = PointToScreen(new Point(Math.Max(80, Width - 720), 120)),
            TopMost = TopMost
        };

        _floatingPreviewForm.Show(this);
        UpdateBottomPanelVisibility();
        UpdateViewMenuChecks();
    }

    private void DockLivePreview(bool showPanel)
    {
        if (_metadataPreviewGroup is null || _previewSplit is null)
            return;

        if (_floatingPreviewForm is not null)
        {
            var form = _floatingPreviewForm;
            _floatingPreviewForm = null;

            _suppressFloatingCloseHandler = true;
            form.Controls.Remove(_metadataPreviewGroup);
            form.Close();
            _suppressFloatingCloseHandler = false;
        }

        if (!_previewSplit.Panel1.Controls.Contains(_metadataPreviewGroup))
            _previewSplit.Panel1.Controls.Add(_metadataPreviewGroup);

        _previewSplit.Panel1Collapsed = false;
        _metadataPreviewGroup.Dock = DockStyle.None;
        _metadataPreviewGroup.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _metadataPreviewGroup.Visible = showPanel;
        AdjustLivePreviewWidth();
        UpdateBottomPanelVisibility();
        UpdateViewMenuChecks();
    }

    // Maakt de simulator los bij de opgegeven schermpositie.
    private void UndockSimulatorPreview(Point screenLocation)
    {
        if (_simulatorIsFloating || _simulatorPreviewGroup is null)
            return;

        _simulatorPreviewGroup.Parent?.Controls.Remove(_simulatorPreviewGroup);
        _previewSplit.Panel2Collapsed = false;
        _simulatorPreviewGroup.Dock = DockStyle.Fill;
        _simulatorPreviewGroup.Visible = true;

        _floatingSimulatorForm = new FloatingToolForm(
            "Simulator preview",
            _simulatorPreviewGroup,
            onClose: () =>
            {
                if (_suppressFloatingCloseHandler)
                    return;

                _floatingSimulatorForm = null;
                _simulatorIsFloating = false;
                DockSimulatorPreview(showPanel: false);
            },
            onMinimize: () =>
            {
                _floatingSimulatorForm = null;
                _simulatorIsFloating = false;
                DockSimulatorPreview(showPanel: true);
            });

        _floatingSimulatorForm.Location = new Point(screenLocation.X + 12, screenLocation.Y + 12);
        _floatingSimulatorForm.TopMost = TopMost;
        _simulatorIsFloating = true;
        _floatingSimulatorForm.Show(this);
        _floatingSimulatorForm.BringToFront();
        UpdateViewMenuChecks();
    }

    // Zet de floating simulator weer terug in het hoofdvenster.
    private void DockSimulatorPreview(bool showPanel)
    {
        if (_simulatorPreviewGroup is null)
            return;

        if (_solverPreviewActive && showPanel)
        {
            SetStatus(T("editor.status.solver_hides_converter_panels", "Solver gebruikt de echte stapweergave; Converter test en Simulator zijn verborgen."));
            return;
        }

        if (_floatingSimulatorForm is not null)
        {
            var form = _floatingSimulatorForm;
            _floatingSimulatorForm = null;

            _suppressFloatingCloseHandler = true;
            form.Controls.Remove(_simulatorPreviewGroup);
            form.Close();
            _suppressFloatingCloseHandler = false;
        }

        if (!_previewSplit.Panel2.Controls.Contains(_simulatorPreviewGroup))
            _previewSplit.Panel2.Controls.Add(_simulatorPreviewGroup);

        _simulatorPreviewGroup.Dock = DockStyle.Fill;
        _previewSplit.Panel2Collapsed = false;
        _simulatorPreviewGroup.Visible = showPanel;
        _simulatorIsFloating = false;
        AdjustPreviewSplitter();
        UpdateBottomPanelVisibility();
        UpdateViewMenuChecks();
    }

    // Toont of verbergt de live preview.
    private void ToggleLivePreview()
    {
        if (_previewSplit is null)
            return;

        if (_floatingPreviewForm is not null)
        {
            _floatingPreviewForm.BringToFront();
            return;
        }

        _mainSplit.Panel2Collapsed = false;
        _previewSplit.Panel1Collapsed = false;
        _metadataPreviewGroup.Visible = !_metadataPreviewGroup.Visible;
        AdjustPreviewSplitter();
        UpdateBottomPanelVisibility();
        UpdateViewMenuChecks();
    }

    // Toont of verbergt de simulator preview.
    private void ToggleSimulatorPreview()
    {
        if (_previewSplit is null)
            return;

        if (_solverPreviewActive)
        {
            SetStatus(T("editor.status.solver_hides_converter_panels", "Solver gebruikt de echte stapweergave; Converter test en Simulator zijn verborgen."));
            return;
        }

        if (_floatingSimulatorForm is not null)
        {
            _floatingSimulatorForm.BringToFront();
            return;
        }

        _mainSplit.Panel2Collapsed = false;
        _previewSplit.Panel2Collapsed = false;
        _simulatorPreviewGroup.Visible = !_simulatorPreviewGroup.Visible;
        AdjustPreviewSplitter();
        UpdateBottomPanelVisibility();
        UpdateViewMenuChecks();
    }

    // Toont de introductietekst uit de NOD in een apart venster.
    private void ShowIntroPreview()
    {
        var editor = CurrentEditor;
        if (editor is null)
            return;

        var meta = NodUiMetadata.Parse(NodTextNormalizer.Normalize(editor.Text, repairConcatenated: true));
        var title = string.IsNullOrWhiteSpace(meta.Urln) ? meta.Name : meta.Urln;

        if (meta.IntroLines.Count == 0)
        {
            MessageBox.Show(this, T("editor.dialog.no_intro", "No introduction text found."), T("editor.menu.view.intro_preview", "Introduction preview"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new IntroDialogForm(title, meta.IntroLines);
        dialog.TopMost = TopMost;
        dialog.ShowDialog(this);
    }

    // Toont of verbergt het testpaneel.
    private void ToggleTestPanel()
    {
        if (_testGroup is null)
            return;

        if (_solverPreviewActive)
        {
            SetStatus(T("editor.status.solver_hides_converter_panels", "Solver gebruikt de echte stapweergave; Converter test en Simulator zijn verborgen."));
            return;
        }

        if (_floatingTestForm is not null)
        {
            _floatingTestForm.BringToFront();
            return;
        }

        _mainSplit.Panel2Collapsed = false;
        _testGroup.Visible = !_testGroup.Visible;
        UpdateBottomPanelVisibility();
        UpdateViewMenuChecks();
    }

    private void UpdateBottomPanelVisibility()
    {
        if (_mainSplit is null || _testGroup is null || _metadataPreviewGroup is null || _simulatorPreviewGroup is null)
            return;

        var hasDockedPanel =
            (_bottomPanel?.Controls.Contains(_testGroup) == true && _testGroup.Visible) ||
            (_previewSplit.Panel1.Controls.Contains(_metadataPreviewGroup) && _metadataPreviewGroup.Visible) ||
            (_previewSplit.Panel2.Controls.Contains(_simulatorPreviewGroup) && _simulatorPreviewGroup.Visible);

        _mainSplit.Panel2Collapsed = !hasDockedPanel;

        if (hasDockedPanel)
        {
            AdjustPreviewSplitter();
            AdjustMainSplitter();
        }
    }

    private void RestoreDefaultPanels()
    {
        if (_testGroup is null || _metadataPreviewGroup is null || _simulatorPreviewGroup is null)
            return;

        if (_mainSplit is not null)
            _mainSplit.Panel2Collapsed = false;

        if (_previewSplit is not null)
        {
            _previewSplit.Panel1Collapsed = false;
            _previewSplit.Panel2Collapsed = false;
        }

        DockTestPanel(showPanel: true);
        DockLivePreview(showPanel: true);
        DockSimulatorPreview(showPanel: true);

        UpdateBottomPanelVisibility();
        UpdateViewMenuChecks();
    }

    private void ResizeSimulatorFileBox(Control? toolbarPanel)
    {
        if (_simFileBox is null || toolbarPanel is null)
            return;

        var rightMargin = 8;
        var available = toolbarPanel.ClientSize.Width - _simFileBox.Left - rightMargin;
        var desired = _simFileBox.Items.Count > 0
            ? TextRenderer.MeasureText(_simFileBox.Items[0]?.ToString() ?? "", _simFileBox.Font).Width + 34
            : _simFileBox.Width;

        _simFileBox.Width = Math.Clamp(Math.Max(available, 120), 120, Math.Max(120, Math.Min(available, desired)));
        _simFileBox.DropDownWidth = Math.Max(_simFileBox.Width, desired);
    }

    // Werkt de regelnummers naast de editor bij.
    private void UpdateLineNumbers(EditorTab tab)
    {
        var count = Math.Max(1, tab.Editor.Lines.Length);
        if (tab.LastLineNumberCount == count)
            return;

        tab.LastLineNumberCount = count;
        tab.LineNumbers.Text = string.Join(Environment.NewLine, Enumerable.Range(1, count).Select(i => i.ToString()));
    }

    private static bool IsCleanEditorText(EditorTab tab)
    {
        return string.Equals(
            NormalizeDirtyComparisonText(tab.Editor.Text),
            NormalizeDirtyComparisonText(tab.CleanText),
            StringComparison.Ordinal);
    }

    private static string NormalizeDirtyComparisonText(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }

    // Werkt de tabtitel bij, inclusief sterretje bij niet-opgeslagen wijzigingen.
    private void SetTabDirty(EditorTab tab, bool dirty)
    {
        if (tab.Dirty == dirty)
            return;

        tab.Dirty = dirty;
        UpdateTabTitle(tab);
        UpdateSaveButtonState();
    }

    private void UpdateTabTitle(EditorTab tab)
    {
        var title = tab.Path is null ? "nieuw" : Path.GetFileName(tab.Path);
        tab.Page.Text = title + (tab.Dirty ? " *" : "");
        _tabControl.Invalidate();
        RefreshEditorTabStrip();
    }

    // Toont NOD-keyword hulp terwijl je aan het begin van een regel typt.
    private void UpdateNodKeywordTip(RichTextBox editor)
    {
        if (_highlighting || _applyingTextHistory || _suppressNodHelpUpdates)
            return;

        if (!editor.Focused || editor.SelectionLength > 0)
        {
            HideNodHelpPopup();
            return;
        }

        if (TryGetKeywordAtCaret(editor, out var caretKeyword))
        {
            var exactAtCaret = NodKeywords.FirstOrDefault(key => key.Equals(caretKeyword, StringComparison.OrdinalIgnoreCase));
            if (exactAtCaret is not null)
            {
                _nodHelpTargetEditor = editor;
                SetNodHelpHtml(BuildNodHelpHtml(exactAtCaret, Array.Empty<string>()));
                PlaceNodHelpPopup(editor.GetPositionFromCharIndex(editor.SelectionStart));
                return;
            }
        }

        var prefix = GetCurrentLineKeywordPrefix(editor, out var hasTrailingWhitespace);
        if (prefix.Length < 2)
        {
            HideNodHelpPopup();
            return;
        }

        var exact = NodKeywords.FirstOrDefault(key => key.Equals(prefix, StringComparison.OrdinalIgnoreCase));
        if (exact is not null || hasTrailingWhitespace)
        {
            HideNodHelpPopup();
            return;
        }

        var existingKeywords = GetExistingNodKeywords(editor);
        var matches = NodKeywords
            .Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Where(key => ShouldSuggestNodKeyword(key, existingKeywords))
            .OrderBy(key => key)
            .Take(6)
            .ToList();

        if (matches.Count == 0)
        {
            HideNodHelpPopup();
            return;
        }

        _nodHelpTargetEditor = editor;
        SetNodHelpHtml(BuildNodHelpHtml(exact, matches));

        var editorPoint = editor.GetPositionFromCharIndex(editor.SelectionStart);
        PlaceNodHelpPopup(editorPoint);
    }

    // Plaatst het kleine NOD-helpvenster bij een editorpunt en houdt het binnen het formulier.
    private void PlaceNodHelpPopup(Point editorPoint)
    {
        var editor = _nodHelpTargetEditor ?? CurrentEditor;
        if (editor is null)
            return;

        editorPoint.Offset(2, 8);
        var screenPoint = editor.PointToScreen(editorPoint);
        var formPoint = PointToClient(screenPoint);
        formPoint.X = Math.Min(Math.Max(8, formPoint.X), Math.Max(8, ClientSize.Width - _nodHelpPopup.Width - 8));
        formPoint.Y = Math.Min(Math.Max(8, formPoint.Y), Math.Max(8, ClientSize.Height - _nodHelpPopup.Height - 8));

        _nodHelpPopup.Location = formPoint;
        _nodHelpPopup.Visible = true;
        _nodHelpPopup.BringToFront();
    }

    private void KeepNodHelpPopupInBounds()
    {
        _nodHelpPopup.Left = Math.Min(Math.Max(8, _nodHelpPopup.Left), Math.Max(8, ClientSize.Width - _nodHelpPopup.Width - 8));
        _nodHelpPopup.Top = Math.Min(Math.Max(8, _nodHelpPopup.Top), Math.Max(8, ClientSize.Height - _nodHelpPopup.Height - 8));
    }

    // Verbergt het HTML-helpvenster voor NOD-keywords.
    private void HideNodHelpPopup()
    {
        if (_nodHelpPopup is not null)
            _nodHelpPopup.Visible = false;
    }

    // Zet popup-HTML klaar of toont die direct zodra WebView2 beschikbaar is.
    private void SetNodHelpHtml(string html)
    {
        if (html == _lastRenderedNodHelpHtml && _nodHelpPopup.Visible)
            return;

        _pendingNodHelpHtml = html;
        ShowPendingNodHelpHtmlIfReady();
    }

    // Toont uitgestelde popup-HTML in WebView2.
    private void ShowPendingNodHelpHtmlIfReady()
    {
        if (_nodHelpBrowserFailed || IsDisposed || _nodHelpBrowser.IsDisposed || _pendingNodHelpHtml is null || _nodHelpBrowser.CoreWebView2 is null)
            return;

        var html = _pendingNodHelpHtml;
        _pendingNodHelpHtml = null;
        try
        {
            _nodHelpPopup.Width = _pendingNodHelpWidth;
            _nodHelpPopup.Height = NodHelpPopupMinHeight;
            _lastRenderedNodHelpHtml = html;
            _nodHelpBrowser.NavigateToString(html);
        }
        catch (COMException)
        {
            _nodHelpBrowserFailed = true;
        }
        catch (ObjectDisposedException)
        {
            _nodHelpBrowserFailed = true;
        }
    }

    // Verwerkt kliklinks in de HTML-help, bijvoorbeeld command invoegen.
    private void NodHelpBrowser_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
            return;

        if (uri.Scheme.Equals("nodinsert", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            var keyword = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'));
            InsertNodCommandTemplate(keyword);
            return;
        }

        if (uri.Scheme.Equals("nodhelp", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            ShowNodHelpDeferred(_lastNodHelpKeyword is null ? null : "cmd:" + _lastNodHelpKeyword.ToLowerInvariant());
            return;
        }

        if (uri.Scheme.Equals("nodlanginfo", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            ShowSignedPackageInformationDeferred();
            return;
        }

        if (uri.Scheme.Equals("nodpage", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            var pageId = Uri.UnescapeDataString((uri.Host + uri.AbsolutePath).Trim('/'));
            ShowNodHelpDeferred(pageId);
        }
    }

    private void ShowSignedPackageInformationDeferred()
    {
        BeginInvoke(new Action(() =>
        {
            var information = BuildCurrentLanguageSignedPackageInformation();
            if (information is not null)
                HelpApi.ShowSignedPackageInformation(this, information, PreferredWebViewColorScheme);
        }));
    }

    // Opent de grote NOD-help pas nadat WebView2 klaar is met het klik-event.
    private void ShowNodHelpDeferred(string? selectedPageId)
    {
        if (IsDisposed)
            return;

        BeginInvoke(new Action(() =>
        {
            if (!IsDisposed)
                ShowNodHelp(selectedPageId);
        }));
    }

    // Voegt een NOD-command template in op de actieve editorregel.
    private void InsertNodCommandTemplate(string keyword)
    {
        var editor = _nodHelpTargetEditor ?? CurrentEditor;
        if (editor is null)
            return;

        var exact = NodKeywords.FirstOrDefault(key => key.Equals(keyword, StringComparison.OrdinalIgnoreCase));
        if (exact is null)
            return;

        var template = IsLegacyInputKeyword(exact)
            ? GetLegacyInputReplacementTemplate(editor, exact)
            : GetNodInsertTemplate(exact);
        ReplaceCurrentLineWithTemplate(editor, template);
        HideNodHelpPopup();
        editor.Focus();
    }

    private static string GetLegacyInputReplacementTemplate(RichTextBox editor, string keyword)
    {
        var label = GetCurrentLineRemainder(editor);
        if (keyword.Equals("input1", StringComparison.OrdinalIgnoreCase))
            return "input x " + (string.IsNullOrWhiteSpace(label) ? "Label" : label);

        return "inputr y " + (string.IsNullOrWhiteSpace(label) ? "Output label" : label);
    }

    private static string GetCurrentLineRemainder(RichTextBox editor)
    {
        var index = editor.SelectionStart;
        var line = editor.GetLineFromCharIndex(index);
        if (line < 0 || line >= editor.Lines.Length)
            return "";

        var text = editor.Lines[line].Trim();
        var match = Regex.Match(text, @"^\S+\s+(?<label>.+)$");
        return match.Success ? match.Groups["label"].Value.Trim() : "";
    }

    // Vervangt de huidige regel of getypte prefix door de gekozen template.
    private static void ReplaceCurrentLineWithTemplate(RichTextBox editor, string template)
    {
        var index = editor.SelectionStart;
        var line = editor.GetLineFromCharIndex(index);
        var lineStart = editor.GetFirstCharIndexFromLine(line);
        if (lineStart < 0)
            return;

        var nextLineStart = line + 1 < editor.Lines.Length
            ? editor.GetFirstCharIndexFromLine(line + 1)
            : editor.TextLength;
        var lineEnd = nextLineStart;
        while (lineEnd > lineStart && (editor.Text[lineEnd - 1] == '\r' || editor.Text[lineEnd - 1] == '\n'))
            lineEnd--;

        editor.Select(lineStart, Math.Max(0, lineEnd - lineStart));
        editor.SelectedText = template;
        editor.SelectionStart = lineStart + template.Length;
        editor.SelectionLength = 0;
    }

    // Haalt de insert-template uit taalbestanden, met fallback per NOD-command.
    private string GetNodInsertTemplate(string keyword)
    {
        var key = keyword.ToLowerInvariant();
        var fallback = key switch
        {
            "name" => "Name ",
            "urln" => "URLN ",
            "input" => "input x Label",
            "inputr" => "inputr y Output label",
            "input1" => "input x Label",
            "input2" => "inputr y Output label",
            "result" => "Result result:",
            "resfou" => "Resfou wrong or BAD number",
            "symb1" => "Symb1 ",
            "symb2" => "Symb2 ",
            "symb3" => "Symb3 ",
            "symb4" => "Symb4 ",
            "format" => "format ##.00",
            "math" => "math ans ",
            "chg" => "chg \"oude-prefix\",\"nieuwe-prefix\"",
            "trans" => "trans \"oude waarde\",\"nieuwe waarde\"",
            "reverse" => "reverse ans ",
            "field" => "field KolomNaam",
            "table" => "table TabelNaam",
            "output" => "output ResultaatKolom",
            "phoneformat" => "phoneformat country NL",
            "lookup" => "lookup TabelNaam",
            "match" => "match veld = waarde",
            "given" => "given x = 0",
            "equation" => "equation y = x",
            "solve" => "solve x",
            "constraint" => "constraint x >= 0",
            "preview" => "preview Example text",
            "backup" => "backup true",
            "indoprint" => "indoprint Introductietekst" + Environment.NewLine + "indoend",
            "indoend" => "indoend",
            "end" => "end",
            _ => keyword + " "
        };

        return T($"editor.nod_insert.{key}", fallback).Replace("\\n", Environment.NewLine);
    }

    // Toont de uitgebreide HTML-handleiding voor NOD.
    private void ShowNodHelp(string? selectedPageId = null)
    {
        HideNodHelpPopup();
        HelpApi.ShowDialog(this, new HelpDialogOptions(
            T("editor.menu.help.nod", "NOD help"),
            BuildNodHelpPages(),
            SelectedPageId: selectedPageId,
            Navigation: GetHelpNavigationLabels(),
            PreferredColorScheme: PreferredWebViewColorScheme,
            ShowSignedPackageBadge: BuildCurrentLanguageSignedPackageInformation() is not null,
            SignedPackageBadgeText: CurrentLanguagePackageIsSigned()
                ? T("help.signed_package_verified", "Signed package verified")
                : T("help.unsigned_language_file", "Unsigned language file"),
            SignedPackageInformation: BuildCurrentLanguageSignedPackageInformation()));
    }

    // Levert vertaalde labels voor de gedeelde helpnavigatie.
    private HelpNavigationLabels GetHelpNavigationLabels()
    {
        return new HelpNavigationLabels(
            T("help.nav.home", "Home"),
            T("help.nav.previous", "Previous"),
            T("help.nav.next", "Next"));
    }

    private bool CurrentLanguagePackageIsSigned()
    {
        return LanguageCatalog.ListAvailable(AppContext.BaseDirectory)
            .FirstOrDefault(language => language.Matches(_language.FileName, _language.PackageId))
            ?.Signed == true;
    }

    private HelpSignedPackageInformation? BuildCurrentLanguageSignedPackageInformation()
    {
        var language = LanguageCatalog.ListAvailable(AppContext.BaseDirectory)
            .FirstOrDefault(item => item.Matches(_language.FileName, _language.PackageId));
        if (language is null)
            return null;

        var signed = language.Signed;
        var code = string.IsNullOrWhiteSpace(language.LanguageCode)
            ? Path.GetFileNameWithoutExtension(language.FileName)
            : language.LanguageCode;
        var author = string.IsNullOrWhiteSpace(language.Producer)
            ? T("dialog.language.info.local_author", "Local file")
            : language.Producer;
        var product = string.IsNullOrWhiteSpace(language.Product) ? "Syscalculator" : language.Product;
        var packageId = string.IsNullOrWhiteSpace(language.PackageId) ? "-" : language.PackageId;
        var version = string.IsNullOrWhiteSpace(language.PackageVersion) ? "-" : language.PackageVersion;
        var algorithm = string.IsNullOrWhiteSpace(language.SignatureAlgorithm) ? "-" : language.SignatureAlgorithm;
        var keyId = string.IsNullOrWhiteSpace(language.SignatureKeyId) ? "-" : language.SignatureKeyId;
        var keySha256 = string.IsNullOrWhiteSpace(language.SignatureKeySha256) ? "-" : language.SignatureKeySha256;

        return new HelpSignedPackageInformation(
            T("dialog.language.info.title", "Language information"),
            language.DisplayName,
            signed
                ? T("help.signed_package_verified", "Signed package verified")
                : T("help.unsigned_language_file", "Unsigned language file"),
            [
                new(T("dialog.language.info.name", "Language"), language.DisplayName),
                new(T("dialog.language.info.code", "Code"), code),
                new(T("dialog.language.info.author", "Author"), author),
                new(T("dialog.language.info.product", "Product"), product),
                new(T("dialog.language.info.package", "Package"), packageId),
                new(T("dialog.language.info.file", "File"), language.FileName),
                new(T("dialog.language.info.version", "Version"), version),
                new(T("dialog.language.info.signed", "Signed"), signed ? T("common.yes", "Yes") : T("common.no", "No")),
                new(T("dialog.language.info.algorithm", "Algorithm"), algorithm),
                new(T("dialog.language.info.key", "Key"), keyId),
                new("SHA-256", keySha256)
            ],
            signed
                ? T("help.signed_package_tip", "This help comes from a verified signed language package.")
                : T("help.unsigned_language_tip", "This help comes from a loose language file and is not signed."),
            signed);
    }

    private void FormulaCard_Click(object? sender, EventArgs e)
    {
        HideNodHelpPopup();
        using var form = new FormulaCardForm(_language, IsFormulaFilmExperimentUnlockCode());
        form.TopMost = TopMost;
        form.ShowDialog(this);
    }

    private bool IsFormulaFilmExperimentUnlockCode()
    {
        var text = CurrentEditor?.Text.Trim();
        return string.Equals(text, "expstart", StringComparison.OrdinalIgnoreCase);
    }

    // Bouwt losse pagina's voor de NOD help.
    private IReadOnlyList<NodHelpPage> BuildNodHelpPages()
    {
        var intro = HelpContent("editor.nod_help.full.intro", "nod/full/nod-intro.html");
        var guide = HelpContent(
            "editor.nod_help.full.guide",
            "nod/full/guide.html",
            new Dictionary<string, string?>
            {
                ["NodEditorScreenshot"] = BuildNodHelpImageTag(
                    "NodEditorHelp.svg",
                    T("help.main.page.nodeditor.screenshot_alt", "Screenshot of the NOD Editor with toolbar, code editor and command tip."))
            });
        var classic = HelpContent("editor.nod_help.full.classic_book", "nod/full/classic_book.html");
        var history = HelpContent("editor.nod_help.full.history", "nod/full/history.html");
        var structure = HelpContent("editor.nod_help.full.structure", "nod/full/structure.html");
        var workflow = HelpContent("editor.nod_help.full.workflow", "nod/full/workflow.html");
        var symbols = HelpContent("editor.nod_help.full.symbols", "nod/full/symbols.html");
        var graphPreview = HelpContent("editor.nod_help.full.graph_preview", "nod/full/graph-preview.html");
        var siScale = HelpContent("editor.nod_help.full.si_scale", "nod/full/si-scale.html");
        var commands = HelpContent("editor.nod_help.full.commands", "nod/full/commands.html");
        var examples = HelpContent("editor.nod_help.full.examples", "nod/full/examples.html");
        var troubleshooting = HelpContent("editor.nod_help.full.troubleshooting", "nod/full/troubleshooting.html");

        var pages = new List<NodHelpPage>
        {
            new("intro", T("editor.nod_help.page.intro", "NOD introductie"), WrapNodHelpPage(T("editor.nod_help.page.intro", "NOD introductie"), intro)),
            new("guide", T("editor.nod_help.page.guide", "NOD Editor gebruiken"), WrapNodHelpPage(T("editor.nod_help.page.guide", "NOD Editor gebruiken"), guide)),
            new("classic", T("editor.nod_help.page.classic", "Van klassiek NOD naar NOD 2.0"), WrapNodHelpPage(T("editor.nod_help.page.classic", "Van klassiek NOD naar NOD 2.0"), classic)),
            new("history", T("editor.nod_help.page.history", "Geschiedenis"), WrapNodHelpPage(T("editor.nod_help.page.history", "Geschiedenis"), history)),
            new("compatibility", T("editor.nod_help.page.compatibility", "Compatibiliteit"), WrapNodHelpPage(T("editor.nod_help.page.compatibility", "Compatibiliteit"), BuildCompatibilityHelp())),
            new("workflow", T("editor.nod_help.page.workflow", "Werkwijze"), WrapNodHelpPage(T("editor.nod_help.page.workflow", "Werkwijze"), workflow)),
            new("structure", T("editor.nod_help.page.structure", "Basisstructuur"), WrapNodHelpPage(T("editor.nod_help.page.structure", "Basisstructuur"), structure)),
            new("symbols", T("editor.nod_help.page.symbols", "Symbolen"), WrapNodHelpPage(T("editor.nod_help.page.symbols", "Symbolen"), symbols + BuildSymbolOverviewLinks())),
            new("graph-preview", T("editor.nod_help.page.graph_preview", "Graph 2D"), WrapNodHelpPage(T("editor.nod_help.page.graph_preview", "Graph 2D"), graphPreview)),
            new("si-scale", T("editor.nod_help.page.si_scale", "SI en schaalweergave"), WrapNodHelpPage(T("editor.nod_help.page.si_scale", "SI en schaalweergave"), siScale)),
            new("commands", T("editor.nod_help.page.commands", "Belangrijke commands"), WrapNodHelpPage(T("editor.nod_help.page.commands", "Belangrijke commands"), commands)),
            new("examples", T("editor.nod_help.page.examples", "Examples"), WrapNodHelpPage(T("editor.nod_help.page.examples", "Examples"), examples)),
            new("troubleshooting", T("editor.nod_help.page.troubleshooting", "Veelgemaakte fouten"), WrapNodHelpPage(T("editor.nod_help.page.troubleshooting", "Veelgemaakte fouten"), troubleshooting))
        };

        var addedCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in NodCommandGroups)
        {
            var groupTitle = T(group.TitleKey, group.FallbackTitle);
            var groupBody = HelpContent(group.ContentKey, group.ContentFile);
            pages.Add(new NodHelpPage(group.Id, groupTitle, WrapNodHelpPage(groupTitle, groupBody)));

            foreach (var keyword in group.Keywords)
            {
                AddNodCommandHelpPage(pages, keyword, groupTitle);
                addedCommands.Add(keyword);
            }
        }

        foreach (var keyword in NodKeywords)
        {
            if (addedCommands.Contains(keyword))
                continue;

            AddNodCommandHelpPage(pages, keyword, T("editor.nod_help.page.commands.other", "Overige commands"));
        }

        return pages;
    }

    private void AddNodCommandHelpPage(List<NodHelpPage> pages, string keyword, string groupTitle)
    {
        var commandTitle = T($"editor.nod_help.page.command.{keyword.ToLowerInvariant()}", keyword);
        var title = groupTitle + " / " + commandTitle;
        var body = T($"editor.nod_help.{keyword.ToLowerInvariant()}", GetDefaultNodHelpHtml(keyword))
            + BuildInputCommandHelp(keyword)
            + BuildMathCommandHelp(keyword)
            + BuildVisualFormulaHelp(keyword)
            + BuildTextCommandHelp(keyword)
            + BuildImprovedTransHelp(keyword)
            + BuildReverseCommandHelp(keyword)
            + BuildModeCommandHelp(keyword)
            + BuildPhoneFormatCommandHelp(keyword)
            + BuildSymbolCommandDiagram(keyword);
        body = AddVersionBadgesToCommandHelp(keyword, body);
        pages.Add(new NodHelpPage("cmd:" + keyword.ToLowerInvariant(), title, WrapNodHelpPage(title, body)));
    }

    private string BuildCompatibilityHelp()
    {
        return HelpContent(
            "editor.nod_help.full.compatibility",
            "nod/full/compatibility.html",
            new Dictionary<string, string?> { ["WarningBoardSvg"] = WarningBoardSvg() });
    }

    private string BuildNodHelpImageTag(string fileName, string altText)
    {
        return HelpApi.ScreenshotImage(HelpLanguageCode(), ResolveHelpLanguageText, fileName, altText);
    }

    private string BuildInputCommandHelp(string keyword)
    {
        var key = keyword.ToLowerInvariant();
        if (key is not ("input" or "input1" or "input2" or "inputr"))
            return "";

        return HelpContent(
            "editor.nod_help.input.compatibility",
            "nod/command/input-compatibility.html",
            new Dictionary<string, string?> { ["WarningBoardSvg"] = WarningBoardSvg() });
    }

    private static string WarningBoardSvg()
    {
        return """
        <svg class="warning-board" viewBox="0 0 52 48" aria-hidden="true" focusable="false">
          <path d="M26 4 49 43H3L26 4Z" fill="#facc15" stroke="#b91c1c" stroke-width="4" stroke-linejoin="round"/>
          <path d="M26 17v13" stroke="#7f1d1d" stroke-width="5" stroke-linecap="round"/>
          <circle cx="26" cy="37" r="3" fill="#7f1d1d"/>
        </svg>
        """;
    }

    private static string AddVersionBadgesToCommandHelp(string keyword, string html)
    {
        if (html.Contains("version-badges", StringComparison.OrdinalIgnoreCase))
            return html;

        var key = keyword.ToLowerInvariant();
        if (key == "math")
            return AddVersionBadgesToSummaries(html, [BothVersionsBadge(), ModernVersionBadge(), ModernVersionBadge(), ModernVersionBadge(), ModernVersionBadge(), ModernVersionBadge(), ModernVersionBadge()]);

        if (key == "chg")
            return AddVersionBadgesToSummaries(html, [BothVersionsBadge(), ModernVersionBadge()]);

        if (key == "trans")
            return AddVersionBadgesToSummaries(html, [BothVersionsBadge(), BothVersionsBadge()]);

        if (key == "reverse")
            return AddVersionBadgesToSummaries(html, [ModernVersionBadge(), ModernVersionBadge(), ModernVersionBadge()]);

        if (key == "phoneformat")
            return AddVersionBadgesToSummaries(html, [ModernVersionBadge(), ModernVersionBadge(), ModernVersionBadge()]);

        return html;
    }

    private static string AddVersionBadgesToSummaries(string html, IReadOnlyList<string> badges)
    {
        var index = 0;
        return Regex.Replace(html, @"<summary>(.*?)</summary>", match =>
        {
            if (index >= badges.Count)
                return match.Value;

            var text = match.Groups[1].Value;
            var badge = badges[index++];
            return $"<summary>{text}{badge}</summary>";
        }, RegexOptions.Singleline | RegexOptions.IgnoreCase);
    }

    private static string BothVersionsBadge()
    {
        return """<span class="version-badges"><span class="version-badge">NOD 1.0</span><span class="version-badge modern">NOD 2.0</span></span>""";
    }

    private static string ModernVersionBadge()
    {
        return """<span class="version-badges"><span class="version-badge modern">NOD 2.0</span></span>""";
    }

    // Zoek/commentaar: Voegt visuele roosterkaarten toe voor formules die anders abstract blijven.
    private string BuildVisualFormulaHelp(string keyword)
    {
        if (keyword.Equals("math", StringComparison.OrdinalIgnoreCase))
            return "";

        if (keyword.Equals("equation", StringComparison.OrdinalIgnoreCase) ||
            keyword.Equals("solve", StringComparison.OrdinalIgnoreCase) ||
            keyword.Equals("given", StringComparison.OrdinalIgnoreCase))
        {
            return HelpContent("editor.nod_help.equation.visuals", "nod/command/equation-visuals.html");
        }

        return "";
    }

    private static string PlaceMathVisuals(string html)
    {
        if (html.Contains("graph-inline-circle", StringComparison.OrdinalIgnoreCase))
            return html;

        html = InsertBeforeFirst(html, "<pre>Name Cirkeloppervlak", CircleGraphInline());
        html = InsertBeforeFirst(html, "<pre>Name Integraal kwadraat", IntegralGraphInline());
        return html;
    }

    private static string InsertBeforeFirst(string html, string marker, string insertion)
    {
        var index = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return html;

        return html[..index] + insertion + html[index..];
    }

    private static string CircleGraphInline()
    {
        return """
        <div class="graph-inline graph-inline-circle">
          <svg class="graph-frame" viewBox="0 0 240 150" role="img" aria-label="Cirkel met straal op rooster">
            <defs>
              <pattern id="grid-circle" width="15" height="15" patternUnits="userSpaceOnUse">
                <path d="M 15 0 L 0 0 0 15" fill="none" stroke="#dbe7f7" stroke-width="1"/>
              </pattern>
            </defs>
            <rect width="240" height="150" fill="url(#grid-circle)"/>
            <line x1="120" y1="12" x2="120" y2="138" class="graph-axis"/>
            <line x1="24" y1="75" x2="216" y2="75" class="graph-axis"/>
            <circle cx="120" cy="75" r="46" class="graph-circle"/>
            <line x1="120" y1="75" x2="166" y2="75" class="graph-radius"/>
            <text x="140" y="67" class="graph-label">r</text>
          </svg>
          <p class="graph-caption">De invoer <code>ans</code> is de straal <code>r</code>. De formule berekent het oppervlak.</p>
        </div>
        """;
    }

    private static string IntegralGraphInline()
    {
        return """
        <div class="graph-inline graph-inline-integral">
          <div class="graph-mathml"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><msubsup><mo>&#x222B;</mo><mn>0</mn><mn>1</mn></msubsup><msup><mi>x</mi><mn>2</mn></msup><mtext>dx</mtext></mrow></math></div>
          <svg class="graph-frame" viewBox="0 0 240 150" role="img" aria-label="Oppervlakte onder x kwadraat van 0 tot 1">
            <defs>
              <pattern id="grid-integral" width="15" height="15" patternUnits="userSpaceOnUse">
                <path d="M 15 0 L 0 0 0 15" fill="none" stroke="#dbe7f7" stroke-width="1"/>
              </pattern>
              <linearGradient id="area-integral" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0" stop-color="#60a5fa" stop-opacity=".55"/>
                <stop offset="1" stop-color="#93c5fd" stop-opacity=".18"/>
              </linearGradient>
            </defs>
            <rect width="240" height="150" fill="url(#grid-integral)"/>
            <line x1="36" y1="124" x2="216" y2="124" class="graph-axis"/>
            <line x1="36" y1="18" x2="36" y2="124" class="graph-axis"/>
            <path d="M 36 124 C 86 122, 132 102, 188 38 L 188 124 Z" fill="url(#area-integral)" stroke="#60a5fa" stroke-width="1"/>
            <rect x="54" y="121" width="16" height="3" class="graph-area-bar"/>
            <rect x="78" y="116" width="16" height="8" class="graph-area-bar"/>
            <rect x="102" y="107" width="16" height="17" class="graph-area-bar"/>
            <rect x="126" y="91" width="16" height="33" class="graph-area-bar"/>
            <rect x="150" y="69" width="16" height="55" class="graph-area-bar"/>
            <rect x="174" y="43" width="16" height="81" class="graph-area-bar"/>
            <path d="M 36 124 C 86 122, 132 102, 188 38" class="graph-curve"/>
            <line x1="188" y1="38" x2="188" y2="124" class="graph-marker"/>
            <text x="31" y="140" class="graph-label">0</text>
            <text x="183" y="140" class="graph-label">1</text>
          </svg>
          <p class="graph-caption">De blauwe vulling is de benaderde oppervlakte onder <code>x^2</code> van <code>0</code> tot <code>1</code>.</p>
        </div>
        """;
    }

    // Zoek/commentaar: Breidt math-help uit met duidelijke groepen en complete voorbeelden.
    private string BuildMathCommandHelp(string keyword)
    {
        if (!keyword.Equals("math", StringComparison.OrdinalIgnoreCase))
            return "";

        var mathHelp = HelpContent("editor.nod_help.math.extra", "nod/command/math-extra.html");

        mathHelp = RemoveMathFlowPills(mathHelp);

        var advancedHelp = HelpContent("editor.nod_help.math.advanced", "nod/command/math-advanced.html");
        if (mathHelp.Contains("math length(vec(3,4))", StringComparison.OrdinalIgnoreCase) ||
            mathHelp.Contains("math vec 3 4", StringComparison.OrdinalIgnoreCase))
            advancedHelp = "";
        var statisticsHelp = HelpContent("editor.nod_help.math.statistics", "nod/command/math-statistics.html");
        var probabilityHelp = HelpContent("editor.nod_help.math.probability", "nod/command/math-probability.html");
        if (mathHelp.Contains("math mean(2,4", StringComparison.OrdinalIgnoreCase) ||
            mathHelp.Contains("<summary>Statistiek</summary>", StringComparison.OrdinalIgnoreCase) ||
            mathHelp.Contains("<summary>Statistics</summary>", StringComparison.OrdinalIgnoreCase))
            statisticsHelp = "";
        var extraMathSections = advancedHelp + statisticsHelp + probabilityHelp;

        var insertionMarker = "<details><summary>Terugrekenen</summary>";
        var markerIndex = mathHelp.IndexOf(insertionMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
            markerIndex = mathHelp.IndexOf("<h2>Veelgemaakte math-fouten", StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
            markerIndex = mathHelp.IndexOf("<h2>Common math mistakes", StringComparison.OrdinalIgnoreCase);

        if (markerIndex >= 0)
            mathHelp = mathHelp[..markerIndex] + extraMathSections + mathHelp[markerIndex..];
        else
            mathHelp += extraMathSections;

        return PlaceMathVisuals(mathHelp);
    }

    private string HelpLanguageCode()
    {
        return HelpApi.LanguageCodeFromFileName(_language.FileName);
    }

    private static string RemoveMathFlowPills(string html)
        => Regex.Replace(
            html,
            @"\s*<div\s+class=""math-flow"">.*?</div>\s*",
            Environment.NewLine,
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

    private string BuildReverseCommandHelp(string keyword)
    {
        if (!keyword.Equals("reverse", StringComparison.OrdinalIgnoreCase))
            return "";

        return HelpContent("editor.nod_help.reverse.extra", "nod/command/reverse-extra.html");
    }

    // Zoek/commentaar: Breidt tekstcommands zoals chg en trans uit met echte voorbeeldpatronen.
    private string BuildTextCommandHelp(string keyword)
    {
        if (keyword.Equals("chg", StringComparison.OrdinalIgnoreCase))
        {
            return HelpContent("editor.nod_help.chg.extra", "nod/command/chg-extra.html");
        }

        if (keyword.Equals("trans", StringComparison.OrdinalIgnoreCase))
        {
            return HelpContent("editor.nod_help.trans.extra", "nod/command/trans-extra.html");
        }

        return "";
    }

    // Legt uit wat NOD 2.0 slimmer doet met trans zonder NOD 1.0-regels te breken.
    private string BuildImprovedTransHelp(string keyword)
    {
        if (!keyword.Equals("trans", StringComparison.OrdinalIgnoreCase))
            return "";

        return HelpContent("editor.nod_help.trans.improved", "nod/command/trans-improved.html");
    }

    // Zoek/commentaar: Breidt mode-help uit met de hoofdsoorten NOD-bestanden.
    private string BuildModeCommandHelp(string keyword)
    {
        if (!keyword.Equals("mode", StringComparison.OrdinalIgnoreCase))
            return "";

        return HelpContent("editor.nod_help.mode.extra", "nod/command/mode-extra.html");
    }

    // Zoek/commentaar: Legt phoneformat uit op basis van de opties die NodParser echt accepteert.
    private string BuildPhoneFormatCommandHelp(string keyword)
    {
        if (!keyword.Equals("phoneformat", StringComparison.OrdinalIgnoreCase))
            return "";

        return HelpContent("editor.nod_help.phoneformat.extra", "nod/command/phoneformat-extra.html");
    }

    // Zoek/commentaar: Maakt klikbare links vanaf Symbolen naar de losse Symb-pagina's.
    private string BuildSymbolOverviewLinks()
    {
        return HelpContent("editor.nod_help.symbols.overview_links", "nod/snippet/symbol-overview-links.html");
    }

    // Zoek/commentaar: Maakt per Symb-command een klein gericht vensterdiagram.
    private string BuildSymbolCommandDiagram(string keyword)
    {
        var key = keyword.ToLowerInvariant();
        if (key is not ("symb1" or "symb2" or "symb3" or "symb4"))
            return "";

        var isInput = key is "symb1" or "symb3";
        var isLeft = key is "symb1" or "symb2";
        var label = key switch
        {
            "symb1" => "Symb1: left of input",
            "symb2" => "Symb2: left of output",
            "symb3" => "Symb3: right of input",
            _ => "Symb4: right of output"
        };
        var code = key switch
        {
            "symb1" => "Symb1 DM",
            "symb2" => "Symb2 EUR",
            "symb3" => "Symb3 DM",
            _ => "Symb4 EUR"
        };
        var inputBefore = key == "symb1" ? "<span class=\"callout\">DM</span>" : "";
        var inputAfter = key == "symb3" ? "<span class=\"callout right-callout\">DM</span>" : "";
        var outputBefore = key == "symb2" ? "<span class=\"callout\">EUR</span>" : "";
        var outputAfter = key == "symb4" ? "<span class=\"callout right-callout\">EUR</span>" : "";
        var rowLabel = isInput ? "Value in Deutsch Mark" : "Value in euros";
        var rowValue = isInput ? "22" : "11,25";
        var rowBefore = isInput ? inputBefore : outputBefore;
        var rowAfter = isInput ? inputAfter : outputAfter;
        var note = isLeft ? "This symbol appears before the value." : "This symbol appears after the value.";

        return HelpContent(
            "editor.nod_help.symbol.command_diagram",
            "nod/snippet/symbol-command-diagram.html",
            new Dictionary<string, string?>
            {
                ["note"] = note,
                ["rowLabel"] = rowLabel,
                ["shotRadioClass"] = isInput ? "on" : "",
                ["rowBefore"] = rowBefore,
                ["rowValue"] = rowValue,
                ["rowAfter"] = rowAfter,
                ["label"] = label,
                ["code"] = code
            });
    }
    // Wikkelt één help-onderwerp in volledige HTML-opmaak.
    private string WrapNodHelpPage(string title, string body)
    {
        return ApplyHelpLanguagePlaceholders(HelpHtml.WrapTopicPage(title, body, NodHelpCss(), HelpHtml.NodCopyButtonsScript()));
    }

    private string NodHelpCss()
    {
        return HelpApi.NodHelpCss() + Environment.NewLine + NodEditorDocumentThemeCss();
    }

    private string NodPopupCss()
    {
        return HelpApi.NodPopupCss() + Environment.NewLine + NodEditorDocumentThemeCss();
    }

    private string NodEditorDocumentThemeCss()
    {
        if (!IsDarkTheme)
            return "";

        return """
        :root { color-scheme: dark; }
        html, body { background:#111827 !important; color:#e5e7eb !important; }
        body { background:#111827 !important; }
        h1, h2, h3, b, .title { color:#93c5fd !important; }
        h2 { border-bottom-color:#334155 !important; }
        p, li, td, .muted, summary { color:#d1d5db !important; }
        a, .cmd-link, .suggest { color:#bfdbfe !important; }
        code, pre, .syntax, .formula, .mathml-formula, .graph-mathml {
          background:#0f1f36 !important; border-color:#60a5fa !important; color:#ffffff !important;
          overflow-wrap:normal !important;
          word-break:normal !important;
        }
        .syntax {
          font-weight:700 !important;
          box-shadow:none !important;
          white-space:pre-wrap !important;
        }
        input, textarea, select {
          background:#1f2937 !important;
          border:1px solid #60a5fa !important;
          color:#ffffff !important;
          caret-color:#ffffff !important;
        }
        input::placeholder,
        textarea::placeholder {
          color:#dbeafe !important;
          opacity:1 !important;
        }
        .cmd-link code,
        a code {
          background:#172554 !important;
          border-color:#3b82f6 !important;
          color:#ffffff !important;
          font-weight:700 !important;
          white-space:nowrap !important;
          overflow-wrap:normal !important;
          word-break:normal !important;
        }
        .cmd-link:hover code,
        a:hover code {
          background:#1d4ed8 !important;
          border-color:#93c5fd !important;
          color:#ffffff !important;
        }
        pre { background:#020617 !important; color:#e5e7eb !important; }
        table, details, .example-card, .formula-card, .math-card, .graph-card {
          background:#111827 !important; border-color:#334155 !important; box-shadow:none !important;
        }
        th, summary { background:#1e293b !important; color:#bfdbfe !important; border-color:#334155 !important; }
        td { border-color:#334155 !important; }
        tr:nth-child(even) td { background:#0f172a !important; }
        .notice, .help-info, .help-tip {
          background:#172554 !important; border-color:#2563eb !important; color:#dbeafe !important;
        }
        .warning, .warning-sign {
          background:#3b1d0a !important; border-color:#b45309 !important; color:#fed7aa !important;
        }
        a.insert, .actions a.insert {
          background:#1d4ed8 !important; border-color:#3b82f6 !important; color:#ffffff !important;
        }
        a.more, .actions a.more {
          background:transparent !important;
          border-color:transparent !important;
          color:#bfdbfe !important;
          padding:0 !important;
          box-shadow:none !important;
          text-decoration:none !important;
        }
        a.more:hover, .actions a.more:hover,
        a.more:focus, .actions a.more:focus {
          color:#ffffff !important;
          text-decoration:underline !important;
        }
        .actions a.signed-package-badge,
        .actions a.signed-package-badge:link,
        .actions a.signed-package-badge:visited,
        .actions a.signed-package-badge:hover,
        .actions a.signed-package-badge:active,
        .actions a.signed-package-badge:focus {
          display:inline-flex !important;
          align-items:center !important;
          justify-content:center !important;
          flex:0 0 22px !important;
          width:22px !important;
          height:22px !important;
          margin:0 !important;
          color:#22c55e !important;
          background:transparent !important;
          border:1px solid transparent !important;
          padding:0 !important;
          border-radius:4px !important;
          box-shadow:none !important;
          line-height:0 !important;
          text-decoration:none !important;
          outline-offset:2px !important;
        }
        .actions a.signed-package-badge.unsigned,
        .actions a.signed-package-badge.unsigned:link,
        .actions a.signed-package-badge.unsigned:visited,
        .actions a.signed-package-badge.unsigned:hover,
        .actions a.signed-package-badge.unsigned:active,
        .actions a.signed-package-badge.unsigned:focus {
          color:#ef4444 !important;
        }
        .actions a.signed-package-badge:hover,
        .actions a.signed-package-badge:focus {
          background:#052e16 !important;
          border-color:#166534 !important;
        }
        .actions a.signed-package-badge.unsigned:hover,
        .actions a.signed-package-badge.unsigned:focus {
          background:#450a0a !important;
          border-color:#991b1b !important;
        }
        .signed-package-badge svg {
          display:block !important;
          width:18px !important;
          height:18px !important;
          flex:0 0 18px !important;
        }
        .graph-frame, .screenshot-frame { background:#0f172a !important; border-color:#334155 !important; box-shadow:none !important; }
        .graph-caption, .shot-caption { color:#9ca3af !important; }
        ::selection { background:#2563eb; color:#ffffff; }
        """;
    }

    // Toont HTML-help voor het NOD-keyword op de regel waar met rechts is geklikt.
    private void ShowNodKeywordHelpAtMouse(RichTextBox editor, Point mouseLocation)
    {
        var charIndex = editor.GetCharIndexFromPosition(mouseLocation);
        var keyword = GetLineKeywordAtIndex(editor, charIndex);
        var exact = NodKeywords.FirstOrDefault(key => key.Equals(keyword, StringComparison.OrdinalIgnoreCase));

        if (exact is null)
        {
            HideNodHelpPopup();
            return;
        }

        _nodHelpTargetEditor = editor;
        SetNodHelpHtml(BuildNodHelpHtml(exact, Array.Empty<string>()));
        PlaceNodHelpPopup(mouseLocation);
    }

    // Bouwt vertaalde HTML voor een exact keyword of een suggestielijst.
    private string BuildNodHelpHtml(string? exactKeyword, IReadOnlyList<string> matches)
    {
        var body = exactKeyword is not null
            ? BuildExactNodHelpHtml(exactKeyword)
            : BuildSuggestionHtml(matches);
        _pendingNodHelpWidth = EstimateNodHelpPopupWidth(body);

        return ApplyHelpLanguagePlaceholders(HelpHtml.WrapBodyPage(body, NodPopupCss(), HelpHtml.NodPopupHeightScript()));
    }

    // Bouwt de HTML voor één keyword plus een klikbare invoegactie.
    private string BuildExactNodHelpHtml(string keyword)
    {
        _lastNodHelpKeyword = keyword;
        var body = T($"editor.nod_help.{keyword.ToLowerInvariant()}", GetDefaultNodHelpHtml(keyword));
        if (keyword.Equals("math", StringComparison.OrdinalIgnoreCase) &&
            !body.Contains("math length(vec(3,4))", StringComparison.OrdinalIgnoreCase))
            body += HelpContent("editor.nod_help.math.popup_advanced", "nod/popup/math-advanced.html");
        var insertText = IsLegacyInputKeyword(keyword)
            ? T("editor.nod_help.replace", "Vervangen")
            : T("editor.nod_help.insert", "Invoegen");
        var moreText = T("editor.nod_help.more", "Meer help");
        var link = $"nodinsert:///{Uri.EscapeDataString(keyword)}";
        return body + $"<div class=\"actions\"><a class=\"insert\" href=\"{link}\">{WebUtility.HtmlEncode(insertText)}</a><a class=\"more\" href=\"nodhelp:///full\">{WebUtility.HtmlEncode(moreText)}</a>{BuildSignedPackageBadgeHtml()}</div>";
    }

    private string BuildSignedPackageBadgeHtml()
    {
        var information = BuildCurrentLanguageSignedPackageInformation();
        if (information is null)
            return "";

        var title = WebUtility.HtmlEncode(information.Status);
        var unsignedClass = information.Verified ? "" : " unsigned";
        return $"""
        <a class="signed-package-badge{unsignedClass}" href="nodlanginfo:///signed" title="{title}" aria-label="{title}">
          <svg width="18" height="18" viewBox="5485 545 1059 1411" focusable="false" aria-hidden="true">
            <path fill="currentColor" fill-rule="evenodd" d="M 5597.109375 1096.890625 L 6431.089844 1096.890625 C 6492.738281 1096.890625 6543.171875 1147.328125 6543.171875 1208.96875 L 6543.171875 1842.980469 C 6543.171875 1904.621094 6492.738281 1955.054688 6431.089844 1955.054688 L 5597.109375 1955.054688 C 5535.46875 1955.054688 5485.039062 1904.621094 5485.039062 1842.980469 L 5485.039062 1208.96875 C 5485.039062 1147.328125 5535.46875 1096.890625 5597.109375 1096.890625"/>
            <path fill="currentColor" fill-rule="evenodd" d="M 6014.101562 545.269531 C 6236.058594 545.269531 6417.660156 726.859375 6417.660156 948.828125 L 6417.660156 1120.898438 L 6267.699219 1120.898438 L 6267.699219 948.828125 C 6267.699219 809.339844 6153.589844 695.230469 6014.101562 695.230469 C 5874.609375 695.230469 5760.488281 809.339844 5760.488281 948.828125 L 5760.488281 1120.898438 L 5610.53125 1120.898438 L 5610.53125 948.828125 C 5610.53125 726.859375 5792.128906 545.269531 6014.101562 545.269531"/>
            <path fill="#ffffff" fill-rule="evenodd" d="M 5791.21875 1488.179688 C 5812.539062 1466.859375 5847.410156 1466.859375 5868.738281 1488.179688 L 5952.078125 1571.523438 L 6159.460938 1364.140625 C 6180.78125 1342.820312 6215.660156 1342.820312 6236.980469 1364.140625 C 6258.300781 1385.460938 6258.300781 1420.339844 6236.980469 1441.660156 L 5990.839844 1687.804688 C 5969.519531 1709.125 5934.640625 1709.125 5913.320312 1687.804688 L 5791.21875 1565.695312 C 5769.898438 1544.375 5769.898438 1509.496094 5791.21875 1488.179688"/>
          </svg>
        </a>
        """;
    }

    private static bool IsLegacyInputKeyword(string keyword)
    {
        return keyword.Equals("input1", StringComparison.OrdinalIgnoreCase)
            || keyword.Equals("input2", StringComparison.OrdinalIgnoreCase);
    }

    // Bouwt HTML met suggesties wanneer het keyword nog niet volledig is.
    private string BuildSuggestionHtml(IReadOnlyList<string> matches)
    {
        var title = T("editor.nod_help.suggestions", "Suggesties");
        var insertText = T("editor.nod_help.insert", "Invoegen");
        var items = string.Join("", matches.Select(key =>
        {
            var help = StripHtml(T($"editor.nod_help.{key.ToLowerInvariant()}", GetDefaultNodHelpHtml(key)));
            var summary = help.Split(['\r', '\n', '.'], StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault()?.Trim();
            var link = $"nodinsert:///{Uri.EscapeDataString(key)}";
            return $"<li><a class=\"suggest\" href=\"{link}\">{WebUtility.HtmlEncode(key)}</a>{(string.IsNullOrWhiteSpace(summary) ? "" : " - " + WebUtility.HtmlEncode(summary))} <span class=\"muted\">({WebUtility.HtmlEncode(insertText)})</span></li>";
        }));

        return $"<div class=\"title\">{WebUtility.HtmlEncode(title)}</div><ul>{items}</ul>";
    }

    // Fallback-HTML wanneer een taalbestand nog geen helptekst voor dit keyword heeft.
    private string GetDefaultNodHelpHtml(string keyword)
    {
        var key = keyword.ToLowerInvariant();
        return HelpContent($"editor.nod_help.{key}", $"nod/popup/{key}.html", BuildGenericNodHelpHtml(keyword));
    }

    private static string BuildGenericNodHelpHtml(string keyword)
    {
        var encodedKeyword = WebUtility.HtmlEncode(keyword);
        return $"<div class=\"title\">{encodedKeyword}</div><div class=\"syntax\">{encodedKeyword} ...</div><div><b>Example:</b> <code>{encodedKeyword} ...</code></div>";
    }
    // Verwijdert HTML zodat dezelfde vertaalde helptekst ook als korte suggestie kan dienen.
    private static string StripHtml(string html)
    {
        return WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", "\n"));
    }

    // Schat de bubble-breedte uit de echte inhoud, zodat korte commando's geen brede lege kaart krijgen.
    private static int EstimateNodHelpPopupWidth(string bodyHtml)
    {
        var importantTextLength = 0;
        foreach (Match match in Regex.Matches(bodyHtml, @"<div\s+class=""syntax"">(?<text>.*?)</div>|<code>(?<text>.*?)</code>", RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var text = WebUtility.HtmlDecode(Regex.Replace(match.Groups["text"].Value, "<[^>]+>", ""));
            importantTextLength = Math.Max(importantTextLength, text.Trim().Length);
        }

        var plainTextLength = StripHtml(bodyHtml)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Length)
            .DefaultIfEmpty(0)
            .Max();

        var desiredWidth = Math.Max(
            306 + (int)Math.Ceiling(importantTextLength * 3.0),
            286 + (int)Math.Ceiling(plainTextLength * 1.8));

        return Math.Clamp(desiredWidth, NodHelpPopupMinWidth, NodHelpPopupMaxWidth);
    }

    // Toont help voor het keyword waar de caret op staat.
    private void ShowNodKeywordHelpAtCaret(RichTextBox editor)
    {
        if (!TryGetKeywordAtCaret(editor, out var keyword))
            return;

        var exact = NodKeywords.FirstOrDefault(key => key.Equals(keyword, StringComparison.OrdinalIgnoreCase));
        if (exact is null)
            return;

        _nodHelpTargetEditor = editor;
        SetNodHelpHtml(BuildNodHelpHtml(exact, Array.Empty<string>()));
        PlaceNodHelpPopup(editor.GetPositionFromCharIndex(editor.SelectionStart));
    }

    // Leest het keyword alleen wanneer de caret echt op het eerste command van de regel staat.
    private static bool TryGetKeywordAtCaret(RichTextBox editor, out string keyword)
    {
        keyword = "";
        if (editor.TextLength == 0)
            return false;

        var index = Math.Min(Math.Max(0, editor.SelectionStart), editor.TextLength);
        var line = editor.GetLineFromCharIndex(index);
        var lineStart = editor.GetFirstCharIndexFromLine(line);
        if (lineStart < 0)
            return false;

        var nextLineStart = line + 1 < editor.Lines.Length
            ? editor.GetFirstCharIndexFromLine(line + 1)
            : editor.TextLength;
        if (nextLineStart < lineStart)
            nextLineStart = editor.TextLength;

        var lineText = editor.Text.Substring(lineStart, Math.Max(0, nextLineStart - lineStart));
        if (lineText.TrimStart().StartsWith("'", StringComparison.Ordinal))
            return false;

        var match = Regex.Match(lineText, @"^(?<space>\s*)(?<keyword>[A-Za-z0-9_]+)\b");
        if (!match.Success)
            return false;

        var keywordStart = lineStart + match.Groups["space"].Length;
        var keywordEnd = keywordStart + match.Groups["keyword"].Length;
        if (index < keywordStart || index > keywordEnd)
            return false;

        keyword = match.Groups["keyword"].Value;
        return true;
    }

    // Geeft een stabiel ankerpunt op het begin van het command, onafhankelijk van caretpositie in het woord.
    private static Point GetKeywordAnchorPoint(RichTextBox editor)
    {
        var index = Math.Min(Math.Max(0, editor.SelectionStart), editor.TextLength);
        var line = editor.GetLineFromCharIndex(index);
        var lineStart = editor.GetFirstCharIndexFromLine(line);
        if (lineStart < 0)
            return editor.GetPositionFromCharIndex(index);

        var nextLineStart = line + 1 < editor.Lines.Length
            ? editor.GetFirstCharIndexFromLine(line + 1)
            : editor.TextLength;
        if (nextLineStart < lineStart)
            nextLineStart = editor.TextLength;

        var lineText = editor.Text.Substring(lineStart, Math.Max(0, nextLineStart - lineStart));
        var match = Regex.Match(lineText, @"^(?<space>\s*)(?<keyword>[A-Za-z0-9_]+)\b");
        if (!match.Success)
            return editor.GetPositionFromCharIndex(index);

        return editor.GetPositionFromCharIndex(lineStart + match.Groups["space"].Length);
    }

    // Leest het keyword/prefix vanaf het begin van de huidige regel.
    private static string GetCurrentLineKeywordPrefix(RichTextBox editor, out bool hasTrailingWhitespace)
    {
        hasTrailingWhitespace = false;
        var index = editor.SelectionStart;
        var line = editor.GetLineFromCharIndex(index);
        var lineStart = editor.GetFirstCharIndexFromLine(line);
        if (lineStart < 0 || index < lineStart)
            return "";

        var typed = editor.Text.Substring(lineStart, index - lineStart);
        if (typed.TrimStart().StartsWith("'", StringComparison.Ordinal))
            return "";

        hasTrailingWhitespace = typed.Length > 0 && char.IsWhiteSpace(typed[^1]);
        var match = Regex.Match(typed, @"^\s*(?<keyword>[A-Za-z0-9_]*)(\s*)$");
        return match.Success ? match.Groups["keyword"].Value : "";
    }

    // Leest welke NOD-commands al in het document staan.
    private static HashSet<string> GetExistingNodKeywords(RichTextBox editor)
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in editor.Lines)
        {
            var line = rawLine.TrimStart();
            if (line.Length == 0 || line.StartsWith("'", StringComparison.Ordinal))
                continue;

            var match = Regex.Match(line, @"^(?<keyword>[A-Za-z0-9_]+)\b");
            if (match.Success)
                existing.Add(match.Groups["keyword"].Value);
        }

        return existing;
    }

    // Bepaalt of een keyword nog nuttig is als suggestie voor toevoegen.
    private static bool ShouldSuggestNodKeyword(string keyword, HashSet<string> existingKeywords)
    {
        if (keyword.Equals("indoprint", StringComparison.OrdinalIgnoreCase))
            return !existingKeywords.Contains("indoprint") || !existingKeywords.Contains("indoend");

        if (IsAlternativeSymbolPair(keyword, "Symb1", "Symb3", existingKeywords))
            return false;

        if (IsAlternativeSymbolPair(keyword, "Symb2", "Symb4", existingKeywords))
            return false;

        if (keyword.Equals("indoend", StringComparison.OrdinalIgnoreCase))
            return !existingKeywords.Contains("indoend") && existingKeywords.Contains("indoprint");

        if (RepeatableNodKeywords.Contains(keyword))
            return true;

        return !existingKeywords.Contains(keyword);
    }

    // Symb1/Symb2 staan links van waarden; Symb3/Symb4 staan rechts van waarden.
    // Per waarde wil je meestal links of rechts, niet allebei: input is Symb1/Symb3, output is Symb2/Symb4.
    private static bool IsAlternativeSymbolPair(string keyword, string first, string second, HashSet<string> existingKeywords)
    {
        if (keyword.Equals(first, StringComparison.OrdinalIgnoreCase))
            return existingKeywords.Contains(first) || existingKeywords.Contains(second);

        if (keyword.Equals(second, StringComparison.OrdinalIgnoreCase))
            return existingKeywords.Contains(second) || existingKeywords.Contains(first);

        return false;
    }

    // Leest het eerste NOD-keyword van de regel bij een bepaalde editorpositie.
    private static string GetLineKeywordAtIndex(RichTextBox editor, int index)
    {
        if (editor.TextLength == 0)
            return "";

        index = Math.Min(Math.Max(0, index), editor.TextLength);
        var line = editor.GetLineFromCharIndex(index);
        var lineStart = editor.GetFirstCharIndexFromLine(line);
        if (lineStart < 0)
            return "";

        var nextLineStart = line + 1 < editor.Lines.Length
            ? editor.GetFirstCharIndexFromLine(line + 1)
            : editor.TextLength;
        var length = Math.Max(0, nextLineStart - lineStart);
        var text = editor.Text.Substring(lineStart, length).Trim();

        if (text.Length == 0 || text.StartsWith("'", StringComparison.Ordinal))
            return "";

        var match = Regex.Match(text, @"^(?<keyword>[A-Za-z0-9_]+)\b");
        return match.Success ? match.Groups["keyword"].Value : "";
    }

    // Voert syntax highlighting uit op de actieve tab.
    private void HighlightCurrent()
    {
        var tab = CurrentTab;
        if (tab is not null)
            HighlightSyntax(tab);
    }

    private void ScheduleSyntaxHighlight(EditorTab tab)
    {
        _pendingHighlightTab = tab;
        _syntaxHighlightTimer.Stop();
        _syntaxHighlightTimer.Start();
    }

    // Kleurt comments, commands, functies en getallen in de editor.
    private void HighlightSyntax(EditorTab tab)
    {
        if (_highlighting) return;

        _highlighting = true;

        var editor = tab.Editor;
        var selectionStart = editor.SelectionStart;
        var selectionLength = editor.SelectionLength;

        editor.SuspendLayout();
        editor.SelectAll();
        editor.SelectionColor = EditorTextColor;
        editor.SelectionFont = new Font(editor.Font, FontStyle.Regular);

        // Comments starting with apostrophe.
        HighlightPattern(editor, @"'.*$", SyntaxCommentColor, FontStyle.Italic, RegexOptions.Multiline);

        // Commands at line start.
        HighlightPattern(editor, @"^\s*(Name|URLN|input1|input2|input|inputr|Result|Resfou|Symb1|Symb2|Symb3|Symb4|format|mode|math|chg|trans|reverse|field|table|output|phoneformat|lookup|match|given|equation|solve|constraint|preview|backup|indoprint|indoend|end)\b", SyntaxCommandColor, FontStyle.Bold, RegexOptions.IgnoreCase | RegexOptions.Multiline);

        // Functions/constants.
        HighlightPattern(editor, @"\b(ans|e|pi|π|sqrt|abs|ln|log|sin|cos|tan|asin|acos|atan|sind|cosd|tand|asind|acosd|atand|rad|deg|mod|rem|diff|integral|limit)\b", SyntaxFunctionColor, FontStyle.Regular, RegexOptions.IgnoreCase);

        // Numbers.
        HighlightPattern(editor, @"(?<!\w)\d+([,.]\d+)?(?!\w)", SyntaxNumberColor, FontStyle.Regular, RegexOptions.None);

        editor.Select(Math.Min(selectionStart, editor.TextLength), Math.Min(selectionLength, Math.Max(0, editor.TextLength - selectionStart)));
        editor.SelectionColor = EditorTextColor;
        editor.ResumeLayout();

        _highlighting = false;
    }

    // Past één regex-highlightregel toe op de RichTextBox.
    private static void HighlightPattern(RichTextBox editor, string pattern, Color color, FontStyle style, RegexOptions options)
    {
        foreach (Match match in Regex.Matches(editor.Text, pattern, options))
        {
            editor.Select(match.Index, match.Length);
            editor.SelectionColor = color;
            editor.SelectionFont = new Font(editor.Font, style);
        }
    }

    // Werkt titelbalk, cursorpositie, regelcount en statusvelden bij.
    private void UpdateUiState()
    {
        var tab = CurrentTab;
        var editor = tab?.Editor;
        UpdateSaveButtonState();
        UpdateUndoRedoState();

        if (tab is null || editor is null)
        {
            _positionLabel.Text = FormatPositionStatus(1, 1);
            _lineCountLabel.Text = FormatLineCountStatus(0);
            Text = EditorWindowTitle();
            return;
        }

        var index = editor.SelectionStart;
        var line = editor.GetLineFromCharIndex(index);
        var first = editor.GetFirstCharIndexFromLine(line);
        var col = Math.Max(0, index - first);

        _positionLabel.Text = FormatPositionStatus(line + 1, col + 1);
        _lineCountLabel.Text = FormatLineCountStatus(Math.Max(1, editor.GetLineFromCharIndex(editor.TextLength) + 1));
        _lineEndingLabel.Text = Environment.NewLine == "\r\n" ? "CRLF" : "LF";

        var title = EditorWindowTitle();
        var file = tab.Path is null ? T("editor.tab.new", "new") : Path.GetFileName(tab.Path);
        try
        {
            var meta = NodUiMetadata.Parse(NodTextNormalizer.Normalize(editor.Text, repairConcatenated: true));
            var dialogName = string.IsNullOrWhiteSpace(meta.Urln) ? file : meta.Urln;
            Text = $"{title} - {dialogName}{(tab.Dirty ? " *" : "")}";
        }
        catch
        {
            Text = $"{title} - {file}{(tab.Dirty ? " *" : "")}";
        }
    }

    // Zet de tekst links in de statusbar.
    private void SetStatus(string text)
    {
        _statusLabel.Text = text;
        UpdateUiState();
    }

    private string EditorWindowTitle()
    {
        return T("editor.title", "NOD Editor Plus") + " - " + T("editor.mode.advanced", "Advanced mode");
    }

    private void UpdateSaveButtonState()
    {
        if (_saveButton is null)
            return;

        _saveButton.Enabled = CurrentTab?.Dirty == true;
    }

    private void UpdateUndoRedoState()
    {
        var tab = CurrentTab;
        var canUndo = tab?.UndoTextStack.Count > 0;
        var canRedo = tab?.RedoTextStack.Count > 0;

        if (_undoButton is not null)
            _undoButton.Enabled = canUndo;

        if (_redoButton is not null)
            _redoButton.Enabled = canRedo;

        if (_undoMenuItem is not null)
            _undoMenuItem.Enabled = canUndo;

        if (_redoMenuItem is not null)
            _redoMenuItem.Enabled = canRedo;
    }

    private string FormatPositionStatus(int line, int column)
    {
        return string.Format(T("editor.status.position", "Ln {0}, Col {1}"), line, column);
    }

    private string FormatLineCountStatus(int lines)
    {
        return string.Format(T("editor.status.lines", "Lines {0}"), lines);
    }

    // Haalt vertaalde UI-tekst op, met fallback als de vertaling ontbreekt.
    private string T(string key, string fallback) => HelpApi.Text(ResolveHelpLanguageText, key, fallback);

    private string HelpContent(string key, string fileName, string fallback = "")
    {
        return HelpApi.Content(
            HelpLanguageCode(),
            ResolveHelpLanguageText,
            key,
            fileName,
            fallback,
            ResolveHelpPackageContent);
    }

    private string HelpContent(
        string key,
        string fileName,
        IReadOnlyDictionary<string, string?> placeholders,
        string fallback = "")
    {
        return HelpApi.Content(
            HelpLanguageCode(),
            ResolveHelpLanguageText,
            key,
            fileName,
            placeholders,
            fallback,
            ResolveHelpPackageContent);
    }

    private string? ResolveHelpPackageContent(string fileName)
    {
        if (string.IsNullOrWhiteSpace(_language.PackageId))
            return null;

        return LanguagePackageService.TryReadHelpContentFile(
            AppContext.BaseDirectory,
            _language.PackageId,
            fileName,
            out var content)
            ? content
            : null;
    }

    // [some.lng.key] in bewerkbare help-HTML/JS komt uit de actieve taal, met eng.lng als default.
    private string ApplyHelpLanguagePlaceholders(string template)
    {
        return HelpApi.ApplyLanguagePlaceholders(ResolveHelpLanguageText, template);
    }

    private string? ResolveHelpLanguageText(string key)
    {
        if (_language.TryText(key, out var value))
            return value;

        return _englishLanguage.TryText(key, out var englishValue)
            ? englishValue
            : null;
    }

    // Toolbar icon drawing.
    private enum ToolbarIcon { New, Open, Save, Find, Validate, Test, Solver, Repair, Wizard }

    // Maakt een bitmap-icoon voor de toolbar.
    private static Bitmap CreateToolbarImage(ToolbarIcon icon)
    {
        var bmp = new Bitmap(20, 20);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        switch (icon)
        {
            case ToolbarIcon.New: DrawNewIcon(g); break;
            case ToolbarIcon.Wizard: DrawWizardIcon(g); break;
            case ToolbarIcon.Open: DrawOpenIcon(g); break;
            case ToolbarIcon.Save: DrawSaveIcon(g); break;
            case ToolbarIcon.Find: DrawFindIcon(g); break;
            case ToolbarIcon.Validate: DrawValidateIcon(g); break;
            case ToolbarIcon.Test: DrawTestIcon(g); break;
            case ToolbarIcon.Solver: DrawSolverIcon(g); break;
            case ToolbarIcon.Repair: DrawRepairIcon(g); break;
        }

        return bmp;
    }

    // Tekent het wizard-icoon.
    private static void DrawWizardIcon(Graphics g)
    {
        using var accent = new Pen(Color.FromArgb(37, 99, 235), 1.8f);
        using var dark = new Pen(Color.FromArgb(31, 41, 55), 1.8f);
        using var fill = new SolidBrush(Color.FromArgb(239, 246, 255));

        g.FillEllipse(fill, 2, 2, 16, 16);
        g.DrawEllipse(accent, 2, 2, 16, 16);
        g.DrawLine(dark, 6, 10, 14, 10);
        g.DrawLine(dark, 11, 7, 14, 10);
        g.DrawLine(dark, 11, 13, 14, 10);
        g.DrawLine(accent, 10, 5, 10, 15);
    }

    // Tekent een klein sterretje/ruitje voor iconen.
    private static void DrawSmallStar(Graphics g, Brush brush, int cx, int cy, int r)
    {
        var points = new Point[]
        {
            new Point(cx, cy - r),
            new Point(cx + r, cy),
            new Point(cx, cy + r),
            new Point(cx - r, cy)
        };

        g.FillPolygon(brush, points);
    }

    // Tekent het nieuw-bestand-icoon.
    private static void DrawNewIcon(Graphics g)
    {
        using var pen = new Pen(Color.FromArgb(31, 41, 55), 1.7f);
        using var plusPen = new Pen(Color.FromArgb(37, 99, 235), 1.9f);
        g.DrawRectangle(pen, 5, 3, 9, 13);
        g.DrawLine(plusPen, 4, 10, 10, 10);
        g.DrawLine(plusPen, 7, 7, 7, 13);
    }

    // Tekent het openen-map-icoon.
    private static void DrawOpenIcon(Graphics g)
    {
        using var outline = new Pen(Color.FromArgb(31, 41, 55), 1.7f);
        using var fill = new SolidBrush(Color.FromArgb(254, 240, 138));
        using var accent = new SolidBrush(Color.FromArgb(250, 204, 21));
        using var back = CreateToolbarFolderPath(3, 5, 14, 10);
        using var front = CreateToolbarFolderPath(3, 7, 14, 8);
        g.FillPath(accent, back);
        g.FillPath(fill, front);
        g.DrawPath(outline, front);
    }

    // Tekent het opslaan-icoon.
    private static void DrawSaveIcon(Graphics g)
    {
        using var pen = new Pen(Color.FromArgb(31, 41, 55), 1.7f);
        using var accent = new Pen(Color.FromArgb(37, 99, 235), 1.7f);
        g.DrawRectangle(pen, 4, 3, 12, 14);
        g.DrawLine(accent, 7, 3, 7, 8);
        g.DrawLine(accent, 7, 8, 13, 8);
        g.DrawLine(pen, 7, 14, 13, 14);
    }

    // Tekent het zoek-icoon.
    private static void DrawFindIcon(Graphics g)
    {
        using var pen = new Pen(Color.FromArgb(31, 41, 55), 1.8f);
        g.DrawEllipse(pen, 4, 4, 8, 8);
        g.DrawLine(pen, 11, 11, 16, 16);
    }

    // Tekent het valideren-icoon.
    private static void DrawValidateIcon(Graphics g)
    {
        using var circlePen = new Pen(Color.FromArgb(22, 163, 74), 1.8f);
        using var checkPen = new Pen(Color.FromArgb(22, 163, 74), 2f);
        g.DrawEllipse(circlePen, 3, 3, 14, 14);
        g.DrawLines(checkPen, new[] { new Point(6, 10), new Point(9, 13), new Point(14, 7) });
    }

    // Tekent het test/play-icoon.
    private static void DrawTestIcon(Graphics g)
    {
        using var pen = new Pen(Color.FromArgb(31, 41, 55), 1.7f);
        using var accent = new SolidBrush(Color.FromArgb(37, 99, 235));
        using var box = CreateToolbarRoundedRectanglePath(new Rectangle(4, 3, 12, 14), 3);
        g.DrawPath(pen, box);
        g.FillPolygon(accent, new[] { new Point(8, 7), new Point(13, 10), new Point(8, 13) });
    }

    // Tekent het solver/stappen-icoon.
    private static void DrawSolverIcon(Graphics g)
    {
        using var pen = new Pen(Color.FromArgb(31, 41, 55), 1.6f);
        using var accent = new Pen(Color.FromArgb(37, 99, 235), 1.9f);
        using var fill = new SolidBrush(Color.FromArgb(239, 246, 255));

        g.FillRectangle(fill, 3, 3, 14, 14);
        g.DrawRectangle(pen, 3, 3, 14, 14);
        g.DrawLine(accent, 6, 7, 10, 7);
        g.DrawLine(accent, 6, 10, 14, 10);
        g.DrawLine(accent, 6, 13, 12, 13);
        g.DrawString("∑", new Font("Cambria Math", 7, FontStyle.Bold), Brushes.DarkBlue, 10, 2);
    }

    // Tekent het herstel/repareer-icoon.
    private static void DrawRepairIcon(Graphics g)
    {
        using var pen = new Pen(Color.FromArgb(234, 88, 12), 1.8f);
        using var dark = new Pen(Color.FromArgb(31, 41, 55), 1.7f);
        g.DrawArc(pen, 4, 4, 10, 10, 35, 280);
        g.FillPolygon(new SolidBrush(Color.FromArgb(234, 88, 12)), new[] { new Point(14, 4), new Point(17, 5), new Point(15, 8) });
        g.DrawLine(dark, 11, 12, 16, 17);
    }

    // Maakt een afgerond rechthoekpad voor getekende toolbar-iconen.
    private static GraphicsPath CreateToolbarRoundedRectanglePath(Rectangle rectangle, int radius)
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

    // Maakt het map-vormpad voor het open-icoon.
    private static GraphicsPath CreateToolbarFolderPath(int x, int y, int width, int height)
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

