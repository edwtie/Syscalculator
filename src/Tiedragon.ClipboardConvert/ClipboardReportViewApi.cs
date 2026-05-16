#nullable enable

using System.Globalization;

namespace Tiedragon.ClipboardConvert;

/// <summary>
/// Own WinForms report UI helpers for clipboard diagnostics.
/// </summary>
public static class ClipboardReportViewApi
{
    public static ClipboardReportView CreateReportView(
        Func<string, string, string>? text = null,
        Func<Control>? mathViewFactory = null,
        Action<Control, string>? setMathMarkup = null)
    {
        return new ClipboardReportView(text, mathViewFactory, setMathMarkup)
        {
            Dock = DockStyle.Fill
        };
    }

    public static void FillReportView(
        ClipboardReportView view,
        ClipboardDebugReport report,
        string previewText,
        string beforeText = "",
        string afterText = "",
        string usedRuleText = "",
        string usedRuleMathMl = "")
    {
        view.SetReport(report, previewText, beforeText, afterText, usedRuleText, usedRuleMathMl);
    }
}

public sealed class ClipboardReportView : UserControl
{
    private const int ReportPreviewRowLimit = 5;

    private readonly Func<string, string, string> _text;
    private readonly Func<Control>? _mathViewFactory;
    private readonly Action<Control, string>? _setMathMarkup;
    private readonly SmoothFlowLayoutPanel _content;
    private readonly List<Panel> _blocks = new();
    private readonly Panel _formatsBody;
    private readonly Label _previewNote;
    private readonly DataGridView _previewGrid;
    private readonly Label _ownerValue;
    private readonly Label _textLengthValue;
    private readonly Label _lineCountValue;
    private readonly Label _columnCountValue;
    private readonly Label _sameAsLastSeenValue;
    private readonly Label _sameAsConvertedValue;
    private readonly Panel _formulaCard;
    private readonly Label _formulaValue;
    private readonly Control _mathView;
    private readonly Panel _beforeAfterCard;
    private readonly DataGridView _beforeGrid;
    private readonly DataGridView _afterGrid;
    private string[] _currentFormats = Array.Empty<string>();
    private string? _currentPreviewText;
    private string _currentBeforeText = "";
    private string _currentAfterText = "";

    public ClipboardReportView()
        : this(null)
    {
    }

    public ClipboardReportView(Func<string, string, string>? text)
        : this(text, null, null)
    {
    }

    public ClipboardReportView(
        Func<string, string, string>? text,
        Func<Control>? mathViewFactory,
        Action<Control, string>? setMathMarkup)
    {
        _text = text ?? ((_, fallback) => fallback);
        _mathViewFactory = mathViewFactory;
        _setMathMarkup = setMathMarkup;

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.FromArgb(247, 249, 252);

        _content = new SmoothFlowLayoutPanel
        {
            AutoScroll = true,
            BackColor = BackColor,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(12),
            WrapContents = false
        };

        Controls.Add(_content);

        _content.Controls.Add(CreateHeaderBlock());

        var summaryBody = CreateSummaryBody(
            out _ownerValue,
            out _textLengthValue,
            out _lineCountValue,
            out _columnCountValue,
            out _sameAsLastSeenValue,
            out _sameAsConvertedValue);
        _content.Controls.Add(CreateCard(T("clipboard.debug.summary", "Summary"), summaryBody, 182));

        var formulaBody = CreateFormulaBody(out _formulaValue, out _mathView);
        _formulaCard = CreateCard(T("clipboard.debug.formula", "Formula"), formulaBody, 126);
        _content.Controls.Add(_formulaCard);

        var beforeAfterBody = CreateBeforeAfterBody(out _beforeGrid, out _afterGrid);
        _beforeAfterCard = CreateCard(T("clipboard.debug.before_after", "Before / After"), beforeAfterBody, 172);
        _content.Controls.Add(_beforeAfterCard);

        _formatsBody = new Panel
        {
            BackColor = Color.White,
            Location = new Point(12, 44),
            Height = 82
        };
        _content.Controls.Add(CreateCard(T("clipboard.debug.formats", "Formats"), _formatsBody, 132));

        var previewBody = CreatePreviewBody(out _previewNote, out _previewGrid);
        _content.Controls.Add(CreateCard(T("clipboard.debug.preview", "Preview"), previewBody, 178));

        Resize += (_, _) => ApplyWidths();
        ApplyWidths();
        UpdateFormats(Array.Empty<string>());
    }

