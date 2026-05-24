#nullable enable
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Reflection;
using Tiedragon.Help;

namespace Tiedragon.ToolEditor;

internal sealed class ToolEditorAboutForm : Form
{
    private const string Website = "https://www.tiedragon.com";
    private const string Email = "info@tiedragon.com";
    private const string DefaultLanguageFile = "ned.lng";

    private ComboBox? _languageCombo;
    private string _selectedLanguageFile = DefaultLanguageFile;
    private Dictionary<string, string> _selectedLanguage = [];
    private readonly Dictionary<string, string> _englishLanguage;

    public ToolEditorAboutForm()
    {
        _englishLanguage = LoadLanguageFile("eng.lng");
        _selectedLanguage = LoadLanguageFile(DefaultLanguageFile);

        Text = "About Syscalculator 2.0";
        ClientSize = new Size(980, 735);
        MinimumSize = ClientSize;
        MaximumSize = ClientSize;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(246, 248, 252);

        BuildContent();
    }

    private void BuildContent()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = Color.FromArgb(246, 248, 252),
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));

        root.Controls.Add(BuildHero(), 0, 0);
        root.Controls.Add(BuildInfoPanel(), 1, 0);

        var bottom = BuildBottomBar();
        root.SetColumnSpan(bottom, 2);
        root.Controls.Add(bottom, 0, 1);

        Controls.Add(root);
    }

    private static Control BuildHero()
    {
        var panel = new AboutRoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0),
            BackColor = Color.White,
            CornerRadius = 10,
            BorderColor = Color.FromArgb(103, 158, 216),
            SecondaryBorderColor = Color.FromArgb(198, 218, 240),
        };

        var picture = new PictureBox
        {
            Location = new Point(28, 14),
            Size = new Size(362, 462),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.White,
            Image = LoadToolEditorLanguagePackageImage(),
        };

        var badge = new AboutRoundedPanel
        {
            Location = new Point(50, 492),
            Size = new Size(318, 70),
            BackColor = Color.FromArgb(232, 241, 252),
            BorderColor = Color.Transparent,
            SecondaryBorderColor = Color.Transparent,
            CornerRadius = 9,
        };

        var badgeText = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14, 10, 14, 10),
            Text = "ToolEditor bouwt, controleert en compileert Syscalculator language packages.",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 65, 170),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        badge.Controls.Add(badgeText);

        panel.Controls.Add(picture);
        panel.Controls.Add(badge);
        return panel;
    }

    private static Image LoadToolEditorLanguagePackageImage()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "About", "lngpdk_software_box_390x520.png"),
            Path.Combine(Environment.CurrentDirectory, "src", "Tiedragon.ToolEditor", "Assets", "About", "lngpdk_software_box_390x520.png"),
            Path.Combine(AppContext.BaseDirectory, "Resources", "ToolEditorLanguagePackage.png"),
            Path.Combine(AppContext.BaseDirectory, "ToolEditorLanguagePackage.png"),
            Path.Combine(Environment.CurrentDirectory, "src", "Tiedragon.ToolEditor", "Resources", "ToolEditorLanguagePackage.png"),
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path))
                continue;

            try
            {
                using var stream = File.OpenRead(path);
                return new Bitmap(stream);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
            }
        }

        return CreateLanguagePackageIllustration(new Size(760, 900));
    }

    private static Bitmap CreateLanguagePackageIllustration(Size size)
    {
        var image = new Bitmap(size.Width, size.Height);
        using var graphics = Graphics.FromImage(image);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        graphics.Clear(Color.White);

        using (var background = new LinearGradientBrush(
                   new Rectangle(Point.Empty, size),
                   Color.White,
                   Color.FromArgb(236, 246, 255),
                   LinearGradientMode.Vertical))
        {
            graphics.FillRectangle(background, new Rectangle(Point.Empty, size));
        }

        DrawPackageBox(graphics, size);
        DrawPackageTitle(graphics);
        DrawPackageGlobe(graphics, new PointF(410, 480), 118);
        DrawSpeechBubble(graphics, new Rectangle(185, 340, 130, 62), "Hello", Color.FromArgb(35, 129, 220), true);
        DrawSpeechBubble(graphics, new Rectangle(560, 355, 130, 62), "Hola", Color.FromArgb(104, 178, 42), false);
        DrawSpeechBubble(graphics, new Rectangle(180, 505, 130, 62), "Hallo", Color.FromArgb(245, 149, 25), true);
        DrawSpeechBubble(graphics, new Rectangle(548, 540, 126, 62), "你好", Color.FromArgb(124, 65, 196), false);
        DrawTranslationBadge(graphics);
        DrawFlagRow(graphics);
        return image;
    }

    private static void DrawPackageBox(Graphics graphics, Size size)
    {
        var front = new Rectangle(112, 34, 610, 780);
        var side = new Point[]
        {
            new(36, 80),
            new(112, 34),
            new(112, 814),
            new(36, 768),
        };

        using var shadow = new SolidBrush(Color.FromArgb(30, 0, 40, 90));
        graphics.FillEllipse(shadow, 92, size.Height - 76, 620, 44);

        using var sideBrush = new LinearGradientBrush(new Rectangle(36, 34, 90, 780), Color.FromArgb(4, 45, 126), Color.FromArgb(0, 92, 191), LinearGradientMode.Vertical);
        graphics.FillPolygon(sideBrush, side);
        using var sidePen = new Pen(Color.FromArgb(6, 54, 136), 2);
        graphics.DrawPolygon(sidePen, side);

        using var frontBrush = new LinearGradientBrush(front, Color.White, Color.FromArgb(244, 249, 255), LinearGradientMode.Vertical);
        using var frontPath = CreateRoundedRectangle(front, 10);
        graphics.FillPath(frontBrush, frontPath);
        using var frontPen = new Pen(Color.FromArgb(170, 190, 214), 2);
        graphics.DrawPath(frontPen, frontPath);

        using var footerBrush = new LinearGradientBrush(new Rectangle(112, 610, 610, 204), Color.FromArgb(0, 82, 184), Color.FromArgb(2, 34, 104), LinearGradientMode.Vertical);
        using var footerPath = new GraphicsPath();
        footerPath.AddBezier(112, 620, 280, 690, 520, 560, 722, 610);
        footerPath.AddLine(722, 610, 722, 804);
        footerPath.AddLine(722, 804, 112, 804);
        footerPath.CloseFigure();
        graphics.FillPath(footerBrush, footerPath);

        using var sideFont = new Font("Segoe UI", 26, FontStyle.Bold);
        using var sideTextBrush = new SolidBrush(Color.FromArgb(220, 240, 255));
        var state = graphics.Save();
        graphics.TranslateTransform(76, 286);
        graphics.RotateTransform(90);
        graphics.DrawString("LANGUAGE", sideFont, sideTextBrush, 0, 0);
        graphics.DrawString("PACKAGE", sideFont, Brushes.White, 0, 42);
        graphics.Restore(state);

        using var gearPen = new Pen(Color.White, 6);
        graphics.DrawEllipse(gearPen, 64, 665, 54, 54);
        graphics.DrawEllipse(gearPen, 80, 681, 22, 22);
    }

    private static void DrawPackageTitle(Graphics graphics)
    {
        using var titleFont = new Font("Segoe UI", 48, FontStyle.Bold);
        using var subtitleFont = new Font("Segoe UI", 20, FontStyle.Regular);
        using var dark = new SolidBrush(Color.FromArgb(7, 48, 132));
        using var light = new SolidBrush(Color.FromArgb(18, 135, 226));
        graphics.DrawString("Language", titleFont, dark, 250, 82);
        graphics.DrawString("Package", titleFont, light, 252, 148);
        graphics.DrawString("for Syscalculator", subtitleFont, new SolidBrush(Color.FromArgb(52, 64, 84)), 258, 234);
        using var linePen = new Pen(Color.FromArgb(91, 168, 229), 2);
        graphics.DrawLine(linePen, 238, 284, 660, 284);
    }

    private static void DrawPackageGlobe(Graphics graphics, PointF center, int radius)
    {
        var bounds = new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        using var brush = new LinearGradientBrush(Rectangle.Round(bounds), Color.FromArgb(69, 171, 241), Color.FromArgb(0, 61, 160), LinearGradientMode.ForwardDiagonal);
        graphics.FillEllipse(brush, bounds);
        using var oceanPen = new Pen(Color.FromArgb(165, 220, 255), 1.5f);
        for (var i = -2; i <= 2; i++)
            graphics.DrawEllipse(oceanPen, center.X - radius + Math.Abs(i) * 18, center.Y - radius, (radius - Math.Abs(i) * 18) * 2, radius * 2);
        for (var i = -2; i <= 2; i++)
            graphics.DrawArc(oceanPen, bounds, 0, 360);

        using var land = new SolidBrush(Color.FromArgb(210, 238, 255));
        graphics.FillClosedCurve(land, [new PointF(345, 410), new PointF(390, 388), new PointF(440, 410), new PointF(420, 448), new PointF(365, 456)]);
        graphics.FillClosedCurve(land, [new PointF(442, 505), new PointF(498, 498), new PointF(514, 548), new PointF(460, 582), new PointF(426, 548)]);
        graphics.FillClosedCurve(land, [new PointF(322, 500), new PointF(360, 486), new PointF(382, 520), new PointF(340, 545)]);
        using var rim = new Pen(Color.FromArgb(5, 65, 164), 3);
        graphics.DrawEllipse(rim, bounds);
    }

    private static void DrawSpeechBubble(Graphics graphics, Rectangle bounds, string text, Color color, bool tailLeft)
    {
        using var path = CreateRoundedRectangle(bounds, 16);
        using var brush = new LinearGradientBrush(bounds, ControlPaint.Light(color), color, LinearGradientMode.Vertical);
        graphics.FillPath(brush, path);
        using var pen = new Pen(ControlPaint.Dark(color), 2);
        graphics.DrawPath(pen, path);

        var tail = tailLeft
            ? new[] { new Point(bounds.Left + 45, bounds.Bottom - 2), new Point(bounds.Left + 78, bounds.Bottom + 36), new Point(bounds.Left + 78, bounds.Bottom - 2) }
            : [new Point(bounds.Right - 45, bounds.Bottom - 2), new Point(bounds.Right - 78, bounds.Bottom + 36), new Point(bounds.Right - 78, bounds.Bottom - 2)];
        graphics.FillPolygon(brush, tail);
        graphics.DrawPolygon(pen, tail);

        using var font = new Font("Segoe UI", 22, FontStyle.Bold);
        TextRenderer.DrawText(graphics, text, font, bounds, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private static void DrawTranslationBadge(Graphics graphics)
    {
        var bounds = new Rectangle(350, 610, 164, 78);
        DrawSpeechBubble(graphics, bounds, "A 文", Color.FromArgb(21, 141, 226), true);
    }

    private static void DrawFlagRow(Graphics graphics)
    {
        DrawFlag(graphics, new Point(235, 724), "NL", Color.FromArgb(214, 36, 42), Color.White, Color.FromArgb(22, 86, 171));
        DrawFlag(graphics, new Point(335, 724), "EN", Color.FromArgb(27, 73, 150), Color.White, Color.FromArgb(196, 34, 55));
        DrawFlag(graphics, new Point(435, 724), "DE", Color.Black, Color.FromArgb(213, 32, 39), Color.FromArgb(255, 206, 0));
        DrawFlag(graphics, new Point(535, 724), "FR", Color.FromArgb(0, 61, 165), Color.White, Color.FromArgb(239, 65, 53));

        using var circlePen = new Pen(Color.White, 3);
        graphics.DrawEllipse(circlePen, 632, 724, 58, 58);
        using var dotBrush = new SolidBrush(Color.White);
        graphics.FillEllipse(dotBrush, 650, 750, 6, 6);
        graphics.FillEllipse(dotBrush, 661, 750, 6, 6);
        graphics.FillEllipse(dotBrush, 672, 750, 6, 6);
        using var font = new Font("Segoe UI", 14, FontStyle.Bold);
        TextRenderer.DrawText(graphics, "MORE", font, new Rectangle(618, 786, 90, 34), Color.White, TextFormatFlags.HorizontalCenter);
    }

    private static void DrawFlag(Graphics graphics, Point origin, string label, Color top, Color middle, Color bottom)
    {
        var circle = new Rectangle(origin.X, origin.Y, 58, 58);
        using var path = new GraphicsPath();
        path.AddEllipse(circle);
        graphics.SetClip(path);
        using (var brush = new SolidBrush(top))
            graphics.FillRectangle(brush, origin.X, origin.Y, 58, 20);
        using (var brush = new SolidBrush(middle))
            graphics.FillRectangle(brush, origin.X, origin.Y + 19, 58, 20);
        using (var brush = new SolidBrush(bottom))
            graphics.FillRectangle(brush, origin.X, origin.Y + 38, 58, 20);
        graphics.ResetClip();
        using var pen = new Pen(Color.White, 3);
        graphics.DrawEllipse(pen, circle);
        using var font = new Font("Segoe UI", 14, FontStyle.Bold);
        TextRenderer.DrawText(graphics, label, font, new Rectangle(origin.X - 12, origin.Y + 62, 82, 34), Color.White, TextFormatFlags.HorizontalCenter);
    }

    private Control BuildInfoPanel()
    {
        var panel = new AboutRoundedTableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            Padding = new Padding(2),
            BackColor = Color.White,
            CornerRadius = 10,
            BorderColor = Color.FromArgb(103, 158, 216),
            SecondaryBorderColor = Color.FromArgb(198, 218, 240),
        };

        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));

        panel.Controls.Add(CreateVersionSection(), 0, 0);
        panel.Controls.Add(CreateTextSection("©", "Copyright", "1995-2026 Edward Tie / Tiedragon\r\nAll rights reserved."), 0, 1);
        panel.Controls.Add(CreateTextSection(
            "▤",
            "Description",
            "Syscalculator gebruikt LngPdk: een EPUB-achtig containerformaat\r\n" +
            "voor softwaretaalpakketten met UI-teksten, HTML-help,\r\n" +
            "manuals, media en checksums."), 0, 2);
        panel.Controls.Add(CreateContactSection(), 0, 3);
        panel.Controls.Add(CreateLanguageSection(), 0, 4);
        return panel;
    }

    private static Control CreateVersionSection()
    {
        var grid = CreateSection("ⓘ", "Version information");
        AddPair(grid, "Product name:", "Syscalculator 2.0");
        AddPair(grid, "Tool:", "Tiedragon ToolEditor");
        AddPair(grid, "Tool version:", GetToolVersion());
        AddPair(grid, "Runtime:", ".NET " + Environment.Version);
        return grid;
    }

    private static Control CreateTextSection(string iconText, string title, string text)
    {
        var grid = CreateSection(iconText, title);
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var label = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Text = text,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(39, 51, 69),
            Padding = new Padding(12, 4, 12, 6),
        };
        grid.SetColumnSpan(label, 2);
        grid.Controls.Add(label, 1, row);
        return grid;
    }

    private static Control CreateContactSection()
    {
        var grid = CreateSection("🌐", "Website");
        AddLink(grid, Website, Website);
        AddIconRow(grid, "✉", "E-mail");
        AddLink(grid, Email, "mailto:" + Email);
        return grid;
    }

    private Control CreateLanguageSection()
    {
        var grid = CreateSection("文", "Language");

        _languageCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 9),
        };

        foreach (var language in FindLanguages())
            _languageCombo.Items.Add(language);

        _languageCombo.SelectedIndexChanged += (_, _) =>
        {
            if (_languageCombo.SelectedItem is not LanguageOption language)
                return;

            _selectedLanguageFile = language.FileName;
            _selectedLanguage = LoadLanguageFile(language.FileName);
        };

        for (var i = 0; i < _languageCombo.Items.Count; i++)
        {
            if (_languageCombo.Items[i] is LanguageOption language &&
                language.FileName.Equals(_selectedLanguageFile, StringComparison.OrdinalIgnoreCase))
            {
                _languageCombo.SelectedIndex = i;
                break;
            }
        }

        if (_languageCombo.SelectedIndex < 0 && _languageCombo.Items.Count > 0)
            _languageCombo.SelectedIndex = 0;

        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        grid.SetColumnSpan(_languageCombo, 2);
        grid.Controls.Add(_languageCombo, 1, row);
        return grid;
    }

    private static TableLayoutPanel CreateSection(string iconText, string title)
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 1,
            ColumnCount = 3,
            Padding = new Padding(10, 8, 12, 8),
            BackColor = Color.White,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        var icon = new Label
        {
            Dock = DockStyle.Fill,
            Text = iconText,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Symbol", 15),
            ForeColor = Color.FromArgb(0, 82, 202),
        };

        var header = new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 65, 170),
        };
        grid.Controls.Add(icon, 0, 0);
        grid.SetColumnSpan(header, 2);
        grid.Controls.Add(header, 1, 0);
        return grid;
    }

    private static void AddPair(TableLayoutPanel grid, string key, string value)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        grid.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = key,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(39, 51, 69),
        }, 1, row);
        grid.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = value,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(39, 51, 69),
        }, 2, row);
    }

    private static void AddIconRow(TableLayoutPanel grid, string iconText, string text)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        grid.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = iconText,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Symbol", 13),
            ForeColor = Color.FromArgb(0, 82, 202),
        }, 0, row);
        grid.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = text,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 65, 170),
        }, 1, row);
    }

    private static void AddLink(TableLayoutPanel grid, string text, string link)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        var label = new LinkLabel
        {
            Dock = DockStyle.Fill,
            Text = text,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            LinkColor = Color.FromArgb(0, 80, 180),
            ActiveLinkColor = Color.FromArgb(0, 50, 130),
        };
        label.LinkClicked += (_, _) => OpenLink(link);
        grid.SetColumnSpan(label, 2);
        grid.Controls.Add(label, 1, row);
    }

    private Control BuildBottomBar()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(238, 242, 248),
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(14, 12, 14, 10),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var privacy = new Button
        {
            Text = "Privacy...",
            Size = new Size(112, 32),
            Margin = new Padding(0, 0, 8, 0),
        };
        privacy.Click += (_, _) => ShowLegalDocument("Privacy Statement", "legal/privacy-statement.html", "legal-privacy");

        var license = new Button
        {
            Text = "License...",
            Size = new Size(112, 32),
            Margin = new Padding(0, 0, 8, 0),
        };
        license.Click += (_, _) => ShowLegalDocument("License Agreement", "legal/license-agreement.html", "legal-license");

        var ok = new Button
        {
            Text = ResolveLanguageText("tool_editor.button.ok") ?? "OK",
            DialogResult = DialogResult.OK,
            Size = new Size(96, 32),
            Margin = Padding.Empty,
        };
        AcceptButton = ok;

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Anchor = AnchorStyles.Right,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        buttons.Controls.Add(privacy);
        buttons.Controls.Add(license);
        buttons.Controls.Add(ok);
        panel.Controls.Add(buttons, 1, 0);
        return panel;
    }

    private void ShowLegalDocument(string title, string fileName, string pageId)
    {
        var body = HelpApi.Content(
            HelpApi.LanguageCodeFromFileName(_selectedLanguageFile),
            ResolveLanguageText,
            pageId,
            fileName);
        var html = HelpHtml.WrapTopicPage(title, body, HelpApi.MainHelpCss());

        HelpApi.ShowDialog(this, new HelpDialogOptions(
            title,
            [new NodHelpPage(pageId, title, html)],
            pageId,
            new HelpNavigationLabels("Home", "Previous", "Next"),
            OkOnly: true,
            ShowTopics: false));
    }

    private string? ResolveLanguageText(string key)
    {
        if (_selectedLanguage.TryGetValue(key, out var value))
            return value;

        return _englishLanguage.TryGetValue(key, out var englishValue)
            ? englishValue
            : null;
    }

    private static string GetToolVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null ? "0.1.0" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static void OpenLink(string link)
    {
        try
        {
            Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            MessageBox.Show(link, "Link openen", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private static IReadOnlyList<LanguageOption> FindLanguages()
    {
        var languageDirectory = Path.Combine(AppContext.BaseDirectory, "Resources", "Languages");
        if (!Directory.Exists(languageDirectory))
            return [new LanguageOption("Nederlands", DefaultLanguageFile)];

        var languages = Directory
            .EnumerateFiles(languageDirectory, "*.lng")
            .Select(path => new LanguageOption(GetLanguageDisplayName(path), Path.GetFileName(path)))
            .OrderBy(language => language.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return languages.Count == 0
            ? [new LanguageOption("Nederlands", DefaultLanguageFile)]
            : languages;
    }

    private static string GetLanguageDisplayName(string path)
    {
        var entries = LoadLanguageFile(Path.GetFileName(path));
        if (entries.TryGetValue("language.name", out var languageName) &&
            !string.IsNullOrWhiteSpace(languageName))
            return languageName;

        return Path.GetFileNameWithoutExtension(path);
    }

    private static Dictionary<string, string> LoadLanguageFile(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "Languages", Path.GetFileName(fileName));
        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path))
            return entries;

        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed.StartsWith(';'))
                continue;

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            entries[key] = value;
        }

        return entries;
    }

    private sealed record LanguageOption(string DisplayName, string FileName)
    {
        public override string ToString() => DisplayName;
    }

    private sealed class AboutRoundedPanel : Panel
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get; set; } = 10;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor { get; set; } = Color.FromArgb(103, 158, 216);

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SecondaryBorderColor { get; set; } = Color.FromArgb(198, 218, 240);

        public AboutRoundedPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRoundedRegion(this, CornerRadius);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            PaintRoundedBorder(e.Graphics, ClientRectangle, CornerRadius, BorderColor, SecondaryBorderColor);
        }
    }

    private sealed class AboutRoundedTableLayoutPanel : TableLayoutPanel
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get; set; } = 10;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor { get; set; } = Color.FromArgb(103, 158, 216);

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SecondaryBorderColor { get; set; } = Color.FromArgb(198, 218, 240);

        public AboutRoundedTableLayoutPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRoundedRegion(this, CornerRadius);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            PaintRoundedBorder(e.Graphics, ClientRectangle, CornerRadius, BorderColor, SecondaryBorderColor);
        }
    }

    private static void UpdateRoundedRegion(Control control, int radius)
    {
        if (control.Width <= 0 || control.Height <= 0)
            return;

        control.Region?.Dispose();
        using var path = CreateRoundedRectangle(new Rectangle(0, 0, control.Width, control.Height), radius);
        control.Region = new Region(path);
    }

    private static void PaintRoundedBorder(Graphics graphics, Rectangle bounds, int radius, Color borderColor, Color secondaryBorderColor)
    {
        if (bounds.Width <= 1 || bounds.Height <= 1 || borderColor.A == 0)
            return;

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var borderBounds = Rectangle.Inflate(bounds, -1, -1);
        using var path = CreateRoundedRectangle(borderBounds, radius);
        using var pen = new Pen(borderColor);
        graphics.DrawPath(pen, path);

        if (secondaryBorderColor.A == 0)
            return;

        var innerBounds = Rectangle.Inflate(bounds, -2, -2);
        using var innerPath = CreateRoundedRectangle(innerBounds, Math.Max(1, radius - 1));
        using var innerPen = new Pen(secondaryBorderColor);
        graphics.DrawPath(innerPen, innerPath);
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return path;

        radius = Math.Max(1, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2));
        var diameter = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
