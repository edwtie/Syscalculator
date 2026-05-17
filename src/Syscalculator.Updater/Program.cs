using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace Syscalculator.Updater;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var options = UpdateOptions.Parse(args);
        var language = UpdaterLanguage.Load(options.InstallDirectory);
        if (!options.IsValid)
        {
            MessageBox.Show(
                language.Text("updater.invalid_options", "The update cannot start because update information is missing."),
                language.Text("updater.title", "Syscalculator updater"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        Application.Run(new UpdaterForm(options, language));
    }
}

internal sealed class UpdaterForm : Form
{
    private readonly UpdateOptions _options;
    private readonly Label _statusLabel = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Button _cancelButton = new();
    private readonly CancellationTokenSource _cancellation = new();
    private readonly UpdaterLanguage _language;

    public UpdaterForm(UpdateOptions options, UpdaterLanguage language)
    {
        _options = options;
        _language = language;

        Text = T("updater.title", "Syscalculator updater");
        Width = 520;
        Height = 180;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;

        _statusLabel.AutoSize = false;
        _statusLabel.Left = 18;
        _statusLabel.Top = 18;
        _statusLabel.Width = 468;
        _statusLabel.Height = 48;
        _statusLabel.Text = T("updater.preparing", "Preparing update...");

        _progressBar.Left = 18;
        _progressBar.Top = 76;
        _progressBar.Width = 468;
        _progressBar.Height = 22;

        _cancelButton.Text = T("updater.cancel", "Cancel");
        _cancelButton.Left = 380;
        _cancelButton.Top = 108;
        _cancelButton.Width = 106;
        _cancelButton.Height = 30;
        _cancelButton.Click += (_, _) =>
        {
            _cancelButton.Enabled = false;
            _statusLabel.Text = T("updater.canceling", "Canceling update...");
            _cancellation.Cancel();
        };

        Controls.Add(_statusLabel);
        Controls.Add(_progressBar);
        Controls.Add(_cancelButton);
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);

        try
        {
            await RunUpdateAsync(_cancellation.Token);
            _cancelButton.Enabled = false;
            _statusLabel.Text = T("updater.completed", "Update completed. Starting Syscalculator...");
            RestartSyscalculator();
            Close();
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = T("updater.canceled", "Update canceled.");
            Close();
        }
        catch (Exception ex)
        {
            _cancelButton.Text = T("updater.close", "Close");
            _cancelButton.Enabled = true;
            _cancelButton.Click += (_, _) => Close();
            MessageBox.Show(
                this,
                string.Format(T("updater.failed", "The update failed.\n\n{0}"), ex.Message),
                T("updater.title", "Syscalculator updater"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async Task RunUpdateAsync(CancellationToken cancellationToken)
    {
        var packagePath = Path.Combine(Path.GetTempPath(), "Syscalculator-update-" + Guid.NewGuid().ToString("N") + ".zip");
        var extractPath = Path.Combine(Path.GetTempPath(), "Syscalculator-update-" + Guid.NewGuid().ToString("N"));

        try
        {
            await DownloadPackageAsync(packagePath, cancellationToken);
            VerifyPackage(packagePath);

            _statusLabel.Text = T("updater.extracting", "Extracting update...");
            Directory.CreateDirectory(extractPath);
            ZipFile.ExtractToDirectory(packagePath, extractPath, overwriteFiles: true);

            await WaitForSyscalculatorToExitAsync(cancellationToken);

            _statusLabel.Text = T("updater.copying", "Updating files...");
            _progressBar.Style = ProgressBarStyle.Marquee;
            CopyDirectory(extractPath, _options.InstallDirectory);
        }
        finally
        {
            TryDeleteFile(packagePath);
            TryDeleteDirectory(extractPath);
        }
    }

    private async Task DownloadPackageAsync(string packagePath, CancellationToken cancellationToken)
    {
        _statusLabel.Text = T("updater.downloading", "Downloading update...");
        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Value = 0;

        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SyscalculatorUpdater", "2.0"));

        using var response = await client.GetAsync(_options.PackageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(packagePath);

        var buffer = new byte[81920];
        long downloaded = 0;
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;

            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            downloaded += read;

            if (totalBytes is > 0)
            {
                var percent = (int)Math.Clamp(downloaded * 100 / totalBytes.Value, 0, 100);
                _progressBar.Value = percent;
                _statusLabel.Text = string.Format(T("updater.downloading_percent", "Downloading update... {0}%"), percent);
            }
        }

        if (totalBytes is > 0)
        {
            _progressBar.Value = 100;
            _statusLabel.Text = string.Format(T("updater.downloading_percent", "Downloading update... {0}%"), 100);
        }
    }

    private void VerifyPackage(string packagePath)
    {
        if (string.IsNullOrWhiteSpace(_options.Sha256))
            return;

        _statusLabel.Text = T("updater.verifying", "Verifying update...");
        using var stream = File.OpenRead(packagePath);
        var hash = Convert.ToHexString(SHA256.HashData(stream));
        if (!hash.Equals(_options.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(T("updater.hash_mismatch", "The update package has a different checksum than expected."));
    }

    private async Task WaitForSyscalculatorToExitAsync(CancellationToken cancellationToken)
    {
        if (_options.ProcessId <= 0)
            return;

        try
        {
            var process = Process.GetProcessById(_options.ProcessId);
            _statusLabel.Text = T("updater.waiting_for_exit", "Waiting for Syscalculator to close...");

            while (!process.HasExited)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(250, cancellationToken);
            }
        }
        catch (ArgumentException)
        {
        }
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(targetDirectory, relative));
        }

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDirectory, file);
            var target = Path.Combine(targetDirectory, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private void RestartSyscalculator()
    {
        if (string.IsNullOrWhiteSpace(_options.RestartPath) || !File.Exists(_options.RestartPath))
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = _options.RestartPath,
            WorkingDirectory = _options.InstallDirectory,
            UseShellExecute = true
        });
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
        }
    }

    private string T(string key, string fallback) => _language.Text(key, fallback);
}

internal sealed class UpdaterLanguage
{
    private readonly Dictionary<string, string> _texts;

    private UpdaterLanguage(Dictionary<string, string> texts)
    {
        _texts = texts;
    }

    public static UpdaterLanguage Load(string installDirectory)
    {
        var baseDirectory = Directory.Exists(installDirectory)
            ? installDirectory
            : AppContext.BaseDirectory;
        var languageFile = LoadConfiguredLanguageFile(baseDirectory);
        var texts = ReadLanguageFile(Path.Combine(baseDirectory, languageFile));

        if (texts.Count == 0 && !languageFile.Equals("eng.lng", StringComparison.OrdinalIgnoreCase))
            texts = ReadLanguageFile(Path.Combine(baseDirectory, "eng.lng"));

        return new UpdaterLanguage(texts);
    }

    public string Text(string key, string fallback)
    {
        var text = _texts.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;

        return text.Replace("\\n", Environment.NewLine, StringComparison.Ordinal);
    }

    private static string LoadConfiguredLanguageFile(string baseDirectory)
    {
        var configPath = Path.Combine(baseDirectory, "language.cfg");
        if (!File.Exists(configPath))
            return "eng.lng";

        foreach (var rawLine in File.ReadAllLines(configPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal))
                continue;

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (!key.Equals("language", StringComparison.OrdinalIgnoreCase) || value.Length == 0)
                continue;

            return value.EndsWith(".lng", StringComparison.OrdinalIgnoreCase)
                ? value
                : value + ".lng";
        }

        return "eng.lng";
    }

    private static Dictionary<string, string> ReadLanguageFile(string path)
    {
        var texts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path))
            return texts;

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal))
                continue;

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (key.Length > 0)
                texts[key] = value;
        }

        return texts;
    }
}

internal sealed class UpdateOptions
{
    public Uri? PackageUrl { get; private init; }
    public string InstallDirectory { get; private init; } = "";
    public string RestartPath { get; private init; } = "";
    public string Sha256 { get; private init; } = "";
    public int ProcessId { get; private init; }

    public bool IsValid => PackageUrl is not null &&
                           Directory.Exists(InstallDirectory) &&
                           !string.IsNullOrWhiteSpace(RestartPath);

    public static UpdateOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index++)
        {
            var key = args[index];
            if (!key.StartsWith("--", StringComparison.Ordinal) || index + 1 >= args.Length)
                continue;

            values[key[2..]] = args[++index];
        }

        Uri.TryCreate(Get(values, "package-url"), UriKind.Absolute, out var packageUrl);
        int.TryParse(Get(values, "pid"), out var processId);

        return new UpdateOptions
        {
            PackageUrl = packageUrl,
            InstallDirectory = Get(values, "install-dir"),
            RestartPath = Get(values, "restart"),
            Sha256 = Get(values, "sha256"),
            ProcessId = processId
        };
    }

    private static string Get(Dictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var value) ? value : "";
    }
}
