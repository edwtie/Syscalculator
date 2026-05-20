#nullable enable
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace Syscalculator.UI.WinForms;

// Zoek/commentaar: Type-overzicht: class AboutForm bevat de hoofdlogica/data voor dit onderdeel.
internal sealed class AboutForm : Form
{
    private LanguageCatalog _language;
    private ComboBox? _languageCombo;
    private readonly IReadOnlyList<LanguageCatalog.LanguageInfo> _languages;
    private readonly Image? _aboutImage;
    private bool _initializingLanguage;
    private bool _languageRefreshPending;
    private string _currentLanguageFile;
    private string? _currentLanguagePackageId;
    private readonly string _updateChannel;

    public string? SelectedLanguageFile { get; private set; }
    public string? SelectedLanguagePackageId { get; private set; }
    public LanguageCatalog.LanguageInfo? SelectedLanguage { get; private set; }

    // Zoek/commentaar: Constructor: maakt en initialiseert AboutForm.
    public AboutForm(
        LanguageCatalog language,
        IReadOnlyList<LanguageCatalog.LanguageInfo> languages,
        string currentLanguageFile,
        string updateChannel,
        string? currentLanguagePackageId = null)
    {
        _language = language;
        _languages = languages;
        _currentLanguageFile = currentLanguageFile;
        _currentLanguagePackageId = currentLanguagePackageId;
        _updateChannel = UpdateChecker.NormalizeChannel(updateChannel);

        ClientSize = new Size(1320, 700);
        MinimumSize = ClientSize;
        MaximumSize = ClientSize;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        SizeGripStyle = SizeGripStyle.Hide;
        StartPosition = FormStartPosition.CenterParent;
        AppWindowIcon.ApplyTo(this);
        _aboutImage = LoadAboutImage();

        RebuildContent();
    }

    // Zoek/commentaar: Methode Dispose: centrale logica voor deze stap.
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _aboutImage?.Dispose();

