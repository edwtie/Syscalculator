#nullable enable
using Tiedragon.LanguagePackage;

namespace Tiedragon.ToolEditor;

internal enum ToolEditorHtmlViewMode
{
    Source,
    Edit
}

internal enum ToolEditorPreviewKind
{
    None,
    Html,
    Language,
    Json,
    Image
}

internal readonly record struct ToolEditorHtmlViewDescriptor(
    ToolEditorPreviewKind PreviewKind,
    bool CanUseVisualEditor,
    bool CanUseHtmlToolbar,
    bool TreatEditAsPreview,
    string[] Extensions,
    Func<string, bool>? ContentDetector = null);

internal readonly record struct ToolEditorHtmlViewState(
    ToolEditorHtmlViewMode Mode,
    ToolEditorPreviewKind PreviewKind,
    bool CanUseHtmlToolbar,
    bool CanUseVisualEditor,
    bool CanUsePreviewPane,
    bool ShowPreviewPane,
    bool EditIsPreview);

internal static class ToolEditorHtmlViewApi
{
    private static readonly ToolEditorHtmlViewDescriptor NoViewDescriptor = new(
        ToolEditorPreviewKind.None,
        CanUseVisualEditor: false,
        CanUseHtmlToolbar: false,
        TreatEditAsPreview: false,
        []);

    private static readonly ToolEditorHtmlViewDescriptor[] ViewDescriptors =
    [
        new(
            ToolEditorPreviewKind.Html,
            CanUseVisualEditor: true,
            CanUseHtmlToolbar: true,
            TreatEditAsPreview: true,
            [".html", ".htm"],
            LooksLikeHtml),
        new(
            ToolEditorPreviewKind.Language,
            CanUseVisualEditor: false,
            CanUseHtmlToolbar: false,
            TreatEditAsPreview: false,
            [".lng"]),
        new(
            ToolEditorPreviewKind.Json,
            CanUseVisualEditor: false,
            CanUseHtmlToolbar: false,
            TreatEditAsPreview: false,
            [".json"]),
        new(
            ToolEditorPreviewKind.Image,
            CanUseVisualEditor: false,
            CanUseHtmlToolbar: false,
            TreatEditAsPreview: false,
            [".png", ".jpg", ".jpeg", ".svg", ".webp", ".gif", ".bmp"])
    ];

    public static ToolEditorHtmlViewState GetState(
        string packagePath,
        string sourceText,
        bool hasImage,
        bool editMode,
        bool previewInitialized,
        bool previewClosedByUser)
    {
        var mode = editMode ? ToolEditorHtmlViewMode.Edit : ToolEditorHtmlViewMode.Source;
        var descriptor = GetDescriptor(packagePath, sourceText, hasImage);
        var previewKind = mode == ToolEditorHtmlViewMode.Edit && !descriptor.TreatEditAsPreview
            ? ToolEditorPreviewKind.None
            : descriptor.PreviewKind;
        var canUseVisualEditor = !hasImage && descriptor.CanUseVisualEditor;
        var canUsePreviewPane = mode == ToolEditorHtmlViewMode.Source && descriptor.PreviewKind != ToolEditorPreviewKind.None;
        var showPreviewPane = canUsePreviewPane && previewInitialized && !previewClosedByUser;
        var editIsPreview = mode == ToolEditorHtmlViewMode.Edit && descriptor.TreatEditAsPreview;

        return new ToolEditorHtmlViewState(
            mode,
            previewKind,
            descriptor.CanUseHtmlToolbar,
            canUseVisualEditor,
            canUsePreviewPane,
            showPreviewPane,
            editIsPreview);
    }

    public static bool CanUseVisualEditor(string packagePath, string sourceText, bool hasImage) =>
        !hasImage && GetDescriptor(packagePath, sourceText, hasImage).CanUseVisualEditor;

    public static bool IsHtmlDocument(string packagePath, string sourceText)
    {
        return GetDescriptor(packagePath, sourceText, hasImage: false).PreviewKind == ToolEditorPreviewKind.Html;
    }

    public static ToolEditorPreviewKind GetPreviewKind(
        string packagePath,
        string sourceText,
        bool hasImage,
        ToolEditorHtmlViewMode mode)
    {
        var descriptor = GetDescriptor(packagePath, sourceText, hasImage);
        return mode == ToolEditorHtmlViewMode.Edit && !descriptor.TreatEditAsPreview
            ? ToolEditorPreviewKind.None
            : descriptor.PreviewKind;
    }

    private static ToolEditorHtmlViewDescriptor GetDescriptor(string packagePath, string sourceText, bool hasImage)
    {
        var policy = LanguagePackagePolicy.Current;
        if (!policy.IsAllowedPackagePath(packagePath))
            return NoViewDescriptor;

        if (hasImage)
            return policy.IsImagePath(packagePath)
                ? BuildImageDescriptor(policy)
                : NoViewDescriptor;

        var extension = Path.GetExtension(packagePath);
        foreach (var descriptor in ViewDescriptors)
        {
            if (!IsDescriptorAllowedByPolicy(descriptor, extension, policy))
                continue;

            if (descriptor.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                return descriptor;

            if (IsPolicyTextPath(packagePath, policy) && descriptor.ContentDetector?.Invoke(sourceText) == true)
                return descriptor;
        }

        return NoViewDescriptor;
    }

    private static ToolEditorHtmlViewDescriptor BuildImageDescriptor(LanguagePackagePolicy policy)
    {
        return new ToolEditorHtmlViewDescriptor(
            ToolEditorPreviewKind.Image,
            CanUseVisualEditor: false,
            CanUseHtmlToolbar: false,
            TreatEditAsPreview: false,
            policy.ImageExtensions.Order(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static bool IsDescriptorAllowedByPolicy(
        ToolEditorHtmlViewDescriptor descriptor,
        string extension,
        LanguagePackagePolicy policy)
    {
        if (descriptor.PreviewKind == ToolEditorPreviewKind.Image)
            return policy.ImageExtensions.Contains(extension);

        return descriptor.Extensions.Any(item =>
            item.Equals(extension, StringComparison.OrdinalIgnoreCase) &&
            policy.AllowedExtensions.Contains(item) &&
            !policy.BlockedExtensions.Contains(item));
    }

    private static bool IsPolicyTextPath(string packagePath, LanguagePackagePolicy policy)
    {
        var extension = Path.GetExtension(packagePath);
        return policy.TextExtensions.Contains(extension) ||
            extension.Equals(".lng", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeHtml(string sourceText)
    {
        return sourceText.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
            sourceText.Contains("<body", StringComparison.OrdinalIgnoreCase) ||
            sourceText.Contains("<img", StringComparison.OrdinalIgnoreCase) ||
            sourceText.Contains("<h1", StringComparison.OrdinalIgnoreCase) ||
            sourceText.Contains("<p", StringComparison.OrdinalIgnoreCase);
    }
}
