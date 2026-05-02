using System.Text;
using NodSystem.Core;

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
    private readonly NodDocument _document;
    private readonly LanguageCatalog _language;

    private TextBox _input = null!;
    private TextBox _output = null!;
    private RadioButton _forward = null!;
    private RadioButton _reverse = null!;
    private Panel _trafficLight = null!;
    private Label _statusLabel = null!;
    private WizardStatus _status = WizardStatus.Empty;

    // Zoek/commentaar: Type-overzicht: enum WizardStatus bevat de hoofdlogica/data voor dit onderdeel.
    private enum WizardStatus { Empty, Ready, Done, Error }

    // Zoek/commentaar: Constructor: maakt en initialiseert WizardExpressForm.
    public WizardExpressForm(NodDocument document)
    {
        _document = document;
        _language = LanguageCatalog.LoadConfigured(AppContext.BaseDirectory);

        Text = T("wizard.title", "WizardExpress");
        Width = 850;
        Height = 600;
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildLayout.
    private void BuildLayout()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(12)
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        _forward = new RadioButton { Text = T("wizard.forward", "Forward"), Checked = true, AutoSize = true };
        _reverse = new RadioButton { Text = T("wizard.reverse", "Reverse"), AutoSize = true };

        var options = new FlowLayoutPanel { Dock = DockStyle.Fill };
        _trafficLight = new Panel
        {
            Width = 28,
            Height = 70,
            Margin = new Padding(0, 0, 10, 0),
            BackColor = Color.FromArgb(48, 48, 48)
        };
        _trafficLight.Paint += TrafficLight_Paint;
        _statusLabel = new Label { Text = T("wizard.status.empty", "No input"), AutoSize = true, Margin = new Padding(0, 8, 18, 0) };
        options.Controls.Add(_trafficLight);
        options.Controls.Add(_statusLabel);
        options.Controls.Add(_forward);
        options.Controls.Add(_reverse);

        var paste = new Button { Text = T("wizard.paste", "Paste from Clipboard") };
        paste.Click += (_, _) =>
        {
            _input.Text = Clipboard.GetText();
            SetWizardStatus(string.IsNullOrWhiteSpace(_input.Text) ? WizardStatus.Empty : WizardStatus.Ready);
        };

        options.Controls.Add(paste);

        panel.SetColumnSpan(options, 2);
        panel.Controls.Add(options, 0, 0);

        _input = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            AcceptsTab = true,
            Font = new Font(FontFamily.GenericMonospace, 10),
            Dock = DockStyle.Fill
        };
        _input.TextChanged += (_, _) =>
        {
            if (_status != WizardStatus.Done || !string.IsNullOrWhiteSpace(_input.Text))
                SetWizardStatus(string.IsNullOrWhiteSpace(_input.Text) ? WizardStatus.Empty : WizardStatus.Ready);
        };

        _output = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            AcceptsTab = true,
            Font = new Font(FontFamily.GenericMonospace, 10),
            Dock = DockStyle.Fill
        };

        panel.Controls.Add(_input, 0, 1);
        panel.Controls.Add(_output, 1, 1);

        var convert = new Button { Text = T("wizard.convert", "Convert") };
        convert.Click += (_, _) => ConvertText();

        var copy = new Button { Text = T("wizard.copy_output", "Output to Clipboard") };
        copy.Click += (_, _) => Clipboard.SetText(_output.Text);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill };
        buttons.Controls.Add(convert);
        buttons.Controls.Add(copy);

        panel.SetColumnSpan(buttons, 2);
        panel.Controls.Add(buttons, 0, 2);

        panel.Controls.Add(new Label { Text = T("wizard.hint", "Left: input / right: output. Tab-delimited tables are preserved."), AutoSize = true }, 0, 3);

        Controls.Add(panel);
        SetWizardStatus(WizardStatus.Empty);
    }

    // Zoek/commentaar: Voert een conversie uit voor ConvertText.
    private void ConvertText()
    {
        if (string.IsNullOrWhiteSpace(_input.Text))
        {
            SetWizardStatus(WizardStatus.Empty);
            return;
        }

        var lines = _input.Text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var sb = new StringBuilder();
        var hadError = false;

        foreach (var line in lines)
        {
            var cells = line.Split('\t');
            for (var i = 0; i < cells.Length; i++)
            {
                if (i > 0)
                    sb.Append('\t');

                var converted = ConvertCell(cells[i], out var cellHadError);
                hadError |= cellHadError;
                sb.Append(converted);
            }

            sb.AppendLine();
        }

        _output.Text = sb.ToString();
        SetWizardStatus(hadError ? WizardStatus.Error : WizardStatus.Done);
    }

    // Zoek/commentaar: Voert een conversie uit voor ConvertCell.
    private string ConvertCell(string cell, out bool hadError)
    {
        hadError = false;
        if (string.IsNullOrWhiteSpace(cell))
            return cell;

        try
        {
            var result = _forward.Checked
                ? NodEngine.ConvertForward(_document, cell.Trim())
                : NodEngine.ConvertReverse(_document, cell.Trim());

            return result.Text;
        }
        catch
        {
            // Oude WizardExpress was tolerant: cel met fout blijft staan.
            hadError = true;
            return cell;
        }
    }

    // Zoek/commentaar: Zet een waarde of status voor SetWizardStatus.
    private void SetWizardStatus(WizardStatus status)
    {
        _status = status;
        _statusLabel.Text = status switch
        {
            WizardStatus.Ready => T("wizard.status.ready", "Ready"),
            WizardStatus.Done => T("wizard.status.done", "Converted"),
            WizardStatus.Error => T("wizard.status.error", "Completed with errors"),
            _ => T("wizard.status.empty", "No input")
        };
        _trafficLight.Invalidate();
    }

    // Zoek/commentaar: Tekent het oude WizardExpress-stoplicht met rood, geel en groen.
    private void TrafficLight_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var housingBrush = new SolidBrush(Color.FromArgb(48, 48, 48));
        using var housingPen = new Pen(Color.FromArgb(25, 25, 25));
        e.Graphics.FillRectangle(housingBrush, 0, 0, _trafficLight.Width - 1, _trafficLight.Height - 1);
        e.Graphics.DrawRectangle(housingPen, 0, 0, _trafficLight.Width - 1, _trafficLight.Height - 1);

        DrawTrafficLamp(e.Graphics, new Rectangle(6, 6, 16, 16), Color.Firebrick, _status is WizardStatus.Empty or WizardStatus.Error);
        DrawTrafficLamp(e.Graphics, new Rectangle(6, 27, 16, 16), Color.Goldenrod, _status == WizardStatus.Ready);
        DrawTrafficLamp(e.Graphics, new Rectangle(6, 48, 16, 16), Color.ForestGreen, _status == WizardStatus.Done);
    }

    // Zoek/commentaar: Tekent een losse lamp in het WizardExpress-stoplicht.
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

    // Zoek/commentaar: Methode T: centrale logica voor deze stap.
    private string T(string key, string fallback) => _language.Text(key, fallback);
}
