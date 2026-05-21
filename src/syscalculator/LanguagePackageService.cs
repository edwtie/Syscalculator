#nullable enable
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SharpCompress.Archives;

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
    private const string PackageExtension = ".lngpdk";
    private const string LegacyZipExtension = ".zip";
    private const string ProducerName = "Tiedragon";
    private const string ProductName = "Syscalculator";
    private const string SoftwareId = "tiedragon.syscalculator";
    private const string PackageType = "language";
    private const int ContainerFormat = 1;
    private const int MaxHeaderBytes = 64 * 1024;
    private const int MaxEntryCount = 2048;
    private const long MaxEntryBytes = 16L * 1024 * 1024;
    private const long MaxTotalEntryBytes = 128L * 1024 * 1024;
    private const long MaxPayloadBytes = 192L * 1024 * 1024;

    private static readonly byte[] PackageMagic = Encoding.ASCII.GetBytes("SYSCALC-LNGPDK");

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
        if (!TryReadArchiveEntry(packagePath, "manifest.json", out var manifestText))
            return null;

        return JsonSerializer.Deserialize<LanguagePackageManifest>(manifestText, JsonOptions);
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

    private static bool TryReadZipPackage(string zipPath, out LanguagePackageInfo package)
    {
        package = null!;
        try
        {
            var manifest = ReadArchiveManifest(zipPath);
            if (manifest is null)
                return false;

            ValidateManifest(manifest);
            ValidatePackageEntries(zipPath);

            if (!ContainsEntry(zipPath, "language/" + manifest.LanguageCode + ".lng"))
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
        content = "";
        using var packageStream = OpenArchivePayloadStream(packagePath);
        using var archive = ArchiveFactory.OpenArchive(packageStream);
        var entry = archive.Entries.FirstOrDefault(entry =>
            !entry.IsDirectory &&
            NormalizeArchiveEntryName(entry.Key).Equals(entryName, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
            return false;
        if (entry.Size > MaxEntryBytes)
            throw new InvalidDataException("Language package entry is too large.");

        using var stream = entry.OpenEntryStream();
        using var reader = new StreamReader(stream);
        content = reader.ReadToEnd();
        return true;
    }

    private static bool ContainsEntry(string packagePath, string entryName)
    {
        using var packageStream = OpenArchivePayloadStream(packagePath);
        using var archive = ArchiveFactory.OpenArchive(packageStream);
        return archive.Entries.Any(entry =>
            !entry.IsDirectory &&
            NormalizeArchiveEntryName(entry.Key).Equals(entryName, StringComparison.OrdinalIgnoreCase));
    }

    private static void ValidatePackageEntries(string packagePath)
    {
        using var packageStream = OpenArchivePayloadStream(packagePath);
        using var archive = ArchiveFactory.OpenArchive(packageStream);
        var count = 0;
        long totalBytes = 0;
        foreach (var entry in archive.Entries)
        {
            if (entry.IsDirectory)
                continue;

            count++;
            if (count > MaxEntryCount)
                throw new InvalidDataException("Language package contains too many files.");
            if (entry.Size > MaxEntryBytes)
                throw new InvalidDataException("Language package contains a file that is too large.");

            totalBytes += Math.Max(0L, entry.Size);
            if (totalBytes > MaxTotalEntryBytes)
                throw new InvalidDataException("Language package is too large after decompression.");

            ValidateEntryName(NormalizeArchiveEntryName(entry.Key));
        }
    }

    private static Stream OpenArchivePayloadStream(string packagePath)
    {
        var source = File.OpenRead(packagePath);
        if (!TryReadWrappedPayload(source, out var payload))
            return source;

        source.Dispose();
        return new MemoryStream(payload, writable: false);
    }

    private static bool TryReadWrappedPayload(Stream source, out byte[] payload)
    {
        payload = [];
        if (!source.CanSeek || source.Length < PackageMagic.Length + sizeof(int) + sizeof(int))
            return false;

        var magic = new byte[PackageMagic.Length];
        var read = source.Read(magic, 0, magic.Length);
        if (read != PackageMagic.Length || !magic.SequenceEqual(PackageMagic))
        {
            source.Position = 0;
            return false;
        }

        using var reader = new BinaryReader(source, Encoding.UTF8, leaveOpen: true);
        var version = reader.ReadInt32();
        if (version != ContainerFormat)
            throw new InvalidDataException($"Unsupported language package container format: {version}.");

        var headerLength = reader.ReadInt32();
        if (headerLength <= 0 || headerLength > MaxHeaderBytes)
            throw new InvalidDataException("Language package header length is invalid.");

        var headerBytes = reader.ReadBytes(headerLength);
        if (headerBytes.Length != headerLength)
            throw new InvalidDataException("Language package header is incomplete.");

        var header = JsonSerializer.Deserialize<LanguagePackageContainerHeader>(
            Encoding.UTF8.GetString(headerBytes),
            JsonOptions) ?? throw new InvalidDataException("Language package header is invalid.");
        ValidateContainerHeader(header);

        var payloadLength = source.Length - source.Position;
        if (payloadLength <= 0 || payloadLength > MaxPayloadBytes)
            throw new InvalidDataException("Language package payload size is invalid.");

        payload = reader.ReadBytes((int)payloadLength);
        if (payload.Length != payloadLength)
            throw new InvalidDataException("Language package payload is incomplete.");

        if (!string.IsNullOrWhiteSpace(header.PayloadSha256))
        {
            var hash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
            if (!hash.Equals(header.PayloadSha256.Trim().ToLowerInvariant(), StringComparison.Ordinal))
                throw new InvalidDataException("Language package payload hash does not match the header.");
        }

        return true;
    }

    private static void ValidateContainerHeader(LanguagePackageContainerHeader header)
    {
        if (header.Format != ContainerFormat)
            throw new InvalidDataException($"Unsupported language package header format: {header.Format}.");
        if (!SoftwareId.Equals(header.SoftwareId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Language package is not intended for Syscalculator.");
        if (!PackageType.Equals(header.PackageType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Language package has an unsupported package type.");
        if (header.Encrypted)
            throw new InvalidDataException("Encrypted language packages are not supported yet.");
        if (!string.IsNullOrWhiteSpace(header.PayloadSha256) && !IsSha256Hex(header.PayloadSha256))
            throw new InvalidDataException("Language package payload hash is invalid.");
        if (!string.IsNullOrWhiteSpace(header.PayloadFormat) &&
            !header.PayloadFormat.Equals("zip", StringComparison.OrdinalIgnoreCase) &&
            !header.PayloadFormat.Equals("7z", StringComparison.OrdinalIgnoreCase) &&
            !header.PayloadFormat.Equals("archive", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Language package payload format is unsupported.");
        }
    }

    private static bool IsSha256Hex(string value)
    {
        return value.Trim().Length == 64 &&
            value.Trim().All(character => char.IsDigit(character) ||
                character is >= 'a' and <= 'f' ||
                character is >= 'A' and <= 'F');
    }

    private static string NormalizeArchiveEntryName(string? entryName)
    {
        return (entryName ?? "").Replace('\\', '/').TrimStart('/');
    }

    private static void ValidateManifest(LanguagePackageManifest manifest)
    {
        if (manifest.Format != CurrentFormat)
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
    public string Key { get; set; } = "";
    public string Id { get; set; } = "";
    public string Producer { get; set; } = "";
    public string Product { get; set; } = "";
    public string SoftwareId { get; set; } = "";
    public string LanguageCode { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string NativeName { get; set; } = "";
    public string AppMinVersion { get; set; } = "";
    public string PackageVersion { get; set; } = "";
    public string FallbackLanguage { get; set; } = "eng";

    public string PackageKey => string.IsNullOrWhiteSpace(Key) ? Id : Key;
}

internal sealed class LanguagePackageContainerHeader
{
    public int Format { get; set; }
    public string SoftwareId { get; set; } = "";
    public string PackageType { get; set; } = "";
    public string PayloadFormat { get; set; } = "";
    public string PayloadSha256 { get; set; } = "";
    public bool Encrypted { get; set; }
    public bool Signed { get; set; }
}
