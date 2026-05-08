using System.Drawing.Drawing2D;
using System.Globalization;
using Microsoft.Win32;
using Tiedragon.NodSystem.Core;
using System.IO;
using System.Net;

namespace Syscalculator.UI.WinForms;

/// <summary>
/// Main converter window, styled close to old Syscalculator 1.x.
/// </summary>
public sealed class MainForm : Form
{
    // Zoek/commentaar: Type-overzicht: class ModernToolbarRenderer bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class ModernToolbarRenderer : ToolStripProfessionalRenderer
    {
        // Zoek/commentaar: Methode OnRenderToolStripBorder: centrale logica voor deze stap.
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
        }

        // Zoek/commentaar: Methode OnRenderButtonBackground: centrale logica voor deze stap.
        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item is not ToolStripButton button)
            {
                base.OnRenderButtonBackground(e);
                return;
            }

            var bounds = new Rectangle(Point.Empty, button.Size);
            if (button.Pressed)
            {
                using var pressedBrush = new SolidBrush(Color.FromArgb(218, 230, 246));
                e.Graphics.FillRectangle(pressedBrush, bounds);
                return;
            }

            if (button.Selected)
            {
                using var hoverBrush = new SolidBrush(Color.FromArgb(232, 239, 248));
                e.Graphics.FillRectangle(hoverBrush, bounds);
            }
        }
    }

    private readonly NodCatalogService _catalogService;
    private LanguageCatalog _language;
    private readonly List<NodCatalogItem> _catalogItems = new();
    private readonly HashSet<string> _shownIntroConverters = new(StringComparer.OrdinalIgnoreCase);
    private readonly System.Windows.Forms.Timer _liveConvertTimer = new();

    private NotifyIcon _notifyIcon = null!;
    private ContextMenuStrip _trayMenu = null!;
    private bool _minimizeToTray = true;
    private bool _alwaysOnTop;
    private bool _traceControlEnabled;
    private bool _allowRealClose;
    private bool _startWithWindows;
    private bool _showIntroductions;

    private NodCatalogItem? _currentItem;
    private NodDocument? _currentDocument;
    private NodUiMetadata? _currentMeta;
    private CalculationTrace? _lastTrace;

    private ComboBox _converterCombo = null!;
    private TableLayoutPanel _toolbarStrip = null!;
    private Label _toolbarFileLabel = null!;
    private TableLayoutPanel _converterBody = null!;

    private RadioButton _inputRadio = null!;
    private RadioButton _outputRadio = null!;

    private Label _inputLabel = null!;
    private Label _outputLabel = null!;
    private Label _inputPrefixLabel = null!;
    private Label _inputSuffixLabel = null!;
    private Label _outputPrefixLabel = null!;
    private Label _outputSuffixLabel = null!;

    private TextBox _inputTextBox = null!;
    private TextBox _outputTextBox = null!;

    private CheckBox _digitGroupCheckBox = null!;
    private CheckBox _decimalsCheckBox = null!;
    private NumericUpDown _decimalsUpDown = null!;
    private CheckBox _liveConvertCheckBox = null!;
    private bool _liveConvertEnabled;
    private bool _reverseDirection;

    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _statusLabel = null!;

    private TextBox? _calculatorTargetTextBox;
    private Button? _calculatorToolbarButton;
    private ToolStripMenuItem? _calculatorMenuItem;
    private ToolStripMenuItem? _reverseDirectionMenuItem;
    private ToolStripMenuItem? _showIntroductionsMenuItem;
    private ToolStripMenuItem? _startWithWindowsMenuItem;
    private bool _updatingText;
    private int _defaultDecimals;

private readonly string? _startupNodPath;
private readonly bool _startInTray;

