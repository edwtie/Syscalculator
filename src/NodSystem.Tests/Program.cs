/*
NOD SYSTEM -- TESTS

Dit testproject gebruikt geen externe testframeworks.
Daardoor kan het direct draaien met:

dotnet run --project src/NodSystem.Tests

De tests controleren:
- legacy math vooruit
- legacy math reverse
- e en pi constanten
- complexe sommen
- equation/given/solve
- data field telefoonnormalisatie
- data field math
*/

using Tiedragon.Graph;
using Tiedragon.Graph.G2D;
using Tiedragon.Graph.G3D;
using Tiedragon.NodSystem.Core;

var total = 0;
var passed = 0;

Test("legacy celsius forward", () =>
{
    var doc = NodParser.Parse("""
    Name Celsius
    format ##.00
    math ans * 1,8
    math ans + 32
    end
    """);

    var result = NodEngine.ConvertForward(doc, "22");
    AssertText("71.60", result.Text);
});

Test("legacy celsius reverse", () =>
{
    var doc = NodParser.Parse("""
    Name Celsius
    format ##.00
    math ans * 1,8
    math ans + 32
    end
    """);

    var result = NodEngine.ConvertReverse(doc, "71.6");
    AssertText("22.00", result.Text);
});

Test("e constant", () =>
{
    var value = NodExpressionEvaluator.Evaluate("e^2", 0);
    AssertNear(7.389056m, value, 0.0001m);
});

Test("pi circle", () =>
{
    var value = NodExpressionEvaluator.Evaluate("pi * ans^2", 5);
    AssertNear(78.539816m, value, 0.0001m);
});

Test("complex math", () =>
{
    var value = NodExpressionEvaluator.Evaluate("sqrt((ans^2 + 25) / 3) + log(ans,2)", 8);
    AssertNear(8.446711546m, value, 0.0001m);
});

Test("expression implicit multiplication and absolute value", () =>
{
    AssertDecimal(16m, NodExpressionEvaluator.Evaluate("2(ans + 3)", 5));
    AssertNear(2m * NodExpressionEvaluator.Evaluate("pi", 0), NodExpressionEvaluator.Evaluate("2pi", 0), 0.0001m);
    AssertDecimal(7m, NodExpressionEvaluator.Evaluate("|ans - 12|", 5));
});

Test("expression variables and rounding functions", () =>
{
    var vars = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
    {
        ["m"] = 80m,
        ["v"] = 12m
    };

    AssertDecimal(5760m, NodExpressionEvaluator.Evaluate("0,5 * m * v^2", 0, vars));
    AssertDecimal(4m, NodExpressionEvaluator.Evaluate("round(3.6)", 0));
    AssertDecimal(3m, NodExpressionEvaluator.Evaluate("floor(3.9)", 0));
    AssertDecimal(4m, NodExpressionEvaluator.Evaluate("ceil(3.1)", 0));
    AssertDecimal(2m, NodExpressionEvaluator.Evaluate("min(2,7)", 0));
    AssertDecimal(7m, NodExpressionEvaluator.Evaluate("max(2,7)", 0));
});

Test("expression vector length and scalar projection", () =>
{
    AssertDecimal(5m, NodExpressionEvaluator.Evaluate("length(vec(3,4))", 0));
    AssertDecimal(5m, NodExpressionEvaluator.Evaluate("length(vec(3,4,0))", 0));
    AssertDecimal(13m, NodExpressionEvaluator.Evaluate("length(vec(3,4,12))", 0));
    AssertDecimal(5m, NodExpressionEvaluator.Evaluate("|vec(3,4)|", 0));
});

Test("graph3d treats vector length as arrow from origin", () =>
{
    AssertDecimal(5m, NodExpressionEvaluator.Evaluate("length(vec(3,4))", 0));
    AssertDecimal(13m, NodExpressionEvaluator.Evaluate("length(vec(3,4,12))", 0));

    var arrow2D = Graph3DApi.From2D(new PointF(3f, 4f), z: 0d);
    AssertNear(3m, (decimal)arrow2D.X, 0.0001m);
    AssertNear(4m, (decimal)arrow2D.Y, 0.0001m);
    AssertNear(0m, (decimal)arrow2D.Z, 0.0001m);

    var arrow3D = new GraphPoint3D(3d, 4d, 12d);
    var plot = new Rectangle(0, 0, 400, 300);
    var view = Graph3DApi.CreateFitView(new[] { new GraphPoint3D(0d, 0d, 0d), arrow3D });
    var projectedOrigin = Graph3DApi.ProjectToScreen(new GraphPoint3D(0d, 0d, 0d), plot, view, Graph3DApi.DefaultCamera);
    var projectedTip = Graph3DApi.ProjectToScreen(arrow3D, plot, view, Graph3DApi.DefaultCamera);

    AssertTrue(float.IsFinite(projectedOrigin.Screen.X), "Graph3D origin projection should be finite.");
    AssertTrue(float.IsFinite(projectedTip.Screen.X), "Graph3D vector tip projection should be finite.");
    AssertTrue(projectedOrigin.Screen != projectedTip.Screen, "Graph3D vector arrow should project to a visible segment.");
});

Test("graph2d treats vector length as arrow from origin", () =>
{
    AssertDecimal(5m, NodExpressionEvaluator.Evaluate("length(vec(3,4))", 0));

    var plot = new Rectangle(0, 0, 400, 300);
    var view = GraphSurfaceApi.CreateFitView(new[] { new PointF(0f, 0f), new PointF(3f, 4f) }, -1d, 4d, plot.Size);
    var projectedOrigin = GraphSurfaceApi.GraphToScreen(new PointF(0f, 0f), plot, view);
    var projectedTip = GraphSurfaceApi.GraphToScreen(new PointF(3f, 4f), plot, view);
    var roundTripTip = GraphSurfaceApi.ScreenToGraph(projectedTip, plot, view);

    AssertTrue(float.IsFinite(projectedOrigin.X), "Graph2D origin projection should be finite.");
    AssertTrue(float.IsFinite(projectedTip.X), "Graph2D vector tip projection should be finite.");
    AssertTrue(projectedOrigin != projectedTip, "Graph2D vector arrow should project to a visible segment.");
    AssertNear(3m, (decimal)roundTripTip.X, 0.0001m);
    AssertNear(4m, (decimal)roundTripTip.Y, 0.0001m);
});

Test("expression vector arithmetic keeps z component", () =>
{
    AssertDecimal(13m, NodExpressionEvaluator.Evaluate("length(vec(1,2,3) + vec(2,2,9))", 0));
    AssertDecimal(12m, NodExpressionEvaluator.Evaluate("z(vec(1,2,3) + vec(2,2,9))", 0));
    AssertDecimal(25m, NodExpressionEvaluator.Evaluate("dot(vec(3,4), vec(3,4,12))", 0));
    AssertDecimal(26m, NodExpressionEvaluator.Evaluate("dot(vec(3,4,1), vec(3,4,1))", 0));
});

Test("expression vector cross and angle", () =>
{
    AssertDecimal(1m, NodExpressionEvaluator.Evaluate("z(cross(vec(1,0,0), vec(0,1,0)))", 0));
    AssertDecimal(1m, NodExpressionEvaluator.Evaluate("x(cross(vec(0,1,0), vec(0,0,1)))", 0));
    AssertNear(90m, NodExpressionEvaluator.Evaluate("angled(vec(1,0), vec(0,1))", 0), 0.0001m);
});