        base.Dispose(disposing);
    }

    // Zoek/commentaar: Bouwt dit UI-onderdeel opnieuw op voor RebuildContent.
    private void RebuildContent()
    {
        Text = T("dialog.about_title", "About Syscalculator 2.0");

        var oldControls = Controls.Cast<Control>().ToList();
        Controls.Clear();
        foreach (var control in oldControls)
            control.Dispose();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(10),
            BackColor = Color.FromArgb(246, 248, 252),
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 928));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

        var hero = BuildHeroControl();
        root.Controls.Add(hero, 0, 0);

        var info = BuildInfoPanel(_currentLanguageFile);
        root.Controls.Add(info, 1, 0);

        var bottom = BuildBottomBar();
        root.SetColumnSpan(bottom, 2);
        root.Controls.Add(bottom, 0, 1);

        Controls.Add(root);
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildHeroControl.
    private Control BuildHeroControl()
    {
        if (_aboutImage is null)
        {
            var fallbackHost = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                BackColor = Color.White,
                BorderColor = Color.FromArgb(103, 158, 216),
                SecondaryBorderColor = Color.FromArgb(198, 218, 240),
                CornerRadius = 10,
            };

            var fallbackLabel = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Text = "Syscalculator 2.0",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 65, 170),
            };
            fallbackHost.Controls.Add(fallbackLabel);
            return fallbackHost;
        }

        var host = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            BackColor = Color.White,
            BorderColor = Color.Transparent,
            SecondaryBorderColor = Color.Transparent,
            CornerRadius = 10,
            AutoScroll = false,
        };

        var picture = new PictureBox
        {
            Location = Point.Empty,
            Size = _aboutImage.Size,
            BackColor = Color.White,
            Image = _aboutImage,
            SizeMode = PictureBoxSizeMode.Normal,
        };

        host.Controls.Add(picture);
        return host;
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildInfoPanel.
    private Control BuildInfoPanel(string currentLanguageFile)
    {
        var panel = new RoundedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            Padding = new Padding(2),
            BackColor = Color.White,
            BorderColor = Color.FromArgb(103, 158, 216),
            SecondaryBorderColor = Color.FromArgb(200, 220, 241),
            CornerRadius = 10,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 178));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 114));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

      
        panel.Controls.Add(BuildVersionSection(), 0, 0);
        panel.Controls.Add(BuildCopyrightSection(), 0, 1);
        panel.Controls.Add(BuildDescriptionSection(), 0, 2);
        panel.Controls.Add(BuildContactSection(), 0, 3);
        panel.Controls.Add(BuildLanguageSection(currentLanguageFile), 0, 4);

        return panel;
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildVersionSection.
    private Control BuildVersionSection()
    {
        var grid = CreateSection("\u24D8", T("about.version_title", "Version information"));
        AddPair(grid, T("about.product_name", "Product name:"), AppVersionInfo.ProductName);
        AddPair(grid, T("about.version", "Version:"), AppVersionInfo.ProductVersion);
        AddPair(grid, T("about.release_channel", "Release channel:"), AppVersionInfo.ReleaseChannel);
        AddPair(grid, T("about.update_channel", "Update channel:"), FormatUpdateChannel(_updateChannel));
        AddPair(grid, T("about.build", "Build date:"), AppVersionInfo.BuildNumber);
        AddPair(grid, T("about.release_date", "Release date:"), FormatReleaseDate(AppVersionInfo.ReleaseDate));
        return grid;
    }

    private string FormatUpdateChannel(string channel)
    {
        return channel.Equals("beta", StringComparison.OrdinalIgnoreCase)
            ? T("menu.config.update_channel.beta", "Beta")
            : T("menu.config.update_channel.daily", "Daily");
    }

    private string GetLicenseText()
    {
        if (AppVersionInfo.ReleaseChannel.Equals("Beta", StringComparison.OrdinalIgnoreCase))
            return T("about.license_text.beta", "Syscalculator 2.0 beta build. Internal evaluation license.");

        if (AppVersionInfo.ReleaseChannel.Equals("Stable", StringComparison.OrdinalIgnoreCase))
            return T("about.license_text.stable", "Syscalculator 2.0. Licensed software.");

        return T("about.license_text.daily", "Syscalculator 2.0 daily build. Internal evaluation license.");
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildCopyrightSection.
    private Control BuildCopyrightSection()
    {
        var grid = CreateSection("\u00A9", T("about.copyright_title", "Copyright"));
        grid.Margin = new Padding(0, 0, 0, 0);
        AddFullRow(grid, "\u00A9 1995\u20132026 Edward Tie / Tiedragon", 24);
        AddFullRow(grid, T("about.rights", "All rights reserved."), 24);
        return grid;
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildDescriptionSection.
    private Control BuildDescriptionSection()
    {
        var grid = CreateSection("\u25A4", T("about.description_title", "Description"));
        AddFullRow(grid, T("about.description_text", "NOD conversion and calculation system for temperature, weights, dimensions, currency conversion and more. Fast, accurate and flexible."), 46);
        return grid;
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildContactSection.
    private Control BuildContactSection()
    {
        var grid = CreateSection("\U0001F310", T("about.website_title", "Website"));
        AddLinkRow(grid, AppVersionInfo.Website, AppVersionInfo.Website);
        AddIconFullRow(grid, "\u2709", T("about.email_title", "E-mail"), 28, FontStyle.Bold);
        AddLinkRow(grid, AppVersionInfo.Email, "mailto:" + AppVersionInfo.Email);
        return grid;
    }

    private string GetPrivacyUrl()
    {
        return _currentLanguageFile.Equals("ned.lng", StringComparison.OrdinalIgnoreCase)
            ? AppVersionInfo.PrivacyUrlDutch
            : AppVersionInfo.PrivacyUrl;
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildLanguageSection.
    private Control BuildLanguageSection(string currentLanguageFile)
    {
        var grid = CreateSection("\u6587", T("about.language_title", "Language"));
        _languageCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Top,
            DisplayMember = nameof(LanguageCatalog.LanguageInfo.DisplayName),
        };

        _languageCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_initializingLanguage)
                return;

            if (_languageCombo.SelectedItem is LanguageCatalog.LanguageInfo language)
                ApplySelectedLanguage(language);
        };

        _initializingLanguage = true;
        foreach (var language in _languages)
            _languageCombo.Items.Add(language);

        for (var i = 0; i < _languageCombo.Items.Count; i++)
        {
            if (_languageCombo.Items[i] is LanguageCatalog.LanguageInfo language &&
                language.Matches(currentLanguageFile, _currentLanguagePackageId))
            {
                _languageCombo.SelectedIndex = i;
                break;
            }
        }

        _initializingLanguage = false;

        if (_languageCombo.SelectedIndex < 0 && _languageCombo.Items.Count > 0)
            _languageCombo.SelectedIndex = 0;

        var comboRow = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        grid.Controls.Add(_languageCombo, 1, comboRow);
        grid.SetColumnSpan(_languageCombo, 2);
        return grid;
    }

    // Zoek/commentaar: Past een regel, instelling of bewerking toe voor ApplySelectedLanguage.
    private void ApplySelectedLanguage(string fileName)
    {
        ApplySelectedLanguage(new LanguageCatalog.LanguageInfo(Path.GetFileNameWithoutExtension(fileName), fileName));
    }

    private void ApplySelectedLanguage(LanguageCatalog.LanguageInfo languageInfo)
    {
        if (languageInfo.Matches(_currentLanguageFile, _currentLanguagePackageId))
            return;

        SelectedLanguage = languageInfo;
        SelectedLanguageFile = languageInfo.FileName;
        SelectedLanguagePackageId = languageInfo.PackageId;
        _currentLanguageFile = languageInfo.FileName;
        _currentLanguagePackageId = languageInfo.PackageId;
        _language = LanguageCatalog.Load(AppContext.BaseDirectory, languageInfo.FileName, languageInfo.PackageId);
        RefreshContentAfterComboEvent();
    }

    // Zoek/commentaar: Ververst de getoonde data of UI voor RefreshContentAfterComboEvent.
    private void RefreshContentAfterComboEvent()
    {
        if (_languageRefreshPending)
            return;

        _languageRefreshPending = true;
        BeginInvoke(new Action(() =>
        {
            _languageRefreshPending = false;
            RebuildContent();
        }));
    }


    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildBottomBar.
    private Control BuildBottomBar()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(14, 10, 14, 8),
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 600));

        var systemButton = new Button
        {
            Text = T("about.system_info", "System information..."),
            Width = 175,
            Height = 34,
            Anchor = AnchorStyles.Left,
        };
        systemButton.Click += (_, _) => ShowSystemInformation();

        var licenseButton = new Button
        {
            Text = T("about.license", "License..."),
            Width = 150,
            Height = 34,
            Anchor = AnchorStyles.None,
        };
        licenseButton.Click += (_, _) => MessageBox.Show(
            this,
            GetLicenseText(),
            T("about.license", "License..."),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        var privacyButton = new Button
        {
            Text = T("about.privacy", "Privacy..."),
            Width = 130,
            Height = 34,
            Anchor = AnchorStyles.None,
        };
        privacyButton.Click += (_, _) => OpenLink(GetPrivacyUrl());

        var okButton = new Button
        {
            Text = "OK",
            Width = 116,
            Height = 34,
            Anchor = AnchorStyles.Right,
            DialogResult = DialogResult.OK,
        };

        var buttonRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
        };
        buttonRow.Controls.Add(okButton);
        buttonRow.Controls.Add(licenseButton);
        buttonRow.Controls.Add(privacyButton);
        buttonRow.Controls.Add(systemButton);

        bar.Controls.Add(buttonRow, 1, 0);

        AcceptButton = okButton;
        return bar;
    }

    // Zoek/commentaar: Maakt een nieuw object of hulponderdeel voor CreateSection.
    private TableLayoutPanel CreateSection(string iconText, string title)
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(12, 6, 6, 4),
            BackColor = Color.White,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 138));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        var icon = new Label
        {
            Text = iconText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Symbol", 17, FontStyle.Regular),
            ForeColor = Color.FromArgb(0, 82, 202),
        };
        grid.Controls.Add(icon, 0, 0);

        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 65, 170),
        };
        grid.Controls.Add(titleLabel, 1, 0);
        grid.SetColumnSpan(titleLabel, 2);

        return grid;
    }

    // Zoek/commentaar: Voegt data of UI-regels toe voor AddPair.
    private static void AddPair(TableLayoutPanel grid, string label, string value)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        grid.Controls.Add(CreateTextLabel(label, FontStyle.Regular), 1, row);
        grid.Controls.Add(CreateValueLabel(value), 2, row);
    }

    // Zoek/commentaar: Voegt data of UI-regels toe voor AddFullRow.
    private static void AddFullRow(TableLayoutPanel grid, string text, int rowHeight = 22)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
        var label = CreateValueLabel(text);
        label.Margin = Padding.Empty;
        grid.Controls.Add(label, 1, row);
        grid.SetColumnSpan(label, 2);
    }

    // Zoek/commentaar: Voegt data of UI-regels toe voor AddIconFullRow.
    private static void AddIconFullRow(TableLayoutPanel grid, string iconText, string text, int rowHeight = 22, FontStyle style = FontStyle.Regular)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));

        var icon = new Label
        {
            Text = iconText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Symbol", 17, FontStyle.Regular),
            ForeColor = Color.FromArgb(0, 82, 202),
            Margin = Padding.Empty,
        };
        grid.Controls.Add(icon, 0, row);

        var label = CreateTextLabel(text, style);
        label.Margin = Padding.Empty;
        label.ForeColor = Color.FromArgb(0, 65, 170);
        grid.Controls.Add(label, 1, row);
        grid.SetColumnSpan(label, 2);
    }

    // Zoek/commentaar: Voegt data of UI-regels toe voor AddLinkRow.
    private static void AddLinkRow(TableLayoutPanel grid, string text, string target, FontStyle style = FontStyle.Regular)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        var link = new LinkLabel
        {
            Text = text,
            Dock = DockStyle.Fill,
            LinkColor = Color.FromArgb(0, 82, 202),
            ActiveLinkColor = Color.FromArgb(0, 110, 220),
            Font = new Font("Segoe UI", 9.2f, style),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        link.LinkClicked += (_, _) => OpenLink(target);
        grid.Controls.Add(link, 1, row);
        grid.SetColumnSpan(link, 2);
    }

    // Zoek/commentaar: Maakt een nieuw object of hulponderdeel voor CreateTextLabel.
    private static Label CreateTextLabel(string text, FontStyle style)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.2f, style),
            TextAlign = ContentAlignment.MiddleLeft,
        };
    }

    // Zoek/commentaar: Maakt een nieuw object of hulponderdeel voor CreateValueLabel.
    private static Label CreateValueLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.2f, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft,
        };
    }

    // Zoek/commentaar: Toont een venster, melding of detailweergave voor ShowSystemInformation.
    private void ShowSystemInformation()
    {
        using var dialog = new Form
        {
            Text = T("about.system_info", "System information..."),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(820, 540),
            ShowInTaskbar = false,
            BackColor = Color.FromArgb(246, 248, 252)
        };
        AppWindowIcon.ApplyTo(dialog);

        var infoText = BuildSystemInformationText();
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(22, 18, 22, 14)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 138));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        var iconPanel = new SystemInfoComputerIconPanel
        {
            Dock = DockStyle.Top,
            Height = 112,
            Margin = new Padding(0, 10, 16, 0)
        };
        root.Controls.Add(iconPanel, 0, 0);

        var content = new RoundedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(14),
            BackColor = Color.White,
            BorderColor = Color.FromArgb(190, 208, 233),
            SecondaryBorderColor = Color.FromArgb(230, 238, 249),
            CornerRadius = 9
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var titleBlock = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Margin = new Padding(0, 0, 0, 4)
        };
        titleBlock.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        titleBlock.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        titleBlock.Controls.Add(new Label
        {
            Text = T("about.system_info", "System information..."),
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 65, 170),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        titleBlock.Controls.Add(new Label
        {
            Text = T("about.system_info_subtitle", "Information for support and feedback."),
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.2f),
            ForeColor = Color.FromArgb(76, 89, 108),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);
        content.Controls.Add(titleBlock, 0, 0);

        var process = Process.GetCurrentProcess();
        var runtime = RuntimeDiagnostics.Capture();
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Point(12, 4)
        };

        tabs.TabPages.Add(CreateSystemInfoTabPage("Windows", new[]
        {
            ("Windows", GetWindowsProductName()),
            ("Versie", GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion")),
            ("Editie", GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "EditionID")),
            ("Build", GetWindowsBuildText()),
            ("OS", RuntimeInformation.OSDescription),
            ("Architectuur", RuntimeInformation.OSArchitecture.ToString()),
            ("Machine", Environment.MachineName)
        }));

        tabs.TabPages.Add(CreateSystemInfoTabPage("Hardware", new[]
        {
            ("Merk", GetRegistryValue(@"HARDWARE\DESCRIPTION\System\BIOS", "SystemManufacturer")),
            ("Model", GetRegistryValue(@"HARDWARE\DESCRIPTION\System\BIOS", "SystemProductName")),
            ("Moederbord", JoinNonEmpty(" ", GetRegistryValue(@"HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardManufacturer"), GetRegistryValue(@"HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardProduct"))),
            ("BIOS", GetRegistryValue(@"HARDWARE\DESCRIPTION\System\BIOS", "BIOSVersion")),
            ("CPU", GetRegistryValue(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString")),
            ("Processoren", Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture)),
            ("Videokaarten", GetVideoAdapterText()),
            ("Geheugen", $"{GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024} MB"),
            ("Scherm", GetScreenText())
        }));

        tabs.TabPages.Add(CreateSystemInfoTabPage("Runtime", new[]
        {
            ("Product", AppVersionInfo.ProductName),
            ("Versie", AppVersionInfo.ProductVersion),
            ("Channel", AppVersionInfo.ReleaseChannel),
            ("Build date", AppVersionInfo.BuildNumber),
            (T("about.runtime_app", "App runtime"), runtime.AppRuntimeDescription),
            (T("about.runtime_bundled", "Bundled"), runtime.FormatBundledForDisplay()),
            (T("about.runtime_installed", "Installed"), runtime.FormatInstalledForDisplay(T)),
            (T("about.runtime_status", "Status"), runtime.FormatStatusForDisplay(T)),
            ("Proces", RuntimeInformation.ProcessArchitecture.ToString()),
            ("Geheugen", $"{process.WorkingSet64 / 1024 / 1024} MB")
        }));

        tabs.TabPages.Add(CreateSystemInfoTabPage("Paden", new[]
        {
            ("Applicatie", Application.ExecutablePath),
            ("Basismap", AppContext.BaseDirectory),
            ("Werkmap", Environment.CurrentDirectory)
        }));

        content.Controls.Add(tabs, 0, 1);

        root.Controls.Add(content, 1, 0);

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 12, 0, 0)
        };

        var okButton = new Button
        {
            Text = "OK",
            Width = 104,
            Height = 30,
            DialogResult = DialogResult.OK
        };

        var copyButton = new Button
        {
            Text = T("about.copy_system_info", "Copy"),
            Image = CreateCopyButtonImage(),
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            Width = 104,
            Height = 30
        };
        copyButton.Click += (_, _) => Clipboard.SetText(infoText);

        bottom.Controls.Add(okButton);
        bottom.Controls.Add(copyButton);
        root.SetColumnSpan(bottom, 2);
        root.Controls.Add(bottom, 0, 1);

        dialog.Controls.Add(root);
        dialog.AcceptButton = okButton;
        dialog.Shown += (_, _) => okButton.Focus();
        dialog.ShowDialog(this);
    }

    private static TabPage CreateSystemInfoTabPage(string title, IReadOnlyList<(string Label, string Value)> rows)
    {
        var page = new TabPage(title)
        {
            BackColor = Color.White,
            Padding = new Padding(12)
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = rows.Count,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 122));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        for (var i = 0; i < rows.Count; i++)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            grid.Controls.Add(new Label
            {
                Text = rows[i].Label,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(62, 77, 99),
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, i);
            grid.Controls.Add(new Label
            {
                Text = NormalizeSystemInfoValue(rows[i].Value),
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(20, 35, 55),
                TextAlign = ContentAlignment.MiddleLeft
            }, 1, i);
        }

        page.Controls.Add(grid);
        return page;
    }

    private static Bitmap CreateCopyButtonImage()
    {
        var bitmap = new Bitmap(18, 18);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var fillBack = new SolidBrush(Color.FromArgb(238, 246, 255));
        using var fillFront = new SolidBrush(Color.FromArgb(250, 253, 255));
        using var outline = new Pen(Color.FromArgb(0, 65, 170), 1.4f)
        {
            LineJoin = LineJoin.Round
        };

        var back = new Rectangle(6, 5, 8, 10);
        var front = new Rectangle(3, 2, 8, 10);
        using (var backPath = RoundedPanel.CreateRoundedRectangle(back, 2))
        {
            graphics.FillPath(fillBack, backPath);
            graphics.DrawPath(outline, backPath);
        }

        using (var frontPath = RoundedPanel.CreateRoundedRectangle(front, 2))
        {
            graphics.FillPath(fillFront, frontPath);
            graphics.DrawPath(outline, frontPath);
        }

        return bitmap;
    }

    private string BuildSystemInformationText()
    {
        var process = Process.GetCurrentProcess();
        var sb = new StringBuilder();
        sb.AppendLine("Syscalculator");
        sb.AppendLine("-------------");
        sb.AppendLine($"Product: {AppVersionInfo.ProductName}");
        sb.AppendLine($"Version: {AppVersionInfo.ProductVersion}");
        sb.AppendLine($"Channel: {AppVersionInfo.ReleaseChannel}");
        sb.AppendLine($"Build date: {AppVersionInfo.BuildNumber}");
        sb.AppendLine($"Release date: {AppVersionInfo.ReleaseDate:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine("System");
        sb.AppendLine("------");
        sb.AppendLine($"Windows: {GetWindowsProductName()}");
        sb.AppendLine($"Version: {GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "DisplayVersion")}");
        sb.AppendLine($"Edition: {GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "EditionID")}");
        sb.AppendLine($"Build: {GetWindowsBuildText()}");
        sb.AppendLine($"OS: {RuntimeInformation.OSDescription}");
        sb.AppendLine($"OS version: {Environment.OSVersion}");
        sb.AppendLine($"Architecture: {RuntimeInformation.OSArchitecture}");
        sb.AppendLine($"Machine: {Environment.MachineName}");
        sb.AppendLine($"User: {Environment.UserName}");
        sb.AppendLine();
        sb.AppendLine("Hardware");
        sb.AppendLine("--------");
        sb.AppendLine($"Manufacturer: {GetRegistryValue(@"HARDWARE\DESCRIPTION\System\BIOS", "SystemManufacturer")}");
        sb.AppendLine($"Model: {GetRegistryValue(@"HARDWARE\DESCRIPTION\System\BIOS", "SystemProductName")}");
        sb.AppendLine($"Motherboard: {JoinNonEmpty(" ", GetRegistryValue(@"HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardManufacturer"), GetRegistryValue(@"HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardProduct"))}");
        sb.AppendLine($"BIOS: {GetRegistryValue(@"HARDWARE\DESCRIPTION\System\BIOS", "BIOSVersion")}");
        sb.AppendLine($"CPU: {GetRegistryValue(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString")}");
        sb.AppendLine($"Processor count: {Environment.ProcessorCount}");
        sb.AppendLine($"GPUs: {GetVideoAdapterText()}");
        sb.AppendLine($"Available managed memory: {GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024 / 1024} MB");
        sb.AppendLine($"Screens: {GetScreenText()}");
        sb.AppendLine($"Screen count: {Screen.AllScreens.Length}");
        sb.AppendLine();
        sb.AppendLine("Runtime");
        sb.AppendLine("-------");
        sb.Append(RuntimeDiagnostics.Capture().ToSupportText(T));
        sb.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
        sb.AppendLine($"64-bit process: {Environment.Is64BitProcess}");
        sb.AppendLine($"Processor count: {Environment.ProcessorCount}");
        sb.AppendLine($"Working set: {process.WorkingSet64 / 1024 / 1024} MB");
        sb.AppendLine();
        sb.AppendLine("Paths");
        sb.AppendLine("-----");
        sb.AppendLine($"Application: {Application.ExecutablePath}");
        sb.AppendLine($"Base directory: {AppContext.BaseDirectory}");
        sb.AppendLine($"Current directory: {Environment.CurrentDirectory}");
        return sb.ToString();
    }

    private static string GetWindowsProductName()
    {
        var productName = GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName");
        if (int.TryParse(GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuildNumber"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var build) &&
            build >= 22000 &&
            productName.Contains("Windows 10", StringComparison.OrdinalIgnoreCase))
        {
            productName = productName.Replace("Windows 10", "Windows 11", StringComparison.OrdinalIgnoreCase);
        }

        return productName;
    }

    private static string GetWindowsBuildText()
    {
        var build = GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuildNumber");
        var ubr = GetRegistryValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "UBR");
        return string.IsNullOrWhiteSpace(ubr) || ubr == "-"
            ? build
            : $"{build}.{ubr}";
    }

    private static string GetScreenText()
    {
        if (Screen.AllScreens.Length == 0)
        {
            return "-";
        }

        return string.Join(", ", Screen.AllScreens.Select(screen =>
            $"{screen.Bounds.Width} x {screen.Bounds.Height}{(screen.Primary ? " primary" : string.Empty)}"));
    }

    private static string GetVideoAdapterText()
    {
        var adapters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var device = new DisplayDevice();
            device.cb = Marshal.SizeOf<DisplayDevice>();
            for (uint i = 0; EnumDisplayDevices(null, i, ref device, 0); i++)
            {
                AddVideoAdapter(adapters, device.DeviceString, "-");
                device = new DisplayDevice();
                device.cb = Marshal.SizeOf<DisplayDevice>();
            }
        }
        catch
        {
            // Display device enumeration is best-effort; registry fallback below can still find adapters.
        }

        try
        {
            using var videoKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Video");
            foreach (var adapterKeyName in videoKey?.GetSubKeyNames() ?? Array.Empty<string>())
            {
                using var adapterKey = videoKey?.OpenSubKey(adapterKeyName);
                foreach (var deviceKeyName in adapterKey?.GetSubKeyNames() ?? Array.Empty<string>())
                {
                    using var deviceKey = adapterKey?.OpenSubKey(deviceKeyName);
                    var name = RegistryValueToString(deviceKey?.GetValue("HardwareInformation.AdapterString"));
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = RegistryValueToString(deviceKey?.GetValue("DriverDesc"));
                    }

                    AddVideoAdapter(adapters, name, RegistryMemoryToText(deviceKey?.GetValue("HardwareInformation.MemorySize")));
                }
            }
        }
        catch
        {
            // Hardware registry access is best-effort; copy text still works without it.
        }

        AddDisplayClassAdapters(adapters);

        return adapters.Count == 0
            ? "-"
            : string.Join(", ", adapters.Select(adapter => adapter.Value == "-" ? adapter.Key : $"{adapter.Key} ({adapter.Value})"));
    }

    private static void AddDisplayClassAdapters(Dictionary<string, string> adapters)
    {
        try
        {
            using var classKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
            foreach (var deviceKeyName in classKey?.GetSubKeyNames() ?? Array.Empty<string>())
            {
                using var deviceKey = classKey?.OpenSubKey(deviceKeyName);
                var name = RegistryValueToString(deviceKey?.GetValue("HardwareInformation.AdapterString"));
                if (name == "-")
                {
                    name = RegistryValueToString(deviceKey?.GetValue("DriverDesc"));
                }

                AddVideoAdapter(adapters, name, RegistryMemoryToText(deviceKey?.GetValue("HardwareInformation.MemorySize")));
            }
        }
        catch
        {
            // Driver class details differ per vendor; other GPU sources remain available.
        }
    }

    private static void AddVideoAdapter(Dictionary<string, string> adapters, string name, string memory)
    {
        name = CleanDeviceDescription(name);
        name = NormalizeSystemInfoValue(name);
        if (name == "-" ||
            name.Equals("Microsoft Basic Display Driver", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Remote Display", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Mirror Driver", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        memory = NormalizeSystemInfoValue(memory);
        if (!adapters.TryGetValue(name, out var existingMemory) || existingMemory == "-")
        {
            adapters[name] = memory;
        }
    }

    private static string RegistryMemoryToText(object? value)
    {
        var bytes = value switch
        {
            int number => number,
            uint number => number,
            long number => number,
            ulong number => number <= long.MaxValue ? (long)number : 0,
            byte[] raw when raw.Length >= 4 => BitConverter.ToUInt32(raw, 0),
            _ => 0L
        };

        return bytes <= 0 ? "-" : FormatBytes(bytes);
    }

    private static string FormatBytes(long bytes)
    {
        const long mb = 1024L * 1024L;
        const long gb = 1024L * mb;
        return bytes >= gb && bytes % gb == 0
            ? $"{bytes / gb} GB"
            : $"{Math.Max(1, bytes / mb)} MB";
    }

    private static string CleanDeviceDescription(string value)
    {
        value = NormalizeSystemInfoValue(value);
        var lastSemicolon = value.LastIndexOf(';');
        return lastSemicolon >= 0 && lastSemicolon + 1 < value.Length
            ? value[(lastSemicolon + 1)..].Trim()
            : value;
    }

    private static string GetRegistryValue(string keyName, string valueName)
    {
        try
        {
            return RegistryValueToString(Registry.GetValue($@"HKEY_LOCAL_MACHINE\{keyName}", valueName, null));
        }
        catch
        {
            return "-";
        }
    }

    private static string RegistryValueToString(object? value)
    {
        return value switch
        {
            null => "-",
            string text when string.IsNullOrWhiteSpace(text) => "-",
            string text => text.Trim(),
            string[] items => JoinNonEmpty(", ", items),
            byte[] bytes => Encoding.Unicode.GetString(bytes).TrimEnd('\0', ' '),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "-"
        };
    }

    private static string NormalizeSystemInfoValue(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    private static string JoinNonEmpty(string separator, params string[] values)
    {
        var joined = string.Join(separator, values.Where(value => !string.IsNullOrWhiteSpace(value) && value != "-").Select(value => value.Trim()));
        return string.IsNullOrWhiteSpace(joined) ? "-" : joined;
    }

    // Zoek/commentaar: Opent een venster, bestand of link voor OpenLink.
    private static void OpenLink(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch
        {
        }
    }

    // Zoek/commentaar: Laadt gegevens of instellingen voor LoadAboutImage.
    private static Image? LoadAboutImage()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "AboutSyscalculator.png");
        if (!File.Exists(path))
            return null;

        using var stream = File.OpenRead(path);
        using var image = Image.FromStream(stream);
        return new Bitmap(image);
    }

    // Zoek/commentaar: Methode T: centrale logica voor deze stap.
    private string T(string key, string fallback) => _language.Text(key, fallback);

    // Zoek/commentaar: Maakt tekst of waarden netjes leesbaar voor FormatReleaseDate.
    private string FormatReleaseDate(string releaseDate)
    {
        if (!DateTime.TryParseExact(
                releaseDate,
                "dd-MM-yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            return releaseDate;
        }

        var culture = GetLanguageCulture(_language.FileName);
        if (culture.Name.Equals("zh-CN", StringComparison.OrdinalIgnoreCase))
            return date.ToString("yyyy'å¹´'M'æœˆ'd'æ—¥'", culture);

        return date.ToString("d MMMM yyyy", culture);
    }

    // Zoek/commentaar: Haalt een waarde, tekst of instelling op voor GetLanguageCulture.
    private static CultureInfo GetLanguageCulture(string fileName)
    {
        var cultureName = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant() switch
        {
            "deu" => "de-DE",
            "eng" => "en-GB",
            "fra" => "fr-FR",
            "ind" => "id-ID",
            "ita" => "it-IT",
            "ned" => "nl-NL",
            "por" => "pt-PT",
            "spa" => "es-ES",
            "zho" => "zh-CN",
            _ => "en-GB",
        };

        return CultureInfo.GetCultureInfo(cultureName);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DisplayDevice lpDisplayDevice, uint dwFlags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public int cb;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;

        public int StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

}

// Zoek/commentaar: Type-overzicht: class RoundedPanel bevat de hoofdlogica/data voor dit onderdeel.
internal class RoundedPanel : Panel
{
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius { get; set; } = 10;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Color.FromArgb(99, 157, 214);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color SecondaryBorderColor { get; set; } = Color.FromArgb(198, 218, 240);

    // Zoek/commentaar: Methode OnSizeChanged: centrale logica voor deze stap.
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateRoundedRegion();
    }

    // Zoek/commentaar: Methode OnPaint: centrale logica voor deze stap.
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        PaintRoundedBorder(e.Graphics, ClientRectangle, CornerRadius, BorderColor, SecondaryBorderColor);
    }

    // Zoek/commentaar: Werkt UI, status of data bij voor UpdateRoundedRegion.
    private void UpdateRoundedRegion()
    {
        if (Width <= 0 || Height <= 0)
            return;

        Region?.Dispose();
        using var path = CreateRoundedRectangle(new Rectangle(0, 0, Width, Height), CornerRadius);
        Region = new Region(path);
    }

    // Zoek/commentaar: Schildert/tekent het control-onderdeel voor PaintRoundedBorder.
    internal static void PaintRoundedBorder(Graphics graphics, Rectangle bounds, int radius, Color color, Color secondaryColor)
    {
        if (bounds.Width <= 1 || bounds.Height <= 1 || color.A == 0)
            return;

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var borderBounds = Rectangle.Inflate(bounds, -1, -1);
        using var path = CreateRoundedRectangle(borderBounds, radius);
        using var pen = new Pen(color);
        graphics.DrawPath(pen, path);

        if (secondaryColor.A == 0)
            return;

        var innerBorderBounds = Rectangle.Inflate(bounds, -2, -2);
        using var innerPath = CreateRoundedRectangle(innerBorderBounds, Math.Max(1, radius - 1));
        using var innerPen = new Pen(secondaryColor);
        graphics.DrawPath(innerPen, innerPath);
    }

    // Zoek/commentaar: Maakt een nieuw object of hulponderdeel voor CreateRoundedRectangle.
    internal static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius) => UiGeometry.CreateRoundedRectangle(bounds, radius);
}