// Zoek/commentaar: Constructor: maakt en initialiseert MainForm.
public MainForm(string? startupNodPath = null, bool startInTray = false)
{
    _startupNodPath = startupNodPath;
    _startInTray = startInTray;

    Text = AppVersionInfo.DisplayVersion;
    Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Resources", "Syscalculator.ico"));
        Width = 450;
        Height = 280;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimumSize = new Size(Width, Height);
        MaximumSize = new Size(Width, Height);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(236, 236, 236);

        _catalogService = new NodCatalogService(AppContext.BaseDirectory);
        _language = LanguageCatalog.LoadConfigured(AppContext.BaseDirectory);
        _alwaysOnTop = LoadAlwaysOnTopSetting();
        _minimizeToTray = LoadBooleanSetting("minimizeToTray", defaultValue: true);
        _reverseDirection = LoadBooleanSetting("reverseDirection", defaultValue: false);
        _showIntroductions = LoadBooleanSetting("showIntroductions", defaultValue: true);
        _startWithWindows = LoadStartWithWindowsSetting();
        TopMost = _alwaysOnTop;

        _liveConvertTimer.Interval = 350;
        _liveConvertTimer.Tick += (_, _) =>
        {
            _liveConvertTimer.Stop();
            if (_liveConvertEnabled)
                ConvertFromActiveSide();
        };

        BuildMenu();
        BuildLayout();
        BuildTray();
        LoadCatalog();
        LoadStartupNodIfNeeded();

        if (_startInTray)
            BeginInvoke(() => HideToTray(showBalloon: false));
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildMenu.
    private void BuildMenu()
    {
        var menu = new MenuStrip
        {
            Dock = DockStyle.Top,
            BackColor = Color.FromArgb(242, 242, 242)
        };

        var file = new ToolStripMenuItem(T("menu.file", "File"));
        file.DropDownItems.Add(T("menu.file.open_nod", "Open NOD..."), null, OpenNod_Click);
        file.DropDownItems.Add(T("menu.file.reload", "Reload converter list"), null, (_, _) => LoadCatalog());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(T("menu.file.exit", "Exit"), null, (_, _) =>
        {
            _allowRealClose = true;
            Close();
        });

        var edit = new ToolStripMenuItem(T("menu.edit", "Edit"));
        edit.DropDownItems.Add(T("menu.edit.copy_input", "Copy input"), null, (_, _) => Clipboard.SetText(_inputTextBox.Text));
        edit.DropDownItems.Add(T("menu.edit.copy_output", "Copy output"), null, (_, _) => Clipboard.SetText(_outputTextBox.Text));
        edit.DropDownItems.Add(T("menu.edit.paste_active", "Paste to active field"), null, (_, _) =>
        {
            var target = _calculatorTargetTextBox ?? _inputTextBox;
            if (Clipboard.ContainsText())
                target.Text = Clipboard.GetText();
        });
        edit.DropDownItems.Add(new ToolStripSeparator());
        edit.DropDownItems.Add(T("menu.edit.clear", "Clear fields"), null, (_, _) => ClearFields());

        var config = new ToolStripMenuItem(T("menu.config", "Config"));
        config.DropDownItems.Add(T("option.digit_group", "Digit group"), null, (_, _) => _digitGroupCheckBox.Checked = !_digitGroupCheckBox.Checked);
        config.DropDownItems.Add(T("option.decimals", "Decimals"), null, (_, _) => _decimalsCheckBox.Checked = !_decimalsCheckBox.Checked);
        _reverseDirectionMenuItem = new ToolStripMenuItem(T("menu.config.reverse_direction", "Reverse direction"))
        {
            CheckOnClick = true,
            Checked = _reverseDirection
        };
        _reverseDirectionMenuItem.CheckedChanged += (_, _) =>
        {
            _reverseDirection = _reverseDirectionMenuItem.Checked;
            SwapInputOutputValues();
            ApplyConverterMetadata();
            SaveSettings();
            SetStatus(_reverseDirection
                ? T("status.direction_reversed", "Direction reversed.")
                : T("status.direction_normal", "Direction restored."));
        };
        config.DropDownItems.Add(_reverseDirectionMenuItem);
        _showIntroductionsMenuItem = new ToolStripMenuItem(T("menu.config.show_introduction", "Show introductions"))
        {
            CheckOnClick = true,
            Checked = _showIntroductions
        };
        _showIntroductionsMenuItem.CheckedChanged += (_, _) =>
        {
            _showIntroductions = _showIntroductionsMenuItem.Checked;
            SaveSettings();
            SetStatus(_showIntroductions
                ? T("status.show_introduction_on", "Introductions will be shown.")
                : T("status.show_introduction_off", "Introductions will be hidden."));
        };
        config.DropDownItems.Add(_showIntroductionsMenuItem);
        var liveConvertItem = new ToolStripMenuItem(T("menu.config.live_convert", "Live convert"))
        {
             CheckOnClick = true,
                Checked = _liveConvertEnabled
        };

        liveConvertItem.CheckedChanged += (_, _) =>
        {   
             _liveConvertEnabled = liveConvertItem.Checked;

             if (_liveConvertCheckBox is not null)
                _liveConvertCheckBox.Checked = liveConvertItem.Checked;
        };

        config.DropDownItems.Add(liveConvertItem);
        var traceControlItem = new ToolStripMenuItem(T("menu.config.trace_control", "Trace control")) { CheckOnClick = true };
        traceControlItem.CheckedChanged += (_, _) =>
        {
            _traceControlEnabled = traceControlItem.Checked;
            if (!_traceControlEnabled)
                _lastTrace = null;
        };
        config.DropDownItems.Add(traceControlItem);
        config.DropDownItems.Add(T("menu.config.language", "Language..."), null, Language_Click);
        var alwaysOnTopItem = new ToolStripMenuItem(T("menu.config.always_on_top", "Always on top"))
        {
            CheckOnClick = true,
            Checked = _alwaysOnTop
        };
        alwaysOnTopItem.CheckedChanged += (_, _) =>
        {
            _alwaysOnTop = alwaysOnTopItem.Checked;
            TopMost = _alwaysOnTop;
            SaveAlwaysOnTopSetting();
        };
        config.DropDownItems.Add(alwaysOnTopItem);
        var minimizeToTrayItem = new ToolStripMenuItem(T("menu.config.minimize_to_tray", "Minimize to tray"))
        {
            CheckOnClick = true,
            Checked = _minimizeToTray
        };
        minimizeToTrayItem.CheckedChanged += (_, _) =>
        {
            _minimizeToTray = minimizeToTrayItem.Checked;
            SaveSettings();
        };
        config.DropDownItems.Add(minimizeToTrayItem);
        _startWithWindowsMenuItem = new ToolStripMenuItem(T("menu.config.start_with_windows", "Start with Windows"))
        {
            CheckOnClick = true,
            Checked = _startWithWindows
        };
        _startWithWindowsMenuItem.CheckedChanged += (_, _) =>
        {
            _startWithWindows = _startWithWindowsMenuItem.Checked;
            SaveStartWithWindowsSetting();
            SetStatus(_startWithWindows
                ? T("status.start_with_windows_on", "Syscalculator will start with Windows.")
                : T("status.start_with_windows_off", "Syscalculator will not start with Windows."));
        };
        config.DropDownItems.Add(_startWithWindowsMenuItem);
        config.DropDownItems.Add(new ToolStripSeparator());
        config.DropDownItems.Add(T("menu.config.catalog_manager", "Catalog manager"), null, CatalogManager_Click);

        var tools = new ToolStripMenuItem(T("menu.tools", "Tools"));
        tools.DropDownItems.Add(T("menu.tools.wizard", "WizardExpress"), null, WizardExpress_Click);
        _calculatorMenuItem = new ToolStripMenuItem(T("menu.tools.calculator", "Calculator"));
        _calculatorMenuItem.Click += Calculator_Click;
        tools.DropDownItems.Add(_calculatorMenuItem);
        tools.DropDownItems.Add(T("menu.tools.nod_editor", "NOD Editor"), null, NodEditor_Click);
        tools.DropDownItems.Add(T("menu.tools.trace", "Trace"), null, TraceViewer_Click);
        tools.DropDownItems.Add(T("menu.tools.show_intro", "Show introduction again"), null, (_, _) => ShowIntroAgain());

        var about = new ToolStripMenuItem(T("menu.about", "About"));
        about.DropDownItems.Add(T("menu.about.help", "Help for users"), null, UserHelp_Click);
        about.DropDownItems.Add(new ToolStripSeparator());
        about.DropDownItems.Add(T("menu.about.syscalculator", "About Syscalculator"), null, About_Click);

        menu.Items.Add(file);
        menu.Items.Add(edit);
        menu.Items.Add(config);
        menu.Items.Add(tools);
        menu.Items.Add(about);

        MainMenuStrip = menu;
        Controls.Add(menu);
        menu.BringToFront();
    }

    // Zoek/commentaar: Methode ChangeLanguage: centrale logica voor deze stap.
    private void ChangeLanguage(string fileName)
    {
        if (_language.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            return;

        LanguageCatalog.SaveConfigured(AppContext.BaseDirectory, fileName);
        _language = LanguageCatalog.Load(AppContext.BaseDirectory, fileName);

        ApplyLanguageImmediately();
    }

    // Zoek/commentaar: Methode Language_Click: centrale logica voor deze stap.
    private void Language_Click(object? sender, EventArgs e)
    {
        using var dialog = new LanguageSelectionForm(
            LanguageCatalog.ListAvailable(AppContext.BaseDirectory),
            _language.FileName,
            _language);

        if (ShowOwnedDialog(dialog) == DialogResult.OK && dialog.SelectedLanguageFile is not null)
            ChangeLanguage(dialog.SelectedLanguageFile);
    }

    // Zoek/commentaar: Toont een venster, melding of detailweergave voor ShowOwnedDialog.
    private DialogResult ShowOwnedDialog(Form form)
    {
        form.TopMost = TopMost;
        return form.ShowDialog(this);
    }

    private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "settings.cfg");
    private const string StartupRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupRunValueName = "Syscalculator2";

    // Zoek/commentaar: Laadt gegevens of instellingen voor LoadAlwaysOnTopSetting.
    private static bool LoadAlwaysOnTopSetting()
    {
        return LoadBooleanSetting("alwaysOnTop", defaultValue: false);
    }

    // Zoek/commentaar: Laadt gegevens of instellingen voor LoadBooleanSetting.
    private static bool LoadBooleanSetting(string settingName, bool defaultValue)
    {
        if (!File.Exists(SettingsPath))
            return defaultValue;

        foreach (var rawLine in File.ReadAllLines(SettingsPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                continue;

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (!key.Equals(settingName, StringComparison.OrdinalIgnoreCase))
                continue;

            return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("aan", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("on", StringComparison.OrdinalIgnoreCase);
        }

        return defaultValue;
    }

    // Zoek/commentaar: Slaat gegevens of instellingen op voor SaveAlwaysOnTopSetting.
    private void SaveAlwaysOnTopSetting()
    {
        SaveSettings();
    }

    // Zoek/commentaar: Slaat gegevens of instellingen op voor SaveSettings.
    private void SaveSettings()
    {
        File.WriteAllText(
            SettingsPath,
            "# Syscalculator UI settings." + Environment.NewLine +
            "alwaysOnTop=" + (_alwaysOnTop ? "true" : "false") + Environment.NewLine +
            "minimizeToTray=" + (_minimizeToTray ? "true" : "false") + Environment.NewLine +
            "reverseDirection=" + (_reverseDirection ? "true" : "false") + Environment.NewLine +
            "showIntroductions=" + (_showIntroductions ? "true" : "false") + Environment.NewLine);
    }

    // Zoek/commentaar: Laadt de Windows-startup registratie uit HKCU.
    private static bool LoadStartWithWindowsSetting()
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRunKey);
        var value = key?.GetValue(StartupRunValueName) as string;

        return !string.IsNullOrWhiteSpace(value) &&
            value.Contains("Syscalculator", StringComparison.OrdinalIgnoreCase);
    }

    // Zoek/commentaar: Bewaart of verwijdert de Windows-startup registratie in HKCU.
    private void SaveStartWithWindowsSetting()
    {
        using var key = Registry.CurrentUser.CreateSubKey(StartupRunKey);

        if (_startWithWindows)
        {
            var command = "\"" + Application.ExecutablePath + "\" --tray";
            key?.SetValue(StartupRunValueName, command, RegistryValueKind.String);
        }
        else
        {
            key?.DeleteValue(StartupRunValueName, throwOnMissingValue: false);
        }
    }

    private sealed class ToolbarIconButton : Button
    {
        protected override bool ShowFocusCues => false;
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildLayout.
    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding = new Padding(8, 4, 8, 0),
            BackColor = Color.FromArgb(236, 236, 236)
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));  // toolbar model
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 126)); // converter body
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));  // options
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // filler/status space

        var toolFrame = BuildClassicToolbarFrame();
        root.Controls.Add(toolFrame, 0, 0);

        var body = BuildConverterBody();
        root.Controls.Add(body, 0, 1);

        var options = BuildOptionsPanel();
        root.Controls.Add(options, 0, 2);

        _statusStrip = new StatusStrip
        {
            Dock = DockStyle.Bottom,
            SizingGrip = true
        };
        _statusLabel = new ToolStripStatusLabel(T("status.ready", "Ready"));
        _statusStrip.Items.Add(_statusLabel);

        Controls.Add(root);
        Controls.Add(_statusStrip);
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildClassicToolbarFrame.
    private Panel BuildClassicToolbarFrame()
    {
        var frame = new Panel
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(246, 246, 246),
            Padding = new Padding(10, 10, 10, 2)
        };

        var strip = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(246, 246, 246),
            ColumnCount = 7,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        _toolbarStrip = strip;
        var fileLabelText = T("toolbar.file", "File:");
        var fileLabelWidth = Math.Max(34, MeasureTextWidth(fileLabelText, Font) + 8);
        var comboColumnWidth = Math.Max(160, 264 - fileLabelWidth);

        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38));
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38));
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38));
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 18));
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, fileLabelWidth));
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, comboColumnWidth));
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        strip.Controls.Add(CreateOldIconButton("WizardExpress", MainToolbarIcon.Wizard, WizardExpress_Click), 0, 0);
        _calculatorToolbarButton = (Button)CreateOldIconButton("Calculator", MainToolbarIcon.Calculator, Calculator_Click);
        strip.Controls.Add(_calculatorToolbarButton, 1, 0);
        strip.Controls.Add(CreateOldIconButton("Open NOD", MainToolbarIcon.OpenFolder, OpenNod_Click), 2, 0);

        var separator = new Label
        {
            BorderStyle = BorderStyle.Fixed3D,
            Width = 2,
            Height = 28,
            Anchor = AnchorStyles.None
        };
        strip.Controls.Add(separator, 3, 0);

        _toolbarFileLabel = new Label
        {
         Text = fileLabelText,
         AutoSize = false,
         Width = fileLabelWidth,
         Anchor = AnchorStyles.Left | AnchorStyles.Right,
         Margin = new Padding(0),
         TextAlign = ContentAlignment.MiddleLeft
        };

        strip.Controls.Add(_toolbarFileLabel, 4, 0);

        _converterCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = Math.Max(120, comboColumnWidth - 6),
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0)
        };
        _converterCombo.SelectedIndexChanged += ConverterCombo_SelectedIndexChanged;
        strip.Controls.Add(_converterCombo, 5, 0);

        frame.Controls.Add(strip);
        return frame;
    }

    // Zoek/commentaar: Maakt een nieuw object of hulponderdeel voor CreateOldIconButton.
    private Control CreateOldIconButton(string tooltip, MainToolbarIcon icon, EventHandler click)
    {
        var button = new ToolbarIconButton
        {
            Width = 34,
            Height = 32,
            Margin = new Padding(0),
            Anchor = AnchorStyles.Bottom,
            FlatStyle = FlatStyle.Flat,
            Image = CreateMainToolbarImage(icon),
            Text = "",
            BackColor = Color.FromArgb(246, 246, 246),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.BorderColor = Color.FromArgb(246, 246, 246);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 238, 248);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(214, 228, 246);
        button.Click += click;

        return button;
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildConverterBody.
    private TableLayoutPanel BuildConverterBody()
    {
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 5,
            Padding = new Padding(16, 6, 20, 0),
            BackColor = Color.FromArgb(236, 236, 236)
        };

        _converterBody = body;
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24));   // radio
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12));   // prefix
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));   // compact spacer
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));  // textbox
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30));   // suffix

        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        _inputLabel = CreateClassicLabel("Input 1");
        _outputLabel = CreateClassicLabel("Input 2");

        _inputRadio = new RadioButton { Checked = true, Anchor = AnchorStyles.Left };
        _outputRadio = new RadioButton { Anchor = AnchorStyles.Left };

        _inputRadio.CheckedChanged += (_, _) =>
        {
            if (_inputRadio.Checked)
                _calculatorTargetTextBox = _inputTextBox;
        };

        _outputRadio.CheckedChanged += (_, _) =>
        {
            if (_outputRadio.Checked)
                _calculatorTargetTextBox = _outputTextBox;
        };

        _inputPrefixLabel = CreateSymbolLabel();
        _inputSuffixLabel = CreateSymbolLabel();
        _outputPrefixLabel = CreateSymbolLabel();
        _outputSuffixLabel = CreateSymbolLabel();

        _inputTextBox = CreateClassicTextBox();
        _outputTextBox = CreateClassicTextBox();

        _inputTextBox.Enter += (_, _) =>
        {
            _inputRadio.Checked = true;
            _calculatorTargetTextBox = _inputTextBox;
        };
        _outputTextBox.Enter += (_, _) =>
        {
            _outputRadio.Checked = true;
            _calculatorTargetTextBox = _outputTextBox;
        };

        _inputTextBox.KeyDown += InputTextBox_KeyDown;
        _outputTextBox.KeyDown += OutputTextBox_KeyDown;

        _inputTextBox.TextChanged += (_, _) =>
{
    if (!_updatingText)
        _inputTextBox.Tag = null;

    ScheduleLiveConvertFromInput();
};

