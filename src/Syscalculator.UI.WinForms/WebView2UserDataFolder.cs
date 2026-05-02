#nullable enable
namespace Syscalculator.UI.WinForms;

// Zoek/commentaar: Houdt WebView2 data uit diepe buildmappen; Edge faalt soms op lange paden.
internal static class WebView2UserDataFolder
{
    public static string GetPath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var path = Path.Combine(root, "Syscalculator", "WebView2");
        Directory.CreateDirectory(path);
        return path;
    }
}
