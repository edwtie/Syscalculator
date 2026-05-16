using System.Text.RegularExpressions;
using Tiedragon.NodSystem.Core;

namespace Syscalculator.UI.WinForms;

internal sealed class NodTemplateWizardForm : Form
{
    private readonly LanguageCatalog _language;
    private readonly ListBox _templateList = new();
    private readonly RichTextBox _preview = new();
    private readonly Label _description = new();

    public NodTemplateItem? SelectedTemplate { get; private set; }

    public NodTemplateWizardForm(LanguageCatalog language)
    {
        _language = language;

        Text = T("editor.template_wizard.title", "Template wizard");
        Width = 820;
        Height = 560;
        MinimizeBox = false;
        MaximizeBox = false;
        AppWindowIcon.ApplyTo(this);
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();
        LoadTemplates();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(12),
            BackColor = Color.White
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        _templateList.Dock = DockStyle.Fill;
        _templateList.IntegralHeight = false;
        _templateList.Font = new Font("Segoe UI", 10);
        _templateList.SelectedIndexChanged += (_, _) => UpdateSelection();
        _templateList.DoubleClick += (_, _) => AcceptSelection();
        root.Controls.Add(_templateList, 0, 0);

        _preview.Dock = DockStyle.Fill;
        _preview.ReadOnly = true;
        _preview.ScrollBars = RichTextBoxScrollBars.Both;
        _preview.WordWrap = false;
        _preview.Font = new Font("Consolas", 10);
        _preview.BackColor = Color.FromArgb(15, 23, 42);
        _preview.ForeColor = Color.FromArgb(226, 232, 240);
        _preview.BorderStyle = BorderStyle.FixedSingle;
        _preview.DetectUrls = false;
        root.Controls.Add(_preview, 1, 0);

        _description.Dock = DockStyle.Fill;
        _description.TextAlign = ContentAlignment.MiddleLeft;
        _description.AutoEllipsis = true;
        root.SetColumnSpan(_description, 2);
        root.Controls.Add(_description, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };

        var create = CreateDialogButton(T("editor.template_wizard.create", "Maken"), primary: true);
        create.Click += (_, _) => AcceptSelection();

        var cancel = CreateDialogButton(T("dialog.cancel", "Annuleren"), primary: false);
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;

        buttons.Controls.Add(create);
        buttons.Controls.Add(cancel);
        root.SetColumnSpan(buttons, 2);
        root.Controls.Add(buttons, 0, 2);

        Controls.Add(root);
        AcceptButton = create;
        CancelButton = cancel;
    }

    private void LoadTemplates()
    {
        foreach (var item in NodTemplateCatalog.Create(_language))
            _templateList.Items.Add(item);

        _templateList.DisplayMember = nameof(NodTemplateItem.Title);
        if (_templateList.Items.Count > 0)
            _templateList.SelectedIndex = 0;
    }

    private void UpdateSelection()
    {
        var item = _templateList.SelectedItem as NodTemplateItem;
        _description.Text = item?.Description ?? "";
        SetPreviewText(NormalizeTemplateText(item?.Text ?? ""));
    }

    private void AcceptSelection()
    {
        if (_templateList.SelectedItem is not NodTemplateItem item)
            return;

        SelectedTemplate = item with { Text = NormalizeTemplateText(item.Text) };
        DialogResult = DialogResult.OK;
    }

    private static string NormalizeTemplateText(string text)
    {
        return NodTextNormalizer.NormalizeForEditor(text, repairConcatenated: true);
    }

    private static Button CreateDialogButton(string text, bool primary)
    {
        var button = new Button
        {
            Text = text,
            Width = 110,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            BackColor = primary ? Color.FromArgb(37, 99, 235) : Color.FromArgb(239, 246, 255),
            ForeColor = primary ? Color.White : Color.FromArgb(15, 63, 143),
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Regular)
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(37, 99, 235);
        button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(29, 78, 216) : Color.FromArgb(219, 234, 254);
        button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(30, 64, 175) : Color.FromArgb(191, 219, 254);
        return button;
    }

    private void SetPreviewText(string text)
    {
        _preview.Text = text;
        HighlightPreview();
        _preview.Select(0, 0);
    }

    private void HighlightPreview()
    {
        var text = _preview.Text;
        if (string.IsNullOrEmpty(text))
            return;

        _preview.SelectAll();
        _preview.SelectionColor = Color.FromArgb(226, 232, 240);

        ApplyHighlight(@"(?im)^\s*(Name|URLN|input1|input2|inputr|Result|Resfou|Symb1|Symb2|Symb3|Symb4|format|mode|math|chg|trans|reverse|field|table|output|phoneformat|lookup|match|given|equation|solve|constraint|preview|backup|indoprint|indoend|end)\b", Color.FromArgb(96, 165, 250), group: 1);
        ApplyHighlight(@"[\*\^=,+/<>-]", Color.FromArgb(244, 114, 182));
        ApplyHighlight(@"(?<![\w.])-?\d+(?:[.,]\d+)?(?![\w.])", Color.FromArgb(251, 191, 36));
        ApplyHighlight("\"[^\"]*\"", Color.FromArgb(134, 239, 172));
    }

    private void ApplyHighlight(string pattern, Color color, int group = 0)
    {
        foreach (Match match in Regex.Matches(_preview.Text, pattern))
        {
            var capture = match.Groups[group];
            if (!capture.Success || capture.Length == 0)
                continue;

            _preview.Select(capture.Index, capture.Length);
            _preview.SelectionColor = color;
        }
    }

    private string T(string key, string fallback) => _language.Text(key, fallback);
}
