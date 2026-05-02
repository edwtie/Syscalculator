namespace Syscalculator.UI.WinForms;

internal sealed class NodTemplateWizardForm : Form
{
    private readonly LanguageCatalog _language;
    private readonly ListBox _templateList = new();
    private readonly TextBox _preview = new();
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
        ShowIcon = false;
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
        _preview.Multiline = true;
        _preview.ReadOnly = true;
        _preview.ScrollBars = ScrollBars.Both;
        _preview.WordWrap = false;
        _preview.Font = new Font("Consolas", 10);
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

        var create = new Button
        {
            Text = T("editor.template_wizard.create", "Maken"),
            Width = 110,
            Height = 30
        };
        create.Click += (_, _) => AcceptSelection();

        var cancel = new Button
        {
            Text = T("dialog.cancel", "Annuleren"),
            Width = 110,
            Height = 30
        };
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
        _preview.Text = item?.Text ?? "";
    }

    private void AcceptSelection()
    {
        if (_templateList.SelectedItem is not NodTemplateItem item)
            return;

        SelectedTemplate = item;
        DialogResult = DialogResult.OK;
    }

    private string T(string key, string fallback) => _language.Text(key, fallback);
}
