#nullable enable
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Syscalculator.UI.WinForms;

internal sealed class RuntimeDiagnostics
{
    private static readonly string[] DefaultFrameworkNames =
    {
        "Microsoft.NETCore.App",
        "Microsoft.WindowsDesktop.App"
    };

    private RuntimeDiagnostics(
        string appRuntimeDescription,
        Architecture processArchitecture,
        IReadOnlyList<RuntimeFrameworkInfo> frameworks)
    {
        AppRuntimeDescription = appRuntimeDescription;
        ProcessArchitecture = processArchitecture;
        Frameworks = frameworks;
    }

    public string AppRuntimeDescription { get; }

    public Architecture ProcessArchitecture { get; }

    public IReadOnlyList<RuntimeFrameworkInfo> Frameworks { get; }

    public static RuntimeDiagnostics Capture()
    {
        var bundled = ReadRuntimeConfigFrameworks();
        if (bundled.Count == 0)
        {
            var version = Environment.Version.ToString();
            bundled.Add(new RuntimeFrameworkVersion("Microsoft.NETCore.App", version));
        }

        foreach (var frameworkName in DefaultFrameworkNames)
        {
            if (!bundled.Any(framework => framework.Name.Equals(frameworkName, StringComparison.OrdinalIgnoreCase)))
            {
                bundled.Add(new RuntimeFrameworkVersion(frameworkName, "-"));
            }
        }

        var processArchitecture = RuntimeInformation.ProcessArchitecture;
        var frameworks = bundled
            .OrderBy(framework => Array.IndexOf(DefaultFrameworkNames, framework.Name) < 0 ? 99 : Array.IndexOf(DefaultFrameworkNames, framework.Name))
            .ThenBy(framework => framework.Name, StringComparer.OrdinalIgnoreCase)
            .Select(framework =>
            {
                var installed = FindHighestInstalledVersion(framework.Name, processArchitecture);
                return RuntimeFrameworkInfo.Create(framework.Name, framework.Version, installed);
            })
            .ToList();

        return new RuntimeDiagnostics(RuntimeInformation.FrameworkDescription, processArchitecture, frameworks);
    }

    public string FormatBundledForDisplay()
    {
        return string.Join("; ", Frameworks.Select(framework => $"{framework.Name} {framework.BundledVersion}"));
    }

    public string FormatInstalledForDisplay(Func<string, string, string> text)
    {
        return string.Join("; ", Frameworks.Select(framework => framework.FormatDisplayText(text)));
    }

    public string FormatStatusForDisplay(Func<string, string, string> text)
    {
        if (Frameworks.Any(framework => framework.Relation == RuntimeVersionRelation.Newer))
        {
            return text("about.runtime_status.newer", "Installed runtime is newer than the bundled runtime.");
        }

        if (Frameworks.Any(framework => framework.Relation == RuntimeVersionRelation.Older))
        {
            return text("about.runtime_status.older", "Installed runtime is older than bundled; startup is still OK because Syscalculator is self-contained.");
        }

        if (Frameworks.Any(framework => framework.Relation == RuntimeVersionRelation.NotInstalled))
        {
            return text("about.runtime_status.not_installed", "No separate shared runtime was found; that is OK because Syscalculator is self-contained.");
        }

        if (Frameworks.All(framework => framework.Relation == RuntimeVersionRelation.Same))
        {
            return text("about.runtime_status.same", "Installed runtime is equal to the bundled runtime.");
        }

        return text("about.runtime_status.checked", "Runtime check completed; Syscalculator uses its bundled self-contained runtime.");
    }

    public string ToSupportText(Func<string, string, string> text)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{text("runtime.support.mode", "Runtime mode")}: {text("runtime.support.self_contained", "self-contained")}");
        sb.AppendLine($"{text("runtime.support.application", "Application runtime")}: {AppRuntimeDescription}");
        sb.AppendLine($"{text("runtime.support.process_architecture", "Process architecture")}: {ProcessArchitecture}");
        foreach (var framework in Frameworks)
        {
            sb.AppendLine(string.Format(
                text("runtime.support.framework", "Runtime {0}: bundled {1}; {2}"),
                framework.Name,
                framework.BundledVersion,
                framework.FormatSupportText(text)));
        }

