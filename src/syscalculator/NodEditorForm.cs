#nullable enable
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Tiedragon.NodSystem.Core;
using Tiedragon.Graph2D;
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
    private const int DockedTestPanelWidth = 382;
    private const int MaxDockedLivePreviewWidth = 250;
    private const int DockedSimulatorWidth = 360;
    private const int NodHelpPopupMinWidth = 330;
    private const int NodHelpPopupMaxWidth = 390;
    private const int NodHelpPopupMinHeight = 72;
    private const int NodHelpPopupMaxHeight = 360;
    private const int RecentFilesLimit = 5;
    private static readonly Color NodHelpBubbleBackColor = Color.FromArgb(247, 251, 255);
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
        public Stack<string> UndoTextStack { get; } = new();
        public Stack<string> RedoTextStack { get; } = new();
    }

    private sealed class NodHelpPopupPanel : Panel
    {
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
            using var fill = new SolidBrush(NodHelpBubbleBackColor);
            using var border = new Pen(Color.FromArgb(115, 164, 232), 2.3f);
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
    private bool _highlighting;
    private bool _applyingTextHistory;
    private bool _suppressNodHelpUpdates;
    private Panel _nodHelpPopup = null!;
    private WebView2 _nodHelpBrowser = null!;
    private RichTextBox? _nodHelpTargetEditor;
    private string? _lastNodHelpKeyword;
    private string? _pendingNodHelpHtml;
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
    private HtmlMathPreviewControl _testFormulaView = null!;
    private HtmlMathPreviewControl _testCalculationView = null!;
    private NumericUpDown _graphXMin = null!;
    private NumericUpDown _graphXMax = null!;
    private NumericUpDown _graphStep = null!;
    private Panel _graphCanvas = null!;
    private Panel _graphPointPanel = null!;
    private DataGridView _graphPointTable = null!;
    private Panel _graphPointTitleBar = null!;
    private Panel _graphPointerStatusPanel = null!;
    private Label _graphPointerStatusLabel = null!;
    private Label _graphStatus = null!;
    private Button _graphToggleTableButton = null!;
    private readonly List<PointF> _graphPreviewPoints = new();
    private readonly List<PointF> _graphPreviewStepPoints = new();
    private string _graphPointClipboardText = "";
    private string _graphDisabledMessage = "";
    private NodDocument? _graphPreviewDocument;
    private string _graphPointerText = "";
    private bool _draggingGraphPointPanel;
    private Point _graphPointPanelDragStart;
    private Point _graphPointPanelStartLocation;
    private bool _graphHasView;
    private bool _graphPanning;
    private Point _graphPanStart;
    private float _graphViewMinX;
    private float _graphViewMaxX;
    private float _graphViewMinY;
    private float _graphViewMaxY;
    private float _graphPanStartMinX;
    private float _graphPanStartMaxX;
    private float _graphPanStartMinY;
    private float _graphPanStartMaxY;
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

    // Startpunt van het editorvenster: bouwt de UI en opent eventueel direct een .nod-bestand.
    public NodEditorForm(string? path = null, bool openTemplateWizard = false)
    {
        _language = LanguageCatalog.LoadConfigured(AppContext.BaseDirectory);

        Text = EditorWindowTitle();
        Width = 1100;
        Height = 780;
        MinimumSize = new Size(860, 580);
        StartPosition = FormStartPosition.CenterParent;

        BuildRootLayout();
        BuildTopStripPanel();
        BuildToolbar();
        BuildMenu();
        ArrangeTopStrips();
        BuildLayout();
        BuildStatusbar();
        BuildNodHelpPopup();
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
        if (e.CloseReason == CloseReason.UserClosing && HasUnsavedTabs())
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
        base.OnFormClosing(e);
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

    private bool HasUnsavedTabs()
    {
        return _tabs.Values.Any(tab => tab.Dirty);
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
            ShowIcon = false,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            TopMost = TopMost
        };

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
            BackColor = Color.FromArgb(245, 245, 245),
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
            BackColor = Color.FromArgb(245, 245, 245),
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
        AddMenuItem(view, T("editor.menu.view.graph_preview", "Graph Preview..."), (_, _) => ShowGraphPreviewWindow());
        AddMenuItem(view, T("editor.menu.view.intro_preview", "Introduction preview"), (_, _) => ShowIntroPreview());
        view.DropDownItems.Add(new ToolStripSeparator());
        AddMenuItem(view, T("editor.menu.view.undock_test_panel", "Undock test panel"), (_, _) => UndockTestPanel());
        AddMenuItem(view, T("editor.menu.view.dock_test_panel", "Dock test panel"), (_, _) => DockTestPanel(showPanel: true));
        AddMenuItem(view, T("editor.menu.view.undock_live_preview", "Undock live preview"), (_, _) => UndockLivePreview());
        AddMenuItem(view, T("editor.menu.view.dock_live_preview", "Dock live preview"), (_, _) => DockLivePreview(showPanel: true));
        AddMenuItem(view, T("editor.menu.view.undock_simulator", "Undock simulator"), (_, _) => UndockSimulatorPreview());
        AddMenuItem(view, T("editor.menu.view.dock_simulator", "Dock simulator"), (_, _) => DockSimulatorPreview(showPanel: true));

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
        _toolStrip = ToolEditorApi.CreateToolbar();

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
    private static ToolStripButton MakeButton(string text, EventHandler handler, ToolEditorIcon icon)
    {
        return ToolEditorApi.CreateButton(text, icon, handler, tooltip: text);
    }

    private static ToolStripButton MakeIconButton(string tooltip, EventHandler handler, ToolEditorIcon icon)
    {
        var button = ToolEditorApi.CreateButton("", icon, handler, tooltip: tooltip);
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
        _testInput.TextChanged += (_, _) => UpdatePreviewFromCurrentText();
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
        var testTabs = new TabControl
        {
            Dock = DockStyle.Fill
        };

        _converterTestPage = new TabPage(T("editor.test.title", "Converter test"));
        _converterTestPage.Controls.Add(testPanel);
        _graphPreviewPage = new TabPage(T("editor.graph.title", "Graph Preview"));
        _graphPreviewPage.Controls.Add(BuildGraphPreviewPanel());
        testTabs.SelectedIndexChanged += (_, _) =>
        {
            if (testTabs.SelectedTab == _graphPreviewPage)
                GenerateGraphPreview();
        };

        testTabs.TabPages.Add(_converterTestPage);
        testTabs.TabPages.Add(_graphPreviewPage);
        _testGroup.Controls.Add(testTabs);

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
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var inputGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 8,
            RowCount = 2
        };
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38));
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        inputGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        inputGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 29));

        inputGrid.Controls.Add(new Label { Text = "X min", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        _graphXMin = MakeGraphNumberBox(-5, -100000, 100000, 1);
        _graphXMin.ValueChanged += (_, _) => GenerateGraphPreview();
        inputGrid.Controls.Add(_graphXMin, 1, 0);

        inputGrid.Controls.Add(new Label { Text = "X max", AutoSize = true, Anchor = AnchorStyles.Left }, 2, 0);
        _graphXMax = MakeGraphNumberBox(5, -100000, 100000, 1);
        _graphXMax.ValueChanged += (_, _) => GenerateGraphPreview();
        inputGrid.Controls.Add(_graphXMax, 3, 0);

        inputGrid.Controls.Add(new Label { Text = T("editor.graph.step", "Step"), AutoSize = true, Anchor = AnchorStyles.Left }, 4, 0);
        _graphStep = MakeGraphNumberBox(1, 0.0001m, 100000, 1);
        _graphStep.ValueChanged += (_, _) => GenerateGraphPreview();
        inputGrid.Controls.Add(_graphStep, 5, 0);

        var copyButton = new GraphToolbarIconButton(GraphToolbarIcon.Copy, T("editor.graph.copy_points", "Copy points")) { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        copyButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_graphPointClipboardText))
                Clipboard.SetText(_graphPointClipboardText);
        };
        inputGrid.Controls.Add(copyButton, 0, 1);
        inputGrid.SetColumnSpan(copyButton, 2);

        var openLargeButton = new GraphToolbarIconButton(GraphToolbarIcon.Open, T("editor.graph.open_large", "Open large graph")) { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        openLargeButton.Click += (_, _) => ShowGraphPreviewWindow();
        inputGrid.Controls.Add(openLargeButton, 2, 1);
        inputGrid.SetColumnSpan(openLargeButton, 2);

        _graphToggleTableButton = new GraphToolbarIconButton(GraphToolbarIcon.TableHidden, T("editor.graph.hide_table", "Hide table")) { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        _graphToggleTableButton.Click += (_, _) => SetGraphPointTableVisible(!_graphPointPanel.Visible);
        inputGrid.Controls.Add(_graphToggleTableButton, 4, 1);
        inputGrid.SetColumnSpan(_graphToggleTableButton, 4);

        panel.Controls.Add(inputGrid, 0, 0);

        var graphHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(240, 244, 249)
        };

        _graphCanvas = new Panel
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
        _graphCanvas.MouseEnter += (_, _) => _graphCanvas.Focus();
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

        return panel;
    }

    private void SetGraphPointTableVisible(bool visible)
    {
        _graphPointPanel.Visible = visible;
        UpdateGraphPointerStatusVisibility();
        if (_graphToggleTableButton is GraphToolbarIconButton iconButton)
        {
            iconButton.Icon = visible ? GraphToolbarIcon.TableHidden : GraphToolbarIcon.TableVisible;
            iconButton.TooltipText = visible ? T("editor.graph.hide_table", "Hide table") : T("editor.graph.show_table", "Show table");
        }
        _graphCanvas.Invalidate();
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
        _graphPointPanel.Cursor = Cursors.SizeAll;
        _graphPointTable.Cursor = Cursors.SizeAll;
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
        _graphPreviewForm.FormClosed += (_, _) => _graphPreviewForm = null;
        _graphPreviewForm.Show(this);
    }

    private void ShowAbout()
    {
        using var form = new AboutForm(
            _language,
            LanguageCatalog.ListAvailable(AppContext.BaseDirectory),
            _language.FileName)
        {
            TopMost = TopMost
        };

        if (form.ShowDialog(this) == DialogResult.OK && form.SelectedLanguageFile is not null)
        {
            if (_language.FileName.Equals(form.SelectedLanguageFile, StringComparison.OrdinalIgnoreCase))
                return;

            LanguageCatalog.SaveConfigured(AppContext.BaseDirectory, form.SelectedLanguageFile);
            _language = LanguageCatalog.Load(AppContext.BaseDirectory, form.SelectedLanguageFile);
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

    private void ApplyPanelLanguage()
    {
        _testGroup.Text = T("editor.test.title", "Converter test") + "  \u00D7";
        _converterTestPage.Text = T("editor.test.title", "Converter test");
        _graphPreviewPage.Text = T("editor.graph.title", "Graph Preview");
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

    private static NumericUpDown MakeGraphNumberBox(decimal value, decimal minimum, decimal maximum, decimal increment)
    {
        return new NumericUpDown
        {
            DecimalPlaces = 4,
            Minimum = minimum,
            Maximum = maximum,
            Increment = increment,
            Value = value,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Width = 70
        };
    }

    // Genereert grafiekpunten door huidige NOD als y=f(x) uit te voeren.
    private void GenerateGraphPreview()
    {
        var editor = CurrentEditor;
        if (editor is null || _graphCanvas is null || _graphPointTable is null)
            return;

        _graphPreviewPoints.Clear();
        _graphPreviewStepPoints.Clear();
        FillGraphPointTable([]);

        try
        {
            var min = (double)_graphXMin.Value;
            var max = (double)_graphXMax.Value;
            var step = (double)_graphStep.Value;

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
                _graphPreviewPoints.Clear();
                _graphPreviewStepPoints.Clear();
                FillGraphPointTable([]);
                SetGraphPointTableVisible(false);
                _graphToggleTableButton.Enabled = false;
                _graphHasView = false;
                _graphCanvas.Invalidate();
                return;
            }

            _graphDisabledMessage = "";
            _graphToggleTableButton.Enabled = true;
            var skipped = 0;
            var total = 0;
            const int maxSamples = 2000;

            for (var x = min; x <= max + (step / 1000.0) && total < maxSamples; x += step)
            {
                total++;
                var input = x.ToString("0.############", CultureInfo.InvariantCulture);

                try
                {
                    var result = NodEngine.ConvertForward(_graphPreviewDocument, input);
                    if (TryGetGraphNumber(result, out var y))
                    {
                        var point = new PointF((float)x, (float)y);
                        _graphPreviewPoints.Add(point);
                        _graphPreviewStepPoints.Add(point);
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

            if (total >= maxSamples)
                skipped++;

            FillGraphPointTable(_graphPreviewStepPoints);
            _graphStatus.Text = _graphPreviewPoints.Count == 0
                ? string.Format(T("editor.graph.no_numeric_points", "No numeric points. Skipped: {0}."), skipped)
                : string.Format(T("editor.graph.points_status", "Points: {0}. Skipped: {1}. Range: {2:0.####} to {3:0.####}."), _graphPreviewPoints.Count, skipped, min, max);
            ResetGraphPreviewView(invalidate: false);
            _graphCanvas.Invalidate();
        }
        catch (Exception ex)
        {
            _graphStatus.Text = string.Format(T("editor.graph.error", "Graph error: {0}"), ex.Message);
            _graphCanvas.Invalidate();
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
            var x = point.X.ToString("0.############", CultureInfo.InvariantCulture);
            var y = point.Y.ToString("0.############", CultureInfo.InvariantCulture);
            _graphPointTable.Rows.Add(x, y);
            clipboardLines.Add($"{x}\t{y}");
        }
        _graphPointTable.ClearSelection();
        _graphPointTable.ResumeLayout();

        _graphPointClipboardText = string.Join(Environment.NewLine, clipboardLines);
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
        return Math.Abs(value) < 0.0000001f
            ? "0"
            : value.ToString("0.0", CultureInfo.CurrentCulture);
    }

    private bool IsGraphPreviewCompatible(NodDocument document, out string reason)
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

    private void ResetGraphPreviewView(bool invalidate = true)
    {
        SetNormalGraphPreviewView();
        _graphHasView = true;
        ResampleGraphPreviewVisibleView();
        UpdateGraphPointerStatus(null);

        if (invalidate)
            _graphCanvas.Invalidate();
    }

    private void SetNormalGraphPreviewView()
    {
        var plot = GetGraphPlotRectangle();
        var aspect = plot.Width > 0 && plot.Height > 0
            ? Math.Max(0.1f, plot.Width / (float)plot.Height)
            : 1f;

        var halfY = GraphNormalHalfYRange;
        var halfX = halfY * aspect;

        _graphViewMinX = -halfX;
        _graphViewMaxX = halfX;
        _graphViewMinY = -halfY;
        _graphViewMaxY = halfY;
    }

    private void ZoomGraphPreview(float factor)
    {
        if (!_graphHasView)
            return;

        ZoomGraphPreviewAt(factor, new PointF(_graphCanvas.ClientSize.Width / 2f, _graphCanvas.ClientSize.Height / 2f));
    }

    private void GraphCanvas_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (_graphHasView)
            ZoomGraphPreviewAt(e.Delta > 0 ? 0.85f : 1.18f, e.Location);
    }

    private void ZoomGraphPreviewAt(float factor, PointF screenPoint)
    {
        var plot = GetGraphPlotRectangle();
        if (plot.Width <= 0 || plot.Height <= 0)
            return;

        var anchor = ScreenToGraphPoint(screenPoint, plot);
        var newWidth = (_graphViewMaxX - _graphViewMinX) * factor;
        var newHeight = (_graphViewMaxY - _graphViewMinY) * factor;
        if (newWidth < 0.0001f || newHeight < 0.0001f)
            return;

        var xRatio = (anchor.X - _graphViewMinX) / (_graphViewMaxX - _graphViewMinX);
        var yRatio = (anchor.Y - _graphViewMinY) / (_graphViewMaxY - _graphViewMinY);
        _graphViewMinX = anchor.X - newWidth * xRatio;
        _graphViewMaxX = _graphViewMinX + newWidth;
        _graphViewMinY = anchor.Y - newHeight * yRatio;
        _graphViewMaxY = _graphViewMinY + newHeight;
        MatchGraphPreviewViewToCanvasAspect();
        ResampleGraphPreviewVisibleView();
        _graphCanvas.Invalidate();
    }

    private void GraphCanvas_MouseDown(object? sender, MouseEventArgs e)
    {
        if (!_graphHasView || e.Button != MouseButtons.Left)
            return;

        _graphPanning = true;
        _graphPanStart = e.Location;
        _graphPanStartMinX = _graphViewMinX;
        _graphPanStartMaxX = _graphViewMaxX;
        _graphPanStartMinY = _graphViewMinY;
        _graphPanStartMaxY = _graphViewMaxY;
        _graphCanvas.Cursor = Cursors.Hand;
    }

    private void GraphCanvas_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_graphPanning)
        {
            UpdateGraphPointerStatus(e.Location);
            return;
        }

        var plot = GetGraphPlotRectangle();
        if (plot.Width <= 0 || plot.Height <= 0)
            return;

        var dx = e.X - _graphPanStart.X;
        var dy = e.Y - _graphPanStart.Y;
        var graphDx = dx / (float)plot.Width * (_graphPanStartMaxX - _graphPanStartMinX);
        var graphDy = dy / (float)plot.Height * (_graphPanStartMaxY - _graphPanStartMinY);

        _graphViewMinX = _graphPanStartMinX - graphDx;
        _graphViewMaxX = _graphPanStartMaxX - graphDx;
        _graphViewMinY = _graphPanStartMinY + graphDy;
        _graphViewMaxY = _graphPanStartMaxY + graphDy;
        MatchGraphPreviewViewToCanvasAspect();
        ResampleGraphPreviewVisibleView();
        UpdateGraphPointerStatus(e.Location);
        _graphCanvas.Invalidate();
    }

    private void MatchGraphPreviewViewToCanvasAspect()
    {
        var plot = GetGraphPlotRectangle();
        if (plot.Width <= 0 || plot.Height <= 0)
            return;

        var targetAspect = plot.Width / (float)plot.Height;
        var centerX = (_graphViewMinX + _graphViewMaxX) / 2f;
        var centerY = (_graphViewMinY + _graphViewMaxY) / 2f;
        var halfX = Math.Max(0.0001f, (_graphViewMaxX - _graphViewMinX) / 2f);
        var halfY = Math.Max(0.0001f, (_graphViewMaxY - _graphViewMinY) / 2f);

        if (halfX / halfY < targetAspect)
            halfX = halfY * targetAspect;
        else
            halfY = halfX / targetAspect;

        _graphViewMinX = centerX - halfX;
        _graphViewMaxX = centerX + halfX;
        _graphViewMinY = centerY - halfY;
        _graphViewMaxY = centerY + halfY;
    }

    private void GraphCanvas_MouseUp(object? sender, MouseEventArgs e)
    {
        _graphPanning = false;
        _graphCanvas.Cursor = Cursors.Default;
    }

    private void ResampleGraphPreviewVisibleView()
    {
        if (_graphPreviewDocument is null || !_graphHasView)
            return;

        var visibleMin = Math.Min(_graphViewMinX, _graphViewMaxX);
        var visibleMax = Math.Max(_graphViewMinX, _graphViewMaxX);
        var width = visibleMax - visibleMin;
        if (width <= 0)
            return;

        var desiredSamples = Math.Clamp(_graphCanvas.ClientSize.Width / 6, 80, 500);
        var step = width / desiredSamples;
        var linePoints = new List<PointF>();
        var stepPoints = new List<PointF>();

        for (var i = 0; i <= desiredSamples; i++)
        {
            var x = visibleMin + step * i;
            var input = x.ToString("0.############", CultureInfo.InvariantCulture);

            try
            {
                var result = NodEngine.ConvertForward(_graphPreviewDocument, input);
                if (TryGetGraphNumber(result, out var y))
                    linePoints.Add(new PointF(x, (float)y));
            }
            catch
            {
                // Niet-numerieke punten horen niet in de grafiek.
            }
        }

        var requestedStep = (float)_graphStep.Value;
        if (requestedStep > 0)
        {
            var firstStep = MathF.Ceiling(visibleMin / requestedStep) * requestedStep;
            for (var x = firstStep; x <= visibleMax + requestedStep / 1000f; x += requestedStep)
            {
                var input = x.ToString("0.############", CultureInfo.InvariantCulture);
                try
                {
                    var result = NodEngine.ConvertForward(_graphPreviewDocument, input);
                    if (TryGetGraphNumber(result, out var y))
                        stepPoints.Add(new PointF(x, (float)y));
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
        if (!_graphHasView)
            ResetGraphPreviewView(invalidate: false);

        GraphSurfaceApi.Draw(
            e.Graphics,
            _graphCanvas,
            _graphPreviewPoints,
            _graphPreviewStepPoints.Count > 0 ? _graphPreviewStepPoints : _graphPreviewPoints,
            new GraphPlotView(_graphViewMinX, _graphViewMaxX, _graphViewMinY, _graphViewMaxY),
            (float)_graphXMin.Value,
            (float)_graphXMax.Value,
            (float)_graphStep.Value,
            _graphDisabledMessage,
            "Generate graph",
            GraphPlotDensity.Compact);
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

    // Houdt de preview-splitter netjes verdeeld tussen live preview en simulator.
    private void AdjustPreviewSplitter()
    {
        if (_previewSplit.Width <= 0)
            return;

        var available = _previewSplit.Width - _previewSplit.SplitterWidth;
        const int desiredPanel1Min = 160;
        if (available < desiredPanel1Min + DockedSimulatorWidth)
            return;

        var splitterDistance = available - DockedSimulatorWidth;

        var min = _previewSplit.Panel1MinSize;
        var maxValid = _previewSplit.Width - _previewSplit.Panel2MinSize;
        if (splitterDistance >= min && splitterDistance <= maxValid)
        {
            _previewSplit.SplitterDistance = splitterDistance;
            AdjustLivePreviewWidth();
        }
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
        using var fill = new SolidBrush(Color.FromArgb(239, 246, 255));
        using var border = new Pen(Color.FromArgb(199, 219, 248), 1);
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

    // Leest de huidige editorinhoud en werkt live preview en simulator bij.
    private void UpdatePreviewFromCurrentText()
    {
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

        GenerateGraphPreview();
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
            BackColor = Color.FromArgb(236, 236, 236),
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(0)
        };

        var titlePanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 26,
            BackColor = Color.FromArgb(242, 242, 242)
        };

        _simTitle = new Label
        {
            Text = T("editor.sim.window_title", "Converter preview"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };

        titlePanel.Controls.Add(_simTitle);
        AttachSimulatorDrag(titlePanel);
        AttachSimulatorDrag(_simTitle);

        _simMenuStrip = new MenuStrip
        {
            Dock = DockStyle.Top,
            BackColor = Color.FromArgb(248, 248, 248),
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
            BackColor = Color.FromArgb(245, 245, 245),
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
            Margin = new Padding(6, 5, 3, 0)
        };
        toolbarPanel.Controls.Add(_simFileLabel);

        _simFileBox = new ComboBox
        {
            Width = 198,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 1, 0, 0)
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
            BackColor = Color.FromArgb(236, 236, 236)
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
            Anchor = AnchorStyles.Left
        };
        body.Controls.Add(_simInputLabel, 3, 0);

        body.Controls.Add(new RadioButton { Enabled = false, Checked = true, Anchor = AnchorStyles.Left }, 0, 1);

        _simInputBox = new TextBox
        {
            Dock = DockStyle.Fill
        };
        body.Controls.Add(_simInputBox, 3, 1);

        var inputSuffix = new Label
        {
            Name = "SimInputSuffix",
            Text = "",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        body.Controls.Add(inputSuffix, 4, 1);

        _simOutputLabel = new Label
        {
            Text = "Input2",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        body.Controls.Add(_simOutputLabel, 3, 2);

        body.Controls.Add(new RadioButton { Enabled = false, Checked = false, Anchor = AnchorStyles.Left }, 0, 3);

        _simOutputBox = new TextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true
        };
        body.Controls.Add(_simOutputBox, 3, 3);

        var outputSuffix = new Label
        {
            Name = "SimOutputSuffix",
            Text = "",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        body.Controls.Add(outputSuffix, 4, 3);

        var optionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight
        };

        _simDigitGroup = new CheckBox { Text = T("option.digit_group", "Digit group"), AutoSize = true, Enabled = false, Margin = new Padding(0, 6, 14, 0) };
        _simDecimals = new CheckBox { Text = T("option.decimals", "Decimals"), AutoSize = true, Enabled = false, Margin = new Padding(0, 6, 6, 0) };
        _simDecimalCount = new NumericUpDown { Width = 50, Minimum = 0, Maximum = 8, Enabled = false, Margin = new Padding(0, 3, 0, 0) };

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
    private static Control MakeSimIconButton(string tooltip, ToolbarIcon icon)
    {
        var box = new PictureBox
        {
            Width = 28,
            Height = 24,
            Margin = new Padding(2, 0, 2, 0),
            BackColor = Color.FromArgb(245, 245, 245),
            Cursor = Cursors.Hand,
            Image = CreateToolbarImage(icon),
            SizeMode = PictureBoxSizeMode.CenterImage
        };

        var tip = new ToolTip();
        tip.SetToolTip(box, tooltip);

        return box;
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
            _nodHelpBrowser.CoreWebView2.WebMessageReceived += NodHelpBrowser_WebMessageReceived;
            ShowPendingNodHelpHtmlIfReady();
        };
        _ = InitializeNodHelpBrowserAsync();

        _nodHelpPopup = new NodHelpPopupPanel
        {
            Width = NodHelpPopupMaxWidth,
            Height = NodHelpPopupMinHeight,
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

    private void AddBlankNewTab()
    {
        AddNewTab(T("editor.tab.new", "new"), "", null, dirty: false);
    }

    // Maakt een nieuwe editor-tab met regelnummers, syntax highlighting en preview-updates.
    private void AddNewTab(string title, string text, string? path, bool dirty)
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

        editor.TextChanged += (_, _) =>
        {
            if (_highlighting || _applyingTextHistory) return;
            TrackEditorTextChange(tab);
            SetTabDirty(tab, !IsCleanEditorText(tab));
            UpdateLineNumbers(tab);
            HighlightSyntax(tab);
            UpdateUiState();
            UpdatePreviewFromCurrentText();
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
        _editorTabStrip.Controls.Add(tab.HeaderPanel);
        _tabControl.TabPages.Add(page);
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
                .Where(File.Exists)
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
                .Where(File.Exists)
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
        var recentFiles = new List<string> { fullPath };
        recentFiles.AddRange(LoadRecentFiles().Where(item => !string.Equals(item, fullPath, StringComparison.OrdinalIgnoreCase)));
        SaveRecentFiles(recentFiles);
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
        _graphPreviewPoints.Clear();
        _graphPreviewStepPoints.Clear();
        _graphPreviewDocument = null;
        _graphHasView = false;
        _graphDisabledMessage = "";
        _graphCanvas?.Invalidate();
        _graphPointTable?.Rows.Clear();
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

        if (uri.Scheme.Equals("nodpage", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            var pageId = Uri.UnescapeDataString((uri.Host + uri.AbsolutePath).Trim('/'));
            ShowNodHelpDeferred(pageId);
        }
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
            "preview" => "preview Voorbeeldtekst",
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
            Navigation: GetHelpNavigationLabels()));
    }

    // Levert vertaalde labels voor de gedeelde helpnavigatie.
    private HelpNavigationLabels GetHelpNavigationLabels()
    {
        return new HelpNavigationLabels(
            T("help.nav.home", "Home"),
            T("help.nav.previous", "Previous"),
            T("help.nav.next", "Next"));
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
        var intro = T("editor.nod_help.full.intro", """
        <h2>Wat is NOD?</h2>
        <p><b>NOD</b> is het tekstformaat waarmee Syscalculator een converter beschrijft. Een NOD-bestand vertelt hoe de converter heet, welke invoer en uitvoer zichtbaar zijn, welke symbolen bij waarden horen en welke regels de waarde omzetten.</p>
        <h2>Het basisidee</h2>
        <p>Een NOD-bestand leest als een klein stappenplan. Eerst komt de naam, daarna de labels, daarna eventueel symbolen en uitlegtekst, vervolgens de regels die rekenen of tekst omzetten. De laatste regel is meestal <a class="cmd-link" href="nodpage:cmd:end"><code>end</code></a>.</p>
        <h2>Waarom gewone tekst?</h2>
        <p>Omdat de kennis dan zichtbaar blijft. Een Celsius/Fahrenheit-regel, een eurokoers of een oude telefoonnummeromzetting zit niet verstopt in programmacode, maar staat als leesbare regels in het bestand.</p>
        <h2>Mini voorbeeld</h2>
        <pre>Name Celsius naar Fahrenheit
        input1 Celsius
        input2 Fahrenheit
        format ##.00
        math ans * 1,8
        math ans + 32
        end</pre>
        <p>Lees dit van boven naar beneden: de converter krijgt een naam, toont twee labels, kiest een getalnotatie en voert twee rekenstappen uit met <code>ans</code> als huidige waarde.</p>
        """);
        var guide = T("editor.nod_help.full.guide", """
        <p>Dit hoofdstuk gaat over werken in het editorvenster. Je schrijft de NOD-tekst in het midden, gebruikt de knoppen bovenin voor openen, opslaan, testen en valideren, en leest de commandotips wanneer je op een bekende regel staat.</p>
        <h2>Het venster</h2>
        {NodEditorScreenshot}
        <p class="shot-caption">Voorbeeld: een Celsius/Fahrenheit-converter met de commandotip voor <code>Name</code>.</p>
        <h2>Een converter maken</h2>
        <ol>
          <li>Begin met <a class="cmd-link" href="nodpage:cmd:name"><code>Name</code></a>, <a class="cmd-link" href="nodpage:cmd:input1"><code>input1</code></a> en <a class="cmd-link" href="nodpage:cmd:input2"><code>input2</code></a>.</li>
          <li>Voeg optioneel <a class="cmd-link" href="nodpage:cmd:symb1"><code>Symb1</code></a> tot en met <a class="cmd-link" href="nodpage:cmd:symb4"><code>Symb4</code></a> toe voor symbolen links of rechts van waarden.</li>
          <li>Schrijf de omzetting met <a class="cmd-link" href="nodpage:cmd:math"><code>math</code></a>, <a class="cmd-link" href="nodpage:cmd:trans"><code>trans</code></a> of <a class="cmd-link" href="nodpage:cmd:chg"><code>chg</code></a>.</li>
          <li>Gebruik <b>Validate</b> om syntaxfouten te vinden en <b>Test</b> om de uitkomst te proberen.</li>
          <li>Sla het bestand op zodra de converter klopt.</li>
        </ol>
        <h2>Commandotips</h2>
        <p>De blauwe ballon is bedoeld als korte hulp bij de regel waar de cursor staat. Soms toont hij ook een modernere schrijfwijze. Gebruik <b>Replace</b> alleen wanneer je die moderne vorm echt wilt overnemen; <b>More help</b> opent de volledige commandopagina.</p>
        <h2>Undo, redo en herstel</h2>
        <p><b>Undo</b> en <b>Redo</b> gaan over recente tekstbewerkingen. <b>Restore</b> is groter: daarmee zet je de huidige tab terug naar de laatst geopende of opgeslagen versie. <b>Tools &gt; Repair lines</b> is iets anders; dat probeert geplakte of samengevoegde NOD-regels te repareren.</p>
        """);
        guide = guide.Replace(
            "{NodEditorScreenshot}",
            BuildNodHelpImageTag(
                "NodEditorHelp.png",
                T("help.main.page.nodeditor.screenshot_alt", "Screenshot of the NOD Editor with toolbar, code editor and command tip.")));
        var classic = T("editor.nod_help.full.classic_book", """
        <h2>Waarom oud en nieuw naast elkaar bestaan</h2>
        <p>Syscalculator 2.0 is geen breuk met de oude NOD-bestanden. De bedoeling is juist dat oude converters herkenbaar blijven, terwijl de editor meer hulp geeft tijdens het schrijven, testen en onderhouden.</p>
        <h2>Wat klassiek NOD sterk maakte</h2>
        <p>Klassiek NOD was klein en direct. Een regel met <a class="cmd-link" href="nodpage:cmd:math"><code>math</code></a> rekende verder met <code>ans</code>. Een regel met <a class="cmd-link" href="nodpage:cmd:trans"><code>trans</code></a> vertaalde een vaste waarde. Een regel met <a class="cmd-link" href="nodpage:cmd:chg"><code>chg</code></a> veranderde het begin van tekst, bijvoorbeeld bij oude telefoonnummeromzettingen.</p>
        <h2>Wat NOD 2.0 toevoegt</h2>
        <p>NOD 2.0 bewaart die eenvoudige basis, maar zet er een moderne editor omheen: syntaxkleuring, commandotips, rechterklik-help, templates, validatie, testuitvoer, preview en herstel. De taal blijft leesbaar, maar de omgeving helpt sneller fouten vinden.</p>
        <h2>Overzicht</h2>
        <table>
        <tr><th>Syscalculator 1.73 / NOD 1.x</th><th>Syscalculator 2.0 / NOD editor</th></tr>
        <tr><td>Kleine tekstbestanden met klassieke commands zoals <code>Name</code>, <code>input1</code>, <code>input2</code>, <code>math</code>, <code>trans</code>, <code>chg</code> en <code>end</code>.</td><td>Dezelfde basis blijft werken, zodat oude NOD-bestanden herkenbaar en bruikbaar blijven.</td></tr>
        <tr><td>Je moest vaak onthouden waar symbolen verschijnen en daarna in de oude calculator testen.</td><td>De editor toont een live preview/simulator waarin je meteen ziet waar <code>Symb1</code> t/m <code>Symb4</code> staan.</td></tr>
        <tr><td><code>chg</code> was vooral klassieke prefixvervanging, handig voor oude telefoonnummer-omzettingen.</td><td><code>chg</code> blijft compatibel, maar NOD 2.0 kan ook patronen met <code>x</code>-capture gebruiken.</td></tr>
        <tr><td>Help zat vooral in voorbeelden en ervaring.</td><td>De editor geeft suggesties, rechterklik-help, commandopagina's en voorbeelden per soort NOD-bestand.</td></tr>
        </table>
        <h2>Vuistregel</h2>
        <p>Gebruik oude commands gerust wanneer je een bestaand bestand onderhoudt. Gebruik de modernere schrijfwijze wanneer de editor die voorstelt en het bestand daardoor duidelijker wordt. Compatibiliteit is belangrijk, maar leesbaarheid is het doel.</p>
        """);
        var history = T("editor.nod_help.full.history", """
        <h2>Geschiedenis</h2>
        <p>De oorsprong van Syscalculator ligt eerder dan de moderne Windows-versies. Het begon in de BBS-tijd, toen nodelists, telefoonnummers en tekstbestanden nog dagelijks handwerk waren. Een converter was toen geen luxe, maar een manier om lijsten bruikbaar te houden zonder alles opnieuw te typen.</p>
        <p>De eerste gedachte was eenvoudig: zet oude gegevens om naar een nieuwe vorm met kleine, leesbare regels. Niet een groot programma voor elk probleem, maar een klein regelbestand dat precies beschrijft wat er moet gebeuren. Daaruit groeide het NOD-denken.</p>
        <div class="history-timeline">
          <div class="history-step"><b>BBS-tijd</b><span>In de BBS-wereld draaide veel om verbindingen, nodes, netnummers en lijsten. Gegevens veranderden regelmatig en moesten netjes blijven. Dat vroeg om kleine hulpmiddelen die tekst konden lezen, herkennen en omzetten.</span></div>
          <div class="history-step"><b>Nodelist</b><span>Een nodelist was in die tijd een belangrijke lijst met systemen, node-adressen en telefoonnummers. Adressen konden eruitzien als <code>2:500/200</code> of <code>2:500/241</code>: zone, net en node. Voor buitenstaanders leek dat misschien alleen een grote technische lijst, maar voor BBS-gebruikers was het de routekaart naar andere systemen.</span></div>
          <div class="history-step"><b>Waarom NOD zo heet</b><span>De naam <code>NOD</code> komt uit die nodelist-wereld. Het begon met node- en nummergegevens die veranderd moesten worden. Niemand wilde honderden regels handmatig nalopen; een klein omzetbestand kon dat werk veel sneller en betrouwbaarder doen.</span></div>
          <div class="history-step"><b>Operatie Decibel 1995</b><span>Op 10 oktober 1995 voerde PTT Telecom de landelijke omnummering <code>Operatie Decibel</code> door. Het doel was dat vaste telefoonnummers voortaan tien cijfers kregen. Veel oude vier- en vijfcijferige netnummers verdwenen of werden samengevoegd tot grotere netnummergebieden.</span></div>
          <div class="history-step"><b>Nodomzet en Decibel</b><span>De omnummering paste precies bij het vroege idee achter <code>Nodomzet</code>: neem bestaande node-, lijst- of nummergegevens, herken het oude patroon en schrijf de nieuwe vorm terug. Nodomzet moest complete kommabestanden uit nodelists kunnen verwerken, niet alleen losse nummers. In plaats van alle telefoonnummers met de hand te veranderen kon een NOD-achtig omzetbestand de tabel regel voor regel uitvoeren.</span></div>
          <div class="history-step"><b>Historische omnummeringstabel</b><span>In de converterlijst staat <code>Operatie Decibel 1995 historisch volledig</code>. Dat NOD-bestand bewaart de omnummering als gewone tekstregels: telkens een oude prefix links en de nieuwe prefix rechts. Daarmee laat het bestand precies zien waarvoor <code>chg</code> en <code>Nodomzet</code> oorspronkelijk zo geschikt waren.</span></div>
          <div class="history-step"><b>Van oude prefix naar nieuw nummer</b><span>De regel <code>chg "01105-","0113-35"</code> betekent bijvoorbeeld: herken het oude begin, plaats het nieuwe begin terug en laat de rest van het abonneenummer staan. Zo kon <code>Nodomzet</code> een historische tabel met veel omnummeringen uitvoerbaar houden zonder programmacode te veranderen.</span></div>
          <div class="history-step"><b>Nodomzet DOS in QuickBASIC 4.5</b><span>De eerste versie was nog geen Windows-programma, maar een DOS-omzetter: <code>Nodomzet 1.0</code>, ontwikkeld in <code>QuickBASIC 4.5</code>. Het programma moest nodelist-kommabestanden lezen, velden met node- en telefoongegevens herkennen en de gewijzigde bestanden weer uitschrijven. Daar werd het idee concreet: een programma leest een NOD-regelbestand, voert de regels stap voor stap uit en geeft een nieuwe waarde terug.</span></div>
          <div class="history-step"><b>Syscalculator door de euro-operatie</b><span><code>Syscalculator</code> ontstond rond de euro-operatie in 1999 doordat NOD met <code>math</code> ineens bruikbaar werd voor valutaomrekening. Wat eerst leek op software voor een eenmalige omzetting, werd een Windows-programma: zichtbaar, herbruikbaar en gestuurd door NOD-bestanden.</span></div>
          <div class="history-step"><b>Tweede commando: math</b><span>In die Syscalculator-periode kwam rekenen erbij: <code>math</code>. Eerst was dat vooral praktisch voor euro- en valutaomrekening. Daardoor werd NOD meer dan een vervangtaal: het kon nu ook berekeningen beschrijven.</span></div>
          <div class="history-step"><b>Van omzetter naar NOD-taal</b><span>Pas daarna werd duidelijk dat hetzelfde principe breder bruikbaar was. Niet alleen telefoonnummers, maar ook valuta, teksttabellen en eenheden konden met regels worden beschreven. Zo groeide NOD uit tot een kleine taal voor converters.</span></div>
          <div class="history-step"><b>NOD-groepen</b><span>Na Syscalculator groeiden de losse bestanden uit tot groepen converters. Een map <code>euro</code> kon valutaregels bevatten; mappen zoals <code>Temperature</code>, <code>Distance</code>, <code>Mass</code>, <code>Volume</code> en <code>Pressure</code> bevatten natuurkundige eenheden. Een drukbestand zoals <code>Pds sq inch - kg sq cm.nod</code> laat zien hoe dezelfde <code>math</code>-regelstructuur later ook voor natuurkundige omzettingen werd gebruikt.</span></div>
          <div class="history-step"><b>Niet bij een keer gebleven</b><span>De eurotijd maakte Syscalculator praktisch voor gulden/euro, Deutschmark/euro en andere munten. Daarna bleef de ontwikkeling doorgaan: afstand, massa, temperatuur, volume, druk, teksttabellen en telefoonnummerconversies kwamen erbij. NOD 1.0 gebruikte simpele commands zoals <code>Name</code>, <code>input1</code>, <code>input2</code>, <code>math</code>, <code>trans</code>, <code>chg</code> en <code>end</code>.</span></div>
          <div class="history-step"><b>Syscalculator 1.72 / 1.73 / 1.74</b><span>De VB6-lijn bleef bruikbaar, maar kreeg ook typische oude-Windows problemen: paden, taalbestanden, <code>App.Path</code>, rechten en oude registry-instellingen. Voor 1.74 blijft onderhoud mogelijk, maar nieuwe ontwikkeling hoort in 2.0.</span></div>
          <div class="history-step"><b>VB6-.NET conversiepoging</b><span>Later is geprobeerd om de oude VB6-code automatisch naar .NET te converteren. Dat bleek geen goede route: forms, API-calls, control arrays, <code>App.Path</code>, <code>Load</code>/<code>Unload</code> en default properties leverden te veel fouten op. De ontwikkelaar kreeg daardoor juist extra werk.</span></div>
          <div class="history-step"><b>Bewaarperiode</b><span>Het idee bleef bestaan, maar de uitvoering was te zwaar en kostte te veel tijd. De broncode werd bewaard totdat er een betere manier kwam om het systeem opnieuw te begrijpen.</span></div>
          <div class="history-step"><b>Nieuwe eurolanden 2026</b><span>In 2026 kwamen er opnieuw euro-NODs bij. Bulgarije voerde op 1 januari 2026 de euro in als 21e land van de eurozone, met de vaste koers <code>1 EUR = 1,95583 BGN</code>. Daardoor hoort ook <code>BGN.nod</code> bij de historische eurogroep, net zoals eerder bestanden voor bijvoorbeeld gulden, Duitse mark, Kroatische kuna en andere toetredingen.</span></div>
          <div class="history-step"><b>Postcodezoeker 2026</b><span>In 2026 is een postcodezoeker in ontwikkeling. Daarmee schuift NOD opnieuw een stukje op: naast omnummeren, rekenen en eenheden omzetten kan het systeem ook gegevens opzoeken en koppelen, bijvoorbeeld postcode plus huisnummer naar adresinformatie.</span></div>
          <div class="history-step"><b>Syscalculator 2.0 beta</b><span>Met moderne C#/.NET en AI-hulp is Syscalculator opnieuw opgebouwd: niet blind geconverteerd, maar opnieuw ontworpen met de oude NOD-regels als referentie. Daardoor blijven NOD 1.x bestanden herkenbaar, terwijl NOD 2.0 nieuwe mogelijkheden krijgt.</span></div>
        </div>
        <h2>Waarom NOD bleef</h2>
        <p>De kracht van NOD is dat de kennis niet verstopt zit in programmacode. Een omzetting staat in gewone tekst. Daardoor kun je later nog begrijpen waarom een telefoonnummer, valuta, woord of formule zo wordt omgezet.</p>
        <table>
        <tr><th>Vroege behoefte</th><th>NOD-oplossing</th></tr>
        <tr><td>Nodelists en oude telefoonlijsten moesten aangepast worden.</td><td><code>chg</code> en later patroon-<code>chg</code> kunnen nummers herkenbaar omzetten.</td></tr>
        <tr><td>Teksten en termen moesten van de ene vorm naar de andere.</td><td><code>trans</code> maakt vertaal- en vervanglijsten leesbaar.</td></tr>
        <tr><td>Valuta en eenheden moesten snel berekend worden.</td><td><code>math</code> gebruikt stappen met <code>ans</code>, zodat de berekening zichtbaar blijft.</td></tr>
        </table>
        <h2>Van NOD 1.0 naar NOD 2.0</h2>
        <p>NOD 1.0 was sterk door eenvoud: elke regel deed iets kleins. <code>trans</code> vertaalde tekst, <code>chg</code> verving prefixen, <code>math</code> rekende door met <code>ans</code>. Dat maakte converters leesbaar en makkelijk te bewaren.</p>
        <p>NOD 2.0 bouwt daarop verder. De oude regels blijven herkenbaar, maar krijgen hulp van een moderne editor: suggesties tijdens typen, rechterklik-uitleg, live preview, simulator, Graph Preview en uitgebreide commandopagina's.</p>
        <h2>Les</h2>
        <p>Converteren van VB6 naar .NET leek handig, maar maakte te veel verborgen fouten. De betere aanpak is: oud gedrag begrijpen, compatibiliteit bewaren waar nodig, en de nieuwe versie schoon opbouwen.</p>
        <table>
        <tr><th>Oude aanpak</th><th>Nieuwe aanpak</th></tr>
        <tr><td>Automatisch VB6-code omzetten naar VB.NET.</td><td>NOD-gedrag opnieuw begrijpen en gericht herbouwen in C#.</td></tr>
        <tr><td>Veel conversiefouten en oude ballast.</td><td>Legacy compatibiliteit plus ruimte voor NOD 2.0.</td></tr>
        <tr><td>Ontwikkelaar moest achteraf alles repareren.</td><td>Editor, tests, help en simulator helpen fouten sneller vinden.</td></tr>
        </table>
        <p>Syscalculator 2.0 is daarom geen simpele kopie van vroeger. Het is een opvolger: gebouwd met respect voor NOD 1.0, maar klaar voor nieuwe functies.</p>
        """);
        history = LocalizeHistoryHelp(history);
        var structure = T("editor.nod_help.full.structure", """
        <h2>Basisstructuur</h2>
        <pre>Name Celsius naar Fahrenheit
        URLN Celsius naar Fahrenheit
        input1 Celsius
        input2 Fahrenheit
        format ##.00
        math ans * 1,8
        math ans + 32
        end</pre>
        """);
        var workflow = T("editor.nod_help.full.workflow", """
        <h2>Werkwijze</h2>
        <ol><li>Begin met <code>Name</code>, <code>input1</code> en <code>input2</code>.</li><li>Kies optioneel symbolen met <code>Symb1</code> t/m <code>Symb4</code>.</li><li>Voeg conversieregels toe met <code>math</code>, <code>trans</code> of <code>chg</code>.</li><li>Test met F6 en valideer met F5.</li><li>Eindig met <code>end</code>.</li></ol>
        """);
        var symbols = T("editor.nod_help.full.symbols", """
        <h2>Symbolen</h2>
        <table><tr><th>Command</th><th>Positie</th><th>Voorbeeld</th></tr>
        <tr><td>Symb1</td><td>links van input</td><td>DM 25</td></tr>
        <tr><td>Symb2</td><td>links van output</td><td>EUR 25</td></tr>
        <tr><td>Symb3</td><td>rechts van input</td><td>25 kg</td></tr>
        <tr><td>Symb4</td><td>rechts van output</td><td>25 lb</td></tr></table>
        <p>Kies per waarde meestal links of rechts, niet allebei, om dubbele tekst zoals <code>DM 25 DM</code> te voorkomen.</p>
        """);
        var commands = T("editor.nod_help.full.commands", """
        <h2>Belangrijke commands</h2>
        <ul>
        <li><b>input</b>: moderne invoerdefinitie, bijvoorbeeld <code>input x</code>, <code>input y</code>, <code>input text</code>, <code>input texta</code>, <code>input textb</code> of <code>input phone</code>. <code>input z</code> is alleen future metadata, geen 3D engine in 2.0.</li>
        <li><b>inputr</b>: moderne reverse/output-kant voor gewone terugrekenbare converters, bijvoorbeeld <code>inputr y Fahrenheit</code>. Dit vervangt nieuwe toepassingen van legacy <code>input2</code>.</li>
        <li><b>math</b>: rekent met <code>ans</code>.</li>
        <li><b>trans</b>: vertaalt exacte tekstwaarden.</li>
        <li><b>chg</b>: wijzigt begintekst/prefix. Werkt ook met patronen zoals <code>050-2xxxxx</code>; de meest specifieke prefix wint.</li>
        <li><b>indoprint ... indoend</b>: introductietekstblok. Vergeet <code>indoend</code> niet.</li>
        <li><b>end</b>: sluit het NOD-bestand af.</li>
        </ul>
        """);
        var examples = T("editor.nod_help.full.examples", """
        <h2>Voorbeelden</h2>
        <h3>Tekst vertalen</h3><pre>Name Ja/Nee vertaling
        input1 Nederlands
        input2 Engels
        trans "ja","yes"
        trans "nee","no"
        end</pre>
        <h3>Prefix/patroon omzetten met chg</h3><p><code>chg</code> vervangt alleen het begin van de tekst. Gebruik komma's tussen oud en nieuw. Patronen met <code>x</code> mogen, bijvoorbeeld uit oude telefoon-omnummeringstabellen.</p><pre>Name Operatie Decibel voorbeeld
        input1 Oud telefoonnummer
        input2 Nieuw telefoonnummer
        chg "01751-xxxxx","070-51xxxxx"
        chg "050-2xxxxx","050-52xxxxx"
        end</pre>
        <h3>Voorbeeld zoals in het venster</h3>
        <p>Dit laat zien hoe labels, invoer, resultaat en symbolen op het scherm terechtkomen.</p>
        <div class="dialog-example"><div class="dialog-title">EuroCalculator</div><div class="dialog-menu"><span>File</span><span>Edit</span><span>Config</span><span>Tools</span><span>About</span></div><div class="dialog-body"><div class="dialog-row"><span></span><div class="dialog-label">Value in Deutsch Mark</div><span></span></div><div class="dialog-row"><span class="dialog-radio on"></span><div class="dialog-input">22</div><div class="dialog-symbol arrow-symbol">DM</div></div><div class="dialog-row"><span></span><div class="dialog-label">Value in euros</div><span></span></div><div class="dialog-row"><span class="dialog-radio"></span><div class="dialog-input">11,25</div><span></span></div><div class="dialog-options"><span>□ Digit group</span><span>☑ Decimals</span><span>2</span></div></div></div>
        <pre>Name Duitse mark naar euro
        input1 Value in Deutsch Mark
        input2 Value in euros
        Symb3 DM
        format ##.00
        math ans / 1,95583
        end</pre>
        <h3>Introductietekst</h3><pre>indoprint Deze converter rekent Celsius om naar Fahrenheit.
        indoend</pre>
        """);
        var troubleshooting = T("editor.nod_help.full.troubleshooting", """
        <h2>Veelgemaakte fouten</h2>
        <ul><li>Gebruik niet tegelijk een linker en rechter symbool voor dezelfde waarde, anders krijg je dubbele tekst.</li><li>Sluit elk introductieblok met <code>indoend</code>.</li><li>Gebruik <code>ans</code> in math-regels voor de huidige waarde.</li><li>Zet <code>end</code> aan het einde van het bestand.</li></ul>
        """);

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
            new("commands", T("editor.nod_help.page.commands", "Belangrijke commands"), WrapNodHelpPage(T("editor.nod_help.page.commands", "Belangrijke commands"), commands)),
            new("examples", T("editor.nod_help.page.examples", "Voorbeelden"), WrapNodHelpPage(T("editor.nod_help.page.examples", "Voorbeelden"), examples)),
            new("troubleshooting", T("editor.nod_help.page.troubleshooting", "Veelgemaakte fouten"), WrapNodHelpPage(T("editor.nod_help.page.troubleshooting", "Veelgemaakte fouten"), troubleshooting))
        };

        foreach (var keyword in NodKeywords)
        {
            var title = T($"editor.nod_help.page.command.{keyword.ToLowerInvariant()}", keyword);
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

        return pages;
    }

    private string BuildCompatibilityHelp()
    {
        return T("editor.nod_help.full.compatibility", $$"""
        <h2>NOD 2.0 beta en oude NOD-bestanden</h2>
        <p>Syscalculator 2.0 is nog beta. Daarom mogen er nog nieuwe mogelijkheden bijkomen, maar oude NOD 1.x-bestanden moeten blijven werken.</p>
        <div class="notice"><b>Compatibel:</b> <code>input1</code>, <code>input2</code>, <code>math</code>, <code>trans</code>, <code>chg</code>, <code>Symb1</code> t/m <code>Symb4</code>, <code>indoprint</code>, <code>indoend</code> en <code>end</code> blijven ondersteund in de 2.x-lijn.</div>
        <div class="warning-sign">{{WarningBoardSvg()}}<div><b>Waarschuwing:</b> gebruik <code>input2</code> niet voor 2D vectoren of limited matrix 2x2. Daar is <code>y</code> of de matrixwaarde echte invoer. Als <code>input2</code> tegelijk als oude reverse/output-kant wordt gelezen, botsen de velden en kan de UI of berekening verkeerd koppelen of crashen.</div></div>
        <h2>Oude stijl tegenover nieuwe stijl</h2>
        <table class="syntax-compare">
        <tr><th>Doel</th><th>Oude NOD</th><th>NOD 2.0 beta</th><th>Opmerking</th></tr>
        <tr><td>Numerieke invoer</td><td><code>input1 Celsius</code></td><td><code>input x Celsius</code></td><td><code>input1</code> wordt intern als <code>x</code> gelezen.</td></tr>
        <tr><td>Numerieke uitvoer/reverse-kant</td><td><code>input2 Fahrenheit</code></td><td><code>inputr y Fahrenheit</code> of <code>output y Fahrenheit</code></td><td><code>input2</code> blijft legacy output/reverse-label, zodat terugrekenen niet botst met een echte tweede invoer.</td></tr>
        <tr><td>Tekstomzetting</td><td><code>input1 Tekst</code></td><td><code>input text Tekst</code></td><td>Voor <code>trans</code> en <code>chg</code> is <code>input text</code> duidelijker.</td></tr>
        <tr><td>Twee teksten</td><td><code>input1 Bron</code><br><code>input2 Doel</code></td><td><code>input texta Bron</code><br><code>input textb Doel</code></td><td>Handig voor vergelijken, vertalen of later uitgebreidere teksttools.</td></tr>
        <tr><td>2D vector</td><td><code>input2</code> niet gebruiken</td><td><code>input x X component</code><br><code>input y Y component</code></td><td>Bij vector is <code>y</code> een echte tweede waarde, geen reverse/output-kant.</td></tr>
        <tr><td>Named input</td><td>Niet expliciet</td><td><code>input height Hoogte</code><br><code>input radius Straal</code></td><td>NOD 2.1-voorbereiding voor meer dan drie waarden. In math wordt dit later leesbaar als <code>ans(height)</code>.</td></tr>
        <tr><td>Telefoonnummer</td><td><code>input1 Telefoon</code></td><td><code>input phone Telefoonnummer</code><br><code>input telefoon Telefoonnummer</code></td><td><code>telefoon</code> werkt als alias voor phone.</td></tr>
        <tr><td>Limited matrix 2x2</td><td>Niet expliciet</td><td>Formulekaart met <code>det = a*d - b*c</code></td><td>Alleen educatief/formulekaart in 2.0 beta, geen matrix-engine.</td></tr>
        <tr><td>Matrix 3x3 / 3D</td><td>Niet ondersteund</td><td>Niet gebruiken in 2.0 beta</td><td>Hoort bij latere 3D graph/geometry-engine.</td></tr>
        </table>
        <h2>Wanneer aanpassen?</h2>
        <ul>
        <li>Laat bestaande oude NOD-bestanden gewoon werken.</li>
        <li>Gebruik voor nieuwe NOD 2.0 beta-bestanden liever <code>input x</code>, <code>input y</code>, <code>input text</code> of <code>input phone</code>.</li>
        <li>Verwijder <code>input1</code>/<code>input2</code> pas in een echte breaking versie zoals NOD 3.0.</li>
        </ul>
        <h2>Kringintegraal en formulekaarten</h2>
        <p>Onderwerpen zoals 2D vectorpijlen, kringintegraal en limited matrix 2x2 kunnen in 2.0 beta al als formulekaart en educatieve uitleg bestaan. Een 2D vector mag als pijl in een grafiek worden getoond. <code>math geometry</code>, <code>mode geometry</code> en 3x3 matrices zijn nog geen 2.0-productiefunctie, omdat daarvoor een 3D graph/geometry-engine nodig is. Volledig rekenen met vectorvelden, paden, 3x3 matrices of 3D hoort bij een latere geavanceerde NOD-laag.</p>
        """);
    }

    private string BuildNodHelpImageTag(string fileName, string altText)
    {
        var path = ResolveLocalizedNodHelpImagePath(fileName);
        if (!File.Exists(path))
            return "";

        var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        var mime = extension switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "svg" => "image/svg+xml",
            _ => "image/png"
        };
        var base64 = Convert.ToBase64String(File.ReadAllBytes(path));
        return $"""<div class="screenshot-frame"><img src="data:{mime};base64,{base64}" alt="{WebUtility.HtmlEncode(altText)}" /></div>""";
    }

    private string ResolveLocalizedNodHelpImagePath(string fileName)
    {
        var resourcesPath = Path.Combine(AppContext.BaseDirectory, "Resources");
        var languageCode = HelpLanguageCode();
        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var candidates = new[]
        {
            Path.Combine(resourcesPath, $"{name}.{languageCode}.svg"),
            Path.Combine(resourcesPath, $"{name}.{languageCode}{extension}"),
            Path.Combine(resourcesPath, $"{name}.svg"),
            Path.Combine(resourcesPath, fileName)
        };

        return candidates.FirstOrDefault(File.Exists) ?? Path.Combine(resourcesPath, fileName);
    }

    private string BuildInputCommandHelp(string keyword)
    {
        var key = keyword.ToLowerInvariant();
        if (key is not ("input" or "input1" or "input2" or "inputr"))
            return "";

        return T("editor.nod_help.input.compatibility", $$"""
        <h2>Input compatibility in NOD 2.0 beta</h2>
        <div class="notice"><b>Belangrijk:</b> oude NOD-bestanden met <code>input1</code> en <code>input2</code> blijven werken in de 2.x-lijn. Voor nieuwe bestanden gebruik je <code>input</code> voor echte invoer en <code>inputr</code> voor de reverse/output-kant.</div>
        <div class="warning-sign">{{WarningBoardSvg()}}<div><b>Botsing voorkomen:</b> bij 2D vector en limited matrix 2x2 is <code>input2</code> niet ondersteund. Schrijf <code>input x</code> en <code>input y</code> voor echte waarden. Gebruik <code>inputr y</code> of <code>output y</code> alleen voor de reverse/output-kant.</div></div>
        <table class="syntax-compare">
        <tr><th>Oude syntax</th><th>Nieuwe syntax</th><th>Gebruik</th></tr>
        <tr><td><code>input1 Celsius</code></td><td><code>input x Celsius</code></td><td>eerste numerieke waarde</td></tr>
        <tr><td><code>input2 Fahrenheit</code></td><td><code>inputr y Fahrenheit</code><br><code>output y Fahrenheit</code></td><td>reverse/output-label</td></tr>
        <tr><td><code>input2</code> bij vector/matrix</td><td><code>input x</code> + <code>input y</code></td><td>niet ondersteund; gebruik expliciete waarden</td></tr>
        <tr><td><code>input1 Tekst</code></td><td><code>input text Tekst</code></td><td>tekst voor <code>trans</code> en <code>chg</code></td></tr>
        <tr><td><code>input1 Bron</code><br><code>input2 Doel</code></td><td><code>input texta Bron</code><br><code>input textb Doel</code></td><td>twee tekstwaarden</td></tr>
        <tr><td><code>input1 Telefoon</code></td><td><code>input phone Telefoonnummer</code></td><td>telefoonnummer/omnummering</td></tr>
        <tr><td>-</td><td><code>input z Z coordinate</code></td><td>Alleen future metadata. Geen 3D/matrix-3x3 ondersteuning in 2.0.</td></tr>
        <tr><td>-</td><td><code>input height Hoogte</code><br><code>input radius Straal</code></td><td>named inputs voor NOD 2.1-voorbereiding</td></tr>
        </table>
        <h2>Input gebruiken in math</h2>
        <p>Voor onderwijs en toekomstige multi-input NOD is de duidelijke schrijfwijze: <code>ans(x)</code>, <code>ans(y)</code> en <code>ans(height)</code>. De korte schrijfwijze <code>ansx</code>, <code>ansy</code> en <code>ansz</code> mag als alias worden gezien. In NOD 2.0 blijft <code>ans</code> de huidige waarde/tussenuitkomst en is <code>ans</code> compatibel met oude bestanden.</p>
        <table class="syntax-compare">
        <tr><th>Input</th><th>Math-notatie</th><th>Betekenis</th></tr>
        <tr><td><code>input x Breedte</code></td><td><code>ans(x)</code> of <code>ansx</code></td><td>waarde van input x</td></tr>
        <tr><td><code>input y Hoogte</code></td><td><code>ans(y)</code> of <code>ansy</code></td><td>waarde van input y</td></tr>
        <tr><td><code>input z Diepte</code></td><td><code>ans(z)</code> of <code>ansz</code></td><td>future metadata voor 3D</td></tr>
        <tr><td><code>input height Hoogte</code></td><td><code>ans(height)</code></td><td>custom named input, bedoeld voor NOD 2.1+</td></tr>
        </table>
        <pre>Name Rechthoek oppervlakte
        input x Breedte
        input height Hoogte
        inputr y Oppervlak
        math ans(x) * ans(height)
        end</pre>
        <div class="notice"><b>Status:</b> named inputs kunnen al als metadata in NOD 2.0 beta worden gelezen. Volledig rekenen met meerdere invoervelden hoort bij NOD 2.1, zodat oude <code>ans</code>-berekeningen niet breken.</div>
        <p><code>input1</code> en <code>input2</code> zijn dus niet kapot of verboden; ze zijn legacy-compatible voor gewone oude converters. Bij 2D vector en limited matrix 2x2 is <code>input2</code> niet ondersteund, omdat daar <code>y</code> of matrixwaarden echte invoerwaarden zijn. Pas bij NOD 3.0 mag dit breder breaking worden.</p>
        """);
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
            return AddVersionBadgesToSummaries(html, [BothVersionsBadge(), ModernVersionBadge(), ModernVersionBadge(), ModernVersionBadge(), ModernVersionBadge(), ModernVersionBadge()]);

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
            return T("editor.nod_help.equation.visuals", """
            <h2>Vergelijking als grafiek</h2>
            <p>Bij <code>mode equation</code> kun je de vergelijking zien als een lijn of kromme op een rooster. <code>given</code> zet een bekende waarde vast; <code>solve</code> vraagt om de onbekende.</p>
            <div class="graph-grid">
              <div class="graph-card">
                <h3>Voorbeeld: <code>y = x * 2</code></h3>
                <svg class="graph-frame" viewBox="0 0 240 150" role="img" aria-label="Lijn y is twee keer x op rooster">
                  <defs>
                    <pattern id="grid-equation" width="15" height="15" patternUnits="userSpaceOnUse">
                      <path d="M 15 0 L 0 0 0 15" fill="none" stroke="#dbe7f7" stroke-width="1"/>
                    </pattern>
                  </defs>
                  <rect width="240" height="150" fill="url(#grid-equation)"/>
                  <line x1="30" y1="124" x2="218" y2="124" class="graph-axis"/>
                  <line x1="45" y1="132" x2="45" y2="18" class="graph-axis"/>
                  <path d="M 45 124 L 185 30" class="graph-line"/>
                  <line x1="150" y1="124" x2="150" y2="54" class="graph-helper"/>
                  <line x1="45" y1="54" x2="150" y2="54" class="graph-helper"/>
                  <circle cx="150" cy="54" r="4" class="graph-point"/>
                  <text x="154" y="48" class="graph-label">x = 10, y = 20</text>
                  <text x="178" y="30" class="graph-label">y = 2x</text>
                </svg>
                <pre>Name Vergelijking oplossen
            mode equation
            given y = 20
            equation y = x * 2
            solve x
            constraint x >= 0
            end</pre>
                <p class="graph-caption">De stip is het punt waar <code>y</code> 20 is. De oplossing is dan <code>x = 10</code>.</p>
              </div>
            </div>
            """);
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

        var mathHelp = T("editor.nod_help.math.extra", """
        <h2>Math in het kort</h2>
        <p><code>math</code> rekent met <code>ans</code>. Bij de eerste regel is <code>ans</code> de invoerwaarde; bij volgende regels is <code>ans</code> het vorige resultaat.</p>
        <div class="notice"><b>Structuur:</b> kies eerst het soort berekening, schrijf daarna de bijpassende syntax. Gebruik bij grotere formules liever haakjes of meerdere regels.</div>
        <h2>Syntax per groep</h2>
        <details open><summary>Basisbewerkingen</summary>
        <div class="notice"><b>NOD 1.0:</b> dit geldt voor simpele stappen zoals <code>math ans * 1,8</code> en <code>math ans + 32</code>. Functies, haakjes, constanten en kansrekening horen bij NOD 2.0.</div>
        <table>
          <tr><th>Doel</th><th>Syntax</th><th>Voorbeeld</th></tr>
          <tr><td>Optellen</td><td><code>math ans + getal</code></td><td><code>math ans + 32</code></td></tr>
          <tr><td>Aftrekken</td><td><code>math ans - getal</code></td><td><code>math ans - 10</code></td></tr>
          <tr><td>Vermenigvuldigen</td><td><code>math ans * factor</code></td><td><code>math ans * 1,8</code></td></tr>
          <tr><td>Delen</td><td><code>math ans / factor</code></td><td><code>math ans / 1,95583</code></td></tr>
          <tr><td>Meerdere stappen</td><td>meerdere <code>math</code>-regels</td><td><code>math ans * 1,8</code><br><code>math ans + 32</code></td></tr>
        </table>
        <div class="example-grid">
          <div class="example-card"><h3>Voorbeeld: euro naar cent</h3><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mtext>cent</mtext><mo>=</mo><mtext>euro</mtext><mo>&#x00D7;</mo><mn>100</mn></mrow></math></div><pre>Name Euro naar cent
        input1 Euro
        input2 Cent
        format #.00
        math ans * 100
        end</pre></div>
          <div class="example-card"><h3>Voorbeeld: korting eraf</h3><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mtext>nieuw</mtext><mo>=</mo><mtext>prijs</mtext><mo>-</mo><mn>10</mn></mrow></math></div><pre>Name Prijs min korting
        input1 Prijs
        input2 Na 10 korting
        format #.00
        math ans - 10
        end</pre></div>
        </div>
        </details>

        <details><summary>Functies en constanten</summary>
        <table>
          <tr><th>Groep</th><th>Syntax</th><th>Opmerking</th></tr>
          <tr><td>Macht</td><td><code>math ans^2</code> of <code>math pow(ans,3)</code></td><td>Voor kwadraat, derde macht, enz.</td></tr>
          <tr><td>Wortel</td><td><code>math sqrt(ans)</code></td><td>Ook bruikbaar in samengestelde formules.</td></tr>
          <tr><td>Pi en e</td><td><code>math pi * ans^2</code><br><code>math e^ans</code></td><td>Voor cirkel, exponentieel, rente.</td></tr>
          <tr><td>Logaritme</td><td><code>math log(ans)</code><br><code>math log(ans,2)</code><br><code>math ln(ans)</code></td><td><code>log(ans)</code> is log10.</td></tr>
          <tr><td>Graden</td><td><code>math sind(ans)</code><br><code>math cosd(ans)</code><br><code>math tand(ans)</code></td><td>Gebruik <code>sin/cos/tan</code> voor radialen.</td></tr>
          <tr><td>Afronden</td><td><code>math round(ans)</code><br><code>math floor(ans)</code><br><code>math ceil(ans)</code></td><td>Voor presentatie of afrondlogica.</td></tr>
          <tr><td>Modulo</td><td><code>math mod(ans,2)</code><br><code>math ans % 5</code></td><td>Restwaarde, bijvoorbeeld even/oneven.</td></tr>
          <tr><td>Kansrekening</td><td><code>math ans!</code><br><code>math comb(ans,2)</code><br><code>math perm(ans,2)</code></td><td>Faculteit, combinaties en permutaties.</td></tr>
          <tr><td>Verwachtingswaarde</td><td><code>math expected(0,0.5,10,0.5)</code></td><td>Paren van waarde en kans. Gebruik in functies een punt voor decimalen.</td></tr>
        </table>
        <div class="example-grid">
          <div class="example-card"><h3>Voorbeeld: cirkel</h3><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mi>A</mi><mo>=</mo><mi>&#x03C0;</mi><mo>&#x00B7;</mo><msup><mi>r</mi><mn>2</mn></msup></mrow></math></div><pre>Name Cirkeloppervlak
        input1 Straal
        input2 Oppervlak
        format #.00
        math pi * ans^2
        end</pre></div>
          <div class="example-card"><h3>Voorbeeld: log basis 2</h3><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><msub><mo>log</mo><mn>2</mn></msub><mo>(</mo><mi>x</mi><mo>)</mo></mrow></math></div><pre>Name Log basis 2
        input1 Getal
        input2 Log2
        format #.00
        math log(ans,2)
        end</pre></div>
          <div class="example-card"><h3>Voorbeeld: kansrekening</h3><pre>Name Combinaties
        input1 n
        input2 n kies 2
        format #.00
        math comb(ans,2)
        end</pre><p><code>2!</code> kan direct met <code>math ans!</code>. <code>comb(n,r)</code> is nCr en <code>perm(n,r)</code> is nPr.</p></div>
        </div>
        </details>

        <details><summary>Complexe expressies</summary>
        <p>Gebruik haakjes als de volgorde belangrijk is. Schrijf vermenigvuldiging altijd expliciet met <code>*</code>.</p>
        <pre>math sqrt(ans^2 + 25)
        math ((ans + 10) * 2) / 3
        math ans * (1 + 0,04)^5</pre>
        <div class="example-grid">
          <div class="example-card"><h3>Voorbeeld: haakjes</h3><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mfrac><mrow><mo>(</mo><mo>(</mo><mi>x</mi><mo>+</mo><mn>10</mn><mo>)</mo><mo>&#x00D7;</mo><mn>2</mn><mo>)</mo></mrow><mn>3</mn></mfrac></math></div><pre>Name Haakjes voorbeeld
        input1 Waarde
        input2 Resultaat
        format #.00
        math ((ans + 10) * 2) / 3
        end</pre></div>
          <div class="example-card"><h3>Voorbeeld: rente</h3><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mtext>waarde</mtext><mo>=</mo><mtext>start</mtext><mo>&#x00D7;</mo><msup><mrow><mo>(</mo><mn>1</mn><mo>+</mo><mi>r</mi><mo>)</mo></mrow><mn>5</mn></msup></mrow></math></div><pre>Name Rente 5 jaar
        input1 Startbedrag
        input2 Waarde
        format #.00
        math ans * (1 + 0,04)^5
        end</pre></div>
          <div class="example-card"><h3>Voorbeeld: Pythagoras</h3><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mi>c</mi><mo>=</mo><msqrt><mrow><msup><mi>a</mi><mn>2</mn></msup><mo>+</mo><msup><mn>5</mn><mn>2</mn></msup></mrow></msqrt></mrow></math></div><pre>Name Schuine zijde
        input1 Zijde a
        input2 Zijde c
        format #.00
        math sqrt(ans^2 + 25)
        end</pre><p>Hier is <code>ans</code> zijde <code>a</code>. De andere zijde is vast <code>5</code>, dus <code>5^2</code> is <code>25</code>.</p></div>
        </div>
        <p><b>Let op:</b> schrijf <code>ans * e^2</code>, niet <code>anse^2</code>.</p>
        </details>

        <details><summary>Calculus: diff, integral, limit</summary>
        <table>
          <tr><th>Doel</th><th>Syntax</th><th>Betekenis</th></tr>
          <tr><td>Afgeleide</td><td><code>math diff ans^2</code></td><td>Benadert de helling bij de invoerwaarde.</td></tr>
          <tr><td>Integraal</td><td><code>math integral 0,1 ans^2</code></td><td>Benadert de oppervlakte van 0 tot 1.</td></tr>
          <tr><td>Limiet</td><td><code>math limit 0 sin(ans)/ans</code></td><td>Benadert de limiet rond 0.</td></tr>
          <tr><td>Stap-uitleg</td><td><code>solve diff ans^2</code><br><code>solve integral 0,1 ans^2</code></td><td>Zelfde berekening, maar bedoeld als duidelijke onderwijs-syntax voor stap-voor-stap animatie.</td></tr>
        </table>
        <div class="notice"><b>Belangrijk:</b> calculus is numeriek. Het geeft een benadering, geen symbolische CAS-uitwerking.</div>
        <div class="example-grid">
          <div class="example-card"><h3>Voorbeeld: limiet</h3><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><munder><mo>lim</mo><mrow><mi>x</mi><mo>&#x2192;</mo><mn>0</mn></mrow></munder><mfrac><mrow><mo>sin</mo><mo>(</mo><mi>x</mi><mo>)</mo></mrow><mi>x</mi></mfrac><mo>=</mo><mn>1</mn></mrow></math></div><pre>Name Limiet sinus
        input1 x
        input2 limiet
        format #.00
        math limit 0 sin(ans)/ans
        end</pre></div>
          <div class="example-card"><h3>Voorbeeld: afgeleide</h3><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mfrac><mi>d</mi><mtext>dx</mtext></mfrac><msup><mi>x</mi><mn>2</mn></msup><mo>=</mo><mn>2</mn><mi>x</mi></mrow></math></div><pre>Name Afgeleide kwadraat
        input1 x
        input2 helling
        format #.00
        math diff ans^2
        end</pre></div>
          <div class="example-card"><h3>Voorbeeld: integraal</h3><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><msubsup><mo>&#x222B;</mo><mn>0</mn><mn>1</mn></msubsup><msup><mi>x</mi><mn>2</mn></msup><mtext>dx</mtext><mo>&#x2248;</mo><mn>0,33</mn></mrow></math></div><pre>Name Integraal kwadraat
        input1 x
        input2 oppervlak
        format #.00
        solve integral 0,1 ans^2
        end</pre></div>
        </div>
        </details>

        <details><summary>Terugrekenen</summary>
        <p>Oude simpele math-regels kunnen meestal automatisch terugrekenen. De expliciete <code>reverse</code>-regel is NOD 2.0 en is bedoeld voor samengestelde expressies of formules waarbij de terugrichting niet vanzelf duidelijk is.</p>
        <pre>Name Fahrenheit naar Celsius
        input1 Celsius
        input2 Fahrenheit
        math ans * 1,8
        math ans + 32
        reverse (ans - 32) / 1,8
        end</pre>
        </details>

        <h2>Veelgemaakte math-fouten</h2>
        <ul>
          <li><code>ans</code> vergeten: schrijf <code>math ans * 100</code>, niet <code>math * 100</code>.</li>
          <li>Te veel in één regel zetten terwijl losse stappen duidelijker zijn.</li>
          <li><code>sin</code> gebruiken terwijl je graden bedoelt. Gebruik dan <code>sind</code>.</li>
          <li>Een terugrichting verwachten bij een complexe formule zonder <code>reverse</code>.</li>
        </ul>
        """);

        mathHelp = RemoveMathFlowPills(mathHelp);

        var probabilityHelp = T("editor.nod_help.math.probability", """
        <details><summary>Kansrekening</summary>
        <p>NOD 2.0 ondersteunt faculteit en veelgebruikte kansrekenfuncties in <code>math</code>.</p>
        <div class="example-grid">
          <div class="example-card"><h3>Faculteit</h3><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mi>n</mi><mo>!</mo><mo>=</mo><mn>1</mn><mo>&#x00D7;</mo><mn>2</mn><mo>&#x00D7;</mo><mo>...</mo><mo>&#x00D7;</mo><mi>n</mi></mrow></math></div><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mtext>ans</mtext><mo>=</mo><mi>n</mi></mrow></math></div><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mn>5</mn><mo>!</mo><mo>=</mo><mn>1</mn><mo>&#x00D7;</mo><mn>2</mn><mo>&#x00D7;</mo><mn>3</mn><mo>&#x00D7;</mo><mn>4</mn><mo>&#x00D7;</mo><mn>5</mn><mo>=</mo><mn>120</mn></mrow></math></div><pre>math ans!</pre><p>Vul je <code>5</code> in, dan is <code>ans = 5</code> en rekent NOD dus <code>5!</code>.</p></div>
          <div class="example-card"><h3>Combinaties</h3><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mrow><mo>(</mo><mfrac linethickness="0"><mi>n</mi><mi>r</mi></mfrac><mo>)</mo></mrow><mo>=</mo><mfrac><mrow><mi>n</mi><mo>!</mo></mrow><mrow><mi>r</mi><mo>!</mo><mo>(</mo><mi>n</mi><mo>-</mo><mi>r</mi><mo>)</mo><mo>!</mo></mrow></mfrac></mrow></math></div><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mtext>ans</mtext><mo>=</mo><mi>n</mi><mo>,</mo><mspace width="0.4em"/><mn>2</mn><mo>=</mo><mi>r</mi></mrow></math></div><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mrow><mo>(</mo><mfrac linethickness="0"><mn>5</mn><mn>2</mn></mfrac><mo>)</mo></mrow><mo>=</mo><mfrac><mrow><mn>5</mn><mo>!</mo></mrow><mrow><mn>2</mn><mo>!</mo><mn>3</mn><mo>!</mo></mrow></mfrac><mo>=</mo><mn>10</mn></mrow></math></div><pre>math comb(ans,2)</pre><p>Vul je <code>5</code> in, dan is <code>ans = n = 5</code> en <code>r = 2</code>: kies 2 uit 5.</p></div>
          <div class="example-card"><h3>Permutaties</h3><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mi>P</mi><mo>(</mo><mi>n</mi><mo>,</mo><mi>r</mi><mo>)</mo><mo>=</mo><mfrac><mrow><mi>n</mi><mo>!</mo></mrow><mrow><mo>(</mo><mi>n</mi><mo>-</mo><mi>r</mi><mo>)</mo><mo>!</mo></mrow></mfrac></mrow></math></div><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mtext>ans</mtext><mo>=</mo><mi>n</mi><mo>,</mo><mspace width="0.4em"/><mn>2</mn><mo>=</mo><mi>r</mi></mrow></math></div><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mi>P</mi><mo>(</mo><mn>5</mn><mo>,</mo><mn>2</mn><mo>)</mo><mo>=</mo><mfrac><mrow><mn>5</mn><mo>!</mo></mrow><mrow><mn>3</mn><mo>!</mo></mrow></mfrac><mo>=</mo><mn>20</mn></mrow></math></div><pre>math perm(ans,2)</pre><p>Vul je <code>5</code> in, dan rekent NOD <code>P(5,2)</code>. Gebruik bijvoorbeeld <code>perm(ans,3)</code> voor 3 plaatsen.</p></div>
          <div class="example-card"><h3>Verwachtingswaarde</h3><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mi>E</mi><mo>(</mo><mi>X</mi><mo>)</mo><mo>=</mo><munderover><mo>&#x2211;</mo><mi>i</mi><mi>n</mi></munderover><msub><mi>x</mi><mi>i</mi></msub><mo>&#x00D7;</mo><msub><mi>p</mi><mi>i</mi></msub></mrow></math></div><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><msub><mi>x</mi><mn>1</mn></msub><mo>=</mo><mn>0</mn><mo>,</mo><mspace width="0.4em"/><msub><mi>p</mi><mn>1</mn></msub><mo>=</mo><mn>0.5</mn><mo>,</mo><mspace width="0.4em"/><msub><mi>x</mi><mn>2</mn></msub><mo>=</mo><mn>10</mn><mo>,</mo><mspace width="0.4em"/><msub><mi>p</mi><mn>2</mn></msub><mo>=</mo><mn>0.5</mn></mrow></math></div><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mi>E</mi><mo>(</mo><mi>X</mi><mo>)</mo><mo>=</mo><mn>0</mn><mo>&#x00D7;</mo><mn>0.5</mn><mo>+</mo><mn>10</mn><mo>&#x00D7;</mo><mn>0.5</mn><mo>=</mo><mn>5</mn></mrow></math></div><pre>math expected(0,0.5,10,0.5)</pre><p>De functie leest steeds paren: waarde, kans, waarde, kans.</p></div>
        </div>
        <table>
          <tr><th>Doel</th><th>Syntax</th><th>Voorbeeld</th></tr>
          <tr><td>Faculteit</td><td><code>math ans!</code> of <code>math factorial(ans)</code></td><td><code>5!</code> geeft <code>120</code>.</td></tr>
          <tr><td>Combinaties</td><td><code>math comb(n,r)</code> of <code>math ncr(n,r)</code></td><td><code>comb(5,2)</code> geeft <code>10</code>.</td></tr>
          <tr><td>Permutaties</td><td><code>math perm(n,r)</code> of <code>math npr(n,r)</code></td><td><code>perm(5,2)</code> geeft <code>20</code>.</td></tr>
          <tr><td>Verwachtingswaarde</td><td><code>math expected(waarde,kans,...)</code></td><td><code>expected(0,0.5,10,0.5)</code> geeft <code>5</code>.</td></tr>
        </table>
        <div class="notice"><b>Let op:</b> binnen functies scheidt komma argumenten. Gebruik daar een punt voor decimalen, dus <code>0.5</code> in plaats van <code>0,5</code>. Buiten functies blijft decimale komma werken, zoals <code>math ans / 1,95583</code>.</div>
        <div class="example-grid">
          <div class="example-card"><h3>Voorbeeld: combinaties</h3><pre>Name N kies 2
        input1 n
        input2 nCr met r = 2
        format #.00
        math comb(ans,2)
        end</pre><p>De invoer is <code>n</code>; <code>r</code> staat in deze converter vast op <code>2</code>.</p></div>
          <div class="example-card"><h3>Voorbeeld: verwachtingswaarde</h3><pre>Name Verwachtingswaarde dobbel
        input1 Invoer
        input2 E
        format #.00
        math expected(0,0.5,10,0.5)
        end</pre><p><code>E</code> of <code>e</code> blijft ook de Euler-constante; voor verwachtingswaarde gebruik je de functie <code>expected</code>.</p></div>
        </div>
        </details>
        """);
        probabilityHelp = LocalizeProbabilityHelp(probabilityHelp);

        var insertionMarker = "<details><summary>Terugrekenen</summary>";
        var markerIndex = mathHelp.IndexOf(insertionMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex >= 0)
            mathHelp = mathHelp[..markerIndex] + probabilityHelp + mathHelp[markerIndex..];
        else
            mathHelp = probabilityHelp + mathHelp;

        return PlaceMathVisuals(mathHelp);
    }

    private string HelpLanguageCode()
    {
        var fileName = _language.FileName;
        var slashIndex = Math.Max(fileName.LastIndexOf('\\'), fileName.LastIndexOf('/'));
        if (slashIndex >= 0)
            fileName = fileName[(slashIndex + 1)..];

        if (fileName.EndsWith(".lng", StringComparison.OrdinalIgnoreCase))
            fileName = fileName[..^4];

        return fileName.ToLowerInvariant();
    }

    private string LocalizeHistoryHelp(string fallback)
        => HelpLanguageCode() switch
        {
            "eng" => BuildLocalizedHistoryHelp(
                "History",
                "Syscalculator started before the modern Windows versions. Its roots are in the BBS era, where nodelists, telephone numbers and text files changed often and manual editing was slow and error-prone.",
                "The first idea was deliberately small: convert old data into a new form with readable rules. Not a new large program for every problem, but a rule file that describes exactly what has to change. That became the NOD way of thinking.",
                new[]
                {
                    ("BBS era", "In the BBS world, many things revolved around connections, nodes, net numbers and lists. Data changed regularly and had to stay tidy. Small tools that could read, recognize and convert text were practical necessities."),
                    ("Nodelist", "A nodelist was an important list of systems, node addresses and telephone numbers. Addresses could look like <code>2:500/200</code> or <code>2:500/241</code>: zone, net and node. To BBS users, it was a route map to other systems."),
                    ("Why it is called NOD", "The name <code>NOD</code> comes from that nodelist world. It began with node and number data that had to be changed. Nobody wanted to check hundreds of lines by hand; a small conversion file could do that faster and more reliably."),
                    ("Operation Decibel 1995", "On 10 October 1995 PTT Telecom carried out the Dutch national renumbering known as <code>Operation Decibel</code>. Fixed telephone numbers moved toward a ten-digit format. Many old four- and five-digit area codes disappeared or were merged into larger areas."),
                    ("Nodomzet and Decibel", "That renumbering matched the early idea behind <code>Nodomzet</code>: take existing node, list or number data, recognize the old pattern and write back the new form. Nodomzet had to process complete comma files from nodelists, not only separate numbers."),
                    ("Historical renumbering table", "The converter list contains <code>Operation Decibel 1995 historical complete</code>. That NOD file preserves the renumbering as plain text rules: an old prefix on the left, the new prefix on the right. It shows exactly why <code>chg</code> and Nodomzet were useful."),
                    ("From old prefix to new number", "A rule such as <code>chg \"01105-\",\"0113-35\"</code> means: recognize the old beginning, replace it with the new beginning, and keep the rest of the subscriber number. The table stays executable without changing program code."),
                    ("Nodomzet DOS in QuickBASIC 4.5", "The first version was not a Windows program. It was a DOS converter: <code>Nodomzet 1.0</code>, developed in <code>QuickBASIC 4.5</code>. It read comma-separated nodelist files, recognized node and telephone fields, and wrote the changed files back."),
                    ("Syscalculator through the euro operation", "<code>Syscalculator</code> emerged around the euro operation in 1999 because NOD gained <code>math</code> rules for currency conversion. What first looked like software for one conversion became a visible, reusable Windows program driven by NOD files."),
                    ("Second command: math", "During the Syscalculator period, calculation was added: <code>math</code>. At first this was practical for guilders, euros and other currencies. It made NOD more than a replacement language; it could now describe calculations."),
                    ("From converter to NOD language", "Only after that did it become clear that the same principle was broader. Not only telephone numbers, but also currencies, text tables and units could be described with rules. NOD grew into a small converter language."),
                    ("NOD groups", "After Syscalculator, loose files grew into converter groups. A folder such as <code>euro</code> could contain currency rules; folders such as <code>Temperature</code>, <code>Distance</code>, <code>Mass</code>, <code>Volume</code> and <code>Pressure</code> contained physical units. A pressure file such as <code>Pds sq inch - kg sq cm.nod</code> shows the same <code>math</code> rule structure used for unit conversion."),
                    ("Not a one-time tool", "The euro period made Syscalculator useful for guilder/euro, Deutschmark/euro and other currencies. Development continued with distance, mass, temperature, volume, pressure, text tables and telephone conversions. NOD 1.0 stayed readable through simple commands such as <code>Name</code>, <code>input1</code>, <code>input2</code>, <code>math</code>, <code>trans</code>, <code>chg</code> and <code>end</code>."),
                    ("Syscalculator 1.72 / 1.73 / 1.74", "The VB6 line remained usable, but also carried typical old-Windows problems: paths, language files, <code>App.Path</code>, permissions and old registry settings. Maintenance is still possible for 1.74, but new development belongs in 2.0."),
                    ("VB6-.NET conversion attempt", "Later, the old VB6 code was converted automatically to .NET. That was not the right route: forms, API calls, control arrays, <code>App.Path</code>, <code>Load</code>/<code>Unload</code> and default properties caused too many errors. The developer got extra repair work instead of a clean result."),
                    ("Preserved period", "The idea survived, but the implementation was too heavy and took too much time. The source code was kept until there was a better way to understand and rebuild the system."),
                    ("New euro countries 2026", "In 2026 new euro NODs were added again. Bulgaria introduced the euro on 1 January 2026 as the 21st eurozone country, with the fixed rate <code>1 EUR = 1.95583 BGN</code>. That makes <code>BGN.nod</code> part of the historical euro group."),
                    ("Postcode finder 2026", "In 2026 a postcode finder is in development. This moves NOD a little further: besides renumbering, calculating and unit conversion, the system can also look up and connect data, for example postcode plus house number to address information."),
                    ("Syscalculator 2.0 beta", "With modern C#/.NET and AI help, Syscalculator is rebuilt rather than blindly converted. Old NOD rules remain recognizable, while NOD 2.0 gets room for new functions, editor help, simulation and graph preview.")
                },
                "Why NOD stayed",
                "NOD keeps the conversion knowledge in ordinary text instead of hiding it in program code. Years later, you can still understand why a telephone number, currency, word or formula is converted in that specific way.",
                "Lesson",
                "Converting VB6 to .NET looked convenient, but it created too many hidden errors. The better approach is to understand the old behavior, preserve compatibility where needed, and rebuild the new version cleanly."),
            "deu" => BuildLocalizedHistoryHelp(
                "Geschichte",
                "Syscalculator ist alter als die modernen Windows-Versionen. Der Ursprung liegt in der BBS-Zeit, als Nodelists, Telefonnummern und Textdateien oft angepasst werden mussten.",
                "Die Grundidee war: eine Umwandlung in einer kleinen lesbaren Regeldatei beschreiben, statt fur jede Tabelle ein neues Programm zu schreiben.",
                new[]
                {
                    ("BBS-Zeit", "Nodes, Netze und Nodelists waren die Wegweiser zwischen Systemen. Daten anderten sich regelmassig und mussten lesbar bleiben. Kleine Werkzeuge zum Lesen, Erkennen und Umwandeln von Text waren deshalb praktisch notwendig."),
                    ("Nodelist", "Eine Nodelist enthielt Systeme, Node-Adressen und Telefonnummern. Adressen wie <code>2:500/200</code> oder <code>2:500/241</code> bestanden aus Zone, Netz und Node."),
                    ("Warum NOD so heisst", "Der Name <code>NOD</code> kommt aus der Node- und Nodelist-Welt. Node-, Listen- und Nummerndaten mussten geandert werden, ohne hunderte Zeilen von Hand zu kontrollieren."),
                    ("Operation Decibel 1995", "Am 10. Oktober 1995 fuhrte PTT Telecom die landesweite niederlandische Umnummerierung <code>Operation Decibel</code> durch. Festnetznummern gingen in Richtung eines zehnstelligen nationalen Formats."),
                    ("Nodomzet und Decibel", "Die Umnummerierung passte genau zur fruhen Idee hinter <code>Nodomzet</code>: vorhandene Node-, Listen- oder Nummerndaten lesen, alte Muster erkennen und die neue Form zuruckschreiben."),
                    ("Historische Umnummerungstabelle", "Die Liste enthalt <code>Operation Decibel 1995 historical complete</code>. Diese NOD-Datei speichert alte Prefixe links und neue Prefixe rechts als normale Textregeln."),
                    ("Vom alten Prefix zur neuen Nummer", "Eine Regel wie <code>chg \"01105-\",\"0113-35\"</code> erkennt den alten Anfang, ersetzt ihn durch den neuen Anfang und lasst den Rest der Teilnehmernummer stehen."),
                    ("Nodomzet DOS in QuickBASIC 4.5", "Die erste Version war kein Windows-Programm, sondern <code>Nodomzet 1.0</code> fur DOS, entwickelt in <code>QuickBASIC 4.5</code>. Sie las Kommadateien aus Nodelists und schrieb geanderte Dateien zuruck."),
                    ("Syscalculator durch die Euro-Operation", "<code>Syscalculator</code> entstand um die Euro-Umstellung 1999, weil NOD mit <code>math</code> fur Wahrungsumrechnung nutzlich wurde."),
                    ("Zweites Kommando: math", "In dieser Zeit kam Rechnen hinzu: <code>math</code>. Zuerst fur Gulden, Euro und andere Wahrungen, spater auch fur Einheiten."),
                    ("Von Umsetzer zu NOD-Sprache", "Danach wurde klar, dass dasselbe Prinzip breiter war: Telefonnummern, Wahrungen, Texttabellen und Einheiten konnten alle mit Regeln beschrieben werden."),
                    ("NOD-Gruppen", "Einzelne Dateien wurden zu Gruppen wie <code>euro</code>, <code>Temperature</code>, <code>Distance</code>, <code>Mass</code>, <code>Volume</code> und <code>Pressure</code>. Ein Druck-Konverter zeigt dieselbe <code>math</code>-Struktur fur physikalische Einheiten."),
                    ("Nicht bei einem Mal geblieben", "Syscalculator blieb nach der Eurozeit nutzlich: Entfernung, Masse, Temperatur, Volumen, Druck, Texttabellen und Telefonkonvertierungen kamen dazu."),
                    ("Syscalculator 1.72 / 1.73 / 1.74", "Die VB6-Linie blieb brauchbar, brachte aber alte Windows-Probleme mit: Pfade, Sprachdateien, <code>App.Path</code>, Rechte und Registry-Einstellungen."),
                    ("VB6-.NET-Konvertierung", "Eine automatische Umwandlung von VB6 nach .NET war keine gute Route. Forms, API-Aufrufe, Control-Arrays, <code>Load</code>/<code>Unload</code> und Default Properties verursachten zu viele Fehler."),
                    ("Bewahrte Zeit", "Die Idee blieb erhalten, aber die Umsetzung war zu schwer. Der Quellcode wurde bewahrt, bis eine bessere Neubau-Methode moglich war."),
                    ("Neue Eurolander 2026", "2026 kamen neue Euro-NODs dazu. Bulgarien fuhrte am 1. Januar 2026 den Euro ein, mit dem festen Kurs <code>1 EUR = 1.95583 BGN</code>. Dadurch gehort <code>BGN.nod</code> zur historischen Eurogruppe."),
                    ("Postleitzahl-Sucher 2026", "2026 ist ein Postleitzahl-Sucher in Entwicklung. NOD bewegt sich damit von Umnummerung, Rechnen und Einheiten auch in Richtung Nachschlagen und Verknupfen von Daten."),
                    ("Syscalculator 2.0 beta", "Die neue Version wird in C#/.NET neu aufgebaut: alte NOD-Regeln bleiben erkennbar, NOD 2.0 bekommt moderne Hilfe, Simulation und Graph Preview.")
                },
                "Warum NOD blieb",
                "NOD speichert Umwandlungswissen als normalen Text. Deshalb bleiben Telefonnummern, Wahrungen, Einheiten und Formeln spater noch nachvollziehbar.",
                "Lehre",
                "Eine automatische VB6-.NET-Konvertierung brachte zu viele Altlasten. Besser ist: Verhalten verstehen, Kompatibilitat bewahren und sauber neu bauen."),
            "fra" => BuildLocalizedHistoryHelp(
                "Historique",
                "Syscalculator est plus ancien que les versions Windows modernes. Il vient de l'epoque BBS, quand les nodelists, numeros de telephone et fichiers texte changeaient souvent.",
                "L'idee etait de decrire une conversion dans un petit fichier de regles lisible, au lieu d'ecrire un programme different pour chaque table.",
                new[]
                {
                    ("Epoque BBS", "Les nodes, reseaux et nodelists formaient la carte des connexions. Les donnees changeaient souvent et devaient rester lisibles. De petits outils capables de lire, reconnaitre et convertir du texte etaient donc essentiels."),
                    ("Nodelist", "Une nodelist contenait des systemes, des adresses de node et des numeros de telephone. Des adresses comme <code>2:500/200</code> ou <code>2:500/241</code> indiquaient zone, reseau et node."),
                    ("Pourquoi NOD", "Le nom <code>NOD</code> vient de ce monde. Les donnees de nodes, listes et numeros devaient etre modifiees sans verifier des centaines de lignes a la main."),
                    ("Operation Decibel 1995", "Le 10 octobre 1995, PTT Telecom a lance la renumerotation nationale neerlandaise <code>Operation Decibel</code>. Les numeros fixes allaient vers un format national a dix chiffres."),
                    ("Nodomzet et Decibel", "Cette renumerotation correspondait exactement a l'idee de <code>Nodomzet</code>: lire des donnees de nodes, listes ou numeros, reconnaitre l'ancien modele et ecrire la nouvelle forme."),
                    ("Table historique", "Le fichier <code>Operation Decibel 1995 historical complete</code> conserve cette renumerotation comme regles texte: ancien prefixe a gauche, nouveau prefixe a droite."),
                    ("De l'ancien prefixe au nouveau numero", "Une regle comme <code>chg \"01105-\",\"0113-35\"</code> reconnait l'ancien debut, le remplace et garde le reste du numero d'abonne."),
                    ("Nodomzet DOS en QuickBASIC 4.5", "La premiere version etait <code>Nodomzet 1.0</code> pour DOS, developpee en <code>QuickBASIC 4.5</code>. Elle lisait des fichiers de nodelist separes par virgules et reecrivait les donnees modifiees."),
                    ("Syscalculator par l'operation euro", "<code>Syscalculator</code> est ne autour du passage a l'euro en 1999, quand NOD a recu <code>math</code> pour les conversions de devises."),
                    ("Deuxieme commande: math", "A cette periode le calcul est arrive: <code>math</code>. D'abord pour les florins, euros et autres devises, ensuite pour les unites."),
                    ("Du convertisseur au langage NOD", "Il est ensuite devenu clair que le principe etait plus large: numeros de telephone, devises, tables de texte et unites pouvaient etre decrits par des regles."),
                    ("Groupes NOD", "Les fichiers separes sont devenus des groupes comme <code>euro</code>, <code>Temperature</code>, <code>Distance</code>, <code>Mass</code>, <code>Volume</code> et <code>Pressure</code>."),
                    ("Pas seulement une fois", "Apres l'euro, le developpement a continue: distance, masse, temperature, volume, pression, tables de texte et conversions telephoniques."),
                    ("Syscalculator 1.72 / 1.73 / 1.74", "La ligne VB6 restait utilisable, mais avec des problemes Windows anciens: chemins, fichiers de langue, <code>App.Path</code>, droits et registre."),
                    ("Experience conversion VB6-.NET", "La conversion automatique de VB6 vers .NET n'etait pas la bonne route. Forms, appels API, control arrays, <code>Load</code>/<code>Unload</code> et proprietes par defaut creaient trop d'erreurs."),
                    ("Periode conservee", "L'idee est restee, mais l'implementation etait trop lourde. Le code source a ete conserve jusqu'a une meilleure reconstruction."),
                    ("Nouveaux pays euro 2026", "En 2026 de nouveaux NOD euro ont ete ajoutes. La Bulgarie a introduit l'euro le 1 janvier 2026 avec le taux fixe <code>1 EUR = 1.95583 BGN</code>, donc <code>BGN.nod</code> appartient au groupe euro historique."),
                    ("Chercheur de codes postaux 2026", "En 2026 un chercheur de codes postaux est en developpement. NOD passe ainsi aussi vers la recherche et la liaison de donnees."),
                    ("Syscalculator 2.0 beta", "La nouvelle version est reconstruite en C#/.NET: les anciennes regles NOD restent reconnaissables, avec aide moderne, simulation et Graph Preview.")
                },
                "Pourquoi NOD est reste",
                "NOD garde la connaissance de conversion dans du texte ordinaire. On peut donc relire plus tard une regle de telephone, devise, unite ou formule.",
                "Lecon",
                "Convertir automatiquement VB6 vers .NET a cree trop de problemes caches. La meilleure voie est de comprendre l'ancien comportement et de reconstruire proprement."),
            "ita" => BuildLocalizedHistoryHelp(
                "Storia",
                "Syscalculator nasce prima delle versioni Windows moderne. Le radici sono nel periodo BBS, quando nodelist, numeri telefonici e file di testo cambiavano spesso.",
                "L'idea era descrivere una conversione in un piccolo file di regole leggibile, invece di creare un programma nuovo per ogni tabella.",
                new[]
                {
                    ("Periodo BBS", "Node, reti e nodelist erano la mappa dei collegamenti. I dati cambiavano spesso e dovevano restare ordinati. Servivano piccoli strumenti per leggere, riconoscere e convertire testo."),
                    ("Nodelist", "Una nodelist conteneva sistemi, indirizzi node e numeri telefonici. Indirizzi come <code>2:500/200</code> o <code>2:500/241</code> indicavano zona, rete e node."),
                    ("Perche NOD", "Il nome <code>NOD</code> nasce da quel mondo. Dati di node, liste e numeri dovevano essere modificati senza controllare centinaia di righe a mano."),
                    ("Operazione Decibel 1995", "Il 10 ottobre 1995 PTT Telecom esegui la rinumerazione nazionale olandese <code>Operation Decibel</code>. I numeri fissi andarono verso un formato nazionale a dieci cifre."),
                    ("Nodomzet e Decibel", "Quella rinumerazione corrispondeva all'idea iniziale di <code>Nodomzet</code>: leggere dati di node, liste o numeri, riconoscere il vecchio modello e scrivere la nuova forma."),
                    ("Tabella storica", "Il file <code>Operation Decibel 1995 historical complete</code> conserva la rinumerazione come regole di testo: vecchio prefisso a sinistra, nuovo prefisso a destra."),
                    ("Dal vecchio prefisso al nuovo numero", "Una regola come <code>chg \"01105-\",\"0113-35\"</code> riconosce l'inizio vecchio, lo sostituisce e lascia invariato il resto del numero."),
                    ("Nodomzet DOS in QuickBASIC 4.5", "La prima versione era <code>Nodomzet 1.0</code> per DOS, sviluppata in <code>QuickBASIC 4.5</code>. Leggeva file nodelist separati da virgole e riscriveva i dati modificati."),
                    ("Syscalculator tramite l'euro", "<code>Syscalculator</code> nacque intorno all'operazione euro del 1999, quando NOD ricevette <code>math</code> per le conversioni di valuta."),
                    ("Secondo comando: math", "In quel periodo arrivo il calcolo: <code>math</code>. Prima per fiorini, euro e altre valute, poi anche per unita."),
                    ("Da convertitore a linguaggio NOD", "Poi divenne chiaro che il principio era piu ampio: numeri telefonici, valute, tabelle di testo e unita potevano essere descritti con regole."),
                    ("Gruppi NOD", "I file singoli diventarono gruppi come <code>euro</code>, <code>Temperature</code>, <code>Distance</code>, <code>Mass</code>, <code>Volume</code> e <code>Pressure</code>."),
                    ("Non solo una volta", "Dopo il periodo euro lo sviluppo continuo: distanza, massa, temperatura, volume, pressione, tabelle di testo e conversioni telefoniche."),
                    ("Syscalculator 1.72 / 1.73 / 1.74", "La linea VB6 rimase utilizzabile, ma portava problemi Windows vecchi: percorsi, file lingua, <code>App.Path</code>, permessi e registro."),
                    ("Conversione VB6-.NET", "La conversione automatica da VB6 a .NET non fu la strada giusta. Forms, chiamate API, control arrays, <code>Load</code>/<code>Unload</code> e proprieta predefinite crearono troppi errori."),
                    ("Periodo conservato", "L'idea rimase, ma l'implementazione era troppo pesante. Il codice sorgente fu conservato finche non fu possibile ricostruire meglio."),
                    ("Nuovi paesi euro 2026", "Nel 2026 sono stati aggiunti nuovi NOD euro. La Bulgaria ha introdotto l'euro il 1 gennaio 2026 con il cambio fisso <code>1 EUR = 1.95583 BGN</code>, quindi <code>BGN.nod</code> appartiene al gruppo euro storico."),
                    ("Ricerca codici postali 2026", "Nel 2026 e in sviluppo una ricerca per codici postali. NOD si sposta cosi anche verso ricerca e collegamento di dati."),
                    ("Syscalculator 2.0 beta", "La nuova versione e ricostruita in C#/.NET: le vecchie regole NOD restano riconoscibili, con aiuto moderno, simulazione e Graph Preview.")
                },
                "Perche NOD e rimasto",
                "NOD conserva la conoscenza della conversione in testo semplice. Cosi numeri, valute, unita e formule restano comprensibili anche dopo anni.",
                "Lezione",
                "La conversione automatica VB6-.NET ha portato troppi problemi nascosti. Meglio capire il vecchio comportamento e ricostruire pulito."),
            "spa" => BuildLocalizedHistoryHelp(
                "Historia",
                "Syscalculator empezo antes de las versiones modernas de Windows. Sus raices estan en la epoca BBS, cuando las nodelists, telefonos y archivos de texto cambiaban con frecuencia.",
                "La idea era describir una conversion en un pequeno archivo de reglas legible, no escribir un programa nuevo para cada tabla.",
                new[]
                {
                    ("Epoca BBS", "Nodes, redes y nodelists eran el mapa de conexion. Los datos cambiaban a menudo y debian mantenerse ordenados. Hacian falta pequenas herramientas para leer, reconocer y convertir texto."),
                    ("Nodelist", "Una nodelist contenia sistemas, direcciones node y numeros telefonicos. Direcciones como <code>2:500/200</code> o <code>2:500/241</code> indicaban zona, red y node."),
                    ("Por que se llama NOD", "El nombre <code>NOD</code> viene de ese mundo. Datos de nodes, listas y numeros debian cambiarse sin revisar cientos de lineas a mano."),
                    ("Operacion Decibel 1995", "El 10 de octubre de 1995 PTT Telecom realizo la renumeracion nacional neerlandesa <code>Operation Decibel</code>. Los numeros fijos avanzaron hacia un formato nacional de diez digitos."),
                    ("Nodomzet y Decibel", "Esa renumeracion encajaba con la idea inicial de <code>Nodomzet</code>: leer datos de nodes, listas o numeros, reconocer el patron antiguo y escribir la nueva forma."),
                    ("Tabla historica", "El archivo <code>Operation Decibel 1995 historical complete</code> conserva la renumeracion como reglas de texto: prefijo antiguo a la izquierda, prefijo nuevo a la derecha."),
                    ("Del prefijo antiguo al numero nuevo", "Una regla como <code>chg \"01105-\",\"0113-35\"</code> reconoce el comienzo antiguo, lo reemplaza y conserva el resto del numero de abonado."),
                    ("Nodomzet DOS en QuickBASIC 4.5", "La primera version fue <code>Nodomzet 1.0</code> para DOS, desarrollada en <code>QuickBASIC 4.5</code>. Leia archivos de nodelist separados por comas y escribia los datos modificados."),
                    ("Syscalculator por la operacion euro", "<code>Syscalculator</code> surgio alrededor de la operacion euro de 1999, cuando NOD recibio <code>math</code> para conversion de monedas."),
                    ("Segundo comando: math", "En ese periodo llego el calculo: <code>math</code>. Primero para florines, euros y otras monedas, despues tambien para unidades."),
                    ("De conversor a lenguaje NOD", "Despues quedo claro que el principio era mas amplio: telefonos, monedas, tablas de texto y unidades podian describirse con reglas."),
                    ("Grupos NOD", "Los archivos sueltos crecieron en grupos como <code>euro</code>, <code>Temperature</code>, <code>Distance</code>, <code>Mass</code>, <code>Volume</code> y <code>Pressure</code>."),
                    ("No fue una sola vez", "Tras la epoca del euro el desarrollo continuo: distancia, masa, temperatura, volumen, presion, tablas de texto y conversiones telefonicas."),
                    ("Syscalculator 1.72 / 1.73 / 1.74", "La linea VB6 siguio siendo util, pero trajo problemas antiguos de Windows: rutas, archivos de idioma, <code>App.Path</code>, permisos y registro."),
                    ("Conversion VB6-.NET", "La conversion automatica de VB6 a .NET no fue buena ruta. Forms, llamadas API, control arrays, <code>Load</code>/<code>Unload</code> y propiedades por defecto causaron demasiados errores."),
                    ("Periodo conservado", "La idea siguio viva, pero la implementacion era demasiado pesada. El codigo fuente se conservo hasta poder reconstruir mejor."),
                    ("Nuevos paises euro 2026", "En 2026 se anadieron nuevos NOD de euro. Bulgaria introdujo el euro el 1 de enero de 2026 con el tipo fijo <code>1 EUR = 1.95583 BGN</code>, por lo que <code>BGN.nod</code> pertenece al grupo euro historico."),
                    ("Buscador postal 2026", "En 2026 hay un buscador de codigos postales en desarrollo. NOD avanza asi tambien hacia busqueda y conexion de datos."),
                    ("Syscalculator 2.0 beta", "La nueva version se reconstruye en C#/.NET: las reglas NOD antiguas siguen reconocibles, con ayuda moderna, simulacion y Graph Preview.")
                },
                "Por que NOD siguio",
                "NOD guarda la logica de conversion como texto normal. Por eso telefonos, monedas, unidades y formulas siguen siendo legibles anos despues.",
                "Leccion",
                "Convertir VB6 automaticamente a .NET produjo demasiada carga antigua. La mejor ruta es entender el comportamiento y reconstruir con limpieza."),
            "por" => BuildLocalizedHistoryHelp(
                "Historia",
                "Syscalculator comecou antes das versoes modernas do Windows. A origem esta na era BBS, quando nodelists, telefones e ficheiros de texto mudavam muitas vezes.",
                "A ideia era descrever uma conversao num pequeno ficheiro de regras legivel, em vez de criar um programa novo para cada tabela.",
                new[]
                {
                    ("Era BBS", "Nodes, redes e nodelists eram o mapa de ligacoes. Os dados mudavam muitas vezes e tinham de ficar organizados. Pequenas ferramentas para ler, reconhecer e converter texto eram necessarias."),
                    ("Nodelist", "Uma nodelist continha sistemas, enderecos node e numeros telefonicos. Enderecos como <code>2:500/200</code> ou <code>2:500/241</code> indicavam zona, rede e node."),
                    ("Porque se chama NOD", "O nome <code>NOD</code> vem desse mundo. Dados de nodes, listas e numeros tinham de mudar sem verificar centenas de linhas a mao."),
                    ("Operacao Decibel 1995", "Em 10 de outubro de 1995 a PTT Telecom fez a renumeracao nacional neerlandesa <code>Operation Decibel</code>. Os numeros fixos passaram para um formato nacional de dez digitos."),
                    ("Nodomzet e Decibel", "Essa renumeracao combinava com a ideia inicial de <code>Nodomzet</code>: ler dados de nodes, listas ou numeros, reconhecer o padrao antigo e escrever a nova forma."),
                    ("Tabela historica", "O ficheiro <code>Operation Decibel 1995 historical complete</code> guarda a renumeracao como regras de texto: prefixo antigo a esquerda, prefixo novo a direita."),
                    ("Do prefixo antigo ao novo numero", "Uma regra como <code>chg \"01105-\",\"0113-35\"</code> reconhece o inicio antigo, substitui-o e mantem o resto do numero."),
                    ("Nodomzet DOS em QuickBASIC 4.5", "A primeira versao foi <code>Nodomzet 1.0</code> para DOS, desenvolvida em <code>QuickBASIC 4.5</code>. Lia ficheiros de nodelist separados por virgulas e escrevia os dados modificados."),
                    ("Syscalculator pela operacao euro", "<code>Syscalculator</code> surgiu por volta da operacao euro de 1999, quando NOD recebeu <code>math</code> para conversao de moedas."),
                    ("Segundo comando: math", "Nesse periodo chegou o calculo: <code>math</code>. Primeiro para florins, euros e outras moedas, depois tambem para unidades."),
                    ("De conversor para linguagem NOD", "Depois ficou claro que o principio era mais amplo: telefones, moedas, tabelas de texto e unidades podiam ser descritos por regras."),
                    ("Grupos NOD", "Ficheiros soltos cresceram para grupos como <code>euro</code>, <code>Temperature</code>, <code>Distance</code>, <code>Mass</code>, <code>Volume</code> e <code>Pressure</code>."),
                    ("Nao ficou numa vez", "Depois da era euro o desenvolvimento continuou: distancia, massa, temperatura, volume, pressao, tabelas de texto e conversoes telefonicas."),
                    ("Syscalculator 1.72 / 1.73 / 1.74", "A linha VB6 continuou utilizavel, mas trouxe problemas antigos do Windows: caminhos, ficheiros de lingua, <code>App.Path</code>, permissoes e registo."),
                    ("Conversao VB6-.NET", "A conversao automatica de VB6 para .NET nao foi boa rota. Forms, chamadas API, control arrays, <code>Load</code>/<code>Unload</code> e propriedades por defeito causaram erros demais."),
                    ("Periodo preservado", "A ideia continuou, mas a implementacao era pesada demais. O codigo fonte foi guardado ate ser possivel reconstruir melhor."),
                    ("Novos paises euro 2026", "Em 2026 foram adicionados novos NODs do euro. A Bulgaria introduziu o euro em 1 de janeiro de 2026 com a taxa fixa <code>1 EUR = 1.95583 BGN</code>, por isso <code>BGN.nod</code> pertence ao grupo euro historico."),
                    ("Pesquisador postal 2026", "Em 2026 esta em desenvolvimento um pesquisador de codigos postais. NOD avanca tambem para pesquisa e ligacao de dados."),
                    ("Syscalculator 2.0 beta", "A nova versao e reconstruida em C#/.NET: regras NOD antigas continuam reconheciveis, com ajuda moderna, simulacao e Graph Preview.")
                },
                "Porque NOD ficou",
                "NOD guarda o conhecimento de conversao em texto normal. Assim numeros, moedas, unidades e formulas continuam compreensiveis anos depois.",
                "Licao",
                "Converter VB6 automaticamente para .NET trouxe demasiados problemas antigos. Melhor e entender o comportamento e reconstruir limpo."),
            "ind" => BuildLocalizedHistoryHelp(
                "Sejarah",
                "Syscalculator bermula sebelum versi Windows modern. Akarnya ada pada masa BBS, ketika nodelist, nomor telepon, dan file teks sering berubah.",
                "Idenya sederhana: tulis konversi sebagai file aturan kecil yang mudah dibaca, bukan membuat program baru untuk setiap tabel.",
                new[]
                {
                    ("Masa BBS", "Node, net, dan nodelist menjadi peta koneksi. Data sering berubah dan harus tetap rapi. Alat kecil untuk membaca, mengenali, dan mengubah teks menjadi kebutuhan praktis."),
                    ("Nodelist", "Nodelist berisi sistem, alamat node, dan nomor telepon. Alamat seperti <code>2:500/200</code> atau <code>2:500/241</code> berarti zone, net, dan node."),
                    ("Mengapa namanya NOD", "Nama <code>NOD</code> berasal dari dunia nodelist. Data node, daftar, dan nomor harus diubah tanpa memeriksa ratusan baris secara manual."),
                    ("Operasi Decibel 1995", "Pada 10 Oktober 1995 PTT Telecom melakukan renumbering nasional Belanda, <code>Operation Decibel</code>. Nomor tetap bergerak ke format nasional sepuluh digit."),
                    ("Nodomzet dan Decibel", "Renumbering itu cocok dengan ide awal <code>Nodomzet</code>: baca data node, daftar, atau nomor; kenali pola lama; lalu tulis kembali bentuk baru."),
                    ("Tabel historis", "File <code>Operation Decibel 1995 historical complete</code> menyimpan renumbering sebagai aturan teks: prefix lama di kiri, prefix baru di kanan."),
                    ("Dari prefix lama ke nomor baru", "Aturan seperti <code>chg \"01105-\",\"0113-35\"</code> mengenali awal lama, menggantinya, dan mempertahankan sisa nomor pelanggan."),
                    ("Nodomzet DOS di QuickBASIC 4.5", "Versi pertama adalah <code>Nodomzet 1.0</code> untuk DOS, dibuat dengan <code>QuickBASIC 4.5</code>. Program ini membaca file nodelist berbasis koma dan menulis kembali data yang sudah diubah."),
                    ("Syscalculator karena operasi euro", "<code>Syscalculator</code> muncul sekitar operasi euro 1999, ketika NOD mendapat <code>math</code> untuk konversi mata uang."),
                    ("Perintah kedua: math", "Pada masa itu perhitungan ditambahkan: <code>math</code>. Awalnya untuk gulden, euro, dan mata uang lain, lalu untuk satuan."),
                    ("Dari konverter ke bahasa NOD", "Setelah itu terlihat bahwa prinsipnya lebih luas: nomor telepon, mata uang, tabel teks, dan satuan bisa dijelaskan dengan aturan."),
                    ("Grup NOD", "File terpisah berkembang menjadi grup seperti <code>euro</code>, <code>Temperature</code>, <code>Distance</code>, <code>Mass</code>, <code>Volume</code>, dan <code>Pressure</code>."),
                    ("Tidak berhenti sekali", "Sesudah masa euro, pengembangan berlanjut: jarak, massa, suhu, volume, tekanan, tabel teks, dan konversi nomor telepon."),
                    ("Syscalculator 1.72 / 1.73 / 1.74", "Lini VB6 masih bisa dipakai, tetapi membawa masalah Windows lama: path, file bahasa, <code>App.Path</code>, izin, dan registry."),
                    ("Konversi VB6-.NET", "Konversi otomatis dari VB6 ke .NET bukan jalan yang baik. Forms, API calls, control arrays, <code>Load</code>/<code>Unload</code>, dan default properties memberi terlalu banyak error."),
                    ("Masa penyimpanan", "Idenya tetap ada, tetapi implementasinya terlalu berat. Source code disimpan sampai ada cara yang lebih baik untuk membangun ulang."),
                    ("Negara euro baru 2026", "Pada 2026 NOD euro baru ditambahkan. Bulgaria memakai euro pada 1 Januari 2026 dengan kurs tetap <code>1 EUR = 1.95583 BGN</code>, sehingga <code>BGN.nod</code> masuk grup euro historis."),
                    ("Pencari kode pos 2026", "Pada 2026 pencari kode pos sedang dikembangkan. NOD bergerak juga ke pencarian dan pengaitan data."),
                    ("Syscalculator 2.0 beta", "Versi baru dibangun ulang dengan C#/.NET: aturan NOD lama tetap dikenali, dengan bantuan modern, simulasi, dan Graph Preview.")
                },
                "Mengapa NOD tetap dipakai",
                "NOD menyimpan pengetahuan konversi sebagai teks biasa. Karena itu nomor, mata uang, satuan, dan rumus tetap dapat dipahami di kemudian hari.",
                "Pelajaran",
                "Konversi otomatis VB6 ke .NET membawa terlalu banyak beban lama. Jalan yang lebih baik adalah memahami perilaku lama dan membangun ulang dengan bersih."),
            "zho" => BuildLocalizedHistoryHelp(
                "历史",
                "Syscalculator 的来源早于现代 Windows 版本。它来自 BBS 时代，那时 nodelist、电话号码和文本文件经常需要更新。",
                "核心想法很简单：用一个小而可读的规则文件描述转换，而不是为每张表重新写程序。",
                new[]
                {
                    ("BBS 时代", "node、net 和 nodelist 是系统之间的路线图。数据经常变化，列表必须保持整齐。能读取、识别和转换文本的小工具非常实用。"),
                    ("Nodelist", "nodelist 包含系统、node 地址和电话号码。<code>2:500/200</code> 或 <code>2:500/241</code> 这样的地址表示 zone、net 和 node。"),
                    ("为什么叫 NOD", "<code>NOD</code> 这个名字来自 nodelist 世界。node、列表和号码数据需要改变，但没人想手工检查几百行。"),
                    ("1995 Operation Decibel", "1995 年 10 月 10 日，PTT Telecom 执行荷兰全国电话号码重编号 <code>Operation Decibel</code>。固定电话逐步变成全国十位格式。"),
                    ("Nodomzet 与 Decibel", "这次重编号正好符合 <code>Nodomzet</code> 的早期想法：读取 node、列表或号码数据，识别旧模式，再写回新形式。"),
                    ("历史重编号表", "<code>Operation Decibel 1995 historical complete</code> 文件把重编号保存为文本规则：左边是旧前缀，右边是新前缀。"),
                    ("从旧前缀到新号码", "规则 <code>chg \"01105-\",\"0113-35\"</code> 表示识别旧开头，替换为新开头，并保留用户号码的其余部分。"),
                    ("QuickBASIC 4.5 中的 Nodomzet DOS", "第一个版本不是 Windows 程序，而是 DOS 转换器 <code>Nodomzet 1.0</code>，使用 <code>QuickBASIC 4.5</code> 开发，用于读取 nodelist 逗号文件并写回修改后的数据。"),
                    ("欧元操作产生 Syscalculator", "1999 年欧元转换时期，NOD 增加了用于货币换算的 <code>math</code>，<code>Syscalculator</code> 因此成为由 NOD 文件驱动的 Windows 程序。"),
                    ("第二个命令：math", "在 Syscalculator 阶段加入了计算：<code>math</code>。最初用于荷兰盾、欧元和其他货币，后来也用于单位。"),
                    ("从转换器到 NOD 语言", "之后可以看出，同一原则更广：电话号码、货币、文本表和单位都可以用规则描述。"),
                    ("NOD 分组", "单独文件逐渐形成 <code>euro</code>、<code>Temperature</code>、<code>Distance</code>、<code>Mass</code>、<code>Volume</code>、<code>Pressure</code> 等分组。"),
                    ("不只是一次性工具", "欧元时期之后开发继续：距离、质量、温度、体积、压力、文本表和电话号码转换都加入进来。"),
                    ("Syscalculator 1.72 / 1.73 / 1.74", "VB6 版本仍然可用，但带有旧 Windows 问题：路径、语言文件、<code>App.Path</code>、权限和注册表设置。"),
                    ("VB6-.NET 转换尝试", "后来尝试把旧 VB6 代码自动转换到 .NET，但 forms、API 调用、control arrays、<code>Load</code>/<code>Unload</code> 和默认属性带来太多错误。"),
                    ("保存时期", "想法保留下来，但实现太沉重。源代码被保存，直到有更好的方式重新理解和重建系统。"),
                    ("2026 新欧元国家", "2026 年又加入新的欧元 NOD。保加利亚在 2026 年 1 月 1 日采用欧元，固定汇率为 <code>1 EUR = 1.95583 BGN</code>，所以 <code>BGN.nod</code> 属于历史 euro 分组。"),
                    ("2026 邮编查询器", "2026 年邮编查询器正在开发中。NOD 因此从重编号、计算和单位转换继续走向数据查询和连接。"),
                    ("Syscalculator 2.0 beta", "新版本用 C#/.NET 重新构建：旧 NOD 规则仍然可识别，同时加入现代帮助、模拟器和 Graph Preview。")
                },
                "为什么保留 NOD",
                "NOD 把转换知识保存在普通文本中，而不是藏在程序代码里。所以多年以后仍能理解电话号码、货币、单位和公式的转换原因。",
                "经验",
                "直接把 VB6 自动转换到 .NET 会带来太多旧问题。更好的方式是理解旧行为，保留必要兼容性，并干净地重建。"),
            _ => fallback
        };

    private static string BuildLocalizedHistoryHelp(
        string title,
        string intro,
        string principle,
        (string Title, string Text)[] steps,
        string whyTitle,
        string why,
        string lessonTitle,
        string lesson)
    {
        var timeline = new StringBuilder();
        foreach (var step in steps)
            timeline.AppendLine($"""          <div class="history-step"><b>{step.Title}</b><span>{step.Text}</span></div>""");

        return $"""
        <h2>{title}</h2>
        <p>{intro}</p>
        <p>{principle}</p>
        <div class="history-timeline">
        {timeline}        </div>
        <h2>{whyTitle}</h2>
        <p>{why}</p>
        <h2>{lessonTitle}</h2>
        <p>{lesson}</p>
        """;
    }

    private string LocalizeProbabilityHelp(string fallback)
        => HelpLanguageCode() switch
        {
            "eng" => BuildLocalizedProbabilityHelp(
                "Probability",
                "NOD 2.0 supports factorial and common probability functions in <code>math</code>.",
                "Factorial", "Combinations", "Permutations", "Expected value",
                "If the input is <code>5</code>, then <code>ans = 5</code> and NOD calculates <code>5!</code>.",
                "If the input is <code>5</code>, then <code>ans = n = 5</code> and <code>r = 2</code>: choose 2 from 5.",
                "If the input is <code>5</code>, NOD calculates <code>P(5,2)</code>. Use <code>perm(ans,3)</code> for 3 places.",
                "The function reads pairs: value, probability, value, probability.",
                "Goal", "Syntax", "Example", "gives",
                "Note:", "inside functions, commas separate arguments. Use a dot for decimals there, for example <code>0.5</code>. Outside functions the decimal comma still works, such as <code>math ans / 1,95583</code>.",
                "Example: combinations", "Example: expected value", "The input is <code>n</code>; <code>r</code> is fixed at <code>2</code> in this converter.", "<code>E</code> or <code>e</code> is still Euler's constant; for expected value use <code>expected</code>."),
            "deu" => BuildLocalizedProbabilityHelp(
                "Wahrscheinlichkeit",
                "NOD 2.0 unterstutzt Fakultat und haufige Wahrscheinlichkeitsfunktionen in <code>math</code>.",
                "Fakultat", "Kombinationen", "Permutationen", "Erwartungswert",
                "Wenn die Eingabe <code>5</code> ist, gilt <code>ans = 5</code> und NOD berechnet <code>5!</code>.",
                "Wenn die Eingabe <code>5</code> ist, gilt <code>ans = n = 5</code> und <code>r = 2</code>: wahle 2 aus 5.",
                "Wenn die Eingabe <code>5</code> ist, berechnet NOD <code>P(5,2)</code>. Nutze <code>perm(ans,3)</code> fur 3 Platze.",
                "Die Funktion liest Paare: Wert, Wahrscheinlichkeit, Wert, Wahrscheinlichkeit.",
                "Ziel", "Syntax", "Beispiel", "ergibt",
                "Achtung:", "innerhalb von Funktionen trennt das Komma Argumente. Verwende dort einen Punkt fur Dezimalzahlen, z. B. <code>0.5</code>. Ausserhalb von Funktionen funktioniert die Dezimalkomma-Schreibweise weiter, z. B. <code>math ans / 1,95583</code>.",
                "Beispiel: Kombinationen", "Beispiel: Erwartungswert", "Die Eingabe ist <code>n</code>; <code>r</code> ist in diesem Konverter fest <code>2</code>.", "<code>E</code> oder <code>e</code> bleibt die Euler-Konstante; fur Erwartungswert nutze <code>expected</code>."),
            "fra" => BuildLocalizedProbabilityHelp(
                "Probabilites",
                "NOD 2.0 prend en charge la factorielle et les fonctions courantes de probabilite dans <code>math</code>.",
                "Factorielle", "Combinaisons", "Permutations", "Esperance",
                "Si l'entree est <code>5</code>, alors <code>ans = 5</code> et NOD calcule <code>5!</code>.",
                "Si l'entree est <code>5</code>, alors <code>ans = n = 5</code> et <code>r = 2</code>: choisir 2 parmi 5.",
                "Si l'entree est <code>5</code>, NOD calcule <code>P(5,2)</code>. Utilisez <code>perm(ans,3)</code> pour 3 places.",
                "La fonction lit des paires: valeur, probabilite, valeur, probabilite.",
                "But", "Syntaxe", "Exemple", "donne",
                "Attention:", "dans les fonctions, la virgule separe les arguments. Utilisez un point pour les decimales, par exemple <code>0.5</code>. Hors fonction, la virgule decimale reste possible, comme <code>math ans / 1,95583</code>.",
                "Exemple: combinaisons", "Exemple: esperance", "L'entree est <code>n</code>; <code>r</code> vaut ici toujours <code>2</code>.", "<code>E</code> ou <code>e</code> reste la constante d'Euler; pour l'esperance utilisez <code>expected</code>."),
            "ita" => BuildLocalizedProbabilityHelp(
                "Probabilita",
                "NOD 2.0 supporta fattoriale e funzioni comuni di probabilita in <code>math</code>.",
                "Fattoriale", "Combinazioni", "Permutazioni", "Valore atteso",
                "Se l'input e <code>5</code>, allora <code>ans = 5</code> e NOD calcola <code>5!</code>.",
                "Se l'input e <code>5</code>, allora <code>ans = n = 5</code> e <code>r = 2</code>: scegli 2 da 5.",
                "Se l'input e <code>5</code>, NOD calcola <code>P(5,2)</code>. Usa <code>perm(ans,3)</code> per 3 posti.",
                "La funzione legge coppie: valore, probabilita, valore, probabilita.",
                "Scopo", "Sintassi", "Esempio", "da",
                "Nota:", "nelle funzioni la virgola separa gli argomenti. Usa il punto per i decimali, per esempio <code>0.5</code>. Fuori dalle funzioni la virgola decimale continua a funzionare, come <code>math ans / 1,95583</code>.",
                "Esempio: combinazioni", "Esempio: valore atteso", "L'input e <code>n</code>; in questo convertitore <code>r</code> e fissato a <code>2</code>.", "<code>E</code> o <code>e</code> resta la costante di Eulero; per il valore atteso usa <code>expected</code>."),
            "spa" => BuildLocalizedProbabilityHelp(
                "Probabilidad",
                "NOD 2.0 admite factorial y funciones comunes de probabilidad en <code>math</code>.",
                "Factorial", "Combinaciones", "Permutaciones", "Valor esperado",
                "Si la entrada es <code>5</code>, entonces <code>ans = 5</code> y NOD calcula <code>5!</code>.",
                "Si la entrada es <code>5</code>, entonces <code>ans = n = 5</code> y <code>r = 2</code>: elegir 2 de 5.",
                "Si la entrada es <code>5</code>, NOD calcula <code>P(5,2)</code>. Usa <code>perm(ans,3)</code> para 3 posiciones.",
                "La funcion lee pares: valor, probabilidad, valor, probabilidad.",
                "Objetivo", "Sintaxis", "Ejemplo", "da",
                "Nota:", "dentro de funciones, la coma separa argumentos. Usa punto para decimales, por ejemplo <code>0.5</code>. Fuera de funciones sigue funcionando la coma decimal, como <code>math ans / 1,95583</code>.",
                "Ejemplo: combinaciones", "Ejemplo: valor esperado", "La entrada es <code>n</code>; <code>r</code> queda fijo en <code>2</code> en este conversor.", "<code>E</code> o <code>e</code> sigue siendo la constante de Euler; para valor esperado usa <code>expected</code>."),
            "por" => BuildLocalizedProbabilityHelp(
                "Probabilidade",
                "NOD 2.0 suporta fatorial e funcoes comuns de probabilidade em <code>math</code>.",
                "Fatorial", "Combinacoes", "Permutacoes", "Valor esperado",
                "Se a entrada for <code>5</code>, entao <code>ans = 5</code> e NOD calcula <code>5!</code>.",
                "Se a entrada for <code>5</code>, entao <code>ans = n = 5</code> e <code>r = 2</code>: escolher 2 de 5.",
                "Se a entrada for <code>5</code>, NOD calcula <code>P(5,2)</code>. Use <code>perm(ans,3)</code> para 3 lugares.",
                "A funcao le pares: valor, probabilidade, valor, probabilidade.",
                "Objetivo", "Sintaxe", "Exemplo", "da",
                "Nota:", "dentro de funcoes, a virgula separa argumentos. Use ponto para decimais, por exemplo <code>0.5</code>. Fora das funcoes a virgula decimal continua a funcionar, como <code>math ans / 1,95583</code>.",
                "Exemplo: combinacoes", "Exemplo: valor esperado", "A entrada e <code>n</code>; neste conversor <code>r</code> fica fixo em <code>2</code>.", "<code>E</code> ou <code>e</code> continua a ser a constante de Euler; para valor esperado use <code>expected</code>."),
            "ind" => BuildLocalizedProbabilityHelp(
                "Peluang",
                "NOD 2.0 mendukung faktorial dan fungsi peluang umum di <code>math</code>.",
                "Faktorial", "Kombinasi", "Permutasi", "Nilai harapan",
                "Jika input <code>5</code>, maka <code>ans = 5</code> dan NOD menghitung <code>5!</code>.",
                "Jika input <code>5</code>, maka <code>ans = n = 5</code> dan <code>r = 2</code>: pilih 2 dari 5.",
                "Jika input <code>5</code>, NOD menghitung <code>P(5,2)</code>. Gunakan <code>perm(ans,3)</code> untuk 3 posisi.",
                "Fungsi membaca pasangan: nilai, peluang, nilai, peluang.",
                "Tujuan", "Sintaks", "Contoh", "menghasilkan",
                "Catatan:", "di dalam fungsi, koma memisahkan argumen. Gunakan titik untuk desimal, misalnya <code>0.5</code>. Di luar fungsi, koma desimal tetap bekerja, seperti <code>math ans / 1,95583</code>.",
                "Contoh: kombinasi", "Contoh: nilai harapan", "Input adalah <code>n</code>; <code>r</code> tetap <code>2</code> dalam converter ini.", "<code>E</code> atau <code>e</code> tetap konstanta Euler; untuk nilai harapan gunakan <code>expected</code>."),
            "zho" => BuildLocalizedProbabilityHelp(
                "概率",
                "NOD 2.0 在 <code>math</code> 中支持阶乘和常用概率函数。",
                "阶乘", "组合", "排列", "期望值",
                "如果输入是 <code>5</code>，则 <code>ans = 5</code>，NOD 计算 <code>5!</code>。",
                "如果输入是 <code>5</code>，则 <code>ans = n = 5</code>，<code>r = 2</code>：从 5 个中选 2 个。",
                "如果输入是 <code>5</code>，NOD 计算 <code>P(5,2)</code>。使用 <code>perm(ans,3)</code> 表示 3 个位置。",
                "函数按成对参数读取：值、概率、值、概率。",
                "目的", "语法", "示例", "得到",
                "注意：", "在函数内部，逗号分隔参数。小数请使用点，例如 <code>0.5</code>。在函数外仍可使用小数逗号，例如 <code>math ans / 1,95583</code>。",
                "示例：组合", "示例：期望值", "输入是 <code>n</code>；在这个转换器中 <code>r</code> 固定为 <code>2</code>。", "<code>E</code> 或 <code>e</code> 仍表示欧拉常数；期望值请使用 <code>expected</code>。"),
            _ => fallback
        };

    private static string BuildLocalizedProbabilityHelp(
        string title,
        string intro,
        string factorialTitle,
        string combinationsTitle,
        string permutationsTitle,
        string expectedTitle,
        string factorialExplanation,
        string combinationsExplanation,
        string permutationsExplanation,
        string expectedExplanation,
        string goalHeader,
        string syntaxHeader,
        string exampleHeader,
        string givesWord,
        string noteLabel,
        string noteText,
        string combinationsExampleTitle,
        string expectedExampleTitle,
        string combinationsExampleExplanation,
        string expectedExampleExplanation)
        => $"""
        <details><summary>{title}</summary>
        <p>{intro}</p>
        <div class="example-grid">
          <div class="example-card"><h3>{factorialTitle}</h3><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mi>n</mi><mo>!</mo><mo>=</mo><mn>1</mn><mo>&#x00D7;</mo><mn>2</mn><mo>&#x00D7;</mo><mo>...</mo><mo>&#x00D7;</mo><mi>n</mi></mrow></math></div><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mtext>ans</mtext><mo>=</mo><mi>n</mi></mrow></math></div><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mn>5</mn><mo>!</mo><mo>=</mo><mn>1</mn><mo>&#x00D7;</mo><mn>2</mn><mo>&#x00D7;</mo><mn>3</mn><mo>&#x00D7;</mo><mn>4</mn><mo>&#x00D7;</mo><mn>5</mn><mo>=</mo><mn>120</mn></mrow></math></div><pre>math ans!</pre><p>{factorialExplanation}</p></div>
          <div class="example-card"><h3>{combinationsTitle}</h3><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mrow><mo>(</mo><mfrac linethickness="0"><mi>n</mi><mi>r</mi></mfrac><mo>)</mo></mrow><mo>=</mo><mfrac><mrow><mi>n</mi><mo>!</mo></mrow><mrow><mi>r</mi><mo>!</mo><mo>(</mo><mi>n</mi><mo>-</mo><mi>r</mi><mo>)</mo><mo>!</mo></mrow></mfrac></mrow></math></div><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mtext>ans</mtext><mo>=</mo><mi>n</mi><mo>,</mo><mspace width="0.4em"/><mn>2</mn><mo>=</mo><mi>r</mi></mrow></math></div><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mrow><mo>(</mo><mfrac linethickness="0"><mn>5</mn><mn>2</mn></mfrac><mo>)</mo></mrow><mo>=</mo><mfrac><mrow><mn>5</mn><mo>!</mo></mrow><mrow><mn>2</mn><mo>!</mo><mn>3</mn><mo>!</mo></mrow></mfrac><mo>=</mo><mn>10</mn></mrow></math></div><pre>math comb(ans,2)</pre><p>{combinationsExplanation}</p></div>
          <div class="example-card"><h3>{permutationsTitle}</h3><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mi>P</mi><mo>(</mo><mi>n</mi><mo>,</mo><mi>r</mi><mo>)</mo><mo>=</mo><mfrac><mrow><mi>n</mi><mo>!</mo></mrow><mrow><mo>(</mo><mi>n</mi><mo>-</mo><mi>r</mi><mo>)</mo><mo>!</mo></mrow></mfrac></mrow></math></div><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mtext>ans</mtext><mo>=</mo><mi>n</mi><mo>,</mo><mspace width="0.4em"/><mn>2</mn><mo>=</mo><mi>r</mi></mrow></math></div><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mi>P</mi><mo>(</mo><mn>5</mn><mo>,</mo><mn>2</mn><mo>)</mo><mo>=</mo><mfrac><mrow><mn>5</mn><mo>!</mo></mrow><mrow><mn>3</mn><mo>!</mo></mrow></mfrac><mo>=</mo><mn>20</mn></mrow></math></div><pre>math perm(ans,2)</pre><p>{permutationsExplanation}</p></div>
          <div class="example-card"><h3>{expectedTitle}</h3><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mi>E</mi><mo>(</mo><mi>X</mi><mo>)</mo><mo>=</mo><munderover><mo>&#x2211;</mo><mi>i</mi><mi>n</mi></munderover><msub><mi>x</mi><mi>i</mi></msub><mo>&#x00D7;</mo><msub><mi>p</mi><mi>i</mi></msub></mrow></math></div><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><msub><mi>x</mi><mn>1</mn></msub><mo>=</mo><mn>0</mn><mo>,</mo><mspace width="0.4em"/><msub><mi>p</mi><mn>1</mn></msub><mo>=</mo><mn>0.5</mn><mo>,</mo><mspace width="0.4em"/><msub><mi>x</mi><mn>2</mn></msub><mo>=</mo><mn>10</mn><mo>,</mo><mspace width="0.4em"/><msub><mi>p</mi><mn>2</mn></msub><mo>=</mo><mn>0.5</mn></mrow></math></div><div class="mathml-formula"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline"><mrow><mi>E</mi><mo>(</mo><mi>X</mi><mo>)</mo><mo>=</mo><mn>0</mn><mo>&#x00D7;</mo><mn>0.5</mn><mo>+</mo><mn>10</mn><mo>&#x00D7;</mo><mn>0.5</mn><mo>=</mo><mn>5</mn></mrow></math></div><pre>math expected(0,0.5,10,0.5)</pre><p>{expectedExplanation}</p></div>
        </div>
        <table>
          <tr><th>{goalHeader}</th><th>{syntaxHeader}</th><th>{exampleHeader}</th></tr>
          <tr><td>{factorialTitle}</td><td><code>math ans!</code> / <code>math factorial(ans)</code></td><td><code>5!</code> {givesWord} <code>120</code>.</td></tr>
          <tr><td>{combinationsTitle}</td><td><code>math comb(n,r)</code> / <code>math ncr(n,r)</code></td><td><code>comb(5,2)</code> {givesWord} <code>10</code>.</td></tr>
          <tr><td>{permutationsTitle}</td><td><code>math perm(n,r)</code> / <code>math npr(n,r)</code></td><td><code>perm(5,2)</code> {givesWord} <code>20</code>.</td></tr>
          <tr><td>{expectedTitle}</td><td><code>math expected(x,p,...)</code></td><td><code>expected(0,0.5,10,0.5)</code> {givesWord} <code>5</code>.</td></tr>
        </table>
        <div class="notice"><b>{noteLabel}</b> {noteText}</div>
        <div class="example-grid">
          <div class="example-card"><h3>{combinationsExampleTitle}</h3><pre>Name NCr r2
        input1 n
        input2 nCr r=2
        format #.00
        math comb(ans,2)
        end</pre><p>{combinationsExampleExplanation}</p></div>
          <div class="example-card"><h3>{expectedExampleTitle}</h3><pre>Name E
        input1 x
        input2 E
        format #.00
        math expected(0,0.5,10,0.5)
        end</pre><p>{expectedExampleExplanation}</p></div>
        </div>
        </details>
        """;

    private static string RemoveMathFlowPills(string html)
        => Regex.Replace(
            html,
            @"\s*<div\s+class=""math-flow"">.*?</div>\s*",
            Environment.NewLine,
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

    // Zoek/commentaar: Breidt reverse-help uit met NOD 2.0 terugrekenvoorbeelden.
    private string BuildReverseCommandHelp(string keyword)
    {
        if (!keyword.Equals("reverse", StringComparison.OrdinalIgnoreCase))
            return "";

        return T("editor.nod_help.reverse.extra", """
        <h2>Wanneer gebruik je reverse?</h2>
        <p><code>reverse</code> is bedoeld voor terugrekenen als een formule niet vanzelf duidelijk omkeerbaar is. In NOD 2.0 kun je hiermee de tweede richting van een converter expliciet beschrijven.</p>
        <div class="notice"><b>Vuistregel:</b> gebruik <code>reverse</code> bij samengestelde <code>math</code>-regels, haakjes, meerdere stappen of vaste correcties. Voor simpele één-stap berekeningen is het vaak niet nodig.</div>

        <details open><summary>Basisvoorbeeld: één stap terug</summary>
        <p>Als vooruit vermenigvuldigt, deelt reverse terug.</p>
        <pre>Name Meter naar centimeter
        input1 Meter
        input2 Centimeter
        format #.00
        math ans * 100
        reverse ans / 100
        end</pre>
        </details>

        <details><summary>Meerdere math-stappen terugrekenen</summary>
        <p>Bij meerdere stappen schrijf je de omgekeerde volgorde zelf in één duidelijke reverse-regel.</p>
        <pre>Name Celsius naar Fahrenheit
        input1 Celsius
        input2 Fahrenheit
        format #.00
        math ans * 1,8
        math ans + 32
        reverse (ans - 32) / 1,8
        end</pre>
        <p>Vooruit: eerst keer 1,8, daarna plus 32. Terug: eerst min 32, daarna delen door 1,8.</p>
        </details>

        <details><summary>Complexe formule met haakjes</summary>
        <p>Gebruik haakjes om precies te laten zien welke volgorde bij terugrekenen hoort.</p>
        <pre>Name Waarde met correctie
        input1 Basis
        input2 Uitkomst
        format #.00
        math ((ans + 10) * 2) / 3
        reverse ((ans * 3) / 2) - 10
        end</pre>
        <div class="notice"><b>Let op:</b> <code>reverse</code> is een rekenregel, geen automatische algebra-uitleg. Test altijd beide richtingen.</div>
        </details>
        """);
    }

    // Zoek/commentaar: Breidt tekstcommands zoals chg en trans uit met echte voorbeeldpatronen.
    private string BuildTextCommandHelp(string keyword)
    {
        if (keyword.Equals("chg", StringComparison.OrdinalIgnoreCase))
        {
            return T("editor.nod_help.chg.extra", """
            <h2>chg voorbeelden</h2>
            <p><code>chg</code> verandert vooral het begin van een tekst. Dat is handig voor telefoonnummers, codes, prefixes en oude nummerplannen.</p>
            <div class="notice"><b>Structuur:</b> links staat wat gevonden moet worden, rechts staat de nieuwe vorm. Gebruik aanhalingstekens als de waarde spaties, komma's of speciale tekens bevat.</div>
            <details open><summary>Prefix vervangen</summary>
            <table>
              <tr><th>Doel</th><th>Regel</th><th>Voorbeeld</th></tr>
              <tr><td>Oude prefix naar nieuw</td><td><code>chg "01751","07051"</code></td><td><code>0175112345</code> wordt <code>0705112345</code></td></tr>
              <tr><td>Met streepje</td><td><code>chg "01751-","070-51"</code></td><td><code>01751-12345</code> wordt <code>070-5112345</code></td></tr>
              <tr><td>Langste match wint</td><td>zet specifieke regels boven algemeen</td><td><code>01751-9xxxx</code> kan apart behandeld worden</td></tr>
            </table>
            <div class="example-grid">
              <div class="example-card"><h3>Voorbeeld: Den Haag 070</h3><pre>Name Omnummering Den Haag
            input1 Oud telefoonnummer
            input2 Nieuw telefoonnummer
            chg "01751-xxxxx","070-51xxxxx"
            chg "01750-xxxxx","070-50xxxxx"
            end</pre></div>
              <div class="example-card"><h3>Voorbeeld: artikelcode</h3><pre>Name Artikelcode vernieuwen
            input1 Oude code
            input2 Nieuwe code
            chg "OLD-","NEW-"
            end</pre></div>
            </div>
            </details>

            <details><summary>Patroon met x</summary>
            <p>Gebruik <code>x</code> als plaats voor tekens die behouden moeten blijven. Dat lijkt op oude omnummeringstabellen.</p>
            <pre>chg "050-2xxxxx","050-52xxxxx"</pre>
            <div class="example-grid">
              <div class="example-card"><h3>Voorbeeld: patroonregel</h3><pre>Name Telefoonpatroon
            input1 Oud
            input2 Nieuw
            chg "050-2xxxxx","050-52xxxxx"
            end</pre></div>
            </div>
            </details>

            <details><summary>Veelgemaakte chg-fouten</summary>
            <ul>
              <li>Te algemene regel boven een specifieke regel zetten.</li>
              <li>Komma vergeten tussen oud en nieuw.</li>
              <li>Spaties of komma's gebruiken zonder aanhalingstekens.</li>
              <li><code>chg</code> gebruiken terwijl je eigenlijk een exacte vertaling nodig hebt; gebruik dan <code>trans</code>.</li>
            </ul>
            </details>
            """);
        }

        if (keyword.Equals("trans", StringComparison.OrdinalIgnoreCase))
        {
            return T("editor.nod_help.trans.extra", """
            <h2>trans voorbeelden</h2>
            <p><code>trans</code> vertaalt een complete waarde. Het is bedoeld voor woorden, vaste codes, statussen, postcodes of korte lijsten.</p>
            <div class="notice"><b>Structuur:</b> <code>trans "oude waarde","nieuwe waarde"</code>. De hele invoer moet overeenkomen met de linkerwaarde.</div>
            <details open><summary>Exacte tekst vertalen</summary>
            <table>
              <tr><th>Doel</th><th>Regel</th><th>Resultaat</th></tr>
              <tr><td>Woord vertalen</td><td><code>trans "hallo","hello"</code></td><td><code>hallo</code> wordt <code>hello</code></td></tr>
              <tr><td>Status vertalen</td><td><code>trans "open","in behandeling"</code></td><td><code>open</code> wordt <code>in behandeling</code></td></tr>
              <tr><td>Adres opzoeken</td><td><code>trans "2566 GB 241","Nieboerweg 241, 2566 GB Den Haag"</code></td><td>postcode + huisnummer wordt adres</td></tr>
            </table>
            <div class="example-grid">
              <div class="example-card"><h3>Voorbeeld: Nederlands naar Engels</h3><pre>Name Nederlands Engels demo
            input1 Nederlands
            input2 Engels
            trans "hallo","hello"
            trans "dank je","thank you"
            trans "goedemorgen","good morning"
            end</pre></div>
              <div class="example-card"><h3>Voorbeeld: postcode demo</h3><pre>Name Postcode naar adres demo
            input1 Postcode huisnummer
            input2 Adres
            trans "2566 GB 241","Nieboerweg 241, 2566 GB Den Haag"
            end</pre></div>
            </div>
            </details>

            <details><summary>Wanneer trans, wanneer chg?</summary>
            <table>
              <tr><th>Gebruik</th><th>Als</th><th>Voorbeeld</th></tr>
              <tr><td><code>trans</code></td><td>de hele invoer precies bekend is</td><td><code>hallo</code> naar <code>hello</code></td></tr>
              <tr><td><code>chg</code></td><td>alleen het begin/patroon verandert</td><td><code>01751-xxxxx</code> naar <code>070-51xxxxx</code></td></tr>
            </table>
            </details>

            <details><summary>Veelgemaakte trans-fouten</summary>
            <ul>
              <li>Richting omdraaien: links is invoer, rechts is uitvoer.</li>
              <li>Een gedeeltelijke match verwachten. Daarvoor is <code>chg</code> beter.</li>
              <li>Een komma in de waarde gebruiken zonder aanhalingstekens.</li>
              <li>Dubbele vertalingen voor dezelfde invoer maken; de eerste duidelijke regel is het best.</li>
            </ul>
            </details>
            """);
        }

        return "";
    }

    // Legt uit wat NOD 2.0 slimmer doet met trans zonder NOD 1.0-regels te breken.
    private string BuildImprovedTransHelp(string keyword)
    {
        if (!keyword.Equals("trans", StringComparison.OrdinalIgnoreCase))
            return "";

        return T("editor.nod_help.trans.improved", """
        <h2>Verbeterde trans: slimmer dan NOD 1.0</h2>
        <div class="notice improved"><b>Compatibel:</b> klassieke NOD 1.0 <code>trans</code> blijft eerst exact zoeken. NOD 2.0 probeert daarna pas een slimme compacte match.</div>
        <table class="syntax-compare">
          <tr><th>Gedrag</th><th>NOD 1.0</th><th>NOD 2.0</th><th>Voorbeeld</th></tr>
          <tr><td>Exacte waarde</td><td><span class="yes">✓</span></td><td><span class="yes">✓</span></td><td><code>trans "hallo","hello"</code></td></tr>
          <tr><td>Hoofdletters negeren</td><td><span class="no">×</span></td><td><span class="yes">✓</span></td><td><code>Hallo</code> matcht <code>hallo</code></td></tr>
          <tr><td>Spaties, punten, streepjes en underscore negeren</td><td><span class="no">×</span></td><td><span class="yes">✓</span></td><td><code>dank_je</code>, <code>dank je</code> en <code>dank-je</code> kunnen dezelfde trans-regel raken</td></tr>
          <tr><td>Dubbelzinnige slimme match</td><td><span class="no">×</span></td><td><span class="yes">✓</span></td><td>Bij twee compacte matches wordt niet stil verkeerd vertaald; de invoer blijft staan.</td></tr>
        </table>
        <div class="example-grid">
          <div class="example-card"><h3>NOD 1.0: klassiek exact</h3><pre>Name Klassieke trans
        input1 Nederlands
        input2 Engels
        trans "hallo","hello"
        end</pre><p>Bedoeld voor vaste waarden. Exacte regels blijven de voorkeur houden.</p></div>
          <div class="example-card"><h3>NOD 2.0: verbeterde trans</h3><pre>Name Slimme trans
        input1 Nederlands
        input2 Engels
        trans "dank je","thank you"
        trans "goedemorgen","good morning"
        end</pre><p><code>dank_je</code>, <code>DANK-JE</code> en <code>dank je</code> kunnen allemaal <code>thank you</code> geven.</p></div>
        </div>
        <p><b>Vuistregel:</b> gebruik <a class="cmd-link" href="nodpage:cmd:trans"><code>trans</code></a> voor complete waarden. Gebruik <a class="cmd-link" href="nodpage:cmd:chg"><code>chg</code></a> wanneer alleen een beginstuk of patroon verandert.</p>
        """);
    }

    // Zoek/commentaar: Breidt mode-help uit met de hoofdsoorten NOD-bestanden.
    private string BuildModeCommandHelp(string keyword)
    {
        if (!keyword.Equals("mode", StringComparison.OrdinalIgnoreCase))
            return "";

        return T("editor.nod_help.mode.extra", """
        <h2>Wanneer gebruik je mode?</h2>
        <p><code>mode</code> vertelt welk soort NOD-bestand je maakt. Voor gewone omrekentools is <code>mode</code> meestal niet nodig; zonder mode werkt het als normale converter.</p>
        <div class="notice"><b>Vuistregel:</b> gebruik <code>mode</code> alleen als het bestand een speciaal type heeft, zoals data-verwerking of vergelijkingen oplossen.</div>
        <div class="notice"><b>Niet in deze versie:</b> <code>mode geometry</code>, <code>math geometry</code> en <code>mode matrix3x3</code> worden niet ondersteund in NOD 2.0 beta. Alleen limited matrix 2x2 als formulekaart/uitleg is toegestaan.</div>
        <details open><summary>Veelgebruikte modes</summary>
        <table>
          <tr><th>Mode</th><th>Status</th><th>Gebruik</th><th>Belangrijke commands</th></tr>
          <tr><td><code>mode calculator</code></td><td>Ondersteund</td><td>Gewone converter of rekenhulp.</td><td><code>math</code>, <code>trans</code>, <code>chg</code></td></tr>
          <tr><td><code>mode data</code></td><td>Ondersteund</td><td>Rijen/velden in een tabel verwerken.</td><td><code>table</code>, <code>field</code>, <code>output</code>, <code>phoneformat</code></td></tr>
          <tr><td><code>mode equation</code></td><td>Ondersteund</td><td>Een vergelijking oplossen.</td><td><code>given</code>, <code>equation</code>, <code>solve</code>, <code>constraint</code></td></tr>
          <tr><td><code>mode geometry</code></td><td>Niet ondersteund</td><td>Toekomstige 3D/vectorlaag.</td><td>Gebruik in 2.0 alleen formulekaarten/uitleg.</td></tr>
          <tr><td><code>mode matrix3x3</code></td><td>Niet ondersteund</td><td>Toekomstige matrixlaag.</td><td>2.0 heeft alleen limited matrix 2x2 als formulekaart.</td></tr>
        </table>
        </details>
        <details><summary>Voorbeeld: gewone converter</summary>
        <pre>Name Celsius naar Fahrenheit
        input1 Celsius
        input2 Fahrenheit
        format #.00
        math ans * 1,8
        math ans + 32
        end</pre>
        <p>Hier is <code>mode</code> niet nodig. Dit is de standaard.</p>
        </details>
        <details><summary>Voorbeeld: data-verwerking</summary>
        <pre>Name Klanten telefoon opschonen
        mode data
        table klanten

        field telefoon
        phoneformat country NL
        chg "01751-xxxxx","070-51xxxxx"
        output telefoon_nieuw
        end</pre>
        <p>Gebruik <code>mode data</code> als het NOD-bestand velden uit een tabel verwerkt.</p>
        </details>
        <details><summary>Voorbeeld: vergelijking oplossen</summary>
        <pre>Name Vergelijking oplossen
        mode equation
        given y = 20
        equation y = x * 2
        solve x
        constraint x >= 0
        end</pre>
        <p>Gebruik <code>mode equation</code> als je een onbekende variabele wilt oplossen.</p>
        </details>
        """);
    }

    // Zoek/commentaar: Legt phoneformat uit op basis van de opties die NodParser echt accepteert.
    private string BuildPhoneFormatCommandHelp(string keyword)
    {
        if (!keyword.Equals("phoneformat", StringComparison.OrdinalIgnoreCase))
            return "";

        return T("editor.nod_help.phoneformat.extra", """
        <h2>phoneformat in NOD 2.0</h2>
        <p><code>phoneformat</code> hoort in <code>mode data</code>, binnen een <code>field</code>-blok. De syntax is <code>phoneformat optie waarde</code>.</p>
        <div class="notice"><b>Belangrijk:</b> <code>phoneformat international</code> is geen geldige optie. Gebruik bijvoorbeeld <code>phoneformat country NL</code> en <code>phoneformat normalize_international true</code>.</div>

        <details open><summary>Basisvoorbeeld</summary>
        <pre>Name Klanten telefoon opschonen
        mode data
        table klanten

        field telefoon
        phoneformat country NL
        phoneformat remove_spaces true
        phoneformat remove_text_prefix true
        phoneformat normalize_international true
        output telefoon_nieuw
        end</pre>
        <p>Dit haalt bijvoorbeeld tekst zoals <code>tel.</code> weg, verwijdert spaties en zet Nederlandse internationale notatie zoals <code>+31</code> of <code>0031</code> terug naar een <code>0</code>-nummer.</p>
        </details>

        <details><summary>Alle ondersteunde opties</summary>
        <table>
          <tr><th>Optie</th><th>Waarde</th><th>Wat doet het?</th></tr>
          <tr><td><code>country</code></td><td><code>NL</code></td><td>Landinstelling. Nu gebruikt voor Nederlandse internationale normalisatie.</td></tr>
          <tr><td><code>keep_separator</code></td><td>tekst</td><td>Wordt opgeslagen als instelling voor scheidingsteken.</td></tr>
          <tr><td><code>remove_spaces</code></td><td><code>true</code>/<code>false</code></td><td>Verwijdert spaties.</td></tr>
          <tr><td><code>remove_dots</code></td><td><code>true</code>/<code>false</code></td><td>Verwijdert punten.</td></tr>
          <tr><td><code>remove_slashes</code></td><td><code>true</code>/<code>false</code></td><td>Verwijdert schuine strepen.</td></tr>
          <tr><td><code>remove_parentheses</code></td><td><code>true</code>/<code>false</code></td><td>Verwijdert haakjes.</td></tr>
          <tr><td><code>remove_text_prefix</code></td><td><code>true</code>/<code>false</code></td><td>Verwijdert tekstprefixen zoals <code>tel.</code> en <code>telefoon:</code>.</td></tr>
          <tr><td><code>normalize_international</code></td><td><code>true</code>/<code>false</code></td><td>Zet bij <code>country NL</code> <code>+31</code> en <code>0031</code> om naar beginnende <code>0</code>.</td></tr>
        </table>
        </details>

        <details><summary>Combineren met chg</summary>
        <p>In een data-veld wordt eerst <code>phoneformat</code> toegepast. Daarna kunnen regels zoals <code>chg</code>, <code>trans</code> of <code>math</code> op de opgeschoonde waarde werken.</p>
        <pre>Name Telefoon omnummeren
        mode data
        table klanten

        field telefoon
        phoneformat country NL
        phoneformat remove_spaces true
        phoneformat remove_text_prefix true
        phoneformat normalize_international true
        chg "01751","07051"
        output telefoon_nieuw
        end</pre>
        </details>
        """);
    }

    // Zoek/commentaar: Maakt klikbare links vanaf Symbolen naar de losse Symb-pagina's.
    private static string BuildSymbolOverviewLinks()
    {
        return """
        <h2>Snel naar symbool-uitleg</h2>
        <p>Klik op een symbool om de plaats in het venster met een voorbeeld te zien.</p>
        <div class="topic-links">
          <a class="topic-link" href="nodpage:cmd:symb1"><b>Symb1</b><span>links van input</span><code>DM 22</code></a>
          <a class="topic-link" href="nodpage:cmd:symb2"><b>Symb2</b><span>links van output</span><code>EUR 11,25</code></a>
          <a class="topic-link" href="nodpage:cmd:symb3"><b>Symb3</b><span>rechts van input</span><code>22 DM</code></a>
          <a class="topic-link" href="nodpage:cmd:symb4"><b>Symb4</b><span>rechts van output</span><code>11,25 EUR</code></a>
        </div>
        <pre>Name Duitse mark naar euro
        input1 Value in Deutsch Mark
        input2 Value in euros
        Symb3 DM
        format #.00
        math ans / 1,95583
        end</pre>
        """;
    }

    // Zoek/commentaar: Maakt per Symb-command een klein gericht vensterdiagram.
    private static string BuildSymbolCommandDiagram(string keyword)
    {
        var key = keyword.ToLowerInvariant();
        if (key is not ("symb1" or "symb2" or "symb3" or "symb4"))
            return "";

        var isInput = key is "symb1" or "symb3";
        var isLeft = key is "symb1" or "symb2";
        var label = key switch
        {
            "symb1" => "Symb1: links van input",
            "symb2" => "Symb2: links van output",
            "symb3" => "Symb3: rechts van input",
            _ => "Symb4: rechts van output"
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
        var note = isLeft ? "Dit symbool staat voor de waarde." : "Dit symbool staat achter de waarde.";

        return $$"""
        <h2>Voorbeeld in het venster</h2>
        <p>{{note}}</p>
        <table class="symbol-mini">
          <tr><td></td><td colspan="4" class="mini-label">{{rowLabel}}</td></tr>
          <tr>
            <td class="mini-radio"><span class="shot-radio {{(isInput ? "on" : "")}}"></span></td>
            <td class="mini-before">{{rowBefore}}</td>
            <td class="mini-field"><span class="shot-input">{{rowValue}}</span></td>
            <td class="mini-after">{{rowAfter}}</td>
            <td class="mini-help"><span class="legend">{{label}}</span></td>
          </tr>
        </table>
        <pre>Name Duitse mark naar euro
        input1 Value in Deutsch Mark
        input2 Value in euros
        {{code}}
        format #.00
        math ans / 1,95583
        end</pre>
        <p><b>Let op:</b> gebruik voor dezelfde waarde meestal links of rechts, niet allebei.</p>
        """;
    }

    // Zoek/commentaar: Maakt een screenshot-achtig voorbeeld zodat Symb1-4 direct zichtbaar zijn.
    private static string BuildSymbolPlacementDiagram()
    {
        return """
        <h2>Waar zie je de symbolen?</h2>
        <p>Dit vensterdiagram laat precies zien waar de vier symbool-commands terechtkomen.</p>
        <div class="symbol-shot">
          <div class="shot-title">Duitse mark naar euro</div>
          <div class="shot-menu">Bestand&nbsp;&nbsp;&nbsp; Bewerken&nbsp;&nbsp;&nbsp; Configuratie&nbsp;&nbsp;&nbsp; Extra&nbsp;&nbsp;&nbsp; Over</div>
          <div class="shot-toolbar"><span class="tool-dot"></span><span class="tool-dot small"></span><span class="tool-folder"></span><span class="toolbar-text">Bestand:</span><span class="file-box">DEM.nod</span></div>
          <table class="shot-table">
            <tr><td></td><td class="shot-label" colspan="4">Value in Deutsch Mark</td></tr>
            <tr>
              <td class="radio-cell"><span class="shot-radio on"></span></td>
              <td class="sym-before"><span class="callout">DM</span></td>
              <td class="field-cell"><span class="shot-input">22</span></td>
              <td class="sym-after"><span class="callout">DM</span></td>
              <td class="legend-cell"><span class="legend">Symb1: links van input</span><br><span class="legend">Symb3: rechts van input</span></td>
            </tr>
            <tr><td></td><td class="shot-label" colspan="4">Value in euros</td></tr>
            <tr>
              <td class="radio-cell"><span class="shot-radio"></span></td>
              <td class="sym-before"><span class="callout">€</span></td>
              <td class="field-cell"><span class="shot-input">11,25</span></td>
              <td class="sym-after"><span class="callout">EUR</span></td>
              <td class="legend-cell"><span class="legend">Symb2: links van output</span><br><span class="legend">Symb4: rechts van output</span></td>
            </tr>
            <tr><td></td><td colspan="4" class="shot-options">□ Groeperen&nbsp;&nbsp;&nbsp; ☑ Decimalen&nbsp; <span class="spin-box">2</span></td></tr>
          </table>
        </div>
        <pre>Name Duitse mark naar euro
        input1 Value in Deutsch Mark
        input2 Value in euros
        Symb1 DM
        Symb2 €
        Symb3 DM
        Symb4 EUR
        format #.00
        math ans / 1,95583
        end</pre>
        <p><b>Let op:</b> gebruik voor een waarde meestal links of rechts, niet allebei. Dus voor input kies je <code>Symb1</code> of <code>Symb3</code>; voor output kies je <code>Symb2</code> of <code>Symb4</code>.</p>
        """;
    }

    private static string GetNodHelpCss()
    {
        var stylesheetPath = Path.Combine(AppContext.BaseDirectory, "Resources", "nod-help.css");
        if (File.Exists(stylesheetPath))
        {
            return File.ReadAllText(stylesheetPath);
        }

        return "body { margin:0; padding:24px 24px 34px; font-family:Segoe UI, Arial, sans-serif; color:#1f2937; background:#ffffff; } h1, h2 { color:#0f3f8f; } pre { background:#101827; color:#e5eefc; padding:12px; border-radius:8px; white-space:pre-wrap; } code { background:#eef4ff; color:#0f3f8f; padding:1px 5px; border-radius:5px; } .notice { background:#fff8e6; border:1px solid #f4d184; border-left:4px solid #d69400; border-radius:8px; padding:10px 12px; margin:10px 0 14px; color:#3f2f12; } .warning-sign { display:flex; align-items:flex-start; gap:12px; background:#fff8e1; border:1px solid #fbbf24; border-left:6px solid #dc2626; border-radius:8px; padding:12px 14px; margin:12px 0 16px; color:#7f1d1d; box-shadow:0 4px 14px rgba(220,38,38,.08); scroll-margin-bottom:92px; } .warning-sign b { color:#b91c1c; } .warning-board { flex:0 0 42px; width:42px; height:39px; display:block; filter:drop-shadow(0 2px 4px rgba(153,27,27,.22)); } .code-copy-wrap { position:relative; margin:10px 0 16px; } .code-copy-wrap pre { margin:0; padding-right:48px; } .code-copy-button { position:absolute; top:8px; right:8px; width:30px; height:30px; border:0; border-radius:6px; background:transparent; color:#dbeafe; font-size:0; cursor:pointer; } .code-copy-button::before, .code-copy-button::after { content:\"\"; position:absolute; width:12px; height:14px; border:1.8px solid currentColor; border-radius:2px; background:transparent; } .code-copy-button::before { left:8px; top:7px; opacity:.72; } .code-copy-button::after { left:11px; top:10px; background:#101827; } .code-copy-button:hover { background:#1f3152; color:#fff; } .code-copy-button:hover::after { background:#1f3152; } .code-copy-button.copied { background:#0f7a34; color:#fff; } .code-copy-button.copied::before { content:\"\"; width:11px; height:6px; left:9px; top:10px; border:0; border-left:2.3px solid currentColor; border-bottom:2.3px solid currentColor; border-radius:0; transform:rotate(-45deg); opacity:1; } .code-copy-button.copied::after { display:none; }";
    }

    // Activeert kopieerknoppen bij alle codevoorbeelden in de volledige NOD-help.
    private static string GetNodHelpScript()
    {
        return """
        <script>
        (() => {
            function copyText(text) {
                if (window.chrome && window.chrome.webview) {
                    window.chrome.webview.postMessage(text);
                    return true;
                }

                if (navigator.clipboard && navigator.clipboard.writeText) {
                    navigator.clipboard.writeText(text);
                    return true;
                }

                const area = document.createElement('textarea');
                area.value = text;
                area.setAttribute('readonly', '');
                area.style.position = 'fixed';
                area.style.left = '-9999px';
                document.body.appendChild(area);
                area.select();
                const ok = document.execCommand('copy');
                area.remove();
                return ok;
            }

            function addCopyButtons() {
                document.querySelectorAll('pre').forEach(pre => {
                    if (pre.closest('.code-copy-wrap')) {
                        return;
                    }

                    const wrapper = document.createElement('div');
                    wrapper.className = 'code-copy-wrap';
                    pre.parentNode.insertBefore(wrapper, pre);
                    wrapper.appendChild(pre);

                    const button = document.createElement('button');
                    button.type = 'button';
                    button.className = 'code-copy-button';
                    button.title = 'Kopieren';
                    button.setAttribute('aria-label', 'Kopieren');
                    wrapper.appendChild(button);

                    button.addEventListener('click', event => {
                        event.preventDefault();
                        event.stopPropagation();
                        const text = pre.innerText.replace(/\n+$/g, '');
                        if (!copyText(text)) {
                            return;
                        }

                        button.classList.add('copied');
                        button.title = 'Gekopieerd';
                        window.setTimeout(() => {
                            button.classList.remove('copied');
                            button.title = 'Kopieren';
                        }, 1200);
                    });
                });
            }

            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', addCopyButtons);
            } else {
                addCopyButtons();
            }
        })();
        </script>
        """;
    }

    // Wikkelt één help-onderwerp in volledige HTML-opmaak.
    private static string WrapNodHelpPage(string title, string body)
    {
        return $$"""
        <!doctype html>
        <html>
        <head>
        <meta http-equiv="X-UA-Compatible" content="IE=edge" />
        <style>
        {{GetNodHelpCss()}}
        </style>
        </head>
        <body><h1>{{WebUtility.HtmlEncode(title)}}</h1>{{body}}{{GetNodHelpScript()}}</body>
        </html>
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

        return $$"""
        <!doctype html>
        <html>
        <head>
        <meta http-equiv="X-UA-Compatible" content="IE=edge" />
        <style>
        * { box-sizing:border-box; }
        html, body { overflow:visible; }
        body { margin:0; padding:10px 13px 7px; font-family:"Segoe UI", Arial, sans-serif; font-size:12px; line-height:1.34; color:#1f2937; background:transparent; }
        .title { font-weight:750; color:#0f3f8f; margin-bottom:5px; font-size:13px; }
        .syntax { font-family:Consolas, "Cascadia Mono", monospace; background:#f6f8fb; border:1px solid #d7e0ee; border-radius:6px; padding:5px 7px; margin:4px 0 7px; white-space:pre-wrap; overflow-wrap:anywhere; }
        .muted { color:#6b7280; }
        .insert { display:inline-block; margin-top:6px; padding:5px 10px; border-radius:999px; background:#0f3f8f; color:#fff; text-decoration:none; font-weight:700; }
        .more { display:inline-block; margin:6px 0 0 8px; color:#0f3f8f; font-weight:700; text-decoration:none; }
        .actions { margin:7px 0 0; }
        .suggest { color:#0f3f8f; text-decoration:none; font-weight:750; }
        code { font-family:Consolas, "Cascadia Mono", monospace; background:#eef4ff; border:1px solid #d7e3f7; border-radius:5px; padding:1px 4px; color:#0f3f8f; overflow-wrap:anywhere; }
        .cmd-link { text-decoration:none; }
        .cmd-link:hover code { background:#dcecff; border-color:#6fa2e9; }
        ul { margin:4px 0 0 0; padding:0; list-style:none; }
        li { margin:4px 0; padding:5px 7px; border:1px solid #e0e7f2; border-radius:6px; background:#fff; }
        b { color:#0f3f8f; }
        </style>
        <script>
        function postHeight() {
            if (!window.chrome || !window.chrome.webview) {
                return;
            }

            const body = document.body;
            const html = document.documentElement;
            const bodyTop = body ? body.getBoundingClientRect().top : 0;
            const childBottom = body
                ? Array.from(body.children).reduce((bottom, child) => {
                    const rect = child.getBoundingClientRect();
                    return Math.max(bottom, rect.bottom - bodyTop);
                  }, 0)
                : 0;
            const height = Math.ceil(Math.max(
                body ? body.getBoundingClientRect().height : 0,
                body ? body.scrollHeight : 0,
                html ? html.scrollHeight : 0,
                childBottom
            ));
            window.chrome.webview.postMessage('nodhelp-height:' + height);
        }

        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', postHeight);
        } else {
            postHeight();
        }
        window.addEventListener('load', postHeight);
        if (window.ResizeObserver) {
            new ResizeObserver(postHeight).observe(document.body);
        }
        requestAnimationFrame(postHeight);
        setTimeout(postHeight, 80);
        </script>
        </head>
        <body>{{body}}</body>
        </html>
        """;
    }

    // Bouwt de HTML voor één keyword plus een klikbare invoegactie.
    private string BuildExactNodHelpHtml(string keyword)
    {
        _lastNodHelpKeyword = keyword;
        var body = T($"editor.nod_help.{keyword.ToLowerInvariant()}", GetDefaultNodHelpHtml(keyword));
        var insertText = IsLegacyInputKeyword(keyword)
            ? T("editor.nod_help.replace", "Vervangen")
            : T("editor.nod_help.insert", "Invoegen");
        var moreText = T("editor.nod_help.more", "Meer help");
        var link = $"nodinsert:///{Uri.EscapeDataString(keyword)}";
        return body + $"<div class=\"actions\"><a class=\"insert\" href=\"{link}\">{WebUtility.HtmlEncode(insertText)}</a><a class=\"more\" href=\"nodhelp:///full\">{WebUtility.HtmlEncode(moreText)}</a></div>";
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
    private static string GetDefaultNodHelpHtml(string keyword)
    {
        return keyword.ToLowerInvariant() switch
        {
            "name" => "<div class=\"title\">Name</div><div class=\"syntax\">Name Celsius naar Fahrenheit</div><div><b>Betekenis:</b> interne naam van de converter.</div><div><b>Voorbeeld:</b> <code>Name Postcode naar adres demo</code>.</div>",
            "urln" => "<div class=\"title\">URLN</div><div class=\"syntax\">URLN Titel voor gebruiker</div><div><b>Betekenis:</b> zichtbare titel of omschrijving.</div><div><b>Voorbeeld:</b> <code>URLN Telefoonnummer omnummering 1995 volledig</code>.</div>",
            "input1" => "<div class=\"title\">input1</div><div class=\"syntax\">input1 Celsius</div><div><b>Betekenis:</b> legacy label van de invoerwaarde. Werkt in NOD 2.0 beta en blijft ondersteund in 2.x.</div><div><b>Vervanger:</b> gebruik voor nieuwe bestanden liever <code>input x Celsius</code>.</div><div><b>Tip:</b> oude combinatie <code>input1</code>/<code>input2</code> wordt modern <code>input x</code>/<code>inputr y</code>.</div><div><b>Voorbeeld:</b> <code>input1 Celsius</code> wordt <code>input x Celsius</code>.</div>",
            "input2" => "<div class=\"title\">input2</div><div class=\"syntax\">input2 Fahrenheit</div><div><b>Betekenis:</b> legacy label van de uitvoerwaarde en reverse-kant. Werkt in NOD 2.0 beta en blijft ondersteund in 2.x.</div><div><b>Vervanger:</b> gebruik voor nieuwe bestanden liever <code>inputr y Fahrenheit</code> of <code>output y Fahrenheit</code>. Gebruik <code>input y</code> alleen als het echt een tweede invoerwaarde is, bijvoorbeeld bij een 2D vector.</div><div><b>Voorbeeld:</b> <code>input1 Celsius</code> wordt <code>input x Celsius</code>; <code>input2 Fahrenheit</code> wordt <code>inputr y Fahrenheit</code>.</div>",
            "inputr" => "<div class=\"title\">inputr</div><div class=\"syntax\">inputr y Fahrenheit</div><div><b>Betekenis:</b> moderne reverse/output-kant voor gewone converters. Dit vult het rechter veld in de calculator, zonder een tweede invoerwaarde te maken.</div><div><b>Voorbeeld:</b> <code>input x Celsius</code> met <code>inputr y Fahrenheit</code>.</div>",
            "input" => "<div class=\"title\">input</div><div class=\"syntax\">input x Label</div><div><b>Betekenis:</b> moderne invoerdefinitie voor NOD 2.0 beta.</div><div><b>Voorbeelden:</b> <code>input x Celsius</code>, <code>input y Breedte</code>, <code>input height Hoogte</code>, <code>input text Tekst</code>, <code>input texta Bron</code>, <code>input textb Doel</code>, <code>input phone Telefoonnummer</code>.</div><div><b>Math-notatie:</b> <code>input x</code> hoort bij <code>ans(x)</code>/<code>ansx</code>; custom input zoals <code>input height</code> hoort bij <code>ans(height)</code> in de NOD 2.1-voorbereiding.</div><div><b>Let op:</b> <code>input z</code> is alleen future metadata; NOD 2.0 ondersteunt geen 3D/matrix-3x3 engine.</div>",
            "result" => "<div class=\"title\">Result</div><div class=\"syntax\">Result result:</div><div><b>Betekenis:</b> tekstlabel voor een normaal resultaat.</div><div><b>Voorbeeld:</b> <code>Result Nieuw nummer:</code>.</div>",
            "resfou" => "<div class=\"title\">Resfou</div><div class=\"syntax\">Resfou wrong or BAD number</div><div><b>Betekenis:</b> fouttekst wanneer de invoer niet geconverteerd kan worden.</div><div><b>Voorbeeld:</b> <code>Resfou Onbekend nummer</code>.</div>",
            "symb1" => "<div class=\"title\">Symb1</div><div class=\"syntax\">Symb1 DM</div><div><b>Betekenis:</b> symbool links van input1.</div><div><b>Voorbeeld:</b> <code>Symb1 DM</code> toont <code>DM 25</code>.</div>",
            "symb2" => "<div class=\"title\">Symb2</div><div class=\"syntax\">Symb2 EUR</div><div><b>Betekenis:</b> symbool links van input2/output.</div><div><b>Voorbeeld:</b> <code>Symb2 EUR</code> toont <code>EUR 12,78</code>.</div>",
            "symb3" => "<div class=\"title\">Symb3</div><div class=\"syntax\">Symb3 kg</div><div><b>Betekenis:</b> symbool rechts van input1.</div><div><b>Voorbeeld:</b> <code>Symb3 kg</code> toont <code>25 kg</code>.</div>",
            "symb4" => "<div class=\"title\">Symb4</div><div class=\"syntax\">Symb4 lb</div><div><b>Betekenis:</b> symbool rechts van input2/output.</div><div><b>Voorbeeld:</b> <code>Symb4 lb</code> toont <code>55 lb</code>.</div>",
            "format" => "<div class=\"title\">format</div><div class=\"syntax\">format ##.00</div><div><b>Betekenis:</b> bepaalt hoeveel decimalen het resultaat toont.</div><div><b>Voorbeeld:</b> <code>format ##.00</code> toont <code>12.50</code>.</div>",
            "mode" => "<div class=\"title\">mode</div><div class=\"syntax\">mode calculator</div><div><b>Betekenis:</b> extra instelling voor het type converter.</div><div><b>Voorbeeld:</b> <code>mode calculator</code>.</div>",
            "math" => "<div class=\"title\">math</div><div class=\"syntax\">math ans * 1,8</div><div><b>Betekenis:</b> voert een rekenstap uit met <code>ans</code> als huidige waarde.</div><div><b>Voorbeeld:</b> <code>math ans + 32</code> telt 32 op bij de huidige waarde.</div>",
            "chg" => "<div class=\"title\">chg</div><div class=\"syntax\">chg \"oude-prefix\",\"nieuwe-prefix\"</div><div><b>Betekenis:</b> verandert het begin van tekst, bijvoorbeeld codes of telefoonnummers.</div><div><b>Voorbeeld:</b> <code>chg \"01751-xxxxx\",\"070-51xxxxx\"</code> maakt van <code>01751-12345</code> de waarde <code>070-5112345</code>.</div><div><b>Patronen:</b> <code>x</code> mag als restteken. Bij meerdere matches wint de langste/meest specifieke prefix.</div>",
            "trans" => "<div class=\"title\">trans</div><div class=\"syntax\">trans \"oude waarde\",\"nieuwe waarde\"</div><div><b>Betekenis:</b> vertaalt een exacte tekstwaarde.</div><div><b>Voorbeeld:</b> <code>trans \"hallo\",\"hello\"</code> vertaalt <code>hallo</code> naar <code>hello</code>.</div>",
            "reverse" => "<div class=\"title\">reverse</div><div class=\"syntax\">reverse ans / 1,8</div><div><b>Betekenis:</b> handmatige terugrekenregel.</div><div><b>Voorbeeld:</b> bij <code>math ans * 1,8</code> kun je <code>reverse ans / 1,8</code> gebruiken.</div>",
            "field" => "<div class=\"title\">field</div><div class=\"syntax\">field KolomNaam</div><div><b>Betekenis:</b> start een datavelddefinitie.</div><div><b>Voorbeeld:</b> <code>field Telefoon</code>.</div>",
            "table" => "<div class=\"title\">table</div><div class=\"syntax\">table TabelNaam</div><div><b>Betekenis:</b> naam van de brontabel.</div><div><b>Voorbeeld:</b> <code>table Klanten</code>.</div>",
            "output" => "<div class=\"title\">output</div><div class=\"syntax\">output ResultaatKolom</div><div><b>Betekenis:</b> naam van het outputveld.</div><div><b>Voorbeeld:</b> <code>output TelefoonNieuw</code>.</div>",
            "phoneformat" => "<div class=\"title\">phoneformat</div><div class=\"syntax\">phoneformat country NL</div><div><b>Betekenis:</b> normaliseert telefoonnummers binnen een <code>field</code> van <code>mode data</code>.</div><div><b>Voorbeeld:</b> <code>phoneformat remove_spaces true</code> verwijdert spaties; <code>phoneformat normalize_international true</code> normaliseert internationale notatie voor Nederland.</div>",
            "lookup" => "<div class=\"title\">lookup</div><div class=\"syntax\">lookup Landen</div><div><b>Betekenis:</b> start een lookup-definitie.</div><div><b>Voorbeeld:</b> <code>lookup Landen</code>.</div>",
            "match" => "<div class=\"title\">match</div><div class=\"syntax\">match code = NL</div><div><b>Betekenis:</b> beschrijft hoe een lookup gekoppeld wordt.</div><div><b>Voorbeeld:</b> <code>match postcode = 2566GB</code>.</div>",
            "given" => "<div class=\"title\">given</div><div class=\"syntax\">given x = 10</div><div><b>Betekenis:</b> bekende waarde voor een vergelijking.</div><div><b>Voorbeeld:</b> <code>given y = 20</code>.</div>",
            "equation" => "<div class=\"title\">equation</div><div class=\"syntax\">equation y = x * 2</div><div><b>Betekenis:</b> definieert een vergelijking.</div><div><b>Voorbeeld:</b> <code>equation y = x * 2</code>.</div>",
            "solve" => "<div class=\"title\">solve</div><div class=\"syntax\">solve x</div><div><b>Betekenis:</b> geeft aan welke variabele opgelost moet worden. Bij calculus mag <code>solve diff</code> of <code>solve integral</code> ook, zodat de berekening als stap-voor-stap uitleg kan worden getoond.</div><div><b>Voorbeelden:</b> <code>solve x</code>, <code>solve diff ans^2</code>, <code>solve integral 0,1 ans^2</code>.</div>",
            "constraint" => "<div class=\"title\">constraint</div><div class=\"syntax\">constraint x >= 0</div><div><b>Betekenis:</b> voorwaarde voor een oplossing.</div><div><b>Voorbeeld:</b> <code>constraint x >= 0</code> voorkomt negatieve oplossingen.</div>",
            "preview" => "<div class=\"title\">preview</div><div class=\"syntax\">preview Voorbeeldtekst</div><div><b>Betekenis:</b> tekst voor voorbeeld/preview.</div><div><b>Voorbeeld:</b> <code>preview Vul 2566 GB 241 in</code>.</div>",
            "backup" => "<div class=\"title\">backup</div><div class=\"syntax\">backup true</div><div><b>Betekenis:</b> compatibiliteitsinstelling uit oudere NOD-bestanden.</div><div><b>Voorbeeld:</b> <code>backup true</code>.</div>",
            "indoprint" => "<div class=\"title\">indoprint</div><div class=\"syntax\">indoprint Introductietekst</div><div><b>Betekenis:</b> regel met introductietekst die voor gebruik getoond kan worden.</div><div><b>Voorbeeld:</b> <code>indoprint Deze converter gebruikt de Decibel-tabel.</code> Sluit daarna af met <code>indoend</code>.</div>",
            "indoend" => "<div class=\"title\">indoend</div><div class=\"syntax\">indoend</div><div><b>Betekenis:</b> einde van een introductietekstblok.</div><div><b>Voorbeeld:</b> <code>indoprint Uitleg</code> gevolgd door <code>indoend</code>.</div>",
            "end" => "<div class=\"title\">end</div><div class=\"syntax\">end</div><div><b>Betekenis:</b> einde van het NOD-bestand.</div><div><b>Voorbeeld:</b> zet <code>end</code> als laatste regel.</div>",
            _ => $"<div class=\"title\">{WebUtility.HtmlEncode(keyword)}</div><div class=\"syntax\">{WebUtility.HtmlEncode(keyword)} ...</div><div><b>Voorbeeld:</b> <code>{WebUtility.HtmlEncode(keyword)} ...</code></div>"
        };
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
        editor.SelectionColor = Color.Black;
        editor.SelectionFont = new Font(editor.Font, FontStyle.Regular);

        // Comments starting with apostrophe.
        HighlightPattern(editor, @"'.*$", Color.ForestGreen, FontStyle.Italic, RegexOptions.Multiline);

        // Commands at line start.
        HighlightPattern(editor, @"^\s*(Name|URLN|input1|input2|input|inputr|Result|Resfou|Symb1|Symb2|Symb3|Symb4|format|mode|math|chg|trans|reverse|field|table|output|phoneformat|lookup|match|given|equation|solve|constraint|preview|backup|indoprint|indoend|end)\b", Color.RoyalBlue, FontStyle.Bold, RegexOptions.IgnoreCase | RegexOptions.Multiline);

        // Functions/constants.
        HighlightPattern(editor, @"\b(ans|e|pi|π|sqrt|abs|ln|log|sin|cos|tan|asin|acos|atan|sind|cosd|tand|asind|acosd|atand|rad|deg|mod|rem|diff|integral|limit)\b", Color.DarkCyan, FontStyle.Regular, RegexOptions.IgnoreCase);

        // Numbers.
        HighlightPattern(editor, @"(?<!\w)\d+([,.]\d+)?(?!\w)", Color.DarkOrange, FontStyle.Regular, RegexOptions.None);

        editor.Select(Math.Min(selectionStart, editor.TextLength), Math.Min(selectionLength, Math.Max(0, editor.TextLength - selectionStart)));
        editor.SelectionColor = Color.Black;
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
        _lineCountLabel.Text = FormatLineCountStatus(editor.Lines.Length);
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
    private string T(string key, string fallback) => _language.Text(key, fallback);

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

