using Tiedragon.ToolEditor;

namespace Syscalculator.UI.WinForms;

/// <summary>
/// Introductiedialoog voor oude NOD-regels:
///
/// indoprint ...
/// indoprint ...
/// indoend
///
/// Deze dialog verschijnt voordat een converter gebruikt wordt.
/// </summary>
public sealed class IntroDialogForm : Form
{
    private readonly ToolEditorUiTheme _uiTheme;
    private bool IsDarkTheme => _uiTheme == ToolEditorUiTheme.Dark;
    private Color WindowBackColor => IsDarkTheme ? Color.FromArgb(18, 24, 32) : SystemColors.Control;
    private Color EditorBackColor => IsDarkTheme ? Color.FromArgb(39, 39, 39) : SystemColors.Window;
    private Color EditorTextColor => IsDarkTheme ? Color.FromArgb(226, 232, 240) : SystemColors.ControlText;

    // Zoek/commentaar: Constructor: maakt en initialiseert IntroDialogForm.
    public IntroDialogForm(string title, IEnumerable<string> lines)
    {
        _uiTheme = ToolEditorUiThemeSettings.Load();
        Text = string.IsNullOrWhiteSpace(title) ? "Introductie" : title;
        AppWindowIcon.ApplyTo(this);
        Width = 560;
        Height = 330;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = WindowBackColor;
        ForeColor = EditorTextColor;

        BuildLayout(lines);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ToolEditorUiThemeSettings.ApplyNativeWindowTheme(this, _uiTheme);
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildLayout.
    private void BuildLayout(IEnumerable<string> lines)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(16),
            BackColor = WindowBackColor,
            ForeColor = EditorTextColor
        };

        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var titleLabel = new Label
        {
            Text = Text,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = WindowBackColor,
            ForeColor = EditorTextColor,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var textBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            Text = string.Join(Environment.NewLine, lines),
            BackColor = EditorBackColor,
            ForeColor = EditorTextColor
        };

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Anchor = AnchorStyles.Right,
            Width = 90,
            BackColor = IsDarkTheme ? Color.FromArgb(37, 99, 235) : SystemColors.Control,
            ForeColor = IsDarkTheme ? Color.White : SystemColors.ControlText,
            UseVisualStyleBackColor = false
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = WindowBackColor,
            ForeColor = EditorTextColor
        };
        buttonPanel.Controls.Add(ok);

        AcceptButton = ok;

        panel.Controls.Add(titleLabel, 0, 0);
        panel.Controls.Add(textBox, 0, 1);
        panel.Controls.Add(buttonPanel, 0, 2);

        Controls.Add(panel);
    }
}
