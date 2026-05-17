namespace Syscalculator.UI.WinForms;

internal static class AppVersionInfo
{
    public const string ProductName = "Syscalculator 2.0";
    public const string ProductVersion = "2.0 Daily";
    public const string ReleaseChannel = "Daily";
    public const string ReleaseDate = "17-05-2026";
    public const string Website = "https://www.tiedragon.com";
    public const string Email = "info@tiedragon.com";

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
}
