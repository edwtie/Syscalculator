#nullable enable

namespace Syscalculator.UI.WinForms;

// Zoek/commentaar: Bundelt labels voor de helpnavigatie.
public sealed record HelpNavigationLabels(string Home, string Previous, string Next);

// Zoek/commentaar: Beschrijft een compleet helpvenster.
public sealed record HelpDialogOptions(
    string Title,
    IReadOnlyList<NodHelpPage> Pages,
    string? SelectedPageId = null,
    HelpNavigationLabels? Navigation = null);

// Zoek/commentaar: Centrale API om Syscalculator-helpvensters te tonen.
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
            navigation.Next);

        if (owner is Form ownerForm)
            form.TopMost = ownerForm.TopMost;

        return form.ShowDialog(owner);
    }
}
