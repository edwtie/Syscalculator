#nullable enable

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Syscalculator.UI.WinForms;

// Lightweight diagnostics for Syscalculator UI test builds. It mirrors the
// ToolEditor debugger so WinForms crashes are visible in Visual Studio/VS Code
// and also saved to a log file.
internal static class SyscalculatorDebugger
{
    private static readonly object Sync = new();
    private static readonly Dictionary<string, int> FirstChanceCounts = new(StringComparer.Ordinal);
    public static bool IsEnabled { get; } =
        string.Equals(Environment.GetEnvironmentVariable("SYSCALCULATOR_DIAGNOSTICS"), "1", StringComparison.OrdinalIgnoreCase);

    private static readonly string LogFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Syscalculator",
        "Logs");

    public static string CurrentLogPath { get; } = Path.Combine(
        LogFolder,
        "syscalculator-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");

    public static void Initialize(string[] args)
    {
        if (!IsEnabled)
            return;

        Directory.CreateDirectory(LogFolder);
        Log("Syscalculator diagnostics started.");
        Log("Arguments: " + (args.Length == 0 ? "(none)" : string.Join(" ", args.Select(QuoteArg))));

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        AppDomain.CurrentDomain.FirstChanceException += (_, e) => ReportFirstChanceException(e.Exception);
        Application.ThreadException += (_, e) => ReportException("UI thread exception", e.Exception, showDialog: true);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var exception = e.ExceptionObject as Exception;
            ReportException("Unhandled domain exception", exception, showDialog: true);
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            ReportException("Unobserved task exception", e.Exception, showDialog: false);
            e.SetObserved();
        };
    }

    public static void Log(string message)
    {
        if (!IsEnabled)
            return;

        WriteBlock(message);
    }

    public static void ReportException(string title, Exception? exception, bool showDialog)
    {
        if (!IsEnabled)
        {
            if (showDialog)
                ShowPlainError(title, exception);
            return;
        }

        var detail = exception?.ToString() ?? "Unknown exception object.";
        WriteBlock(title + Environment.NewLine + detail);

        if (!showDialog)
            return;

        try
        {
            MessageBox.Show(
                title + Environment.NewLine + Environment.NewLine +
                detail + Environment.NewLine + Environment.NewLine +
                "Logbestand:" + Environment.NewLine + CurrentLogPath,
                "Syscalculator debugger",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            // The process may already be failing; the log is the important part.
        }
    }

    private static void ReportFirstChanceException(Exception exception)
    {
        if (!ShouldLogFirstChance(exception))
            return;

        var signature = exception.GetType().FullName + "|" + exception.Source + "|" + exception.Message;
        lock (Sync)
        {
            FirstChanceCounts.TryGetValue(signature, out var count);
            if (count >= 3)
                return;

            FirstChanceCounts[signature] = count + 1;
        }

        WriteBlock("First-chance exception" + Environment.NewLine + exception);
    }

    private static bool ShouldLogFirstChance(Exception exception)
    {
        if (exception is not ArgumentException)
            return false;

        var source = exception.Source ?? "";
        return source.Contains("Windows", StringComparison.OrdinalIgnoreCase) ||
            source.Contains("Forms", StringComparison.OrdinalIgnoreCase) ||
            exception.StackTrace?.Contains("System.Private.Windows.Core", StringComparison.OrdinalIgnoreCase) == true ||
            exception.StackTrace?.Contains("System.Windows.Forms", StringComparison.OrdinalIgnoreCase) == true;
    }

    public static void OpenLogFolder()
    {
        if (!IsEnabled)
            return;

        Directory.CreateDirectory(LogFolder);
        Process.Start(new ProcessStartInfo
        {
            FileName = LogFolder,
            UseShellExecute = true
        });
    }

    private static void WriteBlock(string text)
    {
        try
        {
            Directory.CreateDirectory(LogFolder);
            var message = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] " + text;
            Debug.WriteLine(message);
            Trace.WriteLine(message);
            WriteDebugConsole(message);
            lock (Sync)
                File.AppendAllText(CurrentLogPath, message + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
            // Diagnostics must never create a second failure path.
        }
    }

    private static void WriteDebugConsole(string message)
    {
        try
        {
            OutputDebugString(message + Environment.NewLine);
        }
        catch
        {
            // Debug console mirroring is optional; the file log remains primary.
        }
    }

    private static string QuoteArg(string arg)
    {
        return arg.Contains(' ') ? "\"" + arg + "\"" : arg;
    }

    private static void ShowPlainError(string title, Exception? exception)
    {
        try
        {
            MessageBox.Show(
                title + Environment.NewLine + Environment.NewLine + (exception?.Message ?? "Unknown exception object."),
                "Syscalculator",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            // Error reporting must never create a second failure path.
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern void OutputDebugString(string lpOutputString);
}