    public void SetReport(
        ClipboardDebugReport report,
        string previewText,
        string beforeText = "",
        string afterText = "",
        string usedRuleText = "",
        string usedRuleMathMl = "")
    {
        SuspendLayout();
        try
        {
            SetTextIfChanged(_ownerValue, FormatOwner(report.Owner));
            SetTextIfChanged(_textLengthValue, string.Format(CultureInfo.CurrentCulture, T("clipboard.debug.text_length_value", "{0} chars"), report.TextLength));
            SetTextIfChanged(_lineCountValue, report.LineCount.ToString(CultureInfo.CurrentCulture));
            SetTextIfChanged(_columnCountValue, report.ColumnCount.ToString(CultureInfo.CurrentCulture));
            SetTextIfChanged(_sameAsLastSeenValue, FormatBool(report.SameAsLastSeen));
            SetTextIfChanged(_sameAsConvertedValue, FormatBool(report.SameAsLastConverted));
            UpdateFormula(usedRuleText, usedRuleMathMl);
            UpdateBeforeAfter(beforeText, afterText);
            UpdatePreview(previewText);
            UpdateFormats(report.Formats);
        }
        finally
        {
            ResumeLayout();
        }
    }

    private Panel CreateHeaderBlock()
    {
        var panel = CreateBlockPanel(70, Color.Transparent, BorderStyle.None);
        panel.Margin = new Padding(0, 0, 0, 2);

        panel.Controls.Add(new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39),
            Location = new Point(0, 6),
            Size = new Size(420, 30),
            Text = T("clipboard.debug.report_title", "Clipboard report")
        });

        panel.Controls.Add(new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 8.7f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(0, 38),
            Size = new Size(420, 22),
            Text = T("clipboard.debug.report_subtitle", "Clean overview of the current clipboard content.")
        });

        return panel;
    }

    private TableLayoutPanel CreateSummaryBody(
        out Label ownerValue,
        out Label textLengthValue,
        out Label lineCountValue,
        out Label columnCountValue,
        out Label sameAsLastSeenValue,
        out Label sameAsConvertedValue)
    {
        var table = new TableLayoutPanel
        {
            BackColor = Color.White,
            ColumnCount = 2,
            Location = new Point(12, 44),
            RowCount = 0
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        ownerValue = AddMetric(table, T("clipboard.debug.owner", "Owner"));
        textLengthValue = AddMetric(table, T("clipboard.debug.text_length", "Text length"));
        lineCountValue = AddMetric(table, T("clipboard.debug.rows", "Rows"));
        columnCountValue = AddMetric(table, T("clipboard.debug.columns", "Columns"));
        sameAsLastSeenValue = AddMetric(table, T("clipboard.debug.same_last_seen", "Same as last seen"));
        sameAsConvertedValue = AddMetric(table, T("clipboard.debug.same_converted", "Same as conversion"));
        return table;
    }

    private Control CreateFormulaBody(out Label formulaValue, out Control mathView)
    {
        var panel = new Panel
        {
            BackColor = Color.White,
            Location = new Point(12, 44),
            Height = 82
        };

        formulaValue = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39),
            Location = new Point(0, 0),
            Height = 38,
            TextAlign = ContentAlignment.MiddleCenter
        };
        mathView = _mathViewFactory?.Invoke() ?? new Label
        {
            BackColor = Color.White,
            Font = new Font("Segoe UI", 8f),
            ForeColor = Color.FromArgb(75, 85, 99),
            TextAlign = ContentAlignment.MiddleLeft
        };
        mathView.Location = new Point(0, 32);
        mathView.Height = 44;
        mathView.BackColor = Color.White;

        panel.Controls.Add(formulaValue);
        panel.Controls.Add(mathView);
        return panel;
    }

    private Control CreateBeforeAfterBody(out DataGridView beforeGrid, out DataGridView afterGrid)
    {
        var panel = new TableLayoutPanel
        {
            BackColor = Color.White,
            ColumnCount = 2,
            Location = new Point(12, 44),
            Height = 114
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var beforePanel = CreateTitledGridPanel(T("clipboard.debug.before", "Before"), out beforeGrid);
        var afterPanel = CreateTitledGridPanel(T("clipboard.debug.after", "After"), out afterGrid);
        panel.Controls.Add(beforePanel, 0, 0);
        panel.Controls.Add(afterPanel, 1, 0);
        return panel;
    }

    private Control CreatePreviewBody(out Label previewNote, out DataGridView previewGrid)
    {
        var panel = new Panel
        {
            BackColor = Color.White,
            Location = new Point(12, 44),
            Height = 122
        };

        previewNote = new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 8.4f),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(0, 0),
            Height = 18,
            TextAlign = ContentAlignment.MiddleLeft
        };

        previewGrid = ClipboardGridApi.CreateExcelLikeGrid();
        previewGrid.Location = new Point(0, 22);
        previewGrid.Dock = DockStyle.None;

        panel.Controls.Add(previewNote);
        panel.Controls.Add(previewGrid);
        return panel;
    }

    private static Panel CreateTitledGridPanel(string title, out DataGridView grid)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 10, 0) };
        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            Height = 18,
            Font = new Font("Segoe UI", 8.7f, FontStyle.Bold),
            ForeColor = Color.FromArgb(75, 85, 99),
            Text = title
        });
        grid = ClipboardGridApi.CreateExcelLikeGrid();
        grid.Dock = DockStyle.Fill;
        grid.RowHeadersVisible = false;
        grid.ColumnHeadersHeight = 22;
        grid.ScrollBars = ScrollBars.None;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.DefaultCellStyle.SelectionBackColor = Color.White;
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(17, 24, 39);
        grid.ClearSelection();
        panel.Controls.Add(grid);
        return panel;
    }

    private Panel CreateCard(string title, Control body, int height)
    {
        var card = CreateBlockPanel(height, Color.White, BorderStyle.FixedSingle);
        card.Margin = new Padding(0, 0, 0, 10);

        card.Controls.Add(new Label
        {
            AutoSize = false,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 63, 143),
            Location = new Point(12, 12),
            Size = new Size(420, 24),
            Text = title
        });
        card.Controls.Add(body);
        return card;
    }

    private Panel CreateBlockPanel(int height, Color backColor, BorderStyle borderStyle)
    {
        var panel = new Panel
        {
            BackColor = backColor,
            BorderStyle = borderStyle,
            Height = height,
            Padding = new Padding(12),
            Width = ContentWidth
        };
        _blocks.Add(panel);
        return panel;
    }

    private static Label AddMetric(TableLayoutPanel table, string label)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));

        table.Controls.Add(new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.7f, FontStyle.Bold),
            ForeColor = Color.FromArgb(75, 85, 99),
            Text = label,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);

        var valueLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.7f),
            ForeColor = Color.FromArgb(17, 24, 39),
            TextAlign = ContentAlignment.MiddleLeft
        };
        table.Controls.Add(valueLabel, 1, row);
        return valueLabel;
    }

    private void UpdateFormats(string[] formats)
    {
        if (_currentFormats.SequenceEqual(formats))
            return;

        _currentFormats = formats.ToArray();
        _formatsBody.SuspendLayout();
        try
        {
            _formatsBody.Controls.Clear();
            if (_currentFormats.Length == 0)
            {
                _formatsBody.Controls.Add(CreateLineLabel(T("clipboard.debug.no_formats", "No clipboard formats found."), true));
                return;
            }

            foreach (var format in _currentFormats.Reverse())
                _formatsBody.Controls.Add(CreateLineLabel("- " + format, false));
        }
        finally
        {
            _formatsBody.ResumeLayout();
        }
    }

    private void UpdatePreview(string previewText)
    {
        if (string.Equals(_currentPreviewText, previewText, StringComparison.Ordinal))
            return;

        _currentPreviewText = previewText;
        var rowCount = ClipboardGridApi.ParseTable(previewText).Count;
        SetTextIfChanged(_previewNote, FormatPreviewNote(rowCount));
        ClipboardGridApi.FillExcelLikeGrid(_previewGrid, previewText, minimumRows: 1, minimumColumns: 1, maximumRows: ReportPreviewRowLimit);
    }

    private void UpdateFormula(string usedRuleText, string usedRuleMathMl)
    {
        var visible = !string.IsNullOrWhiteSpace(usedRuleText) || !string.IsNullOrWhiteSpace(usedRuleMathMl);
        _formulaCard.Visible = visible;
        if (!visible)
            return;

        var useMathPreview = _setMathMarkup is not null && !string.IsNullOrWhiteSpace(usedRuleMathMl);
        _formulaValue.Visible = !useMathPreview;
        _mathView.Location = useMathPreview ? new Point(0, 0) : new Point(0, 32);
        _mathView.Height = useMathPreview ? 76 : 44;
        SetTextIfChanged(_formulaValue, PrettyFormulaText(usedRuleText));
        if (_setMathMarkup is not null)
        {
            _setMathMarkup(_mathView, usedRuleMathMl);
        }
        else
        {
            SetTextIfChanged(
                _mathView,
                string.IsNullOrWhiteSpace(usedRuleMathMl)
                    ? ""
                    : T("clipboard.debug.mathml_available", "MathML available in copied/e-mailed report."));
        }
    }

    private void UpdateBeforeAfter(string beforeText, string afterText)
    {
        var visible = !string.IsNullOrEmpty(beforeText) || !string.IsNullOrEmpty(afterText);
        _beforeAfterCard.Visible = visible;
        if (!visible)
            return;

        if (!string.Equals(_currentBeforeText, beforeText, StringComparison.Ordinal))
        {
            _currentBeforeText = beforeText;
            ClipboardGridApi.FillExcelLikeGrid(_beforeGrid, beforeText, minimumRows: 1, minimumColumns: 1, maximumRows: ReportPreviewRowLimit);
            _beforeGrid.ClearSelection();
        }

        if (!string.Equals(_currentAfterText, afterText, StringComparison.Ordinal))
        {
            _currentAfterText = afterText;
            ClipboardGridApi.FillExcelLikeGrid(_afterGrid, afterText, minimumRows: 1, minimumColumns: 1, maximumRows: ReportPreviewRowLimit);
            _afterGrid.ClearSelection();
        }
    }

    private static Label CreateLineLabel(string text, bool muted)
    {
        return new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 8.8f),
            ForeColor = muted ? Color.FromArgb(100, 116, 139) : Color.FromArgb(31, 41, 55),
            Height = 24,
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private void ApplyWidths()
    {
        var contentWidth = ContentWidth;
        foreach (var block in _blocks)
        {
            block.Width = contentWidth;
            foreach (Control child in block.Controls)
            {
                child.Width = Math.Max(120, block.ClientSize.Width - 24);
                if (child is TableLayoutPanel table)
                {
                    table.Height = 140;
                    table.Width = Math.Max(120, block.ClientSize.Width - 24);
                }
                else if (child is Panel panel)
                {
                    panel.Height = Math.Max(24, block.ClientSize.Height - panel.Top - 12);
                    panel.Width = Math.Max(120, block.ClientSize.Width - 24);
                    ResizePanelChildren(panel);
                }
                else if (child is DataGridView grid)
                {
                    grid.Height = Math.Max(24, block.ClientSize.Height - grid.Top - 12);
                    grid.Width = Math.Max(120, block.ClientSize.Width - 24);
                }
            }
        }
    }

    private void ResizePanelChildren(Control panel)
    {
        foreach (Control child in panel.Controls)
        {
            if (child is Label label)
                label.Width = Math.Max(120, panel.ClientSize.Width - label.Left);
            else if (child == _mathView)
                child.Width = Math.Max(120, panel.ClientSize.Width - child.Left);
            else if (child is TableLayoutPanel table)
                table.Width = Math.Max(120, panel.ClientSize.Width - table.Left);
            else if (child is DataGridView grid)
            {
                grid.Height = Math.Max(24, panel.ClientSize.Height - grid.Top);
                grid.Width = Math.Max(120, panel.ClientSize.Width - grid.Left);
            }
        }
    }

    private int ContentWidth => Math.Max(300, ClientSize.Width - _content.Padding.Horizontal - 28);

    private static void SetTextIfChanged(Control control, string text)
    {
        if (!string.Equals(control.Text, text, StringComparison.Ordinal))
            control.Text = text;
    }

    private string T(string key, string fallback) => _text(key, fallback);

    private string FormatPreviewNote(int rowCount)
    {
        if (rowCount <= ReportPreviewRowLimit)
            return string.Format(CultureInfo.CurrentCulture, T("clipboard.debug.preview_rows_all", "{0} rows shown"), rowCount);

        return string.Format(
            CultureInfo.CurrentCulture,
            T("clipboard.debug.preview_rows_limited", "First {0} of {1} rows shown"),
            ReportPreviewRowLimit,
            rowCount);
    }

    private string FormatBool(bool value) => value
        ? T("common.yes", "Yes")
        : T("common.no", "No");

    private string FormatOwner(string owner)
    {
        return string.Equals(owner, "none", StringComparison.OrdinalIgnoreCase)
            ? T("clipboard.debug.none", "None")
            : owner;
    }

    private static string PrettyFormulaText(string value)
    {
        return value
            .Replace("*", " * ", StringComparison.Ordinal)
            .Replace("/", " / ", StringComparison.Ordinal)
            .Replace("  ", " ", StringComparison.Ordinal)
            .Trim();
    }

    private sealed class SmoothFlowLayoutPanel : FlowLayoutPanel
    {
        public SmoothFlowLayoutPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }
}
