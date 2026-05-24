#nullable enable
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tiedragon.Graph.Image;
using Tiedragon.Help;
using Tiedragon.LanguagePackage;
using Tiedragon.NodSystem.Core;

namespace Tiedragon.ToolEditor;

// Copyright (c) Tiedragon. All rights reserved.
//
// ToolEditor is the shared editing surface for Tiedragon package and help tools.
// The first version mirrors the NOD Editor structure: central toolbar, custom
// document tabs, source editor and HTML preview.
public sealed class ToolEditorForm : Form
{
    private const string LanguagePackageMagicText = "SYSCALC-LNGPDK";
    private const int LanguagePackageContainerFormat = 1;
    private const string LanguagePackageSoftwareId = "tiedragon.syscalculator";
    private const string LanguagePackageType = "language";
    private const string ObjectPackageExtension = ".objpdk";
    private const string CompiledPackageExtension = ".lngpdk";
    private const int MaxPackageHeaderBytes = 64 * 1024;
    private const long MaxPackagePayloadBytes = 192L * 1024 * 1024;
    private const int MaxLanguageSyntaxHighlightChars = 180_000;
    private const int MaxSourceSyntaxHighlightChars = 90_000;
    private const int SyntaxHighlightViewportBufferChars = 4_000;
    private const int MaxLanguagePreviewRows = 500;
    private const int WmSetRedraw = 0x000B;
    private const string FormulaIndexPackageTemplate = """
        <h1>{{title}}</h1>
        <p>{{intro}}</p>
        <table>
          <thead><tr><th>{{column_formula}}</th><th>{{column_category}}</th><th>{{column_tags}}</th><th>{{column_description}}</th></tr></thead>
          <tbody>
        {{rows}}  </tbody>
        </table>
        """;

    private const string FormulaCardPackageTemplate = """
        <h1>{{title}}</h1>
        <div class="notice">{{tags}}</div>
        <p>{{description}}</p>

        <h2>{{section_formula}}</h2>
        <p><code>{{formula_text}}</code></p>
        <div class="formula">{{mathml_card}}</div>

        <h2>{{section_text}}</h2>
        <pre>{{plain_text}}</pre>

        <h2>{{section_latex}}</h2>
        <pre>{{latex}}</pre>

        <h2>{{section_mathml}}</h2>
        <pre>{{mathml_pre}}</pre>

        <h2>{{section_example_nod}}</h2>
        <pre>{{example_nod}}</pre>
        """;

    private const string NodFullPagePackageTemplate = """
        <h1>{{title}}</h1>
        <p>{{intro}}</p>

        <h2>{{section_title}}</h2>
        <p>{{section_body}}</p>
        """;

    private const string NodCommandPagePackageTemplate = """
        <h1>{{command}}</h1>
        <p>{{summary}}</p>

        <div class="notice"><b>{{syntax_label}}</b> <code>{{syntax}}</code></div>

        <h2>{{example_title}}</h2>
        <pre>{{example_nod}}</pre>
        """;

    private const string NodPopupPackageTemplate = """
        <div class="title">{{command}}</div>
        <div class="syntax">{{syntax}}</div>
        <div><b>{{meaning_label}}</b> {{meaning}}</div>
        <div><b>{{example_label}}</b> <code>{{example}}</code></div>
        """;

    private const string NodSnippetPackageTemplate = """
        <div class="topic-grid">
          {{items}}
        </div>
        """;
    private static readonly Color SyntaxDefaultColor = Color.FromArgb(31, 41, 55);
    private static readonly Color SyntaxKeywordColor = Color.FromArgb(0, 74, 173);
    private static readonly Color SyntaxAttributeColor = Color.FromArgb(170, 72, 20);
    private static readonly Color SyntaxStringColor = Color.FromArgb(126, 82, 0);
    private static readonly Color SyntaxCommentColor = Color.FromArgb(47, 128, 67);
    private static readonly Color SyntaxSelectorColor = Color.FromArgb(96, 64, 160);

