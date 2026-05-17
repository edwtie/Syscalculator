namespace Syscalculator.UI.WinForms;

internal static class AppVersionInfo
{
    public const string ProductName = "Syscalculator 2.0";
    public const string ReleaseDate = "17-05-2026";
    public const string Website = "https://www.tiedragon.com";
    public const string Email = "info@tiedragon.com";

    public static string ProductVersion => $"2.0 {ReleaseChannel}";
    public static string ReleaseChannel => DetectReleaseChannel();
    public static string BuildNumber => BuildDate;
    public static string BuildDate
    {
        get
        {
            var build = AppVersionGenerated.BuildNumber;
            return build.Length >= 10 ? build[..10] : build;
        }
    }

    public static string DisplayVersion => $"{ProductName} {ReleaseChannel.ToLowerInvariant()} {BuildDate}";

    private static string DetectReleaseChannel()
    {
        var packageId = ReadInstalledPackageId();
        if (packageId.StartsWith("beta-", StringComparison.OrdinalIgnoreCase))
            return "Beta";

        if (packageId.StartsWith("production-", StringComparison.OrdinalIgnoreCase) ||
            packageId.StartsWith("stable-", StringComparison.OrdinalIgnoreCase))
        {
            return "Stable";
        }

        return "Daily";
    }

    private static string ReadInstalledPackageId()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "update-state.cfg");
        if (!File.Exists(path))
            return "";

        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("packageId=", StringComparison.OrdinalIgnoreCase))
                continue;

            return trimmed["packageId=".Length..].Trim();
        }

        return "";
    }
}
