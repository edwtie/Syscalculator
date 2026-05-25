#nullable enable
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text.RegularExpressions;
using Tiedragon.ToolEditor;

namespace Syscalculator.UI.WinForms;

// Zoek/commentaar: Type-overzicht: class LanguageSelectionForm bevat de hoofdlogica/data voor dit onderdeel.
internal sealed class LanguageSelectionForm : Form
{
    private List<LanguageCatalog.LanguageInfo> _languages;
    private readonly LanguageCatalog _language;
    private readonly TextBox _searchBox = new();
    private readonly ListBox _languageList = new();
    private readonly Label _countLabel = new();
    private readonly Button _okButton = new();
    private readonly Button _installButton = new();
    private readonly ContextMenuStrip _languageContextMenu = new();
    private readonly ToolEditorUiTheme _uiTheme;
    private string _currentLanguageFile;
    private string? _currentLanguagePackageId;
    private bool IsDarkTheme => _uiTheme == ToolEditorUiTheme.Dark;
    private Color WindowBackColor => IsDarkTheme ? Color.FromArgb(18, 24, 32) : Color.FromArgb(246, 248, 252);
    private Color PanelBackColor => IsDarkTheme ? Color.FromArgb(31, 41, 55) : Color.White;
    private Color EditorBackColor => IsDarkTheme ? Color.FromArgb(39, 39, 39) : Color.White;
    private Color TextColor => IsDarkTheme ? Color.FromArgb(226, 232, 240) : Color.FromArgb(20, 30, 46);
    private Color MutedTextColor => IsDarkTheme ? Color.FromArgb(148, 163, 184) : Color.DimGray;
    private Color AccentColor => IsDarkTheme ? Color.FromArgb(96, 165, 250) : Color.FromArgb(0, 94, 184);
    private Color SelectionBackColor => IsDarkTheme ? Color.FromArgb(30, 64, 175) : Color.FromArgb(219, 234, 254);
    private Color BorderColor => IsDarkTheme ? Color.FromArgb(71, 85, 105) : Color.FromArgb(232, 236, 244);
    private Color SuccessColor => Color.FromArgb(34, 197, 94);
    private Color DangerColor => Color.FromArgb(239, 68, 68);

    public string? SelectedLanguageFile { get; private set; }
    public string? SelectedLanguagePackageId { get; private set; }
    public LanguageCatalog.LanguageInfo? SelectedLanguage { get; private set; }

