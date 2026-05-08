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
    // Zoek/commentaar: Constructor: maakt en initialiseert IntroDialogForm.
    public IntroDialogForm(string title, IEnumerable<string> lines)
    {
        Text = string.IsNullOrWhiteSpace(title) ? "Introductie" : title;
        Width = 560;
        Height = 330;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        BuildLayout(lines);
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildLayout.
    private void BuildLayout(IEnumerable<string> lines)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(16)
        };

        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var titleLabel = new Label
        {
            Text = Text,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
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
            BackColor = SystemColors.Window
        };

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Anchor = AnchorStyles.Right,
            Width = 90
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        buttonPanel.Controls.Add(ok);

        AcceptButton = ok;

        panel.Controls.Add(titleLabel, 0, 0);
        panel.Controls.Add(textBox, 0, 1);
        panel.Controls.Add(buttonPanel, 0, 2);

        Controls.Add(panel);
    }
}