internal sealed class SystemInfoComputerIconPanel : Panel
{
    private readonly Image? _miniPcImage;

    public SystemInfoComputerIconPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        _miniPcImage = LoadMiniPcImage();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        if (_miniPcImage is not null)
        {
            PaintMiniPcImage(e.Graphics);
            return;
        }

        var bounds = ClientRectangle;
        var centerX = bounds.Left + bounds.Width / 2f - 7f;
        var top = bounds.Top + 8f;

        using var ambientShadow = new SolidBrush(Color.FromArgb(24, 42, 73, 112));
        using var screenBack = new LinearGradientBrush(
            new RectangleF(centerX - 37, top + 2, 68, 43),
            Color.FromArgb(250, 253, 255),
            Color.FromArgb(214, 232, 250),
            LinearGradientMode.Vertical);
        using var glass = new LinearGradientBrush(
            new RectangleF(centerX - 30, top + 8, 56, 15),
            Color.FromArgb(120, 255, 255, 255),
            Color.FromArgb(25, 255, 255, 255),
            LinearGradientMode.Vertical);
        using var screenBorder = new Pen(Color.FromArgb(56, 92, 132), 1.6f);
        using var standBrush = new SolidBrush(Color.FromArgb(86, 110, 139));
        using var standLight = new SolidBrush(Color.FromArgb(122, 150, 181));
        using var miniPcBack = new LinearGradientBrush(
            new RectangleF(centerX - 7, top + 63, 66, 25),
            Color.FromArgb(250, 253, 255),
            Color.FromArgb(192, 211, 232),
            LinearGradientMode.Vertical);
        using var miniPcSide = new LinearGradientBrush(
            new RectangleF(centerX + 39, top + 64, 19, 23),
            Color.FromArgb(210, 226, 244),
            Color.FromArgb(142, 166, 194),
            LinearGradientMode.Horizontal);
        using var miniPcBorder = new Pen(Color.FromArgb(70, 98, 132), 1.35f);
        using var accentBrush = new SolidBrush(Color.FromArgb(0, 102, 204));
        using var portBrush = new SolidBrush(Color.FromArgb(72, 95, 122));
        using var portLine = new Pen(Color.FromArgb(108, 132, 162), 1f);

