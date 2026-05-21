#nullable enable
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Tiedragon.Help;

// Zoek/commentaar: Laadt bewerkbare help-templates en vult dynamische stukken vanuit C#.
public static class HelpHtml
{
    private const string LanguagePlaceholderPattern = @"\[\s*(?<key>[A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z0-9_]+)+)\s*\]";
    private const string TopicPageTemplateFallback = """
        <!doctype html>
        <html>
        <head>
        <meta charset="utf-8" />
        <meta http-equiv="X-UA-Compatible" content="IE=edge" />
        <style>
        {{css}}
        </style>
        {{headTail}}
        </head>
        <body><h1>{{title}}</h1>{{body}}{{bodyTail}}</body>
        </html>
        """;

    private const string BodyPageTemplateFallback = """
        <!doctype html>
        <html>
        <head>
        <meta charset="utf-8" />
        <meta http-equiv="X-UA-Compatible" content="IE=edge" />
        <style>
        {{css}}
        </style>
        {{headTail}}
        </head>
        <body>{{body}}{{bodyTail}}</body>
        </html>
        """;

    private const string NodPopupCssFallback = "body { margin:0; padding:10px 13px 7px; font-family:Segoe UI, Arial, sans-serif; font-size:12px; color:#1f2937; background:transparent; } .title, b { color:#0f3f8f; } .insert { display:inline-block; margin-top:6px; padding:5px 10px; border-radius:999px; background:#0f3f8f; color:#fff; text-decoration:none; font-weight:700; }";
    private const string NodHelpCssFallback = "body { margin:0; padding:24px 24px 34px; font-family:Segoe UI, Arial, sans-serif; color:#1f2937; background:#ffffff; } h1, h2 { color:#0f3f8f; } pre { background:#101827; color:#e5eefc; padding:12px; border-radius:8px; white-space:pre-wrap; } code { background:#eef4ff; color:#0f3f8f; padding:1px 5px; border-radius:5px; } .notice { background:#fff8e6; border:1px solid #f4d184; border-left:4px solid #d69400; border-radius:8px; padding:10px 12px; margin:10px 0 14px; color:#3f2f12; } .warning-sign { display:flex; align-items:flex-start; gap:12px; background:#fff8e1; border:1px solid #fbbf24; border-left:6px solid #dc2626; border-radius:8px; padding:12px 14px; margin:12px 0 16px; color:#7f1d1d; }";
    private const string MainHelpCssFallback = "body { font-family:Segoe UI, Arial, sans-serif; margin:22px 38px 22px 22px; color:#1f2937; line-height:1.5; background:#fff; } h1, h2 { color:#1d4f91; }";

    private static string HelpDirectory => Path.Combine(AppContext.BaseDirectory, "Resources", "Help");

    public static string WrapTopicPage(string title, string body, string css, string? bodyTail = null)
    {
        return ApplyTemplate(ReadHelpFile("document-topic.html", TopicPageTemplateFallback), new Dictionary<string, string?>
        {
            ["title"] = WebUtility.HtmlEncode(title),
            ["body"] = body,
            ["css"] = css,
            ["headTail"] = "",
            ["bodyTail"] = bodyTail ?? ""
        });
    }

    public static string WrapBodyPage(string body, string css, string? headTail = null, string? bodyTail = null)
    {
        return ApplyTemplate(ReadHelpFile("document-body.html", BodyPageTemplateFallback), new Dictionary<string, string?>
        {
            ["body"] = body,
            ["css"] = css,
            ["headTail"] = headTail ?? "",
            ["bodyTail"] = bodyTail ?? ""
        });
    }

    public static string RenderTemplate(string fileName, IReadOnlyDictionary<string, string?> values)
    {
        return ApplyTemplate(ReadHelpFile(fileName, ""), values);
    }

    public static string Css(string fileName, string fallback = "")
    {
        return ReadHelpFile(fileName, fallback);
    }

    public static string Content(string fileName, string fallback = "")
    {
        return ReadHelpFile(Path.Combine("Content", fileName), fallback);
    }

    public static string ApplyContentPlaceholders(string template, IReadOnlyDictionary<string, string?> values)
    {
        var result = template;
        foreach (var (key, value) in values)
            result = result.Replace("{" + key + "}", value ?? "", StringComparison.Ordinal);

        return result;
    }

    public static string ApplyLanguagePlaceholders(string template, Func<string, string?> resolveText)
    {
        return Regex.Replace(
            template,
            LanguagePlaceholderPattern,
            match =>
            {
                var value = resolveText(match.Groups["key"].Value);
                return string.IsNullOrWhiteSpace(value) ? match.Value : value;
            },
            RegexOptions.CultureInvariant);
    }

    public static bool ContainsLanguagePlaceholders(string template)
    {
        return Regex.IsMatch(template, LanguagePlaceholderPattern, RegexOptions.CultureInvariant);
    }

