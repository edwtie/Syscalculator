#nullable enable
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Tiedragon.ToolEditor;

// Copyright (c) Tiedragon. All rights reserved.
//
// ToolEditor is the shared editing surface for Tiedragon package and help tools.
// The first version mirrors the NOD Editor structure: central toolbar, custom
// document tabs, source editor and HTML preview.
public sealed class ToolEditorForm : Form
{
    private static readonly Regex HtmlCommentRegex = new("<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex HtmlTagRegex = new("</?[a-zA-Z][^>]*?>", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex HtmlAttributeRegex = new(@"\s([a-zA-Z_:][-a-zA-Z0-9_:.]*)(?=\s*=)", RegexOptions.Compiled);
    private static readonly Regex QuotedStringRegex = new("(\"[^\"]*\"|'[^']*')", RegexOptions.Compiled);
    private static readonly Regex JsonPropertyRegex = new("\"[^\"\\r\\n]*\"(?=\\s*:)", RegexOptions.Compiled);
    private static readonly Regex CssSelectorRegex = new(@"(^|\})([^{]+)(?=\{)", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex LanguageKeyRegex = new(@"^[^#;\r\n=]+(?=\=)", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex HtmlMediaLinkRegex = new("(?:src|href)\\s*=\\s*[\"'](?<path>[^\"']+\\.(?:png|jpg|jpeg|svg))[\"']", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
        _saveButton = ToolEditorApi.CreateButton("Save", ToolEditorIcon.Save, (_, _) => SaveCurrent(), "Save current document");
        _validateButton = ToolEditorApi.CreateButton("Validate", ToolEditorIcon.Validate, (_, _) => ValidateCurrent(showMessage: true), "Validate current document");
        _previewButton = ToolEditorApi.CreateButton("Preview", ToolEditorIcon.Test, (_, _) => UpdatePreview(), "Refresh HTML preview");
        toolbar.Items.Add(_saveButton);
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(_validateButton);
        toolbar.Items.Add(_previewButton);

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
            _htmlEditor.CoreWebView2.WebMessageReceived += (_, _) =>
            {
                if (_current is not null && _current.HtmlEditMode)
                    SetDirty(_current, true);
            };
            ShowPendingHtmlEditorIfReady();
        };
        _htmlEditor.NavigationCompleted += (_, _) => _loadingHtmlEditor = false;
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
        file.DropDownItems.Add(CreateMenuItem("Opslaan", Keys.Control | Keys.S, (_, _) => SaveCurrent()));
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
        package.DropDownItems.Add("HTML-media links controleren", null, (_, _) => ValidatePackageMediaLinks(showMessage: true));
        package.DropDownItems.Add(new ToolStripSeparator());
        package.DropDownItems.Add("Media bekijken", null, (_, _) => SelectFirstGroup("Media en afbeeldingen"));

        var help = new ToolStripMenuItem("Help");
        help.DropDownItems.Add("ToolEditor", null, (_, _) => MessageBox.Show(this, "ToolEditor bewerkt taalpakketten, help-HTML, NOD-onderwerpen, formulekaarten en media.", "ToolEditor"));

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
        _manifestText = BuildManifestTemplate();
        var languageDocument = AddDocument("language/ned.lng", LoadDutchLanguageText(), null);
        AddDutchHelpDocuments();
        SelectDocument(languageDocument);
    }

    private static string BuildManifestTemplate()
    {
        return """
        {
          "format": 1,
          "key": "ned",
          "id": "tiedragon.language.ned",
          "producer": "Tiedragon",
          "product": "Syscalculator",
          "softwareId": "tiedragon.syscalculator",
          "languageCode": "ned",
          "displayName": "Nederlands",
          "nativeName": "Nederlands",
          "packageVersion": "2026.05.21.001",
          "fallbackLanguage": "eng"
        }
        """;
    }

    private static string BuildHtmlTemplate()
    {
        return """
        <h1>Syscalculator Help</h1>
        <p>Er zijn nog geen Nederlandse helpbestanden gevonden.</p>
        <div class="help-warning">Controleer of de helpbronnen in de repository aanwezig zijn.</div>
        """;
    }

    private void AddDutchHelpDocuments()
    {
        var helpDirectory = FindRepositoryPath("legacy/Syscalculator174.VB6/help");
        if (helpDirectory is null)
        {
            AddDocument("manual/index.html", BuildHtmlTemplate(), null);
            return;
        }

        var helpFiles = Directory.GetFiles(helpDirectory, "*.htm")
            .Where(path => Path.GetFileName(path).Equals("index_nl.htm", StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(path).StartsWith("help_nl_", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => Path.GetFileName(path).Equals("index_nl.htm", StringComparison.OrdinalIgnoreCase) ? "" : Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (helpFiles.Count == 0)
        {
            AddDocument("manual/index.html", BuildHtmlTemplate(), null);
            return;
        }

        var pageNameMap = helpFiles.ToDictionary(
            path => Path.GetFileName(path),
            path => Path.GetFileName(path).Equals("index_nl.htm", StringComparison.OrdinalIgnoreCase)
                ? "index.html"
                : Path.GetFileNameWithoutExtension(path) + ".html",
            StringComparer.OrdinalIgnoreCase);

        var mediaNameMap = AddDutchHelpMedia(helpDirectory);
        foreach (var file in helpFiles)
        {
            var packageName = pageNameMap[Path.GetFileName(file)];
            var html = File.ReadAllText(file, Encoding.Latin1);
            html = ConvertDutchHelpHtml(html, pageNameMap, mediaNameMap);
            AddDocument("manual/" + packageName, html, file);
        }
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

    private static string LoadDutchLanguageText()
    {
        var path = FindRepositoryPath("src/syscalculator/ned.lng");
        if (path is not null)
            return File.ReadAllText(path, Encoding.UTF8);

        return """
        # Syscalculator 2.0 taalbestand
        # Formaat: key=waarde

        language.name=Nederlands
        menu.tools.tool_editor=ToolEditor
        status.ready=Gereed
        """;
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
            Filter = "Language package files (*.html;*.json;*.lng;*.css;*.png;*.jpg;*.jpeg;*.svg)|*.html;*.json;*.lng;*.css;*.png;*.jpg;*.jpeg;*.svg|All files (*.*)|*.*"
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
            extension.Equals(".css", StringComparison.OrdinalIgnoreCase))
        {
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
            "\r\n\r\nToegestaan: manifest.json, language/*.lng, manual/help/NOD/formule HTML/CSS, en media png/jpg/jpeg/svg.";
        SetStatus("Bestand geweigerd: " + Path.GetFileName(fileName), isError: true);
        MessageBox.Show(this, message, "ToolEditor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private async void SaveCurrent()
    {
        if (_current is null)
            return;

        if (_current.ReadOnly)
        {
            SetStatus("Alleen tonen: " + _current.DisplayName, isError: true);
            return;
        }

        await SyncHtmlEditorToSourceAsync();

        var path = _current.FilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Save ToolEditor document",
                FileName = _current.DisplayName.Replace('/', Path.DirectorySeparatorChar),
                Filter = "Language package files (*.html;*.json;*.lng;*.css;*.png;*.jpg;*.jpeg;*.svg)|*.html;*.json;*.lng;*.css;*.png;*.jpg;*.jpeg;*.svg|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            path = dialog.FileName;
            _current.FilePath = path;
            _current.DisplayName = Path.GetFileName(path);
            _current.PackagePath = NormalizePackagePath(_current.DisplayName);
            ApplyDocumentLabels(_current);
            RefreshFileTree();
            RefreshDocumentList();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (_current.ImageBytes is not null)
            File.WriteAllBytes(path, _current.ImageBytes);
        else
            File.WriteAllText(path, _current.Editor.Text, Encoding.UTF8);
        SetDirty(_current, false);
        SetStatus("Saved: " + path, isError: false);
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
            if (_current?.Editor == editor && !document.ReadOnly)
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

    private void CloseDocument(ToolEditorDocument document)
    {
        document.IsOpen = false;
        _tabStrip.Controls.Remove(document.HeaderPanel);

        if (ReferenceEquals(_current, document))
        {
            _current = _documents.LastOrDefault(item => item.IsOpen);
            _editorContent.Controls.Clear();
            if (_current is not null)
                SelectDocument(_current);
            else
                UpdatePreview();
        }

        RefreshTabStrip();
        UpdateUiState();
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

        if (path.StartsWith("help/content/nod/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("help/nod/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("nod/", StringComparison.OrdinalIgnoreCase))
        {
            return ["NOD voor ontwikkelaars", topic];
        }

        if (path.StartsWith("formula/", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("formula", StringComparison.OrdinalIgnoreCase))
        {
            return ["Formulekaart", topic];
        }

        if (path.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) || IsImagePath(path))
            return ["Media en afbeeldingen", fileName];

        return ["Ongeldig pakketbestand", topic];
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

        if (name.Equals("index", StringComparison.OrdinalIgnoreCase))
            return "Startpagina";
        if (name.Equals("readme", StringComparison.OrdinalIgnoreCase))
            return "Overzicht";

        return string.Join(
            ' ',
            name.Replace('_', '-')
                .Split('-', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
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

    private void ValidatePackageMediaLinks(bool showMessage)
    {
        var errors = new List<string>();
        foreach (var document in _documents)
            AddMissingMediaLinkErrors(document, errors);

        if (errors.Count == 0)
        {
            SetStatus("Alle HTML-media links verwijzen naar bestaande package-media.", isError: false);
            if (showMessage)
                MessageBox.Show(this, "Alle HTML-media links zijn gevonden in het package.", "ToolEditor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SetStatus("HTML-media link ontbreekt: " + errors[0], isError: true);
        if (showMessage)
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Ontbrekende media", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            extension.Equals(".css", StringComparison.OrdinalIgnoreCase);
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
            var link = match.Groups["path"].Value;
            var media = FindMediaDocument(link);
            if (media?.ImageBytes is null)
                return match.Value;

            var dataUri = "data:" + ImageMimeType(media.PackagePath) + ";base64," + Convert.ToBase64String(media.ImageBytes);
            return match.Value.Replace(link, dataUri);
        });
    }

    private void SetHtmlEditor(string html)
    {
        SetHtmlEditorHtml(BuildEditableHtml(html));
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
        return $$"""
        <!doctype html>
        <html>
        <head>
          <meta charset="utf-8">
          <style>
            body { font-family: "Segoe UI", Arial, sans-serif; font-size: 14px; margin: 16px; color: #1f2937; background: #fff; }
            body:focus { outline: 2px solid #9cc4ff; outline-offset: 4px; }
            h1 { font-size: 22px; color: #0f3f8f; }
            .notice { border-left: 4px solid #1d70d8; background: #eff6ff; padding: 10px 12px; margin: 10px 0; }
            .help-info, .help-tip, .help-warning { border-left: 4px solid; padding: 10px 12px; margin: 10px 0; }
            .help-info { border-color: #1d70d8; background: #eff6ff; }
            .help-tip { border-color: #16803c; background: #ecfdf3; }
            .help-warning { border-color: #c2410c; background: #fff7ed; }
            pre { white-space: pre-wrap; font-family: Consolas, monospace; background: #f8fafc; border: 1px solid #d7e0ec; padding: 12px; }
            code { font-family: Consolas, monospace; }
            kbd { font-family: Consolas, monospace; border: 1px solid #cbd5e1; border-bottom-width: 2px; border-radius: 4px; background: #f8fafc; padding: 1px 5px; }
            img { max-width: 100%; height: auto; }
          </style>
        </head>
        <body contenteditable="true">
          {{body}}
          <script>
            document.body.addEventListener('input', () => window.chrome.webview.postMessage('changed'));
            document.body.focus();
          </script>
        </body>
        </html>
        """;
    }

    private async Task SyncHtmlEditorToSourceAsync()
    {
        if (_current is null || !_current.HtmlEditMode || _loadingHtmlEditor || _htmlEditor.CoreWebView2 is null)
            return;

        try
        {
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

    private static string BuildImagePreview(ToolEditorDocument document)
    {
        if (document.ImageBytes is null)
            return "<p>No image loaded.</p>";

        var mime = ImageMimeType(document.PackagePath);
        var base64 = Convert.ToBase64String(document.ImageBytes);
        var fileName = WebUtility.HtmlEncode(Path.GetFileName(document.PackagePath));
        return $$"""
        <div class="image-preview">
          <img src="data:{{mime}};base64,{{base64}}" alt="{{fileName}}">
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
        return $$"""
        <!doctype html>
        <html>
        <head>
          <meta charset="utf-8">
          <style>
            body { font-family: "Segoe UI", Arial, sans-serif; font-size: 14px; margin: 18px 22px; color: #1f2937; background: #fff; }
            h1 { font-size: 22px; margin: 0 0 14px; color: #0f3f8f; }
            table { border-collapse: collapse; width: 100%; }
            th, td { border: 1px solid #d7e0ec; padding: 6px 8px; vertical-align: top; }
            th { width: 220px; text-align: left; background: #f3f7fc; color: #173b70; }
            pre { white-space: pre-wrap; font-family: Consolas, monospace; background: #f8fafc; border: 1px solid #d7e0ec; padding: 12px; }
            .notice { border-left: 4px solid #1d70d8; background: #eff6ff; padding: 10px 12px; margin: 10px 0; }
            .help-info, .help-tip, .help-warning { border-left: 4px solid; padding: 10px 12px; margin: 10px 0; }
            .help-info { border-color: #1d70d8; background: #eff6ff; }
            .help-tip { border-color: #16803c; background: #ecfdf3; }
            .help-warning { border-color: #c2410c; background: #fff7ed; }
            code { font-family: Consolas, monospace; }
            kbd { font-family: Consolas, monospace; border: 1px solid #cbd5e1; border-bottom-width: 2px; border-radius: 4px; background: #f8fafc; padding: 1px 5px; }
            .media-meta { color: #334155; margin-bottom: 14px; }
            .image-preview { min-height: 360px; border: 1px solid #d7e0ec; background: #f8fafc; display: flex; align-items: center; justify-content: center; padding: 18px; }
            .image-preview img { max-width: 100%; max-height: 70vh; object-fit: contain; box-shadow: 0 8px 24px rgba(15, 23, 42, .15); background: white; }
          </style>
        </head>
        <body>
          <h1>{{WebUtility.HtmlEncode(title)}}</h1>
          {{body}}
        </body>
        </html>
        """;
    }

    private static string WrapContentHtml(string body)
    {
        return $$"""
        <!doctype html>
        <html>
        <head>
          <meta charset="utf-8">
          <style>
            body { font-family: "Segoe UI", Arial, sans-serif; font-size: 14px; margin: 18px 22px; color: #1f2937; background: #fff; }
            h1 { font-size: 22px; margin: 0 0 14px; color: #0f3f8f; }
            table { border-collapse: collapse; width: 100%; }
            th, td { border: 1px solid #d7e0ec; padding: 6px 8px; vertical-align: top; }
            th { width: 220px; text-align: left; background: #f3f7fc; color: #173b70; }
            pre { white-space: pre-wrap; font-family: Consolas, monospace; background: #f8fafc; border: 1px solid #d7e0ec; padding: 12px; }
            .notice { border-left: 4px solid #1d70d8; background: #eff6ff; padding: 10px 12px; margin: 10px 0; }
            .help-info, .help-tip, .help-warning { border-left: 4px solid; padding: 10px 12px; margin: 10px 0; }
            .help-info { border-color: #1d70d8; background: #eff6ff; }
            .help-tip { border-color: #16803c; background: #ecfdf3; }
            .help-warning { border-color: #c2410c; background: #fff7ed; }
            code { font-family: Consolas, monospace; }
            kbd { font-family: Consolas, monospace; border: 1px solid #cbd5e1; border-bottom-width: 2px; border-radius: 4px; background: #f8fafc; padding: 1px 5px; }
            img { max-width: 100%; height: auto; }
          </style>
        </head>
        <body>
          {{body}}
        </body>
        </html>
        """;
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
            .image-preview { flex: 1 1 auto; min-height: 0; width: 100%; box-sizing: border-box; background: #f8fafc; display: flex; align-items: center; justify-content: center; padding: 0; overflow: auto; }
            .image-preview img { max-width: 100%; max-height: 100%; object-fit: contain; box-shadow: 0 8px 24px rgba(15, 23, 42, .15); background: white; }
          </style>
        </head>
        <body>
          {{body}}
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
        _saveButton.Enabled = hasDocument && _current?.ReadOnly != true;
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
