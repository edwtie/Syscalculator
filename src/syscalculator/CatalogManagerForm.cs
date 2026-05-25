using Tiedragon.ToolEditor;

namespace Syscalculator.UI.WinForms;

/// <summary>
/// Moderne vervanger voor oude Form3/Form4 converterbeheer.
/// </summary>
public sealed class CatalogManagerForm : Form
{
    private readonly NodCatalogService _service;
    private readonly LanguageCatalog _language;
    private readonly ToolEditorUiTheme _uiTheme;
    private readonly BindingSource _binding = new();
    private DataGridView _grid = null!;

    private bool IsDarkTheme => _uiTheme == ToolEditorUiTheme.Dark;
    private Color WindowBackColor => IsDarkTheme ? Color.FromArgb(32, 32, 32) : SystemColors.Control;
    private Color PanelBackColor => IsDarkTheme ? Color.FromArgb(32, 32, 32) : SystemColors.Control;
    private Color TextColor => IsDarkTheme ? Color.White : SystemColors.ControlText;
    private Color MutedTextColor => IsDarkTheme ? Color.FromArgb(210, 220, 236) : SystemColors.ControlText;
    private Color BorderColor => IsDarkTheme ? Color.FromArgb(75, 85, 99) : SystemColors.ControlDark;
    private Color GridBackColor => IsDarkTheme ? Color.FromArgb(38, 38, 38) : SystemColors.Window;
    private Color GridAlternateBackColor => IsDarkTheme ? Color.FromArgb(45, 45, 45) : Color.FromArgb(248, 250, 252);
    private Color GridHeaderBackColor => IsDarkTheme ? Color.FromArgb(48, 48, 48) : SystemColors.Control;
    private Color GridSelectionBackColor => IsDarkTheme ? Color.FromArgb(37, 99, 235) : SystemColors.Highlight;
    private Color ButtonBackColor => IsDarkTheme ? Color.FromArgb(45, 45, 45) : SystemColors.Control;
    private Color ButtonHoverBackColor => IsDarkTheme ? Color.FromArgb(55, 65, 81) : SystemColors.ControlLight;
    private Color ButtonDownBackColor => IsDarkTheme ? Color.FromArgb(31, 41, 55) : SystemColors.ControlDark;

    // Zoek/commentaar: Constructor: maakt en initialiseert CatalogManagerForm.
    public CatalogManagerForm(NodCatalogService service)
    {
        _service = service;
        _language = LanguageCatalog.LoadConfigured(AppContext.BaseDirectory);
        _uiTheme = ToolEditorUiThemeSettings.Load();

        Text = T("catalog.title", "Converter catalog");
        AppWindowIcon.ApplyTo(this);
        BackColor = WindowBackColor;
        ForeColor = TextColor;
        Width = 760;
        Height = 460;
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();
        LoadData();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ToolEditorUiThemeSettings.ApplyNativeWindowTheme(this, _uiTheme);
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildLayout.
    private void BuildLayout()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = PanelBackColor,
            ForeColor = TextColor
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
        ApplyGridTheme(_grid);
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

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelBackColor,
            ForeColor = TextColor
        };

        var add = CreateCatalogButton(T("catalog.add", "Add"));
        add.Click += (_, _) =>
        {
            var items = GetItems();
            items.Add(new NodCatalogItem { DisplayName = T("catalog.new_converter", "New converter"), NodPath = "Converters/new.nod" });
            _binding.DataSource = items;
        };

        var choose = CreateCatalogButton(T("catalog.choose_nod", "Choose NOD..."));
        choose.Click += ChooseNod_Click;

        var save = CreateCatalogButton(T("catalog.save", "Save"));
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

    private void ApplyGridTheme(DataGridView grid)
    {
        grid.BackgroundColor = PanelBackColor;
        grid.BackColor = GridBackColor;
        grid.ForeColor = TextColor;
        grid.GridColor = BorderColor;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.EnableHeadersVisualStyles = false;
        grid.RowHeadersVisible = false;

        grid.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderBackColor;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextColor;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = GridHeaderBackColor;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextColor;

        grid.DefaultCellStyle.BackColor = GridBackColor;
        grid.DefaultCellStyle.ForeColor = MutedTextColor;
        grid.DefaultCellStyle.SelectionBackColor = GridSelectionBackColor;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;

        grid.AlternatingRowsDefaultCellStyle.BackColor = GridAlternateBackColor;
        grid.AlternatingRowsDefaultCellStyle.ForeColor = MutedTextColor;
        grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = GridSelectionBackColor;
        grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;
    }

    private Button CreateCatalogButton(string text)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(92, 27),
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            BackColor = ButtonBackColor,
            ForeColor = TextColor
        };

        button.FlatAppearance.BorderColor = BorderColor;
        button.FlatAppearance.MouseOverBackColor = ButtonHoverBackColor;
        button.FlatAppearance.MouseDownBackColor = ButtonDownBackColor;
        return button;
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