        var screen = new RectangleF(centerX - 39, top, 70, 45);
        using (var shadowPath = RoundedPanel.CreateRoundedRectangle(Rectangle.Round(new RectangleF(screen.X + 3, screen.Y + 4, screen.Width, screen.Height)), 8))
        {
            e.Graphics.FillPath(ambientShadow, shadowPath);
        }

        using (var screenPath = RoundedPanel.CreateRoundedRectangle(Rectangle.Round(screen), 8))
        {
            e.Graphics.FillPath(screenBack, screenPath);
            e.Graphics.DrawPath(screenBorder, screenPath);
        }

        using (var glassPath = RoundedPanel.CreateRoundedRectangle(Rectangle.Round(new RectangleF(screen.X + 7, screen.Y + 7, screen.Width - 14, 15)), 5))
        {
            e.Graphics.FillPath(glass, glassPath);
        }

        e.Graphics.FillRectangle(standBrush, centerX - 6, top + 46, 12, 14);
        e.Graphics.FillRectangle(standLight, centerX - 5, top + 46, 3, 14);
        using (var basePath = RoundedPanel.CreateRoundedRectangle(Rectangle.Round(new RectangleF(centerX - 25, top + 58, 50, 8)), 3))
        {
            e.Graphics.FillPath(standBrush, basePath);
        }

