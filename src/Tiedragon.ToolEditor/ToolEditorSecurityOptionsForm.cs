#nullable enable
using System.Diagnostics;
using Tiedragon.LanguagePackage;

namespace Tiedragon.ToolEditor;

internal sealed class ToolEditorSecurityOptionsForm : Form
{
    private readonly Label _categoryTitle;
    private readonly Label _categoryDescription;
    private readonly ListView _values;
    private readonly ListBox _categoryList;
    private Dictionary<string, PolicyCategory> _categories;
    private Dictionary<string, List<string>> _policyValues;
    private readonly string _policyPath;
    private readonly Func<string, string, string> _t;
    private string _selectedCategory = "";
    private bool _policyDirty;
    private bool _closingAfterPrompt;

    public ToolEditorSecurityOptionsForm(Func<string, string, string>? translate = null)
    {
        _t = translate ?? ((_, fallback) => fallback);
        var policy = LanguagePackagePolicy.Current;
        _policyPath = policy.SourcePath;
        _policyValues = BuildPolicyValues(policy);
        _categories = BuildCategories(new PolicyText(_t), _policyValues);

        Text = T("tool_editor.security.title", "Security settings");
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(620, 420);
        Size = new Size(700, 480);
        Font = new Font("Segoe UI", 9F);
        FormClosing += ToolEditorSecurityOptionsForm_FormClosing;

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 1,
            RowCount = 4,
        };
        shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        shell.Controls.Add(BuildHeader(), 0, 0);
        shell.Controls.Add(BuildPolicyPathRow(policy), 0, 1);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 10, 0, 0),
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _categoryList = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
        };
        foreach (var category in _categories.Keys)
            _categoryList.Items.Add(category);
        _categoryList.SelectedIndexChanged += (_, _) =>
        {
            if (_categoryList.SelectedItem is string key)
                ShowCategory(key);
        };

        _categoryTitle = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 74, 173),
            Margin = new Padding(0, 0, 0, 4),
        };

        _categoryDescription = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 44,
            ForeColor = Color.FromArgb(31, 41, 55),
        };

        _values = new ListView
        {
            Dock = DockStyle.Fill,
            FullRowSelect = true,
            GridLines = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            View = View.Details,
        };
        _values.Columns.Add("Regel", 210);
        _values.Columns.Add("Betekenis", 250);
        _values.DoubleClick += (_, _) => EditSelectedRule();

        var actionBar = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 0, 0, 6),
        };
        actionBar.Controls.Add(MakeActionButton(T("tool_editor.security.add", "Add"), (_, _) => AddRule()));
        actionBar.Controls.Add(MakeActionButton(T("tool_editor.security.edit", "Edit"), (_, _) => EditSelectedRule()));
        actionBar.Controls.Add(MakeActionButton(T("tool_editor.security.delete", "Delete"), (_, _) => DeleteSelectedRule()));

        var detail = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12, 0, 0, 0),
        };
        detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detail.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        detail.Controls.Add(_categoryTitle, 0, 0);
        detail.Controls.Add(_categoryDescription, 0, 1);
        detail.Controls.Add(actionBar, 0, 2);
        detail.Controls.Add(_values, 0, 3);

        body.Controls.Add(_categoryList, 0, 0);
        body.Controls.Add(detail, 1, 0);
        shell.Controls.Add(body, 0, 2);
        shell.Controls.Add(BuildButtons(), 0, 3);
        Controls.Add(shell);

        if (_categoryList.Items.Count > 0)
            _categoryList.SelectedIndex = 0;
    }

    private static Button MakeActionButton(string text, EventHandler click)
    {
        var button = new Button
        {
            AutoSize = true,
            Text = text,
            Margin = new Padding(0, 0, 6, 0),
        };
        button.Click += click;
        return button;
    }

    private string T(string key, string fallback) => _t(key, fallback);

    private Control BuildHeader()
    {
        return new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 74, 173),
            Text = T("tool_editor.security.heading", "Language package policy"),
            Margin = new Padding(0, 0, 0, 6),
        };
    }

    private Control BuildPolicyPathRow(LanguagePackagePolicy policy)
    {
        var pathPanel = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 3,
            Dock = DockStyle.Top,
        };
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        pathPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = T("tool_editor.security.ini", "INI:"),
            Margin = new Padding(0, 5, 8, 0),
        }, 0, 0);

        pathPanel.Controls.Add(new TextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Text = string.IsNullOrWhiteSpace(policy.SourcePath) ? "(fallback defaults)" : policy.SourcePath,
        }, 1, 0);

        var openPolicy = new Button
        {
            AutoSize = true,
            Text = T("tool_editor.security.open", "Open"),
            Enabled = File.Exists(policy.SourcePath),
            Margin = new Padding(8, 0, 0, 0),
        };
        openPolicy.Click += (_, _) => OpenPolicyFile(policy.SourcePath);
        pathPanel.Controls.Add(openPolicy, 2, 0);

        return pathPanel;
    }

    private Control BuildButtons()
    {
        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 10, 0, 0),
        };
        var close = new Button
        {
            Text = T("tool_editor.security.close", "Close"),
            Width = 100,
        };
        close.Click += (_, _) => Close();
        var apply = new Button
        {
            Text = T("tool_editor.security.apply", "Apply"),
            Width = 100,
        };
        apply.Click += (_, _) => ApplyPolicyChanges(showMessage: true);

        buttons.Controls.Add(close);
        buttons.Controls.Add(apply);
        return buttons;
    }

    private void ShowCategory(string key)
    {
        if (!_categories.TryGetValue(key, out var category))
            return;

        _selectedCategory = key;
        _categoryTitle.Text = category.Title;
        _categoryDescription.Text = category.Description;
        _values.BeginUpdate();
        _values.Items.Clear();
        foreach (var row in category.Rows)
        {
            var item = new ListViewItem(row.Rule);
            item.SubItems.Add(row.Description);
            _values.Items.Add(item);
        }
        _values.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        _values.EndUpdate();
    }

    private void AddRule()
    {
        if (!TryEditRule(null, out var rule))
            return;

        StagePolicyRule(add: rule, remove: null);
        ReloadPolicyViewAfterChange();
    }

    private void EditSelectedRule()
    {
        if (_values.SelectedItems.Count == 0)
            return;
        var oldRule = _values.SelectedItems[0].Text;
        if (!TryEditRule(oldRule, out var newRule))
            return;

        StagePolicyRule(add: newRule, remove: oldRule);
        ReloadPolicyViewAfterChange();
    }

    private void DeleteSelectedRule()
    {
        if (_values.SelectedItems.Count == 0)
            return;
        var oldRule = _values.SelectedItems[0].Text;
        if (MessageBox.Show(this, T("tool_editor.security.delete_confirm", "Delete this rule?") + "\r\n\r\n" + oldRule, T("tool_editor.security.title", "Security settings"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        StagePolicyRule(add: null, remove: oldRule);
        ReloadPolicyViewAfterChange();
    }

    private bool TryEditRule(string? existingRule, out string rule)
    {
        rule = "";
        var parser = _categories.TryGetValue(_selectedCategory, out var selectedCategory) &&
            selectedCategory.IniKey.Equals("allowedPrefixes", StringComparison.OrdinalIgnoreCase);
        using var dialog = new PolicyRuleDialog(
            parser ? T("tool_editor.security.parser_rule", "Parser rule") : _selectedCategory,
            parser,
            existingRule,
            _t);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return false;

        rule = dialog.Rule;
        return !string.IsNullOrWhiteSpace(rule);
    }

    private void StagePolicyRule(string? add, string? remove)
    {
        var key = SelectedPolicyKey();
        if (!_policyValues.TryGetValue(key, out var values))
        {
            values = [];
            _policyValues[key] = values;
        }

        if (!string.IsNullOrWhiteSpace(remove))
            values.RemoveAll(value => MatchesPolicyRule(key, value, remove));
        if (!string.IsNullOrWhiteSpace(add) && !values.Any(value => MatchesPolicyRule(key, value, add)))
            values.Add(NormalizePolicyValueForWrite(key, add));

        _policyDirty = true;
    }

    private string SelectedPolicyKey()
    {
        if (_categories.TryGetValue(_selectedCategory, out var category))
            return category.IniKey;

        throw new InvalidOperationException("Unknown category: " + _selectedCategory);
    }

    private void ReloadPolicyViewAfterChange()
    {
        var previousCategory = _selectedCategory;
        _categories = BuildCategories(new PolicyText(_t), _policyValues);
        RefreshCategoryList(previousCategory);
    }

    private void ApplyPolicyChanges(bool showMessage)
    {
        WritePendingPolicyValues();
        LanguagePackagePolicy.ReloadForCurrentProcess();
        _policyValues = BuildPolicyValues(LanguagePackagePolicy.Current);
        var previousCategory = _selectedCategory;
        _categories = BuildCategories(new PolicyText(_t), _policyValues);
        RefreshCategoryList(previousCategory);
        _policyDirty = false;

        if (showMessage)
        {
            MessageBox.Show(
                this,
                T("tool_editor.security.applied", "Policy applied in this ToolEditor session."),
                T("tool_editor.security.title", "Security settings"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    private void ToolEditorSecurityOptionsForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_policyDirty || _closingAfterPrompt)
            return;

        var result = MessageBox.Show(
            this,
            T("tool_editor.security.unsaved_message", "Security settings have unapplied changes.\r\n\r\nApply changes before closing?"),
            T("tool_editor.security.title", "Security settings"),
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Cancel)
        {
            e.Cancel = true;
            return;
        }

        if (result == DialogResult.Yes)
            ApplyPolicyChanges(showMessage: false);

        _closingAfterPrompt = true;
    }

    private void WritePendingPolicyValues()
    {
        if (string.IsNullOrWhiteSpace(_policyPath) || !File.Exists(_policyPath))
            throw new InvalidOperationException(T("tool_editor.security.policy_missing", "Policy file was not found."));

        foreach (var item in _policyValues)
        {
            var separator = item.Key.Equals("allowedPrefixes", StringComparison.OrdinalIgnoreCase) ? ";" : ",";
            WriteIniValue(_policyPath, "package", item.Key, string.Join(separator, item.Value));
        }
    }

    private void RefreshCategoryList(string preferredCategory)
    {
        _categoryList.BeginUpdate();
        _categoryList.Items.Clear();
        foreach (var category in _categories.Keys)
            _categoryList.Items.Add(category);
        _categoryList.EndUpdate();

        var index = _categoryList.Items.IndexOf(preferredCategory);
        if (index < 0 && _categoryList.Items.Count > 0)
            index = 0;
        if (index >= 0)
            _categoryList.SelectedIndex = index;
    }

    private static List<string> SplitPolicyValues(string value)
    {
        var separators = value.Contains(';') ? new[] { ';' } : new[] { ',' };
        return value.Split(separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static bool MatchesPolicyRule(string key, string stored, string displayed)
    {
        if (key.Equals("allowedPrefixes", StringComparison.OrdinalIgnoreCase))
            return PrefixOfParserRule(stored).Equals(PrefixOfParserRule(displayed), StringComparison.OrdinalIgnoreCase);

        return MatchesDisplayedRule(stored, displayed);
    }

    private static bool MatchesDisplayedRule(string stored, string displayed)
    {
        var normalizedStored = NormalizeDisplayedRule(stored);
        var normalizedDisplayed = NormalizeDisplayedRule(displayed);
        return normalizedStored.Equals(normalizedDisplayed, StringComparison.OrdinalIgnoreCase);
    }

    private static string PrefixOfParserRule(string value)
    {
        return value.Split(':', 2)[0].Trim();
    }

    private static string NormalizeDisplayedRule(string value)
    {
        var text = value.Trim();
        if (text.StartsWith("help/", StringComparison.OrdinalIgnoreCase) &&
            !text["help/".Length..].Contains('/'))
        {
            text = text["help/".Length..];
        }
        return text;
    }

    private static string NormalizePolicyValueForWrite(string key, string value)
    {
        var text = value.Trim();
        if (key.Equals("allowedPrefixes", StringComparison.OrdinalIgnoreCase))
            return text;

        if (text.StartsWith("help/", StringComparison.OrdinalIgnoreCase) &&
            !text["help/".Length..].Contains('/'))
        {
            return text["help/".Length..];
        }

        return text;
    }

    private static string ReadIniValue(string path, string section, string key)
    {
        var currentSection = "";
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentSection = line[1..^1].Trim();
                continue;
            }

            if (!currentSection.Equals(section, StringComparison.OrdinalIgnoreCase))
                continue;

            var index = line.IndexOf('=');
            if (index <= 0)
                continue;
            if (line[..index].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                return line[(index + 1)..].Trim();
        }

        return "";
    }

    private static void WriteIniValue(string path, string section, string key, string value)
    {
        var lines = File.ReadAllLines(path).ToList();
        var currentSection = "";
        for (var index = 0; index < lines.Count; index++)
        {
            var trimmed = lines[index].Trim();
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                currentSection = trimmed[1..^1].Trim();
                continue;
            }

            if (!currentSection.Equals(section, StringComparison.OrdinalIgnoreCase))
                continue;

            var separator = trimmed.IndexOf('=');
            if (separator <= 0)
                continue;
            if (trimmed[..separator].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = key + "=" + value;
                File.WriteAllLines(path, lines);
                return;
            }
        }

        lines.Add(key + "=" + value);
        File.WriteAllLines(path, lines);
    }

    private static Dictionary<string, List<string>> BuildPolicyValues(LanguagePackagePolicy policy)
    {
        return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["allowedExtensions"] = policy.AllowedExtensions.Order(StringComparer.OrdinalIgnoreCase).ToList(),
            ["blockedExtensions"] = policy.BlockedExtensions.Order(StringComparer.OrdinalIgnoreCase).ToList(),
            ["imageExtensions"] = policy.ImageExtensions.Order(StringComparer.OrdinalIgnoreCase).ToList(),
            ["textExtensions"] = policy.TextExtensions.Order(StringComparer.OrdinalIgnoreCase).ToList(),
            ["allowedScripts"] = policy.AllowedScripts.Order(StringComparer.OrdinalIgnoreCase).ToList(),
            ["allowedRootHelpFiles"] = policy.AllowedRootHelpFiles.Order(StringComparer.OrdinalIgnoreCase).ToList(),
            ["allowedPrefixes"] = policy.DescribeAllowedPrefixRules().ToList(),
        };
    }

    private static Dictionary<string, PolicyCategory> BuildCategories(PolicyText policy, IReadOnlyDictionary<string, List<string>> values)
    {
        IReadOnlyList<string> Get(string key) => values.TryGetValue(key, out var list) ? list : [];

        return new Dictionary<string, PolicyCategory>
        {
            [policy.Localize("tool_editor.security.category.allowed_extensions", "Allowed extensions")] = new(
                "allowedExtensions",
                policy.Localize("tool_editor.security.category.allowed_extensions", "Allowed extensions"),
                policy.Localize("tool_editor.security.category.allowed_extensions.desc", "These file types may be stored in a package when the path also matches."),
                Get("allowedExtensions")
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .Select(value => new PolicyRow(value, policy.Localize("tool_editor.security.meaning.allowed", "allowed")))),

            [policy.Localize("tool_editor.security.category.blocked_extensions", "Blocked extensions")] = new(
                "blockedExtensions",
                policy.Localize("tool_editor.security.category.blocked_extensions", "Blocked extensions"),
                policy.Localize("tool_editor.security.category.blocked_extensions.desc", "These file types are always rejected."),
                Get("blockedExtensions")
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .Select(value => new PolicyRow(value, policy.Localize("tool_editor.security.meaning.blocked", "blocked")))),

            [policy.Localize("tool_editor.security.category.image_extensions", "Image extensions")] = new(
                "imageExtensions",
                policy.Localize("tool_editor.security.category.image_extensions", "Image extensions"),
                policy.Localize("tool_editor.security.category.image_extensions.desc", "These image types may be previewed and managed as media."),
                Get("imageExtensions")
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .Select(value => new PolicyRow(value, policy.Localize("tool_editor.security.meaning.image", "images only")))),

            [policy.Localize("tool_editor.security.category.text_extensions", "Text extensions")] = new(
                "textExtensions",
                policy.Localize("tool_editor.security.category.text_extensions", "Text extensions"),
                policy.Localize("tool_editor.security.category.text_extensions.desc", "These text types may be edited under text package paths."),
                Get("textExtensions")
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .Select(value => new PolicyRow(value, policy.Localize("tool_editor.security.meaning.text", "HTML/CSS/JS according to policy")))),

            [policy.Localize("tool_editor.security.category.scripts", "Scripts")] = new(
                "allowedScripts",
                policy.Localize("tool_editor.security.category.scripts.title", "JavaScript"),
                policy.Localize("tool_editor.security.category.scripts.desc", "Only these script names may be stored in a package."),
                Get("allowedScripts").Select(value => new PolicyRow(value, policy.Localize("tool_editor.security.meaning.trusted_script", "trusted help script")))),

            [policy.Localize("tool_editor.security.category.templates", "Templates")] = new(
                "allowedRootHelpFiles",
                policy.Localize("tool_editor.security.category.templates.title", "Root help templates"),
                policy.Localize("tool_editor.security.category.templates.desc", "These shared help templates and style files may be stored directly below help/."),
                Get("allowedRootHelpFiles").Select(value => new PolicyRow("help/" + value, policy.Localize("tool_editor.security.meaning.root_help", "root help file")))),

            [policy.Localize("tool_editor.security.category.parser", "Parser rules")] = new(
                "allowedPrefixes",
                policy.Localize("tool_editor.security.category.parser.title", "Parser and package paths"),
                policy.Localize("tool_editor.security.category.parser.desc", "The compiler uses these prefix rules to classify package files."),
                Get("allowedPrefixes").Select(rule => DescribePrefixRule(policy, rule))),
        };
    }

    private static PolicyRow DescribePrefixRule(PolicyText policy, string rule)
    {
        var parts = rule.Split(':', 2);
        var kind = parts.Length == 2 ? parts[1] : "";
        var description = kind switch
        {
            "image" => policy.Localize("tool_editor.security.meaning.image", "images only"),
            "language" or "lng" => policy.Localize("tool_editor.security.meaning.language", "language file"),
            "text" => policy.Localize("tool_editor.security.meaning.text", "HTML/CSS/JS according to policy"),
            _ => kind,
        };
        return new PolicyRow(parts[0], description);
    }

    private static void OpenPolicyFile(string path)
    {
        if (!File.Exists(path))
            return;

        Process.Start(new ProcessStartInfo(path)
        {
            UseShellExecute = true,
        });
    }

    private sealed record PolicyCategory(string IniKey, string Title, string Description, IEnumerable<PolicyRow> Rows);

    private sealed record PolicyRow(string Rule, string Description);

    private sealed class PolicyText(Func<string, string, string> Translate)
    {
        public string Localize(string key, string fallback) => Translate(key, fallback);
    }

    private sealed class PolicyRuleDialog : Form
    {
        private readonly TextBox _ruleBox = new() { Dock = DockStyle.Fill };
        private readonly ComboBox _kindBox = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly bool _parserRule;

        private readonly Func<string, string, string> _t;

        public PolicyRuleDialog(string title, bool parserRule, string? existingRule, Func<string, string, string> translate)
        {
            _t = translate;
            _parserRule = parserRule;
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            Size = parserRule ? new Size(440, 180) : new Size(440, 140);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = parserRule ? 3 : 2,
            };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            if (parserRule)
                shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            shell.Controls.Add(new Label { AutoSize = true, Text = parserRule ? T("tool_editor.security.prefix", "Prefix:") : T("tool_editor.security.rule", "Rule:"), Margin = new Padding(0, 5, 8, 0) }, 0, 0);
            shell.Controls.Add(_ruleBox, 1, 0);

            if (parserRule)
            {
                _kindBox.Items.AddRange(["text", "image", "lng"]);
                _kindBox.SelectedIndex = 0;
                shell.Controls.Add(new Label { AutoSize = true, Text = T("tool_editor.security.type", "Type:"), Margin = new Padding(0, 9, 8, 0) }, 0, 1);
                shell.Controls.Add(_kindBox, 1, 1);
            }

            var buttons = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0),
            };
            var ok = new Button { DialogResult = DialogResult.OK, Text = T("tool_editor.security.ok", "OK"), Width = 90 };
            var cancel = new Button { DialogResult = DialogResult.Cancel, Text = T("tool_editor.security.cancel", "Cancel"), Width = 90 };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            shell.Controls.Add(buttons, 0, parserRule ? 2 : 1);
            shell.SetColumnSpan(buttons, 2);
            Controls.Add(shell);

            if (!string.IsNullOrWhiteSpace(existingRule))
                SetInitialRule(existingRule);

            AcceptButton = ok;
            CancelButton = cancel;
        }

        private string T(string key, string fallback) => _t(key, fallback);

        public string Rule
        {
            get
            {
                var rule = _ruleBox.Text.Trim();
                if (!_parserRule)
                    return rule;
                if (!rule.EndsWith('/'))
                    rule += "/";
                return rule + ":" + (_kindBox.SelectedItem?.ToString() ?? "text");
            }
        }

        private void SetInitialRule(string existingRule)
        {
            if (!_parserRule)
            {
                _ruleBox.Text = NormalizeDisplayedRule(existingRule);
                return;
            }

            var parts = existingRule.Split(':', 2);
            _ruleBox.Text = parts[0].Trim();
            if (parts.Length == 2)
            {
                var kind = parts[1].Trim();
                if (kind.Equals("language", StringComparison.OrdinalIgnoreCase))
                    kind = "lng";
                var index = _kindBox.Items.IndexOf(kind);
                if (index >= 0)
                    _kindBox.SelectedIndex = index;
            }
        }
    }
}
