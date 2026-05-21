namespace Syscalculator.UI.WinForms;

/// <summary>
/// UI-metadata uit een .nod-bestand.
/// Tiedragon.NodSystem.Core focust op engine.
/// Deze helper leest UI-velden zoals input1/input2/Symb apart.
/// </summary>
public sealed class NodUiMetadata
{
    private const string EuroSymbol = "\u20ac";
    private const string MojibakeEuroLatin = "\u00e2\u201a\u00ac";
    private const string MojibakeEuroCyrillic = "\u0442\u0412\u043c";
    private const string MojibakeEuroCyrillicA = "\u0410";
    private const string ReplacementCharacter = "\ufffd";

    public string Name { get; set; } = "NOD";
    public string Urln { get; set; } = "";
    public string Input1 { get; set; } = "Input";
    public string Input2 { get; set; } = "Output";
    public string SymbolBeforeInput { get; set; } = "";
    public string SymbolBeforeOutput { get; set; } = "";
    public string SymbolAfterInput { get; set; } = "";
    public string SymbolAfterOutput { get; set; } = "";
    public string Format { get; set; } = "";
    public List<string> IntroLines { get; } = new();

    // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor Parse.
    public static NodUiMetadata Parse(string text)
    {
        var meta = new NodUiMetadata();
        foreach (var raw in text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n'))
        {
            var line = raw.TrimEnd();
            var trimmed = line.Trim();

            if (trimmed.Length == 0)
                continue;

            // Comments are not UI metadata, except when the old file uses apostrophe comments.
            if (trimmed.StartsWith("'"))
                continue;

            var idx = trimmed.IndexOfAny([' ', '\t']);
            var key = idx < 0 ? trimmed : trimmed[..idx].Trim();
            var val = idx < 0 ? "" : trimmed[(idx + 1)..].Trim();

            switch (key.ToLowerInvariant())
            {
                case "name":
                    meta.Name = val;
                    break;

                case "urln":
                    meta.Urln = val;
                    break;

                case "input1":
                    meta.Input1 = val;
                    break;

                case "input2":
                    meta.Input2 = val;
                    break;

                case "inputr":
                    meta.Input2 = NormalizeInputRLabel(val);
                    break;

                case "symb1":
                    meta.SymbolBeforeInput = NormalizeLegacySymbol(val);
                    break;

                case "symb2":
                    meta.SymbolBeforeOutput = NormalizeLegacySymbol(val);
                    break;

                case "symb3":
                    meta.SymbolAfterInput = NormalizeLegacySymbol(val);
                    break;

                case "symb4":
                    meta.SymbolAfterOutput = NormalizeLegacySymbol(val);
                    break;

                case "format":
                    meta.Format = val;
                    break;

                case "indoprint":
                    meta.IntroLines.Add(val);
                    break;

                case "indoend":
                    break;

                default:
                    // Some very old files may contain empty intro lines between indoprint and indoend.
                    // Keep only explicit indoprint text as dialog text.
                    break;
            }
        }

        return meta;
    }

    // Oude VB6-bestanden gebruikten soms Chr(128) of beschadigde ANSI/UTF-8 tekst voor het euroteken.
    private static string NormalizeLegacySymbol(string value)
    {
        var symbol = value.Trim();

        if (symbol.Length == 0)
            return "";

        if (int.TryParse(symbol, out var code))
        {
            if (code is 128 or 8364)
                return EuroSymbol;

            if (code >= 32 && code <= 126)
                return ((char)code).ToString();
        }

        return symbol
            .Replace(MojibakeEuroLatin, EuroSymbol, StringComparison.Ordinal)
            .Replace(MojibakeEuroCyrillic, EuroSymbol, StringComparison.Ordinal)
            .Replace(MojibakeEuroCyrillicA, EuroSymbol, StringComparison.Ordinal)
            .Replace(ReplacementCharacter, EuroSymbol, StringComparison.Ordinal);
    }

    private static string NormalizeInputRLabel(string value)
    {
        var label = value.Trim();
        if (label.Length == 0)
            return "";

        var idx = label.IndexOfAny([' ', '\t']);
        if (idx > 0)
        {
            var first = label[..idx].Trim();
            var rest = label[(idx + 1)..].Trim();
            if (first.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                first.Equals("output", StringComparison.OrdinalIgnoreCase) ||
                first.Equals("result", StringComparison.OrdinalIgnoreCase))
                return rest;
        }

        return label;
    }
}
