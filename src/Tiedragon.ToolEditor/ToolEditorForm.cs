#nullable enable
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
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
    private readonly TreeView _fileTree;
    private readonly FlowLayoutPanel _tabStrip;
    private readonly Panel _editorContent;
    private readonly WebView2 _preview;
    private readonly Label _statusLabel;
    private readonly ToolStripButton _saveButton;
    private readonly ToolStripButton _validateButton;
    private readonly ToolStripButton _previewButton;
    private readonly List<ToolEditorDocument> _documents = [];
    private string? _pendingHtml;
    private bool _browserFailed;
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

        var toolbar = ToolEditorApi.CreateToolbar();
        toolbar.Items.Add(ToolEditorApi.CreateButton("New", ToolEditorIcon.New, (_, _) => NewLanguagePackageTemplate(), "New language package template"));
        toolbar.Items.Add(ToolEditorApi.CreateButton("Open", ToolEditorIcon.Open, (_, _) => OpenDocument(), "Open text, HTML, JSON or .lng file"));
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
            Font = new Font("Segoe UI", 9),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(31, 41, 55)
        };
        _fileTree.AfterSelect += FileTree_AfterSelect;

        var fileTreeHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(203, 213, 225),
            Padding = new Padding(1)
        };
        fileTreeHost.Controls.Add(_fileTree);

        _tabStrip = ToolEditorTabsApi.CreateStrip();
        _tabStrip.Dock = DockStyle.Fill;
        _tabStrip.WrapContents = false;
        _tabStrip.AutoScroll = true;
        _tabStrip.BackColor = Color.White;
        _tabStrip.Padding = new Padding(0, 2, 0, 0);
        _tabStrip.Margin = Padding.Empty;

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
            RowCount = 2,
            BackColor = Color.White,
            Padding = new Padding(4, 4, 4, 0)
        };
        editorHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editorHost.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        editorHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        editorHost.Controls.Add(_tabStrip, 0, 0);
        editorHost.Controls.Add(_editorContent, 0, 1);

        _preview = new WebView2
        {
            Dock = DockStyle.Fill,
            AllowExternalDrop = false,
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = GetWebView2UserDataFolder()
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

        var editorSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            SplitterWidth = 5,
            BackColor = Color.FromArgb(226, 232, 240),
            FixedPanel = FixedPanel.Panel1,
            Panel1MinSize = 1,
            Panel2MinSize = 1
        };
        editorSplit.Panel1.Padding = new Padding(4, 4, 0, 0);
        editorSplit.Panel1.Controls.Add(fileTreeHost);
        editorSplit.Panel2.Controls.Add(editorHost);
        editorSplit.SizeChanged += (_, _) => ClampSplitter(editorSplit, 220);
        Shown += (_, _) => ClampSplitter(editorSplit, 220);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            SplitterWidth = 5,
            BackColor = Color.FromArgb(226, 232, 240),
            Panel1MinSize = 1,
            Panel2MinSize = 1
        };
        split.Panel1.Controls.Add(editorSplit);
        split.Panel2.Padding = new Padding(0, 4, 4, 0);
        split.Panel2.Controls.Add(previewHost);
        split.SizeChanged += (_, _) => ClampSplitter(split);
        Shown += (_, _) => ClampSplitter(split);

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

        NewLanguagePackageTemplate();
        RefreshFileTree();
        UpdateUiState();
    }

    private void NewLanguagePackageTemplate()
    {
        AddDocument("manifest.json", BuildManifestTemplate(), null);
        AddDocument("language/ned.lng", "app.title=Syscalculator\r\n", null);
        AddDocument("manual/index.html", BuildHtmlTemplate(), null);
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
        <h1>Tiedragon language package</h1>
        <p>Write package help, manual text or release notes here.</p>
        <div class="notice">This page is previewed through the ToolEditor HTML viewer.</div>
        """;
    }

    private void OpenDocument()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open ToolEditor document",
            Filter = "Tool documents (*.html;*.json;*.lng;*.css;*.txt)|*.html;*.json;*.lng;*.css;*.txt|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        AddDocument(Path.GetFileName(dialog.FileName), File.ReadAllText(dialog.FileName), dialog.FileName);
    }

    private void SaveCurrent()
    {
        if (_current is null)
            return;

        var path = _current.FilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Save ToolEditor document",
                FileName = _current.DisplayName.Replace('/', Path.DirectorySeparatorChar),
                Filter = "Tool documents (*.html;*.json;*.lng;*.css;*.txt)|*.html;*.json;*.lng;*.css;*.txt|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            path = dialog.FileName;
            _current.FilePath = path;
            _current.DisplayName = Path.GetFileName(path);
            _current.PackagePath = NormalizePackagePath(_current.DisplayName);
            RefreshFileTree();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, _current.Editor.Text, Encoding.UTF8);
        SetDirty(_current, false);
        SetStatus("Saved: " + path, isError: false);
    }

    private void AddDocument(string displayName, string text, string? filePath)
    {
        var page = new TabPage(displayName);
        var editor = CreateTextEditor(text);
        var document = new ToolEditorDocument(page, displayName, NormalizePackagePath(displayName), filePath, editor);
        var header = ToolEditorTabsApi.CreateHeader(
            page,
            (_, _) => SelectDocument(document),
            (_, _) => CloseDocument(document));

        document.HeaderPanel = header.Panel;
        document.HeaderTitle = header.Title;
        _documents.Add(document);
        _tabStrip.Controls.Add(header.Panel);
        RefreshFileTree();
        SelectDocument(document);
    }

    private RichTextBox CreateTextEditor(string text)
    {
        var editor = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 10),
            WordWrap = false,
            AcceptsTab = true,
            HideSelection = false,
            Text = text
        };
        editor.TextChanged += (_, _) =>
        {
            if (_current?.Editor == editor)
                SetDirty(_current, true);
        };
        return editor;
    }

    private void SelectDocument(ToolEditorDocument document)
    {
        _current = document;
        SelectDocumentInTree(document);
        _editorContent.SuspendLayout();
        _editorContent.Controls.Clear();
        _editorContent.Controls.Add(document.Editor);
        _editorContent.ResumeLayout();
        RefreshTabStrip();
        UpdatePreview();
        UpdateUiState();
    }

    private void CloseDocument(ToolEditorDocument document)
    {
        if (document.Dirty)
        {
            var result = MessageBox.Show(
                this,
                "This document has unsaved changes. Close anyway?",
                "ToolEditor",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
                return;
        }

        _tabStrip.Controls.Remove(document.HeaderPanel);
        _documents.Remove(document);
        RemoveDocumentFromTree(document);
        document.Editor.Dispose();
        document.Page.Dispose();

        if (_current == document)
        {
            _current = _documents.LastOrDefault();
            _editorContent.Controls.Clear();
            if (_current is not null)
                SelectDocument(_current);
            else
                UpdateUiState();
        }

        RefreshTabStrip();
        RefreshFileTree();
    }

    private void RefreshFileTree()
    {
        if (_fileTree.IsDisposed)
            return;

        _fileTree.BeginUpdate();
        _fileTree.Nodes.Clear();
        var root = new TreeNode("Language package")
        {
            NodeFont = new Font(_fileTree.Font, FontStyle.Bold)
        };
        _fileTree.Nodes.Add(root);

        foreach (var document in _documents.OrderBy(document => document.PackagePath, StringComparer.OrdinalIgnoreCase))
            AddDocumentNode(root, document);

        root.ExpandAll();
        _fileTree.EndUpdate();
        if (_current is not null)
            SelectDocumentInTree(_current);
    }

    private static void AddDocumentNode(TreeNode root, ToolEditorDocument document)
    {
        var parts = BuildTreePath(document);
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
    }

    private static IReadOnlyList<string> BuildTreePath(ToolEditorDocument document)
    {
        var path = document.PackagePath.Replace('\\', '/').Trim('/');
        var fileName = Path.GetFileName(path);
        var topic = FriendlyTopicName(fileName);

        if (path.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
            return ["Taal: Nederlands", "Interne pakketgegevens"];

        if (path.StartsWith("language/", StringComparison.OrdinalIgnoreCase))
            return ["Taal: " + FriendlyLanguageName(Path.GetFileNameWithoutExtension(fileName)), "Vertalingen"];

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

        if (path.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
            return ["Media en afbeeldingen", topic];

        return ["Overige inhoud", topic];
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
        if (e.Node?.Tag is ToolEditorDocument document && !ReferenceEquals(document, _current))
            SelectDocument(document);
    }

    private void ValidateCurrent(bool showMessage)
    {
        if (_current is null)
            return;

        var errors = ValidateDocument(_current);
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

    private static List<string> ValidateDocument(ToolEditorDocument document)
    {
        var errors = new List<string>();
        var name = document.DisplayName;
        var extension = Path.GetExtension(name);
        var text = document.Editor.Text;

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

    private static string NormalizePackagePath(string value)
    {
        return value.Replace('\\', '/').Trim('/');
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

    private void UpdatePreview()
    {
        if (_current is null)
        {
            SetHtml(WrapHtml("ToolEditor", "<p>No document selected.</p>"));
            return;
        }

        SetHtml(BuildPreviewHtml(_current));
    }

    private static string BuildPreviewHtml(ToolEditorDocument document)
    {
        var extension = Path.GetExtension(document.DisplayName);
        var text = document.Editor.Text;
        if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase) || text.Contains("<html", StringComparison.OrdinalIgnoreCase))
            return WrapHtml(document.DisplayName, text);
        if (extension.Equals(".lng", StringComparison.OrdinalIgnoreCase))
            return WrapHtml(document.DisplayName, BuildLanguagePreview(text));
        if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            return WrapHtml(document.DisplayName, BuildJsonPreview(text));

        return WrapHtml(document.DisplayName, "<pre>" + WebUtility.HtmlEncode(text) + "</pre>");
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
          </style>
        </head>
        <body>
          <h1>{{WebUtility.HtmlEncode(title)}}</h1>
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

    private static string GetWebView2UserDataFolder()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var path = Path.Combine(root, "Syscalculator", "ToolEditor", "WebView2");
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
        foreach (var document in _documents)
        {
            var title = document.DisplayName + (document.Dirty ? " *" : "");
            ToolEditorTabsApi.SetHeaderState(document.HeaderPanel, document.HeaderTitle, title, ReferenceEquals(document, _current), document.Dirty, Font);
        }

        _tabStrip.Invalidate();
    }

    private void UpdateUiState()
    {
        var hasDocument = _current is not null;
        _saveButton.Enabled = hasDocument;
        _validateButton.Enabled = hasDocument;
        _previewButton.Enabled = hasDocument;
    }

    private void SetStatus(string text, bool isError)
    {
        _statusLabel.Text = text;
        _statusLabel.ForeColor = isError ? Color.FromArgb(170, 35, 35) : Color.FromArgb(31, 41, 55);
    }

    private static void ClampSplitter(SplitContainer split, int? preferredDistance = null)
    {
        if (split.Width <= split.SplitterWidth + 2)
            return;

        var min = Math.Max(1, split.Panel1MinSize);
        var max = split.Width - split.SplitterWidth - Math.Max(1, split.Panel2MinSize);
        if (max < min)
            return;

        var target = Math.Clamp(preferredDistance ?? split.Width / 2, min, max);
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
        }
    }

    private sealed class ToolEditorDocument(TabPage page, string displayName, string packagePath, string? filePath, RichTextBox editor)
    {
        public TabPage Page { get; } = page;
        public string DisplayName { get; set; } = displayName;
        public string PackagePath { get; set; } = packagePath;
        public string? FilePath { get; set; } = filePath;
        public RichTextBox Editor { get; } = editor;
        public Panel HeaderPanel { get; set; } = null!;
        public Label HeaderTitle { get; set; } = null!;
        public bool Dirty { get; set; }
    }
}
