using System.Runtime.InteropServices;

namespace Syscalculator.UI.WinForms;



// Zoek/commentaar: Type-overzicht: class Program bevat de hoofdlogica/data voor dit onderdeel.
internal static class Program
{
    private const int AttachParentProcess = -1;

    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Any(IsHelpSwitch))
        {
            ShowCommandLineHelp();
            return;
        }

        ApplicationConfiguration.Initialize();

        if (ShouldOpenNodTool(args, out var editorPath, out var openTemplateWizard))
        {
            Application.Run(new NodEditorForm(editorPath, openTemplateWizard));
            return;
        }

        var startupNodPath = args.FirstOrDefault(arg => !IsToolSwitch(arg) && !IsWizardToolSwitch(arg));
        Application.Run(new MainForm(startupNodPath));
    }

    private static bool ShouldOpenNodTool(string[] args, out string? editorPath, out bool openTemplateWizard)
    {
        editorPath = null;
        openTemplateWizard = false;

        for (var index = 0; index < args.Length; index++)
        {
            if (!IsToolSwitch(args[index]) && !IsWizardToolSwitch(args[index]))
                continue;

            openTemplateWizard = IsWizardToolSwitch(args[index]);
            editorPath = args.Skip(index + 1).FirstOrDefault(arg => !IsToolSwitch(arg) && !IsWizardToolSwitch(arg));
            return true;
        }

        return false;
    }

    private static bool IsToolSwitch(string arg)
    {
        return arg.Equals("-tool", StringComparison.OrdinalIgnoreCase) ||
               arg.Equals("/tool", StringComparison.OrdinalIgnoreCase) ||
               arg.Equals("--tool", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWizardToolSwitch(string arg)
    {
        return arg.Equals("-wizardtool", StringComparison.OrdinalIgnoreCase) ||
               arg.Equals("/wizardtool", StringComparison.OrdinalIgnoreCase) ||
               arg.Equals("--wizardtool", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHelpSwitch(string arg)
    {
        return arg.Equals("-help", StringComparison.OrdinalIgnoreCase) ||
               arg.Equals("/help", StringComparison.OrdinalIgnoreCase) ||
               arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
               arg.Equals("-?", StringComparison.OrdinalIgnoreCase) ||
               arg.Equals("/?", StringComparison.OrdinalIgnoreCase);
    }

    private static void ShowCommandLineHelp()
    {
        if (!AttachConsole(AttachParentProcess))
            AllocConsole();

        using var output = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
        Console.SetOut(output);

        Console.WriteLine();
        Console.WriteLine(AppVersionInfo.DisplayVersion);
        Console.WriteLine("Command line help");
        Console.WriteLine($"Release date: {AppVersionInfo.ReleaseDate}");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  Syscalculator.exe");
        Console.WriteLine("      Start the classic converter window.");
        Console.WriteLine();
        Console.WriteLine("  Syscalculator.exe <file.nod>");
        Console.WriteLine("      Start the classic converter window and load the NOD file.");
        Console.WriteLine();
        Console.WriteLine("  Syscalculator.exe -tool [file.nod]");
        Console.WriteLine("      Start the NOD Editor development tool.");
        Console.WriteLine();
        Console.WriteLine("  Syscalculator.exe -wizardtool [file.nod]");
        Console.WriteLine("      Start the NOD Editor and open the template wizard immediately.");
        Console.WriteLine();
        Console.WriteLine("  Syscalculator.exe -help");
        Console.WriteLine("      Show this help text.");
        Console.WriteLine();
        Console.WriteLine("Aliases:");
        Console.WriteLine("  -tool, /tool, --tool");
        Console.WriteLine("  -wizardtool, /wizardtool, --wizardtool");
        Console.WriteLine("  -help, /help, --help, -?, /?");
        Console.WriteLine();
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();
}