        var miniPc = new RectangleF(centerX - 8, top + 65, 67, 24);
        using (var miniPcShadow = RoundedPanel.CreateRoundedRectangle(Rectangle.Round(new RectangleF(miniPc.X + 2, miniPc.Y + 3, miniPc.Width, miniPc.Height)), 5))
        {
            e.Graphics.FillPath(ambientShadow, miniPcShadow);
        }

        using (var miniPcPath = RoundedPanel.CreateRoundedRectangle(Rectangle.Round(miniPc), 5))
        {
            e.Graphics.FillPath(miniPcBack, miniPcPath);
            e.Graphics.DrawPath(miniPcBorder, miniPcPath);
        }

        using (var sidePath = RoundedPanel.CreateRoundedRectangle(Rectangle.Round(new RectangleF(miniPc.Right - 20, miniPc.Y + 1, 19, miniPc.Height - 2)), 4))
        {
            e.Graphics.FillPath(miniPcSide, sidePath);
        }

        e.Graphics.FillEllipse(accentBrush, miniPc.X + 9, miniPc.Y + 9, 5, 5);
        e.Graphics.FillRectangle(portBrush, miniPc.X + 22, miniPc.Y + 8, 7, 7);
        e.Graphics.DrawRectangle(portLine, Rectangle.Round(new RectangleF(miniPc.X + 33, miniPc.Y + 8, 8, 7)));
        e.Graphics.DrawLine(portLine, miniPc.Right - 15, miniPc.Y + 9, miniPc.Right - 6, miniPc.Y + 9);
        e.Graphics.DrawLine(portLine, miniPc.Right - 15, miniPc.Y + 15, miniPc.Right - 6, miniPc.Y + 15);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _miniPcImage?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void PaintMiniPcImage(Graphics graphics)
    {
        var available = Rectangle.Inflate(ClientRectangle, -2, -6);
        if (available.Width <= 0 || available.Height <= 0 || _miniPcImage is null)
        {
            return;
        }

        var imageRatio = _miniPcImage.Width / (float)_miniPcImage.Height;
        var targetRatio = available.Width / (float)available.Height;
        int width;
        int height;
        if (imageRatio > targetRatio)
        {
            width = available.Width;
            height = Math.Max(1, (int)(width / imageRatio));
        }
        else
        {
            height = available.Height;
            width = Math.Max(1, (int)(height * imageRatio));
        }

        var target = new Rectangle(
            available.Left + (available.Width - width) / 2,
            available.Top + (available.Height - height) / 2,
            width,
            height);

        using var path = RoundedPanel.CreateRoundedRectangle(target, 8);
        var oldClip = graphics.Clip;
        graphics.SetClip(path);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(_miniPcImage, target);
        graphics.Clip = oldClip;
    }

