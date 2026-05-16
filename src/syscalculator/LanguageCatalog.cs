#nullable enable
namespace Syscalculator.UI.WinForms;

// Zoek/commentaar: Type-overzicht: class LanguageCatalog bevat de hoofdlogica/data voor dit onderdeel.
internal sealed class LanguageCatalog
{
    // Zoek/commentaar: Type-overzicht: record LanguageInfo bevat de hoofdlogica/data voor dit onderdeel.
    public sealed record LanguageInfo(string DisplayName, string FileName);

    private readonly Dictionary<string, string> _texts;
    public string FileName { get; }

    // Zoek/commentaar: Constructor: maakt en initialiseert LanguageCatalog.
    private LanguageCatalog(Dictionary<string, string> texts, string fileName)
    {
        _texts = texts;
        FileName = fileName;
    }

    // Zoek/commentaar: Laadt gegevens of instellingen voor Load.
    public static LanguageCatalog Load(string baseDirectory, string fileName)
    {
        var paths = new[]
        {
            Path.Combine(baseDirectory, fileName),
            Path.Combine(baseDirectory, "Languages", fileName)
        };

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
                    break;
                }
            }
        }

        return Load(baseDirectory, languageFile);
    }

    // Zoek/commentaar: Slaat gegevens of instellingen op voor SaveConfigured.
    public static void SaveConfigured(string baseDirectory, string fileName)
    {
        File.WriteAllText(
            Path.Combine(baseDirectory, "language.cfg"),
            "# Active language file." + Environment.NewLine +
            "# Examples:" + Environment.NewLine +
            "# language=eng.lng" + Environment.NewLine +
            "# language=ned.lng" + Environment.NewLine +
            "# language=deu.lng" + Environment.NewLine +
            "# language=spa.lng" + Environment.NewLine +
            "language=" + fileName + Environment.NewLine);
    }

    // Zoek/commentaar: Methode ListAvailable: centrale logica voor deze stap.
    public static IReadOnlyList<LanguageInfo> ListAvailable(string baseDirectory)
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AddLanguageFiles(files, baseDirectory);
        AddLanguageFiles(files, Path.Combine(baseDirectory, "Languages"));

        return files
            .Select(pair =>
            {
                var texts = ReadLanguageFile(pair.Value);
                var displayName = texts.TryGetValue("language.name", out var name) && !string.IsNullOrWhiteSpace(name)
                    ? name
                    : Path.GetFileNameWithoutExtension(pair.Key);

                return new LanguageInfo(displayName, pair.Key);
            })
            .OrderBy(language => language.DisplayName, StringComparer.CurrentCultureIgnoreCase)
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