        return sb.ToString();
    }

    private static List<RuntimeFrameworkVersion> ReadRuntimeConfigFrameworks()
    {
        var frameworks = new List<RuntimeFrameworkVersion>();
        var path = GetRuntimeConfigPath();
        if (path is null || !File.Exists(path))
        {
            return frameworks;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (!document.RootElement.TryGetProperty("runtimeOptions", out var runtimeOptions))
            {
                return frameworks;
            }

            AddFrameworkArray(runtimeOptions, "includedFrameworks", frameworks);
            AddFrameworkArray(runtimeOptions, "frameworks", frameworks);

            if (runtimeOptions.TryGetProperty("framework", out var framework))
            {
                AddFramework(framework, frameworks);
            }
        }
        catch
        {
            frameworks.Clear();
        }

        return frameworks
            .GroupBy(framework => framework.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static string? GetRuntimeConfigPath()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            return Path.Combine(
                Path.GetDirectoryName(processPath) ?? AppContext.BaseDirectory,
                Path.GetFileNameWithoutExtension(processPath) + ".runtimeconfig.json");
        }

        var entryName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
        return string.IsNullOrWhiteSpace(entryName)
            ? null
            : Path.Combine(AppContext.BaseDirectory, entryName + ".runtimeconfig.json");
    }

    private static void AddFrameworkArray(JsonElement runtimeOptions, string propertyName, List<RuntimeFrameworkVersion> frameworks)
    {
        if (!runtimeOptions.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var framework in array.EnumerateArray())
        {
            AddFramework(framework, frameworks);
        }
    }

    private static void AddFramework(JsonElement framework, List<RuntimeFrameworkVersion> frameworks)
    {
        if (!framework.TryGetProperty("name", out var nameElement) ||
            !framework.TryGetProperty("version", out var versionElement))
        {
            return;
        }

        var name = nameElement.GetString();
        var version = versionElement.GetString();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version))
        {
            return;
        }

        frameworks.Add(new RuntimeFrameworkVersion(name.Trim(), version.Trim()));
    }

    private static string? FindHighestInstalledVersion(string frameworkName, Architecture architecture)
    {
        var candidates = GetDotNetRootCandidates(architecture)
            .Select(root => Path.Combine(root, "shared", frameworkName))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        string? highestText = null;
        Version? highestVersion = null;

        foreach (var sharedFrameworkDir in candidates)
        {
            if (!Directory.Exists(sharedFrameworkDir))
            {
                continue;
            }

            foreach (var versionDir in Directory.EnumerateDirectories(sharedFrameworkDir))
            {
                var versionText = Path.GetFileName(versionDir);
                if (!TryParseVersion(versionText, out var version))
                {
                    continue;
                }

                if (highestVersion is null || version > highestVersion)
                {
                    highestVersion = version;
                    highestText = versionText;
                }
            }
        }

        return highestText;
    }

    private static IEnumerable<string> GetDotNetRootCandidates(Architecture architecture)
    {
        var roots = new List<string>();

        AddRoot(roots, Environment.GetEnvironmentVariable("DOTNET_ROOT"));

        switch (architecture)
        {
            case Architecture.X86:
                AddRoot(roots, Environment.GetEnvironmentVariable("DOTNET_ROOT_X86"));
                AddRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
                break;
            case Architecture.Arm64:
                AddRoot(roots, Environment.GetEnvironmentVariable("DOTNET_ROOT_ARM64"));
                AddRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
                break;
            case Architecture.X64:
            default:
                AddRoot(roots, Environment.GetEnvironmentVariable("DOTNET_ROOT_X64"));
                AddRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
                break;
        }

        foreach (var root in roots)
        {
            if (Path.GetFileName(root).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            {
                yield return root;
            }
            else
            {
                yield return Path.Combine(root, "dotnet");
            }
        }
    }

    private static void AddRoot(List<string> roots, string? root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        root = root.Trim();
        if (!roots.Contains(root, StringComparer.OrdinalIgnoreCase))
        {
            roots.Add(root);
        }
    }

    internal static bool TryParseVersion(string? text, out Version version)
    {
        version = new Version(0, 0);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim();
        var suffixIndex = normalized.IndexOfAny(new[] { '-', '+' });
        if (suffixIndex > 0)
        {
            normalized = normalized[..suffixIndex];
        }

        if (!Version.TryParse(normalized, out var parsed) || parsed is null)
        {
            return false;
        }

        version = parsed;
        return true;
    }

    private sealed record RuntimeFrameworkVersion(string Name, string Version);
}

