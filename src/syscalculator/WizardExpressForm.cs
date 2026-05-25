using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text.RegularExpressions;
using Tiedragon.ClipboardConvert;
using Tiedragon.NodSystem.Core;
using Tiedragon.ToolEditor;

namespace Syscalculator.UI.WinForms;

/// <summary>
/// Moderne versie van WizardExpress.
///
/// Oude werking:
/// - Clipboard lezen
/// - cellen/regels converteren
/// - resultaat terug naar Clipboard
///
/// Deze betaversie converteert tekstregels en tab-gescheiden cellen.
/// </summary>
public sealed class WizardExpressForm : Form
{
    private static readonly Color DarkWindowBackColor = Color.FromArgb(18, 24, 32);
    private static readonly Color DarkPanelBackColor = Color.FromArgb(31, 41, 55);
    private static readonly Color DarkEditorTextColor = Color.FromArgb(226, 232, 240);
    private static readonly Color DarkBorderColor = Color.FromArgb(55, 65, 81);
    private static readonly Regex NumericFragmentRegex = new(
        @"[-+]?(?:\d+(?:[.,]\d+)?|\d*[.,]\d+)",
        RegexOptions.Compiled);

    private readonly NodDocument _document;
    private readonly NodUiMetadata _meta;
    private readonly LanguageCatalog _language;

    private TextBox _input = null!;
    private TextBox _output = null!;
    private MenuStrip _menuStrip = null!;
    private RadioButton _forward = null!;
    private RadioButton _reverse = null!;
    private Panel _trafficLight = null!;
    private Label _statusLabel = null!;
    private readonly System.Windows.Forms.Timer _stateTimer;
    private ClipboardDebugForm? _debugForm;
    private ToolStripMenuItem _debugMenuItem = null!;
    private ToolStripMenuItem _dataMenuItem = null!;
    private bool _advancedMode;
    private string _lastSeenClipboardText = "";
    private string _lastConvertedClipboardText = "";
    private string _lastConversionBeforeText = "";
    private string _lastConversionAfterText = "";
    private string _lastUsedRuleText = "";
    private string _lastUsedRuleMathMl = "";
    private ClipboardConversionSummary? _lastConversionSummary;
    private bool _lastForwardDirection = true;
    private bool _excelClipboardWarningShown;
    private bool _clipboardListenerRegistered;
    private uint _lastObservedClipboardSequence;
    private WizardStatus _status = WizardStatus.Empty;
    private ToolEditorUiTheme _uiTheme;

    private bool IsDarkTheme => _uiTheme == ToolEditorUiTheme.Dark;
    private Color WindowBackColor => IsDarkTheme ? DarkWindowBackColor : Color.FromArgb(244, 246, 249);
    private Color PanelBackColor => IsDarkTheme ? DarkPanelBackColor : Color.White;
    private Color MenuBackColor => IsDarkTheme ? Color.FromArgb(15, 23, 42) : Color.FromArgb(242, 242, 242);
    private Color TextColor => IsDarkTheme ? DarkEditorTextColor : Color.Black;
    private Color ButtonBackColor => IsDarkTheme ? Color.FromArgb(37, 99, 235) : SystemColors.Control;
    private Color ButtonTextColor => IsDarkTheme ? Color.White : SystemColors.ControlText;

    private enum WizardStatus { Empty, Ready, Done, ClipboardOverwritten, Error }

    private enum AdvancedDialogTab { Input, Output, Details }

    public WizardExpressForm(NodDocument document, NodUiMetadata? meta = null)
    {
        _uiTheme = ToolEditorUiThemeSettings.Load();
        _document = document;
        _meta = meta ?? new NodUiMetadata
        {
            Name = document.Name ?? "NOD",
            Input1 = document.LegacyInput1Label ?? "Input",
            Input2 = document.LegacyInput2Label ?? "Output"
        };
        _language = LanguageCatalog.LoadConfigured(AppContext.BaseDirectory);
        _stateTimer = new System.Windows.Forms.Timer { Interval = 500 };
        _stateTimer.Tick += (_, _) => RefreshWizardState();

        Text = T("wizard.title", "WizardExpress");
        AppWindowIcon.ApplyTo(this);
        Width = 215;
        Height = 285;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = WindowBackColor;

        BuildMenu();
        BuildLayout();
        ApplyWizardTheme();
        ToolEditorUiThemeSettings.ThemeChanged += ToolEditorThemeChanged;
        Shown += (_, _) =>
        {
            LoadInputFromClipboard();
        };
        FormClosed += (_, _) =>
        {
            ToolEditorUiThemeSettings.ThemeChanged -= ToolEditorThemeChanged;
            _stateTimer.Stop();
            _debugForm?.Close();
            _debugForm?.Dispose();
            _debugForm = null;
        };
        _stateTimer.Start();
    }

