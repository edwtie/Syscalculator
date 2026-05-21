#nullable enable
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SharpCompress.Archives;

namespace Tiedragon.LanguagePackage;

internal static class Program
{
    private const string ToolName = "Language Package";
    private const string ToolVersion = "1.0";
    private const string MagicText = "SYSCALC-LNGPDK";
    private const int ContainerFormat = 1;
    private const string ProducerName = "Tiedragon";
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
        ".js",
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

    private static readonly HashSet<string> AllowedScriptFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "basis.js",
        "formula.js",
        "nod.js",
    };

    private static readonly Regex HtmlLinkRegex = new(
        "(?<attr>href|src)\\s*=\\s*(?<quote>[\"'])(?<path>[^\"']+)\\k<quote>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ConceptHelpMarkupRegex = new(
        "<div\\s+class\\s*=\\s*[\"'][^\"']*(concept-banner|help-warning)[^\"']*[\"'][\\s\\S]*?\\bConcept\\b[\\s\\S]*?</div>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
                "agent-compile-with-base" => AgentCompileLanguageWithBase(args),
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
        Console.WriteLine(ToolName + " " + ToolVersion);
        Console.WriteLine("Tiedragon.LanguagePackage");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  compile <input-folder> <output.lngpdk>");
        Console.WriteLine("  pack-language <input-folder> <output.lngpdk>");
        Console.WriteLine("  agent-compile <input-folder> <output.lngpdk>");
        Console.WriteLine("  agent-compile-with-base <base-folder-or-package> <input-folder> <output.lngpdk>");
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

    private static int AgentCompileLanguageWithBase(string[] args)
    {
        if (args.Length != 4)
        {
            WriteAgentError("E_ARGS", "agent-compile-with-base requires <base-folder-or-package> <input-folder> <output.lngpdk>.");
            return 2;
        }

        try
        {
            var result = BuildLanguagePackageWithBase(args[1], args[2], args[3]);
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
        if (message.Contains("base package", StringComparison.OrdinalIgnoreCase))
            return "E_BASE_PACKAGE";
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
        if (message.Contains("unsupported file type", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("unsupported package path", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("unsupported script file", StringComparison.OrdinalIgnoreCase))
        {
            return "E_FILE_UNSUPPORTED";
        }
        if (message.Contains("mojibake", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("broken internal link", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("missing image", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("concept warning markup", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("JavaScript links", StringComparison.OrdinalIgnoreCase))
        {
            return "E_QUALITY_GATE";
        }
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
            SoftwareId = manifest.SoftwareId,
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
            Product: manifest.Product,
            SoftwareId: manifest.SoftwareId,
            LanguageCode: manifest.LanguageCode,
            DisplayName: manifest.DisplayName,
            EntryCount: Directory.GetFiles(inputFolder, "*", SearchOption.AllDirectories).Length,
            PackageSha256: ComputeSha256(File.ReadAllBytes(outputPath)),
            PayloadSha256: payloadHash,
            Encrypted: false,
            Signed: false,
            BasePackage: "",
            AddedEntries: [],
            AddedLanguageKeys: []);
    }

    private static LanguagePackageBuildResult BuildLanguagePackageWithBase(
        string basePackageValue,
        string inputFolderValue,
        string outputPathValue)
    {
        var basePackage = Path.GetFullPath(basePackageValue);
        var inputFolder = Path.GetFullPath(inputFolderValue);
        if (!Directory.Exists(inputFolder))
            throw new DirectoryNotFoundException(inputFolder);

        var baseEntries = ReadPackageEntryBytes(basePackage);
        if (!baseEntries.TryGetValue("manifest.json", out var baseManifestBytes))
            throw new InvalidDataException("Base package manifest.json is missing.");

        var baseManifest = JsonSerializer.Deserialize<LanguagePackageManifest>(
            DecodeUtf8Text(baseManifestBytes),
            JsonOptions) ?? throw new InvalidDataException("Base package manifest.json is invalid.");
        ValidateManifest(baseManifest);

        var targetManifestPath = Path.Combine(inputFolder, "manifest.json");
        if (!File.Exists(targetManifestPath))
            throw new FileNotFoundException("manifest.json is required.", targetManifestPath);

        var targetManifest = JsonSerializer.Deserialize<LanguagePackageManifest>(
            File.ReadAllText(targetManifestPath),
            JsonOptions) ?? throw new InvalidDataException("manifest.json is invalid.");
        ValidateManifest(targetManifest);
        ValidateSameApp(baseManifest, targetManifest);

        var tempFolder = Path.Combine(Path.GetTempPath(), "Tiedragon.LanguagePackage", "base-merge", Guid.NewGuid().ToString("N"));
        try
        {
            CopySourceFolder(inputFolder, tempFolder);
            var addedEntries = AddMissingBaseEntries(baseEntries, baseManifest, tempFolder);
            var addedLanguageKeys = AddMissingLanguageKeys(baseEntries, baseManifest, targetManifest, tempFolder);
            var result = BuildLanguagePackage(tempFolder, outputPathValue);
            return result with
            {
                BasePackage = basePackage,
                AddedEntries = addedEntries,
                AddedLanguageKeys = addedLanguageKeys,
            };
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
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
        if (inspection.Header is not null &&
            !inspection.Header.SoftwareId.Equals(manifest.SoftwareId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Header softwareId does not match manifest softwareId.");
        }

        Console.WriteLine("valid: " + packagePath);
        Console.WriteLine("key: " + manifest.PackageKey);
        Console.WriteLine("product: " + manifest.Product);
        Console.WriteLine("softwareId: " + manifest.SoftwareId);
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

    private static IReadOnlyDictionary<string, byte[]> ReadPackageEntryBytes(string sourcePath)
    {
        if (Directory.Exists(sourcePath))
            return ReadDirectoryEntryBytes(sourcePath);
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Base package was not found.", sourcePath);

        var inspection = Inspect(sourcePath);
        ValidateArchiveEntries(inspection.Payload);

        var entries = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        using var archive = ArchiveFactory.OpenArchive(new MemoryStream(inspection.Payload, writable: false));
        foreach (var entry in archive.Entries)
        {
            if (entry.IsDirectory)
                continue;

            var name = NormalizeEntryName(entry.Key);
            ValidateEntryName(name);
            using var stream = entry.OpenEntryStream();
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            entries[name] = memory.ToArray();
        }

        return entries;
    }

    private static IReadOnlyDictionary<string, byte[]> ReadDirectoryEntryBytes(string sourceFolder)
    {
        var root = Path.GetFullPath(sourceFolder);
        var entries = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            ValidateEntryName(relative);
            entries[relative] = File.ReadAllBytes(file);
        }

        return entries;
    }

    private static void CopySourceFolder(string sourceFolder, string targetFolder)
    {
        var root = Path.GetFullPath(sourceFolder);
        foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            ValidateEntryName(relative);
            WriteEntryBytes(targetFolder, relative, File.ReadAllBytes(file));
        }
    }

    private static IReadOnlyList<string> AddMissingBaseEntries(
        IReadOnlyDictionary<string, byte[]> baseEntries,
        LanguagePackageManifest baseManifest,
        string targetFolder)
    {
        var added = new List<string>();
        foreach (var entry in baseEntries.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (entry.Key.Equals("manifest.json", StringComparison.OrdinalIgnoreCase) ||
                entry.Key.Equals("language/" + baseManifest.LanguageCode + ".lng", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var targetPath = GetSafeEntryPath(targetFolder, entry.Key);
            if (File.Exists(targetPath))
                continue;

            WriteEntryBytes(targetFolder, entry.Key, entry.Value);
            added.Add(entry.Key);
        }

        return added;
    }

    private static IReadOnlyList<string> AddMissingLanguageKeys(
        IReadOnlyDictionary<string, byte[]> baseEntries,
        LanguagePackageManifest baseManifest,
        LanguagePackageManifest targetManifest,
        string targetFolder)
    {
        var baseLanguagePath = "language/" + baseManifest.LanguageCode + ".lng";
        if (!baseEntries.TryGetValue(baseLanguagePath, out var baseLanguageBytes))
            throw new InvalidDataException(baseLanguagePath + " is missing in base package.");

        var targetLanguagePath = "language/" + targetManifest.LanguageCode + ".lng";
        var targetPath = GetSafeEntryPath(targetFolder, targetLanguagePath);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        if (!File.Exists(targetPath))
            File.WriteAllText(targetPath, "# Added from base package " + baseManifest.PackageKey + Environment.NewLine, Encoding.UTF8);

        var targetText = File.ReadAllText(targetPath, Encoding.UTF8);
        var knownKeys = ReadLanguageKeys(targetText);
        var added = new List<string>();
        var additions = new StringBuilder();
        foreach (var line in DecodeUtf8Text(baseLanguageBytes).Replace("\r\n", "\n").Split('\n'))
        {
            if (!TryReadLanguageKey(line, out var key) || knownKeys.Contains(key))
                continue;

            additions.AppendLine(line.TrimEnd('\r'));
            knownKeys.Add(key);
            added.Add(key);
        }

        if (additions.Length == 0)
            return added;

        var separator = targetText.EndsWith('\n') ? "" : Environment.NewLine;
        File.AppendAllText(
            targetPath,
            separator + "# Added from base package " + baseManifest.PackageKey + Environment.NewLine + additions,
            Encoding.UTF8);
        return added;
    }

    private static HashSet<string> ReadLanguageKeys(string text)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (TryReadLanguageKey(line, out var key))
                keys.Add(key);
        }

        return keys;
    }

    private static bool TryReadLanguageKey(string line, out string key)
    {
        key = "";
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed.StartsWith(';'))
            return false;

        var separator = trimmed.IndexOf('=');
        if (separator <= 0)
            return false;

        key = trimmed[..separator].Trim();
        return key.Length > 0;
    }

    private static string DecodeUtf8Text(byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');
    }

    private static void ValidateSameApp(LanguagePackageManifest baseManifest, LanguagePackageManifest targetManifest)
    {
        if (!baseManifest.Producer.Equals(targetManifest.Producer, StringComparison.OrdinalIgnoreCase) ||
            !baseManifest.Product.Equals(targetManifest.Product, StringComparison.OrdinalIgnoreCase) ||
            !baseManifest.SoftwareId.Equals(targetManifest.SoftwareId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Base package app identity does not match target manifest.");
        }
    }

    private static void WriteEntryBytes(string rootFolder, string entryName, byte[] bytes)
    {
        var path = GetSafeEntryPath(rootFolder, entryName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
    }

    private static string GetSafeEntryPath(string rootFolder, string entryName)
    {
        var root = Path.GetFullPath(rootFolder);
        var fullPath = Path.GetFullPath(Path.Combine(root, NormalizeEntryName(entryName).Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Unsafe path: " + entryName);

        return fullPath;
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
        var entries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(inputFolder, file).Replace('\\', '/');
            ValidateEntryName(relative);
            entries.Add(NormalizeEntryName(relative));
            var length = new FileInfo(file).Length;
            if (length > MaxEntryBytes)
                throw new InvalidDataException("File is too large: " + relative);
            totalBytes += length;
        }

        if (totalBytes > MaxTotalEntryBytes)
            throw new InvalidDataException("Input folder is too large.");
        if (!File.Exists(Path.Combine(inputFolder, "language", manifest.LanguageCode + ".lng")))
            throw new InvalidDataException($"language/{manifest.LanguageCode}.lng is required.");

        ValidateSourceQuality(inputFolder, entries);
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
        if (!IsSafeProductName(manifest.Product))
            throw new InvalidDataException("Manifest product is invalid.");
        if (!IsSafeKey(manifest.SoftwareId))
            throw new InvalidDataException("Manifest softwareId is invalid.");
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
        if (!IsSafeKey(header.SoftwareId))
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
        if (extension.Equals(".js", StringComparison.OrdinalIgnoreCase) &&
            !AllowedScriptFiles.Contains(Path.GetFileName(normalized)))
        {
            throw new InvalidDataException("Unsupported script file: " + normalized);
        }

        if (!AllowedExtensions.Contains(extension))
            throw new InvalidDataException("Unsupported file type: " + normalized);

        if (!IsAllowedPackagePath(normalized, extension))
            throw new InvalidDataException("Unsupported package path: " + normalized);
    }

    private static void ValidateSourceQuality(string inputFolder, HashSet<string> entries)
    {
        foreach (var entry in entries.Order(StringComparer.OrdinalIgnoreCase))
        {
            if (!IsTextEntry(entry))
                continue;

            var path = Path.Combine(inputFolder, entry.Replace('/', Path.DirectorySeparatorChar));
            var text = File.ReadAllText(path, Encoding.UTF8);
            ValidateTextQuality(entry, text);
            if (Path.GetExtension(entry).Equals(".html", StringComparison.OrdinalIgnoreCase))
                ValidateHtmlReferences(entry, text, entries);
        }
    }

    private static void ValidateTextQuality(string entryName, string text)
    {
        if (ContainsMojibakeMarker(text))
            throw new InvalidDataException("Encoding/mojibake detected: " + entryName);
        if (Path.GetExtension(entryName).Equals(".json", StringComparison.OrdinalIgnoreCase))
            _ = JsonDocument.Parse(text);
        if (ConceptHelpMarkupRegex.IsMatch(text))
            throw new InvalidDataException("Concept warning markup must not be stored in help content: " + entryName);
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidDataException("Empty text file: " + entryName);
    }

    private static void ValidateHtmlReferences(string entryName, string text, HashSet<string> entries)
    {
        foreach (Match match in HtmlLinkRegex.Matches(text))
        {
            var attribute = match.Groups["attr"].Value;
            var value = match.Groups["path"].Value.Trim();
            if (value.Length == 0 || IsIgnoredLink(value))
                continue;
            if (value.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("JavaScript links are not allowed: " + entryName);

            var target = ResolvePackageReference(entryName, value);
            if (target.Length == 0)
                continue;

            if (!entries.Contains(target))
            {
                var kind = attribute.Equals("src", StringComparison.OrdinalIgnoreCase) || IsImagePath(target)
                    ? "Missing image/media reference"
                    : "Broken internal link";
                throw new InvalidDataException(kind + ": " + entryName + " -> " + value);
            }

            if ((attribute.Equals("src", StringComparison.OrdinalIgnoreCase) || IsImagePath(target)) && !IsImagePath(target))
                throw new InvalidDataException("HTML media reference is not an allowed image: " + entryName + " -> " + value);
        }
    }

    private static string ResolvePackageReference(string sourceEntry, string reference)
    {
        var cleaned = reference.Split(['?', '#'], 2)[0].Replace('\\', '/').Trim();
        if (cleaned.Length == 0)
            return "";
        while (cleaned.StartsWith("./", StringComparison.Ordinal))
            cleaned = cleaned[2..];
        if (cleaned.StartsWith("/", StringComparison.Ordinal))
            return NormalizeEntryName(cleaned);

        var sourceDirectory = Path.GetDirectoryName(sourceEntry)?.Replace('\\', '/') ?? "";
        var parts = (sourceDirectory.Length == 0 ? cleaned : sourceDirectory + "/" + cleaned)
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        var stack = new List<string>();
        foreach (var part in parts)
        {
            if (part == ".")
                continue;
            if (part == "..")
            {
                if (stack.Count == 0)
                    throw new InvalidDataException("Unsafe path: " + reference);
                stack.RemoveAt(stack.Count - 1);
                continue;
            }

            stack.Add(part);
        }

        return string.Join('/', stack);
    }

    private static bool IsIgnoredLink(string value)
    {
        return value.StartsWith("#", StringComparison.Ordinal) ||
            value.StartsWith("http:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("https:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("nodpage:", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTextEntry(string entryName)
    {
        var extension = Path.GetExtension(entryName);
        return extension.Equals(".css", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".js", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".json", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".lng", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImagePath(string entryName)
    {
        var extension = Path.GetExtension(entryName);
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".svg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAllowedPackagePath(string normalized, string extension)
    {
        if (normalized.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
            return true;
        if (normalized.StartsWith("language/", StringComparison.OrdinalIgnoreCase))
            return extension.Equals(".lng", StringComparison.OrdinalIgnoreCase);
        if (normalized.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
            return IsImagePath(normalized);
        if (normalized.StartsWith("help/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("manual/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("nod/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("formula/", StringComparison.OrdinalIgnoreCase))
        {
            return extension.Equals(".css", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".js", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool ContainsMojibakeMarker(string text)
    {
        return text.Contains('\u00c3') ||
            text.Contains('\u00c2') ||
            text.Contains('\u00e2') ||
            text.Contains("\u00e4\u00b8", StringComparison.Ordinal) ||
            text.Contains("\u00e6\u2013", StringComparison.Ordinal);
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

    private static bool IsSafeProductName(string value)
    {
        return value.Length > 0 &&
            value.Length <= 96 &&
            value.All(character => char.IsLetterOrDigit(character) ||
                char.IsWhiteSpace(character) ||
                character is '.' or '-' or '_');
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
    string Product,
    string SoftwareId,
    string LanguageCode,
    string DisplayName,
    int EntryCount,
    string PackageSha256,
    string PayloadSha256,
    bool Encrypted,
    bool Signed,
    string BasePackage,
    IReadOnlyList<string> AddedEntries,
    IReadOnlyList<string> AddedLanguageKeys);

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
