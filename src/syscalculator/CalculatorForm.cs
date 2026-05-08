using Tiedragon.ClipboardConvert;

namespace Syscalculator.UI.WinForms;

/// <summary>
/// Eenvoudige calculatorform.
/// Moderne vervanger voor de oude Calculator tool.
///
/// Nieuw:
/// - kan live meeschrijven naar de actieve invoerbox via callback.
/// </summary>
public sealed class CalculatorForm : Form
{
    private readonly Action<string>? _previewResult;
    private readonly LanguageCatalog _language;
    private readonly ToolTip _toolTip = new();

    private TextBox _display = null!;
    private decimal _memory;
    private decimal _current;
    private string _operator = "";
    private bool _newNumber = true;

    private enum CalculatorIcon { Copy, Paste }

    // Zoek/commentaar: Constructor: maakt en initialiseert CalculatorForm.
    public CalculatorForm(string? initialValue = null, Action<string>? previewResult = null)
    {
        _previewResult = previewResult;
        _language = LanguageCatalog.LoadConfigured(AppContext.BaseDirectory);

        Text = T("calculator.title", "Calculator");
        Width = 360;
        Height = 430;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        BuildLayout();

        if (!string.IsNullOrWhiteSpace(initialValue))
            _display.Text = initialValue;
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildLayout.
    private void BuildLayout()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 8,
            Padding = new Padding(12)
        };

