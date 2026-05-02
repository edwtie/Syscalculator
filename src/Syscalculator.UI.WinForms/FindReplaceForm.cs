namespace Syscalculator.UI.WinForms;

/// <summary>
/// Eenvoudig zoek/vervang-venster voor NOD Editor.
/// Werkt op de actieve RichTextBox.
/// </summary>
public sealed class FindReplaceForm : Form
{
    private readonly Func<RichTextBox?> _getEditor;
    private readonly LanguageCatalog _language;

    private TextBox _findBox = null!;
    private TextBox _replaceBox = null!;
    private CheckBox _matchCase = null!;

    // Zoek/commentaar: Constructor: maakt en initialiseert FindReplaceForm.
    public FindReplaceForm(Func<RichTextBox?> getEditor)
    {
        _getEditor = getEditor;
        _language = LanguageCatalog.LoadConfigured(AppContext.BaseDirectory);

        Text = T("find.title", "Find / replace");
        Width = 520;
        Height = 190;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();
    }

    // Zoek/commentaar: Bouwt de UI of data-opbouw voor BuildLayout.
    private void BuildLayout()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 4,
            Padding = new Padding(12)
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        panel.Controls.Add(new Label { Text = T("find.find", "Find"), AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        _findBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        panel.Controls.Add(_findBox, 1, 0);

        var findNext = new Button { Text = T("find.next", "Next") };
        findNext.Click += (_, _) => FindNext();
        panel.Controls.Add(findNext, 2, 0);

        panel.Controls.Add(new Label { Text = T("find.replace", "Replace"), AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        _replaceBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
        panel.Controls.Add(_replaceBox, 1, 1);

        var replace = new Button { Text = T("find.replace_button", "Replace") };
        replace.Click += (_, _) => ReplaceCurrent();
        panel.Controls.Add(replace, 2, 1);

        _matchCase = new CheckBox { Text = T("find.match_case", "Match case"), AutoSize = true };
        panel.SetColumnSpan(_matchCase, 2);
        panel.Controls.Add(_matchCase, 1, 2);

        var replaceAll = new Button { Text = T("find.replace_all", "Replace all"), Anchor = AnchorStyles.Left | AnchorStyles.Right };
        replaceAll.Click += (_, _) => ReplaceAll();
        panel.Controls.Add(replaceAll, 1, 3);

        var close = new Button { Text = T("find.close", "Close") };
        close.Click += (_, _) => Close();
        panel.Controls.Add(close, 2, 3);

        Controls.Add(panel);
    }

    // Zoek/commentaar: Methode FindNext: centrale logica voor deze stap.
    private void FindNext()
    {
        var editor = _getEditor();
        if (editor is null || string.IsNullOrEmpty(_findBox.Text))
            return;

        var comparison = _matchCase.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var start = editor.SelectionStart + editor.SelectionLength;
        var index = editor.Text.IndexOf(_findBox.Text, start, comparison);

        if (index < 0 && start > 0)
            index = editor.Text.IndexOf(_findBox.Text, 0, comparison);

        if (index < 0)
        {
            MessageBox.Show(this, T("find.not_found", "Not found."), T("find.find", "Find"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        editor.Focus();
        editor.Select(index, _findBox.Text.Length);
        editor.ScrollToCaret();
    }

    // Zoek/commentaar: Methode ReplaceCurrent: centrale logica voor deze stap.
    private void ReplaceCurrent()
    {
        var editor = _getEditor();
        if (editor is null || string.IsNullOrEmpty(_findBox.Text))
            return;

        var selected = editor.SelectedText;
        var equal = _matchCase.Checked
            ? selected == _findBox.Text
            : string.Equals(selected, _findBox.Text, StringComparison.OrdinalIgnoreCase);

        if (equal)
            editor.SelectedText = _replaceBox.Text;

        FindNext();
    }

    // Zoek/commentaar: Methode ReplaceAll: centrale logica voor deze stap.
    private void ReplaceAll()
    {
        var editor = _getEditor();
        if (editor is null || string.IsNullOrEmpty(_findBox.Text))
            return;

        var comparison = _matchCase.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var text = editor.Text;
        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(_findBox.Text, index, comparison)) >= 0)
        {
            text = text.Remove(index, _findBox.Text.Length).Insert(index, _replaceBox.Text);
            index += _replaceBox.Text.Length;
            count++;
        }

        editor.Text = text;
        MessageBox.Show(this, string.Format(T("find.replaced_count", "{0} replacements made."), count), T("find.replace", "Replace"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // Zoek/commentaar: Methode T: centrale logica voor deze stap.
    private string T(string key, string fallback) => _language.Text(key, fallback);
}