Test("expression vector invalid output is rejected", () =>
{
    AssertThrows(
        "Vector result cannot be returned as decimal. Use length(...), dot(...), or x/y/z(...).",
        () => NodExpressionEvaluator.Evaluate("vec(3,4)", 0));
});

Test("expression scientific notation", () =>
{
    AssertNear(0.000000000001m, NodExpressionEvaluator.Evaluate("1E-12", 0), 0.0000000000001m);
    AssertNear(2m * NodExpressionEvaluator.Evaluate("e", 0), NodExpressionEvaluator.Evaluate("2e", 0), 0.0001m);
});

Test("nod math vec shorthand calculates vector length", () =>
{
    var doc = NodParser.Parse("""
    Name Vector lengte
    math vec 3 4
    end
    """);

    var result = NodEngine.ConvertForward(doc, "0");
    AssertDecimal(5m, result.NumericValue ?? 0);
});

Test("nod math vec shorthand supports 3d tuple", () =>
{
    var doc = NodParser.Parse("""
    Name Vector lengte 3D
    math vec (3,4,12)
    end
    """);

    var result = NodEngine.ConvertForward(doc, "0");
    AssertDecimal(13m, result.NumericValue ?? 0);
});

Test("expression matrix 2x2 determinant and trace", () =>
{
    AssertDecimal(-2m, NodExpressionEvaluator.Evaluate("det(mat2(1,2,3,4))", 0));
    AssertDecimal(-2m, NodExpressionEvaluator.Evaluate("det2(1,2,3,4)", 0));
    AssertDecimal(5m, NodExpressionEvaluator.Evaluate("trace(mat2(1,2,3,4))", 0));
    AssertDecimal(4m, NodExpressionEvaluator.Evaluate("mget(mat2(1,2,3,4),2,2)", 0));
});

Test("expression matrix vector and matrix multiplication", () =>
{
    AssertDecimal(6m, NodExpressionEvaluator.Evaluate("x(mat2(2,0,0,3) * vec(3,4))", 0));
    AssertDecimal(12m, NodExpressionEvaluator.Evaluate("y(mat2(2,0,0,3) * vec(3,4))", 0));
    AssertDecimal(4m, NodExpressionEvaluator.Evaluate("mget(mat2(1,2,3,4) * mat2(2,0,1,2),1,2)", 0));
});

Test("expression matrix 3x3 determinant trace and lookup", () =>
{
    AssertDecimal(1m, NodExpressionEvaluator.Evaluate("det(mat3(1,2,3,0,1,4,5,6,0))", 0));
    AssertDecimal(2m, NodExpressionEvaluator.Evaluate("trace(mat3(1,2,3,0,1,4,5,6,0))", 0));
    AssertDecimal(6m, NodExpressionEvaluator.Evaluate("mget(mat3(1,2,3,0,1,4,5,6,0),3,2)", 0));
});

Test("expression matrix 3x3 vector and matrix multiplication", () =>
{
    AssertDecimal(14m, NodExpressionEvaluator.Evaluate("x(mat3(1,2,3,0,1,4,5,6,0) * vec(1,2,3))", 0));
    AssertDecimal(14m, NodExpressionEvaluator.Evaluate("y(mat3(1,2,3,0,1,4,5,6,0) * vec(1,2,3))", 0));
    AssertDecimal(17m, NodExpressionEvaluator.Evaluate("z(mat3(1,2,3,0,1,4,5,6,0) * vec(1,2,3))", 0));
    AssertDecimal(6m, NodExpressionEvaluator.Evaluate("mget(mat3(1,2,3,0,1,4,5,6,0) * mat3(1,0,0,0,1,0,0,0,1),3,2)", 0));
});

Test("nod math supports matrix 3x3 determinant", () =>
{
    var doc = NodParser.Parse("""
    Name Matrix 3x3 determinant
    math det(mat3(1,2,3,0,1,4,5,6,0))
    end
    """);

    var result = NodEngine.ConvertForward(doc, "0");
    AssertDecimal(1m, result.NumericValue ?? 0);
});

Test("expression statistics functions", () =>
{
    AssertDecimal(40m, NodExpressionEvaluator.Evaluate("sum(2,4,4,4,5,5,7,9)", 0));
    AssertDecimal(5m, NodExpressionEvaluator.Evaluate("mean(2,4,4,4,5,5,7,9)", 0));
    AssertDecimal(4.5m, NodExpressionEvaluator.Evaluate("median(2,4,4,4,5,5,7,9)", 0));
    AssertDecimal(24m, NodExpressionEvaluator.Evaluate("product(2,3,4)", 0));
    AssertDecimal(4m, NodExpressionEvaluator.Evaluate("variance(2,4,4,4,5,5,7,9)", 0));
    AssertDecimal(2m, NodExpressionEvaluator.Evaluate("stdev(2,4,4,4,5,5,7,9)", 0));
});

Test("probability math factorial", () =>
{
    AssertDecimal(2m, NodExpressionEvaluator.Evaluate("2!", 0));
    AssertDecimal(120m, NodExpressionEvaluator.Evaluate("5!", 0));
    AssertDecimal(720m, NodExpressionEvaluator.Evaluate("fact(6)", 0));
});

Test("probability math combinations and permutations", () =>
{
    AssertDecimal(10m, NodExpressionEvaluator.Evaluate("comb(5,2)", 0));
    AssertDecimal(10m, NodExpressionEvaluator.Evaluate("ncr(5,2)", 0));
    AssertDecimal(20m, NodExpressionEvaluator.Evaluate("perm(5,2)", 0));
    AssertDecimal(20m, NodExpressionEvaluator.Evaluate("npr(5,2)", 0));
});

Test("probability math expected value and capital E constant", () =>
{
    AssertDecimal(5m, NodExpressionEvaluator.Evaluate("expected(0,0.5,10,0.5)", 0));
    AssertNear(7.389056m, NodExpressionEvaluator.Evaluate("E^2", 0), 0.0001m);
});

Test("probability invalid inputs are rejected", () =>
{
    AssertThrows("factorial expects a non-negative whole number.", () => NodExpressionEvaluator.Evaluate("(-1)!", 0));
    AssertThrows("comb expects whole numbers with 0 <= r <= n.", () => NodExpressionEvaluator.Evaluate("comb(3,4)", 0));
    AssertThrows("perm expects whole numbers with 0 <= r <= n.", () => NodExpressionEvaluator.Evaluate("perm(3,4)", 0));
});

Test("equation kinetic energy", () =>
{
    var doc = NodParser.Parse("""
    Name Bewegingsenergie
    mode equation
    given m = 80
    given v = 12
    equation E = 0,5 * m * v^2
    solve E
    constraint E >= 0
    end
    """);

    var result = NodEngine.SolveEquation(doc);
    AssertDecimal(5760m, result.Value);
});

Test("equation can solve right side variable", () =>
{
    var doc = NodParser.Parse("""
    Name Ohm
    mode equation
    given V = 12
    given I = 3
    equation R = V / I
    solve R
    constraint R > 0
    end
    """);

    var result = NodEngine.SolveEquation(doc);
    AssertText("R", result.Variable);
    AssertDecimal(4m, result.Value);
});

