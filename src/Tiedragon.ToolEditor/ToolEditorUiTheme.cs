#nullable enable
using System.Runtime.InteropServices;

namespace Tiedragon.ToolEditor;

public enum ToolEditorUiTheme
{
    Classic,
    Dark
}

public static class ToolEditorUiThemeSettings
{
    public const string SettingName = "uiTheme";
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeBefore20h1 = 19;

    public static event Action<ToolEditorUiTheme>? ThemeChanged;

    public static ToolEditorUiTheme Load(string? baseDirectory = null)
    {
        var value = LoadValue(baseDirectory ?? AppContext.BaseDirectory);
        return Parse(value);
    }

    public static void Save(ToolEditorUiTheme theme, string? baseDirectory = null)
    {
        var settingsPath = GetSettingsPath(baseDirectory ?? AppContext.BaseDirectory);
        var lines = File.Exists(settingsPath)
            ? File.ReadAllLines(settingsPath).ToList()
            : ["# Syscalculator UI settings."];

        var updated = false;
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index].Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal))
                continue;

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            if (!key.Equals(SettingName, StringComparison.OrdinalIgnoreCase))
                continue;

            lines[index] = SettingName + "=" + ToConfigValue(theme);
            updated = true;
            break;
        }

        if (!updated)
            lines.Add(SettingName + "=" + ToConfigValue(theme));

        File.WriteAllLines(settingsPath, lines);
        ThemeChanged?.Invoke(theme);
    }

    public static void ApplyApplicationColorMode(ToolEditorUiTheme theme)
    {
#pragma warning disable WFO5001
        Application.SetColorMode(theme == ToolEditorUiTheme.Dark
            ? SystemColorMode.Dark
            : SystemColorMode.Classic);
#pragma warning restore WFO5001
    }

    public static void ApplyNativeWindowTheme(Control control, ToolEditorUiTheme theme)
    {
        if (control.IsHandleCreated)
            ApplyNativeWindowTheme(control.Handle, theme);

        foreach (Control child in control.Controls)
            ApplyNativeWindowTheme(child, theme);
    }

    public static string ToConfigValue(ToolEditorUiTheme theme)
    {
        return theme == ToolEditorUiTheme.Dark ? "dark" : "classic";
    }

    private static ToolEditorUiTheme Parse(string value)
    {
        return value.Equals("classic", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("light", StringComparison.OrdinalIgnoreCase)
            ? ToolEditorUiTheme.Classic
            : ToolEditorUiTheme.Dark;
    }

    private static string LoadValue(string baseDirectory)
    {
        var settingsPath = GetSettingsPath(baseDirectory);
        if (!File.Exists(settingsPath))
            return "dark";

        foreach (var rawLine in File.ReadAllLines(settingsPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal))
                continue;

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            if (!key.Equals(SettingName, StringComparison.OrdinalIgnoreCase))
                continue;

            return line[(separator + 1)..].Trim();
        }

        return "dark";
    }

    private static string GetSettingsPath(string baseDirectory)
    {
        return Path.Combine(baseDirectory, "settings.cfg");
    }

    private static void ApplyNativeWindowTheme(IntPtr handle, ToolEditorUiTheme theme)
    {
        if (handle == IntPtr.Zero)
            return;

        var dark = theme == ToolEditorUiTheme.Dark ? 1 : 0;
        TrySetDwmDarkMode(handle, DwmwaUseImmersiveDarkMode, dark);
        TrySetDwmDarkMode(handle, DwmwaUseImmersiveDarkModeBefore20h1, dark);
        TrySetWindowTheme(handle, theme == ToolEditorUiTheme.Dark ? "DarkMode_Explorer" : null);
    }

    private static void TrySetDwmDarkMode(IntPtr handle, int attribute, int dark)
    {
        try
        {
            _ = DwmSetWindowAttribute(handle, attribute, ref dark, sizeof(int));
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    private static void TrySetWindowTheme(IntPtr handle, string? theme)
    {
        try
        {
            _ = SetWindowTheme(handle, theme, null);
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);
}
