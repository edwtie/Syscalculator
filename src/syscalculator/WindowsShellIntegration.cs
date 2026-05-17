using System.Runtime.InteropServices;

namespace Syscalculator.UI.WinForms;

internal static class WindowsShellIntegration
{
    public const string MainAppUserModelId = "Tiedragon.Syscalculator";
    public const string NodEditorAppUserModelId = "Tiedragon.Syscalculator.NodEditor";
    public const string NodTemplateWizardAppUserModelId = "Tiedragon.Syscalculator.NodTemplateWizard";

    private const uint ShardPathW = 0x00000003;

    public static void SetCurrentAppUserModelId(string appUserModelId)
    {
        try
        {
            _ = SetCurrentProcessExplicitAppUserModelID(appUserModelId);
        }
        catch
        {
            // Shell integration is convenience UI; startup must not depend on it.
        }
    }

    public static void AddRecentDocument(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;

            SHAddToRecentDocs(ShardPathW, Path.GetFullPath(path));
        }
        catch
        {
            // Recent documents are convenience state; opening/saving files must stay the main action.
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appID);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern void SHAddToRecentDocs(uint uFlags, string pv);
}