Test("equation can solve graph intersection", () =>
{
    var doc = NodParser.Parse("""
    Name Snijpunt
    mode equation
    given y = 20
    equation y = x * 2
    solve x
    constraint x >= 0
    end
    """);

    var result = NodEngine.SolveEquation(doc);
    AssertText("x", result.Variable);
    AssertNear(10m, result.Value, 0.0001m);

    var report = SolverStepBuilder.Build(doc, "0");
    if (report.EquationGraph is null)
        throw new Exception("Intersection solve should expose equation graph info.");
});

Test("intersection solver demo nod parses and solves", () =>
{
    var path = Path.Combine(
        "src",
        "syscalculator",
        "Converters",
        "Math",
        "snijpunt_lijnen_solver_demo.nod");

    if (!File.Exists(path))
        throw new Exception("Intersection solver demo NOD file is missing.");

    var doc = NodParser.Parse(File.ReadAllText(path));
    var result = NodEngine.SolveEquation(doc);
    AssertText("x", result.Variable);
    AssertNear(10m, result.Value, 0.0001m);
});

Test("equation constraint violation is rejected", () =>
{
    var doc = NodParser.Parse("""
    Name Negative constraint
    mode equation
    given m = -1
    equation E = m * 2
    solve E
    constraint E >= 0
    end
    """);

    AssertThrows("Constraint failed: E >= 0", () => NodEngine.SolveEquation(doc));
});

Test("data telefoon messy", () =>
{
    var doc = NodParser.Parse("""
    Name Data
    mode data
    table klanten

    field telefoon
    phoneformat country NL
    phoneformat remove_spaces true
    phoneformat remove_text_prefix true
    phoneformat normalize_international true
    chg 03402,03060
    output telefoon_nieuw
    end
    """);

    var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["telefoon"] = "tel. 03402-36647"
    };

    var result = NodEngine.TransformRow(doc, row)[0];
    AssertText("0340236647", result.Normalized);
    AssertText("0306036647", result.Result);
});

Test("data amount nlg eur", () =>
{
    var doc = NodParser.Parse("""
    Name Data
    mode data
    table klanten

    field bedrag_nlg
    math ans / 2,20371
    output bedrag_eur
    end
    """);

    var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["bedrag_nlg"] = "220,371"
    };

    var result = NodEngine.TransformRow(doc, row)[0];
    var numeric = NodParser.ParseFlexibleDecimal(result.Result);
    AssertNear(100m, numeric, 0.0001m);
});

Test("data phoneformat normalizes international NL variants", () =>
{
    var options = new PhoneFormatOptions
    {
        Country = "NL",
        RemoveSpaces = true,
        RemoveDots = true,
        RemoveSlashes = true,
        RemoveParentheses = true,
        RemoveTextPrefix = true,
        NormalizeInternational = true
    };

    AssertText("0612345678", DataTransformEngine.ApplyPhoneFormat("tel. +31 6 1234 5678", options));
    AssertText("0612345678", DataTransformEngine.ApplyPhoneFormat("telefoon: 0031 (6) 1234-5678", options));
});

Test("data transform applies trans and math expressions in order", () =>
{
    var doc = NodParser.Parse("""
    Name Data order
    mode data
    table klanten

    field korting
    trans "laag","10"
    math ans * 1,21
    output korting_btw
    end
    """);

    var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["korting"] = "laag"
    };

    var result = NodEngine.TransformRow(doc, row)[0];
    AssertText("laag", result.Original);
    AssertText("laag", result.Normalized);
    AssertText("12.10", result.Result);
});


Test("trig degrees sind", () =>
{
    var value = NodExpressionEvaluator.Evaluate("sind(90)", 0);
    AssertNear(1m, value, 0.0001m);
});

Test("trig degrees asind", () =>
{
    var value = NodExpressionEvaluator.Evaluate("asind(1)", 0);
    AssertNear(90m, value, 0.0001m);
});

Test("rad deg conversion", () =>
{
    var value = NodExpressionEvaluator.Evaluate("deg(pi)", 0);
    AssertNear(180m, value, 0.0001m);
});

Test("auto reverse sind", () =>
{
    var doc = NodParser.Parse("""
    Name Sinus graden
    format ##.00
    math sind(ans)
    end
    """);

    var forward = NodEngine.ConvertForward(doc, "90");
    AssertText("1.00", forward.Text);

    var reverse = NodEngine.ConvertReverse(doc, forward.Text);
    AssertText("90.00", reverse.Text);
});

Test("auto reverse ans times e2", () =>
{
    var doc = NodParser.Parse("""
    Name Ans maal e kwadraat
    format ##.00
    math ans * e^2
    end
    """);

    var forward = NodEngine.ConvertForward(doc, "3");
    AssertText("22.17", forward.Text);

    var reverse = NodEngine.ConvertReverse(doc, forward.Text);
    AssertNear(3m, NodParser.ParseFlexibleDecimal(reverse.Text), 0.01m);
});



Test("trace reverse multi-step math", () =>
{
    var doc = NodParser.Parse("""
    Name Trace test
    format ##.00
    math ans * e^2
    math ans + 10
    math sqrt(ans)
    end
    """);

    var forward = NodEngine.ConvertForwardWithTrace(doc, "3");
    AssertText("5.67", forward.Text);

    var reverse = NodEngine.ConvertReverseFromTrace(doc, forward.Trace);
    AssertNear(3m, NodParser.ParseFlexibleDecimal(reverse.Text), 0.01m);
});

Test("trace reverse direct input", () =>
{
    var doc = NodParser.Parse("""
    Name Trace test
    format ##.00
    math ans * e^2
    math ans + 10
    end
    """);

    var forward = NodEngine.ConvertForwardWithTrace(doc, "3");
    AssertText("32.17", forward.Text);

    var reverse = NodEngine.ConvertReverseFromTrace(doc, forward.Trace, "32.17");
    AssertNear(3m, NodParser.ParseFlexibleDecimal(reverse.Text), 0.01m);
});



Test("trace reverse log base 2", () =>
{
    var doc = NodParser.Parse("""
    Name Log trace
    format ##.00
    math log(ans,2)
    end
    """);

    var forward = NodEngine.ConvertForwardWithTrace(doc, "8");
    AssertText("3.00", forward.Text);

    var reverse = NodEngine.ConvertReverseFromTrace(doc, forward.Trace);
    AssertText("8.00", reverse.Text);
});

Test("trace reverse tan degrees", () =>
{
    var doc = NodParser.Parse("""
    Name Tan graden trace
    format ##.00
    math tand(ans)
    end
    """);

    var forward = NodEngine.ConvertForwardWithTrace(doc, "45");
    AssertText("1.00", forward.Text);

    var reverse = NodEngine.ConvertReverseFromTrace(doc, forward.Trace);
    AssertText("45.00", reverse.Text);
});

Test("trace reverse sin radians", () =>
{
    var doc = NodParser.Parse("""
    Name Sin rad trace
    format ##.00
    math sin(ans)
    end
    """);

    var forward = NodEngine.ConvertForwardWithTrace(doc, "1,57079632679");
    AssertText("1.00", forward.Text);

    var reverse = NodEngine.ConvertReverseFromTrace(doc, forward.Trace);
    AssertNear(1.570796m, NodParser.ParseFlexibleDecimal(reverse.Text), 0.01m);
});

