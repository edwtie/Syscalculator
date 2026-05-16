#nullable enable

namespace Syscalculator.UI.WinForms;

internal static class AppWindowIcon
{
    private static readonly Lazy<Icon?> SharedIcon = new(LoadIcon);

    public static void ApplyTo(Form form)
    {
        var icon = SharedIcon.Value;
        if (icon is null)
            return;

        form.ShowIcon = true;
        form.Icon = (Icon)icon.Clone();
    }

    public static Icon CreateTrayIcon()
    {
        var icon = SharedIcon.Value;
        return icon is null
            ? (Icon)SystemIcons.Application.Clone()
            : (Icon)icon.Clone();
    }

    private static Icon? LoadIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "Syscalculator.ico");
        return File.Exists(path) ? new Icon(path) : null;
    }
}
