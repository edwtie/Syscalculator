namespace Syscalculator.UI.WinForms;

/// <summary>
/// Moderne vervanger voor oude Form3/Form4 converterbeheer.
/// </summary>
public sealed class CatalogManagerForm : Form
{
    private readonly NodCatalogService _service;
    private readonly LanguageCatalog _language;
    private readonly BindingSource _binding = new();
    private DataGridView _grid = null!;

    // Zoek/commentaar: Constructor: maakt en initialiseert CatalogManagerForm.
    public CatalogManagerForm(NodCatalogService service)
    {
        _service = service;
        _language = LanguageCatalog.LoadConfigured(AppContext.BaseDirectory);

        Text = T("catalog.title", "Converter catalog");
        Width = 760;
        Height = 460;
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();
        LoadData();
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildLayout.
    private void BuildLayout()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            Padding = new Padding(12)
        };

        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            DataSource = _binding
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(NodCatalogItem.DisplayName),
            HeaderText = T("catalog.column.name", "Naam"),
            FillWeight = 50,
            MinimumWidth = 260
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(NodCatalogItem.NodPath),
            HeaderText = T("catalog.column.nod_file", "NOD-bestand"),
            FillWeight = 40,
            MinimumWidth = 240
        });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(NodCatalogItem.IsDefault),
            HeaderText = T("catalog.column.default", "Standaard"),
            FillWeight = 10,
            MinimumWidth = 90
        });

        panel.Controls.Add(_grid, 0, 0);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill };

        var add = new Button { Text = T("catalog.add", "Add") };
        add.Click += (_, _) =>
        {
            var items = GetItems();
            items.Add(new NodCatalogItem { DisplayName = T("catalog.new_converter", "New converter"), NodPath = "Converters/new.nod" });
            _binding.DataSource = items;
        };

        var choose = new Button { Text = T("catalog.choose_nod", "Choose NOD...") };
        choose.Click += ChooseNod_Click;

        var save = new Button { Text = T("catalog.save", "Save") };
        save.Click += (_, _) =>
        {
            _service.Save(GetItems());
            Close();
        };

        buttons.Controls.Add(add);
        buttons.Controls.Add(choose);
        buttons.Controls.Add(save);

        panel.Controls.Add(buttons, 0, 1);
        Controls.Add(panel);
    }

    // Zoek/commentaar: Laadt gegevens of instellingen voor LoadData.
    private void LoadData()
    {
        _binding.DataSource = _service.Load();
    }

    // Zoek/commentaar: Haalt een waarde, tekst of instelling op voor GetItems.
    private List<NodCatalogItem> GetItems()
    {
        return _binding.DataSource as List<NodCatalogItem> ?? new List<NodCatalogItem>();
    }

    // Zoek/commentaar: Methode ChooseNod_Click: centrale logica voor deze stap.
    private void ChooseNod_Click(object? sender, EventArgs e)
    {
        if (_binding.Current is not NodCatalogItem item)
            return;

        using var dlg = new OpenFileDialog { Filter = T("dialog.open_nod.filter", "NOD files (*.nod)|*.nod|All files (*.*)|*.*") };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        item.NodPath = dlg.FileName;
        item.DisplayName = Path.GetFileNameWithoutExtension(dlg.FileName);
        _grid.Refresh();
    }

    // Zoek/commentaar: Methode T: centrale logica voor deze stap.
    private string T(string key, string fallback) => _language.Text(key, fallback);
}
