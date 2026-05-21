#nullable enable
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SharpCompress.Archives;

namespace Tiedragon.LanguagePackage;

internal static class Program
{
    private const string MagicText = "SYSCALC-LNGPDK";
    private const int ContainerFormat = 1;
    private const string ProducerName = "Tiedragon";
    private const string ProductName = "Syscalculator";
    private const string SoftwareId = "tiedragon.syscalculator";
    private const string PackageType = "language";
    private const int MaxHeaderBytes = 64 * 1024;
    private const int MaxEntryCount = 2048;
    private const long MaxEntryBytes = 16L * 1024 * 1024;
    private const long MaxTotalEntryBytes = 128L * 1024 * 1024;
    private const long MaxPayloadBytes = 192L * 1024 * 1024;

    private static readonly byte[] Magic = Encoding.ASCII.GetBytes(MagicText);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private static readonly JsonSerializerOptions AgentJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".css",
        ".html",
        ".jpg",
        ".jpeg",
        ".json",
        ".lng",
        ".png",
        ".svg",
        ".webp",
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

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
                return Usage();

            return args[0].ToLowerInvariant() switch
            {
                "compile" => PackLanguage(args),
                "pack-language" => PackLanguage(args),
                "agent-compile" => AgentCompileLanguage(args),
                "validate" => ValidatePackage(args),
                "inspect" => InspectPackage(args),
                _ => Usage(),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("error: " + ex.Message);
            return 1;
        }
    }

    private static int Usage()
    {
        Console.WriteLine("Tiedragon.LanguagePackage");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  compile <input-folder> <output.lngpdk>");
        Console.WriteLine("  pack-language <input-folder> <output.lngpdk>");
        Console.WriteLine("  agent-compile <input-folder> <output.lngpdk>");
        Console.WriteLine("  validate <package.lngpdk>");
        Console.WriteLine("  inspect <package.lngpdk>");
        return 2;
    }

    private static int PackLanguage(string[] args)
    {
        if (args.Length != 3)
            return Usage();

        var result = BuildLanguagePackage(args[1], args[2]);

        Console.WriteLine("created: " + result.OutputPath);
        Console.WriteLine("key: " + result.PackageKey);
        Console.WriteLine("language: " + result.LanguageCode);
        Console.WriteLine("entries: " + result.EntryCount);
        Console.WriteLine("packageSha256: " + result.PackageSha256);
        Console.WriteLine("payloadSha256: " + result.PayloadSha256);
        return 0;
    }

    private static int AgentCompileLanguage(string[] args)
    {
        if (args.Length != 3)
        {
            WriteAgentError("E_ARGS", "agent-compile requires <input-folder> <output.lngpdk>.");
            return 2;
        }

        try
        {
            var result = BuildLanguagePackage(args[1], args[2]);
            Console.WriteLine(JsonSerializer.Serialize(result, AgentJsonOptions));
            return 0;
        }
        catch (Exception ex)
        {
            WriteAgentError(GetErrorCode(ex), ex.Message);
            return 1;
        }
    }

    private static void WriteAgentError(string code, string message)
    {
        var error = new LanguagePackageAgentError(false, code, message);
        Console.WriteLine(JsonSerializer.Serialize(error, AgentJsonOptions));
    }

    private static string GetErrorCode(Exception ex)
    {
        if (ex is DirectoryNotFoundException)
            return "E_INPUT_NOT_FOUND";
        if (ex is FileNotFoundException fileNotFound)
            return fileNotFound.FileName?.EndsWith("manifest.json", StringComparison.OrdinalIgnoreCase) == true
                ? "E_MANIFEST_MISSING"
                : "E_FILE_NOT_FOUND";
        if (ex is UnauthorizedAccessException or IOException)
            return "E_IO";
        if (ex is JsonException)
            return "E_MANIFEST_JSON";
        if (ex is InvalidOperationException && ex.Message.Contains(".lngpdk", StringComparison.OrdinalIgnoreCase))
            return "E_OUTPUT_EXTENSION";
        if (ex is not InvalidDataException and not InvalidOperationException)
            return "E_UNKNOWN";

        var message = ex.Message;
        if (message.Contains("manifest", StringComparison.OrdinalIgnoreCase))
            return "E_MANIFEST_INVALID";
        if (message.Contains("language/", StringComparison.OrdinalIgnoreCase) && message.Contains("required", StringComparison.OrdinalIgnoreCase))
            return "E_REQUIRED_FILE";
        if (message.Contains("absolute", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("unsafe path", StringComparison.OrdinalIgnoreCase))
        {
            return "E_PATH_UNSAFE";
        }
        if (message.Contains("blocked file type", StringComparison.OrdinalIgnoreCase))
            return "E_FILE_BLOCKED";
        if (message.Contains("unsupported file type", StringComparison.OrdinalIgnoreCase))
            return "E_FILE_UNSUPPORTED";
        if (message.Contains("too many files", StringComparison.OrdinalIgnoreCase))
            return "E_LIMIT_FILE_COUNT";
        if (message.Contains("too large", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("payload size", StringComparison.OrdinalIgnoreCase))
        {
            return "E_LIMIT_SIZE";
        }
        if (message.Contains("SHA-256", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("checksum", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("hash", StringComparison.OrdinalIgnoreCase))
        {
            return "E_CHECKSUM";
        }
        if (message.Contains("encrypted", StringComparison.OrdinalIgnoreCase))
            return "E_ENCRYPTED";
        if (message.Contains("container format", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("header format", StringComparison.OrdinalIgnoreCase))
        {
            return "E_UNSUPPORTED_FORMAT";
        }

        return "E_PACKAGE_INVALID";
    }

    private static LanguagePackageBuildResult BuildLanguagePackage(string inputFolderValue, string outputPathValue)
    {
        var inputFolder = Path.GetFullPath(inputFolderValue);
        var outputPath = Path.GetFullPath(outputPathValue);
        if (!Directory.Exists(inputFolder))
            throw new DirectoryNotFoundException(inputFolder);
        if (!Path.GetExtension(outputPath).Equals(".lngpdk", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Output file must use .lngpdk extension.");

        var manifestPath = Path.Combine(inputFolder, "manifest.json");
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("manifest.json is required.", manifestPath);

        var manifest = JsonSerializer.Deserialize<LanguagePackageManifest>(
            File.ReadAllText(manifestPath),
            JsonOptions) ?? throw new InvalidDataException("manifest.json is invalid.");
        ValidateManifest(manifest);
        ValidateSourceFolder(inputFolder, manifest);

        var payload = BuildZipPayload(inputFolder);
        var payloadHash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        var header = new LanguagePackageContainerHeader
        {
            Format = ContainerFormat,
            SoftwareId = SoftwareId,
            PackageType = PackageType,
            PayloadFormat = "zip",
            PayloadSha256 = payloadHash,
            Encrypted = false,
            Signed = false,
        };

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        WriteWrappedPackage(outputPath, header, payload);

        return new LanguagePackageBuildResult(
            Success: true,
            OutputPath: outputPath,
            PackageKey: manifest.PackageKey,
            LanguageCode: manifest.LanguageCode,
            DisplayName: manifest.DisplayName,
            EntryCount: Directory.GetFiles(inputFolder, "*", SearchOption.AllDirectories).Length,
            PackageSha256: ComputeSha256(File.ReadAllBytes(outputPath)),
            PayloadSha256: payloadHash,
            Encrypted: false,
            Signed: false);
    }

    private static int ValidatePackage(string[] args)
    {
        if (args.Length != 2)
            return Usage();

        var packagePath = Path.GetFullPath(args[1]);
        var inspection = Inspect(packagePath);
        ValidateArchiveEntries(inspection.Payload);
        var manifestText = ReadArchiveEntry(inspection.Payload, "manifest.json") ??
            throw new InvalidDataException("manifest.json is missing.");
        var manifest = JsonSerializer.Deserialize<LanguagePackageManifest>(manifestText, JsonOptions) ??
            throw new InvalidDataException("manifest.json is invalid.");
        ValidateManifest(manifest);

        if (ReadArchiveEntry(inspection.Payload, "language/" + manifest.LanguageCode + ".lng") is null)
            throw new InvalidDataException($"language/{manifest.LanguageCode}.lng is missing.");

        Console.WriteLine("valid: " + packagePath);
        Console.WriteLine("key: " + manifest.PackageKey);
        Console.WriteLine("language: " + manifest.LanguageCode);
        Console.WriteLine("wrapped: " + inspection.IsWrapped);
        Console.WriteLine("packageSha256: " + inspection.PackageSha256);
        Console.WriteLine("payloadSha256: " + ComputeSha256(inspection.Payload));
        return 0;
    }

    private static int InspectPackage(string[] args)
    {
        if (args.Length != 2)
            return Usage();

        var packagePath = Path.GetFullPath(args[1]);
        var inspection = Inspect(packagePath);
        var manifestText = ReadArchiveEntry(inspection.Payload, "manifest.json");
        var manifest = manifestText is null
            ? null
            : JsonSerializer.Deserialize<LanguagePackageManifest>(manifestText, JsonOptions);

        Console.WriteLine("file: " + packagePath);
        Console.WriteLine("wrapped: " + inspection.IsWrapped);
        Console.WriteLine("packageSha256: " + inspection.PackageSha256);
        Console.WriteLine("payloadBytes: " + inspection.Payload.Length);
        Console.WriteLine("payloadSha256: " + ComputeSha256(inspection.Payload));
        if (inspection.Header is not null)
        {
            Console.WriteLine("softwareId: " + inspection.Header.SoftwareId);
            Console.WriteLine("packageType: " + inspection.Header.PackageType);
            Console.WriteLine("payloadFormat: " + inspection.Header.PayloadFormat);
            Console.WriteLine("headerSha256: " + inspection.Header.PayloadSha256);
            Console.WriteLine("encrypted: " + inspection.Header.Encrypted);
            Console.WriteLine("signed: " + inspection.Header.Signed);
        }

        if (manifest is not null)
        {
            Console.WriteLine("manifestKey: " + manifest.PackageKey);
            Console.WriteLine("displayName: " + manifest.DisplayName);
            Console.WriteLine("languageCode: " + manifest.LanguageCode);
        }

        Console.WriteLine("entries:");
        foreach (var entry in ListEntries(inspection.Payload).OrderBy(entry => entry, StringComparer.OrdinalIgnoreCase))
            Console.WriteLine("  " + entry);

        return 0;
    }

    private static byte[] BuildZipPayload(string inputFolder)
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in Directory.GetFiles(inputFolder, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var relative = Path.GetRelativePath(inputFolder, file).Replace('\\', '/');
                ValidateEntryName(relative);

                var info = new FileInfo(file);
                if (info.Length > MaxEntryBytes)
                    throw new InvalidDataException("File is too large: " + relative);

                var entry = archive.CreateEntry(relative, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var source = File.OpenRead(file);
                source.CopyTo(entryStream);
            }
        }

        return memory.ToArray();
    }

    private static void WriteWrappedPackage(string outputPath, LanguagePackageContainerHeader header, byte[] payload)
    {
        var headerBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header, JsonOptions));
        if (headerBytes.Length > MaxHeaderBytes)
            throw new InvalidDataException("Header is too large.");
        if (payload.Length > MaxPayloadBytes)
            throw new InvalidDataException("Payload is too large.");

        using var output = File.Create(outputPath);
        output.Write(Magic);
        using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);
        writer.Write(ContainerFormat);
        writer.Write(headerBytes.Length);
        writer.Write(headerBytes);
        writer.Write(payload);
    }

    private static PackageInspection Inspect(string packagePath)
    {
        if (!File.Exists(packagePath))
            throw new FileNotFoundException(packagePath);

        var bytes = File.ReadAllBytes(packagePath);
        var packageSha256 = ComputeSha256(bytes);
        if (!TryUnwrap(bytes, out var header, out var payload))
            payload = bytes;

        return new PackageInspection(header is not null, header, packageSha256, payload);
    }

    private static string ComputeSha256(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static bool TryUnwrap(byte[] packageBytes, out LanguagePackageContainerHeader? header, out byte[] payload)
    {
        header = null;
        payload = packageBytes;
        if (packageBytes.Length < Magic.Length + sizeof(int) + sizeof(int) ||
            !packageBytes.AsSpan(0, Magic.Length).SequenceEqual(Magic))
        {
            return false;
        }

        using var memory = new MemoryStream(packageBytes);
        memory.Position = Magic.Length;
        using var reader = new BinaryReader(memory, Encoding.UTF8, leaveOpen: true);
        var version = reader.ReadInt32();
        if (version != ContainerFormat)
            throw new InvalidDataException($"Unsupported container format: {version}.");

        var headerLength = reader.ReadInt32();
        if (headerLength <= 0 || headerLength > MaxHeaderBytes)
            throw new InvalidDataException("Invalid header length.");

        var headerBytes = reader.ReadBytes(headerLength);
        header = JsonSerializer.Deserialize<LanguagePackageContainerHeader>(
            Encoding.UTF8.GetString(headerBytes),
            JsonOptions) ?? throw new InvalidDataException("Invalid header JSON.");
        ValidateContainerHeader(header);

        payload = reader.ReadBytes((int)(memory.Length - memory.Position));
        if (payload.Length <= 0 || payload.Length > MaxPayloadBytes)
            throw new InvalidDataException("Invalid payload size.");

        if (!string.IsNullOrWhiteSpace(header.PayloadSha256))
        {
            var hash = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
            if (!hash.Equals(header.PayloadSha256.Trim().ToLowerInvariant(), StringComparison.Ordinal))
                throw new InvalidDataException("Payload SHA-256 does not match header.");
        }

        return true;
    }

    private static string? ReadArchiveEntry(byte[] payload, string entryName)
    {
        using var archive = ArchiveFactory.OpenArchive(new MemoryStream(payload, writable: false));
        var entry = archive.Entries.FirstOrDefault(entry =>
            !entry.IsDirectory &&
            NormalizeEntryName(entry.Key).Equals(entryName, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
            return null;
        if (entry.Size > MaxEntryBytes)
            throw new InvalidDataException("Archive entry is too large: " + entryName);

        using var stream = entry.OpenEntryStream();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static IReadOnlyList<string> ListEntries(byte[] payload)
    {
        using var archive = ArchiveFactory.OpenArchive(new MemoryStream(payload, writable: false));
        return archive.Entries
            .Where(entry => !entry.IsDirectory)
            .Select(entry => NormalizeEntryName(entry.Key))
            .ToList();
    }

    private static void ValidateSourceFolder(string inputFolder, LanguagePackageManifest manifest)
    {
        var files = Directory.GetFiles(inputFolder, "*", SearchOption.AllDirectories);
        if (files.Length > MaxEntryCount)
            throw new InvalidDataException("Too many files.");

        long totalBytes = 0;
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(inputFolder, file).Replace('\\', '/');
            ValidateEntryName(relative);
            var length = new FileInfo(file).Length;
            if (length > MaxEntryBytes)
                throw new InvalidDataException("File is too large: " + relative);
            totalBytes += length;
        }

        if (totalBytes > MaxTotalEntryBytes)
            throw new InvalidDataException("Input folder is too large.");
        if (!File.Exists(Path.Combine(inputFolder, "language", manifest.LanguageCode + ".lng")))
            throw new InvalidDataException($"language/{manifest.LanguageCode}.lng is required.");
    }

    private static void ValidateArchiveEntries(byte[] payload)
    {
        using var archive = ArchiveFactory.OpenArchive(new MemoryStream(payload, writable: false));
        var count = 0;
        long totalBytes = 0;
        foreach (var entry in archive.Entries)
        {
            if (entry.IsDirectory)
                continue;

            count++;
            if (count > MaxEntryCount)
                throw new InvalidDataException("Package contains too many files.");
            if (entry.Size > MaxEntryBytes)
                throw new InvalidDataException("Package entry is too large: " + entry.Key);
            totalBytes += Math.Max(0L, entry.Size);
            if (totalBytes > MaxTotalEntryBytes)
                throw new InvalidDataException("Package is too large after decompression.");
            ValidateEntryName(NormalizeEntryName(entry.Key));
        }
    }

    private static void ValidateManifest(LanguagePackageManifest manifest)
    {
        if (manifest.Format != 1)
            throw new InvalidDataException("Unsupported manifest format.");
        if (!ProducerName.Equals(manifest.Producer, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Manifest producer must be Tiedragon.");
        if (!ProductName.Equals(manifest.Product, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Manifest product must be Syscalculator.");
        if (!SoftwareId.Equals(manifest.SoftwareId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Manifest softwareId must be tiedragon.syscalculator.");
        if (!IsSafeKey(manifest.PackageKey))
            throw new InvalidDataException("Manifest key is invalid.");
        if (!IsSafeLanguageCode(manifest.LanguageCode))
            throw new InvalidDataException("Language code is invalid.");
        if (string.IsNullOrWhiteSpace(manifest.DisplayName))
            throw new InvalidDataException("displayName is required.");
    }

    private static void ValidateContainerHeader(LanguagePackageContainerHeader header)
    {
        if (header.Format != ContainerFormat)
            throw new InvalidDataException("Unsupported header format.");
        if (!SoftwareId.Equals(header.SoftwareId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Unsupported softwareId.");
        if (!PackageType.Equals(header.PackageType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Unsupported packageType.");
        if (header.Encrypted)
            throw new InvalidDataException("Encrypted packages are not supported yet.");
        if (!string.IsNullOrWhiteSpace(header.PayloadSha256) && !IsSha256Hex(header.PayloadSha256))
            throw new InvalidDataException("Invalid payloadSha256.");
    }

    private static void ValidateEntryName(string entryName)
    {
        if (Path.IsPathRooted(entryName))
            throw new InvalidDataException("Absolute paths are not allowed: " + entryName);

        var normalized = NormalizeEntryName(entryName);
        if (normalized.Length == 0 || normalized.Split('/').Any(part => part == ".."))
            throw new InvalidDataException("Unsafe path: " + entryName);

        var extension = Path.GetExtension(normalized);
        if (BlockedExtensions.Contains(extension))
            throw new InvalidDataException("Blocked file type: " + normalized);
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidDataException("Unsupported file type: " + normalized);
    }

    private static string NormalizeEntryName(string? entryName)
    {
        return (entryName ?? "").Replace('\\', '/').TrimStart('/');
    }

    private static bool IsSafeKey(string value)
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

    private static bool IsSha256Hex(string value)
    {
        var text = value.Trim();
        return text.Length == 64 &&
            text.All(character => char.IsDigit(character) ||
                character is >= 'a' and <= 'f' ||
                character is >= 'A' and <= 'F');
    }
}

internal sealed record PackageInspection(
    bool IsWrapped,
    LanguagePackageContainerHeader? Header,
    string PackageSha256,
    byte[] Payload);

internal sealed record LanguagePackageBuildResult(
    bool Success,
    string OutputPath,
    string PackageKey,
    string LanguageCode,
    string DisplayName,
    int EntryCount,
    string PackageSha256,
    string PayloadSha256,
    bool Encrypted,
    bool Signed);

internal sealed record LanguagePackageAgentError(
    bool Success,
    string Code,
    string Error);

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