        for (var i = 0; i < 5; i++)
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        for (var i = 1; i < 8; i++)
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 14.2f));

        _display = new TextBox
        {
            Text = "0",
            TextAlign = HorizontalAlignment.Right,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14),
            ReadOnly = true
        };

        panel.SetColumnSpan(_display, 5);
        panel.Controls.Add(_display, 0, 0);

        string[,] buttons =
        {
            { "7", "8", "9", "/", "sqrt" },
            { "4", "5", "6", "*", "1/x" },
            { "1", "2", "3", "-", "%" },
            { "0", ",", "=", "+", "+/-" },
            { "C", "CE", "MR", "MC", "M+" },
            { "MS", "Copy", "Paste", "", "" },
            { "", "", "", "", "" }
        };

        for (var r = 0; r < 7; r++)
        {
            for (var c = 0; c < 5; c++)
            {
                var text = buttons[r, c];
                if (string.IsNullOrEmpty(text))
                    continue;

                var btn = new Button
                {
                    Text = GetButtonText(text),
                    Dock = DockStyle.Fill,
                    Margin = new Padding(3)
                };

                if (text == "Copy" || text == "Paste")
                {
                    btn.Text = "";
                    btn.Image = CreateCalculatorIcon(text == "Copy" ? CalculatorIcon.Copy : CalculatorIcon.Paste);
                    _toolTip.SetToolTip(btn, GetButtonText(text));
                }

                btn.Click += (_, _) => Press(text);
                panel.Controls.Add(btn, c, r + 1);
            }
        }

        Controls.Add(panel);
    }

    // Zoek/commentaar: Verwerkt een knopdruk voor Press.
    private void Press(string key)
    {
        var previousDisplay = _display.Text;

        if (char.IsDigit(key[0]) || key == ",")
        {
            if (_newNumber)
            {
                _display.Text = key == "," ? "0," : key;
                _newNumber = false;
            }
            else if (key != "," || !_display.Text.Contains(','))
            {
                _display.Text += key;
            }

            NotifyPreviewIfChanged(previousDisplay);
            return;
        }

        switch (key)
        {
            case "C":
                _current = 0;
                _operator = "";
                _display.Text = "0";
                _newNumber = true;
                break;

            case "CE":
                _display.Text = "0";
                _newNumber = true;
                break;

            case "+/-":
                if (_display.Text.StartsWith("-"))
                    _display.Text = _display.Text[1..];
                else if (_display.Text != "0")
                    _display.Text = "-" + _display.Text;
                break;

            case "sqrt":
                SetDisplay((decimal)Math.Sqrt((double)GetDisplay()));
                _newNumber = true;
                break;

            case "1/x":
                SetDisplay(1 / GetDisplay());
                _newNumber = true;
                break;

            case "%":
                SetDisplay(GetDisplay() / 100);
                _newNumber = true;
                break;

            case "MR":
                SetDisplay(_memory);
                _newNumber = true;
                break;

            case "MC":
                _memory = 0;
                break;

            case "M+":
                _memory += GetDisplay();
                _newNumber = true;
                break;

            case "MS":
                _memory = GetDisplay();
                _newNumber = true;
                break;

            case "Copy":
                ClipboardConvertApi.SetText(_display.Text);
                break;

            case "Paste":
                if (ClipboardConvertApi.TryGetText(out var pastedText))
                {
                    var pasted = pastedText.Trim();
                    if (!string.IsNullOrWhiteSpace(pasted))
                    {
                        _display.Text = pasted;
                        _newNumber = true;
                    }
                }
                break;

            case "+":
            case "-":
            case "*":
            case "/":
                ApplyPending();
                _operator = key;
                _current = GetDisplay();
                _newNumber = true;
                break;

            case "=":
                ApplyPending();
                _operator = "";
                _newNumber = true;
                break;
        }

        NotifyPreviewIfChanged(previousDisplay);
    }

    private void NotifyPreviewIfChanged(string previousDisplay)
    {
        if (_display.Text != previousDisplay)
            _previewResult?.Invoke(_display.Text);
    }

    // Zoek/commentaar: Past een regel, instelling of bewerking toe voor ApplyPending.
    private void ApplyPending()
    {
        if (string.IsNullOrWhiteSpace(_operator))
        {
            _current = GetDisplay();
            return;
        }

        var value = GetDisplay();

        var result = _operator switch
        {
            "+" => _current + value,
            "-" => _current - value,
            "*" => _current * value,
            "/" => value == 0 ? 0 : _current / value,
            _ => value
        };

        SetDisplay(result);
        _current = result;
    }

    // Zoek/commentaar: Haalt een waarde, tekst of instelling op voor GetDisplay.
    private decimal GetDisplay()
    {
        return decimal.TryParse(_display.Text, out var value) ? value : 0;
    }

    // Zoek/commentaar: Zet een waarde of status voor SetDisplay.
    private void SetDisplay(decimal value)
    {
        _display.Text = value.ToString("0.##########");
    }

    // Zoek/commentaar: Haalt een waarde, tekst of instelling op voor GetButtonText.
    private string GetButtonText(string key)
    {
        return key switch
        {
            "Copy" => T("calculator.copy", "Copy"),
            "Paste" => T("calculator.paste", "Paste"),
            _ => key
        };
    }

    private static Bitmap CreateCalculatorIcon(CalculatorIcon icon)
    {
        var bitmap = new Bitmap(22, 22);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var pen = new Pen(Color.FromArgb(0, 65, 170), 1.8f)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round
        };
        using var brush = new SolidBrush(Color.FromArgb(232, 242, 255));

        if (icon == CalculatorIcon.Copy)
        {
            graphics.FillRectangle(brush, 8, 6, 9, 11);
            graphics.DrawRectangle(pen, 8, 6, 9, 11);
            graphics.DrawRectangle(pen, 5, 3, 9, 11);
        }
        else
        {
            graphics.FillRectangle(brush, 6, 6, 11, 12);
            graphics.DrawRectangle(pen, 6, 6, 11, 12);
            graphics.DrawLine(pen, 9, 4, 14, 4);
            graphics.DrawLine(pen, 9, 4, 8, 7);
            graphics.DrawLine(pen, 14, 4, 15, 7);
            graphics.DrawLine(pen, 9, 10, 14, 10);
            graphics.DrawLine(pen, 9, 14, 14, 14);
        }

        return bitmap;
    }

    // Zoek/commentaar: Methode T: centrale logica voor deze stap.
    private string T(string key, string fallback) => _language.Text(key, fallback);
}
