#nullable enable
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Tiedragon.Help;
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
    private const int MaxPackageHeaderBytes = 64 * 1024;
    private const long MaxPackagePayloadBytes = 192L * 1024 * 1024;

    private static readonly byte[] LanguagePackageMagic = Encoding.ASCII.GetBytes(LanguagePackageMagicText);
    private static readonly JsonSerializerOptions LanguagePackageJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private static readonly HashSet<string> AllowedPackageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".css",
        ".html",
        ".jpg",
        ".jpeg",
        ".js",
        ".json",
        ".lng",
        ".png",
        ".svg",
        ".webp",
    };

    private static readonly HashSet<string> BlockedPackageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bat",
        ".cmd",
        ".com",
        ".dll",
        ".exe",
        ".msi",
        ".ps1",
        ".scr",
        ".vbs",
    };

    private static readonly HashSet<string> AllowedPackageScriptFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "basis.js",
        "formula.js",
        "nod.js",
    };

    private static readonly Regex HtmlCommentRegex = new("<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex HtmlTagRegex = new("</?[a-zA-Z][^>]*?>", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex HtmlAttributeRegex = new(@"\s([a-zA-Z_:][-a-zA-Z0-9_:.]*)(?=\s*=)", RegexOptions.Compiled);
    private static readonly Regex QuotedStringRegex = new("(\"[^\"]*\"|'[^']*')", RegexOptions.Compiled);
    private static readonly Regex JsonPropertyRegex = new("\"[^\"\\r\\n]*\"(?=\\s*:)", RegexOptions.Compiled);
    private static readonly Regex CssSelectorRegex = new(@"(^|\})([^{]+)(?=\{)", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex LanguageKeyRegex = new(@"^[^#;\r\n=]+(?=\=)", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex HtmlHrefLinkRegex = new("href\\s*=\\s*(?<quote>[\"'])(?<path>[^\"']+)\\k<quote>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HtmlMediaLinkRegex = new("(?<attr>src|href)\\s*=\\s*(?<quote>[\"'])(?<path>[^\"']+\\.(?:png|jpg|jpeg|svg))\\k<quote>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly TreeView _fileTree;
    private readonly ListView _documentList;
    private readonly FlowLayoutPanel _tabStrip;
    private readonly ToolStrip _htmlToolbar;
    private readonly RowStyle _htmlToolbarRow;
    private readonly Panel _editorContent;
    private readonly WebView2 _htmlEditor;
    private readonly WebView2 _preview;
    private readonly SplitContainer _contentSplit;
    private readonly Label _statusLabel;
    private readonly ToolStripButton _saveButton;
    private readonly ToolStripButton _validateButton;
    private readonly ToolStripButton _previewButton;
    private readonly List<ToolEditorDocument> _documents = [];
    private string _manifestText = "";
    private string? _pendingHtml;
    private string? _pendingHtmlEditor;
    private string? _conceptFolder;
    private bool _browserFailed;
    private bool _htmlEditorFailed;
    private bool _loadingHtmlEditor;
    private bool _updatingNavigation;
    private ToolEditorDocument? _current;

    public ToolEditorForm()
    {
        Text = "Tiedragon ToolEditor";
        Width = 1180;
        Height = 760;
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;
        KeyDown += ToolEditorForm_KeyDown;
        AllowDrop = true;
        DragEnter += ToolEditorForm_DragEnter;
        DragDrop += ToolEditorForm_DragDrop;

        var menu = BuildMenu();
        var toolbar = ToolEditorApi.CreateToolbar();
        toolbar.Items.Add(ToolEditorApi.CreateButton("Nieuw package", ToolEditorIcon.New, async (_, _) => await NewLanguagePackageAsync(), "Nieuw basispackage op basis van Engels"));
        toolbar.Items.Add(ToolEditorApi.CreateButton("Open", ToolEditorIcon.Open, async (_, _) => await OpenLanguagePackageAsync(), "Open taalpackage of concept"));
        toolbar.Items.Add(new ToolStripSeparator());
        _saveButton = ToolEditorApi.CreateButton("Save concept", ToolEditorIcon.Save, async (_, _) => await SaveConceptLanguagePackageAsync(), "Save concept taalpackage");
        _validateButton = ToolEditorApi.CreateButton("Validate", ToolEditorIcon.Validate, (_, _) => ValidateCurrent(showMessage: true), "Validate current document");
        _previewButton = ToolEditorApi.CreateButton("Preview", ToolEditorIcon.Test, (_, _) => UpdatePreview(), "Refresh HTML preview");
        var compileButton = ToolEditorApi.CreateButton("Compileer", ToolEditorIcon.Solver, async (_, _) => await CompileLanguagePackageAsync(), "Compileer taalpackage naar .lngpdk");
        toolbar.Items.Add(_saveButton);
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(_validateButton);
        toolbar.Items.Add(_previewButton);
        toolbar.Items.Add(compileButton);

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
        _documentList.Columns.Add("Soort", 150);
        _documentList.Columns.Add("Onderwerp", 240);
        _documentList.Columns.Add("Pakketpad", 360);
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

        _htmlToolbar = CreateHtmlToolbar();
        _htmlToolbar.Visible = false;

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
            FocusActiveEditor();
        };
        _ = InitializeHtmlEditorAsync();

        _editorContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(1, 0, 1, 1)
        };

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
        editorHost.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        editorHost.RowStyles.Add(_htmlToolbarRow);
        editorHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        editorHost.Controls.Add(_tabStrip, 0, 0);
        editorHost.Controls.Add(_htmlToolbar, 0, 1);
        editorHost.Controls.Add(_editorContent, 0, 2);

        _preview = new WebView2
        {
            Dock = DockStyle.Fill,
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
        _ = InitializeBrowserAsync();

        var previewHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(203, 213, 225),
            Padding = new Padding(1)
        };
        previewHost.Controls.Add(_preview);

        _contentSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            BorderStyle = BorderStyle.None,
            SplitterWidth = 5,
            BackColor = Color.FromArgb(226, 232, 240),
            Panel1MinSize = 1,
            Panel2MinSize = 1
        };
        _contentSplit.Panel1.Controls.Add(editorHost);
        _contentSplit.Panel2.Padding = new Padding(0, 4, 0, 0);
        _contentSplit.Panel2.Controls.Add(previewHost);
        _contentSplit.SizeChanged += (_, _) => ClampSplitter(_contentSplit, 390);
        Shown += (_, _) => ClampSplitter(_contentSplit, 390);

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
        Shown += (_, _) => ClampSplitter(split, 270);

        _statusLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 8, 0),
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = Color.FromArgb(31, 41, 55),
            Text = "Ready"
        };

        Controls.Add(split);
        Controls.Add(_statusLabel);
        Controls.Add(toolbar);
        Controls.Add(menu);
        MainMenuStrip = menu;

        NewLanguagePackageTemplate();
        RefreshFileTree();
        RefreshDocumentList();
        UpdateUiState();
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip
        {
            Dock = DockStyle.Top
        };

        var file = new ToolStripMenuItem("Bestand");
        file.DropDownItems.Add(CreateMenuItem("Nieuw package", Keys.Control | Keys.N, async (_, _) => await NewLanguagePackageAsync()));
        file.DropDownItems.Add(CreateMenuItem("Open taalpackage...", Keys.Control | Keys.O, async (_, _) => await OpenLanguagePackageAsync()));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(CreateMenuItem("Save concept taalpackage", Keys.Control | Keys.S, async (_, _) => await SaveConceptLanguagePackageAsync()));
        file.DropDownItems.Add("Opslaan huidig document", null, (_, _) => SaveCurrent());
        file.DropDownItems.Add("Compileer taalpackage...", null, async (_, _) => await CompileLanguagePackageAsync());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("Sluiten", null, (_, _) => Close());

        var edit = new ToolStripMenuItem("Bewerken");
        edit.DropDownItems.Add(CreateMenuItem("Ongedaan maken", Keys.Control | Keys.Z, (_, _) => _current?.Editor.Undo()));
        edit.DropDownItems.Add(CreateMenuItem("Knippen", Keys.Control | Keys.X, (_, _) => _current?.Editor.Cut()));
        edit.DropDownItems.Add(CreateMenuItem("Kopieren", Keys.Control | Keys.C, (_, _) => _current?.Editor.Copy()));
        edit.DropDownItems.Add(CreateMenuItem("Plakken", Keys.Control | Keys.V, (_, _) => _current?.Editor.Paste()));
        edit.DropDownItems.Add(CreateMenuItem("Alles selecteren", Keys.Control | Keys.A, (_, _) => _current?.Editor.SelectAll()));

        var view = new ToolStripMenuItem("Beeld");
        view.DropDownItems.Add(CreateMenuItem("Preview verversen", Keys.F5, (_, _) => UpdatePreview()));
        view.DropDownItems.Add("Valideren", null, (_, _) => ValidateCurrent(showMessage: true));

        var package = new ToolStripMenuItem("Pakket");
        package.DropDownItems.Add("Mediabestanden tonen", null, (_, _) => ShowMediaFileListDialog());
        package.DropDownItems.Add(new ToolStripSeparator());
        package.DropDownItems.Add("Manifest tonen", null, (_, _) => ShowManifestDialog());
        package.DropDownItems.Add("Manifest valideren", null, (_, _) => ValidateManifest(showMessage: true));
        package.DropDownItems.Add("Media toevoegen...", null, (_, _) => OpenMediaDocument());
        package.DropDownItems.Add("Media vervangen...", null, (_, _) => ReplaceCurrentMedia());
        package.DropDownItems.Add("Geselecteerd bestand verwijderen", null, (_, _) => DeleteCurrentDocument());
        package.DropDownItems.Add("HTML-links controleren", null, (_, _) => ValidatePackageLinks(showMessage: true));
        package.DropDownItems.Add(new ToolStripSeparator());
        package.DropDownItems.Add("Pakket compileren...", null, async (_, _) => await CompileLanguagePackageAsync());
        package.DropDownItems.Add("SHA-256 berekenen...", null, (_, _) => ShowPackageSha256());
        package.DropDownItems.Add("Encryptie...", null, (_, _) => ShowEncryptionStatus());
        package.DropDownItems.Add(new ToolStripSeparator());
        package.DropDownItems.Add("Media bekijken", null, (_, _) => SelectFirstGroup("Media en afbeeldingen"));

        var help = new ToolStripMenuItem("Help");
        help.DropDownItems.Add(CreateMenuItem("ToolEditor help", Keys.F1, (_, _) => ShowToolEditorHelp()));

        menu.Items.Add(file);
        menu.Items.Add(edit);
        menu.Items.Add(view);
        menu.Items.Add(package);
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

    private void ShowToolEditorHelp()
    {
        HelpApi.ShowDialog(this, new HelpDialogOptions(
            "ToolEditor help",
            BuildToolEditorHelpPages(),
            "overview",
            new HelpNavigationLabels("Start", "Vorige", "Volgende")));
    }

    private static IReadOnlyList<NodHelpPage> BuildToolEditorHelpPages()
    {
        return
        [
            BuildToolEditorHelpPage("overview", "ToolEditor gebruiken", "overview.html", BuildToolEditorOverviewFallback()),
            BuildToolEditorHelpPage("package", "Taalpakket workflow", "package-workflow.html", BuildToolEditorPackageFallback()),
            BuildToolEditorHelpPage("html", "HTML bewerken", "html-editor.html", BuildToolEditorHtmlFallback()),
            BuildToolEditorHelpPage("media", "Media en afbeeldingen", "media.html", BuildToolEditorMediaFallback()),
            BuildToolEditorHelpPage("compile", "Valideren en compileren", "compile.html", BuildToolEditorCompileFallback())
        ];
    }

    private static NodHelpPage BuildToolEditorHelpPage(string id, string title, string fileName, string fallback)
    {
        var body = HelpHtml.Content("tool-editor/" + fileName, fallback);
        return new NodHelpPage(id, title, HelpHtml.WrapTopicPage(title, body, HelpApi.NodHelpCss(), ToolEditorHelpPreviewScript()));
    }

    private static string BuildToolEditorOverviewFallback()
    {
        return """
        <p>ToolEditor bewerkt Tiedragon language packages: vertaling, help, NOD-documentatie, formulekaart en media.</p>
        <ul>
          <li>Links staat de vaste pakketstructuur.</li>
          <li>Boven werk je aan de actieve tab.</li>
          <li>Bij source-weergave toont de rechterzijde of onderzijde de HTML-preview.</li>
        </ul>
        """;
    }

    private static string BuildToolEditorPackageFallback()
    {
        return """
        <p>Gebruik <b>Nieuw package</b> voor een basispakket, <b>Save concept taalpackage</b> voor werkbestanden en <b>Compileer taalpackage</b> voor een gecontroleerd .lngpdk-bestand.</p>
        """;
    }

    private static string BuildToolEditorHtmlFallback()
    {
        return """
        <p>HTML-documenten hebben twee standen: <b>Source</b> voor broncode en <b>Edit</b> voor directe bewerking.</p>
        <p>De knoppen H1, H2, P, Info, Tip, Warn, Code en Kbd voegen standaard helpblokken in.</p>
        """;
    }

    private static string BuildToolEditorMediaFallback()
    {
        return """
        <p>Media bevat alleen toegestane afbeeldingen zoals png, jpg en svg. Sleep en zoom in de afbeeldingpreview om details te controleren.</p>
        """;
    }

    private static string BuildToolEditorCompileFallback()
    {
        return """
        <p>Validate controleert structuur, links, media en scripts. Compile maakt een .lngpdk met checksum en strikte bestandslijst.</p>
        """;
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

        toolbar.Items.Add(CreateHtmlButton("Source", "HTML-broncode bewerken", (_, _) => SetHtmlEditMode(false)));
        toolbar.Items.Add(CreateHtmlButton("Edit", "Visuele HTML-editor", (_, _) => SetHtmlEditMode(true)));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(CreateHtmlButton("H1", "Kop 1 invoegen", (_, _) => WrapHtmlSelection("h1", "Kop")));
        toolbar.Items.Add(CreateHtmlButton("H2", "Kop 2 invoegen", (_, _) => WrapHtmlSelection("h2", "Kop")));
        toolbar.Items.Add(CreateHtmlButton("P", "Paragraaf invoegen", (_, _) => WrapHtmlSelection("p", "Tekst")));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(CreateHtmlButton("B", "Vet", (_, _) => WrapHtmlSelection("strong", "tekst")));
        toolbar.Items.Add(CreateHtmlButton("I", "Cursief", (_, _) => WrapHtmlSelection("em", "tekst")));
        toolbar.Items.Add(CreateHtmlButton("Link", "Link invoegen", (_, _) => InsertHtmlLink()));
        toolbar.Items.Add(CreateHtmlButton("Img", "Afbeelding uit media invoegen", (_, _) => InsertHtmlImage()));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(CreateHtmlButton("UL", "Lijst invoegen", (_, _) => InsertHtmlSnippet("<ul>\r\n  <li>Item</li>\r\n</ul>")));
        toolbar.Items.Add(CreateHtmlButton("Info", "Infoblok invoegen", (_, _) => InsertHtmlSnippet("<div class=\"help-info\">Informatie</div>")));
        toolbar.Items.Add(CreateHtmlButton("Tip", "Tipblok invoegen", (_, _) => InsertHtmlSnippet("<div class=\"help-tip\">Tip</div>")));
        toolbar.Items.Add(CreateHtmlButton("Warn", "Waarschuwing invoegen", (_, _) => InsertHtmlSnippet("<div class=\"help-warning\">Waarschuwing</div>")));
        toolbar.Items.Add(CreateHtmlButton("Code", "Codeblok invoegen", (_, _) => InsertHtmlSnippet("<pre><code>code</code></pre>")));
        toolbar.Items.Add(CreateHtmlButton("Kbd", "Toets/keyboard invoegen", (_, _) => InsertHtmlSnippet("<kbd>Ctrl</kbd>")));
        toolbar.Items.Add(CreateHtmlButton("BR", "Regeleinde invoegen", (_, _) => InsertHtmlSnippet("<br>")));
        return toolbar;
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

    private void NewLanguagePackageTemplate()
    {
        _conceptFolder = null;
        _manifestText = BuildManifestTemplate("eng", "English", "English");
        var languageDocument = AddDocument("language/eng.lng", LoadLanguageText("eng", "English"), null);
        AddEnglishHelpDocuments();
        AddNodHelpDocuments();
        AddFormulaCardDocuments();
        SelectDocument(languageDocument);
    }

    private async Task NewLanguagePackageAsync()
    {
        if (!await ConfirmResetPackageAsync())
            return;

        ClearPackageDocuments();
        NewLanguagePackageTemplate();
        RefreshFileTree();
        RefreshDocumentList();
        SetStatus("Nieuw basispackage gemaakt op basis van Engels.", isError: false);
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

    private static string BuildHtmlTemplate()
    {
        return BuildConceptHelpBanner("nl") + """
        <h1>Syscalculator Help</h1>
        <p>Er zijn nog geen Nederlandse helpbestanden gevonden.</p>
        <div class="help-warning">Controleer of de helpbronnen in de repository aanwezig zijn.</div>
        """;
    }

    private void AddDutchHelpDocuments()
    {
        AddLegacyHelpDocuments("nl", BuildHtmlTemplate);
    }

    private void AddEnglishHelpDocuments()
    {
        AddLegacyHelpDocuments("en", BuildEnglishHtmlTemplate);
    }

    private void AddLegacyHelpDocuments(string languagePrefix, Func<string> fallbackFactory)
    {
        var helpDirectory = FindRepositoryPath("legacy/Syscalculator174.VB6/help");
        if (helpDirectory is null)
        {
            AddDocument("manual/index.html", fallbackFactory(), null);
            return;
        }

        var helpFiles = Directory.GetFiles(helpDirectory, "*.htm")
            .Where(path => Path.GetFileName(path).Equals("index_" + languagePrefix + ".htm", StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(path).StartsWith("help_" + languagePrefix + "_", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => Path.GetFileName(path).Equals("index_" + languagePrefix + ".htm", StringComparison.OrdinalIgnoreCase) ? "" : Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (helpFiles.Count == 0)
        {
            AddDocument("manual/index.html", fallbackFactory(), null);
            return;
        }

        var pageNameMap = helpFiles.ToDictionary(
            path => Path.GetFileName(path),
            path => BuildLegacyHelpPackageFileName(Path.GetFileName(path), languagePrefix),
            StringComparer.OrdinalIgnoreCase);

        var mediaNameMap = AddDutchHelpMedia(helpDirectory);
        foreach (var file in helpFiles)
        {
            var packageName = pageNameMap[Path.GetFileName(file)];
            var html = File.ReadAllText(file, Encoding.Latin1);
            html = ConvertDutchHelpHtml(html, pageNameMap, mediaNameMap);
            html = BuildConceptHelpBanner(languagePrefix) + html;
            AddDocument("manual/" + packageName, html, file);
        }
    }

    private static string BuildLegacyHelpPackageFileName(string fileName, string languagePrefix)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        if (name.Equals("index_" + languagePrefix, StringComparison.OrdinalIgnoreCase))
            return "index.html";

        name = Regex.Replace(name, @"^(help|manual)[_-]+" + Regex.Escape(languagePrefix) + @"[_-]+", "", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"^" + Regex.Escape(languagePrefix) + @"[_-]+", "", RegexOptions.IgnoreCase);
        return name + ".html";
    }

    private static string BuildEnglishHtmlTemplate()
    {
        return BuildConceptHelpBanner("en") + """
        <h1>Syscalculator Help</h1>
        <p>This starter language package contains the editable package structure for Syscalculator help, NOD help, formula cards, translations and media.</p>
        <div class="help-info">Use this English base package as the source for a new translation package.</div>
        """;
    }

    private static string BuildConceptHelpBanner(string languagePrefix)
    {
        if (languagePrefix.Equals("nl", StringComparison.OrdinalIgnoreCase))
        {
            return """
            <div class="concept-banner" role="note" aria-label="Conceptwaarschuwing">
              <span><b>Concept:</b> deze helpinformatie is werkmateriaal voor een taalpackage en is nog geen officiele Syscalculator-help.</span>
              <button class="concept-banner-close" type="button" title="Sluiten" aria-label="Sluiten">x</button>
            </div>

            """;
        }

        return """
        <div class="concept-banner" role="note" aria-label="Concept warning">
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

        foreach (var file in helpFiles)
            AddDocument(BuildNodHelpPackagePath(nodRoot, file), File.ReadAllText(file, Encoding.UTF8), file);
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
        var cards = FormulaCardCatalog.GetDefaultCards();
        AddDocument("formula/index.html", BuildFormulaIndexHtml(cards), null);
        foreach (var card in cards.OrderBy(card => card.Title, StringComparer.CurrentCultureIgnoreCase))
            AddDocument(BuildFormulaCardPackagePath(card), BuildFormulaCardHtml(card), null);
    }

    private static string BuildNodHelpFallback()
    {
        return """
        <h1>NOD help</h1>
        <p>NOD is de teksttaal waarmee Syscalculator converters, berekeningen, tekstomzettingen en dataregels beschrijft.</p>
        <div class="help-info">De uitgebreide NOD-helpbronnen zijn niet gevonden in deze checkout.</div>
        """;
    }

    private static string BuildFormulaIndexHtml(IReadOnlyList<FormulaCard> cards)
    {
        var rows = new StringBuilder();
        foreach (var card in cards.OrderBy(card => card.Title, StringComparer.CurrentCultureIgnoreCase))
        {
            rows.Append("<tr><td><a href=\"")
                .Append(WebUtility.HtmlEncode(BuildFormulaCardRelativeLink(card)))
                .Append("\">")
                .Append(WebUtility.HtmlEncode(card.Title))
                .Append("</a></td><td>")
                .Append(WebUtility.HtmlEncode(FriendlyFormulaCategory(BuildFormulaCardCategory(card))))
                .Append("</td><td>")
                .Append(WebUtility.HtmlEncode(string.Join(", ", card.LevelTags)))
                .Append("</td><td>")
                .Append(WebUtility.HtmlEncode(card.Description))
                .Append("</td></tr>");
        }

        return """
        <h1>Formulekaart</h1>
        <p>De formulekaart bundelt wiskundige basisregels, MathML, LaTeX en voorbeeld-NOD voor gebruik in Syscalculator.</p>
        <table>
          <thead><tr><th>Formule</th><th>Categorie</th><th>Tags</th><th>Uitleg</th></tr></thead>
          <tbody>
        """ + rows + """
          </tbody>
        </table>
        """;
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

    private static string BuildFormulaCardHtml(FormulaCard card)
    {
        return $$"""
        <h1>{{WebUtility.HtmlEncode(card.Title)}}</h1>
        <div class="notice">{{WebUtility.HtmlEncode(string.Join(", ", card.LevelTags))}}</div>
        <p>{{RenderFormulaCardDescription(card.Description)}}</p>

        <h2>Formule</h2>
        <p><code>{{WebUtility.HtmlEncode(card.Formula)}}</code></p>
        <div class="formula">{{card.MathMl}}</div>

        <h2>Tekst</h2>
        <pre>{{WebUtility.HtmlEncode(card.PlainText)}}</pre>

        <h2>LaTeX</h2>
        <pre>{{WebUtility.HtmlEncode(card.Latex)}}</pre>

        <h2>MathML</h2>
        <pre>{{WebUtility.HtmlEncode(card.MathMl)}}</pre>

        <h2>Voorbeeld-NOD</h2>
        <pre>{{WebUtility.HtmlEncode(card.ExampleNod)}}</pre>
        """;
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

    private Dictionary<string, string> AddDutchHelpMedia(string helpDirectory)
    {
        var media = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(helpDirectory)
                     .Where(IsImagePath)
                     .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            var name = Path.GetFileName(file);
            var packagePath = "assets/" + name;
            media[name] = packagePath;
            AddImageDocument(packagePath, File.ReadAllBytes(file), file);
        }

        return media;
    }

    private static string ConvertDutchHelpHtml(string html, IReadOnlyDictionary<string, string> pageNameMap, IReadOnlyDictionary<string, string> mediaNameMap)
    {
        html = html.Replace("charset=windows-1252", "charset=utf-8", StringComparison.OrdinalIgnoreCase);
        html = html.Replace("Syscalculator 1.74", "Syscalculator", StringComparison.OrdinalIgnoreCase);
        html = Regex.Replace(
            html,
            @"\s*<div\s+class\s*=\s*[""']nav[""']>\s*<a\s+href\s*=\s*[""']index_nl\.htm[""']>\s*Nederlandse help\s*</a>\s*</div>\s*",
            Environment.NewLine,
            RegexOptions.IgnoreCase);
        foreach (var item in pageNameMap)
            html = ReplaceQuotedPath(html, item.Key, item.Value);
        foreach (var item in mediaNameMap)
            html = ReplaceQuotedPath(html, item.Key, item.Value);

        return html;
    }

    private static string ReplaceQuotedPath(string html, string from, string to)
    {
        return Regex.Replace(
            html,
            "(?<quote>[\"'])" + Regex.Escape(from) + "\\k<quote>",
            match => match.Groups["quote"].Value + to + match.Groups["quote"].Value,
            RegexOptions.IgnoreCase);
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

    private void OpenDocument()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open ToolEditor document",
            Filter = "Language package files (*.html;*.json;*.lng;*.css;*.js;*.png;*.jpg;*.jpeg;*.svg)|*.html;*.json;*.lng;*.css;*.js;*.png;*.jpg;*.jpeg;*.svg|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        if (Path.GetFileName(dialog.FileName).Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
        {
            _manifestText = File.ReadAllText(dialog.FileName);
            SetStatus("Manifest geladen als pakketinfo.", isError: false);
            ShowManifestDialog();
        }
        else if (IsImagePath(dialog.FileName))
            AddOrReplaceImageDocument("assets/" + Path.GetFileName(dialog.FileName), File.ReadAllBytes(dialog.FileName), dialog.FileName, markDirty: false);
        else if (TryBuildImportPackagePath(dialog.FileName, out var packagePath))
            SelectDocument(AddDocument(packagePath, File.ReadAllText(dialog.FileName), dialog.FileName));
        else
            RejectUnsupportedPackageFile(dialog.FileName);
    }

    private async Task OpenLanguagePackageAsync()
    {
        if (!await ConfirmResetPackageAsync())
            return;

        using var dialog = new OpenFileDialog
        {
            Title = "Open taalpackage",
            Filter = "Taalpackage (*.lngpdk;*.zip;manifest.json)|*.lngpdk;*.zip;manifest.json|Alle bestanden (*.*)|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            if (Path.GetFileName(dialog.FileName).Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
                LoadPackageSourceFolder(Path.GetDirectoryName(dialog.FileName)!);
            else
                LoadLanguagePackageArchive(dialog.FileName);

            RefreshFileTree();
            RefreshDocumentList();
            var firstDocument = _documents.FirstOrDefault(document => document.PackagePath.StartsWith("language/", StringComparison.OrdinalIgnoreCase)) ?? _documents.FirstOrDefault();
            if (firstDocument is not null)
                SelectDocument(firstDocument);
            SetStatus("Taalpackage geopend: " + dialog.FileName, isError: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException or JsonException)
        {
            SetStatus("Open taalpackage mislukt: " + ex.Message, isError: true);
            MessageBox.Show(this, ex.Message, "Open taalpackage", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task SaveConceptLanguagePackageAsync()
    {
        await SyncHtmlEditorToSourceAsync();

        var targetFolder = _conceptFolder;
        if (string.IsNullOrWhiteSpace(targetFolder))
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Kies map voor concept taalpackage",
                UseDescriptionForTitle = true,
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            targetFolder = dialog.SelectedPath;
        }

        try
        {
            ValidatePackageSourcePaths();
            WritePackageSourceFolder(targetFolder);
            _conceptFolder = targetFolder;
            foreach (var document in _documents)
            {
                document.FilePath = GetSafePackageFilePath(_conceptFolder, document.PackagePath);
                SetDirty(document, false);
            }

            SetStatus("Concept taalpackage opgeslagen: " + _conceptFolder, isError: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            SetStatus("Save concept mislukt: " + ex.Message, isError: true);
            MessageBox.Show(this, ex.Message, "Save concept taalpackage", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadPackageSourceFolder(string sourceFolder)
    {
        var manifestPath = Path.Combine(sourceFolder, "manifest.json");
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("manifest.json ontbreekt.", manifestPath);

        ClearPackageDocuments();
        _conceptFolder = sourceFolder;
        _manifestText = File.ReadAllText(manifestPath, Encoding.UTF8);

        foreach (var file in Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories)
                     .OrderBy(path => Path.GetRelativePath(sourceFolder, path), StringComparer.OrdinalIgnoreCase))
        {
            var packagePath = Path.GetRelativePath(sourceFolder, file).Replace('\\', '/');
            if (IsManifestPath(packagePath))
                continue;

            AddPackageFileFromDisk(packagePath, file);
        }
    }

    private void LoadLanguagePackageArchive(string packagePath)
    {
        ClearPackageDocuments();
        _conceptFolder = null;
        var payload = ReadLanguagePackagePayload(packagePath);
        using var memory = new MemoryStream(payload);
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);
        var entries = archive.Entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
            .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var manifestEntry = entries.FirstOrDefault(entry => NormalizePackagePath(entry.FullName).Equals("manifest.json", StringComparison.OrdinalIgnoreCase)) ??
            throw new InvalidDataException("manifest.json ontbreekt.");
        using (var stream = manifestEntry.Open())
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
            _manifestText = reader.ReadToEnd();
        }

        foreach (var entry in entries)
        {
            var entryName = NormalizePackagePath(entry.FullName);
            if (IsManifestPath(entryName))
                continue;

            ValidatePackageEntryPathOrThrow(entryName, IsImagePath(entryName));
            using var stream = entry.Open();
            if (IsImagePath(entryName))
            {
                using var buffer = new MemoryStream();
                stream.CopyTo(buffer);
                AddImageDocument(entryName, buffer.ToArray(), null);
            }
            else
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);
                AddDocument(entryName, reader.ReadToEnd(), null);
            }
        }
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
            "Het huidige taalpackage heeft niet-opgeslagen wijzigingen.\r\n\r\nSave concept voordat je verdergaat?",
            "Taalpackage",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Cancel)
            return false;
        if (result == DialogResult.No)
            return true;

        await SaveConceptLanguagePackageAsync();
        return !_documents.Any(document => document.Dirty && !document.ReadOnly);
    }

    private void ClearPackageDocuments()
    {
        _documents.Clear();
        _tabStrip.Controls.Clear();
        _editorContent.Controls.Clear();
        _current = null;
        _pendingHtml = null;
        _pendingHtmlEditor = null;
        _manifestText = "";
        _contentSplit.Panel2Collapsed = true;
        _previewButton.Enabled = false;
        RefreshTabStrip();
        RefreshFileTree();
        RefreshDocumentList();
        UpdateUiState();
    }

    private static byte[] ReadLanguagePackagePayload(string packagePath)
    {
        var bytes = File.ReadAllBytes(packagePath);
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

        var header = JsonSerializer.Deserialize<LanguagePackageContainerHeader>(
            Encoding.UTF8.GetString(headerBytes),
            LanguagePackageJsonOptions) ?? throw new InvalidDataException("Packageheader is ongeldig.");
        ValidateLanguagePackageHeader(header);

        var payload = reader.ReadBytes((int)(memory.Length - memory.Position));
        if (!string.IsNullOrWhiteSpace(header.PayloadSha256) &&
            !ComputeSha256Bytes(payload).Equals(header.PayloadSha256.Trim().ToLowerInvariant(), StringComparison.Ordinal))
        {
            throw new InvalidDataException("Payload SHA-256 klopt niet.");
        }

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
    }

    private void ShowManifestDialog()
    {
        var properties = ReadManifestProperties(_manifestText);
        using var dialog = new Form
        {
            Text = "Eigenschappen van taalpakket",
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

        AddManifestPropertyRow(content, "Naam", properties.DisplayName);
        AddManifestPropertyRow(content, "Taalcode", properties.LanguageCode);
        AddManifestPropertyRow(content, "Native naam", properties.NativeName);
        AddManifestPropertyRow(content, "Pakketversie", properties.PackageVersion);
        AddManifestPropertyRow(content, "Fallback taal", properties.FallbackLanguage);
        AddManifestPropertyRow(content, "Producent", properties.Producer);
        AddManifestPropertyRow(content, "Product", properties.Product);
        AddManifestPropertyRow(content, "Software-id", properties.SoftwareId);
        AddManifestPropertyRow(content, "Package-id", properties.Id);

        var close = new Button
        {
            Text = "Sluiten",
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
            Text = "Mediabestanden",
            Width = 760,
            Height = 420,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowIcon = false
        };

        var list = CreateMediaFileList();
        list.Dock = DockStyle.Fill;
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
            Text = "Openen",
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
            Text = "Sluiten",
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
        list.Columns.Add("Bestand", 220);
        list.Columns.Add("Type", 90);
        list.Columns.Add("Grootte", 110);
        list.Columns.Add("Pakketpad", 300);

        foreach (var document in _documents.Where(document => document.ImageBytes is not null)
                     .OrderBy(document => document.PackagePath, StringComparer.OrdinalIgnoreCase))
        {
            var item = new ListViewItem(Path.GetFileName(document.PackagePath));
            item.SubItems.Add(Path.GetExtension(document.PackagePath).TrimStart('.').ToUpperInvariant());
            item.SubItems.Add((document.ImageBytes?.Length ?? 0).ToString("N0") + " bytes");
            item.SubItems.Add(document.PackagePath);
            item.Tag = document;
            list.Items.Add(item);
        }

        return list;
    }

    private void OpenMediaDocument()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Media toevoegen aan language package",
            Multiselect = true,
            Filter = "Media (*.png;*.jpg;*.jpeg;*.svg)|*.png;*.jpg;*.jpeg;*.svg|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        ImportMediaFiles(dialog.FileNames);
    }

    private void ReplaceCurrentMedia()
    {
        if (_current?.ImageBytes is null)
        {
            SetStatus("Selecteer eerst een media-bestand.", isError: true);
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Title = "Media vervangen",
            Filter = "Media (*.png;*.jpg;*.jpeg;*.svg)|*.png;*.jpg;*.jpeg;*.svg|All files (*.*)|*.*"
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
        SetStatus("Media vervangen: " + _current.PackagePath, isError: false);
    }

    private void DeleteCurrentDocument()
    {
        if (_current is null)
            return;

        if (_current.ReadOnly)
        {
            SetStatus("Alleen tonen: " + _current.DisplayName, isError: true);
            return;
        }

        if (IsProtectedPackageDocument(_current.PackagePath))
        {
            SetStatus("Taalpakket-onderdeel kan niet worden verwijderd: " + BuildTabTitle(_current), isError: true);
            MessageBox.Show(
                this,
                "Dit onderdeel hoort bij de vaste taalpakket-structuur en kan niet worden verwijderd.\r\n\r\nGebruik het kruisje op de tab om alleen de tab te sluiten.",
                "ToolEditor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var result = MessageBox.Show(
            this,
            "Verwijder dit bestand uit het language package?\r\n\r\nHet bronbestand op schijf wordt niet verwijderd.",
            "ToolEditor",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
            return;

        var removedPath = _current.PackagePath;
        RemoveDocumentFromPackage(_current);
        SetStatus("Uit pakket verwijderd: " + removedPath, isError: false);
    }

    private void ImportMediaFiles(IEnumerable<string> fileNames)
    {
        var imported = 0;
        foreach (var fileName in fileNames.Where(IsImagePath))
        {
            AddOrReplaceImageDocument("assets/" + Path.GetFileName(fileName), File.ReadAllBytes(fileName), fileName, markDirty: true);
            imported++;
        }

        if (imported == 0)
            SetStatus("Geen ondersteunde media gevonden.", isError: true);
        else
            SetStatus(imported.ToString("N0") + " media-bestand(en) toegevoegd.", isError: false);
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
                !AllowedPackageScriptFiles.Contains(name))
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
        var message = "Dit bestand hoort niet in de language package-structuur:\r\n\r\n" +
            Path.GetFileName(fileName) +
            "\r\n\r\nToegestaan: manifest.json, language/*.lng, manual/help/NOD/formule HTML/CSS/JS, en media png/jpg/jpeg/svg.";
        SetStatus("Bestand geweigerd: " + Path.GetFileName(fileName), isError: true);
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
            SetStatus("Alleen tonen: " + document.DisplayName, isError: true);
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
                Filter = "Language package files (*.html;*.json;*.lng;*.css;*.js;*.png;*.jpg;*.jpeg;*.svg)|*.html;*.json;*.lng;*.css;*.js;*.png;*.jpg;*.jpeg;*.svg|All files (*.*)|*.*"
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
            File.WriteAllText(path, document.Editor.Text, Encoding.UTF8);
        SetDirty(document, false);
        SetStatus("Saved: " + path, isError: false);
        return true;
    }

    private ToolEditorDocument AddDocument(string displayName, string text, string? filePath)
    {
        var page = new TabPage(displayName);
        var document = new ToolEditorDocument(page, displayName, NormalizePackagePath(displayName), filePath);
        var editor = CreateTextEditor(text, document);
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
        RefreshFileTree();
        RefreshDocumentList();
        return document;
    }

    private ToolEditorDocument AddImageDocument(string displayName, byte[] bytes, string? filePath)
    {
        var page = new TabPage(displayName);
        var document = new ToolEditorDocument(page, displayName, NormalizePackagePath(displayName), filePath)
        {
            ImageBytes = bytes
        };
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
        RefreshFileTree();
        RefreshDocumentList();
        return document;
    }

    private void AddOrReplaceImageDocument(string displayName, byte[] bytes, string? filePath, bool markDirty)
    {
        var packagePath = NormalizePackagePath(displayName);
        var document = _documents.FirstOrDefault(document => document.ImageBytes is not null &&
            document.PackagePath.Equals(packagePath, StringComparison.OrdinalIgnoreCase));
        if (document is null)
        {
            document = AddImageDocument(displayName, bytes, filePath);
            if (markDirty)
                SetDirty(document, true);
            SelectDocument(document);
            return;
        }

        document.ImageBytes = bytes;
        document.FilePath = filePath;
        document.Editor.Text = BuildImageInfoText(document);
        ApplyDocumentLabels(document);
        SetDirty(document, markDirty);
        RefreshFileTree();
        RefreshDocumentList();
        SelectDocument(document);
    }

    private static string BuildImageInfoText(ToolEditorDocument document)
    {
        var bytes = document.ImageBytes?.Length ?? 0;
        return "Media preview\r\n\r\n" +
            "Bestand: " + Path.GetFileName(document.PackagePath) + "\r\n" +
            "Pakketpad: " + document.PackagePath + "\r\n" +
            "Grootte: " + bytes.ToString("N0") + " bytes\r\n";
    }

    private async void SetHtmlEditMode(bool editMode)
    {
        if (_current is null || _current.ImageBytes is not null || !IsHtmlDocument(_current))
        {
            SetStatus("Selecteer eerst een HTML-document.", isError: true);
            return;
        }

        if (!editMode && _current.HtmlEditMode)
            await SyncHtmlEditorToSourceAsync();

        _current.HtmlEditMode = editMode;
        SelectDocument(_current);
        QueueFocusActiveEditor();
    }

    private void WrapHtmlSelection(string tag, string fallbackText)
    {
        if (!CanEditCurrentHtml())
            return;

        if (_current!.HtmlEditMode)
            ExecuteHtmlEditorCommand("formatBlock", "<" + tag + ">");
        else
        {
            var editor = _current.Editor;
            var selected = editor.SelectedText;
            if (string.IsNullOrEmpty(selected))
                selected = fallbackText;

            editor.SelectedText = "<" + tag + ">" + selected + "</" + tag + ">";
            editor.Focus();
        }

        UpdatePreview();
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

        SetStatus("Selecteer eerst een HTML-document.", isError: true);
        return false;
    }

    private RichTextBox CreateTextEditor(string text, ToolEditorDocument document)
    {
        var isLanguageDocument = document.PackagePath.StartsWith("language/", StringComparison.OrdinalIgnoreCase);
        var lineNumbers = new LineNumberPanel();
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
        document.LineNumbers = lineNumbers;
        lineNumbers.Attach(editor);
        editor.TextChanged += (_, _) =>
        {
            if (_current?.Editor == editor && !document.ReadOnly && !document.Highlighting)
                SetDirty(_current, true);

            document.LineNumbers?.Invalidate();
            ScheduleSyntaxHighlight(document);
        };
        editor.VScroll += (_, _) => document.LineNumbers?.Invalidate();
        editor.Resize += (_, _) => document.LineNumbers?.Invalidate();
        ScheduleSyntaxHighlight(document);
        return editor;
    }

    private void SelectDocument(ToolEditorDocument document)
    {
        EnsureDocumentTabOpen(document);
        _current = document;
        SelectDocumentInTree(document);
        SelectDocumentInList(document);
        UpdateHtmlToolbarState(document);
        _editorContent.SuspendLayout();
        _editorContent.Controls.Clear();
        if (document.HtmlEditMode && document.ImageBytes is null && IsHtmlDocument(document))
        {
            _editorContent.Controls.Add(_htmlEditor);
            SetHtmlEditor(document.Editor.Text);
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
        UpdatePreview();
        UpdateUiState();
        UpdateDocumentStatus(document);
        QueueFocusActiveEditor();
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
                _ = _htmlEditor.CoreWebView2?.ExecuteScriptAsync("document.body && document.body.focus();");
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
            SetStatus("Media: " + document.PackagePath + " (" + document.ImageBytes.Length.ToString("N0") + " bytes)", isError: false);
            return;
        }

        SetStatus("Geselecteerd: " + BuildTabTitle(document), isError: false);
    }

    private void UpdateHtmlToolbarState(ToolEditorDocument? document)
    {
        var visible = document is not null && document.ImageBytes is null && IsHtmlDocument(document);
        _htmlToolbar.Visible = visible;
        _htmlToolbarRow.Height = visible ? 32 : 0;
        UpdatePreviewPaneState(document);
    }

    private void UpdatePreviewPaneState(ToolEditorDocument? document)
    {
        var showPreview = document is not null && !document.HtmlEditMode;
        _contentSplit.Panel2Collapsed = !showPreview;
        _previewButton.Enabled = showPreview;
        if (showPreview)
            ClampSplitter(_contentSplit, 390);
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
            control.ContextMenuStrip = menu;
    }

    private ContextMenuStrip CreateTabContextMenu(ToolEditorDocument document)
    {
        var menu = new ContextMenuStrip();
        var closeAll = menu.Items.Add("Sluit alle tabbladen");
        var closeRight = menu.Items.Add("Sluit tabbladen rechts");
        var closeLeft = menu.Items.Add("Sluit tabbladen links");

        menu.Opening += (_, _) =>
        {
            SelectDocument(document);
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
        var root = new TreeNode("Language package")
        {
            NodeFont = new Font(_fileTree.Font, FontStyle.Bold)
        };
        _fileTree.Nodes.Add(root);

        foreach (var document in _documents
                     .OrderBy(document => GetTreeSortGroup(document.PackagePath))
                     .ThenBy(document => document.PackagePath, StringComparer.OrdinalIgnoreCase))
            AddDocumentNode(root, document);

        root.ExpandAll();
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

    private static void AddDocumentNode(TreeNode root, ToolEditorDocument document)
    {
        var parts = BuildTreePath(document.PackagePath);
        var parent = root;
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
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

        if (path.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) || IsImagePath(path))
            return 9;

        return 8;
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
            return ["Help voor gebruikers", topic];

        if (path.StartsWith("help/content/main/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("help/main/", StringComparison.OrdinalIgnoreCase))
        {
            return ["Help voor gebruikers", topic];
        }

        if (TryGetNodHelpRelativePath(path, out var nodRelativePath))
        {
            return BuildNodHelpTreePath(nodRelativePath, topic);
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

    private static (string Group, string Topic) BuildListLabels(string packagePath)
    {
        var path = BuildTreePath(packagePath);
        return path.Count >= 2
            ? (path[0], path[^1])
            : ("Ongeldig pakketbestand", path[0]);
    }

    private static void ApplyDocumentLabels(ToolEditorDocument document)
    {
        var labels = BuildListLabels(document.PackagePath);
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

        if (e.Node?.Tag is ToolEditorDocument document && !ReferenceEquals(document, _current))
            SelectDocument(document);
    }

    private async void ValidateCurrent(bool showMessage)
    {
        if (_current is null)
            return;

        await SyncHtmlEditorToSourceAsync();

        var errors = ValidateDocument(_current);
        AddMissingMediaLinkErrors(_current, errors);
        AddMissingInternalLinkErrors(_current, errors);
        if (errors.Count == 0)
        {
            SetStatus("Validation passed: " + _current.DisplayName, isError: false);
            if (showMessage)
                MessageBox.Show(this, "Validation passed.", "ToolEditor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetStatus("Validation failed: " + errors[0], isError: true);
        if (showMessage)
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "ToolEditor validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ValidateManifest(bool showMessage)
    {
        var errors = new List<string>();
        ValidateJson(_manifestText, errors);
        if (errors.Count == 0)
        {
            SetStatus("Manifest is geldig.", isError: false);
            if (showMessage)
                MessageBox.Show(this, "Manifest is geldig.", "ToolEditor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetStatus("Manifest ongeldig: " + errors[0], isError: true);
        if (showMessage)
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Manifest", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ValidatePackageLinks(bool showMessage)
    {
        var errors = new List<string>();
        foreach (var document in _documents)
        {
            AddMissingMediaLinkErrors(document, errors);
            AddMissingInternalLinkErrors(document, errors);
        }

        if (errors.Count == 0)
        {
            SetStatus("Alle HTML-links verwijzen naar bestaande package-documenten en media.", isError: false);
            if (showMessage)
                MessageBox.Show(this, "Alle HTML-links zijn gevonden in het package.", "ToolEditor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetStatus("HTML-link ontbreekt: " + errors[0], isError: true);
        if (showMessage)
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Ontbrekende links", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private async Task CompileLanguagePackageAsync()
    {
        await SyncHtmlEditorToSourceAsync();

        var errors = new List<string>();
        ValidatePackageForCompile(errors);
        if (errors.Count > 0)
        {
            SetStatus("Package niet gecompileerd: " + errors[0], isError: true);
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Package compileren", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var manifest = ReadManifestProperties(_manifestText);
        using var dialog = new SaveFileDialog
        {
            Title = "Language package compileren",
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
            WritePackageSourceFolder(tempFolder);
            var result = BuildLanguagePackage(tempFolder, outputPath);
            SetStatus("Package gecompileerd. SHA-256: " + result.PackageSha256, isError: false);
            MessageBox.Show(
                this,
                "Package gecompileerd:\r\n" + outputPath +
                "\r\n\r\nPackage SHA-256:\r\n" + result.PackageSha256 +
                "\r\n\r\nPayload SHA-256:\r\n" + result.PayloadSha256 +
                "\r\n\r\nEncryptie: uit (reader weigert encrypted packages nog fail-closed).",
                "Package compileren",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            SetStatus("Package compile mislukt: " + ex.Message, isError: true);
            MessageBox.Show(this, ex.Message, "Package compileren", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            Title = "SHA-256 berekenen",
            Filter = "Tiedragon language package (*.lngpdk)|*.lngpdk|Alle bestanden (*.*)|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var hash = ComputeSha256File(dialog.FileName);
            SetStatus("SHA-256: " + hash, isError: false);
            MessageBox.Show(this, dialog.FileName + "\r\n\r\nSHA-256:\r\n" + hash, "SHA-256", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus("SHA-256 mislukt: " + ex.Message, isError: true);
            MessageBox.Show(this, ex.Message, "SHA-256", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowEncryptionStatus()
    {
        MessageBox.Show(
            this,
            "De `.lngpdk` compile gebruikt nu een strikte container met dubbele SHA-256-controle:\r\n\r\n" +
            "- package SHA-256 voor het volledige bestand\r\n" +
            "- payload SHA-256 in de containerheader\r\n\r\n" +
            "Encryptie staat bewust nog uit. Syscalculator weigert encrypted packages nu fail-closed, zodat er geen half ondersteunde package kan laden. De container heeft het veld `Encrypted`, dus AES-encryptie kan later veilig worden toegevoegd.",
            "Encryptie",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ValidatePackageForCompile(List<string> errors)
    {
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

        var extension = Path.GetExtension(path);
        if (BlockedPackageExtensions.Contains(extension) || !AllowedPackageExtensions.Contains(extension))
            errors.Add("Bestandstype is niet toegestaan: " + packagePath);
        if (extension.Equals(".js", StringComparison.OrdinalIgnoreCase) &&
            !AllowedPackageScriptFiles.Contains(Path.GetFileName(path)))
        {
            errors.Add("Scriptbestand is niet toegestaan: " + packagePath);
        }

        if (isImage && !path.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
            errors.Add("Media hoort onder assets/: " + packagePath);
    }

    private void WritePackageSourceFolder(string rootFolder)
    {
        Directory.CreateDirectory(rootFolder);
        File.WriteAllText(GetSafePackageFilePath(rootFolder, "manifest.json"), _manifestText, Encoding.UTF8);

        foreach (var document in _documents.OrderBy(document => document.PackagePath, StringComparer.OrdinalIgnoreCase))
        {
            var packagePath = NormalizePackagePath(document.PackagePath);
            if (IsManifestPath(packagePath))
                continue;

            var filePath = GetSafePackageFilePath(rootFolder, packagePath);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            if (document.ImageBytes is not null)
                File.WriteAllBytes(filePath, document.ImageBytes);
            else
                File.WriteAllText(filePath, document.Editor.Text, Encoding.UTF8);
        }
    }

    private static LanguagePackageCompileResult BuildLanguagePackage(string sourceFolder, string outputPath)
    {
        var payload = BuildZipPayload(sourceFolder);
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

        foreach (Match match in HtmlMediaLinkRegex.Matches(document.Editor.Text))
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

        foreach (Match match in HtmlHrefLinkRegex.Matches(document.Editor.Text))
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

    private static List<string> ValidateDocument(ToolEditorDocument document)
    {
        var errors = new List<string>();
        if (document.ImageBytes is not null)
            return errors;

        var name = document.DisplayName;
        var extension = Path.GetExtension(name);
        var text = document.Editor.Text;

        if (!IsAllowedPackageDocumentPath(document.PackagePath))
            errors.Add("Package path is not allowed: " + document.PackagePath);

        if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            ValidateJson(text, errors);
        else if (extension.Equals(".lng", StringComparison.OrdinalIgnoreCase))
            ValidateLanguageFile(text, errors);
        else if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase) && !text.Contains('<'))
            errors.Add("HTML document does not contain markup.");

        if (string.IsNullOrWhiteSpace(text))
            errors.Add("Document is empty.");

        return errors;
    }

    private static bool IsAllowedPackageDocumentPath(string packagePath)
    {
        var path = NormalizePackagePath(packagePath);
        if (IsManifestPath(path))
            return true;
        if (path.StartsWith("language/", StringComparison.OrdinalIgnoreCase) &&
            Path.GetExtension(path).Equals(".lng", StringComparison.OrdinalIgnoreCase))
            return true;
        if ((path.StartsWith("manual/", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("help/content/main/", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("help/main/", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("help/content/nod/", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("help/nod/", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("nod/", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("formula/", StringComparison.OrdinalIgnoreCase)) &&
            IsHelpTextPath(path))
        {
            return true;
        }

        return false;
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
        var extension = Path.GetExtension(path);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".svg", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHelpTextPath(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".css", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".js", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHtmlDocument(ToolEditorDocument document)
    {
        return Path.GetExtension(document.PackagePath).Equals(".html", StringComparison.OrdinalIgnoreCase) ||
            document.Editor.Text.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
            document.Editor.Text.Contains("<img", StringComparison.OrdinalIgnoreCase);
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
        if (document.Highlighting || document.Editor.IsDisposed)
            return;

        document.HighlightTimer ??= new System.Windows.Forms.Timer
        {
            Interval = 180
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
        if (editor.IsDisposed || document.Highlighting || document.ImageBytes is not null)
            return;

        document.Highlighting = true;
        try
        {
            var selectionStart = editor.SelectionStart;
            var selectionLength = editor.SelectionLength;
            var text = editor.Text;

            editor.SuspendLayout();
            editor.SelectAll();
            editor.SelectionColor = Color.FromArgb(31, 41, 55);

            var extension = Path.GetExtension(document.PackagePath);
            if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase))
                HighlightHtml(editor, text);
            else if (extension.Equals(".css", StringComparison.OrdinalIgnoreCase))
                HighlightCss(editor, text);
            else if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
                HighlightJson(editor, text);
            else if (extension.Equals(".lng", StringComparison.OrdinalIgnoreCase))
                HighlightLanguage(editor, text);

            editor.Select(Math.Min(selectionStart, editor.TextLength), Math.Min(selectionLength, Math.Max(0, editor.TextLength - selectionStart)));
            editor.SelectionColor = Color.FromArgb(31, 41, 55);
            editor.ResumeLayout();
        }
        finally
        {
            document.Highlighting = false;
        }
    }

    private static void HighlightHtml(RichTextBox editor, string text)
    {
        ApplyMatches(editor, text, HtmlCommentRegex, Color.FromArgb(47, 128, 67));
        ApplyMatches(editor, text, HtmlTagRegex, Color.FromArgb(0, 74, 173));
        ApplyMatches(editor, text, HtmlAttributeRegex, Color.FromArgb(170, 72, 20), groupIndex: 1);
        ApplyMatches(editor, text, QuotedStringRegex, Color.FromArgb(126, 82, 0));
    }

    private static void HighlightCss(RichTextBox editor, string text)
    {
        ApplyMatches(editor, text, CssSelectorRegex, Color.FromArgb(96, 64, 160), groupIndex: 2);
        ApplyMatches(editor, text, HtmlAttributeRegex, Color.FromArgb(170, 72, 20), groupIndex: 1);
        ApplyMatches(editor, text, QuotedStringRegex, Color.FromArgb(126, 82, 0));
    }

    private static void HighlightJson(RichTextBox editor, string text)
    {
        ApplyMatches(editor, text, JsonPropertyRegex, Color.FromArgb(0, 74, 173));
        ApplyMatches(editor, text, QuotedStringRegex, Color.FromArgb(126, 82, 0));
        ApplyMatches(editor, text, JsonPropertyRegex, Color.FromArgb(0, 74, 173));
    }

    private static void HighlightLanguage(RichTextBox editor, string text)
    {
        ApplyMatches(editor, text, LanguageKeyRegex, Color.FromArgb(0, 74, 173));
        ApplyMatches(editor, text, new Regex(@"^[ \t]*[#;].*$", RegexOptions.Multiline), Color.FromArgb(47, 128, 67));
    }

    private static void ApplyMatches(RichTextBox editor, string text, Regex regex, Color color, int groupIndex = 0)
    {
        foreach (Match match in regex.Matches(text))
        {
            var group = match.Groups[groupIndex];
            if (!group.Success || group.Length == 0)
                continue;

            editor.Select(group.Index, group.Length);
            editor.SelectionColor = color;
        }
    }

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

    private async void UpdatePreview()
    {
        if (_current is null)
        {
            SetHtml(WrapHtml("ToolEditor", "<p>No document selected.</p>"));
            return;
        }

        if (_current.HtmlEditMode)
            return;

        await SyncHtmlEditorToSourceAsync();
        SetHtml(BuildPreviewHtml(_current));
    }

    private string BuildPreviewHtml(ToolEditorDocument document)
    {
        var extension = Path.GetExtension(document.DisplayName);
        if (document.ImageBytes is not null)
            return WrapImageHtml(BuildImagePreview(document));

        var text = document.Editor.Text;
        if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase) || text.Contains("<html", StringComparison.OrdinalIgnoreCase))
            return WrapContentHtml(ResolveMediaLinksForPreview(text));
        if (extension.Equals(".lng", StringComparison.OrdinalIgnoreCase))
            return WrapHtml(document.DisplayName, BuildLanguagePreview(text));
        if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            return WrapHtml(document.DisplayName, BuildJsonPreview(text));

        return WrapHtml(document.DisplayName, "<pre>" + WebUtility.HtmlEncode(text) + "</pre>");
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
        SetHtmlEditorHtml(BuildEditableHtml(ResolveMediaLinksForEditor(html)));
    }

    private void SetHtmlEditorHtml(string html)
    {
        _loadingHtmlEditor = true;
        _pendingHtmlEditor = html;
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

    private static string BuildEditableHtml(string body)
    {
        var css = HelpApi.NodHelpCss() + Environment.NewLine +
            ToolEditorConceptBannerCss() + Environment.NewLine +
            "body:focus { outline: 2px solid #9cc4ff; outline-offset: 4px; }";
        return ApplyToolEditorHelpPlaceholders(HelpHtml.WrapBodyPage(body, css, bodyTail: ToolEditorHtmlEditScript()));
    }

    private async Task SyncHtmlEditorToSourceAsync()
    {
        if (_current is null || !_current.HtmlEditMode || _loadingHtmlEditor || _htmlEditor.CoreWebView2 is null)
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

            var cleaned = Regex.Replace(html, @"\s*<script>[\s\S]*?</script>\s*$", "", RegexOptions.IgnoreCase).Trim();
            _current.Editor.Text = cleaned;
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
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed.StartsWith(';'))
                continue;

            var index = trimmed.IndexOf('=');
            var key = index < 0 ? trimmed : trimmed[..index];
            var value = index < 0 ? "" : trimmed[(index + 1)..];
            rows.Append("<tr><th>")
                .Append(WebUtility.HtmlEncode(key))
                .Append("</th><td>")
                .Append(WebUtility.HtmlEncode(value))
                .Append("</td></tr>");
        }

        return rows.Length == 0
            ? "<p>No language keys found.</p>"
            : "<table>" + rows + "</table>";
    }

    private static string WrapHtml(string title, string body)
    {
        var css = HelpApi.NodHelpCss() + Environment.NewLine + """
        .media-meta { color: #334155; margin-bottom: 14px; }
        .image-preview { min-height: 360px; border: 1px solid #d7e0ec; background: #f8fafc; display: flex; align-items: center; justify-content: center; padding: 18px; }
        .image-preview img { max-width: 100%; max-height: 70vh; object-fit: contain; box-shadow: 0 8px 24px rgba(15, 23, 42, .15); background: white; }
        """ + Environment.NewLine + ToolEditorConceptBannerCss();
        return ApplyToolEditorHelpPlaceholders(HelpHtml.WrapTopicPage(title, body, css, ToolEditorHelpPreviewScript()));
    }

    private static string WrapContentHtml(string body)
    {
        var css = HelpApi.NodHelpCss() + Environment.NewLine + ToolEditorConceptBannerCss();
        return ApplyToolEditorHelpPlaceholders(HelpHtml.WrapBodyPage(body, css, bodyTail: ToolEditorHelpPreviewScript()));
    }

    private static string ToolEditorConceptBannerCss()
    {
        return """
        .concept-banner { display: flex; align-items: center; justify-content: space-between; gap: 16px; margin: 0 0 18px; padding: 10px 12px; border: 1px solid #f1c232; border-left: 5px solid #d69400; border-radius: 6px; background: #fff4bf; color: #3f2f12; box-shadow: 0 1px 2px rgba(15, 23, 42, .08); }
        .concept-banner b { color: #7a4a00; }
        .concept-banner-close { flex: 0 0 auto; width: 24px; height: 24px; border: 1px solid #d6a318; border-radius: 50%; background: #fff9dc; color: #6b4500; font: 700 14px/20px "Segoe UI", Arial, sans-serif; cursor: pointer; }
        .concept-banner-close:hover { background: #ffe98f; border-color: #b77900; }
        """;
    }

    private static string ToolEditorHtmlEditScript()
    {
        return """
        <script>
        document.body.contentEditable = 'true';
        document.addEventListener('click', event => {
          const close = event.target && event.target.closest ? event.target.closest('.concept-banner-close') : null;
          if (!close) return;
          event.preventDefault();
          close.closest('.concept-banner')?.remove();
          window.chrome.webview.postMessage('changed');
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
        document.body.focus();
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

    private static string ApplyToolEditorHelpPlaceholders(string html)
    {
        return html
            .Replace("[menu.edit.copy]", "Kopieren", StringComparison.Ordinal)
            .Replace("[help.copy.copied]", "Gekopieerd", StringComparison.Ordinal);
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
        try
        {
            await _preview.EnsureCoreWebView2Async();
            ShowPendingHtmlIfReady();
        }
        catch (COMException)
        {
            _browserFailed = true;
        }
        catch (ObjectDisposedException)
        {
            _browserFailed = true;
        }
        catch (Exception ex)
        {
            _browserFailed = true;
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
        try
        {
            await _htmlEditor.EnsureCoreWebView2Async();
            ShowPendingHtmlEditorIfReady();
        }
        catch (COMException)
        {
            _htmlEditorFailed = true;
        }
        catch (ObjectDisposedException)
        {
            _htmlEditorFailed = true;
        }
        catch (Exception ex)
        {
            _htmlEditorFailed = true;
            _htmlEditor.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "HTML editor could not start.\r\n" + ex.Message
            });
        }
    }

    private void SetHtml(string html)
    {
        _pendingHtml = html;
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

        if (message.Equals("changed", StringComparison.Ordinal) && _current is not null && _current.HtmlEditMode)
            SetDirty(_current, true);
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

        CopyHelpMessageToClipboard(message);
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
            SetStatus("Link: " + link + " -> " + target.PackagePath, isError: false);
            return;
        }

        SetStatus("Linkdoel niet gevonden: " + link, isError: true);
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
            UpdatePreview();
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

    private sealed class ToolEditorDocument(TabPage page, string displayName, string packagePath, string? filePath)
    {
        public TabPage Page { get; } = page;
        public string DisplayName { get; set; } = displayName;
        public string PackagePath { get; set; } = packagePath;
        public string TreeGroup { get; set; } = "";
        public string TreeTopic { get; set; } = "";
        public string? FilePath { get; set; } = filePath;
        public RichTextBox Editor { get; set; } = null!;
        public byte[]? ImageBytes { get; set; }
        public LineNumberPanel? LineNumbers { get; set; }
        public Panel HeaderPanel { get; set; } = null!;
        public Label HeaderTitle { get; set; } = null!;
        public System.Windows.Forms.Timer? HighlightTimer { get; set; }
        public bool Highlighting { get; set; }
        public bool Dirty { get; set; }
        public bool ReadOnly { get; set; }
        public bool IsOpen { get; set; }
        public bool HtmlEditMode { get; set; }
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
        bool Signed);

    private sealed record LanguagePackageCompileResult(
        string OutputPath,
        string PackageSha256,
        string PayloadSha256);

    private sealed class LineNumberPanel : Panel
    {
        private RichTextBox? _editor;

        public LineNumberPanel()
        {
            Dock = DockStyle.Left;
            Width = 52;
            BackColor = Color.FromArgb(248, 250, 252);
            ForeColor = Color.FromArgb(100, 116, 139);
            DoubleBuffered = true;
        }

        public void Attach(RichTextBox editor)
        {
            _editor = editor;
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
            var lastLine = Math.Min(_editor.Lines.Length - 1, _editor.GetLineFromCharIndex(lastIndex) + 1);

            for (var line = firstLine; line <= lastLine; line++)
            {
                var charIndex = _editor.GetFirstCharIndexFromLine(line);
                if (charIndex < 0)
                    continue;

                var position = _editor.GetPositionFromCharIndex(charIndex);
                var text = (line + 1).ToString();
                var size = e.Graphics.MeasureString(text, Font);
                e.Graphics.DrawString(text, Font, brush, Width - size.Width - 7, position.Y);
            }
        }
    }
}
