#nullable enable

using System.Diagnostics;
using System.Text;

namespace Tiedragon.ToolEditor;

// Lightweight diagnostics for ToolEditor test builds. It records crashes and
// unexpected exceptions without requiring Visual Studio to catch them first.
internal static class ToolEditorDebugger
{
    private static readonly object Sync = new();
    private static readonly Dictionary<string, int> FirstChanceCounts = new(StringComparer.Ordinal);
    private static bool _initialized;
    public static bool IsEnabled { get; } =
        string.Equals(Environment.GetEnvironmentVariable("SYSCALCULATOR_DIAGNOSTICS"), "1", StringComparison.OrdinalIgnoreCase);

    private static readonly string LogFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Syscalculator",
        "ToolEditor",
        "Logs");

    public static string CurrentLogPath { get; } = Path.Combine(
        LogFolder,
        "tooleditor-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");

    public static void Initialize()
    {
        InitializeCore(setWinFormsExceptionMode: true);
    }

    public static void InitializeEmbedded()
    {
        InitializeCore(setWinFormsExceptionMode: false);
    }

    private static void InitializeCore(bool setWinFormsExceptionMode)
    {
        if (!IsEnabled)
            return;

        Directory.CreateDirectory(LogFolder);
        if (_initialized)
        {
            Log("ToolEditor diagnostics already active.");
            return;
        }

        _initialized = true;
        Log("ToolEditor diagnostics started.");

        if (setWinFormsExceptionMode)
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        }
        else
        {
            Log("ToolEditor diagnostics embedded mode: using host WinForms exception mode.");
        }

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
                "ToolEditor debugger",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            // The process may already be crashing; the log write above is the
            // important part.
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
            lock (Sync)
                File.AppendAllText(CurrentLogPath, message + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
            // Diagnostics must never create a second failure path.
        }
    }

    private static void ShowPlainError(string title, Exception? exception)
    {
        try
        {
            MessageBox.Show(
                title + Environment.NewLine + Environment.NewLine + (exception?.Message ?? "Unknown exception object."),
                "ToolEditor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            // Error reporting must never create a second failure path.
        }
    }
}
