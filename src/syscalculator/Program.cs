using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace Syscalculator.UI.WinForms;



// Zoek/commentaar: Type-overzicht: class Program bevat de hoofdlogica/data voor dit onderdeel.
internal static class Program
{
    private const int AttachParentProcess = -1;

    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            if (args.Any(IsHelpSwitch))
            {
                SyscalculatorDebugger.Log("Startup mode: CommandLineHelp");
                ShowCommandLineHelp();
                return;
            }

            ApplicationConfiguration.Initialize();
            ApplySystemColorMode();
            SyscalculatorDebugger.Initialize(args);

            if (ShouldOpenNodTool(args, out var editorPath, out var openTemplateWizard))
            {
                SyscalculatorDebugger.Log(
                    "Startup mode: " + (openTemplateWizard ? "NodTemplateWizard" : "NodEditor") +
                    "; file=" + (string.IsNullOrWhiteSpace(editorPath) ? "(none)" : editorPath));
                WindowsShellIntegration.SetCurrentAppUserModelId(
                    openTemplateWizard
                        ? WindowsShellIntegration.NodTemplateWizardAppUserModelId
                        : WindowsShellIntegration.NodEditorAppUserModelId);
                Application.Run(new NodEditorForm(editorPath, openTemplateWizard));
                return;
            }

            WindowsShellIntegration.SetCurrentAppUserModelId(WindowsShellIntegration.MainAppUserModelId);
            var startInTray = args.Any(IsTraySwitch);
            var startupNodPath = args.FirstOrDefault(arg => !IsToolSwitch(arg) && !IsWizardToolSwitch(arg) && !IsTraySwitch(arg));
            SyscalculatorDebugger.Log(
                "Startup mode: MainForm; tray=" + startInTray +
                "; startupNod=" + (string.IsNullOrWhiteSpace(startupNodPath) ? "(none)" : startupNodPath));

            using var singleInstance = SingleInstanceController.Create();
            if (!singleInstance.IsFirstInstance)
            {
                SyscalculatorDebugger.Log("Second instance detected; signaling first instance.");
                singleInstance.SignalFirstInstance(startupNodPath);
                return;
            }

            var mainForm = new MainForm(startupNodPath, startInTray);
            singleInstance.StartListening(mainForm);
            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            SyscalculatorDebugger.ReportException("Fatal Syscalculator exception", ex, showDialog: true);
            throw;
        }
    }

    private static void ApplySystemColorMode()
    {
#pragma warning disable WFO5001
        Application.SetColorMode(SystemColorMode.Dark);
#pragma warning restore WFO5001
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

    private static bool IsTraySwitch(string arg)
    {
        return arg.Equals("-tray", StringComparison.OrdinalIgnoreCase) ||
               arg.Equals("/tray", StringComparison.OrdinalIgnoreCase) ||
               arg.Equals("--tray", StringComparison.OrdinalIgnoreCase);
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
        Console.WriteLine("  Syscalculator.exe -tray");
        Console.WriteLine("      Start Syscalculator hidden in the system tray.");
        Console.WriteLine();
        Console.WriteLine("  Syscalculator.exe -help");
        Console.WriteLine("      Show this help text.");
        Console.WriteLine();
        Console.WriteLine("Aliases:");
        Console.WriteLine("  -tool, /tool, --tool");
        Console.WriteLine("  -wizardtool, /wizardtool, --wizardtool");
        Console.WriteLine("  -tray, /tray, --tray");
        Console.WriteLine("  -help, /help, --help, -?, /?");
        Console.WriteLine();
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();
}

internal sealed class SingleInstanceController : IDisposable
{
    private const string MutexName = @"Local\Tiedragon.Syscalculator.Main";
    private const string PipeName = "Tiedragon.Syscalculator.Main";

    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _cancellation = new();

    private SingleInstanceController(Mutex mutex, bool isFirstInstance)
    {
        _mutex = mutex;
        IsFirstInstance = isFirstInstance;
    }

    public bool IsFirstInstance { get; }

    public static SingleInstanceController Create()
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        return new SingleInstanceController(mutex, createdNew);
    }

    public void StartListening(MainForm mainForm)
    {
        _ = Task.Run(async () =>
        {
            while (!_cancellation.IsCancellationRequested)
            {
                try
                {
                    await using var pipe = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        maxNumberOfServerInstances: 1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await pipe.WaitForConnectionAsync(_cancellation.Token);
                    using var reader = new StreamReader(pipe);
                    var nodPath = await reader.ReadLineAsync(_cancellation.Token);
                    if (!mainForm.IsDisposed)
                        mainForm.BeginInvoke(() => mainForm.ActivateFromSecondInstance(nodPath));
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch
                {
                }
            }
        });
    }

    public void SignalFirstInstance(string? nodPath)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                pipe.Connect(250);
                using var writer = new StreamWriter(pipe) { AutoFlush = true };
                writer.WriteLine(string.IsNullOrWhiteSpace(nodPath) ? "" : Path.GetFullPath(nodPath));
                return;
            }
            catch
            {
                Thread.Sleep(150);
            }
        }
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();

        if (IsFirstInstance)
            _mutex.ReleaseMutex();

        _mutex.Dispose();
    }
}
