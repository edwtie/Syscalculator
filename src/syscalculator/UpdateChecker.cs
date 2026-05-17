using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Syscalculator.UI.WinForms;

internal static class UpdateChecker
{
    public const string DefaultManifestUrl = "https://www.tiedragon.com/api/syscalculator-updates/syscalculator.json";

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync(DefaultManifestUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var manifest = await JsonSerializer.DeserializeAsync<UpdateManifest>(stream, JsonOptions, cancellationToken);
        if (manifest?.Channels is null ||
            !manifest.Channels.TryGetValue(AppVersionInfo.ReleaseChannel.ToLowerInvariant(), out var channel) ||
            channel is null)
        {
            return UpdateCheckResult.NoUpdate();
        }

        return IsNewer(channel.Version, channel.Date)
            ? UpdateCheckResult.Available(channel)
            : UpdateCheckResult.NoUpdate();
    }

    public static void OpenDownload(UpdateChannelInfo update)
    {
        if (string.IsNullOrWhiteSpace(update.DownloadUrl))
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = update.DownloadUrl,
            UseShellExecute = true
        });
    }

    public static bool StartUpdater(UpdateChannelInfo update)
    {
        if (string.IsNullOrWhiteSpace(update.PackageUrl) ||
            !Uri.TryCreate(update.PackageUrl, UriKind.Absolute, out _))
        {
            return false;
        }

        var updaterPath = Path.Combine(AppContext.BaseDirectory, "Updater", "Syscalculator.Updater.exe");
        if (!File.Exists(updaterPath))
            return false;

        var tempDirectory = Path.Combine(Path.GetTempPath(), "Syscalculator.Updater." + Guid.NewGuid().ToString("N"));
        CopyDirectory(Path.GetDirectoryName(updaterPath)!, tempDirectory);
        var tempUpdater = Path.Combine(tempDirectory, "Syscalculator.Updater.exe");

        var args = new[]
        {
            "--package-url", update.PackageUrl,
            "--install-dir", AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            "--restart", Application.ExecutablePath,
            "--pid", Environment.ProcessId.ToString(CultureInfo.InvariantCulture),
            "--sha256", update.Sha256 ?? ""
        };

        Process.Start(new ProcessStartInfo
        {
            FileName = tempUpdater,
            Arguments = BuildArguments(args),
            WorkingDirectory = tempDirectory,
            UseShellExecute = true
        });

        return true;
    }

    private static bool IsNewer(string? version, string? date)
    {
        var currentDate = NormalizeDate(AppVersionInfo.BuildDate);
        var updateDate = NormalizeDate(date) ?? NormalizeDate(version);
        if (currentDate is not null && updateDate is not null && updateDate != currentDate)
            return string.CompareOrdinal(updateDate, currentDate) > 0;

        var currentVersion = NormalizeVersion(AppVersionInfo.BuildDate);
        var updateVersion = NormalizeVersion(version);
        return currentVersion is not null &&
               updateVersion is not null &&
               string.CompareOrdinal(updateVersion, currentVersion) > 0;
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

    private static string BuildArguments(IEnumerable<string> args)
    {
        return string.Join(" ", args.Select(QuoteArgument));
    }

    private static string QuoteArgument(string value)
    {
        if (value.Length == 0)
            return "\"\"";

        return "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }

    private static string? NormalizeDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length >= 8 ? digits[..8] : null;
    }

    private static string? NormalizeVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var parts = value
            .Split(new[] { '.', '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => int.TryParse(part, out var number) ? number.ToString("D4") : "")
            .Where(part => part.Length > 0)
            .ToArray();

        return parts.Length == 0 ? null : string.Join(".", parts);
    }
}

internal sealed class UpdateCheckResult
{
    private UpdateCheckResult(bool hasUpdate, UpdateChannelInfo? update)
    {
        HasUpdate = hasUpdate;
        Update = update;
    }

    public bool HasUpdate { get; }
    public UpdateChannelInfo? Update { get; }

    public static UpdateCheckResult Available(UpdateChannelInfo update) => new(true, update);
    public static UpdateCheckResult NoUpdate() => new(false, null);
}

internal sealed class UpdateManifest
{
    [JsonPropertyName("channels")]
    public Dictionary<string, UpdateChannelInfo>? Channels { get; set; }
}

internal sealed class UpdateChannelInfo
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("downloadUrl")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("packageUrl")]
    public string? PackageUrl { get; set; }

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }

    [JsonPropertyName("releaseNotesUrl")]
    public string? ReleaseNotesUrl { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}