    private static Image? LoadMiniPcImage()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "SystemInfoMiniPc.png");
        if (!File.Exists(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, "SystemInfoMiniPc.png");
        }

        if (!File.Exists(path))
        {
            return null;
        }

        using var stream = File.OpenRead(path);
        using var image = Image.FromStream(stream);
        return new Bitmap(image);
    }
}

// Zoek/commentaar: Type-overzicht: class RoundedTableLayoutPanel bevat de hoofdlogica/data voor dit onderdeel.
internal sealed class RoundedTableLayoutPanel : TableLayoutPanel
{
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius { get; set; } = 10;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Color.FromArgb(99, 157, 214);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color SecondaryBorderColor { get; set; } = Color.FromArgb(198, 218, 240);

    // Zoek/commentaar: Methode OnSizeChanged: centrale logica voor deze stap.
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateRoundedRegion();
    }

    // Zoek/commentaar: Methode OnPaint: centrale logica voor deze stap.
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        RoundedPanel.PaintRoundedBorder(e.Graphics, ClientRectangle, CornerRadius, BorderColor, SecondaryBorderColor);
    }

    // Zoek/commentaar: Werkt UI, status of data bij voor UpdateRoundedRegion.
    private void UpdateRoundedRegion()
    {
        if (Width <= 0 || Height <= 0)
            return;

        Region?.Dispose();
        using var path = RoundedPanel.CreateRoundedRectangle(new Rectangle(0, 0, Width, Height), CornerRadius);
        Region = new Region(path);
    }
}
