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

    public static async Task<UpdateCheckResult> CheckAsync(string channel, CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync(DefaultManifestUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var manifest = await JsonSerializer.DeserializeAsync<UpdateManifest>(stream, JsonOptions, cancellationToken);
        var manifestChannel = NormalizeChannel(channel);
        if (manifest?.Channels is null ||
            !manifest.Channels.TryGetValue(manifestChannel, out var channelInfo) ||
            channelInfo is null)
        {
            return UpdateCheckResult.NoUpdate();
        }

        return IsAvailableUpdate(channelInfo)
            ? UpdateCheckResult.Available(channelInfo)
            : UpdateCheckResult.NoUpdate();
    }

    public static string NormalizeChannel(string? channel)
    {
        return channel?.Trim().ToLowerInvariant() switch
        {
            "beta" => "beta",
            "stable" or "production" => "stable",
            _ => "daily"
        };
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
            WorkingDirectory = tempDirectory,
            UseShellExecute = false
        }.WithArguments(args));

        return true;
    }

    private static bool IsAvailableUpdate(UpdateChannelInfo update)
    {
        if (HasDifferentPackageId(update))
            return true;

        return IsNewer(update.Version, update.Date);
    }

    private static bool HasDifferentPackageId(UpdateChannelInfo update)
    {
        if (string.IsNullOrWhiteSpace(update.PackageId))
            return false;

        var updateDate = NormalizeDate(update.Date) ?? NormalizeDate(update.Version);
        var currentDate = NormalizeDate(AppVersionInfo.BuildDate);
        if (updateDate is not null && currentDate is not null && string.CompareOrdinal(updateDate, currentDate) > 0)
            return true;

        if (updateDate is not null && currentDate is not null && string.CompareOrdinal(updateDate, currentDate) < 0)
            return false;

        return !update.PackageId.Equals(ReadInstalledPackageId(), StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadInstalledPackageId()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "update-state.cfg");
        if (!File.Exists(path))
            return "";

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
            if (key.Equals("packageId", StringComparison.OrdinalIgnoreCase))
                return value;
        }

        return "";
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

internal static class ProcessStartInfoExtensions
{
    public static ProcessStartInfo WithArguments(this ProcessStartInfo startInfo, IEnumerable<string> arguments)
    {
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        return startInfo;
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

    [JsonPropertyName("packageId")]
    public string? PackageId { get; set; }

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }

    [JsonPropertyName("releaseNotesUrl")]
    public string? ReleaseNotesUrl { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}