Test("trace records every math step", () =>
{
    var doc = NodParser.Parse("""
    Name Trace count
    format ##.00
    math ans + 2
    math ans * 3
    math sqrt(ans)
    end
    """);

    var forward = NodEngine.ConvertForwardWithTrace(doc, "10");
    AssertDecimal(3m, forward.Trace.Steps.Count);
    AssertDecimal(10m, forward.Trace.StartValue);
    AssertNear(6m, forward.Trace.FinalValue, 0.01m);
    AssertText("6.00", forward.Text);
});



Test("sql preview chg", () =>
{
    var doc = NodParser.Parse("""
    Name SQL Test
    mode data
    table klanten

    field telefoon
    chg 03402,03060
    output telefoon_nieuw
    end
    """);

    var sql = SqlPreviewGenerator.GeneratePreviewSql(doc);
    if (!sql.Contains("CASE WHEN telefoon LIKE '03402%' THEN '03060'"))
        throw new Exception("SQL preview did not contain expected chg CASE.");
});

Test("sql preview escapes translated apostrophes", () =>
{
    var doc = NodParser.Parse("""
    Name SQL Escape
    mode data
    table klanten

    field naam
    trans "O'Brien","Obrien"
    output naam_schoon
    end
    """);

    var sql = SqlPreviewGenerator.GeneratePreviewSql(doc);
    if (!sql.Contains("WHEN naam = 'O''Brien' THEN 'Obrien'"))
        throw new Exception("SQL preview did not escape apostrophes.");
});

Test("sql update includes lookup skeleton", () =>
{
    var doc = NodParser.Parse("""
    Name Lookup SQL
    mode data
    table klanten

    field postcode
    lookup postcodes
    match postcode = code
    output plaats
    end
    """);

    var sql = SqlPreviewGenerator.GenerateUpdateSql(doc);
    if (!sql.Contains("Lookup update skeleton: postcode -> plaats"))
        throw new Exception("SQL update did not include lookup skeleton.");
    if (!sql.Contains("klanten.postcode = postcodes.code"))
        throw new Exception("SQL update did not include lookup match fields.");
});

Test("report builder preview", () =>
{
    var doc = NodParser.Parse("""
    Name Report Test
    mode data
    table klanten

    field telefoon
    phoneformat country NL
    phoneformat remove_text_prefix true
    chg 03402,03060
    output telefoon_nieuw
    preview true
    backup true
    end
    """);

    var rows = new List<IDictionary<string, string>>
    {
        new Dictionary<string, string> { ["telefoon"] = "tel. 03402-36647" }
    };

    var report = ReportBuilder.BuildPreviewReport(doc, rows);
    AssertDecimal(1m, report.ScannedRows);
    AssertDecimal(1m, report.ChangedFields);
});

Test("report builder counts unchanged rows and approval warning", () =>
{
    var doc = NodParser.Parse("""
    Name Report unchanged
    mode data
    table klanten

    field status
    trans "actief","actief"
    output status_nieuw
    end
    """);

    var rows = new List<IDictionary<string, string>>
    {
        new Dictionary<string, string> { ["status"] = "actief" },
        new Dictionary<string, string> { ["status"] = "onbekend" }
    };

    var report = ReportBuilder.BuildPreviewReport(doc, rows);
    AssertDecimal(2m, report.ScannedRows);
    AssertDecimal(0m, report.ChangedFields);
    AssertDecimal(1m, report.Warnings);
    if (!report.Lines.Any(line => line.Contains("approval required")))
        throw new Exception("Report did not include approval warning.");
});

Test("document batch replace", () =>
{
    var docs = new Dictionary<string, string>
    {
        ["a.txt"] = "Syscalculator was oud.",
        ["b.txt"] = "Geen match."
    };

    var rules = new List<DocumentTextRule>
    {
        new("Syscalculator", "Syscalculcator")
    };

    var result = DocumentBatchEngine.ReplaceAll(docs, rules);
    AssertDecimal(2m, result.DocumentsScanned);
    AssertDecimal(1m, result.DocumentsChanged);
    AssertDecimal(1m, result.Replacements);
});

Test("document batch counts multiple replacements per document", () =>
{
    var docs = new Dictionary<string, string>
    {
        ["a.txt"] = "oud oud oud",
        ["b.txt"] = "oud en nieuw"
    };

    var rules = new List<DocumentTextRule>
    {
        new("oud", "nieuw")
    };

    var result = DocumentBatchEngine.ReplaceAll(docs, rules);
    AssertDecimal(2m, result.DocumentsScanned);
    AssertDecimal(2m, result.DocumentsChanged);
    AssertDecimal(4m, result.Replacements);
    if (!result.Lines.Contains("a.txt: 3 replacements"))
        throw new Exception("Document batch did not report replacements for a.txt.");
});



Test("mod function", () =>
{
    var value = NodExpressionEvaluator.Evaluate("mod(ans,2)", 9);
    AssertDecimal(1m, value);
});

Test("rem function alias", () =>
{
    var value = NodExpressionEvaluator.Evaluate("rem(ans,4)", 10);
    AssertDecimal(2m, value);
});

Test("percent operator", () =>
{
    var value = NodExpressionEvaluator.Evaluate("ans % 5", 12);
    AssertDecimal(2m, value);
});

Test("legacy style ans mod", () =>
{
    var doc = NodParser.Parse("""
    Name Mod test
    format ##.00
    math ans mod 2
    end
    """);

    var result = NodEngine.ConvertForward(doc, "9");
    AssertText("1.00", result.Text);
});



Test("calculus derivative x squared", () =>
{
    var doc = NodParser.Parse("""
    Name Differentiaal
    format ##.00
    math diff ans^2
    end
    """);

    var result = NodEngine.ConvertForward(doc, "3");
    AssertNear(6m, NodParser.ParseFlexibleDecimal(result.Text), 0.01m);
});

Test("calculus integral x squared 0 to 1", () =>
{
    var doc = NodParser.Parse("""
    Name Integraal
    format ##.00
    math integral 0,1 ans^2
    end
    """);

    var result = NodEngine.ConvertForward(doc, "0");
    AssertNear(0.33m, NodParser.ParseFlexibleDecimal(result.Text), 0.01m);
});

Test("solve diff is calculus alias", () =>
{
    var doc = NodParser.Parse("""
    Name Solve diff
    format ##.00
    solve diff ans^2
    end
    """);

    if (doc.Equation is not null)
        throw new Exception("solve diff should not create an equation block.");
    AssertDecimal(1m, doc.CalculusSteps.Count);

    var result = NodEngine.ConvertForward(doc, "3");
    AssertNear(6m, NodParser.ParseFlexibleDecimal(result.Text), 0.01m);
});

Test("solve integral is calculus alias with trace steps", () =>
{
    var doc = NodParser.Parse("""
    Name Solve integral
    format ##.00
    solve integral 0,1 ans^2
    end
    """);

    AssertDecimal(1m, doc.CalculusSteps.Count);

    var result = NodEngine.ConvertForwardWithTrace(doc, "0");
    AssertNear(0.33m, NodParser.ParseFlexibleDecimal(result.Text), 0.01m);
    AssertDecimal(1m, result.Trace.Steps.Count);

    var explanation = result.Trace.Steps[0].ExplanationSteps;
    if (explanation is null || explanation.Count < 3)
        throw new Exception("solve integral should expose explanation steps for animation.");
});