_outputTextBox.TextChanged += (_, _) =>
{
    if (!_updatingText)
        _outputTextBox.Tag = null;

    ScheduleLiveConvertFromOutput();
};
        body.Controls.Add(_inputLabel, 3, 0);
        body.Controls.Add(_inputRadio, 0, 1);
        body.Controls.Add(_inputPrefixLabel, 1, 1);
        body.Controls.Add(_inputTextBox, 3, 1);
        body.Controls.Add(_inputSuffixLabel, 4, 1);

        body.Controls.Add(_outputLabel, 3, 3);
        body.Controls.Add(_outputRadio, 0, 4);
        body.Controls.Add(_outputPrefixLabel, 1, 4);
        body.Controls.Add(_outputTextBox, 3, 4);
        body.Controls.Add(_outputSuffixLabel, 4, 4);

        return body;
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildOptionsPanel.
    private FlowLayoutPanel BuildOptionsPanel()
    {
        var options = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(8, 0, 0, 0),
            BackColor = Color.FromArgb(236, 236, 236)
        };

        _digitGroupCheckBox = new CheckBox
        {
            Text = T("option.digit_group", "Digit group"),
            AutoSize = true,
            Margin = new Padding(0, 6, 90, 0)
        };
        _digitGroupCheckBox.CheckedChanged += (_, _) => RefreshFormattedValues();

        _decimalsCheckBox = new CheckBox
        {
            Text = T("option.decimals", "Decimals"),
            AutoSize = true,
            Margin = new Padding(0, 6, 8, 0)
        };
        _decimalsCheckBox.CheckedChanged += (_, _) =>
        {
            _decimalsUpDown.Enabled = _decimalsCheckBox.Checked;
            RefreshFormattedValues();
        };

        _decimalsUpDown = new NumericUpDown
        {
            Width = 58,
            Minimum = 0,
            Maximum = 8,
            Enabled = false,
            Margin = new Padding(0, 3, 16, 0)
        };
        _decimalsUpDown.ValueChanged += (_, _) => RefreshFormattedValues();

        _liveConvertCheckBox = new CheckBox
        {
            Text = T("option.live_convert", "Live convert"),
            AutoSize = true,
            Checked = _liveConvertEnabled,
            Visible = false
        };

        options.Controls.Add(_digitGroupCheckBox);
        options.Controls.Add(_decimalsCheckBox);
        options.Controls.Add(_decimalsUpDown);
        options.Controls.Add(_liveConvertCheckBox);

        return options;
    }

    // Zoek/commentaar: Maakt een nieuw object of hulponderdeel voor CreateClassicLabel.
    private static Label CreateClassicLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = Color.Black,
            Font = new Font("Segoe UI", 9, FontStyle.Regular)
        };
    }

    // Zoek/commentaar: Maakt een nieuw object of hulponderdeel voor CreateSymbolLabel.
    private static Label CreateSymbolLabel()
{
    return new Label
    {
        Text = "",
        AutoSize = false,
        Anchor = AnchorStyles.Left,
        TextAlign = ContentAlignment.MiddleLeft,
        Margin = new Padding(0)
    };
}

// Zoek/commentaar: Maakt een nieuw object of hulponderdeel voor CreateClassicTextBox.
private static TextBox CreateClassicTextBox()
{
    return new TextBox
    {
        Width = 220,
        Anchor = AnchorStyles.Left,
        TextAlign = HorizontalAlignment.Left
    };
}
// Zoek/commentaar: Past groottes/kolommen aan voor ResizeConverterTextColumns.
private void ResizeConverterTextColumns()
{
    if (_converterBody is null)
        return;

    const int totalTextAndSuffixWidth = 250;
    const int minimumTextBoxWidth = 125;
    const int maximumTextBoxWidth = 220;
    const int minimumSymbolWidth = 12;
    const int symbolPadding = 14;

    var prefixWidth = Math.Max(
        minimumSymbolWidth,
        Math.Max(MeasureLabelWidth(_inputPrefixLabel), MeasureLabelWidth(_outputPrefixLabel)) + symbolPadding);

    var suffixWidth = Math.Max(
        minimumSymbolWidth,
        Math.Max(MeasureLabelWidth(_inputSuffixLabel), MeasureLabelWidth(_outputSuffixLabel)) + symbolPadding);

    var textBoxWidth = Math.Clamp(
        totalTextAndSuffixWidth - suffixWidth,
        minimumTextBoxWidth,
        maximumTextBoxWidth);

    suffixWidth = Math.Max(suffixWidth, totalTextAndSuffixWidth - textBoxWidth);

    _converterBody.SuspendLayout();

    _converterBody.ColumnStyles[1].Width = prefixWidth;
    _converterBody.ColumnStyles[3].Width = textBoxWidth;
    _converterBody.ColumnStyles[4].Width = suffixWidth;

    _inputPrefixLabel.Size = new Size(prefixWidth, 23);
    _outputPrefixLabel.Size = new Size(prefixWidth, 23);

    _inputSuffixLabel.Size = new Size(suffixWidth, 23);
    _outputSuffixLabel.Size = new Size(suffixWidth, 23);

    _inputTextBox.Width = textBoxWidth;
    _outputTextBox.Width = textBoxWidth;

    _converterBody.ResumeLayout(true);
}

