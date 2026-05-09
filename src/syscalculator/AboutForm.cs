#nullable enable
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;

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

    public string? SelectedLanguageFile { get; private set; }

    // Zoek/commentaar: Constructor: maakt en initialiseert AboutForm.
    public AboutForm(
        LanguageCatalog language,
        IReadOnlyList<LanguageCatalog.LanguageInfo> languages,
        string currentLanguageFile)
    {
        _language = language;
        _languages = languages;
        _currentLanguageFile = currentLanguageFile;

        ClientSize = new Size(1320, 700);
        MinimumSize = ClientSize;
        MaximumSize = ClientSize;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        SizeGripStyle = SizeGripStyle.Hide;
        StartPosition = FormStartPosition.CenterParent;
        ShowIcon = true;
        Icon = LoadAppIcon();
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
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 154));
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
        AddPair(grid, T("about.build", "Build number:"), AppVersionInfo.BuildNumber);
        AddPair(grid, T("about.release_date", "Release date:"), FormatReleaseDate(AppVersionInfo.ReleaseDate));
        return grid;
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
                ApplySelectedLanguage(language.FileName);
        };

        _initializingLanguage = true;
        foreach (var language in _languages)
            _languageCombo.Items.Add(language);

        for (var i = 0; i < _languageCombo.Items.Count; i++)
        {
            if (_languageCombo.Items[i] is LanguageCatalog.LanguageInfo language &&
                language.FileName.Equals(currentLanguageFile, StringComparison.OrdinalIgnoreCase))
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
        if (fileName.Equals(_currentLanguageFile, StringComparison.OrdinalIgnoreCase))
            return;

        SelectedLanguageFile = fileName;
        _currentLanguageFile = fileName;
        _language = LanguageCatalog.Load(AppContext.BaseDirectory, fileName);
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
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(14, 10, 14, 8),
        };
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));

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
            T("about.license_text", "Syscalculator 2.0 beta build. Internal evaluation license."),
            T("about.license", "License..."),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        var okButton = new Button
        {
            Text = "OK",
            Width = 116,
            Height = 34,
            Anchor = AnchorStyles.Right,
            DialogResult = DialogResult.OK,
        };

        bar.Controls.Add(systemButton, 0, 0);
        bar.Controls.Add(licenseButton, 1, 0);
        bar.Controls.Add(okButton, 2, 0);

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
        var message =
            "OS: " + Environment.OSVersion + Environment.NewLine +
            ".NET: " + Environment.Version + Environment.NewLine +
            "Machine: " + Environment.MachineName + Environment.NewLine +
            "64-bit process: " + Environment.Is64BitProcess;

        MessageBox.Show(this, message, T("about.system_info", "System information..."), MessageBoxButtons.OK, MessageBoxIcon.Information);
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

    // Zoek/commentaar: Laadt gegevens of instellingen voor LoadAppIcon.
    private static Icon? LoadAppIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "Syscalculator.ico");
        return File.Exists(path) ? new Icon(path) : null;
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
    internal static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = Math.Max(1, radius * 2);
        var path = new GraphicsPath();

        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
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
