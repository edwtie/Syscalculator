#nullable enable
using System.Text.Json;
using Tiedragon.LanguagePackage;

namespace Syscalculator.UI.WinForms;

// Copyright (c) Tiedragon. All rights reserved.
//
// Language packages keep translated text, help, manuals, and assets together.
// The service is intentionally small and strict: a package is data, never code.
internal static class LanguagePackageService
{
    private const string PackageDirectoryName = "LanguagePackages";
    private const string CacheDirectoryName = "Cache";
    private const string PackageExtension = ".lngpdk";
    private const string LegacyZipExtension = ".zip";
    private const string ProducerName = "Tiedragon";
    private const string ProductName = "Syscalculator";
    private const string SoftwareId = "tiedragon.syscalculator";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bat",
        ".cmd",
        ".com",
        ".dll",
        ".exe",
        ".msi",
        ".ps1",
        ".scr",
        ".vbs",
    };

    public static IReadOnlyList<LanguagePackageInfo> ListInstalled(string baseDirectory)
    {
        var root = GetPackageRoot(baseDirectory);
        if (!Directory.Exists(root))
            return [];

        var packages = new List<LanguagePackageInfo>();
        foreach (var zipPath in EnumeratePackageFiles(root))
        {
            if (TryReadZipPackage(zipPath, out var package))
                packages.Add(package);
        }

        foreach (var directory in Directory.GetDirectories(root))
        {
            if (Path.GetFileName(directory).Equals(CacheDirectoryName, StringComparison.OrdinalIgnoreCase))
                continue;

            var manifest = ReadManifest(directory);
            if (manifest is null || !TryResolveLanguageFile(directory, manifest.LanguageCode, out var languagePath))
                continue;

            if (packages.Any(package => package.Manifest.PackageKey.Equals(manifest.PackageKey, StringComparison.OrdinalIgnoreCase)))
                continue;

            packages.Add(LanguagePackageInfo.FromDirectory(manifest, directory, languagePath));
        }

        return packages
            .OrderBy(package => package.Manifest.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(package => package.Manifest.PackageKey, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool TryFindPackage(
        string baseDirectory,
        string packageKey,
        out LanguagePackageInfo package)
    {
        package = null!;
        if (!IsSafePackageKey(packageKey))
            return false;

        var root = GetPackageRoot(baseDirectory);
        foreach (var zipPath in EnumeratePackageFiles(root))
        {
            if (TryReadZipPackage(zipPath, out var zipPackage) &&
                zipPackage.Manifest.PackageKey.Equals(packageKey, StringComparison.OrdinalIgnoreCase))
            {
                package = zipPackage;
                return true;
            }
        }

        foreach (var directory in Directory.GetDirectories(root))
        {
            if (Path.GetFileName(directory).Equals(CacheDirectoryName, StringComparison.OrdinalIgnoreCase))
                continue;

            var manifest = ReadManifest(directory);
            if (manifest is null ||
                !manifest.PackageKey.Equals(packageKey, StringComparison.OrdinalIgnoreCase) ||
                !TryResolveLanguageFile(directory, manifest.LanguageCode, out var languagePath))
            {
                continue;
            }

            package = LanguagePackageInfo.FromDirectory(manifest, directory, languagePath);
            return true;
        }

        return false;
    }

    public static bool TryReadLanguageFile(
        string baseDirectory,
        string packageId,
        string fileName,
        out string content,
        out string resolvedFileName)
    {
        content = "";
        resolvedFileName = fileName;
        if (!TryFindPackage(baseDirectory, packageId, out var package))
            return false;

        var requestedCode = Path.GetFileNameWithoutExtension(fileName);
        if (package.IsArchive)
            return TryReadZipEntry(package.PackagePath, "language/" + requestedCode + ".lng", out content);

        if (!TryResolveLanguageFile(package.PackagePath, requestedCode, out var path))
            return false;

        resolvedFileName = Path.GetFileName(path);
        content = File.ReadAllText(path);
        return true;
    }

    public static bool TryReadContentFile(
        string baseDirectory,
        string packageId,
        string relativePath,
        out string content)
    {
        content = "";
        if (!TryFindPackage(baseDirectory, packageId, out var package))
            return false;

        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        ValidateEntryName(normalized);

        if (package.IsArchive)
            return TryReadZipEntry(package.PackagePath, normalized, out content);

        var packageRoot = Path.GetFullPath(package.PackagePath);
        var path = Path.GetFullPath(Path.Combine(packageRoot, normalized));
        if (!path.StartsWith(packageRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(path))
        {
            return false;
        }

        content = File.ReadAllText(path);
        return true;
    }

    public static bool TryReadHelpContentFile(
        string baseDirectory,
        string packageId,
        string fileName,
        out string content)
    {
        content = "";
        foreach (var candidate in EnumerateHelpContentCandidates(fileName))
        {
            try
            {
                if (TryReadContentFile(baseDirectory, packageId, candidate, out content))
                    return true;
            }
            catch (InvalidDataException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        return false;
    }

    public static LanguagePackageManifest Install(string baseDirectory, string zipPath)
    {
        var manifest = ReadArchiveManifest(zipPath) ??
            throw new InvalidDataException("Language package manifest.json is missing or invalid.");

        ValidateManifest(manifest);

        var root = GetPackageRoot(baseDirectory);
        Directory.CreateDirectory(root);

        ValidatePackageEntries(zipPath);

        if (!ContainsEntry(zipPath, "language/" + manifest.LanguageCode + ".lng"))
            throw new InvalidDataException($"Language package does not contain language/{manifest.LanguageCode}.lng.");

        CopyToCache(root, zipPath, manifest.PackageKey);
        return manifest;
    }

    private static string GetPackageRoot(string baseDirectory)
    {
        return Path.Combine(baseDirectory, PackageDirectoryName);
    }

    private static LanguagePackageManifest? ReadManifest(string packageDirectory)
    {
        var path = Path.Combine(packageDirectory, "manifest.json");
        if (!File.Exists(path))
            return null;

        try
        {
            var manifest = JsonSerializer.Deserialize<LanguagePackageManifest>(File.ReadAllText(path), JsonOptions);
            if (manifest is null)
                return null;

            ValidateManifest(manifest);
            return manifest;
        }
        catch
        {
            return null;
        }
    }

    private static LanguagePackageManifest? ReadArchiveManifest(string packagePath)
    {
        return LanguagePackageReader.TryReadManifest(packagePath, out var manifest)
            ? manifest
            : null;
    }

    private static IEnumerable<string> EnumeratePackageFiles(string root)
    {
        foreach (var path in EnumeratePackageFilesInDirectory(root))
            yield return path;

        var cache = Path.Combine(root, CacheDirectoryName);
        if (!Directory.Exists(cache))
            yield break;

        foreach (var path in EnumeratePackageFilesInDirectory(cache))
            yield return path;
    }

    private static IEnumerable<string> EnumeratePackageFilesInDirectory(string directory)
    {
        foreach (var path in Directory.GetFiles(directory, "*" + PackageExtension))
            yield return path;

        foreach (var path in Directory.GetFiles(directory, "*" + LegacyZipExtension))
            yield return path;
    }

    private static IEnumerable<string> EnumerateHelpContentCandidates(string fileName)
    {
        var normalized = NormalizeArchiveEntryName(fileName);
        if (normalized.Length == 0)
            yield break;

        if (normalized.StartsWith("help/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("manual/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("formula/", StringComparison.OrdinalIgnoreCase))
        {
            yield return normalized;
        }

        yield return "help/" + normalized;
        yield return "help/Content/" + normalized;
        yield return "Content/" + normalized;
        yield return "manual/" + normalized;

        var shortName = Path.GetFileName(normalized);
        if (!string.IsNullOrWhiteSpace(shortName))
            yield return "manual/" + shortName;
    }

    private static bool TryReadZipPackage(string zipPath, out LanguagePackageInfo package)
    {
        package = null!;
        try
        {
            var manifest = ReadArchiveManifest(zipPath);
            if (manifest is null)
                return false;

            ValidateManifest(manifest);
            var header = ValidatePackageEntries(zipPath);

            if (!ContainsEntry(zipPath, "language/" + manifest.LanguageCode + ".lng"))
                return false;

            package = LanguagePackageInfo.FromArchive(manifest, zipPath, header);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadZipEntry(string zipPath, string entryName, out string content)
    {
        try
        {
            return TryReadArchiveEntry(zipPath, entryName, out content);
        }
        catch
        {
            content = "";
            return false;
        }
    }

    private static bool TryReadArchiveEntry(string packagePath, string entryName, out string content)
    {
        return LanguagePackageReader.TryReadEntry(packagePath, NormalizeArchiveEntryName(entryName), out content);
    }

    private static bool ContainsEntry(string packagePath, string entryName)
    {
        return LanguagePackageReader.ContainsEntry(packagePath, NormalizeArchiveEntryName(entryName));
    }

    private static LanguagePackageContainerHeader? ValidatePackageEntries(string packagePath)
    {
        var inspection = LanguagePackageReader.Inspect(packagePath);
        LanguagePackageReader.ValidateEntries(packagePath);
        var header = inspection.Header;
        if (header is null)
            return null;

        if (!SoftwareId.Equals(header.SoftwareId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Language package is not intended for Syscalculator.");
        return header;
    }

    private static string NormalizeArchiveEntryName(string? entryName)
    {
        return (entryName ?? "").Replace('\\', '/').TrimStart('/');
    }

    private static void ValidateManifest(LanguagePackageManifest manifest)
    {
        if (manifest.Format != 1)
            throw new InvalidDataException($"Unsupported language package format: {manifest.Format}.");
        if (!ProducerName.Equals(manifest.Producer, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Language package producer is not trusted.");
        if (!ProductName.Equals(manifest.Product, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Language package product is not supported.");
        if (!SoftwareId.Equals(manifest.SoftwareId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Language package is not intended for Syscalculator.");
        if (!IsSafePackageKey(manifest.PackageKey))
            throw new InvalidDataException("Language package key contains unsafe characters.");
        if (!IsSafeLanguageCode(manifest.LanguageCode))
            throw new InvalidDataException("Language package language code is invalid.");
        if (string.IsNullOrWhiteSpace(manifest.DisplayName))
            throw new InvalidDataException("Language package displayName is required.");
    }

    private static bool TryResolveLanguageFile(string packageDirectory, string languageCode, out string path)
    {
        path = Path.Combine(packageDirectory, "language", languageCode + ".lng");
        return File.Exists(path);
    }

    private static void ValidateEntryName(string entryName)
    {
        if (Path.IsPathRooted(entryName))
            throw new InvalidDataException("Language package contains an absolute path.");

        var normalized = entryName.Replace('\\', '/');
        if (normalized.Split('/').Any(part => part == ".."))
            throw new InvalidDataException("Language package contains a parent directory path.");

        var extension = Path.GetExtension(normalized);
        if (BlockedExtensions.Contains(extension))
            throw new InvalidDataException($"Language package contains blocked file type: {extension}.");
    }

    private static bool IsSafePackageKey(string value)
    {
        return value.Length > 0 &&
            value.Length <= 96 &&
            value.All(character => char.IsLetterOrDigit(character) || character is '.' or '-' or '_');
    }

    private static bool IsSafeLanguageCode(string value)
    {
        return value.Length is >= 2 and <= 12 &&
            value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_');
    }

    private static void CopyToCache(string root, string zipPath, string packageKey)
    {
        var cache = Path.Combine(root, CacheDirectoryName);
        Directory.CreateDirectory(cache);
        File.Copy(zipPath, Path.Combine(cache, packageKey + PackageExtension), overwrite: true);
    }
}

internal sealed record LanguagePackageInfo(
    LanguagePackageManifest Manifest,
    string PackagePath,
    string LanguageFileName,
    bool IsArchive,
    bool Signed,
    string SignatureAlgorithm,
    string SignatureKeyId,
    string SignatureKeySha256)
{
    public static LanguagePackageInfo FromDirectory(
        LanguagePackageManifest manifest,
        string directoryPath,
        string languageFilePath)
    {
        return new LanguagePackageInfo(
            manifest,
            directoryPath,
            Path.GetFileName(languageFilePath),
            IsArchive: false,
            Signed: false,
            SignatureAlgorithm: "",
            SignatureKeyId: "",
            SignatureKeySha256: "");
    }

    public static LanguagePackageInfo FromArchive(
        LanguagePackageManifest manifest,
        string archivePath,
        LanguagePackageContainerHeader? header)
    {
        return new LanguagePackageInfo(
            manifest,
            archivePath,
            manifest.LanguageCode + ".lng",
            IsArchive: true,
            Signed: header?.Signed == true,
            SignatureAlgorithm: header?.SignatureAlgorithm ?? "",
            SignatureKeyId: header?.SignatureKeyId ?? "",
            SignatureKeySha256: header?.Signed == true
                ? LanguagePackageSignatureVerifier.GetTrustedPublicKeySha256(header.SignatureKeyId, header.SignatureAlgorithm)
                : "");
    }
}