Test("solver step report explains equation solve", () =>
{
    var doc = NodParser.Parse("""
    Name Solve equation
    mode equation
    given m = 80
    given v = 12
    equation E = 0,5 * m * v^2
    solve E
    constraint E >= 0
    end
    """);

    var report = SolverStepBuilder.Build(doc, "0");
    AssertText("E = 5760", report.ResultText);
    if (!report.Steps.Any(step => step.Contains("Bekende waarden", StringComparison.OrdinalIgnoreCase)))
        throw new Exception("Equation solver report should list known values.");
    if (!report.Steps.Any(step => step.Contains("Antwoord", StringComparison.OrdinalIgnoreCase)))
        throw new Exception("Equation solver report should include the answer step.");
});

Test("solver step report rounds numeric intersection for students", () =>
{
    var doc = NodParser.Parse("""
    Name Snijpunt
    mode equation
    given y = 20
    equation y = x * 2
    solve x
    end
    """);

    var report = SolverStepBuilder.Build(doc, "0");
    AssertText("x = 10", report.ResultText);
    if (report.RuleCard is null || report.RuleCard.FormulaMathMl.Contains("x</mi><mo>=</mo><mtext>snijpunt", StringComparison.OrdinalIgnoreCase))
        throw new Exception("Equation rule card should explain left equals right as a graph intersection, not x = snijpunt.");
});

Test("solver step report explains calculus solve", () =>
{
    var doc = NodParser.Parse("""
    Name Solve diff
    format ##.00
    solve diff ans^2
    end
    """);

    var report = SolverStepBuilder.Build(doc, "3");
    AssertText("f'(x) = 2x; x = 0", report.ResultText);
    if (report.Steps.Any(step => step.Contains("centrale differentie", StringComparison.OrdinalIgnoreCase) ||
                                 step.Contains("diff ans", StringComparison.OrdinalIgnoreCase) ||
                                 step.Contains("Bewerking:", StringComparison.OrdinalIgnoreCase)))
        throw new Exception("Power-rule solver report should not show technical engine steps.");
    if (!report.Steps.First().Contains("f(x)", StringComparison.OrdinalIgnoreCase))
        throw new Exception("Power-rule solver report should start with the visible function.");
    if (!report.StepMathMl.Any(step => step.Contains("power-rule-animation", StringComparison.OrdinalIgnoreCase)))
        throw new Exception("Power-rule derivative should include the educational SVG animation.");
});

Test("calculus limit sin x over x", () =>
{
    var doc = NodParser.Parse("""
    Name Limiet
    format ##.00
    math limit 0 sin(ans)/ans
    end
    """);

    var result = NodEngine.ConvertForward(doc, "0");
    AssertNear(1m, NodParser.ParseFlexibleDecimal(result.Text), 0.01m);
});



Test("calculus derivative ans", () =>
{
    var doc = NodParser.Parse("""
    Name Differentiaal van ans
    format ##.00
    math diff ans
    end
    """);

    var result = NodEngine.ConvertForward(doc, "123");
    AssertNear(1m, NodParser.ParseFlexibleDecimal(result.Text), 0.01m);
});



Test("parser auto normalizes concatenated nod", () =>
{
    var nod = "Name Celsius naar Fahrenheitinput1 Celsiusinput2 Fahrenheitformat ##.00math ans * 1,8math ans + 32end";
    var doc = NodParser.Parse(nod);
    var result = NodEngine.ConvertForward(doc, "22");
    AssertText("71.60", result.Text);
});

Test("normalizer repairs concatenated nod metadata lines", () =>
{
    var nod = "Name Ans maal e kwadraatURLN e-kwadraat conversieinput1 getalinput2 resultaatResult resformat ##.00math ans * e^2end";
    var normalized = NodTextNormalizer.NormalizeForEditor(nod);
    AssertContains($"{Environment.NewLine}URLN e-kwadraat conversie", normalized);
    AssertContains($"{Environment.NewLine}input1 getal", normalized);
    AssertContains($"{Environment.NewLine}input2 resultaat", normalized);
    AssertContains($"{Environment.NewLine}Result res", normalized);
});

Test("parser marks input1 input2 as deprecated until 3.0", () =>
{
    var doc = NodParser.Parse("""
    Name Legacy labels
    input1 Celsius
    input2 Fahrenheit
    math ans * 1,8
    math ans + 32
    end
    """);

    AssertText("Celsius", doc.LegacyInput1Label ?? "");
    AssertText("Fahrenheit", doc.LegacyInput2Label ?? "");
    AssertDecimal(1m, doc.Inputs.Count);
    AssertText("x", doc.Inputs[0].Name);
    AssertText("Celsius", doc.Inputs[0].Label ?? "");
    AssertDecimal(1m, doc.Outputs.Count);
    AssertText("y", doc.Outputs[0].Name);
    AssertText("Fahrenheit", doc.Outputs[0].Label ?? "");
    AssertDecimal(2m, doc.Deprecations.Count);
    if (!doc.Deprecations[0].Message.Contains("2.0 beta"))
        throw new Exception("input1 deprecation should explain Syscalculator 2.0 beta compatibility.");
    if (!doc.Deprecations[0].Message.Contains("supported through 2.x"))
        throw new Exception("input1 deprecation should promise support through 2.x.");
    if (!doc.Deprecations[0].Message.Contains("input x"))
        throw new Exception("input1 deprecation should point to input x.");
    if (!doc.Deprecations[1].Message.Contains("output y"))
        throw new Exception("input2 deprecation should point to output y.");

    AssertNear(22m, NodParser.ParseFlexibleDecimal(NodEngine.ConvertReverse(doc, "71.60").Text), 0.0001m);
});

Test("parser supports inputr as modern reverse output label", () =>
{
    var doc = NodParser.Parse("""
    Name Modern reverse labels
    input x Celsius
    inputr Fahrenheit
    math ans * 1,8
    math ans + 32
    end
    """);

    AssertDecimal(1m, doc.Inputs.Count);
    AssertText("x", doc.Inputs[0].Name);
    AssertText("Celsius", doc.Inputs[0].Label ?? "");
    AssertDecimal(1m, doc.Outputs.Count);
    AssertText("y", doc.Outputs[0].Name);
    AssertText("Fahrenheit", doc.Outputs[0].Label ?? "");
    AssertDecimal(0m, doc.Deprecations.Count);
    AssertNear(71.6m, NodParser.ParseFlexibleDecimal(NodEngine.ConvertForward(doc, "22").Text), 0.0001m);
    AssertNear(22m, NodParser.ParseFlexibleDecimal(NodEngine.ConvertReverse(doc, "71.60").Text), 0.0001m);
});

Test("parser supports inputr y long label form", () =>
{
    var doc = NodParser.Parse("""
    Name Modern reverse label explicit
    input x Meter
    inputr y Centimeter
    end
    """);

    AssertDecimal(1m, doc.Outputs.Count);
    AssertText("y", doc.Outputs[0].Name);
    AssertText("Centimeter", doc.Outputs[0].Label ?? "");
});