    // Zoek/commentaar: Constructor: maakt en initialiseert LanguageSelectionForm.
    public LanguageSelectionForm(
        IReadOnlyList<LanguageCatalog.LanguageInfo> languages,
        string currentLanguageFile,
        string? currentLanguagePackageId,
        LanguageCatalog language)
    {
        _languages = languages.ToList();
        _language = language;
        _uiTheme = ToolEditorUiThemeSettings.Load();
        _currentLanguageFile = currentLanguageFile;
        _currentLanguagePackageId = currentLanguagePackageId;

        Text = T("dialog.language.title", "Choose language");
        ClientSize = new Size(460, 520);
        MinimumSize = new Size(420, 440);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        AppWindowIcon.ApplyTo(this);
        ShowInTaskbar = false;
        BackColor = WindowBackColor;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        HandleCreated += (_, _) => ToolEditorUiThemeSettings.ApplyNativeWindowTheme(this, _uiTheme);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(16),
            BackColor = WindowBackColor,
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var header = new Label
        {
            Dock = DockStyle.Fill,
            Text = T("dialog.language.title", "Choose language"),
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = AccentColor,
            BackColor = WindowBackColor,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        root.Controls.Add(header, 0, 0);

        _searchBox.Dock = DockStyle.Fill;
        _searchBox.Margin = new Padding(0, 0, 0, 8);
        _searchBox.BackColor = EditorBackColor;
        _searchBox.ForeColor = TextColor;
        _searchBox.BorderStyle = BorderStyle.FixedSingle;
        _searchBox.PlaceholderText = T("dialog.language.search", "Search language...");
        _searchBox.TextChanged += (_, _) => RefreshLanguageList(_searchBox.Text, _currentLanguageFile, _currentLanguagePackageId);
        root.Controls.Add(_searchBox, 0, 1);

        _languageList.Dock = DockStyle.Fill;
        _languageList.BackColor = EditorBackColor;
        _languageList.ForeColor = TextColor;
        _languageList.BorderStyle = BorderStyle.FixedSingle;
        _languageList.DisplayMember = nameof(LanguageCatalog.LanguageInfo.DisplayName);
        _languageList.DrawMode = DrawMode.OwnerDrawFixed;
        _languageList.ItemHeight = 42;
        _languageList.DrawItem += LanguageList_DrawItem;
        _languageList.MouseDown += LanguageList_MouseDown;
        _languageList.DoubleClick += (_, _) => AcceptSelectedLanguage();
        _languageList.SelectedIndexChanged += (_, _) => _okButton.Enabled = _languageList.SelectedItem is not null;
        _languageContextMenu.Items.Add(T("dialog.language.info", "Information..."), null, (_, _) => ShowSelectedLanguageInformation());
        _languageList.ContextMenuStrip = _languageContextMenu;
        ApplyThemeToContextMenu(_languageContextMenu);
        root.Controls.Add(_languageList, 0, 2);

        _countLabel.Dock = DockStyle.Fill;
        _countLabel.BackColor = WindowBackColor;
        _countLabel.ForeColor = MutedTextColor;
        _countLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_countLabel, 0, 3);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0),
        };

        _okButton.Text = T("dialog.language.ok", "OK");
        _okButton.Width = 86;
        _okButton.Enabled = false;
        _okButton.Click += (_, _) => AcceptSelectedLanguage();
        ApplyThemeToButton(_okButton);

        _installButton.Text = T("dialog.language.install_package", "Install package...");
        _installButton.Width = 126;
        _installButton.Click += (_, _) => InstallLanguagePackage();
        ApplyThemeToButton(_installButton);

        var cancelButton = new Button
        {
            Text = T("dialog.language.cancel", "Cancel"),
            Width = 86,
            DialogResult = DialogResult.Cancel,
        };
        ApplyThemeToButton(cancelButton);

        buttons.Controls.Add(_okButton);
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(_installButton);
        root.Controls.Add(buttons, 0, 4);

        AcceptButton = _okButton;
        CancelButton = cancelButton;
        Controls.Add(root);

        RefreshLanguageList("", _currentLanguageFile, _currentLanguagePackageId);
    }

    private void LanguageList_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
            return;

        var index = _languageList.IndexFromPoint(e.Location);
        if (index >= 0 && index < _languageList.Items.Count)
            _languageList.SelectedIndex = index;
    }

    // Zoek/commentaar: Methode LanguageList_DrawItem: centrale logica voor deze stap.
    private void LanguageList_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _languageList.Items.Count)
            return;

        var language = (LanguageCatalog.LanguageInfo)_languageList.Items[e.Index];
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var bounds = e.Bounds;

        using var background = new SolidBrush(selected ? SelectionBackColor : EditorBackColor);
        e.Graphics.FillRectangle(background, bounds);

        using var accent = new SolidBrush(AccentColor);
        e.Graphics.FillRectangle(accent, bounds.Left, bounds.Top, 4, bounds.Height);

        using var nameBrush = new SolidBrush(selected && IsDarkTheme ? Color.White : TextColor);
        using var fileBrush = new SolidBrush(selected && IsDarkTheme ? Color.FromArgb(219, 234, 254) : MutedTextColor);
        using var nameFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        using var fileFont = new Font("Segoe UI", 8F, FontStyle.Regular);

        var x = bounds.Left + 14;
        e.Graphics.DrawString(language.DisplayName, nameFont, nameBrush, x, bounds.Top + 5);
        e.Graphics.DrawString(language.SourceLabel, fileFont, fileBrush, x, bounds.Top + 23);
        DrawTrustLock(e.Graphics, new Rectangle(bounds.Right - 35, bounds.Top + 8, 24, 24), language.Signed);

        using var border = new Pen(BorderColor);
        e.Graphics.DrawLine(border, bounds.Left + 8, bounds.Bottom - 1, bounds.Right - 8, bounds.Bottom - 1);
    }

    // Zoek/commentaar: Ververst de getoonde data of UI voor RefreshLanguageList.
    private void RefreshLanguageList(string filter, string currentLanguageFile, string? currentLanguagePackageId)
    {
        _languageList.BeginUpdate();
        _languageList.Items.Clear();

        foreach (var language in GetDisplayLanguages(filter, currentLanguageFile, currentLanguagePackageId))
            _languageList.Items.Add(language);

        _languageList.EndUpdate();

        SelectCurrentLanguage(currentLanguageFile, currentLanguagePackageId);
        _countLabel.Text = string.Format(T("dialog.language.count", "{0} languages"), _languageList.Items.Count);
        _okButton.Enabled = _languageList.SelectedItem is not null;
    }

    // Zoek/commentaar: Zet de huidige taal bovenaan en laat de rest alfabetisch staan.
    private IEnumerable<LanguageCatalog.LanguageInfo> GetDisplayLanguages(
        string filter,
        string currentLanguageFile,
        string? currentLanguagePackageId)
    {
        var matchingLanguages = _languages
            .Where(language => MatchesFilter(language, filter))
            .ToList();

        return matchingLanguages
            .OrderByDescending(language => language.Matches(currentLanguageFile, currentLanguagePackageId))
            .ThenBy(language => language.DisplayName, StringComparer.CurrentCultureIgnoreCase);
    }

    // Zoek/commentaar: Selecteert het juiste item voor SelectCurrentLanguage.
    private void SelectCurrentLanguage(string currentLanguageFile, string? currentLanguagePackageId)
    {
        for (var i = 0; i < _languageList.Items.Count; i++)
        {
            if (_languageList.Items[i] is LanguageCatalog.LanguageInfo language &&
                language.Matches(currentLanguageFile, currentLanguagePackageId))
            {
                _languageList.SelectedIndex = i;
                return;
            }
        }

        if (_languageList.Items.Count > 0)
            _languageList.SelectedIndex = 0;
    }

    // Zoek/commentaar: Methode MatchesFilter: centrale logica voor deze stap.
    private static bool MatchesFilter(LanguageCatalog.LanguageInfo language, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return true;

        return language.DisplayName.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ||
            language.SourceLabel.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    // Zoek/commentaar: Bevestigt de huidige keuze voor AcceptSelectedLanguage.
    private void AcceptSelectedLanguage()
    {
        if (_languageList.SelectedItem is not LanguageCatalog.LanguageInfo language)
            return;

        SelectedLanguage = language;
        SelectedLanguageFile = language.FileName;
        SelectedLanguagePackageId = language.PackageId;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ShowSelectedLanguageInformation()
    {
        if (_languageList.SelectedItem is not LanguageCatalog.LanguageInfo language)
            return;

        var code = string.IsNullOrWhiteSpace(language.LanguageCode)
            ? Path.GetFileNameWithoutExtension(language.FileName)
            : language.LanguageCode;
        var author = string.IsNullOrWhiteSpace(language.Producer)
            ? T("dialog.language.info.local_author", "Local file")
            : language.Producer;
        var product = string.IsNullOrWhiteSpace(language.Product) ? "Syscalculator" : language.Product;
        var packageId = string.IsNullOrWhiteSpace(language.PackageId) ? "-" : language.PackageId;
        var version = string.IsNullOrWhiteSpace(language.PackageVersion) ? "-" : language.PackageVersion;
        var signed = language.Signed
            ? T("common.yes", "Yes")
            : T("common.no", "No");
        var algorithm = string.IsNullOrWhiteSpace(language.SignatureAlgorithm) ? "-" : language.SignatureAlgorithm;
        var keyId = string.IsNullOrWhiteSpace(language.SignatureKeyId) ? "-" : language.SignatureKeyId;
        var keySha256 = string.IsNullOrWhiteSpace(language.SignatureKeySha256) ? "-" : language.SignatureKeySha256;

        var rows = new[]
        {
            (T("dialog.language.info.name", "Language"), language.DisplayName),
            (T("dialog.language.info.code", "Code"), code),
            (T("dialog.language.info.author", "Author"), author),
            (T("dialog.language.info.product", "Product"), product),
            (T("dialog.language.info.package", "Package"), packageId),
            (T("dialog.language.info.file", "File"), language.FileName),
            (T("dialog.language.info.version", "Version"), version),
            (T("dialog.language.info.signed", "Signed"), signed),
            (T("dialog.language.info.algorithm", "Algorithm"), algorithm),
            (T("dialog.language.info.key", "Key"), keyId),
            ("SHA-256", keySha256),
        };

        using var dialog = new LanguageInformationDialog(
            T("dialog.language.info.title", "Language information"),
            language.DisplayName,
            language.Signed,
            language.Signed
                ? T("help.signed_package_verified", "Signed package verified")
                : T("dialog.language.info.unsigned", "Unsigned package"),
            rows,
            _uiTheme,
            T("dialog.language.ok", "OK"));
        dialog.ShowDialog(this);
    }

    private void DrawTrustLock(Graphics graphics, Rectangle bounds, bool signed)
    {
        LanguageSignedLockSvg.Draw(graphics, bounds, signed ? SuccessColor : DangerColor);
    }

    private void ApplyThemeToButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.BackColor = IsDarkTheme ? Color.FromArgb(39, 39, 39) : SystemColors.Control;
        button.ForeColor = IsDarkTheme ? Color.White : SystemColors.ControlText;
        button.FlatAppearance.BorderColor = IsDarkTheme ? BorderColor : SystemColors.ControlDark;
        button.FlatAppearance.MouseOverBackColor = IsDarkTheme ? Color.FromArgb(51, 65, 85) : Color.FromArgb(230, 238, 248);
        button.FlatAppearance.MouseDownBackColor = IsDarkTheme ? Color.FromArgb(30, 64, 175) : Color.FromArgb(214, 228, 246);
    }

    private void ApplyThemeToContextMenu(ContextMenuStrip menu)
    {
        menu.BackColor = PanelBackColor;
        menu.ForeColor = TextColor;
        menu.RenderMode = ToolStripRenderMode.ManagerRenderMode;
        menu.Renderer = new LanguageMenuRenderer(IsDarkTheme, PanelBackColor, TextColor, SelectionBackColor, BorderColor);
        foreach (ToolStripItem item in menu.Items)
        {
            item.BackColor = PanelBackColor;
            item.ForeColor = TextColor;
        }
    }

    private void InstallLanguagePackage()
    {
        using var dialog = new OpenFileDialog
        {
            Title = T("dialog.language.install_package", "Install package..."),
            Filter = T("dialog.language.package_filter", "Language package (*.lngpdk;*.zip)|*.lngpdk;*.zip|All files (*.*)|*.*"),
            CheckFileExists = true,
            Multiselect = false,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var manifest = LanguagePackageService.Install(AppContext.BaseDirectory, dialog.FileName);
            _languages = LanguageCatalog.ListAvailable(AppContext.BaseDirectory).ToList();
            _currentLanguageFile = manifest.LanguageCode + ".lng";
            _currentLanguagePackageId = manifest.PackageKey;
            _searchBox.Clear();
            RefreshLanguageList("", _currentLanguageFile, _currentLanguagePackageId);
            SelectedLanguage = _languages.FirstOrDefault(language =>
                language.Matches(_currentLanguageFile, _currentLanguagePackageId));
            _okButton.Enabled = _languageList.SelectedItem is not null;
            _countLabel.Text = string.Format(
                T("dialog.language.install_ok", "Installed: {0}"),
                string.IsNullOrWhiteSpace(manifest.NativeName) ? manifest.DisplayName : manifest.NativeName);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                this,
                ex.Message,
                T("dialog.language.install_failed", "Language package refused"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // Zoek/commentaar: Methode T: centrale logica voor deze stap.
    private string T(string key, string fallback) => _language.Text(key, fallback);

    private sealed class LanguageMenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly bool _dark;
        private readonly Color _back;
        private readonly Color _text;
        private readonly Color _selected;
        private readonly Color _border;

        public LanguageMenuRenderer(bool dark, Color back, Color text, Color selected, Color border)
        {
            _dark = dark;
            _back = back;
            _text = text;
            _selected = selected;
            _border = border;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(_back);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var color = e.Item.Selected ? _selected : _back;
            using var brush = new SolidBrush(color);
            e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = _text;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using var pen = new Pen(_dark ? _border : SystemColors.ControlDark);
            e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }
    }

    private sealed class LanguageInformationDialog : Form
    {
        private readonly ToolEditorUiTheme _theme;
        private readonly bool _signed;
        private readonly Color _windowBack;
        private readonly Color _panelBack;
        private readonly Color _text;
        private readonly Color _muted;
        private readonly Color _border;
        private readonly Color _success = Color.FromArgb(34, 197, 94);

        public LanguageInformationDialog(
            string title,
            string languageName,
            bool signed,
            string statusText,
            IReadOnlyList<(string Label, string Value)> rows,
            ToolEditorUiTheme theme,
            string okText)
        {
            _theme = theme;
            _signed = signed;
            var dark = theme == ToolEditorUiTheme.Dark;
            _windowBack = dark ? Color.FromArgb(18, 24, 32) : Color.FromArgb(246, 248, 252);
            _panelBack = dark ? Color.FromArgb(31, 41, 55) : Color.White;
            _text = dark ? Color.FromArgb(226, 232, 240) : Color.FromArgb(20, 30, 46);
            _muted = dark ? Color.FromArgb(148, 163, 184) : Color.DimGray;
            _border = dark ? Color.FromArgb(71, 85, 105) : Color.FromArgb(226, 232, 240);

            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(520, 450);
            BackColor = _windowBack;
            ForeColor = _text;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            AppWindowIcon.ApplyTo(this);
            HandleCreated += (_, _) => ToolEditorUiThemeSettings.ApplyNativeWindowTheme(this, _theme);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(18),
                BackColor = _windowBack
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            var header = new Panel { Dock = DockStyle.Fill, BackColor = _windowBack };
            var badge = new SignedBadge(signed, signed ? _success : Color.FromArgb(239, 68, 68)) { BackColor = _windowBack, Location = new Point(0, 4), Size = new Size(56, 56) };
            var name = new Label
            {
                AutoSize = false,
                Text = languageName,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = _text,
                BackColor = _windowBack,
                Location = new Point(70, 8),
                Size = new Size(390, 26)
            };
            var status = new Label
            {
                AutoSize = false,
                Text = statusText,
                ForeColor = signed ? _success : _muted,
                BackColor = _windowBack,
                Location = new Point(71, 36),
                Size = new Size(390, 22)
            };
            header.Controls.Add(badge);
            header.Controls.Add(name);
            header.Controls.Add(status);
            root.Controls.Add(header, 0, 0);

            var details = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = rows.Count,
                BackColor = _panelBack,
                Padding = new Padding(14),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var rowIndex = 0;
            foreach (var row in rows)
            {
                details.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
                var label = new Label { Text = row.Label, Dock = DockStyle.Fill, ForeColor = _muted, BackColor = _panelBack, TextAlign = ContentAlignment.MiddleLeft };
                var value = new Label { Text = row.Value, Dock = DockStyle.Fill, ForeColor = _text, BackColor = _panelBack, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
                details.Controls.Add(label, 0, rowIndex);
                details.Controls.Add(value, 1, rowIndex);
                rowIndex++;
            }

            var borderPanel = new Panel { Dock = DockStyle.Fill, BackColor = _border, Padding = new Padding(1) };
            borderPanel.Controls.Add(details);
            root.Controls.Add(borderPanel, 0, 1);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, BackColor = _windowBack, Padding = new Padding(0, 10, 0, 0) };
            var ok = new Button { Text = okText, Width = 92, Height = 28, DialogResult = DialogResult.OK };
            ok.FlatStyle = FlatStyle.Flat;
            ok.UseVisualStyleBackColor = false;
            ok.BackColor = dark ? Color.FromArgb(39, 39, 39) : SystemColors.Control;
            ok.ForeColor = dark ? Color.White : SystemColors.ControlText;
            ok.FlatAppearance.BorderColor = _border;
            buttons.Controls.Add(ok);
            root.Controls.Add(buttons, 0, 2);

            AcceptButton = ok;
            CancelButton = ok;
            Controls.Add(root);
        }

        private sealed class SignedBadge : Control
        {
            private readonly bool _signed;
            private readonly Color _successColor;

            public SignedBadge(bool signed, Color successColor)
            {
                _signed = signed;
                _successColor = successColor;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                LanguageSignedLockSvg.Draw(e.Graphics, new Rectangle(2, 2, Width - 4, Height - 4), _signed ? _successColor : Color.Gray);
            }
        }
    }

    private static class LanguageSignedLockSvg
    {
        private const float SvgLeft = 5485f;
        private const float SvgTop = 545f;
        private const float SvgWidth = 1059f;
        private const float SvgHeight = 1411f;
        private static readonly Regex SvgPathTokenRegex = new(@"[A-Za-z]|[-+]?(?:\d*\.\d+|\d+)(?:[eE][-+]?\d+)?", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly SvgPathPart[] SourceSvgPaths =
        [
            new(true, "M 5597.109375 1096.890625 L 6431.089844 1096.890625 C 6492.738281 1096.890625 6543.171875 1147.328125 6543.171875 1208.96875 L 6543.171875 1842.980469 C 6543.171875 1904.621094 6492.738281 1955.054688 6431.089844 1955.054688 L 5597.109375 1955.054688 C 5535.46875 1955.054688 5485.039062 1904.621094 5485.039062 1842.980469 L 5485.039062 1208.96875 C 5485.039062 1147.328125 5535.46875 1096.890625 5597.109375 1096.890625"),
            new(true, "M 6014.101562 545.269531 C 6236.058594 545.269531 6417.660156 726.859375 6417.660156 948.828125 L 6417.660156 1120.898438 L 6267.699219 1120.898438 L 6267.699219 948.828125 C 6267.699219 809.339844 6153.589844 695.230469 6014.101562 695.230469 C 5874.609375 695.230469 5760.488281 809.339844 5760.488281 948.828125 L 5760.488281 1120.898438 L 5610.53125 1120.898438 L 5610.53125 948.828125 C 5610.53125 726.859375 5792.128906 545.269531 6014.101562 545.269531"),
            new(false, "M 5791.21875 1488.179688 C 5812.539062 1466.859375 5847.410156 1466.859375 5868.738281 1488.179688 L 5952.078125 1571.523438 L 6159.460938 1364.140625 C 6180.78125 1342.820312 6215.660156 1342.820312 6236.980469 1364.140625 C 6258.300781 1385.460938 6258.300781 1420.339844 6236.980469 1441.660156 L 5990.839844 1687.804688 C 5969.519531 1709.125 5934.640625 1709.125 5913.320312 1687.804688 L 5791.21875 1565.695312 C 5769.898438 1544.375 5769.898438 1509.496094 5791.21875 1488.179688")
        ];
        private static readonly Lazy<IReadOnlyList<SvgPathShape>> SvgShapes = new(() => SourceSvgPaths
            .Select(part => new SvgPathShape(part.UseBadgeColor, BuildSvgPath(part.Data)))
            .ToArray());

        public static void Draw(Graphics graphics, Rectangle bounds, Color badgeColor)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            var state = graphics.Save();
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var scale = Math.Min(bounds.Width / SvgWidth, bounds.Height / SvgHeight);
            var left = bounds.Left + (bounds.Width - SvgWidth * scale) / 2f;
            var top = bounds.Top + (bounds.Height - SvgHeight * scale) / 2f;
            graphics.TranslateTransform(left, top);
            graphics.ScaleTransform(scale, scale);
            graphics.TranslateTransform(-SvgLeft, -SvgTop);

            foreach (var shape in SvgShapes.Value)
            {
                using var brush = new SolidBrush(shape.UseBadgeColor ? badgeColor : Color.White);
                graphics.FillPath(brush, shape.Path);
            }

            graphics.Restore(state);
        }

        private static GraphicsPath BuildSvgPath(string data)
        {
            var tokens = SvgPathTokenRegex.Matches(data).Select(match => match.Value).ToArray();
            var path = new GraphicsPath(FillMode.Alternate);
            var index = 0;
            var command = '\0';
            var current = PointF.Empty;
            var figureStart = PointF.Empty;

            while (index < tokens.Length)
            {
                if (IsCommand(tokens[index]))
                    command = tokens[index++][0];
                if (command == '\0')
                    break;

                var relative = char.IsLower(command);
                switch (char.ToUpperInvariant(command))
                {
                    case 'M':
                        current = ReadPoint(tokens, ref index, current, relative);
                        figureStart = current;
                        path.StartFigure();
                        command = relative ? 'l' : 'L';
                        break;
                    case 'L':
                        while (CanReadNumbers(tokens, index, 2))
                        {
                            var next = ReadPoint(tokens, ref index, current, relative);
                            path.AddLine(current, next);
                            current = next;
                        }
                        break;
                    case 'C':
                        while (CanReadNumbers(tokens, index, 6))
                        {
                            var control1 = ReadPoint(tokens, ref index, current, relative);
                            var control2 = ReadPoint(tokens, ref index, current, relative);
                            var end = ReadPoint(tokens, ref index, current, relative);
                            path.AddBezier(current, control1, control2, end);
                            current = end;
                        }
                        break;
                    case 'Z':
                        path.CloseFigure();
                        current = figureStart;
                        command = '\0';
                        break;
                    default:
                        index++;
                        break;
                }
            }

            return path;
        }

        private static PointF ReadPoint(string[] tokens, ref int index, PointF current, bool relative)
        {
            var x = float.Parse(tokens[index++], CultureInfo.InvariantCulture);
            var y = float.Parse(tokens[index++], CultureInfo.InvariantCulture);
            return relative ? new PointF(current.X + x, current.Y + y) : new PointF(x, y);
        }

        private static bool CanReadNumbers(string[] tokens, int index, int count)
        {
            if (index + count > tokens.Length)
                return false;

            for (var i = 0; i < count; i++)
            {
                if (IsCommand(tokens[index + i]))
                    return false;
            }

            return true;
        }

        private static bool IsCommand(string token)
        {
            return token.Length == 1 && char.IsLetter(token[0]);
        }

        private sealed record SvgPathPart(bool UseBadgeColor, string Data);

        private sealed record SvgPathShape(bool UseBadgeColor, GraphicsPath Path);
    }
}
