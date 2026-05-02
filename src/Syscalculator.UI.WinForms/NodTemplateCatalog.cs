namespace Syscalculator.UI.WinForms;

internal sealed record NodTemplateItem(string Title, string FileName, string Description, string Text);

internal static class NodTemplateCatalog
{
    public static IReadOnlyList<NodTemplateItem> Create(LanguageCatalog language)
    {
        static string T(LanguageCatalog language, string key, string fallback) => language.Text(key, fallback);

        return
        [
            new NodTemplateItem(
                T(language, "editor.template.convert.title", "Omrekentool"),
                "omrekentool.nod",
                T(language, "editor.template.convert.description", "Basis voor een oude Syscalculator-converter met input, output, symbolen en math-regels."),
                """
                Name Celsius naar Fahrenheit
                URLN Celsius naar Fahrenheit
                input1 Celsius
                input2 Fahrenheit
                Symb3 C
                Symb4 F
                format ##.00
                math ans * 1,8
                math ans + 32
                end
                """),

            new NodTemplateItem(
                T(language, "editor.template.math.title", "Math"),
                "math.nod",
                T(language, "editor.template.math.description", "Een korte rekenconverter met een formule op ans."),
                """
                Name Ans maal e kwadraat
                URLN e-kwadraat conversie
                input1 getal
                input2 resultaat
                format ##.00
                math ans * e^2
                end
                """),

            new NodTemplateItem(
                T(language, "editor.template.graph.title", "Math grafiek"),
                "math_grafiek.nod",
                T(language, "editor.template.graph.description", "Een math-converter die direct bruikbaar is in Graph Preview."),
                """
                Name Sinus graden
                URLN Sinus graden
                input1 graden
                input2 sinus
                format ##.00
                math sind(ans)
                end
                """),

            new NodTemplateItem(
                T(language, "editor.template.translate.title", "Tekst vertalen"),
                "tekst_vertalen.nod",
                T(language, "editor.template.translate.description", "Exacte tekstregels omzetten met trans."),
                """
                Name Nederlands naar Engels demo
                URLN Nederlands naar Engels demo
                input1 Nederlands
                input2 Engels
                trans "hallo","hello"
                trans "dag","goodbye"
                trans "ja","yes"
                trans "nee","no"
                end
                """),

            new NodTemplateItem(
                T(language, "editor.template.replace.title", "Tekst wijzigen"),
                "tekst_wijzigen.nod",
                T(language, "editor.template.replace.description", "Patronen of voorvoegsels vervangen met chg."),
                """
                Name Telefoonnummer omnummering demo
                URLN Telefoonnummer omnummering demo
                input1 Oud telefoonnummer
                input2 Nieuw telefoonnummer
                chg "01751-","070-51"
                chg "0183x-xx xx","0183-x0 xx xx"
                end
                """),

            new NodTemplateItem(
                T(language, "editor.template.data.title", "Data tabel"),
                "data_tabel.nod",
                T(language, "editor.template.data.description", "Startpunt voor data-converters met velden en outputregels."),
                """
                Name Klanten telefoon opschonen
                URLN Data converter demo
                mode data
                table klanten
                field telefoon
                phoneformat country NL
                phoneformat normalize_international true
                output telefoon
                end
                """),

            new NodTemplateItem(
                T(language, "editor.template.solver.title", "Equation solver"),
                "equation_solver.nod",
                T(language, "editor.template.solver.description", "Een equation-template voor Solver stappen."),
                """
                Name Snijpunt lijnen solver demo
                URLN Snijpunt lijnen solver demo
                mode equation
                given y = 20
                equation y = x * 2
                solve x
                constraint x >= 0
                end
                """),

            new NodTemplateItem(
                T(language, "editor.template.diff.title", "Differentieren"),
                "differentieren.nod",
                T(language, "editor.template.diff.description", "Een solve diff-template voor formulekaarten en solver-uitleg."),
                """
                Name Machtsregel animatie demo
                URLN Machtsregel animatie demo
                format ##.00
                solve diff ans^2
                end
                """)
        ];
    }
}
