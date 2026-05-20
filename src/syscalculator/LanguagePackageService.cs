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
        foreach (var directory in Directory.GetDirectories(root))
        {
            if (Path.GetFileName(directory).Equals(CacheDirectoryName, StringComparison.OrdinalIgnoreCase))
                continue;

            var manifest = ReadManifest(directory);
            if (manifest is null || !TryResolveLanguageFile(directory, manifest.LanguageCode, out var languagePath))
                continue;

            packages.Add(new LanguagePackageInfo(manifest, directory, languagePath));
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

        var directory = Path.Combine(GetPackageRoot(baseDirectory), packageId);
        var manifest = ReadManifest(directory);
        if (manifest is null || !TryResolveLanguageFile(directory, manifest.LanguageCode, out var languagePath))
            return false;

        package = new LanguagePackageInfo(manifest, directory, languagePath);
        return true;
    }

    public static bool TryGetLanguageFile(
        string baseDirectory,
        string packageId,
        string fileName,
        out string path)
    {
        path = "";
        if (!TryFindPackage(baseDirectory, packageId, out var package))
            return false;

        var requestedCode = Path.GetFileNameWithoutExtension(fileName);
        if (!TryResolveLanguageFile(package.DirectoryPath, requestedCode, out path))
            return false;

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

        var destination = Path.Combine(root, manifest.Id);
        var temporaryDestination = destination + ".tmp-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(temporaryDestination);

        try
        {
            foreach (var entry in archive.Entries)
                ExtractEntry(entry, temporaryDestination);

            if (!TryResolveLanguageFile(temporaryDestination, manifest.LanguageCode, out _))
                throw new InvalidDataException($"Language package does not contain language/{manifest.LanguageCode}.lng.");

            if (Directory.Exists(destination))
                Directory.Delete(destination, recursive: true);

            Directory.Move(temporaryDestination, destination);
            CopyToCache(root, zipPath, manifest.Id);
            return manifest;
        }
        catch
        {
            if (Directory.Exists(temporaryDestination))
                Directory.Delete(temporaryDestination, recursive: true);

            throw;
        }
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

    private static void ExtractEntry(ZipArchiveEntry entry, string destinationRoot)
    {
        if (string.IsNullOrWhiteSpace(entry.FullName) || entry.FullName.EndsWith('/'))
            return;

        ValidateEntryName(entry.FullName);

        var destinationRootFull = Path.GetFullPath(destinationRoot);
        var destinationPath = Path.GetFullPath(Path.Combine(destinationRootFull, entry.FullName));
        if (!destinationPath.StartsWith(destinationRootFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Language package contains a path outside the package folder.");

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        entry.ExtractToFile(destinationPath, overwrite: true);
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
    string DirectoryPath,
    string LanguageFilePath)
{
    public string LanguageFileName => Path.GetFileName(LanguageFilePath);
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