Test("parser rejects input2 for 2d vector nod", () =>
{
    AssertThrows("input2 is not supported for 2D vector or 2x2 matrix NOD. Use explicit input x/input y for values and inputr y/output y for the reverse or result side.", () => NodParser.Parse("""
    Name 2D vector arrow
    input1 X component
    input2 Y component
    end
    """));
});

Test("parser rejects input2 mixed with modern multi input", () =>
{
    AssertThrows("input2 is not supported for 2D vector or 2x2 matrix NOD. Use explicit input x/input y for values and inputr y/output y for the reverse or result side.", () => NodParser.Parse("""
    Name Mixed vector inputs
    input1 X component
    input2 Result side
    input y Y component
    end
    """));
});

Test("parser supports named input output without deprecation", () =>
{
    var doc = NodParser.Parse("""
    Name Named values
    input x X coordinate
    input y Y coordinate
    output screenPoint Screen point
    end
    """);

    AssertDecimal(2m, doc.Inputs.Count);
    AssertText("x", doc.Inputs[0].Name);
    AssertText("X coordinate", doc.Inputs[0].Label ?? "");
    AssertText("y", doc.Inputs[1].Name);
    AssertText("screenPoint", doc.Outputs[0].Name);
    AssertText("Screen point", doc.Outputs[0].Label ?? "");
    AssertDecimal(0m, doc.Deprecations.Count);
});

Test("parser keeps xyz inputs as graph3d metadata", () =>
{
    var doc = NodParser.Parse("""
    Name 3D point
    input x X coordinate
    input y Y coordinate
    input z Z coordinate
    output point 3D point
    end
    """);

    AssertDecimal(3m, doc.Inputs.Count);
    AssertText("x", doc.Inputs[0].Name);
    AssertText("y", doc.Inputs[1].Name);
    AssertText("z", doc.Inputs[2].Name);
    AssertText("Z coordinate", doc.Inputs[2].Label ?? "");
    AssertText("point", doc.Outputs[0].Name);
    AssertDecimal(0m, doc.Deprecations.Count);
});

Test("parser supports geometry mode for graph3d math", () =>
{
    var doc = NodParser.Parse("""
    Name Geometry 3D vector
    mode geometry
    input x X coordinate
    input y Y coordinate
    input z Z coordinate
    math length(vec(3,4,12))
    end
    """);

    AssertText("geometry", doc.Mode ?? "");
    AssertDecimal(3m, doc.Inputs.Count);
    AssertText("z", doc.Inputs[2].Name);
    var result = NodEngine.ConvertForward(doc, "0");
    AssertDecimal(13m, result.NumericValue ?? 0);
});

Test("parser supports matrix 3x3 mode", () =>
{
    var doc = NodParser.Parse("""
    Name Matrix 3x3
    mode matrix3x3
    input text Matrix values
    math det(mat3(1,2,3,0,1,4,5,6,0))
    end
    """);

    AssertText("matrix3x3", doc.Mode ?? "");
    var result = NodEngine.ConvertForward(doc, "0");
    AssertDecimal(1m, result.NumericValue ?? 0);
});

Test("parser supports text input for trans chg tools", () =>
{
    var doc = NodParser.Parse("""
    Name Text replacement
    input text Original text
    trans "goedemorgen","good morning"
    end
    """);

    AssertDecimal(1m, doc.Inputs.Count);
    AssertText("text", doc.Inputs[0].Name);
    AssertText("text", doc.Inputs[0].Kind ?? "");
    AssertText("Original text", doc.Inputs[0].Label ?? "");
    AssertDecimal(0m, doc.Deprecations.Count);
    AssertText("good morning", NodEngine.ConvertForward(doc, "goedemorgen").Text);
});

Test("parser supports named text input", () =>
{
    var doc = NodParser.Parse("""
    Name Text prefix
    input text phone
    chg "03402","03060"
    end
    """);

    AssertText("phone", doc.Inputs[0].Name);
    AssertText("text", doc.Inputs[0].Kind ?? "");
    AssertText("", doc.Inputs[0].Label ?? "");
    AssertText("0306036647", NodEngine.ConvertForward(doc, "0340236647").Text);
});

Test("parser supports phone input kind", () =>
{
    var doc = NodParser.Parse("""
    Name Telefoonnummer omnummeren
    input telefoon Telefoonnummer
    chg "03402","03060"
    end
    """);

    AssertDecimal(1m, doc.Inputs.Count);
    AssertText("phone", doc.Inputs[0].Name);
    AssertText("phone", doc.Inputs[0].Kind ?? "");
    AssertText("Telefoonnummer", doc.Inputs[0].Label ?? "");
    AssertText("0306036647", NodEngine.ConvertForward(doc, "0340236647").Text);
});

Test("parser supports named phone input alias", () =>
{
    var doc = NodParser.Parse("""
    Name Phone alias
    input phone mobile
    end
    """);

    AssertText("mobile", doc.Inputs[0].Name);
    AssertText("phone", doc.Inputs[0].Kind ?? "");
});

Test("formula card catalog includes education and export fields", () =>
{
    var cards = FormulaCardCatalog.GetDefaultCards();
    if (cards.Count < 6)
        throw new Exception("Expected at least six default formula cards.");

    var vector = cards.First(card => card.Id == "vector-2d-arrow");
    if (!vector.LevelTags.Contains("2D graph"))
        throw new Exception("2D vector card should be tagged for 2D graph.");
    if (!vector.LevelTags.Contains("Limited vector"))
        throw new Exception("2D vector card should be tagged as limited vector.");
    if (!vector.Latex.Contains(@"\vec"))
        throw new Exception("2D vector card should include LaTeX vector notation.");
    if (!vector.MathMl.Contains("&#x2192;"))
        throw new Exception("2D vector card should include MathML arrow notation.");
    if (vector.ExampleNod.Contains("input z"))
        throw new Exception("2D vector card should not use input z.");

    var vectorDoc = NodParser.Parse(vector.ExampleNod);
    AssertText("2D vectorpijl notitie", vectorDoc.Name ?? "");
    AssertDecimal(2m, vectorDoc.Inputs.Count);
    AssertText("x", vectorDoc.Inputs[0].Name);
    AssertText("y", vectorDoc.Inputs[1].Name);

    var matrix = cards.First(card => card.Id == "matrix-2x2-determinant");
    if (!matrix.LevelTags.Contains("PWS"))
        throw new Exception("Matrix card should be tagged for PWS.");
    if (!matrix.LevelTags.Contains("Limited matrix"))
        throw new Exception("Matrix card should be tagged as limited matrix.");
    if (!matrix.Latex.Contains(@"\det"))
        throw new Exception("Matrix card should include LaTeX determinant.");
    if (!matrix.MathMl.Contains("<mtable"))
        throw new Exception("Matrix card should include MathML matrix table.");
    if (matrix.ExampleNod.Contains("input z"))
        throw new Exception("Matrix 2x2 card should not use input z or imply 3x3/3D support.");

    var doc = NodParser.Parse(matrix.ExampleNod);
    AssertText("2x2 matrix determinant notitie", doc.Name ?? "");
    AssertDecimal(1m, doc.Inputs.Count);
    AssertText("text", doc.Inputs[0].Name);
    AssertText("text", doc.Inputs[0].Kind ?? "");
});

