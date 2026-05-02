/*
NOD SYSTEM -- DEMO

Dit consoleprogramma laat voorbeelden zien van:
- complexe NOD 2.0 math
- legacy reverse
- equations
- data/field-transformaties

Gebruik:

dotnet run --project src/NodSystem.Demo
*/

using NodSystem.Core;

Console.OutputEncoding = System.Text.Encoding.UTF8;

RunMath("Auto reverse sinus graden", """
Name Sinus graden
format ##.00
math sind(ans)
end
""", "90", reverseInput: "1");


RunMath("Complex math: sqrt((ans^2 + 25) / 3) + log(ans,2)", """
Name Complexe som
mode numeric
format ##.00
math sqrt((ans^2 + 25) / 3) + log(ans,2)
end
""", "8");

RunMath("Legacy reverse C/F", """
Name Celsius naar Fahrenheit
format ##.00
math ans * 1,8
math ans + 32
end
""", "22", reverseInput: "71.6");

RunEquation("Equation: E = 0,5 * m * v^2", """
Name Bewegingsenergie
mode equation
given m = 80
given v = 12
equation E = 0,5 * m * v^2
solve E
constraint E >= 0
end
""");

RunEquation("Equation: A = pi * r^2", """
Name Cirkeloppervlakte
mode equation
given r = 5
equation A = pi * r^2
solve A
constraint A >= 0
end
""");

RunData("Data row transform", """
Name Klantdata omzetting
mode data
table klanten

field telefoon
phoneformat country NL
phoneformat remove_spaces true
phoneformat remove_text_prefix true
phoneformat normalize_international true
chg 03402,03060
output telefoon_nieuw

field bedrag_nlg
math ans / 2,20371
output bedrag_eur

field woonplaats
trans "'s-Gravenhage","Den Haag"
trans "The Hague","Den Haag"
output woonplaats_normaal

preview true
backup true
end
""");

static void RunMath(string title, string nod, string input, string? reverseInput = null)
{
    var doc = NodParser.Parse(nod);
    var result = NodEngine.ConvertForward(doc, input);
    Console.WriteLine("== " + title + " ==");
    Console.WriteLine("Input : " + input);
    Console.WriteLine("Output: " + result.Text);

    if (reverseInput is not null)
    {
        var reverse = NodEngine.ConvertReverse(doc, reverseInput);
        Console.WriteLine("Reverse input : " + reverseInput);
        Console.WriteLine("Reverse output: " + reverse.Text);
    }

    Console.WriteLine();
}

static void RunEquation(string title, string nod)
{
    var doc = NodParser.Parse(nod);
    var result = NodEngine.SolveEquation(doc);
    Console.WriteLine("== " + title + " ==");
    Console.WriteLine($"{result.Variable} = {result.Value}");
    Console.WriteLine("Explanation: " + result.Explanation);
    Console.WriteLine();
}

static void RunData(string title, string nod)
{
    var doc = NodParser.Parse(nod);
    var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["telefoon"] = "tel. 03402-36647",
        ["bedrag_nlg"] = "220,371",
        ["woonplaats"] = "'s-Gravenhage"
    };

    var results = NodEngine.TransformRow(doc, row);

    Console.WriteLine("== " + title + " ==");
    foreach (var r in results)
    {
        Console.WriteLine($"{r.FieldName} -> {r.OutputField}: {r.Original} -> {r.Normalized} -> {r.Result}");
    }
    Console.WriteLine();
}


RunTrace("Calculation Trace: complexe stappen terugrekenen", """
Name Trace demo
format ##.00
math ans * e^2
math ans + 10
math sqrt(ans)
end
""", "3");

static void RunTrace(string title, string nod, string input)
{
    var doc = NodParser.Parse(nod);
    var forward = NodEngine.ConvertForwardWithTrace(doc, input);
    var reverse = NodEngine.ConvertReverseFromTrace(doc, forward.Trace);

    Console.WriteLine("== " + title + " ==");
    Console.WriteLine("Input : " + input);
    Console.WriteLine("Output: " + forward.Text);
    Console.WriteLine("Reverse via trace: " + reverse.Text);
    Console.WriteLine("Trace steps:");

    foreach (var step in forward.Trace.Steps)
    {
        Console.WriteLine($"  {step.InputValue} -> {step.Expression} -> {step.OutputValue} ; reverse: {step.AutoReverseExpression}");
    }

    Console.WriteLine();
}



RunEnterprisePreview();

static void RunEnterprisePreview()
{
    var doc = NodParser.Parse("""
    Name Enterprise Preview Demo
    mode data
    table klanten

    field telefoon
    chg 03402,03060
    output telefoon_nieuw

    field bedrag_nlg
    math ans / 2,20371
    output bedrag_eur

    preview true
    backup true
    end
    """);

    Console.WriteLine("== SQL Preview Demo ==");
    Console.WriteLine(SqlPreviewGenerator.GeneratePreviewSql(doc));

    var rows = new List<IDictionary<string, string>>
    {
        new Dictionary<string, string>
        {
            ["telefoon"] = "03402-36647",
            ["bedrag_nlg"] = "220,371"
        }
    };

    var context = new NodRunContext(doc.Name, new SafetyOptions
    {
        PreviewRequired = true,
        BackupRequired = true,
        ApprovalRequired = true
    });

    var report = ReportBuilder.BuildPreviewReport(doc, rows, context);
    Console.WriteLine(report.ToString());
    Console.WriteLine();
}

