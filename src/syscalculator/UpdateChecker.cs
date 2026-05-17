using System.Diagnostics;
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

    [JsonPropertyName("releaseNotesUrl")]
    public string? ReleaseNotesUrl { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}
