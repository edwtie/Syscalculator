using NodSystem.Core;

namespace Syscalculator.UI.WinForms;

/// <summary>
/// Een item uit freesyscal.cfg.
/// Oude VB6-regel:
/// displayName,nodPath,*
/// </summary>
public sealed class NodCatalogItem
{
    public string DisplayName { get; set; } = "";
    public string NodPath { get; set; } = "";
    public bool IsDefault { get; set; }

    // Zoek/commentaar: Methode ToString: centrale logica voor deze stap.
    public override string ToString() => DisplayName;
}

/// <summary>
/// Leest en schrijft freesyscal.cfg.
/// Dit vervangt de oude VB6-configuratie rond Form3/Form4.
/// </summary>
public sealed class NodCatalogService
{
    public string BaseDirectory { get; }
    public string CatalogPath { get; }

    // Zoek/commentaar: Constructor: maakt en initialiseert NodCatalogService.
    public NodCatalogService(string baseDirectory)
    {
        BaseDirectory = baseDirectory;
        CatalogPath = Path.Combine(baseDirectory, "freesyscal.cfg");
    }

    // Zoek/commentaar: Laadt gegevens of instellingen voor Load.
    public List<NodCatalogItem> Load()
    {
        EnsureExampleFiles();
        AppendDiscoveredConverters();

        var items = new List<NodCatalogItem>();

        foreach (var line in File.ReadAllLines(CatalogPath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.TrimStart().StartsWith("'"))
                continue;

            var parts = line.Split(',');
            if (parts.Length < 2)
                continue;

            items.Add(new NodCatalogItem
            {
                DisplayName = parts[0].Trim(),
                NodPath = parts[1].Trim(),
                IsDefault = parts.Length >= 3 && parts[2].Trim() == "*"
            });
        }

        return items;
    }

    // Zoek/commentaar: Slaat gegevens of instellingen op voor Save.
    public void Save(IEnumerable<NodCatalogItem> items)
    {
        var lines = items.Select(i => $"{i.DisplayName},{i.NodPath},{(i.IsDefault ? "*" : "")}");
        File.WriteAllLines(CatalogPath, lines);
    }

    // Zoek/commentaar: Methode ResolveNodPath: centrale logica voor deze stap.
    public string ResolveNodPath(NodCatalogItem item)
    {
        if (Path.IsPathRooted(item.NodPath))
            return item.NodPath;

        return Path.GetFullPath(Path.Combine(BaseDirectory, item.NodPath));
    }

    // Zoek/commentaar: Laadt gegevens of instellingen voor LoadDocument.
    public NodDocument LoadDocument(NodCatalogItem item)
    {
        var path = ResolveNodPath(item);
        var text = NodTextNormalizer.Normalize(File.ReadAllText(path));
        return NodParser.Parse(text);
    }

    // Zoek/commentaar: Zorgt dat benodigde bestanden of data bestaan voor EnsureExampleFiles.
    private void EnsureExampleFiles()
    {
        Directory.CreateDirectory(Path.Combine(BaseDirectory, "Converters"));

        if (!File.Exists(CatalogPath))
        {
            File.WriteAllText(CatalogPath, """
            Celsius naar Fahrenheit,Converters/celsius_fahrenheit.nod,*
            Ans maal e kwadraat,Converters/e2.nod,
            Sinus graden,Converters/sinus_graden.nod,
            Postcode naar adres demo,Converters/postcode_adres_demo.nod,
            Nederlands naar Engels demo,Converters/nederlands_engels_demo.nod,
            Operatie Decibel 1995 volledig,Converters/operatie_decibel_1995_demo.nod,
            """);
        }
        else
        {
            AppendCatalogEntryIfMissing("Postcode naar adres demo", "Converters/postcode_adres_demo.nod", isDefault: false);
            AppendCatalogEntryIfMissing("Nederlands naar Engels demo", "Converters/nederlands_engels_demo.nod", isDefault: false);
            AppendCatalogEntryIfMissing("Operatie Decibel 1995 volledig", "Converters/operatie_decibel_1995_demo.nod", isDefault: false);
        }

        WriteIfMissing(Path.Combine(BaseDirectory, "Converters", "celsius_fahrenheit.nod"), """
        Name Celsius naar Fahrenheit
        input1 Celsius
        input2 Fahrenheit
        Symb3 C
        Symb4 F
        format ##.00
        math ans * 1,8
        math ans + 32
        end
        """);

        WriteIfMissing(Path.Combine(BaseDirectory, "Converters", "e2.nod"), """
        Name Ans maal e kwadraat
        input1 Getal
        input2 Resultaat
        format ##.00
        math ans * e^2
        end
        """);

        WriteIfMissing(Path.Combine(BaseDirectory, "Converters", "sinus_graden.nod"), """
        Name Sinus graden
        input1 Graden
        input2 Sinus
        format ##.00
        math sind(ans)
        end
        """);

        WriteIfMissing(Path.Combine(BaseDirectory, "Converters", "postcode_adres_demo.nod"), """
        Name Postcode naar adres demo
        URLN Postcode naar adres demo
        input1 Postcode huisnummer
        input2 Adres
        trans "2566 GB 215","Nieboerweg 215, 2566 GB Den Haag"
        trans "2566 GB 217","Nieboerweg 217, 2566 GB Den Haag"
        trans "2566 GB 219","Nieboerweg 219, 2566 GB Den Haag"
        trans "2566 GB 221","Nieboerweg 221, 2566 GB Den Haag"
        trans "2566 GB 223","Nieboerweg 223, 2566 GB Den Haag"
        trans "2566 GB 225","Nieboerweg 225, 2566 GB Den Haag"
        trans "2566 GB 227","Nieboerweg 227, 2566 GB Den Haag"
        trans "2566 GB 229","Nieboerweg 229, 2566 GB Den Haag"
        trans "2566 GB 231","Nieboerweg 231, 2566 GB Den Haag"
        trans "2566 GB 233","Nieboerweg 233, 2566 GB Den Haag"
        trans "2566 GB 235","Nieboerweg 235, 2566 GB Den Haag"
        trans "2566 GB 239","Nieboerweg 239, 2566 GB Den Haag"
        trans "2566 GB 241","Nieboerweg 241, 2566 GB Den Haag"
        trans "2566 GB 243","Nieboerweg 243, 2566 GB Den Haag"
        end
        """);

        WriteIfMissing(Path.Combine(BaseDirectory, "Converters", "nederlands_engels_demo.nod"), """
        Name Nederlands naar Engels demo
        URLN Nederlands naar Engels demo
        input1 Nederlands
        input2 Engels
        trans "hallo","hello"
        trans "goedemorgen","good morning"
        trans "goedenavond","good evening"
        trans "dank je","thank you"
        trans "alsjeblieft","please"
        trans "ja","yes"
        trans "nee","no"
        trans "water","water"
        trans "brood","bread"
        trans "kaas","cheese"
        trans "straat","street"
        trans "huis","house"
        trans "auto","car"
        trans "fiets","bicycle"
        trans "school","school"
        end
        """);

        WriteIfMissing(Path.Combine(BaseDirectory, "Converters", "operatie_decibel_1995_demo.nod"), """
        Name Operatie Decibel 1995 volledig
        URLN Telefoonnummer omnummering 1995 volledig
        input1 Oud telefoonnummer
        input2 Nieuw telefoonnummer
        chg "01100-","0113-2"
        chg "01751-","070-51"
        chg "01711-","071-30"
        chg "023-","023-5"
        chg "030-","030-2"
        chg "03489-","0348-69"
        chg "050-1","050-31"
        chg "050-2","050-52"
        chg "050-3","050-53"
        chg "050-4","050-54"
        chg "050-5","050-55"
        chg "050-6","050-36"
        chg "050-7","050-57"
        chg "050-8","050-58"
        chg "050-9","050-59"
        chg "053-","053-4"
        chg "058-","058-2"
        chg "070-","070-"
        chg "010-","010-"
        chg "020-","020-"
        end
        """);
    }