Test("formula card closed line integral works as education card", () =>
{
    var card = FormulaCardCatalog.GetDefaultCards().First(card => card.Id == "circle-integral");

    if (!card.Latex.Contains(@"\oint"))
        throw new Exception("Closed line integral card should include LaTeX oint.");
    if (!card.MathMl.Contains("&oint;"))
        throw new Exception("Closed line integral card should include MathML oint.");
    if (!card.LevelTags.Contains("Wiskunde D verdieping"))
        throw new Exception("Closed line integral card should be tagged for Wiskunde D verdieping.");
    if (!card.LevelTags.Contains("Propedeuse"))
        throw new Exception("Closed line integral card should be tagged for propedeuse.");

    var doc = NodParser.Parse(card.ExampleNod);
    AssertText("Kringintegraal notitie", doc.Name ?? "");
    AssertText("", doc.Mode ?? "");
    AssertDecimal(1m, doc.Inputs.Count);
    AssertText("text", doc.Inputs[0].Name);
    AssertText("text", doc.Inputs[0].Kind ?? "");
    AssertText("Beschrijving van formule", doc.Inputs[0].Label ?? "");
    AssertDecimal(0m, doc.Deprecations.Count);
});

Test("formula card catalog covers havo vwo formula table topics", () =>
{
    var cards = FormulaCardCatalog.GetDefaultCards();
    var requiredIds = new[]
    {
        "quadratic-formula",
        "exponential-growth",
        "trig-right-triangle",
        "linear-function",
        "sine-rule",
        "cosine-rule",
        "trig-identities",
        "exact-trig-values",
        "circle-equation",
        "special-right-triangles",
        "derivative-sum-rule",
        "derivative-constant-factor",
        "derivative-product-rule",
        "derivative-quotient-rule",
        "derivative-chain-rule",
        "derivative-trig-basic",
        "derivative-exp-log",
        "integral-power-rule",
        "integral-sum-rule",
        "integral-constant-factor",
        "integral-definite-area",
        "point-line-distance"
    };

    foreach (var id in requiredIds)
    {
        var card = cards.FirstOrDefault(card => card.Id == id)
            ?? throw new Exception($"Expected formula card '{id}'.");

        if (string.IsNullOrWhiteSpace(card.MathMl) || !card.MathMl.Contains("<math"))
            throw new Exception($"Formula card '{id}' should include MathML.");
        if (string.IsNullOrWhiteSpace(card.Latex))
            throw new Exception($"Formula card '{id}' should include LaTeX.");
        if (card.LevelTags.Count == 0)
            throw new Exception($"Formula card '{id}' should include level tags.");
    }

    if (!cards.First(card => card.Id == "quadratic-formula").LevelTags.Contains("HAVO B"))
        throw new Exception("Quadratic formula should be tagged for HAVO B.");
    if (!cards.First(card => card.Id == "integral-power-rule").LevelTags.Contains("VWO B"))
        throw new Exception("Integral power rule should be tagged for VWO B.");
    if (!cards.First(card => card.Id == "derivative-product-rule").LevelTags.Contains("Differentiatie"))
        throw new Exception("Derivative product rule should be tagged for differentiatie.");
    if (!cards.First(card => card.Id == "derivative-trig-basic").LevelTags.Contains("Differentiatie"))
        throw new Exception("Derivative trig card should be tagged for differentiatie.");
    if (!cards.First(card => card.Id == "integral-sum-rule").LevelTags.Contains("Integralen"))
        throw new Exception("Integral sum rule should be tagged for integralen.");
    if (!cards.First(card => card.Id == "integral-definite-area").LevelTags.Contains("Examenbasis"))
        throw new Exception("Definite integral card should be tagged for exam basics.");
    if (cards.Any(card => card.LevelTags.Any(tag => tag.StartsWith("SE", StringComparison.OrdinalIgnoreCase))))
        throw new Exception("Formula cards should be grouped by topic, not school exam period.");
    if (!cards.First(card => card.Id == "sine-rule").LevelTags.Contains("Goniometrie"))
        throw new Exception("Sine rule should be tagged for goniometry.");
    if (!cards.First(card => card.Id == "linear-function").LevelTags.Contains("Meetkunde met coordinaten"))
        throw new Exception("Linear function should be tagged for coordinate geometry.");
    if (!cards.First(card => card.Id == "point-line-distance").LevelTags.Contains("2D"))
        throw new Exception("Point-line distance should be tagged for 2D.");
    if (!cards.First(card => card.Id == "circle-equation").LevelTags.Contains("2D"))
        throw new Exception("Circle equation should be tagged for 2D.");
});

Test("parser supports texta textb inputs for two text values", () =>
{
    var doc = NodParser.Parse("""
    Name Text compare
    input texta Source text
    input textb Target text
    trans "oud","nieuw"
    end
    """);

    AssertDecimal(2m, doc.Inputs.Count);
    AssertText("texta", doc.Inputs[0].Name);
    AssertText("text", doc.Inputs[0].Kind ?? "");
    AssertText("Source text", doc.Inputs[0].Label ?? "");
    AssertText("textb", doc.Inputs[1].Name);
    AssertText("text", doc.Inputs[1].Kind ?? "");
    AssertText("Target text", doc.Inputs[1].Label ?? "");
});

Test("parser maps legacy text converters to texta textb before 3.0", () =>
{
    var doc = NodParser.Parse("""
    Name Legacy text translation
    input1 Nederlands
    input2 Engels
    trans "goedemorgen","good morning"
    end
    """);

    AssertDecimal(2m, doc.Inputs.Count);
    AssertText("texta", doc.Inputs[0].Name);
    AssertText("Nederlands", doc.Inputs[0].Label ?? "");
    AssertText("textb", doc.Inputs[1].Name);
    AssertText("Engels", doc.Inputs[1].Label ?? "");
    AssertText("text", doc.Inputs[0].Kind ?? "");
    AssertText("text", doc.Inputs[1].Kind ?? "");
    if (!doc.Deprecations[0].Message.Contains("input texta"))
        throw new Exception("input1 text deprecation should point to input texta.");
    if (!doc.Deprecations[1].Message.Contains("input textb"))
        throw new Exception("input2 text deprecation should point to input textb.");
});

Test("parser reports line number for legacy anse typo", () =>
{
    AssertThrows(
        "Line 3: use 'ans * e^2', not 'anse^2'.",
        () => NodParser.Parse("""
        Name Broken
        format ##.00
        math anse^2
        end
        """));
});

Test("postcode address trans quoted comma", () =>
{
    var doc = NodParser.Parse("""
    Name Postcode naar adres demo
    input1 Postcode huisnummer
    input2 Adres
    trans "2566 GB 241","Nieboerweg 241, 2566 GB Den Haag"
    end
    """);

    var result = NodEngine.ConvertForward(doc, "2566 GB 241");
    AssertText("Nieboerweg 241, 2566 GB Den Haag", result.Text);

    var compact = NodEngine.ConvertForward(doc, "2566GB 241");
    AssertText("Nieboerweg 241, 2566 GB Den Haag", compact.Text);

    var noSpace = NodEngine.ConvertForward(doc, "2566GB241");
    AssertText("Nieboerweg 241, 2566 GB Den Haag", noSpace.Text);
});