// Zoek/commentaar: Meet tekst of UI-afmetingen voor MeasureLabelWidth.
private static int MeasureLabelWidth(Label label)
{
    return MeasureTextWidth(label.Text, label.Font);
}

// Zoek/commentaar: Meet tekst of UI-afmetingen voor MeasureTextWidth.
private static int MeasureTextWidth(string text, Font font)
{
    if (string.IsNullOrWhiteSpace(text))
        return 0;

    return TextRenderer.MeasureText(text, font).Width;
}

// Zoek/commentaar: Past een regel, instelling of bewerking toe voor ApplyLanguageImmediately.
private void ApplyLanguageImmediately()
{
    RebuildMainMenu();
    UpdateCalculatorAvailability();

    _digitGroupCheckBox.Text = T("option.digit_group", "Digit group");
    _decimalsCheckBox.Text = T("option.decimals", "Decimals");
    _liveConvertCheckBox.Text = T("option.live_convert", "Live convert");

    ResizeToolbarLanguageColumns();
    ResizeConverterTextColumns();

    RebuildTrayMenu();

    SetStatus(T("status.language_changed", "Language changed."));
}

// Zoek/commentaar: Bouwt dit UI-onderdeel opnieuw op voor RebuildMainMenu.
private void RebuildMainMenu()
{
    var oldMenu = MainMenuStrip;

    if (oldMenu is not null)
    {
        Controls.Remove(oldMenu);
        MainMenuStrip = null;
        oldMenu.Dispose();
    }

    BuildMenu();
}

// Zoek/commentaar: Past groottes/kolommen aan voor ResizeToolbarLanguageColumns.
private void ResizeToolbarLanguageColumns()
{
    if (_toolbarStrip is null || _toolbarFileLabel is null || _converterCombo is null)
        return;

    var fileLabelText = T("toolbar.file", "File:");
    var fileLabelWidth = Math.Max(34, MeasureTextWidth(fileLabelText, Font) + 8);
    var comboColumnWidth = Math.Max(160, 264 - fileLabelWidth);

    _toolbarStrip.SuspendLayout();

    _toolbarFileLabel.Text = fileLabelText;
    _toolbarFileLabel.Width = fileLabelWidth;

    _toolbarStrip.ColumnStyles[4].Width = fileLabelWidth;
    _toolbarStrip.ColumnStyles[5].Width = comboColumnWidth;

    _converterCombo.Width = Math.Max(120, comboColumnWidth - 6);

    _toolbarStrip.ResumeLayout(true);
}

// Zoek/commentaar: Bouwt dit UI-onderdeel opnieuw op voor RebuildTrayMenu.
private void RebuildTrayMenu()
{
    if (_notifyIcon is null)
        return;

    _notifyIcon.ContextMenuStrip = null;
    _trayMenu?.Dispose();

    _trayMenu = new ContextMenuStrip();

    _trayMenu.Items.Add(T("tray.show", "Show Syscalculator"), null, (_, _) => RestoreFromTray());
    _trayMenu.Items.Add(T("tray.hide", "Hide to system tray"), null, (_, _) => HideToTray(showBalloon: true));
    _trayMenu.Items.Add(new ToolStripSeparator());
    _trayMenu.Items.Add(T("tray.wizard", "WizardExpress"), null, WizardExpress_Click);
    _trayMenu.Items.Add(T("tray.calculator", "Calculator"), null, Calculator_Click);
    _trayMenu.Items.Add(new ToolStripSeparator());
    _trayMenu.Items.Add(T("tray.exit", "Exit"), null, (_, _) =>
    {
        _allowRealClose = true;
        _notifyIcon.Visible = false;
        Close();
    });

    _notifyIcon.ContextMenuStrip = _trayMenu;
    _notifyIcon.Text = BuildTrayText();
}
    // Zoek/commentaar: Type-overzicht: enum MainToolbarIcon bevat de hoofdlogica/data voor dit onderdeel.
    private enum MainToolbarIcon
    {
        Wizard,
        Calculator,
        OpenFolder
    }

    // Zoek/commentaar: Maakt een nieuw object of hulponderdeel voor CreateMainToolbarImage.
    private static Bitmap CreateMainToolbarImage(MainToolbarIcon icon)
    {
        var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Transparent);

        switch (icon)
        {
            case MainToolbarIcon.Wizard:
                DrawModernConvertIcon(g);
                break;
            case MainToolbarIcon.Calculator:
                DrawModernCalculatorIcon(g);
                break;
            case MainToolbarIcon.OpenFolder:
                DrawModernFolderIcon(g);
                break;
        }

        return bmp;
    }

    // Zoek/commentaar: Tekent een visueel onderdeel voor DrawModernConvertIcon.
    private static void DrawModernConvertIcon(Graphics g)
    {
        using var blue = new SolidBrush(Color.FromArgb(37, 99, 235));
        using var softBlue = new SolidBrush(Color.FromArgb(219, 234, 254));
        using var dark = CreateIconPen(Color.FromArgb(30, 64, 175), 1.8f);
        using var topPill = CreateRoundedRectanglePath(new Rectangle(5, 7, 22, 8), 4);
        using var bottomPill = CreateRoundedRectanglePath(new Rectangle(5, 18, 22, 8), 4);

        g.FillPath(softBlue, topPill);
        g.FillPath(softBlue, bottomPill);
        DrawArrow(g, dark, new Point(9, 11), new Point(23, 11), right: true);
        DrawArrow(g, dark, new Point(23, 22), new Point(9, 22), right: false);
        g.FillEllipse(blue, 7, 9, 4, 4);
        g.FillEllipse(blue, 21, 20, 4, 4);
    }

    // Zoek/commentaar: Tekent een visueel onderdeel voor DrawModernCalculatorIcon.
    private static void DrawModernCalculatorIcon(Graphics g)
    {
        using var body = new SolidBrush(Color.FromArgb(37, 99, 235));
        using var display = new SolidBrush(Color.FromArgb(239, 246, 255));
        using var key = new SolidBrush(Color.White);
        using var box = CreateRoundedRectanglePath(new Rectangle(8, 4, 17, 24), 5);
        using var displayPath = CreateRoundedRectanglePath(new Rectangle(11, 8, 11, 5), 2);

        g.FillPath(body, box);
        g.FillPath(display, displayPath);

        for (var y = 17; y <= 23; y += 5)
        {
            for (var x = 12; x <= 19; x += 5)
                g.FillEllipse(key, x, y, 3, 3);
        }
    }

    // Zoek/commentaar: Tekent een visueel onderdeel voor DrawModernFolderIcon.
    private static void DrawModernFolderIcon(Graphics g)
    {
        using var tab = new SolidBrush(Color.FromArgb(250, 204, 21));
        using var body = new SolidBrush(Color.FromArgb(254, 240, 138));
        using var outline = CreateIconPen(Color.FromArgb(202, 138, 4), 1.4f);
        using var tabPath = CreateRoundedRectanglePath(new Rectangle(5, 9, 11, 6), 2);
        using var bodyPath = CreateRoundedRectanglePath(new Rectangle(5, 12, 22, 13), 3);

        g.FillPath(tab, tabPath);
        g.FillPath(body, bodyPath);
        g.DrawPath(outline, bodyPath);
    }

    // Zoek/commentaar: Tekent een visueel onderdeel voor DrawArrow.
    private static void DrawArrow(Graphics g, Pen pen, Point start, Point end, bool right)
    {
        g.DrawLine(pen, start, end);
        if (right)
        {
            g.DrawLine(pen, end, new Point(end.X - 4, end.Y - 3));
            g.DrawLine(pen, end, new Point(end.X - 4, end.Y + 3));
            return;
        }

        g.DrawLine(pen, end, new Point(end.X + 4, end.Y - 3));
        g.DrawLine(pen, end, new Point(end.X + 4, end.Y + 3));
    }

    // Zoek/commentaar: Maakt een nieuw object of hulponderdeel voor CreateIconPen.
    private static Pen CreateIconPen(Color color, float width)
    {
        return new Pen(color, width)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
    }

    // Zoek/commentaar: Maakt een nieuw object of hulponderdeel voor CreateRoundedRectanglePath.
    private static GraphicsPath CreateRoundedRectanglePath(Rectangle rectangle, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    // Zoek/commentaar: Maakt een nieuw object of hulponderdeel voor CreateFolderPath.
    private static GraphicsPath CreateFolderPath(int x, int y, int width, int height)
    {
        var path = new GraphicsPath();
        path.AddLines(new[]
        {
            new Point(x, y + 2),
            new Point(x + 6, y + 2),
            new Point(x + 8, y),
            new Point(x + width, y),
            new Point(x + width, y + height),
            new Point(x + width - 1, y + height + 1),
            new Point(x + 1, y + height + 1),
            new Point(x, y + height)
        });
        path.CloseFigure();
        return path;
    }

    // Zoek/commentaar: Tekent een visueel onderdeel voor DrawWizardIcon.
    private static void DrawWizardIcon(Graphics g)
    {
        using var brush1 = new SolidBrush(Color.Blue);
        using var brush2 = new SolidBrush(Color.Red);
        using var pen = new Pen(Color.Black, 1f);
        g.DrawString("1\u2194", new Font("Arial", 8, FontStyle.Bold), brush1, 2, 2);
        g.DrawString("2\u2194", new Font("Arial", 8, FontStyle.Bold), brush2, 2, 14);
        g.DrawLine(pen, 20, 6, 27, 25);
    }

    // Zoek/commentaar: Tekent een visueel onderdeel voor DrawCalculatorIcon.
    private static void DrawCalculatorIcon(Graphics g)
    {
        using var body = new SolidBrush(Color.FromArgb(0, 65, 170));
        using var key = new SolidBrush(Color.White);
        g.FillRectangle(body, 5, 2, 21, 27);
        for (var y = 7; y <= 23; y += 5)
        {
            for (var x = 9; x <= 21; x += 5)
                g.FillRectangle(key, x, y, 3, 3);
        }
    }

    // Zoek/commentaar: Tekent een visueel onderdeel voor DrawOpenFolderIcon.
    private static void DrawOpenFolderIcon(Graphics g)
    {
        using var pen = new Pen(Color.Peru, 1f);
        using var folder = new SolidBrush(Color.FromArgb(245, 205, 65));
        using var folderDark = new SolidBrush(Color.Goldenrod);
        g.FillRectangle(folderDark, 5, 8, 10, 5);
        g.DrawRectangle(pen, 5, 8, 10, 5);
        g.FillRectangle(folder, 3, 12, 26, 14);
        g.DrawRectangle(pen, 3, 12, 26, 14);
    }

    // Zoek/commentaar: Laadt gegevens of instellingen voor LoadCatalog.
    private void LoadCatalog()
    {
        _catalogItems.Clear();
        _catalogItems.AddRange(_catalogService.Load());

        _converterCombo.Items.Clear();
        foreach (var item in _catalogItems)
            _converterCombo.Items.Add(item);

        var defaultIndex = _catalogItems.FindIndex(i => i.IsDefault);
        if (_converterCombo.Items.Count > 0)
            _converterCombo.SelectedIndex = defaultIndex >= 0 ? defaultIndex : 0;

        SetStatus(T("status.catalog_loaded", "Converter list loaded."));
    }

    // Zoek/commentaar: Voert een conversie uit voor ConverterCombo_SelectedIndexChanged.
    private void ConverterCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_converterCombo.SelectedItem is not NodCatalogItem item)
            return;

        LoadConverter(item);
    }