    private void ToolEditorThemeChanged(ToolEditorUiTheme theme)
    {
        if (IsDisposed || Disposing)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => ToolEditorThemeChanged(theme)));
            return;
        }

        if (_uiTheme == theme)
            return;

        _uiTheme = theme;
        ToolEditorUiThemeSettings.ApplyNativeWindowTheme(this, theme);
        ApplyWizardTheme();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ToolEditorUiThemeSettings.ApplyNativeWindowTheme(this, _uiTheme);
        _clipboardListenerRegistered = ClipboardConvertApi.TryAddClipboardFormatListener(Handle);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (_clipboardListenerRegistered)
        {
            ClipboardConvertApi.TryRemoveClipboardFormatListener(Handle);
            _clipboardListenerRegistered = false;
        }

        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == ClipboardConvertApi.WmClipboardUpdate &&
            IsHandleCreated &&
            !IsDisposed &&
            _input is not null &&
            _output is not null)
        {
            BeginInvoke(new Action(() => RefreshWizardState()));
        }

        base.WndProc(ref m);
    }

    private void BuildLayout()
    {
        _forward = new RadioButton { Text = BuildDirectionCaption(forward: true), Checked = true, AutoSize = true, Font = new Font("Segoe UI", 8f), BackColor = WindowBackColor, ForeColor = TextColor, UseVisualStyleBackColor = false };
        _reverse = new RadioButton { Text = BuildDirectionCaption(forward: false), AutoSize = true, Font = new Font("Segoe UI", 8f), BackColor = WindowBackColor, ForeColor = TextColor, UseVisualStyleBackColor = false };
        _forward.CheckedChanged += (_, _) => RefreshWizardState(forceReady: true);
        _reverse.CheckedChanged += (_, _) => RefreshWizardState(forceReady: true);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8, 32, 8, 8),
            BackColor = WindowBackColor,
            ForeColor = TextColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildActionRow(), 0, 1);
        root.Controls.Add(BuildFooter(), 0, 2);

        _input = new TextBox { Visible = false, Multiline = true };
        _output = new TextBox { Visible = false, Multiline = true, ReadOnly = true };
        Controls.Add(_input);
        Controls.Add(_output);

        Controls.Add(root);
        SetWizardStatus(WizardStatus.Empty);
        UpdateDebugInfo(ClipboardSnapshot.Empty);
    }

    private void BuildMenu()
    {
        _menuStrip = new MenuStrip
        {
            Dock = DockStyle.Top,
            BackColor = MenuBackColor,
            ForeColor = TextColor
        };

        var file = new ToolStripMenuItem(T("wizard.menu.file", "File"));
        file.DropDownItems.Add(T("wizard.menu.file.close", "Close"), null, (_, _) => Close());

        var extra = new ToolStripMenuItem(T("wizard.menu.extra", "Extra"));
        var advancedModeItem = new ToolStripMenuItem(T("wizard.menu.extra.advanced", "Advanced mode"))
        {
            CheckOnClick = true
        };
        advancedModeItem.CheckedChanged += (_, _) => SetAdvancedMode(advancedModeItem.Checked);
        extra.DropDownItems.Add(advancedModeItem);
        _dataMenuItem = new ToolStripMenuItem(T("wizard.menu.extra.data", "Excel-style data"))
        {
            Enabled = false
        };
        _dataMenuItem.Click += (_, _) =>
        {
            if (!_advancedMode)
                return;

            var snapshot = ReadClipboardSnapshot();
            ShowTableDialog();
            UpdateAdvancedData(string.IsNullOrEmpty(_input.Text) ? snapshot.Text : _input.Text, _output.Text);
            UpdateDebugInfo(snapshot);
        };
        extra.DropDownItems.Add(_dataMenuItem);
        _debugMenuItem = new ToolStripMenuItem(T("wizard.menu.extra.debug", "Clipboard debug"))
        {
            Enabled = false,
            Image = ClipboardDoctorIconApi.CreateClipboardDoctorIcon()
        };
        _debugMenuItem.Click += (_, _) =>
        {
            if (!_advancedMode)
                return;
            ShowDebugDialog();
            UpdateAdvancedData(_input.Text, _output.Text);
            UpdateDebugInfo(ReadClipboardSnapshot());
        };
        extra.DropDownItems.Add(_debugMenuItem);

        _menuStrip.Items.Add(file);
        _menuStrip.Items.Add(extra);
        MainMenuStrip = _menuStrip;
        Controls.Add(_menuStrip);
        _menuStrip.BringToFront();
    }

    private void ApplyWizardTheme()
    {
        BackColor = WindowBackColor;
        ApplyThemeToControl(this);
        ApplyThemeToToolStrip(_menuStrip);
        _statusLabel.ForeColor = Color.Black;
        _trafficLight.Invalidate();
    }

    private void ApplyThemeToControl(Control control)
    {
        if (control is MenuStrip)
        {
            control.BackColor = MenuBackColor;
            control.ForeColor = TextColor;
        }
        else if (control is TextBox)
        {
            control.BackColor = IsDarkTheme ? Color.FromArgb(39, 39, 39) : SystemColors.Window;
            control.ForeColor = TextColor;
        }
        else if (control is RadioButton radioButton)
        {
            radioButton.BackColor = WindowBackColor;
            radioButton.ForeColor = TextColor;
            radioButton.UseVisualStyleBackColor = false;
        }
        else if (control is Button button)
        {
            button.BackColor = ButtonBackColor;
            button.ForeColor = ButtonTextColor;
            button.UseVisualStyleBackColor = false;
            if (button.FlatStyle == FlatStyle.Flat)
            {
                button.FlatAppearance.BorderColor = IsDarkTheme ? DarkBorderColor : SystemColors.ControlDark;
                button.FlatAppearance.MouseOverBackColor = IsDarkTheme ? Color.FromArgb(30, 64, 175) : SystemColors.ControlLight;
                button.FlatAppearance.MouseDownBackColor = IsDarkTheme ? Color.FromArgb(30, 58, 138) : SystemColors.ControlDark;
            }
        }
        else if (control is Label)
        {
            control.ForeColor = TextColor;
            if (control.BackColor != Color.Transparent)
                control.BackColor = control.Parent == _trafficLight ? control.BackColor : WindowBackColor;
        }
        else if (control is Panel { BorderStyle: BorderStyle.FixedSingle })
        {
            control.BackColor = PanelBackColor;
            control.ForeColor = TextColor;
        }
        else if (control is TableLayoutPanel or FlowLayoutPanel or Panel)
        {
            if (control.BackColor != Color.Transparent)
                control.BackColor = WindowBackColor;
            control.ForeColor = TextColor;
        }

        foreach (Control child in control.Controls)
            ApplyThemeToControl(child);
    }

    private void ApplyThemeToToolStrip(ToolStrip? toolStrip)
    {
        if (toolStrip is null)
            return;

        toolStrip.BackColor = MenuBackColor;
        toolStrip.ForeColor = TextColor;
        foreach (ToolStripItem item in toolStrip.Items)
            ApplyThemeToToolStripItem(item);
    }

    private void ApplyThemeToToolStripItem(ToolStripItem item)
    {
        item.BackColor = MenuBackColor;
        item.ForeColor = TextColor;

        if (item is ToolStripDropDownItem dropDown)
        {
            dropDown.DropDown.BackColor = MenuBackColor;
            dropDown.DropDown.ForeColor = TextColor;
            foreach (ToolStripItem child in dropDown.DropDownItems)
                ApplyThemeToToolStripItem(child);
        }
    }

    private void SetAdvancedMode(bool enabled)
    {
        _advancedMode = enabled;
        _dataMenuItem.Enabled = _advancedMode;
        _debugMenuItem.Enabled = _advancedMode;
        if (_advancedMode)
        {
            var snapshot = ReadClipboardSnapshot();
            ShowTableDialog();
            UpdateAdvancedData(string.IsNullOrEmpty(_input.Text) ? snapshot.Text : _input.Text, _output.Text);
            UpdateDebugInfo(snapshot);
            return;
        }

        if (_debugForm is not null && !_debugForm.IsDisposed)
            _debugForm.Close();
        _debugForm = null;
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Margin = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var intro = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        intro.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        intro.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

        var title = new Label
        {
            Text = T("wizard.title", "WizardExpress"),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = TextColor,
            BackColor = WindowBackColor,
            Margin = new Padding(0, 0, 0, 2)
        };
        intro.Controls.Add(title, 0, 0);

        header.Controls.Add(intro, 0, 0);

        var directionPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            BackColor = WindowBackColor,
            Margin = new Padding(0, 6, 0, 0)
        };
        directionPanel.Controls.Add(_forward);
        directionPanel.Controls.Add(_reverse);
        intro.Controls.Add(directionPanel, 0, 1);

        return header;
    }

    private Control BuildActionRow()
    {
        var convert = new Button
        {
            Text = T("wizard.convert", "OK"),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(96, 28),
            Padding = new Padding(8, 3, 8, 3),
            Margin = Padding.Empty,
            BackColor = ButtonBackColor,
            ForeColor = ButtonTextColor,
            UseVisualStyleBackColor = false
        };
        convert.Click += (_, _) => ConvertText();

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Anchor = AnchorStyles.None,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = WindowBackColor
        };
        actions.Controls.Add(convert);
        actions.Controls[0].Margin = new Padding(60, 0, 0, 0);
        return actions;
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Margin = Padding.Empty,
            BackColor = WindowBackColor
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var statusCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelBackColor,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(8, 6, 8, 6),
            Margin = new Padding(0, 4, 0, 0)
        };

        var statusTitle = new Label
        {
            Text = T("wizard.status.caption", "Status"),
            AutoSize = true,
            BackColor = PanelBackColor,
            ForeColor = TextColor,
            Location = new Point(6, -1)
        };
        statusCard.Controls.Add(statusTitle);

        _trafficLight = new Panel
        {
            Width = 170,
            Height = 22,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Location = new Point(10, 20),
            BorderStyle = BorderStyle.FixedSingle
        };
        statusCard.Controls.Add(_trafficLight);

        _statusLabel = new Label
        {
            Text = T("wizard.status.empty", "Start"),
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
            ForeColor = Color.Black,
            TextAlign = ContentAlignment.MiddleCenter
        };
        _trafficLight.Controls.Add(_statusLabel);

        footer.Controls.Add(statusCard, 0, 0);

        return footer;
    }

    private void ConvertText()
    {
        var sourceText = ReadClipboardSnapshot().Text;
        if (string.IsNullOrWhiteSpace(sourceText))
            sourceText = _input.Text;

        _input.Text = sourceText;

        if (string.IsNullOrWhiteSpace(sourceText))
        {
            SetWizardStatus(WizardStatus.Empty);
            return;
        }

        var result = ClipboardConvertApi.ConvertDelimitedText(
            sourceText,
            (cell, rowIndex) =>
            {
                var converted = ConvertCell(cell, rowIndex, out var cellHadError);
                return new ClipboardCellConversion(converted, cellHadError);
            });

        var outputText = result.Text;
        _output.Text = outputText;
        SetWizardStatus(result.HadError ? WizardStatus.Error : WizardStatus.Done);
        if (!string.IsNullOrWhiteSpace(outputText))
        {
            CopyOutputToClipboard(outputText);
            BeginInvoke(() => CopyOutputToClipboard(outputText));
        }

        _lastConvertedClipboardText = outputText;
        _lastSeenClipboardText = outputText;
        _excelClipboardWarningShown = false;
        _lastConversionBeforeText = sourceText;
        _lastConversionAfterText = outputText;
        _lastUsedRuleText = BuildUsedRuleText();
        _lastUsedRuleMathMl = BuildUsedRuleMathMl();
        _lastConversionSummary = result.Summary;
        _lastForwardDirection = _forward.Checked;
        UpdateAdvancedData(sourceText, outputText);
        UpdateDebugInfo(ReadClipboardSnapshot());
    }

    private void LoadInputFromClipboard()
    {
        try
        {
            var snapshot = ReadClipboardSnapshot();
            var clipboardText = snapshot.Text;
            if (string.IsNullOrWhiteSpace(clipboardText))
            {
                SetWizardStatus(string.IsNullOrWhiteSpace(_input.Text) ? WizardStatus.Empty : WizardStatus.Ready);
                UpdateAdvancedData(_input.Text, _output.Text);
                UpdateDebugInfo(snapshot);
                return;
            }

            _input.Text = clipboardText;
            _lastObservedClipboardSequence = snapshot.SequenceNumber;
            _lastSeenClipboardText = _input.Text;
            if (!string.Equals(clipboardText, _lastConvertedClipboardText, StringComparison.Ordinal))
            {
                _lastConversionSummary = null;
                _lastConversionBeforeText = "";
                _lastConversionAfterText = "";
                _lastUsedRuleText = "";
                _lastUsedRuleMathMl = "";
            }
            SetWizardStatus(string.IsNullOrWhiteSpace(_input.Text) ? WizardStatus.Empty : WizardStatus.Ready);
            UpdateAdvancedData(_input.Text, _output.Text);
            UpdateDebugInfo(snapshot);
        }
        catch
        {
            SetWizardStatus(WizardStatus.Error);
            UpdateDebugInfo(ClipboardSnapshot.Empty);
        }
    }

    private void CopyOutputToClipboard()
    {
        if (string.IsNullOrWhiteSpace(_output.Text))
            return;

        CopyOutputToClipboard(_output.Text);
    }

    private void CopyOutputToClipboard(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return;

        if (!ClipboardConvertApi.SetText(rawText, Handle))
            SetWizardStatus(WizardStatus.Error);
    }

    private string ConvertCell(string cell, int rowIndex, out bool hadError)
    {
        hadError = false;
        if (string.IsNullOrWhiteSpace(cell))
            return cell;

        var prepared = PrepareCellForFormulaConversion(cell, rowIndex);
        if (!prepared.ShouldConvert)
            return prepared.OutputText;

        try
        {
            var result = _forward.Checked
                ? NodEngine.ConvertForward(_document, prepared.ConversionText)
                : NodEngine.ConvertReverse(_document, prepared.ConversionText);

            return DecorateWizardOutput(result.Text);
        }
        catch
        {
            hadError = true;
            return cell;
        }
    }

    private string BuildDirectionCaption(bool forward)
    {
        var symbol = forward
            ? FirstNonEmpty(_meta.SymbolBeforeInput, _meta.SymbolAfterInput)
            : FirstNonEmpty(_meta.SymbolBeforeOutput, _meta.SymbolAfterOutput);
        var label = forward ? _meta.Input1 : _meta.Input2;

        if (!string.IsNullOrWhiteSpace(symbol))
            return symbol;

        if (!string.IsNullOrWhiteSpace(label))
            return label;

        return forward ? T("wizard.forward", "Forward") : T("wizard.reverse", "Reverse");
    }

    private PreparedWizardCell PrepareCellForFormulaConversion(string cell, int rowIndex)
    {
        if (!UsesFormulaStyleCellParsing())
            return new PreparedWizardCell(true, cell.Trim(), cell);

        var trimmed = cell.Trim();
        if (trimmed.Length == 0)
            return new PreparedWizardCell(false, "", cell);

        if (!TryExtractConvertibleFragment(trimmed, out var fragment))
            return new PreparedWizardCell(false, "", cell);

        if (!decimal.TryParse(
                fragment.Replace(',', '.'),
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out _))
        {
            return new PreparedWizardCell(false, "", cell);
        }

        return new PreparedWizardCell(true, fragment, rowIndex == 0 ? trimmed : cell);
    }

    private bool UsesFormulaStyleCellParsing()
    {
        return _document.LegacyMathSteps.Count > 0 ||
               _document.MathExpressions20.Count > 0 ||
               _document.CalculusSteps.Count > 0;
    }

    private string BuildUsedRuleText()
    {
        if (_document.LegacyMathSteps.Count > 0 || _document.MathExpressions20.Count > 0)
        {
            var formulas = new List<string>();
            var inputName = GetFormulaInputSymbol();
            var outputName = GetFormulaOutputSymbol();
            if (_document.LegacyMathSteps.Count > 0)
                formulas.Add(outputName + " = " + BuildLegacyFormulaText(inputName));
            formulas.AddRange(_document.MathExpressions20.Select(expression =>
                outputName + " = " + expression.Replace("ans", inputName, StringComparison.OrdinalIgnoreCase)));
            return string.Join("; ", formulas);
        }

        var name = _document.Name ?? _meta.Name;
        if (!string.IsNullOrWhiteSpace(name))
            return name.Trim();

        if (_document.ChangeRules.Count > 0)
            return "chg";

        if (_document.TranslateRules.Count > 0)
            return "trans";

        return "";
    }

    private string BuildLegacyFormulaText(string inputName)
    {
        var expression = inputName;
        foreach (var step in _document.LegacyMathSteps)
            expression += " " + step.Operator + " " + step.Number.ToString(CultureInfo.CurrentCulture);
        return expression;
    }

    private string GetFormulaInputSymbol()
    {
        var symbol = FormulaSymbolOrDefault(_meta.SymbolAfterInput, "");
        if (!string.IsNullOrWhiteSpace(symbol))
            return symbol;

        var input = _document.Inputs.FirstOrDefault(input => input.Name.Equals("x", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(input?.Name))
            return input.Name.Trim();

        return "x";
    }

    private string GetFormulaOutputSymbol()
    {
        var symbol = FormulaSymbolOrDefault(_meta.SymbolAfterOutput, "");
        if (!string.IsNullOrWhiteSpace(symbol))
            return symbol;

        var output = _document.Outputs.FirstOrDefault(output => output.Name.Equals("y", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(output?.Name))
            return output.Name.Trim();

        return "y";
    }

    private static string FormulaSymbolOrDefault(string? symbol, string fallback)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return fallback;

        var clean = symbol.Trim();
        return clean.Length <= 4 ? clean : fallback;
    }

    private string BuildUsedRuleMathMl()
    {
        if (_document.LegacyMathSteps.Count == 0 && _document.MathExpressions20.Count == 0)
            return "";

        var formulas = new List<string>();
        var inputName = GetFormulaInputSymbol();
        var outputName = GetFormulaOutputSymbol();
        if (_document.LegacyMathSteps.Count > 0)
            formulas.Add(MathMlRow(IdentifierToMathMl(outputName), "<mo>=</mo>", BuildLegacyFormulaMathMl(inputName)));
        formulas.AddRange(_document.MathExpressions20.Select(expression =>
            MathMlRow(IdentifierToMathMl(outputName), "<mo>=</mo>", ExpressionToSimpleMathMl(expression, inputName))));

        return """<math xmlns="http://www.w3.org/1998/Math/MathML" display="block"><mrow>""" +
               string.Join("<mo>&#x21D2;</mo>", formulas) +
               "</mrow></math>";
    }

    private string BuildLegacyFormulaMathMl(string inputName)
    {
        var expression = IdentifierToMathMl(inputName);
        foreach (var step in _document.LegacyMathSteps)
            expression = MathMlRow(expression, OperatorToMathMl(step.Operator), NumberToMathMl(step.Number));
        return expression;
    }

    private static string MathMlRow(params string[] parts)
    {
        return "<mrow>" + string.Join("", parts) + "</mrow>";
    }

    private static string OperatorToMathMl(char op)
    {
        return op switch
        {
            '*' => "<mo>&#x00D7;</mo>",
            '/' => "<mo>&#x00F7;</mo>",
            '+' => "<mo>+</mo>",
            '-' => "<mo>-</mo>",
            _ => "<mo>" + System.Net.WebUtility.HtmlEncode(op.ToString()) + "</mo>"
        };
    }

    private static string NumberToMathMl(decimal number)
    {
        return "<mn>" + System.Net.WebUtility.HtmlEncode(number.ToString(CultureInfo.CurrentCulture)) + "</mn>";
    }

    private static string IdentifierToMathMl(string value)
    {
        return "<mi>" + System.Net.WebUtility.HtmlEncode(value) + "</mi>";
    }

    private static string ExpressionToSimpleMathMl(string expression, string inputName)
    {
        var encoded = System.Net.WebUtility.HtmlEncode(expression);
        encoded = encoded
            .Replace("ans", IdentifierToMathMl(inputName), StringComparison.OrdinalIgnoreCase)
            .Replace("*", "<mo>&#x00D7;</mo>", StringComparison.Ordinal)
            .Replace("/", "<mo>&#x00F7;</mo>", StringComparison.Ordinal)
            .Replace("+", "<mo>+</mo>", StringComparison.Ordinal)
            .Replace("-", "<mo>-</mo>", StringComparison.Ordinal);

        return "<mrow>" + encoded + "</mrow>";
    }

    private static bool TryExtractConvertibleFragment(string cell, out string fragment)
    {
        fragment = "";

        var match = NumericFragmentRegex.Match(cell);
        if (!match.Success)
            return false;

        fragment = match.Value.Trim();
        return fragment.Length > 0;
    }

    private string DecorateWizardOutput(string resultText)
    {
        // WizardExpress should behave like a plain clipboard converter:
        // return only the converted value so spreadsheets can accept it cleanly.
        return resultText.Trim();
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return "";
    }

    private void RefreshWizardState(bool forceReady = false)
    {
        if (forceReady)
        {
            if (_status == WizardStatus.Done)
                SetWizardStatus(WizardStatus.Empty);
            return;
        }

        try
        {
            var snapshot = ReadClipboardSnapshot();
            if (snapshot.SequenceNumber != 0 &&
                snapshot.SequenceNumber == _lastObservedClipboardSequence &&
                _status is WizardStatus.Done or WizardStatus.ClipboardOverwritten)
            {
                return;
            }

            _lastObservedClipboardSequence = snapshot.SequenceNumber;
            var currentClipboard = snapshot.Text;

            // Hoofdregel van de oude wizard:
            // zolang de huidige clipboarddata exact gelijk is aan de laatst geconverteerde data,
            // is er niets veranderd en blijft de status "Converted".
            if (!string.IsNullOrWhiteSpace(_lastConvertedClipboardText) &&
                string.Equals(currentClipboard, _lastConvertedClipboardText, StringComparison.Ordinal) &&
                _lastForwardDirection == _forward.Checked)
            {
                _lastSeenClipboardText = currentClipboard;
                if (_status != WizardStatus.Done)
                    SetWizardStatus(WizardStatus.Done);
                UpdateDebugInfo(snapshot);
                return;
            }

            if (_status == WizardStatus.ClipboardOverwritten &&
                !string.Equals(currentClipboard, _lastConvertedClipboardText, StringComparison.Ordinal) &&
                _lastForwardDirection == _forward.Checked &&
                IsExcelClipboardOwner(snapshot.Owner))
            {
                UpdateDebugInfo(snapshot);
                return;
            }

            if (_status != WizardStatus.Done)
            {
                if (!string.Equals(currentClipboard, _lastSeenClipboardText, StringComparison.Ordinal))
                {
                    _input.Text = currentClipboard;
                    _lastSeenClipboardText = currentClipboard;
                    _lastConversionSummary = null;
                    _lastConversionBeforeText = "";
                    _lastConversionAfterText = "";
                    _lastUsedRuleText = "";
                    _lastUsedRuleMathMl = "";
                    SetWizardStatus(WizardStatus.Empty);
                    UpdateAdvancedData(_input.Text, _output.Text);
                    UpdateDebugInfo(snapshot);
                    return;
                }

                SetWizardStatus(string.IsNullOrWhiteSpace(currentClipboard) ? WizardStatus.Empty : WizardStatus.Ready);
                UpdateDebugInfo(snapshot);
                return;
            }

            if (_status == WizardStatus.Done &&
                (!string.Equals(currentClipboard, _lastConvertedClipboardText, StringComparison.Ordinal) ||
                 _lastForwardDirection != _forward.Checked))
            {
                if (_lastForwardDirection == _forward.Checked && IsExcelClipboardOwner(snapshot.Owner))
                {
                    SetWizardStatus(WizardStatus.ClipboardOverwritten);
                    ShowExcelClipboardWarningIfNeeded();
                    UpdateDebugInfo(snapshot);
                    return;
                }

                _input.Text = currentClipboard;
                _output.Text = "";
                _lastSeenClipboardText = currentClipboard;
                _lastConversionSummary = null;
                _lastConversionBeforeText = "";
                _lastConversionAfterText = "";
                _lastUsedRuleText = "";
                _lastUsedRuleMathMl = "";
                SetWizardStatus(WizardStatus.Empty);
                UpdateAdvancedData(_input.Text, _output.Text);
                UpdateDebugInfo(snapshot);
            }
        }
        catch
        {
            if (_status == WizardStatus.Done)
                SetWizardStatus(WizardStatus.Empty);
            UpdateDebugInfo(ClipboardSnapshot.Empty);
        }
    }

    private void SetWizardStatus(WizardStatus status)
    {
        _status = status;
        _statusLabel.Text = status switch
        {
            WizardStatus.Ready => T("wizard.status.ready", "Ready"),
            WizardStatus.Done => T("wizard.status.done", "Converted"),
            WizardStatus.ClipboardOverwritten => T("wizard.status.clipboard_overwritten", "Excel overwrote the clipboard. Use Ctrl+V."),
            WizardStatus.Error => T("wizard.status.error", "Error"),
            _ => T("wizard.status.empty", "Start")
        };
        _trafficLight.BackColor = status switch
        {
            WizardStatus.Done => Color.LimeGreen,
            WizardStatus.ClipboardOverwritten => Color.DarkOrange,
            WizardStatus.Ready => Color.Goldenrod,
            WizardStatus.Error => Color.Tomato,
            _ => Color.Red
        };
    }

    private static bool IsExcelClipboardOwner(string owner)
    {
        return owner.Contains("excel", StringComparison.OrdinalIgnoreCase);
    }

    private void ShowExcelClipboardWarningIfNeeded()
    {
        if (_excelClipboardWarningShown)
            return;

        _excelClipboardWarningShown = true;
        BeginInvoke(() =>
        {
            var restored = ClipboardConvertApi.SetText(_lastConvertedClipboardText, Handle);
            MessageBox.Show(
                this,
                restored
                    ? T("wizard.excel_clipboard_restored.message", "Excel has overwritten the clipboard. WizardExpress restored the converted value. Use Ctrl+V now. If paste still does not work, copy the values from Excel again and run WizardExpress again.")
                    : T("wizard.excel_clipboard_overwritten.message", "Excel has overwritten the clipboard. Use Ctrl+V immediately after WizardExpress writes the converted value. If paste does not work, copy the values from Excel again and run WizardExpress again."),
                T("wizard.excel_clipboard_overwritten.title", "Excel clipboard warning"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        });
    }

    private static ClipboardSnapshot ReadClipboardSnapshot()
    {
        return ClipboardConvertApi.ReadSnapshot();
    }

    private void UpdateDebugInfo(ClipboardSnapshot snapshot)
    {
        if (_debugForm is null || _debugForm.IsDisposed)
            return;

        var report = ClipboardDebugApi.CreateReport(snapshot, _lastSeenClipboardText, _lastConvertedClipboardText);
        _debugForm.SetReport(
            report,
            snapshot.Text,
            _lastConversionSummary,
            _lastConversionBeforeText,
            _lastConversionAfterText,
            _lastUsedRuleText,
            _lastUsedRuleMathMl);
    }

    private void ShowDebugDialog()
    {
        ShowAdvancedDialog(AdvancedDialogTab.Details);
    }

    private void ShowTableDialog()
    {
        ShowAdvancedDialog(AdvancedDialogTab.Input);
    }

    private void ShowAdvancedDialog(AdvancedDialogTab selectedTab)
    {
        if (!_advancedMode)
            return;

        if (_debugForm is not null && !_debugForm.IsDisposed)
        {
            if (!_debugForm.Visible)
                _debugForm.Show(this);
            _debugForm.BringToFront();
            _debugForm.Activate();
            _debugForm.SelectTab(selectedTab);
            return;
        }

        _debugForm = new ClipboardDebugForm(T, () => CopyOutputToClipboard())
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(Right + 12, Top)
        };
        _debugForm.FormClosed += (_, _) => _debugForm = null;
        _debugForm.Show(this);
        _debugForm.SelectTab(selectedTab);
    }

    private void UpdateAdvancedData(string inputText, string outputText)
    {
        if (_debugForm is null || _debugForm.IsDisposed)
            return;

        _debugForm.SetData(inputText, outputText);
    }

    private sealed class ClipboardDebugForm : Form
    {
        private readonly Func<string, string, string> _text;
        private readonly Action _copyOutput;
        private readonly TabControl _tabs;
        private readonly DataGridView _inputGrid;
        private readonly DataGridView _outputGrid;
        private readonly ClipboardReportView _reportView;
        private readonly Label _summary;
        private readonly DataGridView _grid;
        private readonly Button _copyOutputButton;
        private readonly Button _copyReportButton;
        private readonly Button _mailReportButton;
        private readonly Button _savePdfButton;
        private ClipboardDebugReport _lastReport;
        private string _lastClipboardText = "";
        private string _lastBeforeText = "";
        private string _lastAfterText = "";
        private string _lastUsedRuleText = "";
        private string _lastUsedRuleMathMl = "";
        private ClipboardConversionSummary? _lastConversionSummary;

        public ClipboardDebugForm(Func<string, string, string> text, Action copyOutput)
        {
            _text = text;
            _copyOutput = copyOutput;
            Text = T("wizard.advanced.title", "WizardExpress Data");
            AppWindowIcon.ApplyTo(this);
            Width = 680;
            Height = 430;
            MinimumSize = new Size(560, 340);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;

            _summary = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(10, 8, 10, 4),
                BackColor = Color.FromArgb(239, 246, 255),
                ForeColor = Color.FromArgb(15, 63, 143),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Text = T("clipboard.debug.window_title", "Clipboard Debug")
            };

            _grid = CreateGrid();
            _inputGrid = CreateGrid();
            _outputGrid = CreateGrid();
            _reportView = ClipboardReportViewApi.CreateReportView(
                T,
                () => new HtmlMathPreviewControl
                {
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(17, 24, 39),
                    Font = new Font("Cambria Math", 22f, FontStyle.Bold)
                },
                (control, markup) =>
                {
                    if (control is HtmlMathPreviewControl mathView)
                        mathView.MathMarkup = markup;
                });
            _copyOutputButton = new Button
            {
                Dock = DockStyle.Left,
                Width = 135,
                Text = T("wizard.advanced.copy_tsv", "Copy output TSV")
            };
            _copyOutputButton.Click += (_, _) => _copyOutput();
            _mailReportButton = new Button
            {
                Dock = DockStyle.Right,
                Width = 165,
                Text = T("clipboard.debug.mail_button", "Report by e-mail...")
            };
            _mailReportButton.Click += (_, _) => MailReport();
            _copyReportButton = new Button
            {
                Dock = DockStyle.Right,
                Width = 145,
                Text = T("clipboard.debug.copy_button", "Copy report")
            };
            _copyReportButton.Click += (_, _) => CopyReport();
            _savePdfButton = new Button
            {
                Dock = DockStyle.Right,
                Width = 125,
                Text = T("clipboard.debug.save_pdf_button", "Save PDF...")
            };
            _savePdfButton.Click += (_, _) => SavePdfReport();

            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                Padding = new Padding(6, 4, 6, 4),
                BackColor = Color.FromArgb(247, 249, 252)
            };
            footer.Controls.Add(_copyOutputButton);
            footer.Controls.Add(_mailReportButton);
            footer.Controls.Add(_copyReportButton);
            footer.Controls.Add(_savePdfButton);

            _tabs = new TabControl
            {
                Dock = DockStyle.Fill
            };
            _tabs.TabPages.Add(CreateTab(T("wizard.input", "Input"), _inputGrid));
            _tabs.TabPages.Add(CreateTab(T("wizard.output", "Output"), _outputGrid));
            _tabs.TabPages.Add(CreateTab(T("clipboard.debug.preview", "Preview"), _grid));
            _tabs.TabPages.Add(CreateTab(T("clipboard.debug.details", "Details"), _reportView));

            Controls.Add(_tabs);
            Controls.Add(footer);
            Controls.Add(_summary);
        }

        public void SelectTab(AdvancedDialogTab tab)
        {
            var index = tab switch
            {
                AdvancedDialogTab.Output => 1,
                AdvancedDialogTab.Details => 3,
                _ => 0
            };
            if (index >= 0 && index < _tabs.TabPages.Count)
                _tabs.SelectedIndex = index;
        }

        public void SetData(string inputText, string outputText)
        {
            FillGrid(_inputGrid, inputText);
            FillGrid(_outputGrid, outputText);
            _copyOutputButton.Enabled = !string.IsNullOrWhiteSpace(outputText);
        }

        public void SetReport(
            ClipboardDebugReport report,
            string clipboardText,
            ClipboardConversionSummary? conversionSummary,
            string beforeText,
            string afterText,
            string usedRuleText,
            string usedRuleMathMl)
        {
            _lastReport = report;
            _lastClipboardText = clipboardText;
            _lastConversionSummary = conversionSummary;
            _lastBeforeText = beforeText;
            _lastAfterText = afterText;
            _lastUsedRuleText = usedRuleText;
            _lastUsedRuleMathMl = usedRuleMathMl;
            _summary.Text = string.Format(
                CultureInfo.CurrentCulture,
                T("clipboard.debug.summary_format", "Rows {0}  |  Columns {1}  |  Text {2} chars"),
                report.LineCount,
                report.ColumnCount,
                report.TextLength);
            ClipboardReportViewApi.FillReportView(
                _reportView,
                report,
                clipboardText,
                beforeText,
                afterText,
                usedRuleText,
                usedRuleMathMl);
            FillGrid(_grid, clipboardText);
        }

        private static TabPage CreateTab(string title, Control content)
        {
            var page = new TabPage(title)
            {
                Padding = new Padding(6),
                BackColor = Color.White
            };
            page.Controls.Add(content);
            return page;
        }

        private static DataGridView CreateGrid()
        {
            return ClipboardGridApi.CreateExcelLikeGrid();
        }

        private static void FillGrid(DataGridView grid, string text)
        {
            ClipboardGridApi.FillExcelLikeGrid(grid, text);
        }

        private void MailReport()
        {
            var subject = ClipboardSupportMailApi.BuildSubject(AppVersionInfo.ProductName, T);
            var body = BuildSupportReportBody();
            var htmlBody = BuildSupportReportHtmlBody();

            try
            {
                var mailPath = ClipboardSupportMailApi.SaveHtmlMailMessage(
                    Path.Combine(Path.GetTempPath(), "Syscalculator"),
                    AppVersionInfo.Email,
                    subject,
                    htmlBody,
                    body);
                Process.Start(new ProcessStartInfo(mailPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    string.Format(CultureInfo.CurrentCulture, T("clipboard.debug.mail_failed", "Could not open e-mail program: {0}"), ex.Message),
                    T("clipboard.debug.window_title", "Clipboard Debug"),
                    MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            }
        }

        private void CopyReport()
        {
            var body = BuildSupportReportBody();
            if (CopySupportReportToClipboard(body, BuildSupportReportHtmlBody()))
            {
                _summary.Text = T("clipboard.debug.report_copied", "Report copied to clipboard.");
                return;
            }

            MessageBox.Show(
                this,
                T("clipboard.debug.copy_failed", "Could not copy the report to clipboard."),
                T("clipboard.debug.window_title", "Clipboard Debug"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private async void SavePdfReport()
        {
            using var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "pdf",
                FileName = "clipboard-diagnosis-" + DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture) + ".pdf",
                Filter = T("clipboard.debug.pdf_filter", "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*"),
                Title = T("clipboard.debug.save_pdf_title", "Save diagnosis report")
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                var formulaJpeg = await RenderFormulaJpegAsync();
                var logoJpeg = RenderTiedragonLogoJpeg();
                ClipboardPdfReportApi.SaveReportPdf(
                    dialog.FileName,
                    _lastReport,
                    _lastClipboardText,
                    AppVersionInfo.DisplayVersion,
                    T,
                    _lastConversionSummary,
                    _lastBeforeText,
                    _lastAfterText,
                    _lastUsedRuleText,
                    _lastUsedRuleMathMl,
                    formulaJpeg,
                    logoJpeg);
                _summary.Text = T("clipboard.debug.pdf_saved", "PDF report saved.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    string.Format(CultureInfo.CurrentCulture, T("clipboard.debug.pdf_failed", "Could not save PDF report: {0}"), ex.Message),
                    T("clipboard.debug.window_title", "Clipboard Debug"),
                    MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            }
        }

        private async Task<byte[]?> RenderFormulaJpegAsync()
        {
            if (string.IsNullOrWhiteSpace(_lastUsedRuleMathMl))
                return null;

            try
            {
                using var host = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual,
                    Location = new Point(-32000, -32000),
                    Size = new Size(500, 58)
                };

                var preview = new HtmlMathPreviewControl
                {
                    BackColor = Color.White,
                    Dock = DockStyle.Fill,
                    ForeColor = Color.FromArgb(17, 24, 39),
                    Font = new Font("Cambria Math", 22f, FontStyle.Bold)
                };

                host.Controls.Add(preview);
                host.Show(this);
                preview.MathMarkup = _lastUsedRuleMathMl;

                try
                {
                    return await preview.CaptureJpegAsync();
                }
                finally
                {
                    host.Close();
                }
            }
            catch
            {
                return null;
            }
        }

        private static byte[]? RenderTiedragonLogoJpeg()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Resources", "AboutSyscalculator.png");
            if (!File.Exists(path))
                return null;

            try
            {
                using var source = Image.FromFile(path);
                var crop = new Rectangle(35, 8, 120, 126);
                crop.Intersect(new Rectangle(Point.Empty, source.Size));
                if (crop.Width <= 0 || crop.Height <= 0)
                    return null;

                using var bitmap = new Bitmap(crop.Width, crop.Height);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.White);
                    graphics.DrawImage(source, new Rectangle(Point.Empty, crop.Size), crop, GraphicsUnit.Pixel);
                }

                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Jpeg);
                return stream.ToArray();
            }
            catch
            {
                return null;
            }
        }

        private string BuildSupportReportBody()
        {
            return ClipboardSupportMailApi.BuildBody(
                _lastReport,
                _lastClipboardText,
                AppVersionInfo.DisplayVersion,
                T,
                _lastConversionSummary,
                _lastBeforeText,
                _lastAfterText,
                _lastUsedRuleText,
                _lastUsedRuleMathMl);
        }

        private string BuildSupportReportHtmlBody()
        {
            return ClipboardSupportMailApi.BuildHtmlBody(
                _lastReport,
                _lastClipboardText,
                AppVersionInfo.DisplayVersion,
                T,
                _lastConversionSummary,
                _lastBeforeText,
                _lastAfterText,
                _lastUsedRuleText,
                _lastUsedRuleMathMl);
        }

        private bool CopySupportReportToClipboard(string plainText, string html)
        {
            try
            {
                var data = new DataObject();
                data.SetText(plainText, TextDataFormat.UnicodeText);
                data.SetData(DataFormats.Html, ClipboardSupportMailApi.BuildClipboardHtmlFragment(html));
                Clipboard.SetDataObject(data, true);
                return true;
            }
            catch
            {
                return ClipboardConvertApi.SetText(plainText, Handle);
            }
        }

        private string T(string key, string fallback) => _text(key, fallback);
    }

    private void TrafficLight_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var housingBrush = new SolidBrush(Color.FromArgb(48, 48, 48));
        using var housingPen = new Pen(Color.FromArgb(25, 25, 25));
        e.Graphics.FillRectangle(housingBrush, 0, 0, _trafficLight.Width - 1, _trafficLight.Height - 1);
        e.Graphics.DrawRectangle(housingPen, 0, 0, _trafficLight.Width - 1, _trafficLight.Height - 1);

        DrawTrafficLamp(e.Graphics, new Rectangle(3, 4, 6, 6), Color.Firebrick, _status is WizardStatus.Empty or WizardStatus.Error);
        DrawTrafficLamp(e.Graphics, new Rectangle(11, 4, 6, 6), Color.Goldenrod, _status == WizardStatus.Ready);
        DrawTrafficLamp(e.Graphics, new Rectangle(19, 4, 6, 6), Color.ForestGreen, _status == WizardStatus.Done);
    }

    private static void DrawTrafficLamp(Graphics graphics, Rectangle bounds, Color color, bool active)
    {
        var fill = active ? color : Color.FromArgb(70, 70, 70);
        using var brush = new SolidBrush(fill);
        using var pen = new Pen(active ? ControlPaint.Light(color) : Color.FromArgb(35, 35, 35));
        graphics.FillEllipse(brush, bounds);
        graphics.DrawEllipse(pen, bounds);

        if (!active)
            return;

        using var shine = new SolidBrush(Color.FromArgb(110, Color.White));
        graphics.FillEllipse(shine, bounds.Left + 4, bounds.Top + 3, 5, 5);
    }

    private string T(string key, string fallback) => _language.Text(key, fallback);

    private readonly record struct PreparedWizardCell(bool ShouldConvert, string ConversionText, string OutputText);
}