internal sealed class RuntimeFrameworkInfo
{
    private RuntimeFrameworkInfo(
        string name,
        string bundledVersion,
        string? installedVersion,
        RuntimeVersionRelation relation)
    {
        Name = name;
        BundledVersion = bundledVersion;
        InstalledVersion = installedVersion;
        Relation = relation;
    }

    public string Name { get; }

    public string BundledVersion { get; }

    public string? InstalledVersion { get; }

    public RuntimeVersionRelation Relation { get; }

    public string FormatDisplayText(Func<string, string, string> text)
    {
        return Relation switch
        {
            RuntimeVersionRelation.NotInstalled => string.Format(text("about.runtime.not_installed", "{0}: not found (not required)"), Name),
            RuntimeVersionRelation.Older => string.Format(text("about.runtime.older", "{0}: {1} (older than bundled {2})"), Name, InstalledVersion, BundledVersion),
            RuntimeVersionRelation.Same => string.Format(text("about.runtime.same", "{0}: {1} (same as bundled)"), Name, InstalledVersion),
            RuntimeVersionRelation.Newer => string.Format(text("about.runtime.newer", "{0}: {1} (newer than bundled {2})"), Name, InstalledVersion, BundledVersion),
            _ => string.Format(text("about.runtime.unknown", "{0}: {1}"), Name, InstalledVersion ?? "-")
        };
    }

    public string FormatSupportText(Func<string, string, string> text)
    {
        return Relation switch
        {
            RuntimeVersionRelation.NotInstalled => text("runtime.support.not_installed", "shared runtime not found; not required because the app is self-contained"),
            RuntimeVersionRelation.Older => string.Format(text("runtime.support.older", "installed {0} (older than bundled)"), InstalledVersion),
            RuntimeVersionRelation.Same => string.Format(text("runtime.support.same", "installed {0} (same as bundled)"), InstalledVersion),
            RuntimeVersionRelation.Newer => string.Format(text("runtime.support.newer", "installed {0} (newer than bundled)"), InstalledVersion),
            _ => string.Format(text("runtime.support.unknown", "installed {0}"), InstalledVersion ?? "-")
        };
    }

    public static RuntimeFrameworkInfo Create(string name, string bundledVersion, string? installedVersion)
    {
        var relation = RuntimeVersionRelation.Unknown;
        if (string.IsNullOrWhiteSpace(installedVersion))
        {
            relation = RuntimeVersionRelation.NotInstalled;
        }
        else if (RuntimeDiagnostics.TryParseVersion(bundledVersion, out var bundled) &&
                 RuntimeDiagnostics.TryParseVersion(installedVersion, out var installed))
        {
            relation = installed.CompareTo(bundled) switch
            {
                < 0 => RuntimeVersionRelation.Older,
                0 => RuntimeVersionRelation.Same,
                > 0 => RuntimeVersionRelation.Newer
            };
        }

        return new RuntimeFrameworkInfo(name, bundledVersion, installedVersion, relation);
    }
}

internal enum RuntimeVersionRelation
{
    Unknown,
    NotInstalled,
    Older,
    Same,
    Newer
}