Test("smart trans ignores spaces and punctuation", () =>
{
    var doc = NodParser.Parse("""
    Name Nederlands naar Engels demo
    input1 Nederlands
    input2 Engels
    trans "goedemorgen","good morning"
    trans "dank je","thank you"
    trans "hallo","hello"
    end
    """);

    AssertText("good morning", NodEngine.ConvertForward(doc, "goede-morgen!").Text);
    AssertText("good morning", NodEngine.ConvertForward(doc, "goede_morgen").Text);
    AssertText("thank you", NodEngine.ConvertForward(doc, "dankje").Text);
    AssertText("hello", NodEngine.ConvertForward(doc, "hello").Text);
    AssertText("dank je", NodEngine.ConvertReverse(doc, "thank-you!").Text);
    AssertText("dank je", NodEngine.ConvertReverse(doc, "thank_you").Text);
    AssertText("hallo", NodEngine.ConvertReverse(doc, "hallo").Text);
});

Test("smart chg supports decibel phone renumbering", () =>
{
    var doc = NodParser.Parse("""
    Name Operatie Decibel 1995 demo
    input1 Oud telefoonnummer
    input2 Nieuw telefoonnummer
    chg "03489-xxxx","0348-69xxxx"
    chg "01711-xxxxx","071-30xxxxx"
    chg "01751-xxxxx","070-51xxxxx"
    end
    """);

    AssertText("0348-691234", NodEngine.ConvertForward(doc, "03489-1234").Text);
    AssertText("0348-691234", NodEngine.ConvertForward(doc, "034891234").Text);
    AssertText("03489-1234", NodEngine.ConvertReverse(doc, "0348-691234").Text);
    AssertText("070-5112345", NodEngine.ConvertForward(doc, "01751-12345").Text);
    AssertText("01751-12345", NodEngine.ConvertReverse(doc, "070-5112345").Text);
});

Test("smart chg prefers most specific prefix", () =>
{
    var doc = NodParser.Parse("""
    Name Decibel afwijkende reeks
    input1 Oud telefoonnummer
    input2 Nieuw telefoonnummer
    chg "050-xxxxxx","050-3xxxxxx"
    chg "050-2xxxxx","050-52xxxxx"
    end
    """);

    AssertText("050-5212345", NodEngine.ConvertForward(doc, "050-212345").Text);
    AssertText("050-3999999", NodEngine.ConvertForward(doc, "050-999999").Text);
    AssertText("050-212345", NodEngine.ConvertReverse(doc, "050-5212345").Text);
});

Test("legacy chg prefix rules remain compatible", () =>
{
    var doc = NodParser.Parse("""
    Name Legacy chg test
    input1 Oud
    input2 Nieuw

    chg "01100-","0113-2"
    chg "01711-","071-30"
    chg "03489-","0348-69"

    end
    """);

    AssertText("0113-212345", NodEngine.ConvertForward(doc, "01100-12345").Text);
    AssertText("071-3012345", NodEngine.ConvertForward(doc, "01711-12345").Text);
    AssertText("0348-6912345", NodEngine.ConvertForward(doc, "03489-12345").Text);
});

Test("nod 2 pattern chg captures x positions", () =>
{
    var doc = NodParser.Parse("""
    Name Pattern chg test
    input1 Oud
    input2 Nieuw

    chg "0183x-xx xx","0183-x0 xx xx"

    end
    """);

    AssertText("0183-501234", NodEngine.ConvertForward(doc, "01835-1234").Text);
    AssertText("0183-501234", NodEngine.ConvertForward(doc, "018351234").Text);
    AssertText("0183-501234", NodEngine.ConvertForward(doc, "01835 1234").Text);
    AssertText("0183-501234", NodEngine.ConvertForward(doc, "0183 5 12 34").Text);
    AssertText("0183-109876", NodEngine.ConvertForward(doc, "01831-9876").Text);
    AssertText("0183-109876", NodEngine.ConvertForward(doc, "018319876").Text);
});

Test("invalid pattern chg is rejected", () =>
{
    var doc = NodParser.Parse("""
    Name Invalid pattern test
    input1 Oud
    input2 Nieuw

    chg "013x-xx x","0183-x0 xx xx"

    end
    """);

    AssertThrows(
        "Invalid chg pattern: left pattern captures 4 x-position(s), but right pattern uses 5 x-position(s).",
        () => NodEngine.ConvertForward(doc, "0131-2345"));
});

Test("full decibel table parses and converts known cases", () =>
{
    var path = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..",
        "syscalculator",
        "Converters",
        "Text",
        "operatie_decibel_1995_demo.nod"));

    var doc = NodParser.Parse(File.ReadAllText(path));

    AssertText("070-5112345", NodEngine.ConvertForward(doc, "01751-12345").Text);
    AssertText("050-5212345", NodEngine.ConvertForward(doc, "050-212345").Text);
    AssertText("024-671234", NodEngine.ConvertForward(doc, "08897-1234").Text);
});

Test("all legacy Syscalculator 1.74 nod files parse", () =>
{
    var root = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..",
        "legacy",
        "Syscalculator174.VB6"));

    var files = Directory.GetFiles(root, "*.nod", SearchOption.AllDirectories);
    if (files.Length == 0)
        throw new Exception("No legacy .nod files found.");

    foreach (var file in files)
    {
        try
        {
            NodParser.Parse(File.ReadAllText(file));
        }
        catch (Exception ex)
        {
            var relative = Path.GetRelativePath(root, file);
            throw new Exception($"{relative}: {ex.Message}");
        }
    }
});


Console.WriteLine();
Console.WriteLine($"Passed {passed}/{total} tests.");
if (passed != total) Environment.Exit(1);
Console.WriteLine("All tests passed.");

void Test(string name, Action action)
{
    total++;

    try
    {
        action();
        passed++;
        Console.WriteLine("[PASS] " + name);
    }
    catch (Exception ex)
    {
        Console.WriteLine("[FAIL] " + name + ": " + ex.Message);
    }
}

static void AssertText(string expected, string actual)
{
    if (!string.Equals(expected, actual, StringComparison.Ordinal))
        throw new Exception($"Expected text '{expected}', got '{actual}'.");
}

static void AssertContains(string expected, string actual)
{
    if (!actual.Contains(expected, StringComparison.Ordinal))
        throw new Exception($"Expected text to contain '{expected}', got '{actual}'.");
}

static void AssertDecimal(decimal expected, decimal actual)
{
    if (expected != actual)
        throw new Exception($"Expected decimal {expected}, got {actual}.");
}

static void AssertNear(decimal expected, decimal actual, decimal tolerance)
{
    if (Math.Abs(expected - actual) > tolerance)
        throw new Exception($"Expected near {expected}, got {actual}.");
}

static void AssertTrue(bool condition, string message)
{
    if (!condition)
        throw new Exception(message);
}

static void AssertThrows(string expectedMessage, Action action)
{
    try
    {
        action();
    }
    catch (Exception ex)
    {
        if (!string.Equals(expectedMessage, ex.Message, StringComparison.Ordinal))
            throw new Exception($"Expected error '{expectedMessage}', got '{ex.Message}'.");

        return;
    }

    throw new Exception($"Expected error '{expectedMessage}', but no exception was thrown.");
}

