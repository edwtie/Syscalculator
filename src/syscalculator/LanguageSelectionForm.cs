#nullable enable
namespace Syscalculator.UI.WinForms;

// Zoek/commentaar: Type-overzicht: class LanguageSelectionForm bevat de hoofdlogica/data voor dit onderdeel.
internal sealed class LanguageSelectionForm : Form
{
    private readonly IReadOnlyList<LanguageCatalog.LanguageInfo> _languages;
    private readonly LanguageCatalog _language;
    private readonly TextBox _searchBox = new();
    private readonly ListBox _languageList = new();
    private readonly Label _countLabel = new();
    private readonly Button _okButton = new();

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
        _languages = languages;
        _language = language;

        Text = T("dialog.language.title", "Choose language");
        ClientSize = new Size(460, 520);
        MinimumSize = new Size(420, 440);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        AppWindowIcon.ApplyTo(this);
        ShowInTaskbar = false;
        BackColor = Color.FromArgb(246, 248, 252);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(16),
            BackColor = Color.FromArgb(246, 248, 252),
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
            ForeColor = Color.FromArgb(0, 65, 170),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        root.Controls.Add(header, 0, 0);

        _searchBox.Dock = DockStyle.Fill;
        _searchBox.Margin = new Padding(0, 0, 0, 8);
        _searchBox.PlaceholderText = T("dialog.language.search", "Search language...");
        _searchBox.TextChanged += (_, _) => RefreshLanguageList(_searchBox.Text, currentLanguageFile, currentLanguagePackageId);
        root.Controls.Add(_searchBox, 0, 1);

        _languageList.Dock = DockStyle.Fill;
        _languageList.BackColor = Color.White;
        _languageList.BorderStyle = BorderStyle.FixedSingle;
        _languageList.DisplayMember = nameof(LanguageCatalog.LanguageInfo.DisplayName);
        _languageList.DrawMode = DrawMode.OwnerDrawFixed;
        _languageList.ItemHeight = 42;
        _languageList.DrawItem += LanguageList_DrawItem;
        _languageList.DoubleClick += (_, _) => AcceptSelectedLanguage();
        _languageList.SelectedIndexChanged += (_, _) => _okButton.Enabled = _languageList.SelectedItem is not null;
        root.Controls.Add(_languageList, 0, 2);

        _countLabel.Dock = DockStyle.Fill;
        _countLabel.ForeColor = Color.DimGray;
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

        var cancelButton = new Button
        {
            Text = T("dialog.language.cancel", "Cancel"),
            Width = 86,
            DialogResult = DialogResult.Cancel,
        };

        buttons.Controls.Add(_okButton);
        buttons.Controls.Add(cancelButton);
        root.Controls.Add(buttons, 0, 4);

        AcceptButton = _okButton;
        CancelButton = cancelButton;
        Controls.Add(root);

        RefreshLanguageList("", currentLanguageFile, currentLanguagePackageId);
    }

    // Zoek/commentaar: Methode LanguageList_DrawItem: centrale logica voor deze stap.
    private void LanguageList_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _languageList.Items.Count)
            return;

        var language = (LanguageCatalog.LanguageInfo)_languageList.Items[e.Index];
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var bounds = e.Bounds;

        using var background = new SolidBrush(selected ? Color.FromArgb(219, 234, 254) : Color.White);
        e.Graphics.FillRectangle(background, bounds);

        using var accent = new SolidBrush(Color.FromArgb(0, 94, 184));
        e.Graphics.FillRectangle(accent, bounds.Left, bounds.Top, 4, bounds.Height);

        using var nameBrush = new SolidBrush(Color.FromArgb(20, 30, 46));
        using var fileBrush = new SolidBrush(Color.DimGray);
        using var nameFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        using var fileFont = new Font("Segoe UI", 8F, FontStyle.Regular);

        var x = bounds.Left + 14;
        e.Graphics.DrawString(language.DisplayName, nameFont, nameBrush, x, bounds.Top + 5);
        e.Graphics.DrawString(language.SourceLabel, fileFont, fileBrush, x, bounds.Top + 23);

        using var border = new Pen(Color.FromArgb(232, 236, 244));
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

    // Zoek/commentaar: Methode T: centrale logica voor deze stap.
    private string T(string key, string fallback) => _language.Text(key, fallback);
}