    private static readonly byte[] LanguagePackageMagic = Encoding.ASCII.GetBytes(LanguagePackageMagicText);
    private static readonly JsonSerializerOptions LanguagePackageJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private static readonly Regex HtmlCommentRegex = new("<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex HtmlTagRegex = new("</?[a-zA-Z][^>]*?>", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex HtmlAttributeRegex = new(@"\s([a-zA-Z_:][-a-zA-Z0-9_:.]*)(?=\s*=)", RegexOptions.Compiled);
    private static readonly Regex QuotedStringRegex = new("(\"[^\"]*\"|'[^']*')", RegexOptions.Compiled);
    private static readonly Regex JsonPropertyRegex = new("\"[^\"\\r\\n]*\"(?=\\s*:)", RegexOptions.Compiled);
    private static readonly Regex CssSelectorRegex = new(@"(^|\})([^{]+)(?=\{)", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex LanguageKeyRegex = new(@"^[^#;\r\n=]+(?=\=)", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex LanguageCommentRegex = new(@"^[ \t]*[#;].*$", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex JavaScriptCommentRegex = new(@"//.*?$|/\*.*?\*/", RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex JavaScriptKeywordRegex = new(@"\b(?:const|let|var|function|return|if|else|for|while|do|switch|case|break|continue|try|catch|finally|throw|new|class|extends|import|export|from|async|await|true|false|null|undefined|document|window)\b", RegexOptions.Compiled);
    private static readonly Regex HtmlHrefLinkRegex = new("href\\s*=\\s*(?<quote>[\"'])(?<path>[^\"']+)\\k<quote>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HtmlMediaLinkRegex = new("(?<attr>src|href)\\s*=\\s*(?<quote>[\"'])(?<path>[^\"']+\\.(?:png|jpg|jpeg|svg|webp|gif|bmp))\\k<quote>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ConceptBannerRegex = new("<div\\s+class=\"concept-banner\"[\\s\\S]*?</div>\\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LanguagePlaceholderRegex = new(@"\[[A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z0-9_]+)+\]", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex HeadingPlaceholderRegex = new(@"<h(?<level>[12])[^>]*>\s*(?<placeholder>\[[A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z0-9_]+)+\])\s*</h\k<level>>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex LegacyConceptWarningRegex = new("<div\\s+class=\"help-warning\"><b>Concept:</b>[\\s\\S]*?</div>\\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly TreeView _fileTree;
    private readonly ListView _documentList;
    private readonly FlowLayoutPanel _tabStrip;
    private readonly ToolStrip _htmlToolbar;
    private readonly RowStyle _htmlToolbarRow;
    private readonly ToolStripButton _sourceModeButton;
    private readonly ToolStripButton _editModeButton;
    private readonly Panel _editorContent;
    private readonly TableLayoutPanel _htmlEditHost;
    private readonly WebView2 _htmlEditor;
    private readonly WebView2 _preview;
    private readonly Label _previewCloseButton;
    private readonly ToolEditorConceptBanner _previewConceptBanner;
    private readonly ToolEditorConceptBanner _editConceptBanner;
    private readonly RowStyle _previewConceptRow;
    private readonly RowStyle _editConceptRow;
    private readonly System.Windows.Forms.Timer _previewConceptAnimationTimer;
    private readonly System.Windows.Forms.Timer _previewRefreshTimer;
    private readonly SplitContainer _contentSplit;
    private readonly Label _statusLabel;
    private readonly Label _statusInfoLabel;
    private readonly ToolStripButton _saveButton;
    private readonly ToolStripButton _validateButton;
    private readonly ToolStripButton _previewButton;
    private readonly List<ToolEditorDocument> _documents = [];
    private string _manifestText = "";
    private string? _pendingHtml;
    private string? _pendingHtmlEditor;
    private string? _conceptFolder;
    private string? _conceptPackagePath;
    private ToolEditorLanguageInfo? _cachedConfiguredToolEditorLanguage;
    private string? _cachedToolEditorHelpLanguageCode;
    private string? _cachedToolEditorHelpSourceText;
    private Dictionary<string, string>? _cachedToolEditorHelpTexts;
    private bool _browserFailed;
    private bool _htmlEditorFailed;
    private bool _loadingHtmlEditor;
    private bool _updatingNavigation;
    private bool _conceptBannerDismissed;
    private bool _closeConfirmed;
    private bool _closePromptActive;
    private bool _webViewInitializationStarted;
    private bool _htmlEditorInitializationStarted;
    private bool _starterPackageInitializationStarted;
    private int _suspendNavigationRefresh;
    private bool _previewPaneClosedByUser;
    private bool _preferredHtmlEditMode;
    private bool _htmlEditorContentDirty;
    private bool _loadingDocumentText;
    private bool _pendingPreviewInitialize;
    private bool _openedPackageSigned;
    private string _openedPackageSignatureAlgorithm = "";
    private string _openedPackageSignatureKeyId = "";
    private ToolEditorDocument? _current;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    public ToolEditorForm()
    {
        ToolEditorDebugger.InitializeEmbedded();
        ToolEditorDebugger.Log("ToolEditorForm constructor started.");
        Text = TToolEditor("tool_editor.window.title", "Tiedragon ToolEditor");
        Width = 1180;
        Height = 760;
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;
        KeyDown += ToolEditorForm_KeyDown;
        FormClosing += ToolEditorForm_FormClosing;
        FormClosed += ToolEditorForm_FormClosed;
        AllowDrop = true;
        DragEnter += ToolEditorForm_DragEnter;
        DragDrop += ToolEditorForm_DragDrop;

        ToolEditorDebugger.Log("ToolEditorForm building menu and toolbar.");
        var menu = BuildMenu();
        var toolbar = ToolEditorApi.CreateToolbar();
        toolbar.Items.Add(ToolEditorApi.CreateButton(TToolEditor("tool_editor.toolbar.new_package", "New package"), ToolEditorIcon.New, async (_, _) => await NewLanguagePackageAsync(), TToolEditor("tool_editor.toolbar.new_package.tip", "Create a package from the configured Syscalculator language")));
        toolbar.Items.Add(ToolEditorApi.CreateButton(TToolEditor("tool_editor.toolbar.open", "Open"), ToolEditorIcon.Open, async (_, _) => await OpenLanguagePackageAsync(), TToolEditor("tool_editor.toolbar.open.tip", "Open language package or concept")));
        toolbar.Items.Add(new ToolStripSeparator());
        _saveButton = ToolEditorApi.CreateButton(TToolEditor("tool_editor.toolbar.save_concept", "Save concept"), ToolEditorIcon.Save, async (_, _) => await SaveConceptLanguagePackageAsync(), TToolEditor("tool_editor.toolbar.save_concept.tip", "Save concept language package"));
        _validateButton = ToolEditorApi.CreateButton(TToolEditor("tool_editor.toolbar.validate", "Validate"), ToolEditorIcon.Validate, (_, _) => ValidateCurrent(showMessage: true), TToolEditor("tool_editor.toolbar.validate.tip", "Validate current document"));
        _previewButton = ToolEditorApi.CreateButton(TToolEditor("tool_editor.toolbar.preview", "Preview"), ToolEditorIcon.Test, (_, _) => ShowPreviewPane(), TToolEditor("tool_editor.toolbar.preview.tip", "Refresh HTML preview"));
        var compileButton = ToolEditorApi.CreateButton(TToolEditor("tool_editor.toolbar.compile", "Compile"), ToolEditorIcon.Solver, async (_, _) => await CompileLanguagePackageAsync(), TToolEditor("tool_editor.toolbar.compile.tip", "Compile language package to .lngpdk"));
        toolbar.Items.Add(_saveButton);
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(_validateButton);
        toolbar.Items.Add(_previewButton);
        toolbar.Items.Add(compileButton);

        ToolEditorDebugger.Log("ToolEditorForm building navigation controls.");
        _fileTree = new TreeView
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            FullRowSelect = true,
            HideSelection = false,
            ShowLines = true,
            ShowPlusMinus = true,
            ShowRootLines = true,
            ShowNodeToolTips = true,
            Font = new Font("Segoe UI", 9),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(31, 41, 55)
        };
        _fileTree.AfterSelect += FileTree_AfterSelect;
        _fileTree.AllowDrop = true;
        _fileTree.DragEnter += ToolEditorForm_DragEnter;
        _fileTree.DragDrop += ToolEditorForm_DragDrop;

        var fileTreeHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(203, 213, 225),
            Padding = new Padding(1)
        };
        fileTreeHost.Controls.Add(_fileTree);

        _documentList = new ListView
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            FullRowSelect = true,
            HideSelection = false,
            MultiSelect = false,
            View = View.Details,
            Font = new Font("Segoe UI", 9),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(31, 41, 55)
        };
        _documentList.Columns.Add(TToolEditor("tool_editor.document.column.type", "Type"), 150);
        _documentList.Columns.Add(TToolEditor("tool_editor.document.column.topic", "Topic"), 240);
        _documentList.Columns.Add(TToolEditor("tool_editor.media.column.package_path", "Package path"), 360);
        _documentList.SelectedIndexChanged += DocumentList_SelectedIndexChanged;
        _documentList.AllowDrop = true;
        _documentList.DragEnter += ToolEditorForm_DragEnter;
        _documentList.DragDrop += ToolEditorForm_DragDrop;

        _tabStrip = ToolEditorTabsApi.CreateStrip();
        _tabStrip.Dock = DockStyle.Fill;
        _tabStrip.WrapContents = false;
        _tabStrip.AutoScroll = false;
        _tabStrip.BackColor = Color.White;
        _tabStrip.Padding = new Padding(0, 2, 0, 0);
        _tabStrip.Margin = Padding.Empty;

        _sourceModeButton = CreateHtmlModeButton("Source", "HTML-broncode bewerken", (_, _) => SetHtmlEditMode(false));
        _editModeButton = CreateHtmlModeButton("Edit", "Visuele HTML-editor", (_, _) => SetHtmlEditMode(true));
        _htmlToolbar = CreateHtmlToolbar();
        _htmlToolbar.Visible = false;

        ToolEditorDebugger.Log("ToolEditorForm building WebView controls.");
        _htmlEditor = new WebView2
        {
            Dock = DockStyle.Fill,
            AllowExternalDrop = false,
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = GetWebView2UserDataFolder("HtmlEditor")
            }
        };
        _htmlEditor.CoreWebView2InitializationCompleted += (_, e) =>
        {
            if (!e.IsSuccess || _htmlEditor.CoreWebView2 is null)
                return;

            _htmlEditor.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            _htmlEditor.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            _htmlEditor.CoreWebView2.Settings.IsStatusBarEnabled = false;
            _htmlEditor.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _htmlEditor.CoreWebView2.WebMessageReceived += (_, args) => HandleHtmlEditorMessage(args);
            ShowPendingHtmlEditorIfReady();
        };
        _htmlEditor.NavigationCompleted += (_, _) =>
        {
            _loadingHtmlEditor = false;
            _htmlEditorContentDirty = false;
            FocusActiveEditor();
        };

        ToolEditorDebugger.Log("ToolEditorForm building editor layout.");
        _editorContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(1, 0, 1, 1)
        };
        _htmlEditHost = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White,
            Padding = Padding.Empty
        };
        _editConceptRow = new RowStyle(SizeType.Absolute, 0);
        _htmlEditHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _htmlEditHost.RowStyles.Add(_editConceptRow);
        _htmlEditHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var editorHost = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.White,
            Padding = new Padding(4, 4, 4, 0)
        };
        _htmlToolbarRow = new RowStyle(SizeType.Absolute, 0);
        editorHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editorHost.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        editorHost.RowStyles.Add(_htmlToolbarRow);
        editorHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        editorHost.Controls.Add(_tabStrip, 0, 0);
        editorHost.Controls.Add(_htmlToolbar, 0, 1);
        editorHost.Controls.Add(_editorContent, 0, 2);

        _preview = new WebView2
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            AllowExternalDrop = false,
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = GetWebView2UserDataFolder("Preview")
            }
        };
        _preview.CoreWebView2InitializationCompleted += (_, e) =>
        {
            if (!e.IsSuccess || _preview.CoreWebView2 is null)
                return;

            _preview.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _preview.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            _preview.CoreWebView2.Settings.IsStatusBarEnabled = false;
            _preview.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _preview.CoreWebView2.WebMessageReceived += (_, args) => HandlePreviewMessage(args);
            ShowPendingHtmlIfReady();
        };

        var previewHost = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.FromArgb(226, 232, 240),
            Padding = Padding.Empty
        };
        previewHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _previewConceptRow = new RowStyle(SizeType.Absolute, 0);
        previewHost.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        previewHost.RowStyles.Add(_previewConceptRow);
        previewHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var previewHeader = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 24,
            Margin = Padding.Empty,
            BackColor = Color.FromArgb(226, 232, 240),
            Padding = new Padding(8, 1, 8, 1)
        };
        var previewHeaderLine = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 1,
            BackColor = Color.FromArgb(203, 213, 225),
            Margin = Padding.Empty
        };

        var previewWebHost = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.White
        };

        var previewTitle = new Label
        {
            AutoSize = true,
            Text = TToolEditor("tool_editor.preview.title", "Preview"),
            ForeColor = Color.FromArgb(71, 85, 105),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            Location = new Point(8, 3)
        };
        _previewCloseButton = new Label
        {
            Text = "×",
            Dock = DockStyle.Right,
            Width = 30,
            BackColor = Color.FromArgb(226, 232, 240),
            ForeColor = Color.FromArgb(51, 65, 85),
            Font = new Font("Segoe UI", 11f, FontStyle.Regular),
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        _previewCloseButton.MouseEnter += (_, _) => _previewCloseButton.BackColor = Color.FromArgb(203, 213, 225);
        _previewCloseButton.MouseLeave += (_, _) => _previewCloseButton.BackColor = Color.FromArgb(226, 232, 240);
        _previewCloseButton.Click += (_, _) => ClosePreviewPane();

        _previewConceptBanner = new ToolEditorConceptBanner
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Visible = false
        };
        _previewConceptBanner.CloseRequested += (_, _) => DismissConceptBanner();
        _editConceptBanner = new ToolEditorConceptBanner
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Visible = false
        };
        _editConceptBanner.CloseRequested += (_, _) => DismissConceptBanner();
        _previewConceptAnimationTimer = new System.Windows.Forms.Timer { Interval = 55 };
        _previewConceptAnimationTimer.Tick += (_, _) =>
        {
            if (_previewConceptBanner.Visible)
                _previewConceptBanner.AdvanceStripe();
            if (_editConceptBanner.Visible)
                _editConceptBanner.AdvanceStripe();
        };
        _previewRefreshTimer = new System.Windows.Forms.Timer { Interval = 140 };
        _previewRefreshTimer.Tick += PreviewRefreshTimer_Tick;

        previewHeader.Controls.Add(previewHeaderLine);
        previewHeader.Controls.Add(_previewCloseButton);
        previewHeader.Controls.Add(previewTitle);
        _previewCloseButton.BringToFront();
        previewWebHost.Controls.Add(_preview);
        previewHost.Controls.Add(previewHeader, 0, 0);
        previewHost.Controls.Add(_previewConceptBanner, 0, 1);
        previewHost.Controls.Add(previewWebHost, 0, 2);

        _contentSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            BorderStyle = BorderStyle.None,
            SplitterWidth = 1,
            BackColor = Color.FromArgb(226, 232, 240),
            Panel1MinSize = 1,
            Panel2MinSize = 1
        };
        _contentSplit.Panel1.Controls.Add(editorHost);
        _contentSplit.Panel2.Padding = Padding.Empty;
        _contentSplit.Panel2.Controls.Add(previewHost);
        _contentSplit.SizeChanged += (_, _) => ClampPreviewSplitter(_contentSplit, 240);
        Shown += (_, _) =>
        {
            ClampPreviewSplitter(_contentSplit, 240);
            QueueStarterPackageInitialization();
        };

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            SplitterWidth = 5,
            BackColor = Color.FromArgb(226, 232, 240),
            FixedPanel = FixedPanel.Panel1,
            Panel1MinSize = 1,
            Panel2MinSize = 1
        };
        split.Panel1.Padding = new Padding(4, 4, 0, 0);
        split.Panel1.Controls.Add(fileTreeHost);
        split.Panel2.Controls.Add(_contentSplit);
        split.SizeChanged += (_, _) => ClampSplitter(split, 270);
        Shown += (_, _) =>
        {
            ClampSplitter(split, 270);
            QueueStarterPackageInitialization();
        };

        var statusBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 24,
            BackColor = Color.FromArgb(248, 250, 252)
        };
        _statusInfoLabel = new Label
        {
            Dock = DockStyle.Right,
            Width = 520,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(8, 0, 8, 0),
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = Color.FromArgb(71, 85, 105),
            Text = ""
        };
        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 8, 0),
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = Color.FromArgb(31, 41, 55),
            Text = TToolEditor("tool_editor.status.ready", "Ready")
        };
        statusBar.Controls.Add(_statusLabel);
        statusBar.Controls.Add(_statusInfoLabel);

        Controls.Add(split);
        Controls.Add(statusBar);
        Controls.Add(toolbar);
        Controls.Add(menu);
        MainMenuStrip = menu;

        UpdateUiState();
        ToolEditorDebugger.Log("ToolEditorForm constructor completed.");
    }

    private void QueueStarterPackageInitialization()
    {
        if (_starterPackageInitializationStarted)
            return;

        _starterPackageInitializationStarted = true;
        BeginInvoke(new Action(StartStarterPackageInitialization));
    }

    private void StartStarterPackageInitialization()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            ToolEditorDebugger.Log("ToolEditorForm loading starter package.");
            NewLanguagePackageTemplate(loadConfiguredPackage: false);
            UpdateUiState();
            ToolEditorDebugger.Log("ToolEditorForm starter package loaded in " + stopwatch.ElapsedMilliseconds.ToString("N0") + " ms.");
        }
        catch (Exception ex)
        {
            ToolEditorDebugger.ReportException("ToolEditor starter package failed", ex, showDialog: true);
            SetStatus(TToolEditor("tool_editor.status.starter_failed", "Starter package failed to load: ") + ex.Message, isError: true);
        }
    }

    private void StartWebViewInitialization()
    {
        if (_webViewInitializationStarted)
            return;

        _webViewInitializationStarted = true;
        ToolEditorDebugger.Log("ToolEditorForm starting preview WebView2 initialization.");
        _ = InitializeBrowserAsync();
    }

    private void StartHtmlEditorInitialization()
    {
        if (_htmlEditorInitializationStarted)
            return;

        _htmlEditorInitializationStarted = true;
        ToolEditorDebugger.Log("ToolEditorForm starting HTML editor WebView2 initialization.");
        _ = InitializeHtmlEditorAsync();
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip
        {
            Dock = DockStyle.Top
        };

        var file = new ToolStripMenuItem(TToolEditor("tool_editor.menu.file", "File"));
        file.DropDownItems.Add(CreateMenuItem(TToolEditor("tool_editor.menu.file.new_package", "New package"), Keys.Control | Keys.N, async (_, _) => await NewLanguagePackageAsync()));
        file.DropDownItems.Add(CreateMenuItem(TToolEditor("tool_editor.menu.file.open_package", "Open language package..."), Keys.Control | Keys.O, async (_, _) => await OpenLanguagePackageAsync()));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(CreateMenuItem(TToolEditor("tool_editor.menu.file.save_concept", "Save concept language package"), Keys.Control | Keys.S, async (_, _) => await SaveConceptLanguagePackageAsync()));
        file.DropDownItems.Add(TToolEditor("tool_editor.menu.file.save_current", "Save current document"), null, (_, _) => SaveCurrent());
        file.DropDownItems.Add(TToolEditor("tool_editor.menu.file.compile", "Compile language package..."), null, async (_, _) => await CompileLanguagePackageAsync());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(TToolEditor("tool_editor.menu.file.close", "Close"), null, (_, _) => Close());

        var edit = new ToolStripMenuItem(TToolEditor("tool_editor.menu.edit", "Edit"));
        edit.DropDownItems.Add(CreateMenuItem(TToolEditor("tool_editor.menu.edit.undo", "Undo"), Keys.Control | Keys.Z, (_, _) => _current?.Editor.Undo()));
        edit.DropDownItems.Add(CreateMenuItem(TToolEditor("tool_editor.menu.edit.cut", "Cut"), Keys.Control | Keys.X, (_, _) => _current?.Editor.Cut()));
        edit.DropDownItems.Add(CreateMenuItem(TToolEditor("tool_editor.menu.edit.copy", "Copy"), Keys.Control | Keys.C, (_, _) => _current?.Editor.Copy()));
        edit.DropDownItems.Add(CreateMenuItem(TToolEditor("tool_editor.menu.edit.paste", "Paste"), Keys.Control | Keys.V, (_, _) => _current?.Editor.Paste()));
        edit.DropDownItems.Add(CreateMenuItem(TToolEditor("tool_editor.menu.edit.select_all", "Select all"), Keys.Control | Keys.A, (_, _) => _current?.Editor.SelectAll()));

        var view = new ToolStripMenuItem(TToolEditor("tool_editor.menu.view", "View"));
        view.DropDownItems.Add(CreateMenuItem(TToolEditor("tool_editor.menu.view.refresh_preview", "Refresh preview"), Keys.F5, (_, _) => ShowPreviewPane()));
        view.DropDownItems.Add(TToolEditor("tool_editor.menu.view.validate", "Validate"), null, (_, _) => ValidateCurrent(showMessage: true));

        var package = new ToolStripMenuItem(TToolEditor("tool_editor.menu.package", "Package"));
        package.DropDownItems.Add(TToolEditor("tool_editor.menu.package.show_media", "Show media files"), null, (_, _) => ShowMediaFileListDialog());
        package.DropDownItems.Add(new ToolStripSeparator());
        package.DropDownItems.Add(TToolEditor("tool_editor.menu.package.show_manifest", "Show manifest"), null, (_, _) => ShowManifestDialog());
        package.DropDownItems.Add(TToolEditor("tool_editor.menu.package.validate_manifest", "Validate manifest"), null, (_, _) => ValidateManifest(showMessage: true));
        package.DropDownItems.Add(TToolEditor("tool_editor.menu.package.add_media", "Add media..."), null, (_, _) => OpenMediaDocument());
        package.DropDownItems.Add(TToolEditor("tool_editor.menu.package.replace_media", "Replace media..."), null, (_, _) => ReplaceCurrentMedia());
        package.DropDownItems.Add(TToolEditor("tool_editor.menu.package.delete_selected", "Delete selected file"), null, (_, _) => DeleteCurrentDocument());
        package.DropDownItems.Add(TToolEditor("tool_editor.menu.package.check_links", "Check HTML links"), null, (_, _) => ValidatePackageLinks(showMessage: true));
        package.DropDownItems.Add(new ToolStripSeparator());
        package.DropDownItems.Add(TToolEditor("tool_editor.menu.package.compile", "Compile package..."), null, async (_, _) => await CompileLanguagePackageAsync());
        package.DropDownItems.Add(TToolEditor("tool_editor.menu.package.sha256", "Calculate SHA-256..."), null, (_, _) => ShowPackageSha256());
        package.DropDownItems.Add(TToolEditor("tool_editor.menu.package.encryption", "Encryption..."), null, (_, _) => ShowEncryptionStatus());
        package.DropDownItems.Add(new ToolStripSeparator());
        package.DropDownItems.Add(TToolEditor("tool_editor.menu.package.view_media", "View media"), null, (_, _) => SelectFirstGroup(TreeLabel("media", "Media en afbeeldingen")));

        var extra = new ToolStripMenuItem(TToolEditor("tool_editor.menu.extra", "Extra"));
        extra.DropDownItems.Add(TToolEditor("tool_editor.security.menu", "Security settings..."), null, (_, _) => ShowSecurityOptions());
        if (ToolEditorDebugger.IsEnabled)
            extra.DropDownItems.Add(TToolEditor("tool_editor.menu.extra.open_debug_log", "Open debug log"), null, (_, _) => ToolEditorDebugger.OpenLogFolder());

        var help = new ToolStripMenuItem(TToolEditor("tool_editor.menu.help", "Help"));
        help.DropDownItems.Add(CreateMenuItem(TToolEditor("tool_editor.menu.help.open", "ToolEditor help"), Keys.F1, (_, _) => ShowToolEditorHelp()));
        help.DropDownItems.Add(new ToolStripSeparator());
        help.DropDownItems.Add(TToolEditor("tool_editor.menu.help.about", "About Syscalculator"), null, (_, _) => ShowAboutSyscalculator());

        menu.Items.Add(file);
        menu.Items.Add(edit);
        menu.Items.Add(view);
        menu.Items.Add(package);
        menu.Items.Add(extra);
        menu.Items.Add(help);
        return menu;
    }

    private static ToolStripMenuItem CreateMenuItem(string text, Keys shortcutKeys, EventHandler click)
    {
        var item = new ToolStripMenuItem(text)
        {
            ShortcutKeys = shortcutKeys
        };
        item.Click += click;
        return item;
    }

    private void ShowSecurityOptions()
    {
        using var dialog = new ToolEditorSecurityOptionsForm(TToolEditor);
        dialog.ShowDialog(this);
    }

    private ToolEditorLanguageInfo GetConfiguredToolEditorLanguage()
    {
        return _cachedConfiguredToolEditorLanguage ??= ReadConfiguredToolEditorLanguage();
    }

    private string TToolEditor(string key, string fallback)
    {
        var language = GetConfiguredToolEditorLanguage();
        var texts = LoadToolEditorHelpTexts(language);
        return texts.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? DecodeToolEditorText(value)
            : fallback;
    }

    private static string DecodeToolEditorText(string value)
    {
        return value
            .Replace("\\r\\n", "\r\n", StringComparison.Ordinal)
            .Replace("\\n", "\n", StringComparison.Ordinal);
    }

    private void ShowToolEditorHelp()
    {
        var language = GetConfiguredToolEditorLanguage();
        var texts = LoadToolEditorHelpTexts(language);
        string? ResolveText(string key) => texts.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
        string T(string key, string fallback) => HelpApi.Text(ResolveText, key, fallback);

        HelpApi.ShowDialog(this, new HelpDialogOptions(
            T("tool_editor.help.title", "ToolEditor help"),
            BuildToolEditorHelpPages(language.Code, ResolveText, fileName => ResolveToolEditorHelpContent(language, fileName)),
            "overview",
            new HelpNavigationLabels(
                T("help.nav.home", "Start"),
                T("help.nav.previous", "Vorige"),
                T("help.nav.next", "Volgende"))));
    }

    private void ShowAboutSyscalculator()
    {
        using var form = new ToolEditorAboutForm();
        form.ShowDialog(this);
    }

    private static IReadOnlyList<NodHelpPage> BuildToolEditorHelpPages(
        string languageCode,
        HelpTextResolver resolveText,
        HelpContentResolver resolveContent)
    {
        return
        [
            BuildToolEditorHelpPage(languageCode, resolveText, resolveContent, "overview", "tool_editor.help.page.overview", "ToolEditor gebruiken", "overview.html", BuildToolEditorOverviewFallback()),
            BuildToolEditorHelpPage(languageCode, resolveText, resolveContent, "package", "tool_editor.help.page.package", "Taalpakket workflow", "package-workflow.html", BuildToolEditorPackageFallback()),
            BuildToolEditorHelpPage(languageCode, resolveText, resolveContent, "html", "tool_editor.help.page.html", "HTML bewerken", "html-editor.html", BuildToolEditorHtmlFallback()),
            BuildToolEditorHelpPage(languageCode, resolveText, resolveContent, "media", "tool_editor.help.page.media", "Media en afbeeldingen", "media.html", BuildToolEditorMediaFallback()),
            BuildToolEditorHelpPage(languageCode, resolveText, resolveContent, "compile", "tool_editor.help.page.compile", "Valideren en compileren", "compile.html", BuildToolEditorCompileFallback())
        ];
    }

    private static NodHelpPage BuildToolEditorHelpPage(
        string languageCode,
        HelpTextResolver resolveText,
        HelpContentResolver resolveContent,
        string id,
        string titleKey,
        string fallbackTitle,
        string fileName,
        string fallback)
    {
        var title = HelpApi.Text(resolveText, titleKey, fallbackTitle);
        var body = HelpApi.Content(
            languageCode,
            resolveText,
            "tool_editor.help.content." + id,
            "tool-editor/" + fileName,
            fallback,
            resolveContent);
        return new NodHelpPage(id, title, HelpHtml.WrapTopicPage(title, body, HelpApi.NodHelpCss(), ToolEditorHelpPreviewScript()));
    }

    private static string BuildToolEditorOverviewFallback()
    {
        return """
        <p>ToolEditor bewerkt Tiedragon-taalpakketten: vertaling, help, NOD-documentatie, formulekaart en media.</p>
        <ul>
          <li>Links staat de vaste pakketstructuur.</li>
          <li>Boven werk je aan de actieve tab.</li>
          <li>Bij bronweergave toont de rechterzijde of onderzijde de HTML-voorbeeldweergave.</li>
        </ul>
        """;
    }

    private static string BuildToolEditorPackageFallback()
    {
        return """
        <p>Gebruik <b>Nieuw pakket</b> voor een basispakket, <b>Concept opslaan</b> voor werkbestanden en <b>Taalpakket compileren</b> voor een gecontroleerd .lngpdk-bestand.</p>
        """;
    }

    private static string BuildToolEditorHtmlFallback()
    {
        return """
        <p>HTML-documenten hebben twee standen: <b>Bron</b> voor broncode en <b>Bewerken</b> voor directe bewerking.</p>
        <p>De knoppen H1, H2, P, Info, Tip, Warn, Code en Kbd voegen standaard helpblokken in.</p>
        """;
    }

    private static string BuildToolEditorMediaFallback()
    {
        return """
        <p>Media bevat alleen toegestane afbeeldingen zoals png, jpg, svg, webp, gif en bmp. Sleep en zoom in de afbeeldingpreview om details te controleren.</p>
        """;
    }

    private static string BuildToolEditorCompileFallback()
    {
        return """
        <p>Valideren controleert structuur, links, media en scripts. Compileren maakt een .lngpdk met checksum en strikte bestandslijst.</p>
        """;
    }

    private Dictionary<string, string> LoadToolEditorHelpTexts(ToolEditorLanguageInfo language)
    {
        string sourceText;
        if (TryReadLanguageTextFromOpenPackage(language.Code, out var openPackageText))
            sourceText = openPackageText;
        else if (!string.IsNullOrWhiteSpace(language.PackagePath) &&
            TryReadPackageTextEntry(language.PackagePath, "language/" + language.Code + ".lng", out var packageText))
        {
            sourceText = packageText;
        }
        else
            sourceText = LoadLanguageText(language.Code, language.DisplayName);

        if (_cachedToolEditorHelpTexts is not null &&
            string.Equals(_cachedToolEditorHelpLanguageCode, language.Code, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(_cachedToolEditorHelpSourceText, sourceText, StringComparison.Ordinal))
        {
            return _cachedToolEditorHelpTexts;
        }

        _cachedToolEditorHelpLanguageCode = language.Code;
        _cachedToolEditorHelpSourceText = sourceText;
        _cachedToolEditorHelpTexts = ReadLanguageText(sourceText);
        return _cachedToolEditorHelpTexts;
    }

    private bool TryReadLanguageTextFromOpenPackage(string languageCode, out string text)
    {
        var packagePath = "language/" + NormalizeLanguageCode(languageCode) + ".lng";
        var document = _documents.FirstOrDefault(document =>
            NormalizePackagePath(document.PackagePath).Equals(packagePath, StringComparison.OrdinalIgnoreCase));
        if (document is not null)
        {
            text = document.Editor.Text;
            return true;
        }

        text = "";
        return false;
    }

    private string? ResolveToolEditorHelpContent(ToolEditorLanguageInfo language, string fileName)
    {
        var normalized = NormalizePackagePath(fileName);
        var packagePath = "help/content/" + normalized;
        var openDocument = _documents.FirstOrDefault(document =>
            NormalizePackagePath(document.PackagePath).Equals(packagePath, StringComparison.OrdinalIgnoreCase));
        if (openDocument is not null)
            return openDocument.Editor.Text;

        if (!string.IsNullOrWhiteSpace(language.PackagePath) &&
            TryReadPackageTextEntry(language.PackagePath, packagePath, out var packageContent))
        {
            return packageContent;
        }

        return null;
    }

    private static bool TryReadPackageTextEntry(string packagePath, string entryName, out string content)
    {
        content = "";
        try
        {
            var normalizedEntryName = NormalizePackagePath(entryName);
            var payload = ReadLanguagePackagePayload(packagePath);
            using var memory = new MemoryStream(payload);
            using var archive = new ZipArchive(memory, ZipArchiveMode.Read);
            var entry = archive.Entries.FirstOrDefault(entry =>
                NormalizePackagePath(entry.FullName).Equals(normalizedEntryName, StringComparison.OrdinalIgnoreCase));
            if (entry is null)
                return false;

            using var stream = entry.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            content = reader.ReadToEnd();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Dictionary<string, string> ReadLanguageText(string content)
    {
        var texts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in content.Split(["\r\n", "\n"], StringSplitOptions.None))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';'))
                continue;

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            var value = DecodeLanguageValue(line[(separator + 1)..].Trim());
            if (key.Length > 0)
                texts[key] = value;
        }

        return texts;
    }

    private static string DecodeLanguageValue(string value)
    {
        return value
            .Replace("\\r\\n", "\n", StringComparison.Ordinal)
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace("\\t", "\t", StringComparison.Ordinal);
    }

    private ToolStrip CreateHtmlToolbar()
    {
        var toolbar = new ToolStrip
        {
            GripStyle = ToolStripGripStyle.Hidden,
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(2, 2, 2, 2),
            RenderMode = ToolStripRenderMode.System
        };

        toolbar.Items.Add(_sourceModeButton);
        toolbar.Items.Add(_editModeButton);
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(CreateHtmlDropDown("Paragraph", "Teksttype", [
            ("Paragraph", "Paragraaf invoegen", () => WrapHtmlSelection("p", "Tekst")),
            ("Heading 1", "Kop 1 invoegen", () => WrapHtmlSelection("h1", "Kop")),
            ("Heading 2", "Kop 2 invoegen", () => WrapHtmlSelection("h2", "Kop")),
            ("Code block", "Codeblok invoegen", () => InsertHtmlSnippet("<pre><code>code</code></pre>"))
        ]));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(CreateHtmlButton("B", "Vet", (_, _) => ApplyHtmlInlineCommand("bold", "strong", "tekst")));
        toolbar.Items.Add(CreateHtmlButton("I", "Cursief", (_, _) => ApplyHtmlInlineCommand("italic", "em", "tekst")));
        toolbar.Items.Add(CreateHtmlDropDown("A", "Tekstkleur", [
            ("Blauw", "Blauwe tekst", () => ApplyHtmlColor("#003f8f")),
            ("Groen", "Groene tekst", () => ApplyHtmlColor("#17633a")),
            ("Rood", "Rode tekst", () => ApplyHtmlColor("#b42318")),
            ("Standaard", "Standaard tekstkleur", () => WrapHtmlSelection("span", "tekst"))
        ]));
        toolbar.Items.Add(CreateHtmlButton("Link", "Link invoegen", (_, _) => InsertHtmlLink()));
        toolbar.Items.Add(CreateHtmlButton("Quote", "Citaat invoegen", (_, _) => InsertHtmlSnippet("<blockquote>Citaat</blockquote>")));
        toolbar.Items.Add(CreateHtmlButton("Cite", "Bronvermelding invoegen", (_, _) => WrapHtmlSelection("cite", "Bron")));
        toolbar.Items.Add(CreateHtmlDropDown("List", "Lijst invoegen", [
            ("Bullets", "Opsomming invoegen", () => InsertHtmlSnippet("<ul>\r\n  <li>Item</li>\r\n</ul>")),
            ("Numbers", "Genummerde lijst invoegen", () => InsertHtmlSnippet("<ol>\r\n  <li>Item</li>\r\n</ol>"))
        ]));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(CreateHtmlDropDown("Insert", "Invoegen", [
            ("Image", "Afbeelding uit media invoegen", InsertHtmlImage),
            ("Info", "Infoblok invoegen", () => InsertHtmlSnippet("<div class=\"help-info\">Informatie</div>")),
            ("Tip", "Tipblok invoegen", () => InsertHtmlSnippet("<div class=\"help-tip\">Tip</div>")),
            ("Warn", "Waarschuwing invoegen", () => InsertHtmlSnippet("<div class=\"help-warning\">Waarschuwing</div>")),
            ("Keyboard", "Toets/keyboard invoegen", () => InsertHtmlSnippet("<kbd>Ctrl</kbd>")),
            ("Line break", "Regeleinde invoegen", () => InsertHtmlSnippet("<br>"))
        ]));
        toolbar.Items.Add(CreateHtmlDropDown("Symbol", "Speciale tekens", [
            ("Omega", "Omega invoegen", () => InsertHtmlSnippet("Ω")),
            ("Multiply", "Vermenigvuldigingsteken invoegen", () => InsertHtmlSnippet("×")),
            ("Divide", "Deelteken invoegen", () => InsertHtmlSnippet("÷")),
            ("Plus minus", "Plusminus invoegen", () => InsertHtmlSnippet("±")),
            ("Square root", "Wortelteken invoegen", () => InsertHtmlSnippet("√")),
            ("Arrow", "Pijl invoegen", () => InsertHtmlSnippet("→"))
        ]));
        return toolbar;
    }

    private static ToolStripButton CreateHtmlModeButton(string text, string tooltip, EventHandler click)
    {
        var button = new ToolStripButton(text)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            AutoSize = false,
            Width = 68,
            Height = 26,
            CheckOnClick = false,
            ToolTipText = tooltip,
            Padding = new Padding(6, 1, 6, 1),
            Margin = new Padding(1, 1, 1, 1)
        };
        button.Click += click;
        return button;
    }

    private static ToolStripButton CreateHtmlButton(string text, string tooltip, EventHandler click)
    {
        var button = new ToolStripButton(text)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            AutoSize = true,
            ToolTipText = tooltip,
            Padding = new Padding(4, 1, 4, 1)
        };
        button.Click += click;
        return button;
    }

    private static ToolStripDropDownButton CreateHtmlDropDown(
        string text,
        string tooltip,
        IEnumerable<(string Text, string Tooltip, Action Action)> items)
    {
        var button = new ToolStripDropDownButton(text)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            AutoSize = true,
            ToolTipText = tooltip,
            Padding = new Padding(4, 1, 4, 1)
        };

        foreach (var item in items)
        {
            var menuItem = new ToolStripMenuItem(item.Text)
            {
                ToolTipText = item.Tooltip
            };
            menuItem.Click += (_, _) => item.Action();
            button.DropDownItems.Add(menuItem);
        }

        return button;
    }

    private void NewLanguagePackageTemplate(bool loadConfiguredPackage)
    {
        ToolEditorDebugger.Log("NewLanguagePackageTemplate: reading configured language.");
        var language = GetConfiguredToolEditorLanguage();
        ToolEditorDebugger.Log("NewLanguagePackageTemplate: language=" + language.Code +
            "; package=" + (string.IsNullOrWhiteSpace(language.PackagePath) ? "(none)" : language.PackagePath));
        if (loadConfiguredPackage && !string.IsNullOrWhiteSpace(language.PackagePath))
        {
            ToolEditorDebugger.Log("NewLanguagePackageTemplate: loading configured language package archive.");
            LoadLanguagePackageArchive(language.PackagePath);
            ToolEditorDebugger.Log("NewLanguagePackageTemplate: configured archive loaded; documents=" + _documents.Count.ToString("N0"));
            var packageLanguageDocument = _documents.FirstOrDefault(document =>
                    document.PackagePath.Equals("language/" + language.Code + ".lng", StringComparison.OrdinalIgnoreCase)) ??
                _documents.FirstOrDefault(document => document.PackagePath.StartsWith("language/", StringComparison.OrdinalIgnoreCase)) ??
                _documents.FirstOrDefault();
            if (packageLanguageDocument is not null)
                SelectDocument(packageLanguageDocument);
            ToolEditorDebugger.Log("NewLanguagePackageTemplate: configured package selected.");
            return;
        }

        if (!loadConfiguredPackage && !string.IsNullOrWhiteSpace(language.PackagePath))
            ToolEditorDebugger.Log("NewLanguagePackageTemplate: configured package is active, but startup uses editable starter concept.");

        _conceptFolder = null;
        ToolEditorDocument languageDocument;
        using (SuspendNavigationRefresh())
        {
            ToolEditorDebugger.Log("NewLanguagePackageTemplate: building manifest and language document.");
            _manifestText = BuildManifestTemplate(language.Code, language.DisplayName, language.NativeName);
            languageDocument = AddDocument("language/" + language.Code + ".lng", LoadLanguageText(language.Code, language.DisplayName), null);
            ToolEditorDebugger.Log("NewLanguagePackageTemplate: adding HTML templates.");
            AddHtmlTemplateDocuments();
            ToolEditorDebugger.Log("NewLanguagePackageTemplate: adding Help runtime documents.");
            AddHelpRuntimeDocuments();
            ToolEditorDebugger.Log("NewLanguagePackageTemplate: adding main help documents.");
            AddMainHelpMediaDocuments();
            AddMainHelpDocuments(language.Code);
            ToolEditorDebugger.Log("NewLanguagePackageTemplate: adding NOD help documents.");
            AddNodHelpDocuments();
            ToolEditorDebugger.Log("NewLanguagePackageTemplate: adding formula card documents.");
            AddFormulaCardDocuments();
            ToolEditorDebugger.Log("NewLanguagePackageTemplate: adding ToolEditor help documents.");
            AddToolEditorHelpDocuments();
            ToolEditorDebugger.Log("NewLanguagePackageTemplate: adding legal documents.");
            AddLegalDocuments(language.Code);
        }

        RefreshFileTree();
        RefreshDocumentList();
        SelectDocument(languageDocument);
        ToolEditorDebugger.Log("NewLanguagePackageTemplate: starter package selected; documents=" + _documents.Count.ToString("N0"));
    }

    private void AddHtmlTemplateDocuments()
    {
        AddDocument("source/templates/formula/index.html", FormulaIndexPackageTemplate, null);
        AddDocument("source/templates/formula/card.html", FormulaCardPackageTemplate, null);
        AddDocument("source/templates/nod/full-page.html", NodFullPagePackageTemplate, null);
        AddDocument("source/templates/nod/command-page.html", NodCommandPagePackageTemplate, null);
        AddDocument("source/templates/nod/popup.html", NodPopupPackageTemplate, null);
        AddDocument("source/templates/nod/snippet.html", NodSnippetPackageTemplate, null);
    }

    private void AddHelpRuntimeDocuments()
    {
        foreach (var fileName in new[]
                 {
                     "basis.js",
                     "formula.js",
                     "nod.js",
                     "main-help.js",
                     "nod-popup.js",
                     "main-help.css",
                     "nod-popup.css",
                     "formula-film.css",
                     "document-body.html",
                     "document-topic.html",
                     "formula-card.html",
                     "formula-film.html"
                 })
        {
            AddHelpRootResourceDocument(fileName);
        }

        AddHelpApiReferenceDocuments();
    }

    private void AddHelpRootResourceDocument(string fileName)
    {
        var sourcePath = FindToolEditorResourcePath("Resources/Help/" + fileName) ??
            FindRepositoryPath("src/syscalculator/Resources/Help/" + fileName);
        if (sourcePath is null)
            return;

        AddLazyDocument("help/" + fileName, () => File.ReadAllText(sourcePath, Encoding.UTF8), sourcePath);
    }

    private void AddHelpApiReferenceDocuments()
    {
        var helpApiRoot = FindToolEditorResourcePath("Resources/Help/Content/HelpApi") ??
            FindRepositoryPath("src/syscalculator/Resources/Help/Content/HelpApi");
        if (helpApiRoot is null)
            return;

        foreach (var file in Directory.GetFiles(helpApiRoot, "*.html")
                     .OrderBy(path => HelpApiReferenceSortKey(Path.GetFileName(path)), StringComparer.OrdinalIgnoreCase))
        {
            AddLazyDocument("help/content/HelpApi/" + Path.GetFileName(file), () => File.ReadAllText(file, Encoding.UTF8), file);
        }
    }

    private static string HelpApiReferenceSortKey(string fileName)
    {
        return fileName.ToLowerInvariant() switch
        {
            "readme.html" => "0",
            "javascript-css.html" => "1",
            "notice.html" => "2",
            "warning.html" => "3",
            "details.html" => "4",
            "code.html" => "5",
            "table.html" => "6",
            "screenshot.html" => "7",
            "command-link.html" => "8",
            "example-card.html" => "9",
            "dont.html" => "99",
            _ => "50-" + fileName
        };
    }

    private async Task NewLanguagePackageAsync()
    {
        if (!await ConfirmResetPackageAsync())
            return;

        ClearPackageDocuments();
        NewLanguagePackageTemplate(loadConfiguredPackage: false);
        SetStatus(TToolEditor("tool_editor.status.new_package_created", "New base package created from the configured Syscalculator language."), isError: false);
    }

    private static string BuildManifestTemplate(string languageCode, string displayName, string nativeName)
    {
        var manifest = new Dictionary<string, object?>
        {
            ["format"] = 1,
            ["key"] = languageCode,
            ["id"] = "tiedragon.language." + languageCode,
            ["producer"] = "Tiedragon",
            ["product"] = "Syscalculator",
            ["softwareId"] = "tiedragon.syscalculator",
            ["languageCode"] = languageCode,
            ["displayName"] = displayName,
            ["nativeName"] = nativeName,
            ["packageVersion"] = DateTime.Now.ToString("yyyy.MM.dd.001"),
            ["fallbackLanguage"] = "eng",
        };

        return JsonSerializer.Serialize(manifest, LanguagePackageJsonOptions);
    }

    private static ToolEditorLanguageInfo ReadConfiguredToolEditorLanguage()
    {
        var code = "eng";
        string? packageKey = null;
        var configPath = Path.Combine(AppContext.BaseDirectory, "language.cfg");
        if (File.Exists(configPath))
        {
            foreach (var rawLine in File.ReadAllLines(configPath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';'))
                    continue;

                var separator = line.IndexOf('=');
                if (separator <= 0)
                    continue;

                var key = line[..separator].Trim();
                var value = line[(separator + 1)..].Trim();
                if (key.Equals("language", StringComparison.OrdinalIgnoreCase))
                {
                    code = Path.GetFileNameWithoutExtension(value);
                    continue;
                }

                if (key.Equals("languagePackage", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
                    packageKey = value;
            }
        }

        if (!string.IsNullOrWhiteSpace(packageKey) &&
            TryFindConfiguredLanguagePackage(packageKey, out var packageLanguage))
        {
            return packageLanguage;
        }

        if (string.IsNullOrWhiteSpace(code))
            code = "eng";

        code = NormalizeLanguageCode(code);
        return new ToolEditorLanguageInfo(code, LanguageDisplayName(code), LanguageNativeName(code), null, null);
    }

    private static bool TryFindConfiguredLanguagePackage(string packageKey, out ToolEditorLanguageInfo language)
    {
        language = null!;
        foreach (var directory in EnumerateLanguagePackageDirectories())
        {
            if (!Directory.Exists(directory))
                continue;

            foreach (var packagePath in Directory.GetFiles(directory, "*.lngpdk").Concat(Directory.GetFiles(directory, "*.zip")))
            {
                if (TryReadLanguagePackageInfo(packagePath, out var candidate) &&
                    string.Equals(candidate.PackageKey, packageKey, StringComparison.OrdinalIgnoreCase))
                {
                    language = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private static IEnumerable<string> EnumerateLanguagePackageDirectories()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "LanguagePackages");
        yield return Path.Combine(AppContext.BaseDirectory, "LanguagePackages", "Cache");

        var webPackages = FindRepositoryPath("web/packages/languages");
        if (webPackages is not null)
            yield return webPackages;
    }

    private static bool TryReadLanguagePackageInfo(string packagePath, out ToolEditorLanguageInfo language)
    {
        language = null!;
        try
        {
            var payload = ReadLanguagePackagePayload(packagePath);
            using var memory = new MemoryStream(payload);
            using var archive = new ZipArchive(memory, ZipArchiveMode.Read);
            var entry = archive.Entries.FirstOrDefault(entry =>
                NormalizePackagePath(entry.FullName).Equals("manifest.json", StringComparison.OrdinalIgnoreCase));
            if (entry is null)
                return false;

            using var stream = entry.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var json = JsonDocument.Parse(reader.ReadToEnd());
            var root = json.RootElement;
            var code = NormalizeLanguageCode(GetManifestString(root, "languageCode"));
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var key = GetManifestString(root, "key");
            if (string.IsNullOrWhiteSpace(key))
                key = GetManifestString(root, "id");
            if (string.IsNullOrWhiteSpace(key))
                return false;

            var displayName = GetManifestString(root, "displayName");
            var nativeName = GetManifestString(root, "nativeName");
            language = new ToolEditorLanguageInfo(
                code,
                string.IsNullOrWhiteSpace(displayName) ? LanguageDisplayName(code) : displayName,
                string.IsNullOrWhiteSpace(nativeName) ? LanguageNativeName(code) : nativeName,
                key,
                packagePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeLanguageCode(string code)
    {
        return code.ToLowerInvariant() switch
        {
            "nl" or "nl-nl" => "ned",
            "en" or "en-us" or "en-gb" => "eng",
            "de" => "deu",
            "fr" => "fra",
            "es" or "esp" => "spa",
            "it" => "ita",
            "pt" => "por",
            "id" => "ind",
            "zh" or "zh-cn" or "zh-tw" => "zho",
            _ => code.ToLowerInvariant()
        };
    }

    private static string LanguageDisplayName(string code)
    {
        return code.ToLowerInvariant() switch
        {
            "ned" => "Nederlands",
            "eng" => "English",
            "deu" => "Deutsch",
            "fra" => "Français",
            "spa" => "Español",
            "ita" => "Italiano",
            "por" => "Português",
            "ind" => "Indonesia",
            "zho" => "中文",
            _ => code.ToUpperInvariant()
        };
    }

    private static string LanguageNativeName(string code)
    {
        return code.ToLowerInvariant() switch
        {
            "ind" => "Bahasa Indonesia",
            _ => LanguageDisplayName(code)
        };
    }

    private static string BuildEnglishHtmlTemplate()
    {
        return """
        <h1>Syscalculator Help</h1>
        <p>This starter language package contains the editable package structure for Syscalculator help, NOD help, formula cards, translations and media.</p>
        <div class="help-info">Use this English base package as the source for a new translation package.</div>
        """;
    }

    private void AddMainHelpMediaDocuments()
    {
        AddMainHelpMediaDocument("WizardExpressHelp.png");
        AddMainHelpMediaDocument("NodEditorHelp.svg");
    }

    private void AddMainHelpMediaDocument(string fileName)
    {
        var sourcePath = FindToolEditorResourcePath("Resources/" + fileName) ??
            FindRepositoryPath("src/syscalculator/Resources/" + fileName);
        if (sourcePath is null)
            return;

        AddImageDocument("assets/" + fileName, File.ReadAllBytes(sourcePath), sourcePath);
    }

    private void AddMainHelpDocuments(string languageCode)
    {
        var pages = new (string BodyKey, string ResourceName)[]
        {
            ("help.main.page.intro.body", "intro.html"),
            ("help.main.page.main.body", "main.html"),
            ("help.main.page.fields.body", "fields.html"),
            ("help.main.page.wizard.body", "wizard.html"),
            ("help.main.page.calculator.body", "calculator.html"),
            ("help.main.page.applications.body", "applications.html"),
            ("help.main.page.configuration.body", "configuration.html"),
            ("help.main.page.updater.body", "updater.html"),
            ("help.main.page.window.body", "window.html"),
            ("help.main.page.nodfiles.body", "nodfiles.html"),
            ("help.main.page.nodeditor.body", "nodeditor.html"),
            ("help.main.page.support.body", "support.html")
        };

        foreach (var page in pages)
        {
            var sourcePath = FindToolEditorResourcePath("Resources/Help/Content/main/" + page.ResourceName) ??
                FindRepositoryPath("src/syscalculator/Resources/Help/Content/main/" + page.ResourceName);
            AddLazyDocument(
                "help/content/main/" + page.ResourceName,
                () => BuildMainHelpDocumentSource(languageCode, page.BodyKey, page.ResourceName, sourcePath),
                sourcePath);
        }
    }

    private string BuildMainHelpDocumentSource(string languageCode, string bodyKey, string resourceName, string? sourcePath)
    {
        var languageMap = GetCurrentLanguageMap();
        string? ResolveText(string key) => languageMap.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

        var fallback = sourcePath is null ? BuildEnglishHtmlTemplate() : File.ReadAllText(sourcePath, Encoding.UTF8);
        var html = HelpApi.Content(
            languageCode,
            ResolveText,
            bodyKey,
            "main/" + resourceName,
            fallback);
        html = HelpHtml.ApplyContentPlaceholders(html, new Dictionary<string, string?>
        {
            ["NodEditorScreenshot"] = BuildPackageScreenshotImage(
                "assets/NodEditorHelp.svg",
                ResolveText("help.main.page.nodeditor.screenshot_alt") ?? "Screenshot of the NOD Editor with toolbar, code editor and command tip.") ?? HelpApi.ScreenshotImage(
                languageCode,
                ResolveText,
                "NodEditorHelp.svg",
                ResolveText("help.main.page.nodeditor.screenshot_alt") ?? "Screenshot of the NOD Editor with toolbar, code editor and command tip."),
            ["WizardExpressScreenshot"] = BuildPackageScreenshotImage(
                "assets/WizardExpressHelp.png",
                ResolveText("help.main.page.wizard.screenshot_alt") ?? "Screenshot of WizardExpress in front of Syscalculator and a spreadsheet.") ?? HelpApi.ScreenshotImage(
                languageCode,
                ResolveText,
                "WizardExpressHelp.png",
                ResolveText("help.main.page.wizard.screenshot_alt") ?? "Screenshot of WizardExpress in front of Syscalculator and a spreadsheet.")
        });
        return FormatGeneratedHelpSource(html);
    }

    private static string BuildConceptHelpBanner(string languagePrefix)
    {
        if (languagePrefix.Equals("nl", StringComparison.OrdinalIgnoreCase))
        {
            return """
            <div class="concept-banner" role="note" aria-label="Conceptwaarschuwing">
              <span class="concept-banner-icon" aria-hidden="true"><svg viewBox="0 0 32 32" focusable="false"><path d="M10 14a6 6 0 1 1 12 0c0 2.1-1 3.3-2 4.6-.8 1-1.5 1.9-1.7 3.4h-4.6c-.2-1.5-.9-2.4-1.7-3.4-1-1.3-2-2.5-2-4.6Z" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round"/><path d="M13 25h6M14 28h4M16 2v3M5 14H2M30 14h-3M7.5 5.5l2.1 2.1M24.5 5.5l-2.1 2.1" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"/></svg></span>
              <span><b>Concept:</b> deze helpinformatie is werkmateriaal voor een taalpackage en is nog geen officiele Syscalculator-help.</span>
              <button class="concept-banner-close" type="button" title="Sluiten" aria-label="Sluiten">x</button>
            </div>

            """;
        }

        return """
        <div class="concept-banner" role="note" aria-label="Concept warning">
          <span class="concept-banner-icon" aria-hidden="true"><svg viewBox="0 0 32 32" focusable="false"><path d="M10 14a6 6 0 1 1 12 0c0 2.1-1 3.3-2 4.6-.8 1-1.5 1.9-1.7 3.4h-4.6c-.2-1.5-.9-2.4-1.7-3.4-1-1.3-2-2.5-2-4.6Z" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round"/><path d="M13 25h6M14 28h4M16 2v3M5 14H2M30 14h-3M7.5 5.5l2.1 2.1M24.5 5.5l-2.1 2.1" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"/></svg></span>
          <span><b>Concept:</b> this help information is package draft material and is not official Syscalculator help yet.</span>
          <button class="concept-banner-close" type="button" title="Close" aria-label="Close">x</button>
        </div>

        """;
    }

    private void AddNodHelpDocuments()
    {
        var nodRoot = FindToolEditorResourcePath("Resources/Help/Content/nod") ??
            FindRepositoryPath("src/syscalculator/Resources/Help/Content/nod");
        if (nodRoot is null)
        {
            AddDocument("help/content/nod/full/index.html", BuildNodHelpFallback(), null);
            return;
        }

        var helpFiles = Directory.GetFiles(nodRoot, "*.html", SearchOption.AllDirectories)
            .OrderBy(path => BuildNodHelpSortKey(nodRoot, path), StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (helpFiles.Count == 0)
        {
            AddDocument("help/content/nod/full/index.html", BuildNodHelpFallback(), null);
            return;
        }

        var languageMap = GetCurrentLanguageMap();
        string? ResolveText(string key) => languageMap.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

        foreach (var file in helpFiles)
        {
            AddLazyDocument(
                BuildNodHelpPackagePath(nodRoot, file),
                () =>
                {
                    var html = File.ReadAllText(file, Encoding.UTF8);
                    html = HelpApi.ApplyLanguagePlaceholders(ResolveText, html);
                    html = HelpHtml.ApplyContentPlaceholders(html, new Dictionary<string, string?>
                    {
                        ["WarningBoardSvg"] = WarningBoardSvg()
                    });
                    return FormatGeneratedHelpSource(html);
                },
                file);
        }
    }

    private static string FormatGeneratedHelpSource(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return html;

        var formatted = html.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);
        formatted = Regex.Replace(
            formatted,
            @">\s*(?=<(h[1-6]|p|div|table|thead|tbody|tr|ul|ol|li|pre|details|summary)\b)",
            ">" + Environment.NewLine,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        formatted = Regex.Replace(
            formatted,
            @"</(h[1-6]|p|div|table|thead|tbody|tr|ul|ol|li|pre|details|summary)>\s*",
            match => match.Value.TrimEnd() + Environment.NewLine,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        formatted = Regex.Replace(
            formatted,
            @"\n{3,}",
            Environment.NewLine + Environment.NewLine,
            RegexOptions.CultureInvariant);

        return EnsureTrailingEndLine(formatted.Trim());
    }

    private static string BuildNodHelpSortKey(string nodRoot, string file)
    {
        var packagePath = BuildNodHelpPackagePath(nodRoot, file);
        if (packagePath.Equals("help/content/nod/full/index.html", StringComparison.OrdinalIgnoreCase))
            return "0";

        return packagePath;
    }

    private static string BuildNodHelpPackagePath(string nodRoot, string file)
    {
        var relative = Path.GetRelativePath(nodRoot, file).Replace('\\', '/');
        if (relative.Equals("full/title.html", StringComparison.OrdinalIgnoreCase))
            relative = "full/index.html";

        return "help/content/nod/" + relative;
    }

    private void AddFormulaCardDocuments()
    {
        var languageMap = GetCurrentLanguageMap();
        var cards = FormulaCardCatalog.GetDefaultCards();
        var localizedCards = cards.Select(card => LocalizeFormulaCard(card, languageMap)).ToArray();
        AddLazyDocument(
            "formula/index.html",
            () =>
            {
                var currentLanguageMap = GetCurrentLanguageMap();
                var currentCards = FormulaCardCatalog.GetDefaultCards()
                    .Select(card => LocalizeFormulaCard(card, currentLanguageMap))
                    .ToArray();
                var indexTemplate = FindTemplateText("source/templates/formula/index.html", "templates/formula-index.html", FormulaIndexPackageTemplate);
                return BuildFormulaIndexHtml(currentCards, currentLanguageMap, indexTemplate);
            },
            null);
        foreach (var card in localizedCards.OrderBy(card => card.Title, StringComparer.CurrentCultureIgnoreCase))
        {
            var source = card.Source;
            AddLazyDocument(
                BuildFormulaCardPackagePath(source),
                () =>
                {
                    var currentLanguageMap = GetCurrentLanguageMap();
                    var cardTemplate = FindTemplateText("source/templates/formula/card.html", "templates/formula-card.html", FormulaCardPackageTemplate);
                    return BuildFormulaCardHtml(LocalizeFormulaCard(source, currentLanguageMap), currentLanguageMap, cardTemplate);
                },
                null);
        }
    }

    private string FindTemplateText(string packagePath, string legacyPackagePath, string fallback)
    {
        var template = _documents.FirstOrDefault(document =>
            document.PackagePath.Equals(packagePath, StringComparison.OrdinalIgnoreCase)) ??
            _documents.FirstOrDefault(document =>
                document.PackagePath.Equals(legacyPackagePath, StringComparison.OrdinalIgnoreCase));
        return template is null ? fallback : template.Editor.Text;
    }

    private static string BuildNodHelpFallback()
    {
        return """
        <h1>NOD help</h1>
        <p>NOD is de teksttaal waarmee Syscalculator converters, berekeningen, tekstomzettingen en dataregels beschrijft.</p>
        <div class="help-info">De uitgebreide NOD-helpbronnen zijn niet gevonden in deze checkout.</div>
        """;
    }

    private static LocalizedFormulaCard LocalizeFormulaCard(FormulaCard card, IReadOnlyDictionary<string, string> languageMap)
    {
        var key = "formula_card.card." + card.Id + ".";
        return new LocalizedFormulaCard(
            card,
            T(languageMap, key + "title", card.Title),
            T(languageMap, key + "formula_text", card.Formula),
            T(languageMap, key + "plain_text", card.PlainText),
            SplitFormulaTags(T(languageMap, key + "tags", string.Join("|", card.LevelTags))),
            T(languageMap, key + "description", card.Description),
            T(languageMap, key + "example_nod", card.ExampleNod));
    }

    private static IReadOnlyList<string> SplitFormulaTags(string value)
    {
        return value
            .Split(['|', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToArray();
    }

    private static string T(IReadOnlyDictionary<string, string> languageMap, string key, string fallback)
    {
        return languageMap.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    private static string BuildFormulaIndexHtml(IReadOnlyList<LocalizedFormulaCard> cards, IReadOnlyDictionary<string, string> languageMap, string template)
    {
        var rows = new StringBuilder();
        foreach (var card in cards.OrderBy(card => card.Title, StringComparer.CurrentCultureIgnoreCase))
        {
            rows.Append("            <tr><td><a href=\"")
                .Append(WebUtility.HtmlEncode(BuildFormulaCardRelativeLink(card.Source)))
                .Append("\">")
                .Append(WebUtility.HtmlEncode(card.Title))
                .Append("</a></td><td>")
                .Append(WebUtility.HtmlEncode(FriendlyFormulaCategory(BuildFormulaCardCategory(card.Source))))
                .Append("</td><td>")
                .Append(WebUtility.HtmlEncode(string.Join(", ", card.Tags)))
                .Append("</td><td>")
                .Append(WebUtility.HtmlEncode(card.Description))
                .Append("</td></tr>")
                .AppendLine();
        }

        return ApplyFormulaTemplate(template, new Dictionary<string, string?>
        {
            ["title"] = WebUtility.HtmlEncode(T(languageMap, "formula_card.index.title", "Formulekaart")),
            ["intro"] = WebUtility.HtmlEncode(T(languageMap, "formula_card.index.intro", "De formulekaart bundelt wiskundige basisregels, MathML, LaTeX en voorbeeld-NOD voor gebruik in Syscalculator.")),
            ["column_formula"] = WebUtility.HtmlEncode(T(languageMap, "formula_card.index.column.formula", "Formule")),
            ["column_category"] = WebUtility.HtmlEncode(T(languageMap, "formula_card.index.column.category", "Categorie")),
            ["column_tags"] = WebUtility.HtmlEncode(T(languageMap, "formula_card.index.column.tags", "Tags")),
            ["column_description"] = WebUtility.HtmlEncode(T(languageMap, "formula_card.index.column.description", "Uitleg")),
            ["rows"] = rows.ToString()
        });
    }

    private static string BuildFormulaCardPackagePath(FormulaCard card)
    {
        return "formula/" + BuildFormulaCardCategory(card) + "/" + card.Id + ".html";
    }

    private static string BuildFormulaCardRelativeLink(FormulaCard card)
    {
        return BuildFormulaCardCategory(card) + "/" + card.Id + ".html";
    }

    private static string BuildFormulaCardCategory(FormulaCard card)
    {
        var tags = card.LevelTags;
        if (tags.Any(tag => tag.Equals("Statistiek", StringComparison.OrdinalIgnoreCase)))
            return "statistiek";
        if (tags.Any(tag => tag.Equals("Kansrekening", StringComparison.OrdinalIgnoreCase)))
            return "kansrekening";
        if (tags.Any(tag => tag.Equals("Goniometrie", StringComparison.OrdinalIgnoreCase)))
            return "goniometrie";
        if (tags.Any(tag => tag.Contains("Meetkunde", StringComparison.OrdinalIgnoreCase) || tag.Equals("2D", StringComparison.OrdinalIgnoreCase) || tag.Equals("3D", StringComparison.OrdinalIgnoreCase)))
            return "meetkunde";
        if (tags.Any(tag => tag.Contains("Analyse", StringComparison.OrdinalIgnoreCase) || tag.Contains("Different", StringComparison.OrdinalIgnoreCase) || tag.Contains("Integra", StringComparison.OrdinalIgnoreCase)))
            return "analyse";
        if (tags.Any(tag => tag.Equals("Algebra", StringComparison.OrdinalIgnoreCase)))
            return "algebra";

        return "basis";
    }

    private void AddToolEditorHelpDocuments()
    {
        var toolEditorRoot = FindToolEditorResourcePath("Resources/Help/Content/tool-editor") ??
            FindRepositoryPath("src/syscalculator/Resources/Help/Content/tool-editor");
        if (toolEditorRoot is not null)
        {
            foreach (var file in Directory.GetFiles(toolEditorRoot, "*.html")
                         .OrderBy(path => ToolEditorHelpSortKey(Path.GetFileName(path)), StringComparer.OrdinalIgnoreCase))
            {
                AddLazyDocument("help/content/tool-editor/" + Path.GetFileName(file), () => File.ReadAllText(file, Encoding.UTF8), file);
            }

            return;
        }

        AddDocument("help/content/tool-editor/overview.html", BuildToolEditorOverviewFallback(), null);
        AddDocument("help/content/tool-editor/package-workflow.html", BuildToolEditorPackageFallback(), null);
        AddDocument("help/content/tool-editor/html-editor.html", BuildToolEditorHtmlFallback(), null);
        AddDocument("help/content/tool-editor/media.html", BuildToolEditorMediaFallback(), null);
        AddDocument("help/content/tool-editor/compile.html", BuildToolEditorCompileFallback(), null);
    }

    private void AddLegalDocuments(string languageCode)
    {
        var languageMap = GetCurrentLanguageMap();
        string? ResolveText(string key) => languageMap.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

        AddLegalDocument(
            languageCode,
            ResolveText,
            "legal.license.title",
            "License Agreement",
            "license-agreement.html");
        AddLegalDocument(
            languageCode,
            ResolveText,
            "legal.privacy.title",
            "Privacy Statement",
            "privacy-statement.html");
    }

    private void AddLegalDocument(
        string languageCode,
        HelpTextResolver resolveText,
        string titleKey,
        string fallbackTitle,
        string fileName)
    {
        var title = HelpApi.Text(resolveText, titleKey, fallbackTitle);
        var sourcePath = FindToolEditorResourcePath("Resources/Help/Content/legal/" + fileName) ??
            FindRepositoryPath("src/syscalculator/Resources/Help/Content/legal/" + fileName);
        var body = HelpApi.Content(
            languageCode,
            resolveText,
            titleKey,
            "legal/" + fileName,
            sourcePath is null ? "" : File.ReadAllText(sourcePath, Encoding.UTF8));

        AddLazyDocument(
            "help/content/legal/" + fileName,
            () => HelpHtml.WrapTopicPage(title, body, HelpApi.MainHelpCss()),
            sourcePath);
    }

    private IReadOnlyDictionary<string, string> GetCurrentLanguageMap()
    {
        return ReadLanguageText(
            _documents.FirstOrDefault(document =>
                    document.PackagePath.StartsWith("language/", StringComparison.OrdinalIgnoreCase) &&
                    document.PackagePath.EndsWith(".lng", StringComparison.OrdinalIgnoreCase))
                ?.Editor.Text ?? "");
    }

    private static string ToolEditorHelpSortKey(string fileName)
    {
        return fileName.ToLowerInvariant() switch
        {
            "overview.html" => "0",
            "package-workflow.html" => "1",
            "html-editor.html" => "2",
            "media.html" => "3",
            "compile.html" => "4",
            _ => "9-" + fileName
        };
    }

    private static string BuildFormulaCardHtml(LocalizedFormulaCard card, IReadOnlyDictionary<string, string> languageMap, string template)
    {
        return ApplyFormulaTemplate(template, new Dictionary<string, string?>
        {
            ["title"] = WebUtility.HtmlEncode(card.Title),
            ["tags"] = WebUtility.HtmlEncode(string.Join(", ", card.Tags)),
            ["description"] = RenderFormulaCardDescription(card.Description),
            ["section_formula"] = WebUtility.HtmlEncode(T(languageMap, "formula_card.section.formula", "Formule")),
            ["formula_text"] = WebUtility.HtmlEncode(card.FormulaText),
            ["mathml_card"] = card.Source.MathMl,
            ["section_text"] = WebUtility.HtmlEncode(T(languageMap, "formula_card.section.text", "Tekst")),
            ["plain_text"] = WebUtility.HtmlEncode(card.PlainText),
            ["section_latex"] = WebUtility.HtmlEncode(T(languageMap, "formula_card.section.latex", "LaTeX")),
            ["latex"] = WebUtility.HtmlEncode(card.Source.Latex),
            ["section_mathml"] = WebUtility.HtmlEncode(T(languageMap, "formula_card.section.mathml", "MathML")),
            ["mathml_pre"] = WebUtility.HtmlEncode(card.Source.MathMl),
            ["section_example_nod"] = WebUtility.HtmlEncode(T(languageMap, "formula_card.section.example_nod", "Voorbeeld-NOD")),
            ["example_nod"] = WebUtility.HtmlEncode(card.ExampleNod)
        });
    }

    private static string ApplyFormulaTemplate(string template, IReadOnlyDictionary<string, string?> values)
    {
        var result = template;
        foreach (var (key, value) in values)
            result = result.Replace("{{" + key + "}}", value ?? "", StringComparison.Ordinal);

        return result;
    }

    private static string RenderFormulaCardDescription(string description)
    {
        var html = WebUtility.HtmlEncode(description);
        foreach (var command in new[] { "length", "distance", "dot", "angle", "angled", "cross", "det", "trace", "mget", "vec" })
        {
            html = Regex.Replace(
                html,
                @"\b" + Regex.Escape(command) + @"\b",
                "<a class=\"cmd-link\" href=\"nodpage:cmd:" + command + "\"><code>" + command + "</code></a>",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return html;
    }

    private static string LoadLanguageText(string languageCode, string displayName)
    {
        var path = FindToolEditorResourcePath("Resources/Languages/" + languageCode + ".lng") ??
            FindRepositoryPath("src/syscalculator/" + languageCode + ".lng");
        if (path is not null)
            return File.ReadAllText(path, Encoding.UTF8);

        return "# Syscalculator 2.0 language file\r\n" +
            "# Format: key=value\r\n\r\n" +
            "language.name=" + displayName + "\r\n" +
            """
            menu.tools.tool_editor=ToolEditor
            status.ready=Ready
            """;
    }

    private static string LoadDutchLanguageText()
    {
        return LoadLanguageText("ned", "Nederlands");
    }

    private static string LoadFallbackLanguageText()
    {
        return """
        # Syscalculator 2.0 taalbestand
        # Formaat: key=waarde

        language.name=Nederlands
        menu.tools.tool_editor=ToolEditor
        status.ready=Gereed
        """;
    }

    private static string? FindToolEditorResourcePath(string relativePath)
    {
        var path = Path.Combine(AppContext.BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path) || Directory.Exists(path))
            return path;

        return null;
    }

    private static string? FindRepositoryPath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(path) || Directory.Exists(path))
                return path;

            directory = directory.Parent;
        }

        return null;
    }

    private async void OpenDocument()
    {
        using var dialog = new OpenFileDialog
        {
            Title = TToolEditor("tool_editor.dialog.open_document.title", "Open ToolEditor document"),
            Filter = "Language package files (*.html;*.json;*.lng;*.css;*.js;*.png;*.jpg;*.jpeg;*.svg;*.webp;*.gif;*.bmp)|*.html;*.json;*.lng;*.css;*.js;*.png;*.jpg;*.jpeg;*.svg;*.webp;*.gif;*.bmp|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        await ExecuteOpenFilePipelineAsync(ToolEditorOpenRequest.SingleFile(dialog.FileName));
    }

    private async Task OpenLanguagePackageAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = TToolEditor("tool_editor.dialog.open_package.title", "Open language package"),
            Filter = TToolEditor("tool_editor.dialog.open_package.filter", "Tiedragon language packages (*.objpdk;*.lngpdk)|*.objpdk;*.lngpdk|Source manifest (manifest.json)|manifest.json|Legacy zip (*.zip)|*.zip|All files (*.*)|*.*"),
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var extension = Path.GetExtension(dialog.FileName);
        var request = Path.GetFileName(dialog.FileName).Equals("manifest.json", StringComparison.OrdinalIgnoreCase)
            ? ToolEditorOpenRequest.PackageSourceFolder(Path.GetDirectoryName(dialog.FileName)!)
            : extension.Equals(ObjectPackageExtension, StringComparison.OrdinalIgnoreCase)
                ? ToolEditorOpenRequest.ObjectPackage(dialog.FileName)
                : ToolEditorOpenRequest.PackageArchive(dialog.FileName);
        await ExecuteOpenFilePipelineAsync(request);
    }

    private async Task ExecuteOpenFilePipelineAsync(ToolEditorOpenRequest request)
    {
        if (request.ResetPackage && !await ConfirmResetPackageAsync())
            return;

        try
        {
            ToolEditorDebugger.Log("Open pipeline: " + request.Kind + " " + request.Path);
            ToolEditorDocument? selectedDocument = null;
            using (SuspendNavigationRefresh())
            {
                selectedDocument = ExecuteOpenFilePipelineStep(request);
            }

            RefreshFileTree();
            RefreshDocumentList();
            selectedDocument ??= request.SelectPreferredDocument ? FindPreferredOpenDocument() : null;
            if (selectedDocument is not null)
                SelectDocument(selectedDocument);

            if (request.ShowManifestDialog)
                ShowManifestDialog();

            SetStatus(TToolEditor(request.SuccessStatusKey, request.SuccessStatusFallback) + request.Path, isError: false);
            ToolEditorDebugger.Log("Open pipeline completed: documents=" + _documents.Count.ToString("N0"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException or JsonException)
        {
            SetStatus(TToolEditor(request.ErrorStatusKey, request.ErrorStatusFallback) + ex.Message, isError: true);
            MessageBox.Show(this, ex.Message, TToolEditor(request.DialogTitleKey, request.DialogTitleFallback), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private ToolEditorDocument? ExecuteOpenFilePipelineStep(ToolEditorOpenRequest request)
    {
        return request.Kind switch
        {
            ToolEditorOpenKind.SingleFile => OpenSinglePackageFile(request.Path),
            ToolEditorOpenKind.PackageSourceFolder => OpenPackageSourceFolder(request.Path),
            ToolEditorOpenKind.ObjectPackage => OpenObjectPackage(request.Path),
            ToolEditorOpenKind.PackageArchive => OpenPackageArchive(request.Path),
            _ => throw new InvalidOperationException("Onbekende open-pipeline stap.")
        };
    }

    private ToolEditorDocument? OpenSinglePackageFile(string fileName)
    {
        if (Path.GetFileName(fileName).Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
        {
            _manifestText = File.ReadAllText(fileName, Encoding.UTF8);
            return null;
        }

        if (IsImagePath(fileName))
            return AddOrReplaceImageDocument("assets/" + Path.GetFileName(fileName), File.ReadAllBytes(fileName), fileName, markDirty: false, selectDocument: false);

        if (TryBuildImportPackagePath(fileName, out var packagePath))
            return AddDocument(packagePath, File.ReadAllText(fileName, Encoding.UTF8), fileName);

        RejectUnsupportedPackageFile(fileName);
        return null;
    }

    private ToolEditorDocument? OpenPackageSourceFolder(string sourceFolder)
    {
        LoadPackageSourceFolder(sourceFolder);
        return FindPreferredOpenDocument();
    }

    private ToolEditorDocument? OpenObjectPackage(string packagePath)
    {
        LoadObjectPackageArchive(packagePath);
        return FindPreferredOpenDocument();
    }

    private ToolEditorDocument? OpenPackageArchive(string packagePath)
    {
        LoadLanguagePackageArchive(packagePath);
        return FindPreferredOpenDocument();
    }

    private ToolEditorDocument? FindPreferredOpenDocument()
    {
        return _documents.FirstOrDefault(document => document.PackagePath.StartsWith("language/", StringComparison.OrdinalIgnoreCase)) ??
            _documents.FirstOrDefault(document => document.ImageBytes is null) ??
            _documents.FirstOrDefault();
    }

    private async Task SaveConceptLanguagePackageAsync()
    {
        await SyncHtmlEditorToSourceAsync();

        var targetPath = _conceptPackagePath;
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            var manifest = ReadManifestProperties(_manifestText);
            using var dialog = new SaveFileDialog
            {
                Title = TToolEditor("tool_editor.dialog.save_concept.title", "Save concept language package"),
                FileName = "Syscalculator.Language." + SanitizeFileName(manifest.LanguageCode) + ObjectPackageExtension,
                Filter = "Tiedragon object package (*.objpdk)|*.objpdk",
                OverwritePrompt = true,
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            targetPath = Path.GetFullPath(dialog.FileName);
        }

        if (!Path.GetExtension(targetPath).Equals(ObjectPackageExtension, StringComparison.OrdinalIgnoreCase))
            targetPath += ObjectPackageExtension;

        try
        {
            ValidatePackageSourcePaths();
            WriteObjectPackageArchive(targetPath);
            _conceptPackagePath = targetPath;
            _conceptFolder = null;
            _openedPackageSigned = false;
            _openedPackageSignatureAlgorithm = "";
            _openedPackageSignatureKeyId = "";
            foreach (var document in _documents)
            {
                document.FilePath = null;
                SetDirty(document, false);
            }

            SetStatus(TToolEditor("tool_editor.status.concept_saved", "Concept language package saved: ") + _conceptPackagePath, isError: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            SetStatus(TToolEditor("tool_editor.status.concept_save_failed", "Save concept failed: ") + ex.Message, isError: true);
            MessageBox.Show(this, ex.Message, TToolEditor("tool_editor.menu.file.save_concept", "Save concept language package"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadPackageSourceFolder(string sourceFolder)
    {
        var manifestPath = Path.Combine(sourceFolder, "manifest.json");
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("manifest.json ontbreekt.", manifestPath);

        ClearPackageDocuments();
        _conceptFolder = sourceFolder;
        _conceptPackagePath = null;
        _manifestText = File.ReadAllText(manifestPath, Encoding.UTF8);

        using (SuspendNavigationRefresh())
        {
            foreach (var file in Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories)
                         .OrderBy(path => Path.GetRelativePath(sourceFolder, path), StringComparer.OrdinalIgnoreCase))
            {
                var packagePath = Path.GetRelativePath(sourceFolder, file).Replace('\\', '/');
                if (IsManifestPath(packagePath))
                    continue;

                AddPackageFileFromDisk(packagePath, file);
            }
        }

        RefreshFileTree();
        RefreshDocumentList();
    }

    private void LoadLanguagePackageArchive(string packagePath)
    {
        ToolEditorDebugger.Log("LoadLanguagePackageArchive: start " + packagePath);
        ClearPackageDocuments();
        ToolEditorDebugger.Log("LoadLanguagePackageArchive: documents cleared.");
        _conceptFolder = null;
        _conceptPackagePath = null;
        var payload = ReadLanguagePackagePayload(packagePath, out var header);
        _openedPackageSigned = header?.Signed == true;
        _openedPackageSignatureAlgorithm = header?.SignatureAlgorithm ?? "";
        _openedPackageSignatureKeyId = header?.SignatureKeyId ?? "";
        ToolEditorDebugger.Log("LoadLanguagePackageArchive: payload bytes=" + payload.Length.ToString("N0"));
        LoadPackageZipPayload(payload, "LoadLanguagePackageArchive");
    }

    private void LoadObjectPackageArchive(string packagePath)
    {
        ToolEditorDebugger.Log("LoadObjectPackageArchive: start " + packagePath);
        ClearPackageDocuments();
        _conceptFolder = null;
        _conceptPackagePath = Path.GetFullPath(packagePath);
        _openedPackageSigned = false;
        _openedPackageSignatureAlgorithm = "";
        _openedPackageSignatureKeyId = "";
        LoadPackageZipPayload(File.ReadAllBytes(packagePath), "LoadObjectPackageArchive");
    }

    private void LoadPackageZipPayload(byte[] payload, string logScope)
    {
        ToolEditorDebugger.Log(logScope + ": payload bytes=" + payload.Length.ToString("N0"));
        using var memory = new MemoryStream(payload);
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);
        ToolEditorDebugger.Log(logScope + ": zip opened.");
        var entries = archive.Entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
            .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        ToolEditorDebugger.Log(logScope + ": entries=" + entries.Count.ToString("N0"));

        var manifestEntry = entries.FirstOrDefault(entry => NormalizePackagePath(entry.FullName).Equals("manifest.json", StringComparison.OrdinalIgnoreCase)) ??
            throw new InvalidDataException("manifest.json ontbreekt.");
        ToolEditorDebugger.Log(logScope + ": reading manifest.");
        using (var stream = manifestEntry.Open())
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
            _manifestText = reader.ReadToEnd();
        }
        ToolEditorDebugger.Log(logScope + ": manifest read.");

        using (SuspendNavigationRefresh())
        {
            var entryIndex = 0;
            foreach (var entry in entries)
            {
                entryIndex++;
                var entryName = NormalizePackagePath(entry.FullName);
                if (IsManifestPath(entryName))
                    continue;

                if (entryIndex == 1 || entryIndex % 25 == 0)
                    ToolEditorDebugger.Log(logScope + ": entry " + entryIndex.ToString("N0") + "/" + entries.Count.ToString("N0") + " " + entryName);
                ValidatePackageEntryPathOrThrow(entryName, IsImagePath(entryName));
                using var stream = entry.Open();
                if (IsImagePath(entryName))
                {
                    using var buffer = new MemoryStream();
                    stream.CopyTo(buffer);
                    var document = AddImageDocument(entryName, buffer.ToArray(), null);
                    document.LastModified = entry.LastWriteTime;
                }
                else
                {
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    var document = AddDocument(entryName, reader.ReadToEnd(), null);
                    document.LastModified = entry.LastWriteTime;
                }
            }
        }

        RefreshFileTree();
        RefreshDocumentList();
        ToolEditorDebugger.Log(logScope + ": completed; documents=" + _documents.Count.ToString("N0"));
    }

    private void AddPackageFileFromDisk(string packagePath, string file)
    {
        ValidatePackageEntryPathOrThrow(packagePath, IsImagePath(packagePath));
        if (IsImagePath(packagePath))
            AddImageDocument(packagePath, File.ReadAllBytes(file), file);
        else
            AddDocument(packagePath, File.ReadAllText(file, Encoding.UTF8), file);
    }

    private void ValidatePackageSourcePaths()
    {
        var errors = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var document in _documents)
        {
            ValidatePackageEntryPath(document.PackagePath, document.ImageBytes is not null, errors);
            if (!seen.Add(NormalizePackagePath(document.PackagePath)))
                errors.Add("Dubbel packagepad: " + document.PackagePath);
        }

        if (errors.Count > 0)
            throw new InvalidDataException(errors[0]);
    }

    private static void ValidatePackageEntryPathOrThrow(string packagePath, bool isImage)
    {
        var errors = new List<string>();
        ValidatePackageEntryPath(packagePath, isImage, errors);
        if (errors.Count > 0)
            throw new InvalidDataException(errors[0]);
    }

    private async Task<bool> ConfirmResetPackageAsync()
    {
        if (!_documents.Any(document => document.Dirty && !document.ReadOnly))
            return true;

        var result = MessageBox.Show(
            this,
            TToolEditor("tool_editor.confirm_reset.message", "The current language package has unsaved changes.\r\n\r\nSave concept before continuing?"),
            TToolEditor("tool_editor.confirm_reset.title", "Language package"),
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Cancel)
            return false;
        if (result == DialogResult.No)
            return true;

        await SaveConceptLanguagePackageAsync();
        return !_documents.Any(document => document.Dirty && !document.ReadOnly);
    }

    private async void ToolEditorForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_closeConfirmed)
            return;

        if (_closePromptActive)
        {
            e.Cancel = true;
            return;
        }

        if (!_documents.Any(document => document.Dirty && !document.ReadOnly))
            return;

        e.Cancel = true;
        _closePromptActive = true;
        try
        {
            if (!await ConfirmResetPackageAsync())
                return;

            _closeConfirmed = true;
            BeginInvoke(Close);
        }
        finally
        {
            _closePromptActive = false;
        }
    }

    private void ToolEditorForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        ToolEditorDebugger.Log("ToolEditorForm closed; disposing WebView2 controls.");
        _previewConceptAnimationTimer.Stop();
        _previewConceptAnimationTimer.Dispose();
        _previewRefreshTimer.Stop();
        _previewRefreshTimer.Dispose();
        DisposeWebView(_htmlEditor, "HTML editor WebView2");
        DisposeWebView(_preview, "Preview WebView2");
    }

    private static void DisposeWebView(WebView2 webView, string name)
    {
        try
        {
            if (!webView.IsDisposed)
                webView.Dispose();
        }
        catch (Exception ex)
        {
            ToolEditorDebugger.ReportException("Failed to dispose " + name, ex, showDialog: false);
        }
    }

    private void ClearPackageDocuments()
    {
        _documents.Clear();
        _tabStrip.Controls.Clear();
        _editorContent.Controls.Clear();
        _current = null;
        _pendingHtml = null;
        _pendingHtmlEditor = null;
        _pendingPreviewInitialize = false;
        _previewRefreshTimer.Stop();
        _manifestText = "";
        _conceptFolder = null;
        _conceptPackagePath = null;
        _openedPackageSigned = false;
        _openedPackageSignatureAlgorithm = "";
        _openedPackageSignatureKeyId = "";
        _conceptBannerDismissed = false;
        _contentSplit.Panel2Collapsed = true;
        _previewButton.Enabled = false;
        RefreshTabStrip();
        RefreshFileTree();
        RefreshDocumentList();
        UpdateUiState();
    }

    private static byte[] ReadLanguagePackagePayload(string packagePath)
    {
        return ReadLanguagePackagePayload(packagePath, out _);
    }

    private static byte[] ReadLanguagePackagePayload(string packagePath, out LanguagePackageContainerHeader? header)
    {
        var bytes = File.ReadAllBytes(packagePath);
        header = null;
        if (bytes.Length < LanguagePackageMagic.Length + sizeof(int) + sizeof(int) ||
            !bytes.Take(LanguagePackageMagic.Length).SequenceEqual(LanguagePackageMagic))
        {
            return bytes;
        }

        using var memory = new MemoryStream(bytes);
        memory.Position = LanguagePackageMagic.Length;
        using var reader = new BinaryReader(memory, Encoding.UTF8, leaveOpen: true);
        var format = reader.ReadInt32();
        if (format != LanguagePackageContainerFormat)
            throw new InvalidDataException("Onbekend taalpackage containerformaat: " + format);

        var headerLength = reader.ReadInt32();
        if (headerLength <= 0 || headerLength > MaxPackageHeaderBytes)
            throw new InvalidDataException("Packageheader is ongeldig.");

        var headerBytes = reader.ReadBytes(headerLength);
        if (headerBytes.Length != headerLength)
            throw new InvalidDataException("Packageheader is incompleet.");

        header = JsonSerializer.Deserialize<LanguagePackageContainerHeader>(
            Encoding.UTF8.GetString(headerBytes),
            LanguagePackageJsonOptions) ?? throw new InvalidDataException("Packageheader is ongeldig.");
        ValidateLanguagePackageHeader(header);

        var payload = reader.ReadBytes((int)(memory.Length - memory.Position));
        if (!string.IsNullOrWhiteSpace(header.PayloadSha256) &&
            !ComputeSha256Bytes(payload).Equals(header.PayloadSha256.Trim().ToLowerInvariant(), StringComparison.Ordinal))
        {
            throw new InvalidDataException("Payload SHA-256 klopt niet.");
        }

        LanguagePackageSignatureVerifier.VerifyOrThrow(
            header.Signed,
            header.SignatureAlgorithm,
            header.SignatureKeyId,
            header.Signature,
            payload,
            header.PayloadSha256,
            header.SoftwareId,
            header.PackageType,
            header.PayloadFormat);

        return payload;
    }

    private static void ValidateLanguagePackageHeader(LanguagePackageContainerHeader header)
    {
        if (header.Format != LanguagePackageContainerFormat)
            throw new InvalidDataException("Packageheader formaat wordt niet ondersteund.");
        if (!LanguagePackageSoftwareId.Equals(header.SoftwareId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Package is niet bedoeld voor Syscalculator.");
        if (!LanguagePackageType.Equals(header.PackageType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Package is geen taalpackage.");
        if (header.Encrypted)
            throw new InvalidDataException("Encrypted taalpackages worden nog niet geopend.");
        if (!header.Signed &&
            (!string.IsNullOrWhiteSpace(header.SignatureAlgorithm) ||
             !string.IsNullOrWhiteSpace(header.SignatureKeyId) ||
             !string.IsNullOrWhiteSpace(header.Signature)))
        {
            throw new InvalidDataException("Signature-velden zijn aanwezig maar Signed staat uit.");
        }
    }

    private void ShowManifestDialog()
    {
        var properties = ReadManifestProperties(_manifestText);
        using var dialog = new Form
        {
            Text = TToolEditor("tool_editor.manifest.properties.title", "Language package properties"),
            Width = 560,
            Height = 420,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowIcon = false
        };

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 0,
            BackColor = Color.White,
            Padding = new Padding(18, 16, 18, 12)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddManifestPropertyRow(content, TToolEditor("tool_editor.manifest.property.name", "Name"), properties.DisplayName);
        AddManifestPropertyRow(content, TToolEditor("tool_editor.manifest.property.language_code", "Language code"), properties.LanguageCode);
        AddManifestPropertyRow(content, TToolEditor("tool_editor.manifest.property.native_name", "Native name"), properties.NativeName);
        AddManifestPropertyRow(content, TToolEditor("tool_editor.manifest.property.package_version", "Package version"), properties.PackageVersion);
        AddManifestPropertyRow(content, TToolEditor("tool_editor.manifest.property.fallback_language", "Fallback language"), properties.FallbackLanguage);
        AddManifestPropertyRow(content, TToolEditor("tool_editor.manifest.property.producer", "Producer"), properties.Producer);
        AddManifestPropertyRow(content, TToolEditor("tool_editor.manifest.property.product", "Product"), properties.Product);
        AddManifestPropertyRow(content, TToolEditor("tool_editor.manifest.property.software_id", "Software ID"), properties.SoftwareId);
        AddManifestPropertyRow(content, TToolEditor("tool_editor.manifest.property.package_id", "Package ID"), properties.Id);

        var close = new Button
        {
            Text = TToolEditor("tool_editor.button.close", "Close"),
            Dock = DockStyle.Right,
            Width = 96
        };
        close.Click += (_, _) => dialog.Close();

        var buttons = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            Padding = new Padding(8)
        };
        buttons.Controls.Add(close);

        dialog.Controls.Add(content);
        dialog.Controls.Add(buttons);
        dialog.ShowDialog(this);
    }

    private static void AddManifestPropertyRow(TableLayoutPanel content, string label, string value)
    {
        var row = content.RowCount++;
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        content.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(71, 85, 105)
        }, 0, row);
        content.Controls.Add(new Label
        {
            Text = string.IsNullOrWhiteSpace(value) ? "-" : value,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42)
        }, 1, row);
    }

    private static ManifestProperties ReadManifestProperties(string manifestText)
    {
        try
        {
            using var json = JsonDocument.Parse(manifestText);
            var root = json.RootElement;
            return new ManifestProperties(
                GetManifestString(root, "id"),
                GetManifestString(root, "producer"),
                GetManifestString(root, "product"),
                GetManifestString(root, "softwareId"),
                GetManifestString(root, "languageCode"),
                GetManifestString(root, "displayName"),
                GetManifestString(root, "nativeName"),
                GetManifestString(root, "packageVersion"),
                GetManifestString(root, "fallbackLanguage"));
        }
        catch (JsonException)
        {
            return new ManifestProperties("", "", "", "", "", "Ongeldig manifest", "", "", "");
        }
    }

    private static string GetManifestString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
    }

    private void ShowMediaFileListDialog()
    {
        using var dialog = new Form
        {
            Text = TToolEditor("tool_editor.media.files.title", "Media files"),
            Width = 760,
            Height = 420,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowIcon = false
        };

        var list = CreateMediaFileList();
        list.Dock = DockStyle.Fill;
        list.AllowDrop = true;
        list.DragEnter += ToolEditorForm_DragEnter;
        list.DragDrop += MediaList_DragDrop;
        AttachMediaListContextMenu(list);
        list.DoubleClick += (_, _) =>
        {
            if (list.SelectedItems.Count == 0)
                return;

            if (list.SelectedItems[0].Tag is ToolEditorDocument document)
            {
                SelectDocument(document);
                dialog.Close();
            }
        };

        var open = new Button
        {
            Text = TToolEditor("tool_editor.button.open", "Open"),
            Dock = DockStyle.Right,
            Width = 96
        };
        open.Click += (_, _) =>
        {
            if (list.SelectedItems.Count == 0)
                return;

            if (list.SelectedItems[0].Tag is ToolEditorDocument document)
            {
                SelectDocument(document);
                dialog.Close();
            }
        };

        var close = new Button
        {
            Text = TToolEditor("tool_editor.button.close", "Close"),
            Dock = DockStyle.Right,
            Width = 96
        };
        close.Click += (_, _) => dialog.Close();

        var buttons = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            Padding = new Padding(8)
        };
        buttons.Controls.Add(open);
        buttons.Controls.Add(close);

        dialog.Controls.Add(list);
        dialog.Controls.Add(buttons);
        dialog.ShowDialog(this);
    }

    private ListView CreateMediaFileList()
    {
        var list = new ListView
        {
            BorderStyle = BorderStyle.FixedSingle,
            FullRowSelect = true,
            HideSelection = false,
            MultiSelect = false,
            View = View.Details,
            Font = new Font("Segoe UI", 9),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(31, 41, 55)
        };
        list.Columns.Add(TToolEditor("tool_editor.media.column.file", "File"), 220);
        list.Columns.Add("Type", 90);
        list.Columns.Add(TToolEditor("tool_editor.media.column.dimensions", "Dimensions"), 100);
        list.Columns.Add(TToolEditor("tool_editor.media.column.size", "Size"), 110);
        list.Columns.Add(TToolEditor("tool_editor.media.column.date", "Date"), 150);
        list.Columns.Add(TToolEditor("tool_editor.media.column.source", "Source"), 170);
        list.Columns.Add(TToolEditor("tool_editor.media.column.package_path", "Package path"), 300);

        foreach (var document in _documents.Where(document => document.ImageBytes is not null)
                     .OrderBy(document => document.PackagePath, StringComparer.OrdinalIgnoreCase))
        {
            var item = new ListViewItem(Path.GetFileName(document.PackagePath));
            var metadata = ReadMediaMetadata(document);
            item.SubItems.Add(Path.GetExtension(document.PackagePath).TrimStart('.').ToUpperInvariant());
            item.SubItems.Add(FormatMediaDimensions(metadata));
            item.SubItems.Add((document.ImageBytes?.Length ?? 0).ToString("N0") + " bytes");
            item.SubItems.Add(FormatDocumentDate(document, metadata));
            item.SubItems.Add(string.IsNullOrWhiteSpace(metadata.Source) ? "-" : metadata.Source);
            item.SubItems.Add(document.PackagePath);
            item.Tag = document;
            list.Items.Add(item);
        }

        return list;
    }

    private static string FormatDocumentDate(ToolEditorDocument document, GraphImageMetadata? metadata = null)
    {
        metadata ??= ReadMediaMetadata(document);
        if (metadata.DateTaken is not null)
            return metadata.DateTaken.Value.ToString("yyyy-MM-dd HH:mm");

        if (document.LastModified is not null)
            return document.LastModified.Value.LocalDateTime.ToString("yyyy-MM-dd HH:mm");

        if (!string.IsNullOrWhiteSpace(document.FilePath) && File.Exists(document.FilePath))
            return File.GetLastWriteTime(document.FilePath).ToString("yyyy-MM-dd HH:mm");

        return "-";
    }

    private void OpenMediaDocument()
    {
        using var dialog = new OpenFileDialog
        {
            Title = TToolEditor("tool_editor.media.add.title", "Add media to language package"),
            Multiselect = true,
            Filter = "Media (*.png;*.jpg;*.jpeg;*.svg;*.webp;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.svg;*.webp;*.gif;*.bmp|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        ImportMediaFiles(dialog.FileNames);
    }

    private void ReplaceCurrentMedia()
    {
        if (_current?.ImageBytes is null)
        {
            SetStatus(TToolEditor("tool_editor.status.select_media_first", "Select a media file first."), isError: true);
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Title = TToolEditor("tool_editor.media.replace.title", "Replace media"),
            Filter = "Media (*.png;*.jpg;*.jpeg;*.svg;*.webp;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.svg;*.webp;*.gif;*.bmp|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var packagePath = "assets/" + Path.GetFileName(dialog.FileName);
        _current.ImageBytes = File.ReadAllBytes(dialog.FileName);
        _current.FilePath = dialog.FileName;
        _current.DisplayName = packagePath;
        _current.PackagePath = NormalizePackagePath(packagePath);
        _current.Editor.Text = BuildImageInfoText(_current);
        ApplyDocumentLabels(_current);
        SetDirty(_current, true);
        RefreshFileTree();
        RefreshDocumentList();
        UpdatePreview();
        SetStatus(TToolEditor("tool_editor.status.media_replaced", "Media replaced: ") + _current.PackagePath, isError: false);
    }

    private void DeleteCurrentDocument()
    {
        if (_current is null)
            return;

        DeletePackageDocument(_current);
    }

    private void DeletePackageDocument(ToolEditorDocument document)
    {
        if (document.ReadOnly)
        {
            SetStatus(TToolEditor("tool_editor.status.readonly", "Read-only: ") + document.DisplayName, isError: true);
            return;
        }

        if (IsProtectedPackageDocument(document.PackagePath))
        {
            SetStatus(TToolEditor("tool_editor.status.package_part_cannot_delete", "Package part cannot be deleted: ") + BuildTabTitle(document), isError: true);
            MessageBox.Show(
                this,
                TToolEditor("tool_editor.delete.protected.message", "This item belongs to the fixed language package structure and cannot be deleted.\r\n\r\nUse the tab close button to close only the tab."),
                "ToolEditor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var result = MessageBox.Show(
            this,
            TToolEditor("tool_editor.delete.confirm.message", "Delete this file from the language package?\r\n\r\nThe source file on disk will not be deleted."),
            "ToolEditor",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
            return;

        var removedPath = document.PackagePath;
        RemoveDocumentFromPackage(document);
        SetStatus(TToolEditor("tool_editor.status.removed_from_package", "Removed from package: ") + removedPath, isError: false);
    }

    private void RenameMediaDocument(ToolEditorDocument document)
    {
        if (document.ImageBytes is null)
            return;

        var currentName = Path.GetFileName(document.PackagePath);
        using var dialog = new Form
        {
            Text = TToolEditor("tool_editor.media.rename.title", "Rename media"),
            Width = 430,
            Height = 150,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            ShowIcon = false
        };

        var input = new TextBox
        {
            Dock = DockStyle.Top,
            Text = currentName,
            Margin = new Padding(0, 4, 0, 0)
        };
        var label = new Label
        {
            Dock = DockStyle.Top,
            Text = TToolEditor("tool_editor.media.rename.new_name", "New file name:"),
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var ok = new Button
        {
            Text = TToolEditor("tool_editor.button.ok", "OK"),
            DialogResult = DialogResult.OK,
            Dock = DockStyle.Right,
            Width = 90
        };
        var cancel = new Button
        {
            Text = TToolEditor("tool_editor.button.cancel", "Cancel"),
            DialogResult = DialogResult.Cancel,
            Dock = DockStyle.Right,
            Width = 100
        };
        var buttons = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            Padding = new Padding(8)
        };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };
        content.Controls.Add(input);
        content.Controls.Add(label);
        dialog.Controls.Add(content);
        dialog.Controls.Add(buttons);
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var newName = Path.GetFileName(input.Text.Trim());
        if (string.IsNullOrWhiteSpace(newName) || !IsImagePath(newName))
        {
            MessageBox.Show(this, TToolEditor("tool_editor.media.rename.invalid_name", "Use a valid image name: png, jpg, jpeg, svg, webp, gif or bmp."), TToolEditor("tool_editor.media.rename.title", "Rename media"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var newPackagePath = NormalizePackagePath("assets/" + newName);
        if (_documents.Any(item => !ReferenceEquals(item, document) && item.PackagePath.Equals(newPackagePath, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, TToolEditor("tool_editor.media.rename.exists", "A media file with this name already exists."), TToolEditor("tool_editor.media.rename.title", "Rename media"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        document.DisplayName = newPackagePath;
        document.PackagePath = newPackagePath;
        document.FilePath = null;
        document.Editor.Text = BuildImageInfoText(document);
        ApplyDocumentLabels(document);
        SetDirty(document, true);
        RefreshFileTree();
        RefreshDocumentList();
        RefreshTabStrip();
        SelectDocument(document);
        SetStatus(TToolEditor("tool_editor.status.media_renamed", "Media renamed: ") + newPackagePath, isError: false);
    }

    private void ImportMediaFiles(IEnumerable<string> fileNames, bool selectImported = true)
    {
        var imported = 0;
        foreach (var fileName in fileNames.Where(IsImagePath))
        {
            AddOrReplaceImageDocument("assets/" + Path.GetFileName(fileName), File.ReadAllBytes(fileName), fileName, markDirty: true, selectDocument: selectImported);
            imported++;
        }

        if (imported == 0)
            SetStatus(TToolEditor("tool_editor.status.no_supported_media", "No supported media found."), isError: true);
        else
            SetStatus(imported.ToString("N0") + " " + TToolEditor("tool_editor.status.media_added_suffix", "media file(s) added."), isError: false);
    }

    private static bool TryBuildImportPackagePath(string fileName, out string packagePath)
    {
        var name = Path.GetFileName(fileName);
        var extension = Path.GetExtension(name);
        if (name.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
        {
            packagePath = "manifest.json";
            return true;
        }

        if (extension.Equals(".lng", StringComparison.OrdinalIgnoreCase))
        {
            packagePath = "language/" + name;
            return true;
        }

        if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".css", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".js", StringComparison.OrdinalIgnoreCase))
        {
            if (extension.Equals(".js", StringComparison.OrdinalIgnoreCase) &&
                !LanguagePackagePolicy.Current.IsAllowedScriptFile(name))
            {
                packagePath = "";
                return false;
            }

            packagePath = "manual/" + name;
            return true;
        }

        packagePath = "";
        return false;
    }

    private void RejectUnsupportedPackageFile(string fileName)
    {
        var message = TToolEditor("tool_editor.reject_file.message_prefix", "This file does not belong in the language package structure:\r\n\r\n") +
            Path.GetFileName(fileName) +
            TToolEditor("tool_editor.reject_file.message_suffix", "\r\n\r\nAllowed: manifest.json, language/*.lng, manual/help/NOD/formula HTML/CSS/JS, and media png/jpg/jpeg/svg/webp/gif/bmp.");
        SetStatus(TToolEditor("tool_editor.status.file_rejected", "File rejected: ") + Path.GetFileName(fileName), isError: true);
        MessageBox.Show(this, message, "ToolEditor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private async void SaveCurrent()
    {
        if (_current is null)
            return;

        await SaveDocumentAsync(_current);
    }

    private async Task<bool> SaveDocumentAsync(ToolEditorDocument document)
    {
        if (document.ReadOnly)
        {
            SetStatus(TToolEditor("tool_editor.status.readonly", "Read-only: ") + document.DisplayName, isError: true);
            return false;
        }

        if (ReferenceEquals(_current, document))
            await SyncHtmlEditorToSourceAsync();

        var path = document.FilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Save ToolEditor document",
                FileName = document.DisplayName.Replace('/', Path.DirectorySeparatorChar),
                Filter = "Language package files (*.html;*.json;*.lng;*.css;*.js;*.png;*.jpg;*.jpeg;*.svg;*.webp;*.gif;*.bmp)|*.html;*.json;*.lng;*.css;*.js;*.png;*.jpg;*.jpeg;*.svg;*.webp;*.gif;*.bmp|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return false;

            path = dialog.FileName;
            document.FilePath = path;
            document.DisplayName = Path.GetFileName(path);
            document.PackagePath = NormalizePackagePath(document.DisplayName);
            ApplyDocumentLabels(document);
            RefreshFileTree();
            RefreshDocumentList();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (document.ImageBytes is not null)
            File.WriteAllBytes(path, document.ImageBytes);
        else
            File.WriteAllText(path, GetDocumentTextForStorage(document), Encoding.UTF8);
        SetDirty(document, false);
        SetStatus(TToolEditor("tool_editor.status.saved", "Saved: ") + path, isError: false);
        return true;
    }

    private ToolEditorDocument AddDocument(string displayName, string text, string? filePath)
    {
        return AddDocument(displayName, text, filePath, createLineNumbers: true);
    }

    private ToolEditorDocument AddLazyDocument(string displayName, Func<string> textFactory, string? filePath)
    {
        var document = AddDocument(displayName, "", filePath, createLineNumbers: false);
        document.LazyTextFactory = textFactory;
        document.LazyTextLoaded = false;
        return document;
    }

    private ToolEditorDocument AddDocument(string displayName, string text, string? filePath, bool createLineNumbers)
    {
        var page = new TabPage(displayName);
        var document = new ToolEditorDocument(page, displayName, NormalizePackagePath(displayName), filePath);
        ApplyFileMetadata(document);
        text = PrepareDocumentText(document, text);

        var editor = CreateTextEditor(text, document, createLineNumbers);
        if (IsManifestPath(document.PackagePath))
        {
            document.ReadOnly = true;
            editor.ReadOnly = true;
            editor.BackColor = Color.FromArgb(248, 250, 252);
        }

        document.Editor = editor;
        ApplyDocumentLabels(document);
        var header = ToolEditorTabsApi.CreateHeader(
            page,
            (_, _) => SelectDocument(document),
            (_, _) => CloseDocument(document));

        document.HeaderPanel = header.Panel;
        document.HeaderTitle = header.Title;
        AttachTabContextMenu(document, header.Panel, header.Title, header.CloseButton);
        _documents.Add(document);
        RefreshNavigationIfNeeded();
        return document;
    }

    private static string PrepareDocumentText(ToolEditorDocument document, string text)
    {
        if (!Path.GetExtension(document.PackagePath).Equals(".html", StringComparison.OrdinalIgnoreCase))
            return text;

        text = StripToolEditorConceptBanners(text);
        text = EnsureHtmlDocumentMarkup(text);
        return EnsureTrailingEndLine(text);
    }

    private void EnsureDocumentTextLoaded(ToolEditorDocument document, bool ensureEditorUi = false)
    {
        if (ensureEditorUi && document.ImageBytes is null)
            EnsureLineNumbersAttached(document);

        if (document.LazyTextLoaded || document.LazyTextFactory is null)
            return;

        var text = PrepareDocumentText(document, document.LazyTextFactory());
        document.LazyTextLoaded = true;
        _loadingDocumentText = true;
        try
        {
            document.Editor.Text = text;
            document.SyntaxHighlightVersion = null;
            SetDirty(document, false);
        }
        finally
        {
            _loadingDocumentText = false;
        }
    }

    private void EnsureAllLazyDocumentsLoaded()
    {
        foreach (var document in _documents)
            EnsureDocumentTextLoaded(document);
    }

    private static string StripToolEditorConceptBanners(string html)
    {
        return LegacyConceptWarningRegex.Replace(ConceptBannerRegex.Replace(html, ""), "");
    }

    private static string EnsureHtmlDocumentMarkup(string html)
    {
        if (string.IsNullOrWhiteSpace(html) || html.Contains('<'))
            return html;

        return "<h1>" + WebUtility.HtmlEncode(html.Trim()) + "</h1>" + Environment.NewLine;
    }

    private static string EnsureTrailingEndLine(string text)
    {
        return string.IsNullOrEmpty(text) || text.EndsWith('\n')
            ? text
            : text + Environment.NewLine;
    }

    private ToolEditorDocument AddImageDocument(string displayName, byte[] bytes, string? filePath)
    {
        var page = new TabPage(displayName);
        var document = new ToolEditorDocument(page, displayName, NormalizePackagePath(displayName), filePath)
        {
            ImageBytes = bytes
        };
        ApplyFileMetadata(document);
        var editor = CreateTextEditor(BuildImageInfoText(document), document);
        editor.ReadOnly = true;
        editor.BackColor = Color.FromArgb(248, 250, 252);
        document.Editor = editor;
        ApplyDocumentLabels(document);
        var header = ToolEditorTabsApi.CreateHeader(
            page,
            (_, _) => SelectDocument(document),
            (_, _) => CloseDocument(document));

        document.HeaderPanel = header.Panel;
        document.HeaderTitle = header.Title;
        AttachTabContextMenu(document, header.Panel, header.Title, header.CloseButton);
        _documents.Add(document);
        RefreshNavigationIfNeeded();
        return document;
    }

    private IDisposable SuspendNavigationRefresh()
    {
        _suspendNavigationRefresh++;
        return new NavigationRefreshScope(this);
    }

    private void RefreshNavigationIfNeeded()
    {
        if (_suspendNavigationRefresh > 0)
            return;

        RefreshFileTree();
        RefreshDocumentList();
    }

    private static void ApplyFileMetadata(ToolEditorDocument document)
    {
        if (!string.IsNullOrWhiteSpace(document.FilePath) && File.Exists(document.FilePath))
            document.LastModified = File.GetLastWriteTime(document.FilePath);
    }

    private ToolEditorDocument AddOrReplaceImageDocument(string displayName, byte[] bytes, string? filePath, bool markDirty, bool selectDocument = true)
    {
        var packagePath = NormalizePackagePath(displayName);
        var document = _documents.FirstOrDefault(document => document.ImageBytes is not null &&
            document.PackagePath.Equals(packagePath, StringComparison.OrdinalIgnoreCase));
        if (document is null)
        {
            document = AddImageDocument(displayName, bytes, filePath);
            if (markDirty)
                SetDirty(document, true);
            if (selectDocument)
                SelectDocument(document);
            return document;
        }

        document.ImageBytes = bytes;
        document.FilePath = filePath;
        document.Editor.Text = BuildImageInfoText(document);
        ApplyDocumentLabels(document);
        SetDirty(document, markDirty);
        RefreshFileTree();
        RefreshDocumentList();
        if (selectDocument)
            SelectDocument(document);
        return document;
    }

    private string BuildImageInfoText(ToolEditorDocument document)
    {
        var bytes = document.ImageBytes?.Length ?? 0;
        var metadata = ReadMediaMetadata(document);
        return TToolEditor("tool_editor.media.preview.title", "Media preview") + "\r\n\r\n" +
            TToolEditor("tool_editor.media.preview.file", "File: ") + Path.GetFileName(document.PackagePath) + "\r\n" +
            TToolEditor("tool_editor.media.preview.package_path", "Package path: ") + document.PackagePath + "\r\n" +
            TToolEditor("tool_editor.media.preview.dimensions", "Dimensions: ") + FormatMediaDimensions(metadata) + "\r\n" +
            TToolEditor("tool_editor.media.preview.size", "Size: ") + bytes.ToString("N0") + " bytes\r\n" +
            TToolEditor("tool_editor.media.preview.date", "Date: ") + FormatDocumentDate(document, metadata) + "\r\n" +
            TToolEditor("tool_editor.media.preview.source", "Source: ") + (string.IsNullOrWhiteSpace(metadata.Source) ? "-" : metadata.Source) + "\r\n";
    }

    private static GraphImageMetadata ReadMediaMetadata(ToolEditorDocument document)
    {
        return GraphImageMetadataReader.Read(document.PackagePath, document.ImageBytes);
    }

    private static string FormatMediaDimensions(GraphImageMetadata metadata)
    {
        return metadata.Width is not null && metadata.Height is not null
            ? metadata.Width.Value.ToString("N0") + " x " + metadata.Height.Value.ToString("N0")
            : "-";
    }

    private async void SetHtmlEditMode(bool editMode)
    {
        if (_current is null || !GetHtmlViewState(_current).CanUseVisualEditor)
        {
            SetStatus(TToolEditor("tool_editor.status.select_html_first", "Select an HTML document first."), isError: true);
            return;
        }

        _preferredHtmlEditMode = editMode;
        if (editMode && IsGeneratedPlaceholderHtmlDocument(_current))
        {
            _current.Editor.Text = BuildRenderedSourceText(_current);
            SetDirty(_current, true);
            SetStatus(TToolEditor("tool_editor.status.generated_help_converted", "Generated help was converted to editable HTML."), isError: false);
        }

        if (!editMode && _current.HtmlEditMode)
            await SyncHtmlEditorToSourceAsync();

        _current.HtmlEditMode = editMode;
        SelectDocument(_current);
        QueueFocusActiveEditor();
    }

    private void WrapHtmlSelection(string tag, string fallbackText, string attributes = "")
    {
        if (!CanEditCurrentHtml())
            return;

        if (_current!.HtmlEditMode)
        {
            if (string.IsNullOrEmpty(attributes))
                ExecuteHtmlEditorCommand("formatBlock", "<" + tag + ">");
            else
                ExecuteHtmlEditorInsertHtml("<" + tag + attributes + ">" + WebUtility.HtmlEncode(fallbackText) + "</" + tag + ">");
        }
        else
        {
            var editor = _current.Editor;
            var selected = editor.SelectedText;
            if (string.IsNullOrEmpty(selected))
                selected = fallbackText;

            editor.SelectedText = "<" + tag + attributes + ">" + selected + "</" + tag + ">";
            editor.Focus();
        }

        UpdatePreview();
    }

    private void ApplyHtmlInlineCommand(string command, string tag, string fallbackText)
    {
        if (!CanEditCurrentHtml())
            return;

        if (_current!.HtmlEditMode)
            ExecuteHtmlEditorCommand(command, "");
        else
        {
            var editor = _current.Editor;
            var selected = string.IsNullOrEmpty(editor.SelectedText) ? fallbackText : editor.SelectedText;
            editor.SelectedText = "<" + tag + ">" + selected + "</" + tag + ">";
            editor.Focus();
        }

        UpdatePreview();
    }

    private void ApplyHtmlColor(string color)
    {
        if (!CanEditCurrentHtml())
            return;

        if (_current!.HtmlEditMode)
            ExecuteHtmlEditorCommand("foreColor", color);
        else
            WrapHtmlSelection("span", "tekst", " style=\"color:" + color + "\"");
    }

    private void InsertHtmlLink()
    {
        if (!CanEditCurrentHtml())
            return;

        if (_current!.HtmlEditMode)
            ExecuteHtmlEditorCommand("createLink", "#");
        else
        {
            var editor = _current.Editor;
            var selected = string.IsNullOrWhiteSpace(editor.SelectedText) ? "linktekst" : editor.SelectedText;
            editor.SelectedText = "<a href=\"#\">" + selected + "</a>";
            editor.Focus();
        }

        UpdatePreview();
    }

    private void InsertHtmlImage()
    {
        if (!CanEditCurrentHtml())
            return;

        var media = _documents.FirstOrDefault(document => document.ImageBytes is not null);
        var path = media?.PackagePath ?? "assets/afbeelding.png";
        if (_current!.HtmlEditMode)
            ExecuteHtmlEditorInsertHtml("<img src=\"" + path + "\" alt=\"\">");
        else
            InsertHtmlSnippet("<img src=\"" + path + "\" alt=\"\">");
    }

    private void InsertHtmlSnippet(string snippet)
    {
        if (!CanEditCurrentHtml())
            return;

        if (_current!.HtmlEditMode)
            ExecuteHtmlEditorInsertHtml(snippet);
        else
        {
            _current.Editor.SelectedText = snippet;
            _current.Editor.Focus();
        }

        UpdatePreview();
    }

    private void ExecuteHtmlEditorCommand(string command, string value)
    {
        if (_htmlEditor.CoreWebView2 is null)
            return;

        var script = "document.execCommand(" +
            JsonSerializer.Serialize(command) + ", false, " +
            JsonSerializer.Serialize(value) + "); window.chrome.webview.postMessage('changed');";
        _ = _htmlEditor.CoreWebView2.ExecuteScriptAsync(script);
    }

    private void ExecuteHtmlEditorInsertHtml(string html)
    {
        if (_htmlEditor.CoreWebView2 is null)
            return;

        var script = "document.execCommand('insertHTML', false, " +
            JsonSerializer.Serialize(html) + "); window.chrome.webview.postMessage('changed');";
        _ = _htmlEditor.CoreWebView2.ExecuteScriptAsync(script);
    }

    private bool CanEditCurrentHtml()
    {
        if (_current is not null && _current.ImageBytes is null && IsHtmlDocument(_current))
            return true;

        SetStatus(TToolEditor("tool_editor.status.select_html_first", "Select an HTML document first."), isError: true);
        return false;
    }

    private RichTextBox CreateTextEditor(string text, ToolEditorDocument document, bool createLineNumbers = true)
    {
        var isLanguageDocument = document.PackagePath.StartsWith("language/", StringComparison.OrdinalIgnoreCase);
        var editor = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 10),
            WordWrap = isLanguageDocument,
            ScrollBars = isLanguageDocument ? RichTextBoxScrollBars.ForcedVertical : RichTextBoxScrollBars.Both,
            AcceptsTab = true,
            HideSelection = false,
            Text = text
        };
        document.Editor = editor;
        if (createLineNumbers)
            EnsureLineNumbersAttached(document);
        editor.TextChanged += (_, _) =>
        {
            if (_current?.Editor == editor && !document.ReadOnly && !document.Highlighting && !_loadingDocumentText)
                SetDirty(_current, true);

            document.LineNumbers?.Invalidate();
            document.SyntaxHighlightVersion = null;
            if (ReferenceEquals(_current, document))
            {
                UpdateStatusMetrics(document);
                ScheduleSyntaxHighlight(document);
            }
            document.LineNumbers?.RefreshMetrics();
        };
        editor.VScroll += (_, _) =>
        {
            document.LineNumbers?.Invalidate();
            if (ReferenceEquals(_current, document))
                ScheduleSyntaxHighlight(document);
        };
        editor.Resize += (_, _) =>
        {
            document.LineNumbers?.Invalidate();
            if (ReferenceEquals(_current, document))
                ScheduleSyntaxHighlight(document);
        };
        return editor;
    }

    private static void EnsureLineNumbersAttached(ToolEditorDocument document)
    {
        if (document.LineNumbers is not null)
            return;

        var lineNumbers = new LineNumberPanel();
        document.LineNumbers = lineNumbers;
        lineNumbers.Attach(document.Editor);
    }

    private async void SelectDocument(ToolEditorDocument document)
    {
        if (_current is not null &&
            !ReferenceEquals(_current, document) &&
            _current.HtmlEditMode &&
            _htmlEditorContentDirty)
        {
            await SyncHtmlEditorToSourceAsync();
        }

        EnsureDocumentTextLoaded(document, ensureEditorUi: true);
        ApplyPreferredHtmlEditMode(document);
        EnsureDocumentTabOpen(document);
        _current = document;
        SelectDocumentInTree(document);
        SelectDocumentInList(document);
        UpdateHtmlToolbarState(document);
        _editorContent.SuspendLayout();
        _editorContent.Controls.Clear();
        UpdateEditConceptBannerState(document);
        var viewState = GetHtmlViewState(document);
        if (viewState.EditIsPreview)
        {
            _htmlEditHost.SuspendLayout();
            _htmlEditHost.Controls.Clear();
            _htmlEditHost.Controls.Add(_editConceptBanner, 0, 0);
            _htmlEditHost.Controls.Add(_htmlEditor, 0, 1);
            _editConceptBanner.BringToFront();
            _htmlEditHost.ResumeLayout();
            _editorContent.Controls.Add(_htmlEditHost);
            SetHtmlEditor(document.Editor.Text);
        }
        else if (IsGeneratedPlaceholderHtmlDocument(document))
        {
            var renderedEditor = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 10),
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                HideSelection = false,
                ReadOnly = true,
                BackColor = Color.White,
                Text = BuildRenderedSourceText(document)
            };
            var lineNumbers = new LineNumberPanel();
            lineNumbers.Attach(renderedEditor);
            renderedEditor.VScroll += (_, _) => lineNumbers.Invalidate();
            renderedEditor.Resize += (_, _) => lineNumbers.Invalidate();
            ApplySyntaxHighlight(renderedEditor, document.PackagePath);
            var host = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            host.Controls.Add(renderedEditor);
            host.Controls.Add(lineNumbers);
            _editorContent.Controls.Add(host);
        }
        else if (document.LineNumbers is not null && document.ImageBytes is null)
        {
            var host = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            host.Controls.Add(document.Editor);
            host.Controls.Add(document.LineNumbers);
            _editorContent.Controls.Add(host);
        }
        else
        {
            _editorContent.Controls.Add(document.Editor);
        }

        _editorContent.ResumeLayout();
        RefreshTabStrip();
        UpdatePreviewPaneState(document);
        UpdatePreview(initializePreview: _webViewInitializationStarted);
        UpdateUiState();
        UpdateDocumentStatus(document);
        ScheduleSyntaxHighlight(document);
        QueueFocusActiveEditor();
    }

    private void ApplyPreferredHtmlEditMode(ToolEditorDocument document)
    {
        if (!GetHtmlViewState(document).CanUseVisualEditor)
        {
            document.HtmlEditMode = false;
            return;
        }

        if (_preferredHtmlEditMode && IsGeneratedPlaceholderHtmlDocument(document))
        {
            document.Editor.Text = BuildRenderedSourceText(document);
            SetDirty(document, true);
        }

        document.HtmlEditMode = _preferredHtmlEditMode;
    }

    private string BuildRenderedSourceText(ToolEditorDocument document)
    {
        var expanded = ApplyToolEditorHelpPlaceholders(document.Editor.Text);
        return StripToolEditorConceptBanners(expanded);
    }

    private static bool IsGeneratedPlaceholderHtmlDocument(ToolEditorDocument document)
    {
        return document.ImageBytes is null &&
            IsHtmlDocument(document) &&
            LanguagePlaceholderRegex.IsMatch(document.Editor.Text);
    }

    private void SelectMediaManager()
    {
        _current = null;
        UpdateHtmlToolbarState(null);
        _editorContent.SuspendLayout();
        _editorContent.Controls.Clear();
        _editorContent.Controls.Add(BuildMediaManagerView());
        _editorContent.ResumeLayout();
        RefreshTabStrip();
        SetStatus(TToolEditor("tool_editor.tree.media", "Media and images") + ": " + _documents.Count(document => document.ImageBytes is not null).ToString("N0") + " " + TToolEditor("tool_editor.status.files", "files"), isError: false);
        UpdateStatusMetrics(null);
    }

    private Control BuildMediaManagerView()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White,
            Padding = new Padding(12)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = TToolEditor("tool_editor.tree.media", "Media and images"),
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 65, 170),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var list = CreateMediaFileList();
        list.Dock = DockStyle.Fill;
        list.AllowDrop = true;
        list.DragEnter += ToolEditorForm_DragEnter;
        list.DragDrop += MediaList_DragDrop;
        AttachMediaListContextMenu(list);
        list.DoubleClick += (_, _) =>
        {
            if (list.SelectedItems.Count > 0 && list.SelectedItems[0].Tag is ToolEditorDocument document)
                SelectDocument(document);
        };

        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(list, 0, 1);
        return layout;
    }

    private void AttachMediaListContextMenu(ListView list)
    {
        var menu = new ContextMenuStrip();
        var open = menu.Items.Add(TToolEditor("tool_editor.button.open", "Open"));
        var copy = menu.Items.Add(TToolEditor("tool_editor.menu.edit.copy", "Copy"));
        var cut = menu.Items.Add(TToolEditor("tool_editor.menu.edit.cut", "Cut"));
        var paste = menu.Items.Add(TToolEditor("tool_editor.menu.edit.paste", "Paste"));
        menu.Items.Add(new ToolStripSeparator());
        var rename = menu.Items.Add(TToolEditor("tool_editor.media.rename.title", "Rename media") + "...");
        var delete = menu.Items.Add(TToolEditor("tool_editor.menu.package.delete_selected", "Delete selected file"));

        ToolEditorDocument? SelectedMediaDocument()
        {
            return list.SelectedItems.Count > 0
                ? list.SelectedItems[0].Tag as ToolEditorDocument
                : null;
        }

        menu.Opening += (_, e) =>
        {
            var document = SelectedMediaDocument();
            var enabled = document?.ImageBytes is not null;
            open.Enabled = enabled;
            copy.Enabled = enabled;
            cut.Enabled = enabled;
            paste.Enabled = ClipboardContainsSupportedMediaFiles();
            rename.Enabled = enabled;
            delete.Enabled = enabled;
            if (!enabled && !ClipboardContainsSupportedMediaFiles())
                e.Cancel = true;
        };

        open.Click += (_, _) =>
        {
            if (SelectedMediaDocument() is { } document)
                SelectDocument(document);
        };
        copy.Click += (_, _) =>
        {
            if (SelectedMediaDocument() is { } document)
                CopyMediaDocumentToWindowsClipboard(document);
        };
        cut.Click += (_, _) =>
        {
            if (SelectedMediaDocument() is { } document)
                CutMediaDocumentToWindowsClipboard(document);
        };
        paste.Click += (_, _) => PasteMediaFilesFromWindowsClipboard();
        rename.Click += (_, _) =>
        {
            if (SelectedMediaDocument() is { } document)
                RenameMediaDocument(document);
        };
        delete.Click += (_, _) =>
        {
            if (SelectedMediaDocument() is { } document)
                DeletePackageDocument(document);
        };

        list.ContextMenuStrip = menu;
    }

    private void CopyMediaDocumentToWindowsClipboard(ToolEditorDocument document)
    {
        if (document.ImageBytes is null)
            return;

        var file = ExportMediaDocumentToClipboardFile(document);
        var files = new System.Collections.Specialized.StringCollection { file };
        Clipboard.SetFileDropList(files);
        SetStatus(TToolEditor("tool_editor.status.media_copied_clipboard", "Media copied to Windows clipboard: ") + Path.GetFileName(file), isError: false);
    }

    private void CutMediaDocumentToWindowsClipboard(ToolEditorDocument document)
    {
        CopyMediaDocumentToWindowsClipboard(document);
        DeletePackageDocument(document);
    }

    private void PasteMediaFilesFromWindowsClipboard()
    {
        if (!ClipboardContainsSupportedMediaFiles())
        {
            SetStatus(TToolEditor("tool_editor.status.no_clipboard_media", "No supported media on the Windows clipboard."), isError: true);
            return;
        }

        ImportMediaFiles(Clipboard.GetFileDropList().Cast<string>(), selectImported: false);
        SelectMediaManager();
    }

    private static bool ClipboardContainsSupportedMediaFiles()
    {
        return Clipboard.ContainsFileDropList() &&
            Clipboard.GetFileDropList().Cast<string>().Any(IsImagePath);
    }

    private static string ExportMediaDocumentToClipboardFile(ToolEditorDocument document)
    {
        if (document.ImageBytes is null)
            throw new InvalidOperationException("Document is not media.");

        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Syscalculator",
            "ToolEditor",
            "Clipboard");
        Directory.CreateDirectory(folder);
        var target = Path.Combine(folder, Path.GetFileName(document.PackagePath));
        if (File.Exists(target))
            target = Path.Combine(folder, Path.GetFileNameWithoutExtension(document.PackagePath) + "-" + Guid.NewGuid().ToString("N")[..8] + Path.GetExtension(document.PackagePath));

        File.WriteAllBytes(target, document.ImageBytes);
        return target;
    }

    private string GetAvailableMediaPackagePath(string preferredPackagePath, ToolEditorDocument? ignoreDocument = null)
    {
        var normalized = NormalizePackagePath(preferredPackagePath);
        var directory = GetPackageDirectory(normalized);
        if (string.IsNullOrWhiteSpace(directory))
            directory = "assets";

        var baseName = Path.GetFileNameWithoutExtension(normalized);
        var extension = Path.GetExtension(normalized);
        var candidate = NormalizePackagePath(directory + "/" + baseName + extension);
        var index = 2;
        while (_documents.Any(document => !ReferenceEquals(document, ignoreDocument) &&
            document.PackagePath.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = NormalizePackagePath(directory + "/" + baseName + " copy " + index.ToString() + extension);
            index++;
        }

        return candidate;
    }

    private void QueueFocusActiveEditor()
    {
        if (IsDisposed)
            return;

        if (!IsHandleCreated)
            return;

        BeginInvoke(FocusActiveEditor);
    }

    private void FocusActiveEditor()
    {
        if (_current is null || IsDisposed)
            return;

        if (_current.HtmlEditMode && _current.ImageBytes is null && IsHtmlDocument(_current))
        {
            if (!_htmlEditor.IsDisposed)
            {
                _htmlEditor.Focus();
                _ = _htmlEditor.CoreWebView2?.ExecuteScriptAsync("window.toolEditorFocusBody && window.toolEditorFocusBody();");
            }

            return;
        }

        if (!_current.Editor.IsDisposed)
            _current.Editor.Focus();
    }

    private void UpdateDocumentStatus(ToolEditorDocument document)
    {
        if (document.ImageBytes is not null)
        {
            SetStatus(TToolEditor("tool_editor.status.media", "Media") + ": " + document.PackagePath + " (" + document.ImageBytes.Length.ToString("N0") + " bytes)", isError: false);
            UpdateStatusMetrics(document);
            return;
        }

        SetStatus(TToolEditor("tool_editor.status.selected", "Selected: ") + BuildTabTitle(document), isError: false);
        UpdateStatusMetrics(document);
    }

    private void UpdateHtmlToolbarState(ToolEditorDocument? document)
    {
        var visible = document is not null && GetHtmlViewState(document).CanUseHtmlToolbar;
        _htmlToolbar.Visible = visible;
        _htmlToolbarRow.Height = visible ? 32 : 0;
        UpdateHtmlModeButtons(document);
        UpdatePreviewPaneState(document);
    }

    private void UpdateHtmlModeButtons(ToolEditorDocument? document)
    {
        var editMode = document?.HtmlEditMode == true;
        _sourceModeButton.Checked = !editMode;
        _editModeButton.Checked = editMode;
        StyleHtmlModeButton(_sourceModeButton, !editMode);
        StyleHtmlModeButton(_editModeButton, editMode);
    }

    private static void StyleHtmlModeButton(ToolStripButton button, bool active)
    {
        button.Font = active
            ? new Font(button.Font, FontStyle.Bold)
            : new Font(button.Font, FontStyle.Regular);
        button.BackColor = active ? Color.FromArgb(222, 235, 255) : Color.FromArgb(248, 250, 252);
        button.ForeColor = active ? Color.FromArgb(0, 63, 143) : Color.FromArgb(15, 23, 42);
        button.DisplayStyle = ToolStripItemDisplayStyle.Text;
    }

    private void UpdatePreviewPaneState(ToolEditorDocument? document)
    {
        var viewState = document is null
            ? default
            : GetHtmlViewState(document);
        _contentSplit.Panel2Collapsed = !viewState.ShowPreviewPane;
        _previewButton.Enabled = viewState.CanUsePreviewPane;
        UpdatePreviewConceptBannerState(viewState.ShowPreviewPane ? document : null);
        UpdateEditConceptBannerState(document);
        if (viewState.ShowPreviewPane)
            ClampPreviewSplitter(_contentSplit, 240);
    }

    private void UpdatePreviewConceptBannerState(ToolEditorDocument? document = null)
    {
        document ??= _current;
        var viewState = document is null
            ? default
            : GetHtmlViewState(document);
        var canShowBanner = document is not null &&
            !_previewPaneClosedByUser &&
            viewState.ShowPreviewPane &&
            !document.HtmlEditMode;
        var showSignedBanner = canShowBanner && _openedPackageSigned;
        var showConceptBanner = canShowBanner &&
            !_openedPackageSigned &&
            !_conceptBannerDismissed &&
            viewState.PreviewKind == ToolEditorPreviewKind.Html;
        var showBanner = showSignedBanner || showConceptBanner;

        _previewConceptBanner.Kind = showSignedBanner ? ToolEditorBannerKind.Signed : ToolEditorBannerKind.Concept;
        _previewConceptBanner.Visible = showBanner;
        _previewConceptRow.Height = showBanner ? (showSignedBanner ? 48 : 72) : 0;
        UpdateConceptAnimationTimer();

        if (!showBanner)
            return;

        _previewConceptBanner.Message = showSignedBanner ? GetSignedBannerText() : GetConceptBannerText();
    }

    private void UpdateEditConceptBannerState(ToolEditorDocument? document = null)
    {
        document ??= _current;
        var canShowBanner = document is not null && GetHtmlViewState(document).EditIsPreview;
        var showSignedBanner = canShowBanner && _openedPackageSigned;
        var showConceptBanner = canShowBanner && !_openedPackageSigned && !_conceptBannerDismissed;
        var showBanner = showSignedBanner || showConceptBanner;

        _editConceptBanner.Kind = showSignedBanner ? ToolEditorBannerKind.Signed : ToolEditorBannerKind.Concept;
        _editConceptBanner.Visible = showBanner;
        _editConceptRow.Height = showBanner ? (showSignedBanner ? 48 : 72) : 0;
        UpdateConceptAnimationTimer();

        if (!showBanner)
            return;

        _editConceptBanner.Message = showSignedBanner ? GetSignedBannerText() : GetConceptBannerText();
    }

    private void UpdateConceptAnimationTimer()
    {
        var shouldAnimate =
            (_previewConceptBanner.Visible && _previewConceptBanner.Kind == ToolEditorBannerKind.Concept) ||
            (_editConceptBanner.Visible && _editConceptBanner.Kind == ToolEditorBannerKind.Concept);
        if (shouldAnimate)
        {
            if (!_previewConceptAnimationTimer.Enabled)
                _previewConceptAnimationTimer.Start();
        }
        else if (_previewConceptAnimationTimer.Enabled)
        {
            _previewConceptAnimationTimer.Stop();
        }
    }

    private string GetConceptBannerText()
    {
        return CurrentConceptBannerLanguagePrefix().Equals("nl", StringComparison.OrdinalIgnoreCase)
            ? "Concept: deze helpinformatie is werkmateriaal voor een taalpackage en is nog geen officiele Syscalculator-help."
            : "Concept: this help information is package draft material and is not official Syscalculator help yet.";
    }

    private string GetSignedBannerText()
    {
        var details = string.IsNullOrWhiteSpace(_openedPackageSignatureKeyId)
            ? ""
            : " Key: " + _openedPackageSignatureKeyId.Trim() + ".";
        if (!string.IsNullOrWhiteSpace(_openedPackageSignatureAlgorithm))
            details += " Algoritme: " + _openedPackageSignatureAlgorithm.Trim() + ".";
        return CurrentConceptBannerLanguagePrefix().Equals("nl", StringComparison.OrdinalIgnoreCase)
            ? "Signed: dit taalpakket is digitaal ondertekend en geverifieerd. Bewerk via .objpdk en compileer/sign opnieuw." + details
            : "Signed: this language package is digitally signed and verified. Edit through .objpdk and compile/sign again." + details;
    }

    private void ShowPreviewPane()
    {
        _previewPaneClosedByUser = false;
        UpdatePreview(initializePreview: true);
    }

    private void ClosePreviewPane()
    {
        _previewPaneClosedByUser = true;
        UpdatePreviewPaneState(_current);
    }

    private async void CloseDocument(ToolEditorDocument document)
    {
        await CloseDocumentsAsync([document]);
    }

    private async Task<bool> CloseDocumentsAsync(IEnumerable<ToolEditorDocument> documents)
    {
        var closing = documents.Where(document => document.IsOpen).Distinct().ToList();
        if (closing.Count == 0)
            return true;

        foreach (var document in closing)
        {
            if (!await ConfirmCloseDocumentAsync(document))
                return false;
        }

        var currentWasClosed = _current is not null && closing.Contains(_current);
        foreach (var document in closing)
        {
            document.IsOpen = false;
            _tabStrip.Controls.Remove(document.HeaderPanel);
        }

        if (currentWasClosed)
        {
            _current = GetOpenDocumentsInTabOrder().LastOrDefault();
            _editorContent.Controls.Clear();
            if (_current is not null)
                SelectDocument(_current);
            else
                UpdatePreview();
        }

        RefreshTabStrip();
        UpdateUiState();
        return true;
    }

    private async Task<bool> ConfirmCloseDocumentAsync(ToolEditorDocument document)
    {
        if (!document.Dirty || document.ReadOnly)
            return true;

        var result = MessageBox.Show(
            this,
            "Tab heeft niet-opgeslagen wijzigingen.\r\n\r\nOpslaan voordat de tab wordt gesloten?\r\n\r\n" + BuildTabTitle(document),
            "ToolEditor",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Cancel)
            return false;
        if (result == DialogResult.No)
            return true;

        SelectDocument(document);
        return await SaveDocumentAsync(document);
    }

    private void AttachTabContextMenu(ToolEditorDocument document, params Control[] controls)
    {
        var menu = CreateTabContextMenu(document);
        foreach (var control in controls)
        {
            control.MouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Right)
                    return;

                var screenLocation = control.PointToScreen(e.Location);
                BeginInvoke(() =>
                {
                    if (IsDisposed || menu.IsDisposed)
                        return;

                    var target = document.HeaderPanel is { IsDisposed: false, Visible: true }
                        ? document.HeaderPanel
                        : _tabStrip;
                    menu.Show(target, target.PointToClient(screenLocation));
                });
            };
        }
    }

    private ContextMenuStrip CreateTabContextMenu(ToolEditorDocument document)
    {
        var menu = new ContextMenuStrip();
        var closeAll = menu.Items.Add("Sluit alle tabbladen");
        var closeRight = menu.Items.Add("Sluit tabbladen rechts");
        var closeLeft = menu.Items.Add("Sluit tabbladen links");

        menu.Opening += (_, _) =>
        {
            var openDocuments = GetOpenDocumentsInTabOrder();
            var index = openDocuments.IndexOf(document);
            closeAll.Enabled = openDocuments.Count > 0;
            closeLeft.Enabled = index > 0;
            closeRight.Enabled = index >= 0 && index < openDocuments.Count - 1;
        };

        closeAll.Click += async (_, _) => await CloseDocumentsAsync(GetOpenDocumentsInTabOrder());
        closeRight.Click += async (_, _) =>
        {
            var openDocuments = GetOpenDocumentsInTabOrder();
            var index = openDocuments.IndexOf(document);
            if (index >= 0)
                await CloseDocumentsAsync(openDocuments.Skip(index + 1));
        };
        closeLeft.Click += async (_, _) =>
        {
            var openDocuments = GetOpenDocumentsInTabOrder();
            var index = openDocuments.IndexOf(document);
            if (index > 0)
                await CloseDocumentsAsync(openDocuments.Take(index));
        };

        return menu;
    }

    private List<ToolEditorDocument> GetOpenDocumentsInTabOrder()
    {
        var result = new List<ToolEditorDocument>();
        foreach (Control control in _tabStrip.Controls)
        {
            var document = _documents.FirstOrDefault(item => item.IsOpen && ReferenceEquals(item.HeaderPanel, control));
            if (document is not null)
                result.Add(document);
        }

        return result;
    }

    private void RemoveDocumentFromPackage(ToolEditorDocument document)
    {
        var wasCurrent = ReferenceEquals(_current, document);
        var nextDocument = wasCurrent ? _documents.LastOrDefault(item => !ReferenceEquals(item, document) && item.IsOpen) : _current;

        if (wasCurrent)
            _current = null;

        _tabStrip.Controls.Remove(document.HeaderPanel);
        _documents.Remove(document);
        document.HighlightTimer?.Dispose();
        document.Editor.Dispose();
        document.LineNumbers?.Dispose();
        document.Page.Dispose();

        RefreshTabStrip();
        RefreshFileTree();
        RefreshDocumentList();

        if (nextDocument is not null)
        {
            SelectDocument(nextDocument);
            return;
        }

        _editorContent.Controls.Clear();
        UpdatePreview();
        UpdateUiState();
    }

    private void EnsureDocumentTabOpen(ToolEditorDocument document)
    {
        if (document.IsOpen)
            return;

        document.IsOpen = true;
        if (!_tabStrip.Controls.Contains(document.HeaderPanel))
            _tabStrip.Controls.Add(document.HeaderPanel);

        RefreshTabStrip();
    }

    private void RefreshDocumentList()
    {
        if (_documentList.IsDisposed)
            return;

        _updatingNavigation = true;
        _documentList.BeginUpdate();
        _documentList.Items.Clear();
        foreach (var document in _documents.OrderBy(document => document.TreeGroup, StringComparer.CurrentCultureIgnoreCase)
                     .ThenBy(document => document.TreeTopic, StringComparer.CurrentCultureIgnoreCase))
        {
            var item = new ListViewItem(document.TreeGroup);
            item.SubItems.Add(document.TreeTopic);
            item.SubItems.Add(document.PackagePath);
            item.Tag = document;
            _documentList.Items.Add(item);
        }

        _documentList.EndUpdate();
        try
        {
            if (_current is not null)
                SelectDocumentInList(_current);
        }
        finally
        {
            _updatingNavigation = false;
        }
    }

    private void SelectDocumentInList(ToolEditorDocument document)
    {
        foreach (ListViewItem item in _documentList.Items)
        {
            var selected = ReferenceEquals(item.Tag, document);
            if (item.Selected == selected)
                continue;

            item.Selected = selected;
            if (selected)
                item.EnsureVisible();
        }
    }

    private void RemoveDocumentFromList(ToolEditorDocument document)
    {
        foreach (ListViewItem item in _documentList.Items)
        {
            if (!ReferenceEquals(item.Tag, document))
                continue;

            _documentList.Items.Remove(item);
            return;
        }
    }

    private void DocumentList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_updatingNavigation)
            return;

        if (_documentList.SelectedItems.Count == 0)
            return;

        if (_documentList.SelectedItems[0].Tag is ToolEditorDocument document && !ReferenceEquals(document, _current))
            SelectDocument(document);
    }

    private void SelectFirstGroup(string group)
    {
        var document = _documents.FirstOrDefault(document =>
            document.TreeGroup.Equals(group, StringComparison.CurrentCultureIgnoreCase));
        if (document is not null)
            SelectDocument(document);
    }

    private void RefreshFileTree()
    {
        if (_fileTree.IsDisposed)
            return;

        _updatingNavigation = true;
        _fileTree.BeginUpdate();
        _fileTree.SelectedNode = null;
        _fileTree.Nodes.Clear();
        var root = new TreeNode(TreeLabel("root", "Taalpakket"))
        {
            NodeFont = new Font(_fileTree.Font, FontStyle.Bold)
        };
        _fileTree.Nodes.Add(root);

        foreach (var document in _documents
                     .OrderBy(document => GetTreeSortGroup(document.PackagePath))
                     .ThenBy(document => document.PackagePath, StringComparer.OrdinalIgnoreCase))
            AddDocumentNode(root, document);

        root.Expand();
        _fileTree.EndUpdate();
        try
        {
            if (_current is not null)
                SelectDocumentInTree(_current);
        }
        finally
        {
            _updatingNavigation = false;
        }
    }

    private void AddDocumentNode(TreeNode root, ToolEditorDocument document)
    {
        var parts = BuildDocumentTreePath(document);
        var parent = root;
        for (var i = 0; i < parts.Count; i++)
        {
            var part = LocalizeTreePart(parts[i]);
            var existing = FindChild(parent, part);
            if (existing is null)
            {
                existing = new TreeNode(part);
                parent.Nodes.Add(existing);
            }

            parent = existing;
        }

        parent.Tag = document;
        parent.ToolTipText = BuildDocumentTooltip(document);
    }

    private static IReadOnlyList<string> BuildDocumentTreePath(ToolEditorDocument document)
    {
        var parts = BuildTreePath(document.PackagePath).ToList();
        if (parts.Count > 0 &&
            Path.GetExtension(document.PackagePath).Equals(".html", StringComparison.OrdinalIgnoreCase) &&
            TryGetDocumentHeading(document.Editor.Text, out var heading))
        {
            parts[^1] = heading;
        }

        return parts;
    }

    private static bool TryGetDocumentHeading(string text, out string heading)
    {
        heading = string.Empty;
        var match = Regex.Match(text, @"<h[12][^>]*>(?<title>[\s\S]*?)</h[12]>", RegexOptions.IgnoreCase);
        if (!match.Success)
            return false;

        var title = Regex.Replace(match.Groups["title"].Value, "<[^>]+>", "", RegexOptions.IgnoreCase);
        title = WebUtility.HtmlDecode(title).Trim();
        if (title.Length == 0 || title.StartsWith('['))
            return false;

        heading = title;
        return true;
    }

    private string LocalizeTreePart(string part)
    {
        if (part.StartsWith("Taal: ", StringComparison.OrdinalIgnoreCase))
            return TreeLabel("language_prefix", "Taal: ") + part["Taal: ".Length..];

        return part switch
        {
            "Vertaling" => TreeLabel("translation", "Vertaling"),
            "Help voor gebruikers" => TreeLabel("user_help", "Help voor gebruikers"),
            "NOD voor gebruikers" => TreeLabel("nod_users", "NOD voor gebruikers"),
            "NOD voor ontwikkelaars" => TreeLabel("nod_developers", "NOD voor ontwikkelaars"),
            "Belangrijke commands" => TreeLabel("important_commands", "Belangrijke commands"),
            "Formulekaart" => TreeLabel("formula_card", "Formulekaart"),
            "Taalmanager help" => TreeLabel("tool_editor_help", "Taalmanager help"),
            "Juridische documenten" => TreeLabel("legal_documents", "Juridische documenten"),
            "JavaScript documenten" => TreeLabel("javascript_documents", "JavaScript documenten"),
            "CSS documenten" => TreeLabel("css_documents", "CSS documenten"),
            "HTML templates" => TreeLabel("html_templates", "HTML-sjablonen"),
            "Media en afbeeldingen" => TreeLabel("media", "Media en afbeeldingen"),
            "Ongeldig pakketbestand" => TreeLabel("invalid_package_file", "Ongeldig pakketbestand"),
            "Startpagina" => TreeLabel("start_page", "Startpagina"),
            "Basis" => TreeLabel("base", "Basis"),
            "Help" => TreeLabel("help", "Help"),
            "Handleiding" => TreeLabel("manual", "Handleiding"),
            "Overig" => TreeLabel("other", "Overig"),
            "Overige scripts" => TreeLabel("other_scripts", "Overige scripts"),
            "Overige stijlen" => TreeLabel("other_styles", "Overige stijlen"),
            "NOD help" => TreeLabel("nod_help", "NOD help"),
            "Snippets" => TreeLabel("snippets", "Snippets"),
            _ => part
        };
    }

    private string TreeLabel(string key, string fallback)
    {
        return TToolEditor("tool_editor.tree." + key, fallback);
    }

    private static int GetTreeSortGroup(string packagePath)
    {
        var path = packagePath.Replace('\\', '/').Trim('/');
        if (path.StartsWith("language/", StringComparison.OrdinalIgnoreCase))
            return 0;

        if (path.StartsWith("manual/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("help/content/main/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("help/main/", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (path.StartsWith("help/content/nod/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("help/nod/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("nod/", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (path.StartsWith("formula/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("formula", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (path.StartsWith("help/content/tool-editor/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("help/tool-editor/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("tool-editor/", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (path.StartsWith("help/content/legal/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("help/legal/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("legal/", StringComparison.OrdinalIgnoreCase))
        {
            return 5;
        }

        if (Path.GetExtension(path).Equals(".js", StringComparison.OrdinalIgnoreCase))
            return 6;

        if (Path.GetExtension(path).Equals(".css", StringComparison.OrdinalIgnoreCase))
            return 7;

        if (path.StartsWith("help/content/helpapi/", StringComparison.OrdinalIgnoreCase))
            return 8;

        if (path.StartsWith("source/templates/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("templates/", StringComparison.OrdinalIgnoreCase))
            return 8;

        if (path.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) || IsImagePath(path))
            return 9;

        return 10;
    }

    private static string BuildDocumentTooltip(ToolEditorDocument document)
    {
        var tooltip = document.TreeGroup + Environment.NewLine +
            document.TreeTopic + Environment.NewLine +
            document.PackagePath;
        if (document.ImageBytes is not null)
            tooltip += Environment.NewLine + document.ImageBytes.Length.ToString("N0") + " bytes";

        return tooltip;
    }

    private static IReadOnlyList<string> BuildTreePath(string packagePath)
    {
        var path = packagePath.Replace('\\', '/').Trim('/');
        var fileName = Path.GetFileName(path);
        var topic = FriendlyTopicName(fileName);

        if (path.StartsWith("language/", StringComparison.OrdinalIgnoreCase))
            return ["Taal: " + FriendlyLanguageName(Path.GetFileNameWithoutExtension(fileName)), "Vertaling"];

        if (path.StartsWith("manual/", StringComparison.OrdinalIgnoreCase))
            return IsUserNodHelpFile(fileName)
                ? ["Help voor gebruikers", "NOD voor gebruikers", FriendlyUserNodHelpTopicName(fileName)]
                : ["Help voor gebruikers", topic];

        if (path.StartsWith("help/content/main/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("help/main/", StringComparison.OrdinalIgnoreCase))
        {
            return IsUserNodHelpFile(fileName)
                ? ["Help voor gebruikers", "NOD voor gebruikers", FriendlyUserNodHelpTopicName(fileName)]
                : ["Help voor gebruikers", topic];
        }

        if (TryGetNodHelpRelativePath(path, out var nodRelativePath))
        {
            return BuildNodHelpTreePath(nodRelativePath, topic);
        }

        if (TryGetToolEditorHelpRelativePath(path, out var toolEditorRelativePath))
        {
            return ["Taalmanager help", FriendlyToolEditorTopicName(Path.GetFileName(toolEditorRelativePath))];
        }

        if (TryGetLegalDocumentRelativePath(path, out var legalRelativePath))
        {
            return ["Juridische documenten", FriendlyLegalDocumentName(Path.GetFileName(legalRelativePath))];
        }

        if (Path.GetExtension(path).Equals(".js", StringComparison.OrdinalIgnoreCase))
            return BuildJavaScriptTreePath(fileName);

        if (Path.GetExtension(path).Equals(".css", StringComparison.OrdinalIgnoreCase))
            return BuildCssTreePath(path, fileName);

        if (TryGetHelpApiReferenceRelativePath(path, out var helpApiRelativePath))
        {
            return ["HTML templates", "HelpApi", FriendlyHelpApiReferenceName(Path.GetFileName(helpApiRelativePath))];
        }

        if (TryGetHelpTemplateRelativePath(path, out var helpTemplateRelativePath))
        {
            return ["HTML templates", "Help", FriendlyHelpTemplateName(Path.GetFileName(helpTemplateRelativePath))];
        }

        if (path.StartsWith("source/templates/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("templates/", StringComparison.OrdinalIgnoreCase))
        {
            return BuildTemplateTreePath(path, fileName);
        }

        if (path.StartsWith("formula/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("formula", StringComparison.OrdinalIgnoreCase))
        {
            var formulaParts = path.StartsWith("formula/", StringComparison.OrdinalIgnoreCase)
                ? path["formula/".Length..].Split('/', StringSplitOptions.RemoveEmptyEntries)
                : [];
            if (formulaParts.Length >= 2)
                return ["Formulekaart", FriendlyFormulaCategory(formulaParts[0]), FriendlyTopicName(formulaParts[^1])];

            return ["Formulekaart", topic];
        }

        if (path.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) || IsImagePath(path))
            return ["Media en afbeeldingen", fileName];

        return ["Ongeldig pakketbestand", topic];
    }

    private static string FriendlyTemplateDocumentName(string fileName)
    {
        return Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant() switch
        {
            "index" or "formula-index" => "Index",
            "card" or "formula-card" => "Kaartinhoud",
            _ => FriendlyTopicName(fileName)
        };
    }

    private static IReadOnlyList<string> BuildTemplateTreePath(string path, string fileName)
    {
        var normalized = NormalizePackagePath(path);
        if (normalized.StartsWith("source/templates/formula/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("templates/formula-", StringComparison.OrdinalIgnoreCase))
        {
            return ["HTML templates", "Formulekaart", FriendlyTemplateDocumentName(fileName)];
        }

        if (normalized.StartsWith("source/templates/nod/", StringComparison.OrdinalIgnoreCase))
            return ["HTML templates", "NOD help", FriendlyTemplateDocumentName(fileName)];

        if (normalized.StartsWith("source/templates/manual/", StringComparison.OrdinalIgnoreCase))
            return ["HTML templates", "Handleiding", FriendlyTemplateDocumentName(fileName)];

        if (normalized.StartsWith("source/templates/help/", StringComparison.OrdinalIgnoreCase))
            return ["HTML templates", "Help", FriendlyTemplateDocumentName(fileName)];

        return ["HTML templates", "Overig", FriendlyTemplateDocumentName(fileName)];
    }

    private static bool TryGetHelpTemplateRelativePath(string path, out string relativePath)
    {
        const string helpPrefix = "help/";

        if (path.StartsWith(helpPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var rest = path[helpPrefix.Length..];
            if (!rest.Contains('/') && Path.GetExtension(rest).Equals(".html", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = rest;
                return true;
            }
        }

        relativePath = string.Empty;
        return false;
    }

    private static string FriendlyHelpTemplateName(string fileName)
    {
        return Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant() switch
        {
            "document-body" => "Documentinhoud",
            "document-topic" => "Documentonderwerp",
            "formula-card" => "Formulekaart",
            "formula-film" => "Formulefilm",
            _ => FriendlyTemplateDocumentName(fileName)
        };
    }

    private static bool TryGetHelpApiReferenceRelativePath(string path, out string relativePath)
    {
        const string prefix = "help/content/helpapi/";
        if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = path[prefix.Length..];
            return true;
        }

        relativePath = string.Empty;
        return false;
    }

    private static string FriendlyHelpApiReferenceName(string fileName)
    {
        return Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant() switch
        {
            "readme" => "Overzicht",
            "javascript-css" => "JavaScript en CSS",
            "notice" => "Info blok",
            "warning" => "Waarschuwing",
            "details" => "Uitklapblok",
            "code" => "Code",
            "table" => "Tabel",
            "screenshot" => "Afbeelding",
            "command-link" => "Command-link",
            "example-card" => "Voorbeeldkaart",
            "dont" => "Niet doen",
            _ => FriendlyTopicName(fileName)
        };
    }

    private static bool TryGetLegalDocumentRelativePath(string path, out string relativePath)
    {
        const string contentPrefix = "help/content/legal/";
        const string helpPrefix = "help/legal/";
        const string legalPrefix = "legal/";

        if (path.StartsWith(contentPrefix, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = path[contentPrefix.Length..];
            return true;
        }

        if (path.StartsWith(helpPrefix, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = path[helpPrefix.Length..];
            return true;
        }

        if (path.StartsWith(legalPrefix, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = path[legalPrefix.Length..];
            return true;
        }

        relativePath = string.Empty;
        return false;
    }

    private static string FriendlyLegalDocumentName(string fileName)
    {
        return Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant() switch
        {
            "license-agreement" => "Licentieovereenkomst",
            "privacy-statement" => "Privacyverklaring",
            _ => FriendlyTopicName(fileName)
        };
    }

    private static string FriendlyJavaScriptDocumentName(string fileName)
    {
        return Path.GetFileName(fileName).ToLowerInvariant() switch
        {
            "basis.js" => "Basisweergave",
            "main-help.js" => "Hoofdhelp",
            "nod.js" => "NOD weergave",
            "nod-popup.js" => "NOD popup tips",
            "formula.js" => "Formulekaart weergave",
            _ => FriendlyTopicName(fileName)
        };
    }

    private static IReadOnlyList<string> BuildJavaScriptTreePath(string fileName)
    {
        return Path.GetFileName(fileName).ToLowerInvariant() switch
        {
            "basis.js" => ["JavaScript documenten", "Basis", "Basisweergave"],
            "main-help.js" => ["JavaScript documenten", "Help voor gebruikers", "Hoofdhelp"],
            "nod.js" => ["JavaScript documenten", "NOD help", "NOD weergave"],
            "nod-popup.js" => ["JavaScript documenten", "NOD help", "NOD popup tips"],
            "formula.js" => ["JavaScript documenten", "Formulekaart", "Formulekaart weergave"],
            _ => ["JavaScript documenten", "Overige scripts", FriendlyJavaScriptDocumentName(fileName)]
        };
    }

    private static string FriendlyCssDocumentName(string fileName)
    {
        return Path.GetFileName(fileName).ToLowerInvariant() switch
        {
            "main-help.css" => "Hoofdhelp",
            "nod-popup.css" => "NOD popup tips",
            _ => FriendlyTopicName(fileName)
        };
    }

    private static IReadOnlyList<string> BuildCssTreePath(string path, string fileName)
    {
        var normalized = NormalizePackagePath(path);
        return Path.GetFileName(fileName).ToLowerInvariant() switch
        {
            "main-help.css" => ["CSS documenten", "Help voor gebruikers", "Hoofdhelp"],
            "nod-popup.css" => ["CSS documenten", "NOD help", "NOD popup tips"],
            _ when normalized.StartsWith("help/", StringComparison.OrdinalIgnoreCase) => ["CSS documenten", "Help", FriendlyCssDocumentName(fileName)],
            _ => ["CSS documenten", "Overige stijlen", FriendlyCssDocumentName(fileName)]
        };
    }

    private static bool TryGetNodHelpRelativePath(string path, out string relativePath)
    {
        const string contentPrefix = "help/content/nod/";
        const string helpPrefix = "help/nod/";
        const string nodPrefix = "nod/";

        if (path.StartsWith(contentPrefix, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = path[contentPrefix.Length..];
            return true;
        }

        if (path.StartsWith(helpPrefix, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = path[helpPrefix.Length..];
            return true;
        }

        if (path.StartsWith(nodPrefix, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = path[nodPrefix.Length..];
            return true;
        }

        relativePath = string.Empty;
        return false;
    }

    private static bool TryGetToolEditorHelpRelativePath(string path, out string relativePath)
    {
        const string contentPrefix = "help/content/tool-editor/";
        const string helpPrefix = "help/tool-editor/";
        const string toolEditorPrefix = "tool-editor/";

        if (path.StartsWith(contentPrefix, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = path[contentPrefix.Length..];
            return true;
        }

        if (path.StartsWith(helpPrefix, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = path[helpPrefix.Length..];
            return true;
        }

        if (path.StartsWith(toolEditorPrefix, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = path[toolEditorPrefix.Length..];
            return true;
        }

        relativePath = string.Empty;
        return false;
    }

    private static string FriendlyToolEditorTopicName(string fileName)
    {
        return Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant() switch
        {
            "overview" => "ToolEditor gebruiken",
            "package-workflow" => "Taalpakket workflow",
            "html-editor" => "HTML bewerken",
            "media" => "Media en afbeeldingen",
            "compile" => "Valideren en compileren",
            _ => FriendlyTopicName(fileName)
        };
    }

    private static string FriendlyNodHelpCategory(string category)
    {
        return category.ToLowerInvariant() switch
        {
            "full" => "NOD help",
            "command" => "Belangrijke commands",
            "popup" => "Belangrijke commands",
            "snippet" => "Snippets",
            _ => FriendlyTopicName(category)
        };
    }

    private static IReadOnlyList<string> BuildNodHelpTreePath(string relativePath, string fallbackTopic)
    {
        var parts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return ["NOD voor ontwikkelaars", fallbackTopic];

        var section = parts[0].ToLowerInvariant();
        var fileName = parts[^1];
        var topic = FriendlyTopicName(fileName);
        if (section == "full")
        {
            var fullPage = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
            if (fullPage is "index" or "title")
                return ["NOD voor ontwikkelaars", "Startpagina"];
            if (fullPage == "keyword_title")
                return ["NOD voor ontwikkelaars", "Belangrijke commands", "Startpagina"];

            if (TryGetNodCommandGroupFromFullPage(fileName, out var group))
                return ["NOD voor ontwikkelaars", "Belangrijke commands", group, topic];

            return ["NOD voor ontwikkelaars", topic];
        }

        if (section == "popup")
        {
            var command = Path.GetFileNameWithoutExtension(fileName);
            return ["NOD voor ontwikkelaars", "Belangrijke commands", FriendlyNodCommandGroup(command), topic];
        }

        if (section == "command")
        {
            var command = Path.GetFileNameWithoutExtension(fileName);
            return ["NOD voor ontwikkelaars", "Belangrijke commands", FriendlyNodCommandGroup(command), topic];
        }

        if (section == "snippet")
            return ["NOD voor ontwikkelaars", "Snippets", topic];

        return ["NOD voor ontwikkelaars", FriendlyNodHelpCategory(section), topic];
    }

    private static bool TryGetNodCommandGroupFromFullPage(string fileName, out string group)
    {
        group = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant() switch
        {
            "commands-basic" => "Basis en velden",
            "commands-math" => "Rekenen",
            "commands-text" => "Tekst en vertaling",
            "commands-data" => "Data",
            "commands-equation" => "Vergelijkingen",
            "commands-system" => "NOD-systeem",
            _ => ""
        };

        return group.Length > 0;
    }

    private static string FriendlyNodCommandGroup(string commandOrPage)
    {
        var key = commandOrPage.ToLowerInvariant();
        if (key.EndsWith("-extra", StringComparison.OrdinalIgnoreCase))
            key = key[..^"-extra".Length];
        if (key.EndsWith("-advanced", StringComparison.OrdinalIgnoreCase))
            key = key[..^"-advanced".Length];
        if (key.EndsWith("-improved", StringComparison.OrdinalIgnoreCase))
            key = key[..^"-improved".Length];

        return key switch
        {
            "name" or "input" or "inputr" or "input1" or "input2" or "result" or "resfou" or "symb1" or "symb2" or "symb3" or "symb4" or "format" => "Basis en velden",
            "math" or "reverse" => "Rekenen",
            "trans" or "chg" => "Tekst en vertaling",
            "table" or "field" or "output" or "phoneformat" or "lookup" or "match" => "Data",
            "given" or "equation" or "solve" or "constraint" => "Vergelijkingen",
            "mode" or "urln" or "preview" or "backup" or "indoprint" or "indoend" or "end" => "NOD-systeem",
            _ => "Overige commands"
        };
    }

    private static string FriendlyFormulaCategory(string category)
    {
        return category.ToLowerInvariant() switch
        {
            "algebra" => "Algebra",
            "analyse" => "Analyse",
            "basis" => "Basis",
            "goniometrie" => "Goniometrie",
            "kansrekening" => "Kansrekening",
            "meetkunde" => "Meetkunde",
            "statistiek" => "Statistiek",
            _ => FriendlyTopicName(category)
        };
    }

    private (string Group, string Topic) BuildListLabels(ToolEditorDocument document)
    {
        var path = BuildDocumentTreePath(document);
        return path.Count >= 2
            ? (LocalizeTreePart(path[0]), LocalizeTreePart(path[^1]))
            : (TreeLabel("invalid_package_file", "Ongeldig pakketbestand"), LocalizeTreePart(path[0]));
    }

    private void ApplyDocumentLabels(ToolEditorDocument document)
    {
        var labels = BuildListLabels(document);
        document.TreeGroup = labels.Group;
        document.TreeTopic = labels.Topic;
    }

    private static string FriendlyLanguageName(string code)
    {
        return code.ToLowerInvariant() switch
        {
            "ned" or "nl" or "nl-nl" => "Nederlands",
            "eng" or "en" or "en-us" or "en-gb" => "English",
            "deu" or "de" => "Deutsch",
            "fra" or "fr" => "Francais",
            "spa" or "es" => "Espanol",
            "ita" or "it" => "Italiano",
            "por" or "pt" => "Portugues",
            "ind" or "id" => "Indonesia",
            "zho" or "zh" => "Chinese",
            _ => code.ToUpperInvariant()
        };
    }

    private static string FriendlyTopicName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(name))
            return "Onderwerp";

        name = Regex.Replace(name, @"^(help|manual)[_-]+(nl|ned)[_-]+", "", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"^(nl|ned)[_-]+", "", RegexOptions.IgnoreCase);

        if (name.Equals("index", StringComparison.OrdinalIgnoreCase))
            return "Startpagina";
        if (name.Equals("readme", StringComparison.OrdinalIgnoreCase))
            return "Overzicht";

        return string.Join(
            ' ',
            name.Replace('_', '-')
                .Split('-', StringSplitOptions.RemoveEmptyEntries)
                .Select(FriendlyTopicPart));
    }

    private static string FriendlyTopicPart(string part)
    {
        if (part.Equals("nod", StringComparison.OrdinalIgnoreCase))
            return "NOD";

        return char.ToUpperInvariant(part[0]) + part[1..];
    }

    private static TreeNode? FindChild(TreeNode parent, string text)
    {
        foreach (TreeNode node in parent.Nodes)
        {
            if (node.Text.Equals(text, StringComparison.OrdinalIgnoreCase))
                return node;
        }

        return null;
    }

    private void SelectDocumentInTree(ToolEditorDocument document)
    {
        var node = FindDocumentNode(_fileTree.Nodes, document);
        if (node is null || _fileTree.SelectedNode == node)
            return;

        _fileTree.SelectedNode = node;
        node.EnsureVisible();
    }

    private static TreeNode? FindDocumentNode(TreeNodeCollection nodes, ToolEditorDocument document)
    {
        foreach (TreeNode node in nodes)
        {
            if (ReferenceEquals(node.Tag, document))
                return node;

            var child = FindDocumentNode(node.Nodes, document);
            if (child is not null)
                return child;
        }

        return null;
    }

    private void RemoveDocumentFromTree(ToolEditorDocument document)
    {
        var node = FindDocumentNode(_fileTree.Nodes, document);
        node?.Remove();
    }

    private void FileTree_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (_updatingNavigation)
            return;

        if (e.Node?.Tag is null && e.Node?.Text.Equals("Media en afbeeldingen", StringComparison.OrdinalIgnoreCase) == true)
        {
            SelectMediaManager();
            return;
        }

        if (e.Node?.Tag is ToolEditorDocument document && !ReferenceEquals(document, _current))
            SelectDocument(document);
    }

    private async void ValidateCurrent(bool showMessage)
    {
        if (_current is null)
            return;

        EnsureDocumentTextLoaded(_current, ensureEditorUi: true);
        await SyncHtmlEditorToSourceAsync();

        var errors = ValidateDocument(_current);
        AddMissingMediaLinkErrors(_current, errors);
        AddMissingInternalLinkErrors(_current, errors);
        if (errors.Count == 0)
        {
            SetStatus(TToolEditor("tool_editor.status.validation_passed", "Validation passed: ") + _current.DisplayName, isError: false);
            if (showMessage)
                MessageBox.Show(this, TToolEditor("tool_editor.validation.passed", "Validation passed."), "ToolEditor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetStatus(TToolEditor("tool_editor.status.validation_failed", "Validation failed: ") + errors[0], isError: true);
        if (showMessage)
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "ToolEditor validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ValidateManifest(bool showMessage)
    {
        var errors = new List<string>();
        ValidateJson(_manifestText, errors);
        if (errors.Count == 0)
        {
            SetStatus(TToolEditor("tool_editor.status.manifest_valid", "Manifest is valid."), isError: false);
            if (showMessage)
                MessageBox.Show(this, TToolEditor("tool_editor.manifest.valid", "Manifest is valid."), "ToolEditor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetStatus(TToolEditor("tool_editor.status.manifest_invalid", "Manifest invalid: ") + errors[0], isError: true);
        if (showMessage)
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Manifest", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ValidatePackageLinks(bool showMessage)
    {
        EnsureAllLazyDocumentsLoaded();
        var errors = new List<string>();
        foreach (var document in _documents)
        {
            AddMissingMediaLinkErrors(document, errors);
            AddMissingInternalLinkErrors(document, errors);
        }

        if (errors.Count == 0)
        {
            SetStatus(TToolEditor("tool_editor.status.links_valid", "All HTML links point to existing package documents and media."), isError: false);
            if (showMessage)
                MessageBox.Show(this, TToolEditor("tool_editor.links.valid", "All HTML links were found in the package."), "ToolEditor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetStatus(TToolEditor("tool_editor.status.link_missing", "HTML link missing: ") + errors[0], isError: true);
        if (showMessage)
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Ontbrekende links", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private async Task CompileLanguagePackageAsync()
    {
        EnsureAllLazyDocumentsLoaded();
        await SyncHtmlEditorToSourceAsync();

        var errors = new List<string>();
        ValidatePackageForCompile(errors);
        errors = errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (errors.Count > 0)
        {
            SetStatus(TToolEditor("tool_editor.status.package_not_compiled", "Package not compiled: ") + errors[0], isError: true);
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), TToolEditor("tool_editor.compile.title", "Compile package"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var manifest = ReadManifestProperties(_manifestText);
        using var dialog = new SaveFileDialog
        {
            Title = TToolEditor("tool_editor.compile.title", "Compile package"),
            FileName = "Syscalculator-" + SanitizeFileName(manifest.LanguageCode) + ".lngpdk",
            Filter = "Tiedragon language package (*.lngpdk)|*.lngpdk",
            OverwritePrompt = true,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var outputPath = Path.GetFullPath(dialog.FileName);
        if (!Path.GetExtension(outputPath).Equals(".lngpdk", StringComparison.OrdinalIgnoreCase))
            outputPath += ".lngpdk";

        var tempFolder = Path.Combine(Path.GetTempPath(), "Tiedragon.ToolEditor", "compile", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempFolder);
            var objectPackagePath = Path.Combine(tempFolder, "source" + ObjectPackageExtension);
            WriteObjectPackageArchive(objectPackagePath);
            var result = BuildLanguagePackage(objectPackagePath, outputPath);
            SetStatus(TToolEditor("tool_editor.status.package_compiled", "Package compiled. SHA-256: ") + result.PackageSha256, isError: false);
            MessageBox.Show(
                this,
                TToolEditor("tool_editor.compile.success.path", "Package compiled:\r\n") + outputPath +
                "\r\n\r\nPackage SHA-256:\r\n" + result.PackageSha256 +
                "\r\n\r\nPayload SHA-256:\r\n" + result.PayloadSha256 +
                "\r\n\r\n" + TToolEditor("tool_editor.compile.success.encryption", "Encryption: not active yet\r\nThe reader currently accepts only unencrypted packages."),
                TToolEditor("tool_editor.compile.title", "Compile package"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            SetStatus(TToolEditor("tool_editor.status.package_compile_failed", "Package compile failed: ") + ex.Message, isError: true);
            MessageBox.Show(this, ex.Message, TToolEditor("tool_editor.menu.package.compile", "Compile package..."), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private void ShowPackageSha256()
    {
        using var dialog = new OpenFileDialog
        {
            Title = TToolEditor("tool_editor.sha256.title", "Calculate SHA-256"),
            Filter = TToolEditor("tool_editor.sha256.filter", "Tiedragon language package (*.lngpdk)|*.lngpdk|All files (*.*)|*.*"),
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var hash = ComputeSha256File(dialog.FileName);
            SetStatus(TToolEditor("tool_editor.status.sha256", "SHA-256: ") + hash, isError: false);
            MessageBox.Show(this, dialog.FileName + "\r\n\r\nSHA-256:\r\n" + hash, "SHA-256", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus(TToolEditor("tool_editor.status.sha256_failed", "SHA-256 failed: ") + ex.Message, isError: true);
            MessageBox.Show(this, ex.Message, "SHA-256", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowEncryptionStatus()
    {
        MessageBox.Show(
            this,
            TToolEditor("tool_editor.security.info.message", "The `.lngpdk` compile uses a strict container with double SHA-256 checks:\r\n\r\n- package SHA-256 for the full file\r\n- payload SHA-256 in the container header\r\n\r\nEncryption is intentionally disabled for now. Syscalculator refuses encrypted packages fail-closed until support is complete.\r\n\r\nSigning uses RSA-PSS-SHA256. If `Signed` is enabled, the reader verifies against language-package-trusted-keys.json and refuses fail-closed without a trusted public key."),
            TToolEditor("tool_editor.security.info.title", "Package security"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ValidatePackageForCompile(List<string> errors)
    {
        EnsureAllLazyDocumentsLoaded();
        ValidateJson(_manifestText, errors);
        if (errors.Count > 0)
            return;

        using var json = JsonDocument.Parse(_manifestText);
        var root = json.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            errors.Add("manifest.json moet een JSON-object zijn.");
            return;
        }

        if (!root.TryGetProperty("format", out var format) ||
            format.ValueKind != JsonValueKind.Number ||
            !format.TryGetInt32(out var manifestFormat) ||
            manifestFormat != 1)
        {
            errors.Add("manifest.json mist format 1.");
        }

        var manifest = ReadManifestProperties(_manifestText);
        if (!manifest.Producer.Equals("Tiedragon", StringComparison.OrdinalIgnoreCase))
            errors.Add("manifest.json producer moet Tiedragon zijn.");
        if (!manifest.Product.Equals("Syscalculator", StringComparison.OrdinalIgnoreCase))
            errors.Add("manifest.json product moet Syscalculator zijn.");
        if (!manifest.SoftwareId.Equals(LanguagePackageSoftwareId, StringComparison.OrdinalIgnoreCase))
            errors.Add("manifest.json softwareId moet " + LanguagePackageSoftwareId + " zijn.");
        if (!IsSafePackageToken(manifest.LanguageCode))
            errors.Add("manifest.json languageCode is ongeldig.");
        if (string.IsNullOrWhiteSpace(manifest.DisplayName))
            errors.Add("manifest.json displayName ontbreekt.");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var document in _documents)
        {
            ValidatePackageEntryPath(document.PackagePath, document.ImageBytes is not null, errors);
            if (!seen.Add(NormalizePackagePath(document.PackagePath)))
                errors.Add("Dubbel packagepad: " + document.PackagePath);

            errors.AddRange(ValidateDocument(document));
            AddMissingMediaLinkErrors(document, errors);
            AddMissingInternalLinkErrors(document, errors);
        }

        if (!string.IsNullOrWhiteSpace(manifest.LanguageCode))
        {
            var languagePath = "language/" + manifest.LanguageCode + ".lng";
            if (!_documents.Any(document => document.PackagePath.Equals(languagePath, StringComparison.OrdinalIgnoreCase)))
                errors.Add(languagePath + " ontbreekt.");
        }
    }

    private static void ValidatePackageEntryPath(string packagePath, bool isImage, List<string> errors)
    {
        var path = NormalizePackagePath(packagePath);
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path) || path.Split('/').Any(part => part == ".."))
        {
            errors.Add("Ongeldig packagepad: " + packagePath);
            return;
        }

        if (!LanguagePackagePolicy.Current.IsAllowedPackagePath(path))
            errors.Add("Package path is not allowed: " + packagePath);

        if (isImage && !path.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
            errors.Add("Media hoort onder assets/: " + packagePath);
    }

    private void WritePackageSourceFolder(string rootFolder)
    {
        EnsureAllLazyDocumentsLoaded();
        Directory.CreateDirectory(rootFolder);
        File.WriteAllText(GetSafePackageFilePath(rootFolder, "manifest.json"), _manifestText, Encoding.UTF8);
        var explicitEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var document in _documents.OrderBy(document => document.PackagePath, StringComparer.OrdinalIgnoreCase))
        {
            var packagePath = NormalizePackagePath(document.PackagePath);
            if (IsManifestPath(packagePath) || IsCompileSourceOnlyDocument(packagePath))
                continue;
            if (IsGeneratedFormulaHtmlDocument(packagePath) && !document.Dirty)
                continue;

            var filePath = GetSafePackageFilePath(rootFolder, packagePath);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            if (document.ImageBytes is not null)
                File.WriteAllBytes(filePath, document.ImageBytes);
            else
                File.WriteAllText(filePath, GetDocumentTextForStorage(document), Encoding.UTF8);
            explicitEntries.Add(packagePath);
        }

        foreach (var entry in BuildGeneratedFormulaDocumentsForCompile())
        {
            if (explicitEntries.Contains(entry.Key))
                continue;

            var filePath = GetSafePackageFilePath(rootFolder, entry.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, entry.Value, Encoding.UTF8);
        }
    }

    private void WriteObjectPackageArchive(string outputPath)
    {
        EnsureAllLazyDocumentsLoaded();
        var fullOutputPath = Path.GetFullPath(outputPath);
        var outputDirectory = Path.GetDirectoryName(fullOutputPath) ??
            throw new InvalidOperationException("Outputpad is ongeldig.");
        Directory.CreateDirectory(outputDirectory);

        var tempPath = Path.Combine(outputDirectory, Path.GetFileName(fullOutputPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var file = File.Create(tempPath))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Create))
            {
                WriteObjectPackageEntry(archive, "manifest.json", Encoding.UTF8.GetBytes(_manifestText));

                foreach (var document in _documents.OrderBy(document => document.PackagePath, StringComparer.OrdinalIgnoreCase))
                {
                    var packagePath = NormalizePackagePath(document.PackagePath);
                    if (IsManifestPath(packagePath))
                        continue;

                    var bytes = document.ImageBytes ?? Encoding.UTF8.GetBytes(GetDocumentTextForStorage(document));
                    WriteObjectPackageEntry(archive, packagePath, bytes);
                }
            }

            File.Move(tempPath, fullOutputPath, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static void WriteObjectPackageEntry(ZipArchive archive, string packagePath, byte[] bytes)
    {
        var normalizedPath = NormalizePackagePath(packagePath);
        ValidatePackageEntryPathOrThrow(normalizedPath, IsImagePath(normalizedPath));
        var entry = archive.CreateEntry(normalizedPath, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(bytes);
    }

    private IReadOnlyDictionary<string, string> BuildGeneratedFormulaDocumentsForCompile()
    {
        var languageMap = ReadLanguageText(
            _documents.FirstOrDefault(document =>
                    document.PackagePath.StartsWith("language/", StringComparison.OrdinalIgnoreCase) &&
                    document.PackagePath.EndsWith(".lng", StringComparison.OrdinalIgnoreCase))
                ?.Editor.Text ?? "");
        var indexTemplate = FindTemplateText("source/templates/formula/index.html", "templates/formula-index.html", FormulaIndexPackageTemplate);
        var cardTemplate = FindTemplateText("source/templates/formula/card.html", "templates/formula-card.html", FormulaCardPackageTemplate);
        var cards = FormulaCardCatalog.GetDefaultCards()
            .Select(card => LocalizeFormulaCard(card, languageMap))
            .ToArray();
        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["formula/index.html"] = BuildFormulaIndexHtml(cards, languageMap, indexTemplate)
        };

        foreach (var card in cards.OrderBy(card => card.Title, StringComparer.CurrentCultureIgnoreCase))
            entries[BuildFormulaCardPackagePath(card.Source)] = BuildFormulaCardHtml(card, languageMap, cardTemplate);

        return entries;
    }

    private static bool IsCompileSourceOnlyDocument(string packagePath)
    {
        var path = NormalizePackagePath(packagePath);
        return path.StartsWith("source/templates/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("templates/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGeneratedFormulaHtmlDocument(string packagePath)
    {
        var path = NormalizePackagePath(packagePath);
        return path.StartsWith("formula/", StringComparison.OrdinalIgnoreCase) &&
            Path.GetExtension(path).Equals(".html", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetDocumentTextForStorage(ToolEditorDocument document)
    {
        var text = document.Editor.Text;
        return Path.GetExtension(document.PackagePath).Equals(".html", StringComparison.OrdinalIgnoreCase)
            ? EnsureTrailingEndLine(text)
            : text;
    }

    private static LanguagePackageCompileResult BuildLanguagePackage(string sourcePath, string outputPath)
    {
        using var preparedSource = PreparePackageSourceFolder(sourcePath);
        var payload = BuildZipPayload(preparedSource.Folder);
        var payloadSha256 = ComputeSha256Bytes(payload);
        var header = new LanguagePackageContainerHeader(
            LanguagePackageContainerFormat,
            LanguagePackageSoftwareId,
            LanguagePackageType,
            "zip",
            payloadSha256,
            Encrypted: false,
            Signed: false);

        var headerBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header, LanguagePackageJsonOptions));
        if (headerBytes.Length > MaxPackageHeaderBytes)
            throw new InvalidDataException("Packageheader is te groot.");
        if (payload.Length > MaxPackagePayloadBytes)
            throw new InvalidDataException("Packagepayload is te groot.");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        using (var output = File.Create(outputPath))
        {
            output.Write(LanguagePackageMagic);
            using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);
            writer.Write(LanguagePackageContainerFormat);
            writer.Write(headerBytes.Length);
            writer.Write(headerBytes);
            writer.Write(payload);
        }

        return new LanguagePackageCompileResult(outputPath, ComputeSha256File(outputPath), payloadSha256);
    }

    private static PreparedPackageSourceFolder PreparePackageSourceFolder(string sourcePath)
    {
        var fullPath = Path.GetFullPath(sourcePath);
        if (Directory.Exists(fullPath))
            return new PreparedPackageSourceFolder(fullPath, null);

        if (File.Exists(fullPath) && Path.GetExtension(fullPath).Equals(ObjectPackageExtension, StringComparison.OrdinalIgnoreCase))
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "Tiedragon.ToolEditor", "objpdk", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempFolder);
            ExtractObjectPackageArchive(fullPath, tempFolder);
            return new PreparedPackageSourceFolder(tempFolder, tempFolder);
        }

        throw new InvalidDataException("Compilebron moet een map of .objpdk zijn: " + sourcePath);
    }

    private static void ExtractObjectPackageArchive(string archivePath, string targetFolder)
    {
        var root = Path.GetFullPath(targetFolder);
        using var file = File.OpenRead(archivePath);
        using var archive = new ZipArchive(file, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
                continue;

            var packagePath = NormalizePackagePath(entry.FullName);
            ValidatePackageEntryPathOrThrow(packagePath, IsImagePath(packagePath));
            var outputPath = Path.GetFullPath(Path.Combine(root, packagePath.Replace('/', Path.DirectorySeparatorChar)));
            var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
            if (!outputPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Packagepad valt buiten tijdelijke map: " + packagePath);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            using var input = entry.Open();
            using var output = File.Create(outputPath);
            input.CopyTo(output);
        }
    }

    private static byte[] BuildZipPayload(string sourceFolder)
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var relative = Path.GetRelativePath(sourceFolder, file).Replace('\\', '/');
                var entry = archive.CreateEntry(relative, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var source = File.OpenRead(file);
                source.CopyTo(entryStream);
            }
        }

        return memory.ToArray();
    }

    private static string GetSafePackageFilePath(string rootFolder, string packagePath)
    {
        var root = Path.GetFullPath(rootFolder);
        var fullPath = Path.GetFullPath(Path.Combine(root, NormalizePackagePath(packagePath).Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Packagepad valt buiten tijdelijke map: " + packagePath);

        return fullPath;
    }

    private static string ComputeSha256Bytes(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static string ComputeSha256File(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static bool IsSafePackageToken(string value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_');
    }

    private static string SanitizeFileName(string value)
    {
        var cleaned = new string(value.Where(character => char.IsLetterOrDigit(character) || character is '-' or '_').ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "language" : cleaned;
    }

    private void AddMissingMediaLinkErrors(ToolEditorDocument document, List<string> errors)
    {
        if (document.ImageBytes is not null || !IsHtmlDocument(document))
            return;

        foreach (Match match in HtmlMediaLinkRegex.Matches(GetDocumentEngineHtml(document)))
        {
            var link = match.Groups["path"].Value;
            if (!MediaExists(link))
                errors.Add(document.PackagePath + " verwijst naar ontbrekende media: " + link);
        }
    }

    private void AddMissingInternalLinkErrors(ToolEditorDocument document, List<string> errors)
    {
        if (document.ImageBytes is not null || !IsHtmlDocument(document))
            return;

        foreach (Match match in HtmlHrefLinkRegex.Matches(GetDocumentEngineHtml(document)))
        {
            var link = match.Groups["path"].Value;
            if (!ShouldValidateInternalLink(link))
                continue;

            if (!TryResolvePackageLink(link, document, out _))
                errors.Add(document.PackagePath + " verwijst naar ontbrekend document: " + link);
        }
    }

    private static bool ShouldValidateInternalLink(string link)
    {
        var cleaned = WebUtility.HtmlDecode(link).Trim();
        if (string.IsNullOrWhiteSpace(cleaned) || cleaned.StartsWith("#", StringComparison.Ordinal))
            return false;
        if (cleaned.StartsWith("http:", StringComparison.OrdinalIgnoreCase) ||
            cleaned.StartsWith("https:", StringComparison.OrdinalIgnoreCase) ||
            cleaned.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            cleaned.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        if (IsImagePath(cleaned))
            return false;

        return cleaned.StartsWith("nodpage:", StringComparison.OrdinalIgnoreCase) ||
            cleaned.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
    }

    private List<string> ValidateDocument(ToolEditorDocument document)
    {
        var errors = new List<string>();
        if (document.ImageBytes is not null)
            return errors;

        var name = document.DisplayName;
        var extension = Path.GetExtension(name);
        var text = document.Editor.Text;
        var engineText = extension.Equals(".html", StringComparison.OrdinalIgnoreCase)
            ? GetDocumentEngineHtml(document)
            : text;

        if (!IsAllowedPackageDocumentPath(document.PackagePath))
            errors.Add("Package path is not allowed: " + document.PackagePath);

        ValidateNoMojibake(text, errors);

        if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            ValidateJson(text, errors);
        else if (extension.Equals(".lng", StringComparison.OrdinalIgnoreCase))
            ValidateLanguageFile(text, errors);
        else if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase))
        {
            if (!engineText.Contains('<'))
                errors.Add(document.PackagePath + ": HTML document does not contain markup.");

            AddMathMlValidationErrors(document.PackagePath, engineText, errors);
        }

        if (string.IsNullOrWhiteSpace(text))
            errors.Add("Document is empty.");

        return errors;
    }

    private static void AddMathMlValidationErrors(string packagePath, string html, List<string> errors)
    {
        foreach (var error in MathMlParser.ValidateFragments(html))
            errors.Add(packagePath + ": " + error);
    }

    private string GetDocumentEngineHtml(ToolEditorDocument document)
    {
        return IsHtmlDocument(document)
            ? ApplyToolEditorHelpPlaceholders(document.Editor.Text)
            : document.Editor.Text;
    }

    private static void ValidateNoMojibake(string text, List<string> errors)
    {
        var lineNumber = 0;
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            lineNumber++;
            if (ContainsMojibakeMarker(line))
                errors.Add("Line " + lineNumber + " contains possible encoding damage/mojibake.");
        }
    }

    private static bool ContainsMojibakeMarker(string text)
    {
        return text.Contains('\u00c3') ||
            text.Contains('\u00c2') ||
            text.Contains('\u00e2') ||
            text.Contains("\u00e4\u00b8", StringComparison.Ordinal) ||
            text.Contains("\u00e6\u2013", StringComparison.Ordinal);
    }

    private static bool IsAllowedPackageDocumentPath(string packagePath)
    {
        var path = NormalizePackagePath(packagePath);
        return LanguagePackagePolicy.Current.IsAllowedPackagePath(path) &&
            !LanguagePackagePolicy.Current.IsImagePath(path);
    }

    private static bool IsProtectedPackageDocument(string packagePath)
    {
        var path = NormalizePackagePath(packagePath);
        return path.StartsWith("language/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("manual/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("help/content/main/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("help/main/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("help/content/nod/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("help/nod/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("nod/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("help/content/tool-editor/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("help/tool-editor/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("tool-editor/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("source/templates/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("templates/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("formula/", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePackagePath(string value)
    {
        return value.Replace('\\', '/').Trim('/');
    }

    private static bool IsManifestPath(string packagePath)
    {
        return NormalizePackagePath(packagePath).Equals("manifest.json", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImagePath(string path)
    {
        return LanguagePackagePolicy.Current.IsImagePath(path);
    }

    private static bool IsUserNodHelpFile(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
        return name.Contains("nod", StringComparison.OrdinalIgnoreCase);
    }

    private static string FriendlyUserNodHelpTopicName(string fileName)
    {
        return Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant() switch
        {
            "nodfiles" => "NOD-bestanden en Catalogus",
            "nodeditor" => "NOD Editor gebruiksaanwijzing",
            "nod_catalogus" or "nod_catalog" or "nod_catalogo" or "nod_cataleg" => "NOD Catalogus",
            _ => FriendlyTopicName(fileName)
        };
    }

    private ToolEditorHtmlViewState GetHtmlViewState(ToolEditorDocument document)
    {
        return ToolEditorHtmlViewApi.GetState(
            document.PackagePath,
            document.Editor.Text,
            document.ImageBytes is not null,
            document.HtmlEditMode,
            _webViewInitializationStarted,
            _previewPaneClosedByUser);
    }

    private static bool IsHtmlDocument(ToolEditorDocument document)
    {
        return ToolEditorHtmlViewApi.IsHtmlDocument(document.PackagePath, document.Editor.Text);
    }

    private bool MediaExists(string link)
    {
        return FindMediaDocument(link) is not null;
    }

    private ToolEditorDocument? FindMediaDocument(string link)
    {
        var normalized = NormalizeMediaLink(link);
        var fileName = Path.GetFileName(normalized);
        return _documents.FirstOrDefault(document =>
            document.ImageBytes is not null &&
            (document.PackagePath.Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
             Path.GetFileName(document.PackagePath).Equals(fileName, StringComparison.OrdinalIgnoreCase)));
    }

    private static string NormalizeMediaLink(string link)
    {
        var cleaned = link.Split(['?', '#'], 2)[0].Replace('\\', '/').Trim();
        while (cleaned.StartsWith("./", StringComparison.Ordinal))
            cleaned = cleaned[2..];
        while (cleaned.StartsWith("../", StringComparison.Ordinal))
            cleaned = cleaned[3..];
        return cleaned.Trim('/');
    }

    private void ScheduleSyntaxHighlight(ToolEditorDocument document)
    {
        if (!ReferenceEquals(_current, document) ||
            document.Highlighting ||
            document.Editor.IsDisposed ||
            document.Editor.TextLength > GetSyntaxHighlightLimit(document.PackagePath) ||
            !IsSyntaxHighlightedSource(document.PackagePath, out _))
        {
            return;
        }

        document.HighlightTimer ??= new System.Windows.Forms.Timer
        {
            Interval = 320
        };
        document.HighlightTimer.Stop();
        document.HighlightTimer.Tick -= HighlightTimer_Tick;
        document.HighlightTimer.Tick += HighlightTimer_Tick;
        document.HighlightTimer.Tag = document;
        document.HighlightTimer.Start();
    }

    private void HighlightTimer_Tick(object? sender, EventArgs e)
    {
        if (sender is not System.Windows.Forms.Timer timer)
            return;

        timer.Stop();
        if (timer.Tag is ToolEditorDocument document)
            ApplySyntaxHighlight(document);
    }

    private static void ApplySyntaxHighlight(ToolEditorDocument document)
    {
        var editor = document.Editor;
        if (editor.IsDisposed ||
            document.Highlighting ||
            document.ImageBytes is not null ||
            editor.TextLength > GetSyntaxHighlightLimit(document.PackagePath))
        {
            return;
        }

        if (!editor.IsHandleCreated)
            return;

        var range = GetSyntaxHighlightRange(editor);
        if (range.Length <= 0)
            return;

        var text = editor.Text;
        var visibleText = text.Substring(range.Start, Math.Min(range.Length, text.Length - range.Start));
        var version = BuildSyntaxHighlightVersion(document, range, visibleText);
        if (string.Equals(document.SyntaxHighlightVersion, version, StringComparison.Ordinal))
            return;

        document.Highlighting = true;
        try
        {
            ApplySyntaxHighlight(editor, document.PackagePath, range, visibleText);
            document.SyntaxHighlightVersion = version;
        }
        finally
        {
            document.Highlighting = false;
        }
    }

    private static void ApplySyntaxHighlight(RichTextBox editor, string packagePath)
    {
        if (editor.IsDisposed ||
            !editor.IsHandleCreated ||
            editor.TextLength > GetSyntaxHighlightLimit(packagePath) ||
            !IsSyntaxHighlightedSource(packagePath, out var extension))
        {
            return;
        }

        var range = GetSyntaxHighlightRange(editor);
        if (range.Length <= 0)
            return;

        var text = editor.Text;
        var visibleText = text.Substring(range.Start, Math.Min(range.Length, text.Length - range.Start));
        ApplySyntaxHighlight(editor, packagePath, range, visibleText, extension);
    }

    private static void ApplySyntaxHighlight(RichTextBox editor, string packagePath, SyntaxHighlightRange range, string visibleText)
    {
        if (!IsSyntaxHighlightedSource(packagePath, out var extension))
            return;

        ApplySyntaxHighlight(editor, packagePath, range, visibleText, extension);
    }

    private static void ApplySyntaxHighlight(RichTextBox editor, string packagePath, SyntaxHighlightRange range, string visibleText, string extension)
    {
        var selectionStart = editor.SelectionStart;
        var selectionLength = editor.SelectionLength;

        SetControlRedraw(editor, enabled: false);
        try
        {
            editor.SuspendLayout();
            editor.Select(range.Start, visibleText.Length);
            editor.SelectionColor = SyntaxDefaultColor;

            if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".htm", StringComparison.OrdinalIgnoreCase))
                HighlightHtml(editor, visibleText, range.Start);
            else if (extension.Equals(".css", StringComparison.OrdinalIgnoreCase))
                HighlightCss(editor, visibleText, range.Start);
            else if (extension.Equals(".lng", StringComparison.OrdinalIgnoreCase))
                HighlightLanguage(editor, visibleText, range.Start);
            else if (extension.Equals(".js", StringComparison.OrdinalIgnoreCase))
                HighlightJavaScript(editor, visibleText, range.Start);
            else if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
                HighlightJson(editor, visibleText, range.Start);

            editor.Select(
                Math.Min(selectionStart, editor.TextLength),
                Math.Min(selectionLength, Math.Max(0, editor.TextLength - selectionStart)));
        }
        finally
        {
            editor.ResumeLayout();
            SetControlRedraw(editor, enabled: true);
        }
    }

    private static SyntaxHighlightRange GetSyntaxHighlightRange(RichTextBox editor)
    {
        var textLength = editor.TextLength;
        if (textLength <= 0)
            return new SyntaxHighlightRange(0, 0);

        var bottomRight = new Point(Math.Max(0, editor.ClientSize.Width - 1), Math.Max(0, editor.ClientSize.Height - 1));
        var first = Math.Clamp(editor.GetCharIndexFromPosition(Point.Empty), 0, textLength);
        var last = Math.Clamp(editor.GetCharIndexFromPosition(bottomRight), first, textLength);
        var start = Math.Max(0, first - SyntaxHighlightViewportBufferChars);
        var end = Math.Min(textLength, Math.Max(last + SyntaxHighlightViewportBufferChars, start + SyntaxHighlightViewportBufferChars));
        return new SyntaxHighlightRange(start, end - start);
    }

    private static string BuildSyntaxHighlightVersion(ToolEditorDocument document, SyntaxHighlightRange range, string visibleText)
    {
        var editor = document.Editor;
        return document.PackagePath + "|" +
            editor.TextLength.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" +
            range.Start.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" +
            range.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" +
            visibleText.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static bool IsSyntaxHighlightedSource(string packagePath, out string extension)
    {
        extension = Path.GetExtension(packagePath);
        if (!LanguagePackagePolicy.Current.IsAllowedPackagePath(packagePath))
            return false;

        return extension.Equals(".lng", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".htm", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".css", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".js", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetSyntaxHighlightLimit(string packagePath)
    {
        return Path.GetExtension(packagePath).Equals(".lng", StringComparison.OrdinalIgnoreCase)
            ? MaxLanguageSyntaxHighlightChars
            : MaxSourceSyntaxHighlightChars;
    }

    private static void HighlightHtml(RichTextBox editor, string text, int offset)
    {
        ApplyMatches(editor, text, HtmlCommentRegex, SyntaxCommentColor, offset);
        ApplyMatches(editor, text, HtmlTagRegex, SyntaxKeywordColor, offset);
        ApplyMatches(editor, text, HtmlAttributeRegex, SyntaxAttributeColor, offset, groupIndex: 1);
        ApplyMatches(editor, text, QuotedStringRegex, SyntaxStringColor, offset);
    }

    private static void HighlightCss(RichTextBox editor, string text, int offset)
    {
        ApplyMatches(editor, text, CssSelectorRegex, SyntaxSelectorColor, offset, groupIndex: 2);
        ApplyMatches(editor, text, HtmlAttributeRegex, SyntaxAttributeColor, offset, groupIndex: 1);
        ApplyMatches(editor, text, QuotedStringRegex, SyntaxStringColor, offset);
    }

    private static void HighlightLanguage(RichTextBox editor, string text, int offset)
    {
        ApplyMatches(editor, text, LanguageKeyRegex, SyntaxKeywordColor, offset);
        ApplyMatches(editor, text, LanguageCommentRegex, SyntaxCommentColor, offset);
    }

    private static void HighlightJavaScript(RichTextBox editor, string text, int offset)
    {
        ApplyMatches(editor, text, QuotedStringRegex, SyntaxStringColor, offset);
        ApplyMatches(editor, text, JavaScriptKeywordRegex, SyntaxKeywordColor, offset);
        ApplyMatches(editor, text, JavaScriptCommentRegex, SyntaxCommentColor, offset);
    }

    private static void HighlightJson(RichTextBox editor, string text, int offset)
    {
        ApplyMatches(editor, text, QuotedStringRegex, SyntaxStringColor, offset);
        ApplyMatches(editor, text, JsonPropertyRegex, SyntaxKeywordColor, offset);
    }

    private static void SetControlRedraw(Control control, bool enabled)
    {
        if (!control.IsHandleCreated)
            return;

        SendMessage(control.Handle, WmSetRedraw, enabled ? 1 : 0, 0);
        if (enabled)
            control.Invalidate();
    }

    private static void ApplyMatches(RichTextBox editor, string text, Regex regex, Color color, int offset, int groupIndex = 0)
    {
        foreach (Match match in regex.Matches(text))
        {
            var group = match.Groups[groupIndex];
            if (!group.Success || group.Length == 0)
                continue;

            editor.Select(offset + group.Index, group.Length);
            editor.SelectionColor = color;
        }
    }

    private readonly record struct SyntaxHighlightRange(int Start, int Length);

    private static void ValidateJson(string text, List<string> errors)
    {
        try
        {
            using var json = JsonDocument.Parse(text);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                errors.Add("JSON root must be an object.");
                return;
            }

            RequireString(root, "producer", "Tiedragon", errors);
            RequireString(root, "product", "Syscalculator", errors);
            RequireString(root, "softwareId", "tiedragon.syscalculator", errors);
            RequireString(root, "languageCode", null, errors);
            RequireString(root, "displayName", null, errors);
        }
        catch (JsonException ex)
        {
            errors.Add("JSON is invalid: " + ex.Message);
        }
    }

    private static void RequireString(JsonElement root, string propertyName, string? requiredValue, List<string> errors)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
        {
            errors.Add(propertyName + " is required.");
            return;
        }

        if (requiredValue is not null && !requiredValue.Equals(value.GetString(), StringComparison.OrdinalIgnoreCase))
            errors.Add(propertyName + " must be " + requiredValue + ".");
    }

    private static void ValidateLanguageFile(string text, List<string> errors)
    {
        var lineNumber = 0;
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            lineNumber++;
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed.StartsWith(';'))
                continue;
            if (!trimmed.Contains('='))
                errors.Add("Line " + lineNumber + " is missing '='.");
        }
    }

    private void UpdatePreview(bool initializePreview = false)
    {
        if (!_webViewInitializationStarted && !initializePreview)
        {
            UpdatePreviewPaneState(_current);
            return;
        }

        if (_previewPaneClosedByUser && !initializePreview)
        {
            UpdatePreviewPaneState(_current);
            return;
        }

        if (_current is null)
        {
            SetHtml(WrapHtml("ToolEditor", "<p>No document selected.</p>"), initializePreview);
            return;
        }

        if (_current.HtmlEditMode)
            return;

        _pendingPreviewInitialize |= initializePreview;
        _previewRefreshTimer.Stop();
        _previewRefreshTimer.Start();
    }

    private async void PreviewRefreshTimer_Tick(object? sender, EventArgs e)
    {
        _previewRefreshTimer.Stop();
        var initializePreview = _pendingPreviewInitialize;
        _pendingPreviewInitialize = false;
        await RenderPreviewAsync(initializePreview);
    }

    private async Task RenderPreviewAsync(bool initializePreview)
    {
        if (!_webViewInitializationStarted && !initializePreview)
        {
            UpdatePreviewPaneState(_current);
            return;
        }

        if (_previewPaneClosedByUser && !initializePreview)
        {
            UpdatePreviewPaneState(_current);
            return;
        }

        if (_current is null)
        {
            SetHtml(WrapHtml("ToolEditor", "<p>No document selected.</p>"), initializePreview);
            return;
        }

        if (_current.HtmlEditMode)
            return;

        await SyncHtmlEditorToSourceAsync();
        SetHtml(BuildPreviewHtml(_current), initializePreview);
    }

    private string BuildPreviewHtml(ToolEditorDocument document)
    {
        UpdatePreviewConceptBannerState(document);
        var text = document.Editor.Text;
        return GetHtmlViewState(document).PreviewKind switch
        {
            ToolEditorPreviewKind.Image => WrapImageHtml(BuildImagePreview(document)),
            ToolEditorPreviewKind.Html => WrapContentHtml(ResolveMediaLinksForPreview(StripToolEditorConceptBanners(text))),
            ToolEditorPreviewKind.Language => WrapHtml(document.DisplayName, BuildLanguagePreview(text)),
            ToolEditorPreviewKind.Json => WrapHtml(document.DisplayName, BuildJsonPreview(text)),
            _ => WrapHtml(document.DisplayName, "<pre>" + WebUtility.HtmlEncode(text) + "</pre>")
        };
    }

    private string ResolveMediaLinksForPreview(string html)
    {
        return HtmlMediaLinkRegex.Replace(html, match =>
        {
            var attribute = match.Groups["attr"].Value;
            var quote = match.Groups["quote"].Value;
            var link = match.Groups["path"].Value;
            var media = FindMediaDocument(link);
            if (media?.ImageBytes is null)
                return match.Value;

            var dataUri = "data:" + ImageMimeType(media.PackagePath) + ";base64," + Convert.ToBase64String(media.ImageBytes);
            return attribute + "=" + quote + dataUri + quote;
        });
    }

    private void SetHtmlEditor(string html)
    {
        SetHtmlEditorHtml(BuildEditableHtml(ResolveMediaLinksForEditor(StripToolEditorConceptBanners(html))));
    }

    private void SetHtmlEditorHtml(string html)
    {
        _loadingHtmlEditor = true;
        _htmlEditorContentDirty = false;
        _pendingHtmlEditor = html;
        StartHtmlEditorInitialization();
        ShowPendingHtmlEditorIfReady();
    }

    private void ShowPendingHtmlEditorIfReady()
    {
        if (_htmlEditorFailed || IsDisposed || _htmlEditor.IsDisposed || _pendingHtmlEditor is null || _htmlEditor.CoreWebView2 is null)
            return;

        var html = _pendingHtmlEditor;
        _pendingHtmlEditor = null;
        try
        {
            _htmlEditor.NavigateToString(html);
        }
        catch (COMException)
        {
            _htmlEditorFailed = true;
        }
        catch (ObjectDisposedException)
        {
            _htmlEditorFailed = true;
        }
    }

    private string BuildEditableHtml(string body)
    {
        var css = HelpApi.NodHelpCss() + Environment.NewLine +
            ToolEditorConceptBannerCss() + Environment.NewLine +
            "body:focus { outline: 2px solid #9cc4ff; outline-offset: 4px; }";

        return ApplyToolEditorHelpPlaceholders(HelpHtml.WrapBodyPage(body, css, bodyTail: ToolEditorHtmlEditScript()));
    }

    private async Task SyncHtmlEditorToSourceAsync()
    {
        if (_current is null || !_current.HtmlEditMode || !_htmlEditorContentDirty || _loadingHtmlEditor || _htmlEditor.CoreWebView2 is null)
            return;

        try
        {
            await _htmlEditor.CoreWebView2.ExecuteScriptAsync("""
                for (const image of document.querySelectorAll('img[data-tool-src]')) {
                  image.setAttribute('src', image.getAttribute('data-tool-src'));
                  image.removeAttribute('data-tool-src');
                }
                """);
            var json = await _htmlEditor.CoreWebView2.ExecuteScriptAsync("document.body.innerHTML");
            var html = JsonSerializer.Deserialize<string>(json);
            if (html is null)
                return;

            var cleaned = StripToolEditorConceptBanners(Regex.Replace(html, @"\s*<script>[\s\S]*?</script>\s*$", "", RegexOptions.IgnoreCase).Trim());
            _current.Editor.Text = cleaned;
            _htmlEditorContentDirty = false;
        }
        catch (COMException)
        {
            _htmlEditorFailed = true;
        }
        catch (ObjectDisposedException)
        {
            _htmlEditorFailed = true;
        }
    }

    private string ResolveMediaLinksForEditor(string html)
    {
        return HtmlMediaLinkRegex.Replace(html, match =>
        {
            var attribute = match.Groups["attr"].Value;
            if (!attribute.Equals("src", StringComparison.OrdinalIgnoreCase))
                return match.Value;

            var quote = match.Groups["quote"].Value;
            var link = match.Groups["path"].Value;
            var media = FindMediaDocument(link);
            if (media?.ImageBytes is null)
                return match.Value;

            var dataUri = "data:" + ImageMimeType(media.PackagePath) + ";base64," + Convert.ToBase64String(media.ImageBytes);
            return attribute + "=" + quote + dataUri + quote + " data-tool-src=" + quote + WebUtility.HtmlEncode(link) + quote;
        });
    }

    private static string BuildImagePreview(ToolEditorDocument document)
    {
        if (document.ImageBytes is null)
            return "<p>No image loaded.</p>";

        var mime = ImageMimeType(document.PackagePath);
        var base64 = Convert.ToBase64String(document.ImageBytes);
        var fileName = WebUtility.HtmlEncode(Path.GetFileName(document.PackagePath));
        return $$"""
        <div class="image-preview">
          <img id="media-image" src="data:{{mime}};base64,{{base64}}" alt="{{fileName}}">
          <div class="image-tools" aria-label="Afbeelding zoom">
            <button type="button" data-zoom="in" title="Inzoomen">+</button>
            <button type="button" data-zoom="out" title="Uitzoomen">-</button>
            <button type="button" data-zoom="reset" title="Canvas 100%" aria-label="Canvas 100%"><span class="fit-icon"></span></button>
          </div>
        </div>
        """;
    }

    private static string ImageMimeType(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            _ => "image/png"
        };
    }

    private static string BuildJsonPreview(string text)
    {
        try
        {
            using var json = JsonDocument.Parse(text);
            var rows = new StringBuilder();
            foreach (var property in json.RootElement.EnumerateObject())
            {
                rows.Append("<tr><th>")
                    .Append(WebUtility.HtmlEncode(property.Name))
                    .Append("</th><td>")
                    .Append(WebUtility.HtmlEncode(property.Value.ToString()))
                    .Append("</td></tr>");
            }

            return "<table>" + rows + "</table>";
        }
        catch
        {
            return "<pre>" + WebUtility.HtmlEncode(text) + "</pre>";
        }
    }

    private static string BuildLanguagePreview(string text)
    {
        var rows = new StringBuilder();
        var count = 0;
        var skipped = 0;
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed.StartsWith(';'))
                continue;

            if (count >= MaxLanguagePreviewRows)
            {
                skipped++;
                continue;
            }

            var index = trimmed.IndexOf('=');
            var key = index < 0 ? trimmed : trimmed[..index];
            var value = index < 0 ? "" : trimmed[(index + 1)..];
            rows.Append("<tr><th>")
                .Append(WebUtility.HtmlEncode(key))
                .Append("</th><td>")
                .Append(WebUtility.HtmlEncode(value))
                .Append("</td></tr>");
            count++;
        }

        if (rows.Length == 0)
            return "<p>No language keys found.</p>";

        var notice = skipped > 0
            ? "<p class=\"help-info\">Preview toont de eerste " + MaxLanguagePreviewRows.ToString(System.Globalization.CultureInfo.InvariantCulture) +
              " sleutels. Er zijn nog " + skipped.ToString("N0") + " sleutels verborgen voor snelheid.</p>"
            : "";
        return notice + "<table>" + rows + "</table>";
    }

    private string WrapHtml(string title, string body)
    {
        var css = HelpApi.NodHelpCss() + Environment.NewLine + """
        .media-meta { color: #334155; margin-bottom: 14px; }
        .image-preview { min-height: 360px; border: 1px solid #d7e0ec; background: #f8fafc; display: flex; align-items: center; justify-content: center; padding: 18px; }
        .image-preview img { max-width: 100%; max-height: 70vh; object-fit: contain; box-shadow: 0 8px 24px rgba(15, 23, 42, .15); background: white; }
        """ + Environment.NewLine + ToolEditorConceptBannerCss();
        return ApplyToolEditorHelpPlaceholders(HelpHtml.WrapTopicPage(title, body, css, ToolEditorHelpPreviewScript()));
    }

    private string WrapContentHtml(string body)
    {
        var css = HelpApi.NodHelpCss() + Environment.NewLine + ToolEditorConceptBannerCss();
        return ApplyToolEditorHelpPlaceholders(HelpHtml.WrapBodyPage(body, css, bodyTail: ToolEditorHelpPreviewScript()));
    }

    private bool ShouldShowConceptBanner()
    {
        return !_conceptBannerDismissed;
    }

    private string CurrentConceptBannerLanguagePrefix()
    {
        try
        {
            using var json = JsonDocument.Parse(_manifestText);
            if (json.RootElement.TryGetProperty("languageCode", out var languageCode))
            {
                var value = languageCode.GetString();
                if (value is not null && (value.Equals("ned", StringComparison.OrdinalIgnoreCase) || value.Equals("nl", StringComparison.OrdinalIgnoreCase)))
                    return "nl";
            }
        }
        catch (JsonException)
        {
            // Invalid concept manifests still get the default English draft warning.
        }

        return "en";
    }

    private static string ToolEditorConceptBannerCss()
    {
        return """
        html, body { overflow-x: hidden; }
        .concept-banner { position: relative; display: flex; align-items: center; justify-content: space-between; gap: 14px; box-sizing: border-box; width: auto; max-width: none !important; min-height: 56px; margin: -24px -44px 22px -28px; padding: 12px 22px 10px 28px; border: 0; border-bottom: 1px solid #e0b800; border-radius: 0; background: #ffd400; color: #1f2937; box-shadow: none; overflow: hidden; }
        .concept-banner::before { content: ""; position: absolute; left: 0; top: 0; right: 0; height: 1px; background: #f3c600; }
        .concept-banner span { color: #1f2937; }
        .concept-banner-icon { flex: 0 0 auto; width: 34px; height: 34px; border-radius: 50%; display: inline-flex; align-items: center; justify-content: center; background: #fff9d6; color: #111827; border: 1px solid #d6a318; }
        .concept-banner-icon svg { width: 25px; height: 25px; display: block; }
        .concept-banner b { color: #7a4a00; }
        .concept-banner-close { flex: 0 0 auto; width: 23px; height: 23px; border: 1px solid #dbe3ee; border-radius: 50%; background: #ffffff; color: #334155; font: 700 13px/19px "Segoe UI", Arial, sans-serif; cursor: pointer; }
        .concept-banner-close:hover { background: #e2e8f0; border-color: #94a3b8; }
        """;
    }

    private static string ToolEditorHtmlEditScript()
    {
        return """
        <script>
        document.body.contentEditable = 'true';
        document.body.tabIndex = 0;
        window.toolEditorFocusBody = () => {
          document.body.focus({ preventScroll: true });
          const selection = window.getSelection();
          if (!selection || selection.rangeCount > 0) return;
          const range = document.createRange();
          range.selectNodeContents(document.body);
          range.collapse(true);
          selection.removeAllRanges();
          selection.addRange(range);
        };
        document.addEventListener('click', event => {
          const close = event.target && event.target.closest ? event.target.closest('.concept-banner-close') : null;
          if (!close) return;
          event.preventDefault();
          close.closest('.concept-banner')?.remove();
          window.chrome.webview.postMessage('tooleditor:concept-dismissed');
        });
        document.body.addEventListener('click', event => {
          const link = event.target && event.target.closest ? event.target.closest('a[href]') : null;
          if (link) event.preventDefault();
        });
        document.body.addEventListener('contextmenu', event => {
          const link = event.target && event.target.closest ? event.target.closest('a[href]') : null;
          if (!link) return;
          event.preventDefault();
          window.chrome.webview.postMessage('tooleditor:goto:' + link.getAttribute('href'));
        });
        document.body.addEventListener('input', () => window.chrome.webview.postMessage('changed'));
        window.toolEditorFocusBody();
        </script>
        """;
    }

    private static string ToolEditorHelpPreviewScript()
    {
        return HelpHtml.NodCopyButtonsScript() + """
        <script>
        document.addEventListener('click', event => {
          const close = event.target && event.target.closest ? event.target.closest('.concept-banner-close') : null;
          if (!close) return;
          event.preventDefault();
          close.closest('.concept-banner')?.remove();
          window.chrome.webview.postMessage('tooleditor:concept-dismissed');
        });
        document.addEventListener('click', event => {
          const link = event.target && event.target.closest ? event.target.closest('a[href]') : null;
          if (!link) return;
          const href = link.getAttribute('href') || '';
          if (!href || href.startsWith('#') || href.startsWith('http:') || href.startsWith('https:') || href.startsWith('mailto:')) return;
          event.preventDefault();
          window.chrome.webview.postMessage('tooleditor:goto:' + href);
        });
        </script>
        """;
    }

    private string ApplyToolEditorHelpPlaceholders(string html)
    {
        var language = ReadCurrentPackageLanguage();
        var texts = LoadToolEditorHelpTexts(language);
        string? ResolveText(string key) => texts.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : key switch
            {
                "menu.edit.copy" => "Kopieren",
                "help.copy.copied" => "Gekopieerd",
                _ => null
            };

        var languageCode = language.Code;
        var prepared = HeadingPlaceholderRegex.Replace(html, match =>
        {
            var placeholder = match.Groups["placeholder"].Value;
            var key = placeholder[1..^1];
            var replacement = ResolveText(key);
            return IsBlockHtml(replacement) ? replacement! : match.Value;
        });
        var expanded = HelpApi.ApplyLanguagePlaceholders(ResolveText, prepared);
        return HelpHtml.ApplyContentPlaceholders(expanded, new Dictionary<string, string?>
        {
            ["WarningBoardSvg"] = BuildPackageInlineSvg("assets/warning_board.svg") ?? WarningBoardSvg(),
            ["NodEditorScreenshot"] = BuildPackageScreenshotImage(
                "assets/NodEditorHelp.svg",
                ResolveText("help.main.page.nodeditor.screenshot_alt") ?? "Screenshot of the NOD Editor with toolbar, code editor and command tip.") ?? HelpApi.ScreenshotImage(
                languageCode,
                ResolveText,
                "NodEditorHelp.svg",
                ResolveText("help.main.page.nodeditor.screenshot_alt") ?? "Screenshot of the NOD Editor with toolbar, code editor and command tip."),
            ["WizardExpressScreenshot"] = BuildPackageScreenshotImage(
                "assets/WizardExpressHelp.png",
                ResolveText("help.main.page.wizard.screenshot_alt") ?? "Screenshot of WizardExpress in front of Syscalculator and a spreadsheet.") ?? HelpApi.ScreenshotImage(
                languageCode,
                ResolveText,
                "WizardExpressHelp.png",
                ResolveText("help.main.page.wizard.screenshot_alt") ?? "Screenshot of WizardExpress in front of Syscalculator and a spreadsheet.")
        });
    }

    private static bool IsBlockHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return false;

        var trimmed = html.TrimStart();
        return trimmed.StartsWith("<h", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<p", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<div", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<table", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<details", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<ul", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<ol", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<pre", StringComparison.OrdinalIgnoreCase);
    }

    private string? BuildPackageInlineSvg(string packagePath)
    {
        var media = FindMediaDocument(packagePath);
        if (media?.ImageBytes is null)
            return null;

        try
        {
            return Encoding.UTF8.GetString(media.ImageBytes);
        }
        catch
        {
            return null;
        }
    }

    private string? BuildPackageScreenshotImage(string packagePath, string altText)
    {
        var media = FindMediaDocument(packagePath);
        if (media?.ImageBytes is null)
            return null;

        var mime = ImageMimeType(media.PackagePath);
        var base64 = Convert.ToBase64String(media.ImageBytes);
        return "<div class=\"screenshot-frame\"><img src=\"data:" +
            mime +
            ";base64," +
            base64 +
            "\" alt=\"" +
            WebUtility.HtmlEncode(altText) +
            "\" /></div>";
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

    private ToolEditorLanguageInfo ReadCurrentPackageLanguage()
    {
        try
        {
            using var json = JsonDocument.Parse(_manifestText);
            var root = json.RootElement;
            var code = NormalizeLanguageCode(GetManifestString(root, "languageCode"));
            if (string.IsNullOrWhiteSpace(code))
                return GetConfiguredToolEditorLanguage();

            var key = GetManifestString(root, "key");
            if (string.IsNullOrWhiteSpace(key))
                key = GetManifestString(root, "id");
            var displayName = GetManifestString(root, "displayName");
            var nativeName = GetManifestString(root, "nativeName");
            return new ToolEditorLanguageInfo(
                code,
                string.IsNullOrWhiteSpace(displayName) ? LanguageDisplayName(code) : displayName,
                string.IsNullOrWhiteSpace(nativeName) ? LanguageNativeName(code) : nativeName,
                string.IsNullOrWhiteSpace(key) ? null : key,
                null);
        }
        catch (JsonException)
        {
            return GetConfiguredToolEditorLanguage();
        }
    }

    private static string WrapImageHtml(string body)
    {
        return $$"""
        <!doctype html>
        <html>
        <head>
          <meta charset="utf-8">
          <style>
            html, body { width: 100%; height: 100%; margin: 0; overflow: hidden; }
            body { box-sizing: border-box; display: flex; font-family: "Segoe UI", Arial, sans-serif; font-size: 14px; color: #1f2937; background: #fff; }
            .image-preview { position: relative; flex: 1 1 auto; min-height: 0; width: 100%; box-sizing: border-box; background: #f8fafc; display: flex; align-items: center; justify-content: center; padding: 0; overflow: hidden; cursor: grab; user-select: none; }
            .image-preview.dragging { cursor: grabbing; }
            .image-preview img { max-width: 100%; max-height: 100%; object-fit: contain; transform-origin: center center; box-shadow: 0 8px 24px rgba(15, 23, 42, .15); background: white; pointer-events: none; }
            .image-tools { position: fixed; right: 18px; top: 50%; transform: translateY(-50%); display: flex; flex-direction: column; gap: 8px; padding: 6px; border: 1px solid #cfe0f5; border-radius: 18px; background: rgba(255, 255, 255, .92); box-shadow: 0 8px 18px rgba(15, 23, 42, .14); }
            .image-tools button { width: 38px; height: 34px; border: 1px solid #cfe0f5; border-radius: 14px; background: #fff; color: #123f73; font: 700 15px "Segoe UI", Arial, sans-serif; cursor: pointer; }
            .image-tools button:hover { background: #edf6ff; border-color: #8abcf4; }
            .fit-icon { display: inline-block; width: 15px; height: 15px; border: 2px solid currentColor; border-radius: 3px; box-sizing: border-box; vertical-align: middle; }
          </style>
        </head>
        <body>
          {{body}}
          <script>
            let zoom = 1;
            let panX = 0;
            let panY = 0;
            let dragging = false;
            let dragX = 0;
            let dragY = 0;
            const preview = document.querySelector('.image-preview');
            const image = document.getElementById('media-image');
            const applyZoom = () => {
              image.style.transform = `translate(${panX}px, ${panY}px) scale(${zoom})`;
              image.style.maxWidth = zoom === 1 ? '100%' : 'none';
              image.style.maxHeight = zoom === 1 ? '100%' : 'none';
            };
            document.querySelector('.image-tools').addEventListener('click', event => {
              const action = event.target && event.target.dataset ? event.target.dataset.zoom : '';
              if (action === 'in') zoom = Math.min(zoom * 1.2, 8);
              if (action === 'out') zoom = Math.max(zoom / 1.2, .2);
              if (action === 'reset') {
                zoom = 1;
                panX = 0;
                panY = 0;
              }
              applyZoom();
            });
            preview.addEventListener('pointerdown', event => {
              if (event.target.closest('.image-tools')) return;
              dragging = true;
              dragX = event.clientX - panX;
              dragY = event.clientY - panY;
              preview.classList.add('dragging');
              preview.setPointerCapture(event.pointerId);
            });
            preview.addEventListener('pointermove', event => {
              if (!dragging) return;
              panX = event.clientX - dragX;
              panY = event.clientY - dragY;
              applyZoom();
            });
            const stopDrag = event => {
              dragging = false;
              preview.classList.remove('dragging');
              if (preview.hasPointerCapture(event.pointerId))
                preview.releasePointerCapture(event.pointerId);
            };
            preview.addEventListener('pointerup', stopDrag);
            preview.addEventListener('pointercancel', stopDrag);
          </script>
        </body>
        </html>
        """;
    }

    private async Task InitializeBrowserAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _preview.EnsureCoreWebView2Async();
            ShowPendingHtmlIfReady();
            ToolEditorDebugger.Log("Preview WebView2 initialized in " + stopwatch.ElapsedMilliseconds.ToString("N0") + " ms.");
        }
        catch (COMException)
        {
            _browserFailed = true;
            ToolEditorDebugger.Log("Preview WebView2 COM initialization failed after " + stopwatch.ElapsedMilliseconds.ToString("N0") + " ms.");
        }
        catch (ObjectDisposedException)
        {
            _browserFailed = true;
            ToolEditorDebugger.Log("Preview WebView2 disposed during initialization after " + stopwatch.ElapsedMilliseconds.ToString("N0") + " ms.");
        }
        catch (Exception ex)
        {
            _browserFailed = true;
            ToolEditorDebugger.ReportException("Preview WebView2 initialization failed", ex, showDialog: false);
            _preview.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "WebView2 could not start.\r\n" + ex.Message
            });
        }
    }

    private async Task InitializeHtmlEditorAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _htmlEditor.EnsureCoreWebView2Async();
            ShowPendingHtmlEditorIfReady();
            ToolEditorDebugger.Log("HTML editor WebView2 initialized in " + stopwatch.ElapsedMilliseconds.ToString("N0") + " ms.");
        }
        catch (COMException)
        {
            _htmlEditorFailed = true;
            ToolEditorDebugger.Log("HTML editor WebView2 COM initialization failed after " + stopwatch.ElapsedMilliseconds.ToString("N0") + " ms.");
        }
        catch (ObjectDisposedException)
        {
            _htmlEditorFailed = true;
            ToolEditorDebugger.Log("HTML editor WebView2 disposed during initialization after " + stopwatch.ElapsedMilliseconds.ToString("N0") + " ms.");
        }
        catch (Exception ex)
        {
            _htmlEditorFailed = true;
            ToolEditorDebugger.ReportException("HTML editor WebView2 initialization failed", ex, showDialog: false);
            _htmlEditor.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "HTML editor could not start.\r\n" + ex.Message
            });
        }
    }

    private void SetHtml(string html, bool initializePreview = false)
    {
        _pendingHtml = html;
        if (initializePreview)
            StartWebViewInitialization();

        UpdatePreviewPaneState(_current);
        ShowPendingHtmlIfReady();
    }

    private void ShowPendingHtmlIfReady()
    {
        if (_browserFailed || IsDisposed || _preview.IsDisposed || _pendingHtml is null || _preview.CoreWebView2 is null)
            return;

        var html = _pendingHtml;
        _pendingHtml = null;
        try
        {
            _preview.NavigateToString(html);
        }
        catch (COMException)
        {
            _browserFailed = true;
        }
        catch (ObjectDisposedException)
        {
            _browserFailed = true;
        }
    }

    private void HandleHtmlEditorMessage(CoreWebView2WebMessageReceivedEventArgs args)
    {
        var message = GetWebMessage(args);
        if (message is null)
            return;

        if (message.StartsWith("tooleditor:goto:", StringComparison.Ordinal))
        {
            NavigateToPackageLink(message["tooleditor:goto:".Length..], preferPreview: false);
            return;
        }

        if (message.Equals("tooleditor:concept-dismissed", StringComparison.Ordinal))
        {
            DismissConceptBanner();
            return;
        }

        if (message.Equals("changed", StringComparison.Ordinal) && _current is not null && _current.HtmlEditMode)
        {
            _htmlEditorContentDirty = true;
            SetDirty(_current, true);
        }
    }

    private void HandlePreviewMessage(CoreWebView2WebMessageReceivedEventArgs args)
    {
        var message = GetWebMessage(args);
        if (message is null)
            return;

        if (message.StartsWith("tooleditor:goto:", StringComparison.Ordinal))
        {
            NavigateToPackageLink(message["tooleditor:goto:".Length..], preferPreview: true);
            return;
        }

        if (message.Equals("tooleditor:concept-dismissed", StringComparison.Ordinal))
        {
            DismissConceptBanner();
            return;
        }

        CopyHelpMessageToClipboard(message);
    }

    private void DismissConceptBanner()
    {
        _conceptBannerDismissed = true;
        UpdatePreviewConceptBannerState(_current);
        UpdateEditConceptBannerState(_current);
        RemoveConceptBannersFromBrowsers();
    }

    private void RemoveConceptBannersFromBrowsers()
    {
        const string script = "document.querySelectorAll('.concept-banner').forEach(element => element.remove());";
        _ = _preview.CoreWebView2?.ExecuteScriptAsync(script);
        _ = _htmlEditor.CoreWebView2?.ExecuteScriptAsync(script);
    }

    private static string? GetWebMessage(CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            return args.TryGetWebMessageAsString();
        }
        catch
        {
            return null;
        }
    }

    private void NavigateToPackageLink(string link, bool preferPreview)
    {
        if (TryResolvePackageLink(link, _current, out var target))
        {
            if (preferPreview && target.ImageBytes is null && IsHtmlDocument(target))
                target.HtmlEditMode = false;

            SelectDocument(target);
            SetStatus(TToolEditor("tool_editor.status.link_target", "Link: ") + link + " -> " + target.PackagePath, isError: false);
            return;
        }

        SetStatus(TToolEditor("tool_editor.status.link_target_missing", "Link target not found: ") + link, isError: true);
    }

    private bool TryResolvePackageLink(string link, ToolEditorDocument? source, out ToolEditorDocument target)
    {
        foreach (var candidate in BuildPackageLinkCandidates(link, source))
        {
            var normalized = NormalizePackagePath(candidate);
            var exact = _documents.FirstOrDefault(document => document.PackagePath.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
            {
                target = exact;
                return true;
            }
        }

        target = null!;
        return false;
    }

    private static IEnumerable<string> BuildPackageLinkCandidates(string link, ToolEditorDocument? source)
    {
        var cleaned = NormalizeMediaLink(WebUtility.HtmlDecode(link));
        if (string.IsNullOrWhiteSpace(cleaned) || cleaned.StartsWith("#", StringComparison.Ordinal))
            yield break;

        if (cleaned.StartsWith("nodpage:", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var candidate in BuildNodPageLinkCandidates(cleaned))
                yield return candidate;
            yield break;
        }

        if (cleaned.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            if (cleaned.Contains('/', StringComparison.Ordinal))
                yield return cleaned;

            if (source is not null)
            {
                var sourceDirectory = GetPackageDirectory(source.PackagePath);
                if (!string.IsNullOrWhiteSpace(sourceDirectory))
                    yield return NormalizePackagePath(sourceDirectory + "/" + cleaned);
            }

            yield return "manual/" + Path.GetFileName(cleaned);
            yield return "formula/" + cleaned;
            yield return "help/content/nod/full/" + Path.GetFileName(cleaned);
        }
    }

    private static IEnumerable<string> BuildNodPageLinkCandidates(string link)
    {
        var key = link["nodpage:".Length..].Trim().Trim('/');
        if (key.StartsWith("cmd:", StringComparison.OrdinalIgnoreCase))
        {
            var command = key["cmd:".Length..].Trim().ToLowerInvariant();
            yield return "help/content/nod/popup/" + command + ".html";
            yield return "help/content/nod/command/" + command + ".html";

            if (IsAdvancedMathCommand(command))
            {
                yield return "help/content/nod/command/math-advanced.html";
                yield return "help/content/nod/command/math-extra.html";
                yield return "help/content/nod/popup/math.html";
            }

            yield break;
        }

        yield return "help/content/nod/full/" + key + ".html";
        yield return "help/content/nod/command/" + key + ".html";
        yield return "help/content/nod/popup/" + key + ".html";
        yield return "help/content/nod/snippet/" + key + ".html";
        yield return "help/content/main/" + key + ".html";
        yield return "manual/" + key + ".html";
        foreach (var localized in BuildLocalizedMainHelpCandidates(key))
            yield return localized;
    }

    private static IEnumerable<string> BuildLocalizedMainHelpCandidates(string key)
    {
        switch (key.ToLowerInvariant())
        {
            case "main":
                yield return "manual/index.html";
                yield return "manual/converteren.html";
                yield return "manual/convert.html";
                break;
            case "fields":
                yield return "manual/velden.html";
                yield return "manual/fields.html";
                break;
            case "wizard":
                yield return "manual/wizardexpress.html";
                yield return "manual/wizard.html";
                break;
            case "calculator":
                yield return "manual/rekenmachine.html";
                yield return "manual/calculator.html";
                break;
            case "configuration":
                yield return "manual/configuratie.html";
                yield return "manual/configuration.html";
                break;
            case "window":
                yield return "manual/venster-opties.html";
                yield return "manual/window-options.html";
                break;
            case "nodfiles":
                yield return "manual/nod-catalogus.html";
                yield return "manual/nod-catalog.html";
                break;
            case "support":
                yield return "manual/support.html";
                break;
        }
    }

    private static bool IsAdvancedMathCommand(string command)
    {
        return command is "length" or "norm" or "mag" or "vec" or "distance" or "dot" or "angle" or "angled" or "cross" or "det" or "trace" or "mget" or "x" or "y" or "z";
    }

    private static string GetPackageDirectory(string packagePath)
    {
        var normalized = NormalizePackagePath(packagePath);
        var index = normalized.LastIndexOf('/');
        return index < 0 ? "" : normalized[..index];
    }

    private static void CopyHelpMessageToClipboard(string text)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(text))
                Clipboard.SetText(text);
        }
        catch
        {
            // Help-copy is auxiliary; the preview should keep working if clipboard access fails.
        }
    }

    private static string GetWebView2UserDataFolder(string name)
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var path = Path.Combine(root, "Syscalculator", "ToolEditor", "WebView2", name);
        Directory.CreateDirectory(path);
        return path;
    }

    private void SetDirty(ToolEditorDocument document, bool dirty)
    {
        if (document.Dirty == dirty)
            return;

        document.Dirty = dirty;
        RefreshTabStrip();
        UpdateUiState();
    }

    private void RefreshTabStrip()
    {
        foreach (var document in _documents.Where(document => document.IsOpen))
        {
            var title = BuildTabTitle(document) + (document.Dirty ? " *" : "");
            ToolEditorTabsApi.SetHeaderState(document.HeaderPanel, document.HeaderTitle, title, ReferenceEquals(document, _current), document.Dirty, Font);
        }

        _tabStrip.Invalidate();
    }

    private static string BuildTabTitle(ToolEditorDocument document)
    {
        if (document.PackagePath.StartsWith("language/", StringComparison.OrdinalIgnoreCase))
            return "Vertaling";

        return string.IsNullOrWhiteSpace(document.TreeTopic)
            ? FriendlyTopicName(Path.GetFileName(document.PackagePath))
            : document.TreeTopic;
    }

    private void UpdateUiState()
    {
        var hasDocument = _current is not null;
        _saveButton.Enabled = hasDocument;
        _validateButton.Enabled = hasDocument;
        UpdateHtmlToolbarState(_current);
    }

    private void SetStatus(string text, bool isError)
    {
        _statusLabel.Text = text;
        _statusLabel.ForeColor = isError ? Color.FromArgb(170, 35, 35) : Color.FromArgb(31, 41, 55);
        UpdateStatusMetrics(_current);
    }

    private void UpdateStatusMetrics(ToolEditorDocument? document)
    {
        if (_statusInfoLabel.IsDisposed)
            return;

        var documentCount = _documents.Count;
        var openCount = _documents.Count(item => item.IsOpen);
        var mediaCount = _documents.Count(item => item.ImageBytes is not null);
        if (document is null)
        {
            _statusInfoLabel.Text = TToolEditor("tool_editor.status.pages", "Pages") + " " + documentCount.ToString("N0") +
                "   " + TToolEditor("tool_editor.status.tabs", "Tabs") + " " + openCount.ToString("N0") +
                "   " + TToolEditor("tool_editor.status.media", "Media") + " " + mediaCount.ToString("N0");
            return;
        }

        var index = Math.Max(1, _documents.IndexOf(document) + 1);
        if (document.ImageBytes is not null)
        {
            _statusInfoLabel.Text = TToolEditor("tool_editor.status.page", "Page") + " " + index.ToString("N0") + "/" + documentCount.ToString("N0") +
                "   " + TToolEditor("tool_editor.status.media", "Media") + " " + mediaCount.ToString("N0") +
                "   " + TToolEditor("tool_editor.status.size", "Size") + " " + document.ImageBytes.Length.ToString("N0") + " bytes";
            return;
        }

        var lineCount = Math.Max(1, document.Editor.Lines.Length);
        _statusInfoLabel.Text = TToolEditor("tool_editor.status.lines", "Lines") + " " + lineCount.ToString("N0") +
            "   " + TToolEditor("tool_editor.status.characters", "Characters") + " " + document.Editor.TextLength.ToString("N0") +
            "   " + TToolEditor("tool_editor.status.page", "Page") + " " + index.ToString("N0") + "/" + documentCount.ToString("N0") +
            "   " + TToolEditor("tool_editor.status.tabs", "Tabs") + " " + openCount.ToString("N0");
    }

    private static string[] GetDroppedFiles(DragEventArgs e)
    {
        return e.Data?.GetData(DataFormats.FileDrop) as string[] ?? [];
    }

    private void ToolEditorForm_DragEnter(object? sender, DragEventArgs e)
    {
        var files = GetDroppedFiles(e);
        e.Effect = files.Any(IsImagePath) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void ToolEditorForm_DragDrop(object? sender, DragEventArgs e)
    {
        ImportMediaFiles(GetDroppedFiles(e));
    }

    private void MediaList_DragDrop(object? sender, DragEventArgs e)
    {
        ImportMediaFiles(GetDroppedFiles(e), selectImported: false);
        SelectMediaManager();
    }

    private static void ClampSplitter(SplitContainer split, int? preferredDistance = null)
    {
        var length = split.Orientation == Orientation.Horizontal ? split.Height : split.Width;
        if (length <= split.SplitterWidth + 2)
            return;

        var min = Math.Max(1, split.Panel1MinSize);
        var max = length - split.SplitterWidth - Math.Max(1, split.Panel2MinSize);
        if (max < min)
            return;

        var target = Math.Clamp(preferredDistance ?? length / 2, min, max);
        try
        {
            if (split.SplitterDistance != target)
                split.SplitterDistance = target;
        }
        catch (InvalidOperationException)
        {
            // WinForms can resize split containers before the final client size is stable.
        }
    }

    private static void ClampPreviewSplitter(SplitContainer split, int preferredPreviewSize)
    {
        var length = split.Orientation == Orientation.Horizontal ? split.Height : split.Width;
        if (length <= split.SplitterWidth + 2)
            return;

        var min = Math.Max(1, split.Panel1MinSize);
        var max = length - split.SplitterWidth - Math.Max(1, split.Panel2MinSize);
        if (max < min)
            return;

        var target = Math.Clamp(length - split.SplitterWidth - preferredPreviewSize, min, max);
        try
        {
            if (split.SplitterDistance != target)
                split.SplitterDistance = target;
        }
        catch (InvalidOperationException)
        {
            // WinForms can resize split containers before the final client size is stable.
        }
    }

    private void ToolEditorForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.S)
        {
            SaveCurrent();
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.F5)
        {
            ShowPreviewPane();
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Delete && !IsEditorEditingText())
        {
            DeleteCurrentDocument();
            e.SuppressKeyPress = true;
        }
    }

    private bool IsEditorEditingText()
    {
        return _current?.Editor.Focused == true && _current.ImageBytes is null;
    }

    private enum ToolEditorOpenKind
    {
        SingleFile,
        PackageSourceFolder,
        ObjectPackage,
        PackageArchive
    }

    private sealed record ToolEditorOpenRequest(
        ToolEditorOpenKind Kind,
        string Path,
        bool ResetPackage,
        bool SelectPreferredDocument,
        bool ShowManifestDialog,
        string SuccessStatusKey,
        string SuccessStatusFallback,
        string ErrorStatusKey,
        string ErrorStatusFallback,
        string DialogTitleKey,
        string DialogTitleFallback)
    {
        public static ToolEditorOpenRequest SingleFile(string path)
        {
            var isManifest = System.IO.Path.GetFileName(path).Equals("manifest.json", StringComparison.OrdinalIgnoreCase);
            return new ToolEditorOpenRequest(
                ToolEditorOpenKind.SingleFile,
                path,
                ResetPackage: false,
                SelectPreferredDocument: false,
                ShowManifestDialog: isManifest,
                isManifest ? "tool_editor.status.manifest_loaded" : "tool_editor.status.file_opened",
                isManifest ? "Manifest loaded: " : "File opened: ",
                "tool_editor.status.open_file_failed",
                "Open file failed: ",
                "tool_editor.dialog.open_file.title",
                "Open file");
        }

        public static ToolEditorOpenRequest PackageSourceFolder(string path)
        {
            return new ToolEditorOpenRequest(
                ToolEditorOpenKind.PackageSourceFolder,
                path,
                ResetPackage: true,
                SelectPreferredDocument: true,
                ShowManifestDialog: false,
                "tool_editor.status.concept_package_opened",
                "Concept language package opened: ",
                "tool_editor.status.open_package_failed",
                "Open language package failed: ",
                "tool_editor.dialog.open_package.title",
                "Open language package");
        }

        public static ToolEditorOpenRequest ObjectPackage(string path)
        {
            return new ToolEditorOpenRequest(
                ToolEditorOpenKind.ObjectPackage,
                path,
                ResetPackage: true,
                SelectPreferredDocument: true,
                ShowManifestDialog: false,
                "tool_editor.status.concept_package_opened",
                "Concept language package opened: ",
                "tool_editor.status.open_concept_failed",
                "Open concept language package failed: ",
                "tool_editor.dialog.open_concept.title",
                "Open concept language package");
        }

        public static ToolEditorOpenRequest PackageArchive(string path)
        {
            return new ToolEditorOpenRequest(
                ToolEditorOpenKind.PackageArchive,
                path,
                ResetPackage: true,
                SelectPreferredDocument: true,
                ShowManifestDialog: false,
                "tool_editor.status.language_package_opened",
                "Language package opened: ",
                "tool_editor.status.open_package_failed",
                "Open language package failed: ",
                "tool_editor.dialog.open_package.title",
                "Open language package");
        }
    }

    private sealed class PreparedPackageSourceFolder(string folder, string? temporaryFolder) : IDisposable
    {
        public string Folder { get; } = folder;

        public void Dispose()
        {
            if (string.IsNullOrWhiteSpace(temporaryFolder))
                return;

            try
            {
                if (Directory.Exists(temporaryFolder))
                    Directory.Delete(temporaryFolder, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private sealed record LocalizedFormulaCard(
        FormulaCard Source,
        string Title,
        string FormulaText,
        string PlainText,
        IReadOnlyList<string> Tags,
        string Description,
        string ExampleNod);

    private sealed class ToolEditorDocument(TabPage page, string displayName, string packagePath, string? filePath)
    {
        public TabPage Page { get; } = page;
        public string DisplayName { get; set; } = displayName;
        public string PackagePath { get; set; } = packagePath;
        public string TreeGroup { get; set; } = "";
        public string TreeTopic { get; set; } = "";
        public string? FilePath { get; set; } = filePath;
        public DateTimeOffset? LastModified { get; set; }
        public RichTextBox Editor { get; set; } = null!;
        public byte[]? ImageBytes { get; set; }
        public LineNumberPanel? LineNumbers { get; set; }
        public Panel HeaderPanel { get; set; } = null!;
        public Label HeaderTitle { get; set; } = null!;
        public System.Windows.Forms.Timer? HighlightTimer { get; set; }
        public Func<string>? LazyTextFactory { get; set; }
        public bool Highlighting { get; set; }
        public string? SyntaxHighlightVersion { get; set; }
        public bool LazyTextLoaded { get; set; } = true;
        public bool Dirty { get; set; }
        public bool ReadOnly { get; set; }
        public bool IsOpen { get; set; }
        public bool HtmlEditMode { get; set; }
    }

    private sealed record ToolEditorLanguageInfo(
        string Code,
        string DisplayName,
        string NativeName,
        string? PackageKey,
        string? PackagePath);

    private sealed class NavigationRefreshScope : IDisposable
    {
        private readonly ToolEditorForm _owner;
        private bool _disposed;

        public NavigationRefreshScope(ToolEditorForm owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _owner._suspendNavigationRefresh = Math.Max(0, _owner._suspendNavigationRefresh - 1);
        }
    }

    private sealed record ManifestProperties(
        string Id,
        string Producer,
        string Product,
        string SoftwareId,
        string LanguageCode,
        string DisplayName,
        string NativeName,
        string PackageVersion,
        string FallbackLanguage);

    private sealed record LanguagePackageContainerHeader(
        int Format,
        string SoftwareId,
        string PackageType,
        string PayloadFormat,
        string PayloadSha256,
        bool Encrypted,
        bool Signed,
        string SignatureAlgorithm = "",
        string SignatureKeyId = "",
        string Signature = "");

    private sealed record LanguagePackageCompileResult(
        string OutputPath,
        string PackageSha256,
        string PayloadSha256);

    private sealed class LineNumberPanel : Panel
    {
        private const int MinPanelWidth = 34;
        private const int HorizontalPadding = 8;
        private RichTextBox? _editor;
        private int _lineCount = 1;

        public LineNumberPanel()
        {
            Dock = DockStyle.Left;
            Width = MinPanelWidth;
            BackColor = Color.FromArgb(248, 250, 252);
            ForeColor = Color.FromArgb(100, 116, 139);
            DoubleBuffered = true;
        }

        public void Attach(RichTextBox editor)
        {
            _editor = editor;
            RefreshMetrics();
            Invalidate();
        }

        public void RefreshMetrics()
        {
            if (_editor is null || _editor.IsDisposed)
                return;

            var lineCount = Math.Max(1, _editor.GetLineFromCharIndex(_editor.TextLength) + 1);
            if (_lineCount == lineCount)
            {
                Invalidate();
                return;
            }

            _lineCount = lineCount;
            UpdateWidth();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_editor is null || _editor.IsDisposed)
                return;

            using var border = new Pen(Color.FromArgb(226, 232, 240));
            e.Graphics.DrawLine(border, Width - 1, 0, Width - 1, Height);

            using var brush = new SolidBrush(ForeColor);
            var firstIndex = _editor.GetCharIndexFromPosition(new Point(0, 0));
            var firstLine = _editor.GetLineFromCharIndex(firstIndex);
            var lastIndex = _editor.GetCharIndexFromPosition(new Point(0, _editor.ClientSize.Height));
            var lastLine = Math.Min(_lineCount - 1, _editor.GetLineFromCharIndex(lastIndex) + 1);

            for (var line = firstLine; line <= lastLine; line++)
            {
                var charIndex = _editor.GetFirstCharIndexFromLine(line);
                if (charIndex < 0)
                    continue;

                var position = _editor.GetPositionFromCharIndex(charIndex);
                var text = (line + 1).ToString();
                var size = e.Graphics.MeasureString(text, Font);
                e.Graphics.DrawString(text, Font, brush, Width - size.Width - HorizontalPadding, position.Y);
            }
        }

        private void UpdateWidth()
        {
            if (_editor is null || _editor.IsDisposed)
                return;

            var digits = _lineCount.ToString(System.Globalization.CultureInfo.InvariantCulture).Length;
            var digitWidth = TextRenderer.MeasureText(new string('8', digits), Font).Width;
            var desiredWidth = Math.Max(MinPanelWidth, digitWidth + HorizontalPadding + 4);
            if (Width != desiredWidth)
                Width = desiredWidth;
        }
    }
}