    public static string MainHelpCss()
    {
        return Css("main-help.css", MainHelpCssFallback);
    }

    public static string NodPopupCss()
    {
        return Css("nod-popup.css", NodPopupCssFallback);
    }

    public static string NodHelpCss()
    {
        var stylesheetPath = Path.Combine(AppContext.BaseDirectory, "Resources", "nod-help.css");
        if (File.Exists(stylesheetPath))
            return File.ReadAllText(stylesheetPath);

        return NodHelpCssFallback;
    }

    public static string BuildScreenshotImageTag(
        string languageCode,
        string fileName,
        string altText,
        Func<string, string?>? resolveText = null)
    {
        var path = ResolveLocalizedImagePath(languageCode, fileName);
        if (!File.Exists(path))
            return "";

        var mime = GetMimeType(path);
        var base64 = mime == "image/svg+xml"
            ? Convert.ToBase64String(Encoding.UTF8.GetBytes(ReadSvgFile(path, resolveText)))
            : Convert.ToBase64String(File.ReadAllBytes(path));
        return $"""<div class="screenshot-frame"><img src="data:{mime};base64,{base64}" alt="{WebUtility.HtmlEncode(altText)}" /></div>""";
    }

    public static string NodCopyButtonsScript()
    {
        return ScriptTag("basis.js") + ScriptTag("nod.js") + InlineScript("window.syscalNodHelp && window.syscalNodHelp.installCopyButtons();");
    }

    public static string NodPopupHeightScript()
    {
        return ScriptTag("basis.js") + ScriptTag("nod.js") + InlineScript("window.syscalNodHelp && window.syscalNodHelp.installPopupHeight();");
    }

    public static string FormulaCardCopyButtonsScript()
    {
        return ScriptTag("basis.js") + ScriptTag("formula.js") + InlineScript("window.syscalFormulaHelp && window.syscalFormulaHelp.installCopyButtons();");
    }

    public static string FormulaSearchScript()
    {
        return ReadHelpFile("basis.js", "") + Environment.NewLine +
            ReadHelpFile("formula.js", "") + Environment.NewLine +
            "window.syscalFormulaHelp && window.syscalFormulaHelp.installSearch();";
    }

    public static string FormulaFilmScript()
    {
        return ScriptTag("basis.js") + ScriptTag("formula.js") + InlineScript("window.syscalFormulaHelp && window.syscalFormulaHelp.installFilm();");
    }

    private static string ScriptTag(string fileName)
    {
        var script = ReadHelpFile(fileName, "");
        return string.IsNullOrWhiteSpace(script) ? "" : $"<script>\n{script}\n</script>";
    }

    private static string InlineScript(string script)
    {
        return string.IsNullOrWhiteSpace(script) ? "" : $"<script>\n{script}\n</script>";
    }

    private static string ReadSvgFile(string path, Func<string, string?>? resolveText)
    {
        var svg = File.ReadAllText(path, Encoding.UTF8);
        if (resolveText is null)
            return svg;

        return ApplyLanguagePlaceholders(svg, key =>
        {
            var value = resolveText(key);
            return value is null ? null : WebUtility.HtmlEncode(value);
        });
    }

    private static string ApplyTemplate(string template, IReadOnlyDictionary<string, string?> values)
    {
        var result = template;
        foreach (var (key, value) in values)
            result = result.Replace("{{" + key + "}}", value ?? "", StringComparison.Ordinal);

        return result;
    }

    private static string ReadHelpFile(string fileName, string fallback)
    {
        var path = Path.Combine(HelpDirectory, fileName);
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : fallback;
        }
        catch (IOException)
        {
            return fallback;
        }
        catch (UnauthorizedAccessException)
        {
            return fallback;
        }
    }

    private static string ResolveLocalizedImagePath(string languageCode, string fileName)
    {
        var resourcesPath = Path.Combine(AppContext.BaseDirectory, "Resources");
        var normalizedLanguageCode = string.IsNullOrWhiteSpace(languageCode)
            ? "eng"
            : languageCode.ToLowerInvariant();
        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var candidates = new[]
        {
            Path.Combine(resourcesPath, $"{name}.{normalizedLanguageCode}.svg"),
            Path.Combine(resourcesPath, $"{name}.{normalizedLanguageCode}{extension}"),
            Path.Combine(resourcesPath, $"{name}.svg"),
            Path.Combine(resourcesPath, fileName)
        };

        return candidates.FirstOrDefault(File.Exists) ?? Path.Combine(resourcesPath, fileName);
    }

    private static string GetMimeType(string path)
    {
        return Path.GetExtension(path).TrimStart('.').ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "svg" => "image/svg+xml",
            _ => "image/png"
        };
    }
}