// Zoek/commentaar: Laadt gegevens of instellingen voor LoadStartupNodIfNeeded.
private void LoadStartupNodIfNeeded()
{
    if (string.IsNullOrWhiteSpace(_startupNodPath))
        return;

    if (!File.Exists(_startupNodPath))
    {
        MessageBox.Show(
            this,
            string.Format(T("dialog.open_nod.file_not_found", "File not found: {0}"), _startupNodPath),
            T("dialog.open_nod.failed_title", "NOD load failed"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);

        return;
    }

    var item = new NodCatalogItem
    {
        DisplayName = Path.GetFileNameWithoutExtension(_startupNodPath),
        NodPath = _startupNodPath
    };

    LoadConverter(item);
}
    // Zoek/commentaar: Laadt gegevens of instellingen voor LoadConverter.
    private void LoadConverter(NodCatalogItem item)
    {
        try
        {
            var path = _catalogService.ResolveNodPath(item);
            var text = NodTextNormalizer.Normalize(File.ReadAllText(path));

            _currentItem = item;
            _currentDocument = NodParser.Parse(text);
            _currentMeta = NodUiMetadata.Parse(text);
            _lastTrace = null;

            ShowIntroIfNeeded(item, _currentMeta);

            ApplyConverterMetadata();

            ConfigureFormatOptions(_currentMeta);
            ClearFields();

            _inputRadio.Checked = true;
            _calculatorTargetTextBox = _inputTextBox;
            UpdateCalculatorAvailability();

            Text = "Syscalculator 2.0 - " + (string.IsNullOrWhiteSpace(_currentMeta.Urln) ? _currentMeta.Name : _currentMeta.Urln);

            if (_notifyIcon is not null)
                _notifyIcon.Text = BuildTrayText();

            SetStatus(string.Format(T("status.loaded", "Loaded: {0}"), item.DisplayName));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, T("dialog.open_nod.failed_title", "NOD load failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Zoek/commentaar: Methode ConfigureFormatOptions: centrale logica voor deze stap.
    private void ConfigureFormatOptions(NodUiMetadata meta)
    {
        _defaultDecimals = ExtractDecimals(meta.Format);
        _decimalsCheckBox.Checked = _defaultDecimals > 0;
        _decimalsUpDown.Value = _defaultDecimals;
        _decimalsUpDown.Enabled = _decimalsCheckBox.Checked;
        _digitGroupCheckBox.Checked = false;
    }

    // Zoek/commentaar: Past de converterlabels toe en ondersteunt de klassieke richting-omkeren optie.
    private void ApplyConverterMetadata()
    {
        if (_currentMeta is null || _inputLabel is null || _outputLabel is null)
            return;

        if (_reverseDirection)
        {
            _inputLabel.Text = _currentMeta.Input2;
            _outputLabel.Text = _currentMeta.Input1;

            _inputPrefixLabel.Text = _currentMeta.SymbolBeforeOutput;
            _inputSuffixLabel.Text = _currentMeta.SymbolAfterOutput;
            _outputPrefixLabel.Text = _currentMeta.SymbolBeforeInput;
            _outputSuffixLabel.Text = _currentMeta.SymbolAfterInput;
        }
        else
        {
            _inputLabel.Text = _currentMeta.Input1;
            _outputLabel.Text = _currentMeta.Input2;

            _inputPrefixLabel.Text = _currentMeta.SymbolBeforeInput;
            _inputSuffixLabel.Text = _currentMeta.SymbolAfterInput;
            _outputPrefixLabel.Text = _currentMeta.SymbolBeforeOutput;
            _outputSuffixLabel.Text = _currentMeta.SymbolAfterOutput;
        }

        ResizeConverterTextColumns();
    }

    // Zoek/commentaar: Laat de bestaande invoerwaarden meedraaien met de klassieke richting-omkeren optie.
    private void SwapInputOutputValues()
    {
        if (_inputTextBox is null || _outputTextBox is null)
            return;

        _updatingText = true;

        (_inputTextBox.Text, _outputTextBox.Text) = (_outputTextBox.Text, _inputTextBox.Text);
        (_inputTextBox.Tag, _outputTextBox.Tag) = (_outputTextBox.Tag, _inputTextBox.Tag);

        _updatingText = false;
    }

    // Zoek/commentaar: Methode ExtractDecimals: centrale logica voor deze stap.
    private static int ExtractDecimals(string? format)
    {
        if (string.IsNullOrWhiteSpace(format) || !format.Contains('.'))
            return 0;

        var index = format.IndexOf('.') + 1;
        var count = 0;

        while (index < format.Length)
        {
            var ch = format[index];
            if (ch == '0' || ch == '#')
            {
                count++;
                index++;
                continue;
            }

            break;
        }

        return count;
    }

    // Zoek/commentaar: Plant een vertraagde actie voor ScheduleLiveConvertFromInput.
    private void ScheduleLiveConvertFromInput()
    {
        if (_updatingText || !_liveConvertEnabled || !_inputRadio.Checked)
            return;

        
        _liveConvertTimer.Stop();
        _liveConvertTimer.Start();
    }

    // Zoek/commentaar: Plant een vertraagde actie voor ScheduleLiveConvertFromOutput.
    private void ScheduleLiveConvertFromOutput()
    {
        if (_updatingText || !_liveConvertEnabled || !_outputRadio.Checked)
            return;

        
        _liveConvertTimer.Stop();
        _liveConvertTimer.Start();
    }

    // Zoek/commentaar: Voert een conversie uit voor ConvertFromActiveSide.
    private void ConvertFromActiveSide()
    {
        if (_inputRadio.Checked && !string.IsNullOrWhiteSpace(_inputTextBox.Text))
            ConvertFromInputField();
        else if (_outputRadio.Checked && !string.IsNullOrWhiteSpace(_outputTextBox.Text))
            ConvertFromOutputField();
    }

    // Zoek/commentaar: Ververst de getoonde data of UI voor RefreshFormattedValues.
    private void RefreshFormattedValues()
    {
        if (_updatingText)
            return;

        _updatingText = true;

        if (_inputTextBox.Tag is string inputRaw && !string.IsNullOrWhiteSpace(inputRaw))
            _inputTextBox.Text = FormatNumericForDisplay(inputRaw);

        if (_outputTextBox.Tag is string outputRaw && !string.IsNullOrWhiteSpace(outputRaw))
            _outputTextBox.Text = FormatNumericForDisplay(outputRaw);

        _updatingText = false;
    }

    // Zoek/commentaar: Maakt tekst of waarden netjes leesbaar voor FormatNumericForDisplay.
    private string FormatNumericForDisplay(string text)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0)
            return text;

        if (!TryParseNumber(text, out var value))
            return text;

        var decimals = _decimalsCheckBox.Checked ? (int)_decimalsUpDown.Value : 0;
        var format = (_digitGroupCheckBox.Checked ? "N" : "F") + decimals.ToString(CultureInfo.InvariantCulture);
        return value.ToString(format, CultureInfo.CurrentCulture);
    }

    // Zoek/commentaar: Normaliseert invoertekst naar een vaste vorm voor NormalizeForEngine.
    private static string NormalizeForEngine(string text)
    {
        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var culture = CultureInfo.CurrentCulture;
        var group = culture.NumberFormat.NumberGroupSeparator;
        var decimalSep = culture.NumberFormat.NumberDecimalSeparator;

        if (!string.IsNullOrEmpty(group))
            text = text.Replace(group, "");

        text = text.Replace(" ", "");

        if (decimalSep != ".")
            text = text.Replace(decimalSep, ".");

        if (text.Contains(',') && text.Contains('.'))
        {
            var lastComma = text.LastIndexOf(',');
            var lastDot = text.LastIndexOf('.');
            if (lastComma > lastDot)
                text = text.Replace(".", "").Replace(',', '.');
        }
        else
        {
            text = text.Replace(',', '.');
        }

        return text;
    }

    // Zoek/commentaar: Tekstconverters zoals trans/chg mogen spaties en komma's niet verliezen.
    private static string NormalizeForDocument(NodDocument document, string text)
    {
        text = (text ?? "").Trim();
        return IsNumericDocument(document) ? NormalizeForEngine(text) : text;
    }

    // Zoek/commentaar: Herkent wanneer de converter echt rekenkundige invoer verwacht.
    private static bool IsNumericDocument(NodDocument document)
    {
        return document.LegacyMathSteps.Count > 0 ||
            document.MathExpressions20.Count > 0 ||
            document.CalculusSteps.Count > 0 ||
            document.Equation is not null;
    }

    private bool CanUseCalculator()
    {
        return _currentDocument is not null && IsNumericDocument(_currentDocument);
    }

    private void UpdateCalculatorAvailability()
    {
        var enabled = CanUseCalculator();

        if (_calculatorToolbarButton is not null)
        {
            _calculatorToolbarButton.Enabled = enabled;
            _calculatorToolbarButton.Cursor = enabled ? Cursors.Hand : Cursors.Default;
        }

        if (_calculatorMenuItem is not null)
            _calculatorMenuItem.Enabled = enabled;
    }

    // Zoek/commentaar: Haalt een waarde, tekst of instelling op voor GetTextBoxRawValue.
    private static string GetTextBoxRawValue(TextBox textBox)
    {
        if (textBox.Tag is string raw && !string.IsNullOrWhiteSpace(raw))
            return raw;

        return textBox.Text;
    }
  // Zoek/commentaar: Probeert tekst veilig te parsen voor TryParseNumber.
  private static bool TryParseNumber(string text, out decimal value)
{
    text = (text ?? "").Trim();

    if (text.Length == 0)
    {
        value = 0;
        return false;
    }

    // Engine-output gebruikt punt als decimaalteken, bijvoorbeeld 33.80.
    // Dit moet eerst invariant gelezen worden, anders maakt NL-regio er 3380 van.
    if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        return true;

    // Gebruikersinvoer mag wel volgens Windows-regio zijn, bijvoorbeeld 33,80.
    text = NormalizeForEngine(text);
    return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}

    // Zoek/commentaar: Voert een conversie uit voor ConvertForward.
    private void ConvertForward()
    {
        ConvertForward(_inputTextBox, _outputTextBox);
    }

    // Zoek/commentaar: Voert een conversie uit vanuit het bovenste veld.
    private void ConvertFromInputField()
    {
        if (_reverseDirection)
            ConvertReverse(_inputTextBox, _outputTextBox);
        else
            ConvertForward(_inputTextBox, _outputTextBox);
    }

    // Zoek/commentaar: Voert een conversie uit vanuit het onderste veld.
    private void ConvertFromOutputField()
    {
        if (_reverseDirection)
            ConvertForward(_outputTextBox, _inputTextBox);
        else
            ConvertReverse(_outputTextBox, _inputTextBox);
    }

    // Zoek/commentaar: Voert een conversie vooruit uit tussen twee velden.
    private void ConvertForward(TextBox sourceTextBox, TextBox targetTextBox)
    {
        if (_updatingText)
        return;

    if (_currentDocument is null)
        return;

    if (string.IsNullOrWhiteSpace(sourceTextBox.Text))
    {
        _updatingText = true;
        targetTextBox.Clear();
        targetTextBox.Tag = null;
        _updatingText = false;
        SetStatus("");
        return;
    }

        try
        {
            var document = _currentDocument;
            var engineInput = NormalizeForDocument(document, sourceTextBox.Text);
            sourceTextBox.Tag = engineInput;

            string outputText;

            if (_traceControlEnabled && (document.LegacyMathSteps.Count > 0 || document.MathExpressions20.Count > 0 || document.CalculusSteps.Count > 0))
            {
                var traceResult = NodEngine.ConvertForwardWithTrace(document, engineInput);
                outputText = traceResult.Text;
                _lastTrace = traceResult.Trace;
            }
            else
            {
                var result = NodEngine.ConvertForward(document, engineInput);
                outputText = result.Text;
                _lastTrace = null;
            }

            _updatingText = true;
            targetTextBox.Tag = outputText;
            targetTextBox.Text = FormatNumericForDisplay(outputText);
            _updatingText = false;

            SetStatus(T("status.forward", "Forward calculated."));
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    // Zoek/commentaar: Voert een conversie uit voor ConvertReverse.
    private void ConvertReverse()
    {
        ConvertReverse(_outputTextBox, _inputTextBox);
    }

    // Zoek/commentaar: Voert een conversie terug uit tussen twee velden.
    private void ConvertReverse(TextBox sourceTextBox, TextBox targetTextBox)
    {
        if (_updatingText)
        return;

    if (_currentDocument is null)
        return;

    if (string.IsNullOrWhiteSpace(sourceTextBox.Text))
    {
        _updatingText = true;
        targetTextBox.Clear();
        targetTextBox.Tag = null;
        _updatingText = false;
        SetStatus("");
        return;
    }

        try
        {
            var document = _currentDocument;
            var engineInput = NormalizeForDocument(document, sourceTextBox.Text);
            sourceTextBox.Tag = engineInput;

            NodResult result;

            if (_traceControlEnabled && _lastTrace is not null)
                result = NodEngine.ConvertReverseFromTrace(document, _lastTrace, engineInput);
            else
                result = NodEngine.ConvertReverse(document, engineInput);

            _updatingText = true;
            targetTextBox.Tag = result.Text;
            targetTextBox.Text = FormatNumericForDisplay(result.Text);
            _updatingText = false;

            SetStatus(T("status.reverse", "Reverse calculated."));
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    // Zoek/commentaar: Methode InputTextBox_KeyDown: centrale logica voor deze stap.
    private void InputTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            ConvertFromInputField();
        }
    }

    // Zoek/commentaar: Methode OutputTextBox_KeyDown: centrale logica voor deze stap.
    private void OutputTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            ConvertFromOutputField();
        }
    }

    // Zoek/commentaar: Opent een venster, bestand of link voor OpenNod_Click.
    private void OpenNod_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Filter = T("dialog.open_nod.filter", "NOD files (*.nod)|*.nod|All files (*.*)|*.*"),
            Title = T("dialog.open_nod.title", "Open NOD")
        };

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        var item = new NodCatalogItem
        {
            DisplayName = Path.GetFileNameWithoutExtension(dlg.FileName),
            NodPath = dlg.FileName
        };

        LoadConverter(item);
    }

    // Zoek/commentaar: Methode Calculator_Click: centrale logica voor deze stap.
    private void Calculator_Click(object? sender, EventArgs e)
    {
        if (!CanUseCalculator())
        {
            SetStatus(T("status.calculator_disabled", "Calculator is only available for numeric converters."));
            return;
        }

        var target = _calculatorTargetTextBox ?? _inputTextBox;
        if (string.IsNullOrWhiteSpace(target.Text))
        {
            SetCalculatorTargetValue(target, "0", convertAfterSet: false);
            target.SelectAll();
        }

        var initial = target.Text;

        using var form = new CalculatorForm(
            initialValue: initial,
            previewResult: value => SetCalculatorTargetValue(target, value, convertAfterSet: true, focusTarget: false, updateStatus: false));

        form.Text = target == _outputTextBox
            ? T("calculator.title_input2", "Calculator - input 2")
            : T("calculator.title_input1", "Calculator - input 1");

        ShowOwnedDialog(form);
    }

    private void SetCalculatorTargetValue(TextBox target, string value, bool convertAfterSet, bool focusTarget = true, bool updateStatus = true)
    {
        value = string.IsNullOrWhiteSpace(value) ? "0" : value;

        _updatingText = true;
        target.Text = value;
        target.Tag = _currentDocument is null
            ? NormalizeForEngine(value)
            : NormalizeForDocument(_currentDocument, value);
        _updatingText = false;

        if (focusTarget)
        {
            target.Focus();
            target.SelectionStart = target.TextLength;
            target.SelectionLength = 0;
        }

        if (target == _inputTextBox)
        {
            _inputRadio.Checked = true;
            if (updateStatus)
                SetStatus(T("status.calculator_to_input1", "Calculator -> input 1."));
            if (convertAfterSet && _liveConvertEnabled)
                ConvertForward();
        }
        else if (target == _outputTextBox)
        {
            _outputRadio.Checked = true;
            if (updateStatus)
                SetStatus(T("status.calculator_to_input2", "Calculator -> input 2."));
            if (convertAfterSet && _liveConvertEnabled)
                ConvertReverse();
        }
    }

    // Zoek/commentaar: Methode WizardExpress_Click: centrale logica voor deze stap.
    private void WizardExpress_Click(object? sender, EventArgs e)
    {
        if (_currentDocument is null)
            return;

        using var form = new WizardExpressForm(_currentDocument, _currentMeta);
        ShowOwnedDialog(form);
    }

    // Zoek/commentaar: Methode NodEditor_Click: centrale logica voor deze stap.
    private void NodEditor_Click(object? sender, EventArgs e)
    {
        string? path = null;
        if (_currentItem is not null)
            path = _catalogService.ResolveNodPath(_currentItem);

        using var form = new NodEditorForm(path);
        ShowOwnedDialog(form);

        if (_currentItem is not null)
            LoadConverter(_currentItem);
    }

    // Zoek/commentaar: Methode CatalogManager_Click: centrale logica voor deze stap.
    private void CatalogManager_Click(object? sender, EventArgs e)
    {
        using var form = new CatalogManagerForm(_catalogService);
        ShowOwnedDialog(form);
        LoadCatalog();
    }

    // Zoek/commentaar: Methode TraceViewer_Click: centrale logica voor deze stap.
    private void TraceViewer_Click(object? sender, EventArgs e)
    {
        using var form = new TraceViewerForm(_lastTrace);
        ShowOwnedDialog(form);
    }

    // Zoek/commentaar: Methode About_Click: centrale logica voor deze stap.
    private void About_Click(object? sender, EventArgs e)
    {
        using var form = new AboutForm(
            _language,
            LanguageCatalog.ListAvailable(AppContext.BaseDirectory),
            _language.FileName);

        if (ShowOwnedDialog(form) == DialogResult.OK && form.SelectedLanguageFile is not null)
            ChangeLanguage(form.SelectedLanguageFile);
    }

    private void UserHelp_Click(object? sender, EventArgs e)
    {
        HelpApi.ShowDialog(this, new HelpDialogOptions(
            T("help.main.title", "Syscalculator help"),
            BuildMainHelpPages(),
            SelectedPageId: "intro",
            Navigation: GetHelpNavigationLabels()));
    }

    // Zoek/commentaar: Levert vertaalde labels voor de gedeelde helpnavigatie.
    private HelpNavigationLabels GetHelpNavigationLabels()
    {
        return new HelpNavigationLabels(
            T("help.nav.home", "Home"),
            T("help.nav.previous", "Previous"),
            T("help.nav.next", "Next"));
    }

    private IReadOnlyList<NodHelpPage> BuildMainHelpPages()
    {
        var intro = T("help.main.page.intro.body", """
<p><b>Syscalculator</b> is for quick conversions. Choose a converter, type a value, and read the result. For fast clipboard work you can use WizardExpress. For making or changing converters use the NOD Editor.</p>
<p>This help is for daily use. It explains the main screen, WizardExpress, and the applications where clipboard behavior can differ.</p>
<ul>
  <li><a href="nodpage:main">Main window</a></li>
  <li><a href="nodpage:wizard">WizardExpress</a></li>
  <li><a href="nodpage:applications">Applications</a></li>
  <li><a href="nodpage:configuration">Configuration</a></li>
</ul>
""");

        var main = T("help.main.page.main.body", """
<h2>Main window</h2>
<ul>
  <li>Choose a converter from the list at the top.</li>
  <li>Type the source value in the first field.</li>
  <li>Read the converted value in the second field.</li>
  <li>Use <b>Open NOD</b> when you want to load a specific converter file.</li>
  <li>Use <b>Reload converter list</b> when new converter files were added.</li>
  <li>Use <b>Calculator</b> when you want to type directly into another field.</li>
</ul>
""");

        var wizard = T("help.main.page.wizard.body", """
<p>WizardExpress is the quick clipboard mode. It is meant for users who want to copy values, convert them quickly, and paste them back without opening the advanced editor.</p>
<ol>
  <li>Copy values to the clipboard.</li>
  <li>Open WizardExpress.</li>
  <li>Click <b>OK</b>.</li>
  <li>Paste the converted values back with <b>Ctrl+V</b>, or use the paste menu in applications other than Excel.</li>
</ol>
<h2>Screenshot</h2>
{WizardExpressScreenshot}
<p class="shot-caption">Example: values are copied from a spreadsheet, WizardExpress converts them, and the result can be pasted back.</p>
<div class="warning">
  <b>Excel / Microsoft 365:</b> Excel has its own clipboard management. Paste immediately with <b>Ctrl+V</b> after WizardExpress. See <a href="nodpage:applications">Applications</a> for the Excel details.
</div>
""");
        wizard = wizard.Replace(
            "{WizardExpressScreenshot}",
            BuildHelpImageTag(
                "WizardExpressHelp.png",
                T("help.main.page.wizard.screenshot_alt", "Screenshot of WizardExpress in front of Syscalculator and a spreadsheet.")));

        var applications = T("help.main.page.applications.body", """
<p>WizardExpress can be used with several applications, but clipboard behavior differs per program.</p>
<h2>Works well with</h2>
<ul>
  <li>plain text programs</li>
  <li>LibreOffice Calc</li>
</ul>
<p>For spreadsheet work, plain values without unit symbols are usually the safest result.</p>
<h2>Excel / Microsoft 365</h2>
<p>Microsoft Excel can handle clipboard data differently from plain text programs and LibreOffice Calc.</p>
<ul>
  <li>Excel often uses richer Office clipboard formats.</li>
  <li>WizardExpress follows the classic clipboard reset and write-back model.</li>
  <li>Because of that, Excel can behave differently after conversion.</li>
</ul>
<div class="warning">
  <b>Excel clipboard management:</b> after WizardExpress writes the converted result, paste immediately with <b>Ctrl+V</b>. Avoid right-click paste in Excel, because Excel can replace the converted clipboard data with its own clipboard state. If paste still does not work, copy the values from Excel again and run WizardExpress again.
</div>
<p>If paste behavior is difficult in Excel, use one of these routes:</p>
<ul>
  <li>try LibreOffice Calc</li>
  <li>work through files instead of clipboard</li>
  <li>use normal plain value paste</li>
</ul>
<p>Excel for the web / Microsoft 365 online can be even stricter because browser clipboard behavior is added on top.</p>
<h2>Data and reporting</h2>
<p>The advanced WizardExpress window combines data view and debug reporting. The <b>Input</b> and <b>Output</b> tabs show the working data as grids.</p>
<p><b>Reporting</b> means a compact diagnosis of the clipboard conversion: row and column counts, clipboard formats, the used rule, before/after information, and a short preview.</p>
<p>The report deliberately shows only the first rows when there is a lot of data. That keeps copied, e-mailed, and PDF reports readable. It is a report, not a full data export.</p>
""");

        var configuration = T("help.main.page.configuration.body", """
<p>The <b>Config</b> menu contains the daily behavior settings for Syscalculator.</p>
<ul>
  <li><b>Reverse direction</b>: swaps the converter direction, including input and output fields.</li>
  <li><b>Show introductions</b>: shows or hides converter introduction text when a converter has one.</li>
  <li><b>Start with Windows</b>: starts Syscalculator automatically when Windows starts.</li>
  <li><b>Always on top</b>: keeps Syscalculator, WizardExpress, and Calculator above other windows.</li>
  <li><b>Minimize to tray</b>: hides Syscalculator in the system tray instead of leaving it on the taskbar.</li>
  <li><b>Language</b>: changes the interface language.</li>
  <li><b>Digit group</b> and <b>Decimals</b>: control how converted numbers are formatted.</li>
  <li><b>Live convert</b>: converts while you type instead of waiting for a separate action.</li>
  <li><b>Catalog manager</b>: manages the converter catalog.</li>
</ul>
<p>Use <b>Tools &gt; Show introduction again</b> when you want to reread the introduction for the current converter.</p>
""");

        return new[]
        {
            new NodHelpPage("intro", T("help.main.page.intro.title", "Start"), WrapMainHelpPage(T("help.main.page.intro.title", "Start"), intro)),
            new NodHelpPage("main", T("help.main.page.main.title", "Main window"), WrapMainHelpPage(T("help.main.page.main.title", "Main window"), main)),
            new NodHelpPage("wizard", T("help.main.page.wizard.title", "WizardExpress"), WrapMainHelpPage(T("help.main.page.wizard.title", "WizardExpress"), wizard)),
            new NodHelpPage("applications", "  " + T("help.main.page.applications.title", "Applications"), WrapMainHelpPage(T("help.main.page.applications.title", "Applications"), applications)),
            new NodHelpPage("configuration", T("help.main.page.configuration.title", "Configuration"), WrapMainHelpPage(T("help.main.page.configuration.title", "Configuration"), configuration))
        };
    }

    private static string WrapMainHelpPage(string title, string body)
    {
        return $$"""
        <!doctype html>
        <html>
        <head>
        <meta charset="utf-8" />
        <meta http-equiv="X-UA-Compatible" content="IE=edge" />
        <style>
        {{GetMainHelpCss()}}
        </style>
        </head>
        <body><h1>{{WebUtility.HtmlEncode(title)}}</h1>{{body}}</body>
        </html>
        """;
    }

    private static string BuildHelpImageTag(string fileName, string altText)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", fileName);
        if (!File.Exists(path))
            return "";

        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        var mime = extension == "jpg" || extension == "jpeg" ? "image/jpeg" : "image/png";
        var base64 = Convert.ToBase64String(File.ReadAllBytes(path));
        return $"""<div class="screenshot-frame"><img src="data:{mime};base64,{base64}" alt="{WebUtility.HtmlEncode(altText)}" /></div>""";
    }

    private static string GetMainHelpCss()
    {
        return """
        ::-webkit-scrollbar { width:15px; height:15px; }
        ::-webkit-scrollbar-track { background:#f8fafc; }
        ::-webkit-scrollbar-thumb { background:#8b949e; border:3px solid #f8fafc; border-radius:999px; min-height:44px; }
        ::-webkit-scrollbar-thumb:hover { background:#6b7280; }
        ::-webkit-scrollbar-corner { background:#f8fafc; }
        body { font-family: "Segoe UI", Arial, sans-serif; margin: 22px 38px 22px 22px; padding-bottom:18px; color:#1f2937; line-height:1.5; background:#ffffff; }
        h1 { font-size: 26px; margin: 0 0 16px; color:#1d4f91; }
        h2 { font-size: 19px; margin: 22px 0 10px; color:#234f84; }
        p { margin: 0 0 12px; }
        ul, ol { margin: 10px 0 14px 22px; padding: 0; }
        li { margin: 6px 0; }
        a { color:#1d5fa7; text-decoration:none; }
        a:hover { text-decoration:underline; }
        code { background:#f2f5f8; border:1px solid #d9e2ec; padding:1px 5px; border-radius:4px; font-family: Consolas, monospace; }
        .screenshot-frame { margin:12px 0 8px; border:1px solid #d7dee8; border-radius:8px; overflow:hidden; background:#f8fafc; box-shadow:0 8px 22px rgba(15,23,42,.12); }
        .screenshot-frame img { display:block; width:100%; height:auto; }
        .shot-caption { font-size:12px; color:#475569; margin-top:6px; }
        .warning { position:relative; background:#fff8e1; border:1px solid #fbbf24; border-left:6px solid #dc2626; border-radius:8px; padding:12px 12px 12px 62px; margin:12px 0 16px; color:#7f1d1d; box-shadow:0 4px 14px rgba(220,38,38,.08); min-height:42px; scroll-margin-bottom:92px; }
        .warning::before { content:""; position:absolute; left:14px; top:14px; width:36px; height:33px; background:center / contain no-repeat url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 52 48'%3E%3Cpath d='M26 3 50 45H2L26 3Z' fill='%23facc15' stroke='%23111827' stroke-width='3'/%3E%3Cpath d='M26 16v15' stroke='%23111827' stroke-width='4' stroke-linecap='round'/%3E%3Ccircle cx='26' cy='37' r='2.6' fill='%23111827'/%3E%3C/svg%3E"); filter:drop-shadow(0 2px 3px rgba(153,27,27,.24)); }
        .warning b { color:#b91c1c; }
        """;
    }

    // Zoek/commentaar: Toont een venster, melding of detailweergave voor ShowIntroAgain.
    private void ShowIntroAgain()
    {
        if (_currentMeta is null)
            return;

        if (_currentMeta.IntroLines.Count == 0)
        {
            MessageBox.Show(this, T("dialog.no_intro", "This converter has no introduction text."), T("dialog.intro_title", "Introduction"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var title = string.IsNullOrWhiteSpace(_currentMeta.Urln) ? _currentMeta.Name : _currentMeta.Urln;
        using var dialog = new IntroDialogForm(title, _currentMeta.IntroLines);
        ShowOwnedDialog(dialog);
    }

    // Zoek/commentaar: Toont een venster, melding of detailweergave voor ShowIntroIfNeeded.
    private void ShowIntroIfNeeded(NodCatalogItem item, NodUiMetadata meta)
    {
        if (meta.IntroLines.Count == 0)
            return;

        if (!_showIntroductions)
            return;

        var key = item.NodPath;
        if (_shownIntroConverters.Contains(key))
            return;

        _shownIntroConverters.Add(key);

        var title = string.IsNullOrWhiteSpace(meta.Urln) ? meta.Name : meta.Urln;
        using var dialog = new IntroDialogForm(title, meta.IntroLines);
        ShowOwnedDialog(dialog);
    }

    // Zoek/commentaar: Methode ClearFields: centrale logica voor deze stap.
    private void ClearFields()
    {
        _updatingText = true;
        _inputTextBox.Tag = null;
        _outputTextBox.Tag = null;
        _inputTextBox.Text = "";
        _outputTextBox.Text = "";
        _updatingText = false;
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildTray.
    private void BuildTray()
    {
        _trayMenu = new ContextMenuStrip();

        _trayMenu.Items.Add(T("tray.show", "Show Syscalculator"), null, (_, _) => RestoreFromTray());
        _trayMenu.Items.Add(T("tray.hide", "Hide to system tray"), null, (_, _) => HideToTray(showBalloon: true));
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add(T("tray.wizard", "WizardExpress"), null, WizardExpress_Click);
        _trayMenu.Items.Add(T("tray.calculator", "Calculator"), null, Calculator_Click);
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add(T("tray.exit", "Exit"), null, (_, _) =>
        {
            _allowRealClose = true;
            _notifyIcon.Visible = false;
            Close();
        });

        _notifyIcon = new NotifyIcon
        {
            Text = "Syscalculator 2.0",
            Icon = CreateTrayIcon(),
            ContextMenuStrip = _trayMenu,
            Visible = false
        };

        _notifyIcon.DoubleClick += (_, _) => RestoreFromTray();
    }

    // Zoek/commentaar: Maakt een nieuw object of hulponderdeel voor CreateTrayIcon.
    private static Icon CreateTrayIcon()
    {
        var bmp = new Bitmap(32, 32);

        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            DrawCalculatorIcon(g);
        }

        return (Icon)Icon.FromHandle(bmp.GetHicon()).Clone();
    }

    // Zoek/commentaar: Verbergt het venster of onderdeel voor HideToTray.
    private void HideToTray(bool showBalloon)
    {
        if (_notifyIcon is null)
            return;

        Hide();
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        _notifyIcon.Visible = true;
        _notifyIcon.Text = BuildTrayText();

        if (showBalloon)
        {
            _notifyIcon.ShowBalloonTip(
                1000,
                T("dialog.tray_title", "Syscalculator"),
                T("dialog.tray_message", "Syscalculator is running in the system tray."),
                ToolTipIcon.Info);
        }
    }

    // Zoek/commentaar: Herstelt een venster of toestand voor RestoreFromTray.
    private void RestoreFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        if (_notifyIcon is not null)
            _notifyIcon.Visible = false;
        BringToFront();
        Activate();
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildTrayText.
    private string BuildTrayText()
    {
        var name = _currentMeta is null
            ? "Syscalculator 2.0"
            : (string.IsNullOrWhiteSpace(_currentMeta.Urln) ? _currentMeta.Name : _currentMeta.Urln);

        return name.Length > 60 ? name[..60] : name;
    }

    // Zoek/commentaar: Methode OnResize: centrale logica voor deze stap.
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (_minimizeToTray && WindowState == FormWindowState.Minimized)
            HideToTray(showBalloon: true);
    }

    // Zoek/commentaar: Methode OnFormClosing: centrale logica voor deze stap.
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowRealClose && _minimizeToTray && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray(showBalloon: true);
            return;
        }

        if (_notifyIcon is not null)
            _notifyIcon.Visible = false;

        base.OnFormClosing(e);
    }

    // Zoek/commentaar: Methode Dispose: centrale logica voor deze stap.
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon?.Dispose();
            _trayMenu?.Dispose();
        }

        base.Dispose(disposing);
    }

    // Zoek/commentaar: Methode T: centrale logica voor deze stap.
    private string T(string key, string fallback) => _language.Text(key, fallback);

    // Zoek/commentaar: Zet een waarde of status voor SetStatus.
    private void SetStatus(string text) => _statusLabel.Text = text;

    // Zoek/commentaar: Toont een venster, melding of detailweergave voor ShowError.
    private void ShowError(Exception ex)
    {
        SetStatus(string.Format(T("status.error", "Error: {0}"), ex.Message));
        MessageBox.Show(this, ex.Message, T("dialog.error_title", "Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}

