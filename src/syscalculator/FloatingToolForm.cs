namespace Syscalculator.UI.WinForms;

/// <summary>
/// Floating tool window voor detachable editorpanelen.
/// 
/// De content wordt niet vernietigd bij sluiten:
/// de eigenaar kan het paneel terugdocken of verbergen.
/// </summary>
public sealed class FloatingToolForm : Form
{
    private readonly Control _content;
    private readonly Action _onClose;
    private readonly Action? _onMinimize;
    private bool _closingFromMinimize;

    // Zoek/commentaar: Constructor: maakt en initialiseert FloatingToolForm.
    public FloatingToolForm(string title, Control content, Action onClose, Action? onMinimize = null)
    {
        _content = content;
        _onClose = onClose;
        _onMinimize = onMinimize;

        Text = title;
        Width = 520;
        Height = 360;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        MinimizeBox = true;
        MaximizeBox = false;

        content.Dock = DockStyle.Fill;
        Controls.Add(content);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (WindowState != FormWindowState.Minimized || _onMinimize is null)
            return;

        _closingFromMinimize = true;
        Controls.Remove(_content);
        _onMinimize();
        Close();
    }

    // Zoek/commentaar: Methode OnFormClosing: centrale logica voor deze stap.
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_closingFromMinimize)
        {
            Controls.Remove(_content);
            _onClose();
        }

        base.OnFormClosing(e);
    }
}
