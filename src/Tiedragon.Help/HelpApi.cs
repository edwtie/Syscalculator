#nullable enable

namespace Tiedragon.Help;

public delegate string? HelpTextResolver(string key);
public delegate string? HelpContentResolver(string fileName);

// Zoek/commentaar: Bundelt labels voor de helpnavigatie.
public sealed record HelpNavigationLabels(string Home, string Previous, string Next);

// Zoek/commentaar: Beschrijft een compleet helpvenster.
public sealed record HelpDialogOptions(
    string Title,
    IReadOnlyList<NodHelpPage> Pages,
    string? SelectedPageId = null,
    HelpNavigationLabels? Navigation = null,
    bool OkOnly = false,
    string OkText = "OK",
    bool ShowTopics = true);

// Zoek/commentaar: Centrale API voor helpvensters, taalcontent en help-assets.
public static class HelpApi
{
    public static DialogResult ShowDialog(IWin32Window owner, HelpDialogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var navigation = options.Navigation ?? new HelpNavigationLabels("Home", "Previous", "Next");
        using var form = new NodHelpForm(
            options.Title,
            options.Pages,
            options.SelectedPageId,
            navigation.Home,
            navigation.Previous,
            navigation.Next,
            options.OkOnly,
            options.OkText,
            options.ShowTopics);

        if (owner is Form ownerForm)
        {
            form.TopMost = ownerForm.TopMost;
            if (ownerForm.Icon is not null)
            {
                form.ShowIcon = true;
                form.Icon = (Icon)ownerForm.Icon.Clone();
            }
        }

        return form.ShowDialog(owner);
    }

    public static string Text(HelpTextResolver resolveText, string key, string fallback)
    {
        return resolveText(key) ?? fallback;
    }

    public static string LanguageCodeFromFileName(string fileName)
    {
        var slashIndex = Math.Max(fileName.LastIndexOf('\\'), fileName.LastIndexOf('/'));
        if (slashIndex >= 0)
            fileName = fileName[(slashIndex + 1)..];

        if (fileName.EndsWith(".lng", StringComparison.OrdinalIgnoreCase))
            fileName = fileName[..^4];

        return fileName.ToLowerInvariant();
    }

    public static string Content(
        string languageCode,
        HelpTextResolver resolveText,
        string key,
        string fileName,
        string fallback = "",
        HelpContentResolver? resolveContent = null)
    {
        var template = resolveContent?.Invoke(fileName) ?? HelpHtml.Content(fileName, fallback);
        var content = HelpHtml.ContainsLanguagePlaceholders(template) ||
                      languageCode.Equals("eng", StringComparison.OrdinalIgnoreCase)
            ? template
            : Text(resolveText, key, template);

        return ApplyLanguagePlaceholders(resolveText, content);
    }

    public static string Content(
        string languageCode,
        HelpTextResolver resolveText,
        string key,
        string fileName,
        IReadOnlyDictionary<string, string?> placeholders,
        string fallback = "",
        HelpContentResolver? resolveContent = null)
    {
        return HelpHtml.ApplyContentPlaceholders(
            Content(languageCode, resolveText, key, fileName, fallback, resolveContent),
            placeholders);
    }

    public static string ApplyLanguagePlaceholders(HelpTextResolver resolveText, string template)
    {
        return HelpHtml.ApplyLanguagePlaceholders(template, key => resolveText(key));
    }

    public static string ScreenshotImage(
        string languageCode,
        HelpTextResolver resolveText,
        string fileName,
        string altText)
    {
        return HelpHtml.BuildScreenshotImageTag(languageCode, fileName, altText, key => resolveText(key));
    }

    public static string MainHelpCss() => HelpHtml.MainHelpCss();

    public static string NodHelpCss() => HelpHtml.NodHelpCss();

    public static string NodPopupCss() => HelpHtml.NodPopupCss();
}
