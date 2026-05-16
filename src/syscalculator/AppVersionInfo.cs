namespace Syscalculator.UI.WinForms;

internal static class AppVersionInfo
{
    public const string ProductName = "Syscalculator 2.0";
    public const string ProductVersion = "2.0 Daily";
    public const string ReleaseChannel = "Daily";
    public const string ReleaseDate = "08-05-2026";
    public const string Website = "https://www.tiedragon.com";
    public const string Email = "info@tiedragon.com";

    public static string BuildNumber => AppVersionGenerated.BuildNumber;
    public static string DisplayVersion => $"{ProductName} {ReleaseChannel.ToLowerInvariant()} build {BuildNumber}";
}
