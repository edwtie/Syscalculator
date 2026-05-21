#nullable enable
using System.IO.Compression;
using System.Text.Json;

namespace Syscalculator.UI.WinForms;

// Copyright (c) Tiedragon. All rights reserved.
//
// Language packages keep translated text, help, manuals, and assets together.
// The service is intentionally small and strict: a package is data, never code.
internal static class LanguagePackageService
{
    private const int CurrentFormat = 1;
    private const string PackageDirectoryName = "LanguagePackages";
    private const string CacheDirectoryName = "Cache";

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
        foreach (var zipPath in EnumeratePackageZipFiles(root))
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

            if (packages.Any(package => package.Manifest.Id.Equals(manifest.Id, StringComparison.OrdinalIgnoreCase)))
                continue;

            packages.Add(LanguagePackageInfo.FromDirectory(manifest, directory, languagePath));
        }

        return packages
            .OrderBy(package => package.Manifest.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(package => package.Manifest.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool TryFindPackage(
        string baseDirectory,
        string packageId,
        out LanguagePackageInfo package)
    {
        package = null!;
        if (!IsSafePackageId(packageId))
            return false;

        var root = GetPackageRoot(baseDirectory);
        foreach (var zipPath in EnumeratePackageZipFiles(root))
        {
            if (TryReadZipPackage(zipPath, out var zipPackage) &&
                zipPackage.Manifest.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase))
            {
                package = zipPackage;
                return true;
            }
        }

        var directory = Path.Combine(GetPackageRoot(baseDirectory), packageId);
        var manifest = ReadManifest(directory);
        if (manifest is null || !TryResolveLanguageFile(directory, manifest.LanguageCode, out var languagePath))
            return false;

        package = LanguagePackageInfo.FromDirectory(manifest, directory, languagePath);
        return true;
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

    public static LanguagePackageManifest Install(string baseDirectory, string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var manifest = ReadManifest(archive) ??
            throw new InvalidDataException("Language package manifest.json is missing or invalid.");

        ValidateManifest(manifest);

        var root = GetPackageRoot(baseDirectory);
        Directory.CreateDirectory(root);

        foreach (var entry in archive.Entries)
        {
            if (!string.IsNullOrWhiteSpace(entry.FullName) && !entry.FullName.EndsWith('/'))
                ValidateEntryName(entry.FullName);
        }

        if (!ContainsEntry(archive, "language/" + manifest.LanguageCode + ".lng"))
            throw new InvalidDataException($"Language package does not contain language/{manifest.LanguageCode}.lng.");

        CopyToCache(root, zipPath, manifest.Id);
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

    private static LanguagePackageManifest? ReadManifest(ZipArchive archive)
    {
        var entry = archive.Entries.FirstOrDefault(entry =>
            entry.FullName.Equals("manifest.json", StringComparison.OrdinalIgnoreCase));
        if (entry is null)
            return null;

        using var stream = entry.Open();
        return JsonSerializer.Deserialize<LanguagePackageManifest>(stream, JsonOptions);
    }

    private static IEnumerable<string> EnumeratePackageZipFiles(string root)
    {
        foreach (var path in Directory.GetFiles(root, "*.zip"))
            yield return path;

        var cache = Path.Combine(root, CacheDirectoryName);
        if (!Directory.Exists(cache))
            yield break;

        foreach (var path in Directory.GetFiles(cache, "*.zip"))
            yield return path;
    }

    private static bool TryReadZipPackage(string zipPath, out LanguagePackageInfo package)
    {
        package = null!;
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var manifest = ReadManifest(archive);
            if (manifest is null)
                return false;

            ValidateManifest(manifest);
            foreach (var entry in archive.Entries)
            {
                if (!string.IsNullOrWhiteSpace(entry.FullName) && !entry.FullName.EndsWith('/'))
                    ValidateEntryName(entry.FullName);
            }

            if (!ContainsEntry(archive, "language/" + manifest.LanguageCode + ".lng"))
                return false;

            package = LanguagePackageInfo.FromArchive(manifest, zipPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadZipEntry(string zipPath, string entryName, out string content)
    {
        content = "";
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.Entries.FirstOrDefault(entry =>
                entry.FullName.Equals(entryName, StringComparison.OrdinalIgnoreCase));
            if (entry is null)
                return false;

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            content = reader.ReadToEnd();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ContainsEntry(ZipArchive archive, string entryName)
    {
        return archive.Entries.Any(entry =>
            entry.FullName.Equals(entryName, StringComparison.OrdinalIgnoreCase));
    }

    private static void ValidateManifest(LanguagePackageManifest manifest)
    {
        if (manifest.Format != CurrentFormat)
            throw new InvalidDataException($"Unsupported language package format: {manifest.Format}.");
        if (!IsSafePackageId(manifest.Id))
            throw new InvalidDataException("Language package id contains unsafe characters.");
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

    private static bool IsSafePackageId(string value)
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

    private static void CopyToCache(string root, string zipPath, string packageId)
    {
        var cache = Path.Combine(root, CacheDirectoryName);
        Directory.CreateDirectory(cache);
        File.Copy(zipPath, Path.Combine(cache, packageId + ".zip"), overwrite: true);
    }
}

internal sealed record LanguagePackageInfo(
    LanguagePackageManifest Manifest,
    string PackagePath,
    string LanguageFileName,
    bool IsArchive)
{
    public static LanguagePackageInfo FromDirectory(
        LanguagePackageManifest manifest,
        string directoryPath,
        string languageFilePath)
    {
        return new LanguagePackageInfo(manifest, directoryPath, Path.GetFileName(languageFilePath), IsArchive: false);
    }

    public static LanguagePackageInfo FromArchive(LanguagePackageManifest manifest, string archivePath)
    {
        return new LanguagePackageInfo(manifest, archivePath, manifest.LanguageCode + ".lng", IsArchive: true);
    }
}

internal sealed class LanguagePackageManifest
{
    public int Format { get; set; }
    public string Id { get; set; } = "";
    public string LanguageCode { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string NativeName { get; set; } = "";
    public string AppMinVersion { get; set; } = "";
    public string PackageVersion { get; set; } = "";
    public string FallbackLanguage { get; set; } = "eng";
}
