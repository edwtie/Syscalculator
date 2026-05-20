#nullable enable
namespace Syscalculator.UI.WinForms;

// Zoek/commentaar: Type-overzicht: class LanguageCatalog bevat de hoofdlogica/data voor dit onderdeel.
internal sealed class LanguageCatalog
{
    // Zoek/commentaar: Type-overzicht: record LanguageInfo bevat de hoofdlogica/data voor dit onderdeel.
    public sealed record LanguageInfo(string DisplayName, string FileName, string? PackageId = null)
    {
        public string SourceLabel => PackageId is null ? FileName : PackageId + "/" + FileName;

        public bool Matches(string fileName, string? packageId)
        {
            return FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(PackageId, packageId, StringComparison.OrdinalIgnoreCase);
        }
    }

    private readonly Dictionary<string, string> _texts;
    public string FileName { get; }
    public string? PackageId { get; }

    // Zoek/commentaar: Constructor: maakt en initialiseert LanguageCatalog.
    private LanguageCatalog(Dictionary<string, string> texts, string fileName, string? packageId = null)
    {
        _texts = texts;
        FileName = fileName;
        PackageId = packageId;
    }

    // Zoek/commentaar: Laadt gegevens of instellingen voor Load.
    public static LanguageCatalog Load(string baseDirectory, string fileName)
    {
        return Load(baseDirectory, fileName, packageId: null);
    }

    public static LanguageCatalog Load(string baseDirectory, string fileName, string? packageId)
    {
        var paths = new[]
        {
            Path.Combine(baseDirectory, fileName),
            Path.Combine(baseDirectory, "Languages", fileName)
        };

        if (!string.IsNullOrWhiteSpace(packageId) &&
            LanguagePackageService.TryGetLanguageFile(baseDirectory, packageId, fileName, out var packagePath))
        {
            return new LanguageCatalog(ReadLanguageFile(packagePath), Path.GetFileName(packagePath), packageId);
        }

        foreach (var path in paths)
        {
            if (File.Exists(path))
                return new LanguageCatalog(ReadLanguageFile(path), fileName);
        }

        return new LanguageCatalog(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), fileName);
    }

    // Zoek/commentaar: Laadt gegevens of instellingen voor LoadConfigured.
    public static LanguageCatalog LoadConfigured(string baseDirectory)
    {
        var languageFile = "eng.lng";
        string? packageId = null;
        var configPath = Path.Combine(baseDirectory, "language.cfg");

        if (File.Exists(configPath))
        {
            foreach (var rawLine in File.ReadAllLines(configPath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                    continue;

                var separator = line.IndexOf('=');
                if (separator <= 0)
                    continue;

                var key = line[..separator].Trim();
                var value = line[(separator + 1)..].Trim();
                if (key.Equals("language", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
                {
                    languageFile = value.EndsWith(".lng", StringComparison.OrdinalIgnoreCase)
                        ? value
                        : value + ".lng";
                    continue;
                }

                if (key.Equals("languagePackage", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
                {
                    packageId = value;
                }
            }
        }

        return Load(baseDirectory, languageFile, packageId);
    }

    // Zoek/commentaar: Slaat gegevens of instellingen op voor SaveConfigured.
    public static void SaveConfigured(string baseDirectory, string fileName)
    {
        SaveConfigured(baseDirectory, new LanguageInfo(Path.GetFileNameWithoutExtension(fileName), fileName));
    }

    public static void SaveConfigured(string baseDirectory, LanguageInfo language)
    {
        File.WriteAllText(
            Path.Combine(baseDirectory, "language.cfg"),
            "# Active language file." + Environment.NewLine +
            "# Optional languagePackage points to a folder in LanguagePackages." + Environment.NewLine +
            "# Examples:" + Environment.NewLine +
            "# language=eng.lng" + Environment.NewLine +
            "# language=ned.lng" + Environment.NewLine +
            "# languagePackage=syscalculator.language.ned" + Environment.NewLine +
            "# language=deu.lng" + Environment.NewLine +
            "# language=spa.lng" + Environment.NewLine +
            "language=" + language.FileName + Environment.NewLine +
            (string.IsNullOrWhiteSpace(language.PackageId)
                ? ""
                : "languagePackage=" + language.PackageId + Environment.NewLine));
    }

    // Zoek/commentaar: Methode ListAvailable: centrale logica voor deze stap.
    public static IReadOnlyList<LanguageInfo> ListAvailable(string baseDirectory)
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AddLanguageFiles(files, baseDirectory);
        AddLanguageFiles(files, Path.Combine(baseDirectory, "Languages"));

        var languages = files
            .Select(pair =>
            {
                var texts = ReadLanguageFile(pair.Value);
                var displayName = texts.TryGetValue("language.name", out var name) && !string.IsNullOrWhiteSpace(name)
                    ? name
                    : Path.GetFileNameWithoutExtension(pair.Key);

                return new LanguageInfo(displayName, pair.Key);
            })
            .ToList();

        foreach (var package in LanguagePackageService.ListInstalled(baseDirectory))
        {
            var texts = ReadLanguageFile(package.LanguageFilePath);
            var displayName = !string.IsNullOrWhiteSpace(package.Manifest.NativeName)
                ? package.Manifest.NativeName
                : package.Manifest.DisplayName;
            if (string.IsNullOrWhiteSpace(displayName) &&
                texts.TryGetValue("language.name", out var name) &&
                !string.IsNullOrWhiteSpace(name))
            {
                displayName = name;
            }

            languages.Add(new LanguageInfo(displayName, package.LanguageFileName, package.Manifest.Id));
        }

        return languages
            .OrderBy(language => language.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(language => language.SourceLabel, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // Zoek/commentaar: Methode Text: centrale logica voor deze stap.
    public string Text(string key, string fallback)
    {
        return _texts.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    public bool TryText(string key, out string value)
    {
        if (_texts.TryGetValue(key, out var text) && !string.IsNullOrWhiteSpace(text))
        {
            value = text;
            return true;
        }

        value = "";
        return false;
    }

    // Zoek/commentaar: Voegt data of UI-regels toe voor AddLanguageFiles.
    private static void AddLanguageFiles(Dictionary<string, string> files, string directory)
    {
        if (!Directory.Exists(directory))
            return;

        foreach (var path in Directory.GetFiles(directory, "*.lng"))
        {
            var fileName = Path.GetFileName(path);
            if (!files.ContainsKey(fileName))
                files[fileName] = path;
        }
    }

    private static Dictionary<string, string> ReadLanguageFile(string path)
    {
        var texts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                continue;

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (key.Length > 0)
                texts[key] = value;
        }

        return texts;
    }
}
