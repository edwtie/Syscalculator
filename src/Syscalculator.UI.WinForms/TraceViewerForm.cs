using NodSystem.Core;

namespace Syscalculator.UI.WinForms;

/// <summary>
/// Toont Calculation Trace.
/// </summary>
public sealed class TraceViewerForm : Form
{
    // Zoek/commentaar: Constructor: maakt en initialiseert TraceViewerForm.
    public TraceViewerForm(CalculationTrace? trace)
    {
        var language = LanguageCatalog.LoadConfigured(AppContext.BaseDirectory);
        Text = language.Text("trace.title", "Calculation Trace");
        Width = 760;
        Height = 420;
        StartPosition = FormStartPosition.CenterParent;

        var box = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            Font = new Font(FontFamily.GenericMonospace, 10),
            ReadOnly = true
        };

        if (trace is null)
        {
            box.Text = language.Text("trace.none", "No trace available.");
        }
        else
        {
            var lines = new List<string>
            {
                $"Start: {trace.StartValue}",
                $"Final: {trace.FinalValue}",
                ""
            };

            foreach (var step in trace.Steps)
            {
                lines.Add($"{step.InputValue} -> {step.Expression} -> {step.OutputValue}");
                lines.Add($"reverse: {step.AutoReverseExpression}");
                lines.Add("");
            }

            box.Text = string.Join(Environment.NewLine, lines);
        }

        Controls.Add(box);
    }
}
