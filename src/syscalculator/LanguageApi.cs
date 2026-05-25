#nullable enable

namespace Syscalculator.UI.WinForms;

public sealed record LanguageApiLanguageInfo(
    string DisplayName,
    string FileName,
    string SourceLabel,
    string? PackageId,
    bool Signed,
    string SignatureAlgorithm,
    string SignatureKeyId,
    string SignatureKeySha256,
    string LanguageCode,
    string Producer,
    string Product,
    string PackageVersion);

public sealed record LanguageApiConfiguredLanguage(
    string BaseDirectory,
    string DisplayName,
    string FileName,
    string SourceLabel,
    string? PackageId,
    bool Signed,
    string SignatureAlgorithm,
    string SignatureKeyId,
    string SignatureKeySha256,
    string LanguageCode,
    string Producer,
    string Product,
    string PackageVersion);

public static class LanguageApi
{
    public static IReadOnlyList<LanguageApiLanguageInfo> ListAvailable(string? baseDirectory = null)
    {
        var root = ResolveBaseDirectory(baseDirectory);
        return LanguageCatalog.ListAvailable(root)
            .Select(ToApiLanguageInfo)
            .ToArray();
    }

    public static LanguageApiConfiguredLanguage GetConfigured(string? baseDirectory = null)
    {
        var root = ResolveBaseDirectory(baseDirectory);
        var configured = LanguageCatalog.LoadConfigured(root);
        var language = LanguageCatalog.ListAvailable(root)
            .FirstOrDefault(language => language.Matches(configured.FileName, configured.PackageId));
        var info = language is null
            ? new LanguageApiLanguageInfo(
                Path.GetFileNameWithoutExtension(configured.FileName),
                configured.FileName,
                configured.PackageId is null ? configured.FileName : configured.PackageId + "/" + configured.FileName,
                configured.PackageId,
                Signed: false,
                SignatureAlgorithm: "",
                SignatureKeyId: "",
                SignatureKeySha256: "",
                LanguageCode: Path.GetFileNameWithoutExtension(configured.FileName),
                Producer: "",
                Product: "",
                PackageVersion: "")
            : ToApiLanguageInfo(language);

        return new LanguageApiConfiguredLanguage(
            root,
            info.DisplayName,
            info.FileName,
            info.SourceLabel,
            info.PackageId,
            info.Signed,
            info.SignatureAlgorithm,
            info.SignatureKeyId,
            info.SignatureKeySha256,
            info.LanguageCode,
            info.Producer,
            info.Product,
            info.PackageVersion);
    }

    public static LanguageApiLanguageInfo InstallPackage(string packagePath, string? baseDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        var root = ResolveBaseDirectory(baseDirectory);
        var manifest = LanguagePackageService.Install(root, packagePath);
        var fileName = manifest.LanguageCode + ".lng";
        var language = LanguageCatalog.ListAvailable(root)
            .FirstOrDefault(language => language.Matches(fileName, manifest.PackageKey));
        return language is null
            ? new LanguageApiLanguageInfo(
                string.IsNullOrWhiteSpace(manifest.NativeName) ? manifest.DisplayName : manifest.NativeName,
                fileName,
                manifest.PackageKey + "/" + fileName,
                manifest.PackageKey,
                Signed: false,
                SignatureAlgorithm: "",
                SignatureKeyId: "",
                SignatureKeySha256: "",
                LanguageCode: manifest.LanguageCode,
                Producer: manifest.Producer,
                Product: manifest.Product,
                PackageVersion: manifest.PackageVersion)
            : ToApiLanguageInfo(language);
    }

    public static string Text(string key, string fallback = "", string? baseDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return LanguageCatalog.LoadConfigured(ResolveBaseDirectory(baseDirectory)).Text(key, fallback);
    }

    private static LanguageApiLanguageInfo ToApiLanguageInfo(LanguageCatalog.LanguageInfo language)
    {
        return new LanguageApiLanguageInfo(
            language.DisplayName,
            language.FileName,
            language.SourceLabel,
            language.PackageId,
            language.Signed,
            language.SignatureAlgorithm,
            language.SignatureKeyId,
            language.SignatureKeySha256,
            string.IsNullOrWhiteSpace(language.LanguageCode) ? Path.GetFileNameWithoutExtension(language.FileName) : language.LanguageCode,
            language.Producer,
            language.Product,
            language.PackageVersion);
    }

    private static string ResolveBaseDirectory(string? baseDirectory)
    {
        return string.IsNullOrWhiteSpace(baseDirectory)
            ? AppContext.BaseDirectory
            : Path.GetFullPath(baseDirectory);
    }
}