    // Zoek/commentaar: Schrijft data naar schijf wanneer nodig voor WriteIfMissing.
    private static void WriteIfMissing(string path, string text)
    {
        if (!File.Exists(path))
            File.WriteAllText(path, text);
    }

    // Zoek/commentaar: Voegt een ingebouwde converter toe aan een bestaande hoofdpagina-lijst.
    private void AppendCatalogEntryIfMissing(string displayName, string nodPath, bool isDefault, bool updateDisplayName = false)
    {
        var lines = File.ReadAllLines(CatalogPath).ToList();

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var parts = line.Split(',');
            if (parts.Length >= 2 && parts[1].Trim().Equals(nodPath, StringComparison.OrdinalIgnoreCase))
            {
                if (updateDisplayName && ShouldRefreshDisplayName(parts[0].Trim(), displayName, nodPath))
                {
                    var marker = parts.Length >= 3 ? parts[2].Trim() : "";
                    lines[index] = $"{displayName},{parts[1].Trim()},{marker}";
                    File.WriteAllLines(CatalogPath, lines);
                }

                return;
            }
        }

        lines.Add($"{displayName},{nodPath},{(isDefault ? "*" : "")}");
        File.WriteAllLines(CatalogPath, lines);
    }

    // Zoek/commentaar: Ververs oude generieke namen zodat de hoofdlijst niet vol EuroCalculator staat.
    private static bool ShouldRefreshDisplayName(string currentName, string discoveredName, string nodPath)
    {
        if (string.IsNullOrWhiteSpace(discoveredName))
            return false;

        if (currentName.Equals(discoveredName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (currentName.Equals("EuroCalculator", StringComparison.OrdinalIgnoreCase))
            return true;

        if (currentName.Equals("EuroCalculcator", StringComparison.OrdinalIgnoreCase))
            return true;

        return nodPath.StartsWith("Converters/euro/", StringComparison.OrdinalIgnoreCase)
            && discoveredName.Contains(" naar ", StringComparison.OrdinalIgnoreCase);
    }

    // Zoek/commentaar: Nieuwe .nod-bestanden in Converters en submappen automatisch zichtbaar maken.
    private void AppendDiscoveredConverters()
    {
        var converterRoot = Path.Combine(BaseDirectory, "Converters");
        if (!Directory.Exists(converterRoot))
            return;

        foreach (var file in Directory.EnumerateFiles(converterRoot, "*.nod", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            var relativePath = Path.GetRelativePath(BaseDirectory, file).Replace('\\', '/');
            var displayName = ReadDisplayName(file);
            AppendCatalogEntryIfMissing(displayName, relativePath, isDefault: false, updateDisplayName: true);
        }
    }

    // Zoek/commentaar: Haalt de zichtbare naam uit Name, anders uit bestandsnaam/mapnaam.
    private static string ReadDisplayName(string path)
    {
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.StartsWith("Name ", StringComparison.OrdinalIgnoreCase))
                return line[5..].Trim();
        }

        var directory = Path.GetFileName(Path.GetDirectoryName(path));
        var name = Path.GetFileNameWithoutExtension(path);
        return string.IsNullOrWhiteSpace(directory) || directory.Equals("Converters", StringComparison.OrdinalIgnoreCase)
            ? name
            : $"{directory} - {name}";
    }
}
