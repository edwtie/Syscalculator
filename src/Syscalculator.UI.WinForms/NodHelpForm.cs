#nullable enable
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Syscalculator.UI.WinForms;

// Zoek/commentaar: Type-overzicht: record NodHelpPage bevat één onderwerp in de NOD help.
public sealed record NodHelpPage(string Id, string Title, string Html);

// Zoek/commentaar: Type-overzicht: class NodHelpForm toont de uitgebreide HTML-help voor NOD.
public sealed class NodHelpForm : Form
{
    private readonly IReadOnlyList<NodHelpPage> _pages;
    private readonly ListBox _topics;
    private readonly WebView2 _browser;
    private string? _pendingHtml;
    private bool _browserFailed;

    // Zoek/commentaar: Constructor: maakt en initialiseert NodHelpForm.
    public NodHelpForm(string title, IReadOnlyList<NodHelpPage> pages, string? selectedPageId = null)
    {
        _pages = pages;

        Text = title;
        Width = 860;
        Height = 640;
        MinimumSize = new Size(640, 440);
        StartPosition = FormStartPosition.CenterParent;

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterDistance = 220
        };

        _topics = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            Font = new Font("Segoe UI", 9)
        };
        _topics.DisplayMember = nameof(NodHelpPage.Title);
        foreach (var page in _pages)
            _topics.Items.Add(page);
        _topics.SelectedIndexChanged += (_, _) => ShowSelectedPage();

        _browser = new WebView2
        {
            Dock = DockStyle.Fill,
            AllowExternalDrop = false,
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = WebView2UserDataFolder.GetPath()
            }
        };
        _browser.NavigationStarting += Browser_NavigationStarting;
        _browser.CoreWebView2InitializationCompleted += (_, e) =>
        {
            if (!e.IsSuccess || _browser.CoreWebView2 is null)
                return;

            _browser.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            _browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            _browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _browser.CoreWebView2.WebMessageReceived += Browser_WebMessageReceived;
            ShowPendingHtmlIfReady();
        };
        _ = InitializeBrowserAsync();

        split.Panel1.Controls.Add(_topics);
        split.Panel2.Controls.Add(_browser);
        Controls.Add(split);

        var selectedIndex = 0;
        if (!string.IsNullOrWhiteSpace(selectedPageId))
        {
            for (var i = 0; i < _pages.Count; i++)
            {
                if (_pages[i].Id.Equals(selectedPageId, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    break;
                }
            }
        }

        if (_topics.Items.Count > 0)
            _topics.SelectedIndex = selectedIndex;
    }

    // Zoek/commentaar: Verwerkt acties uit de HTML-help, zoals codevoorbeelden kopieren.
    private void Browser_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var text = e.TryGetWebMessageAsString();
            if (!string.IsNullOrWhiteSpace(text))
                Clipboard.SetText(text);
        }
        catch
        {
            // Kopieren is een hulpfunctie; de help zelf moet bruikbaar blijven.
        }
    }

    // Zoek/commentaar: Verwerkt interne help-links zoals nodpage:cmd:symb1.
    private void Browser_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
            return;

        if (!uri.Scheme.Equals("nodpage", StringComparison.OrdinalIgnoreCase))
            return;

        e.Cancel = true;
        var pageId = Uri.UnescapeDataString((uri.Host + uri.AbsolutePath).Trim('/'));
        SelectPage(pageId);
    }

    // Zoek/commentaar: Selecteert een help-onderwerp vanuit een link in de HTML.
    private void SelectPage(string pageId)
    {
        for (var i = 0; i < _topics.Items.Count; i++)
        {
            if (_topics.Items[i] is NodHelpPage page && page.Id.Equals(pageId, StringComparison.OrdinalIgnoreCase))
            {
                _topics.SelectedIndex = i;
                return;
            }
        }
    }

    // Zoek/commentaar: Toont de geselecteerde help-pagina in de browser.
    private void ShowSelectedPage()
    {
        if (_topics.SelectedItem is not NodHelpPage page)
            return;

        SetHtml(page.Html);
    }

    // Zoek/commentaar: Start de moderne Edge/WebView2 engine voor de NOD help.
    private async Task InitializeBrowserAsync()
    {
        try
        {
            await _browser.EnsureCoreWebView2Async();
            ShowPendingHtmlIfReady();
        }
        catch (COMException)
        {
            _browserFailed = true;
        }
        catch (ObjectDisposedException)
        {
            _browserFailed = true;
        }
        catch (Exception ex)
        {
            _browserFailed = true;
            _browser.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "WebView2 kon niet worden gestart.\r\n" + ex.Message
            });
        }
    }

    // Zoek/commentaar: Zet HTML klaar of toont die direct zodra WebView2 beschikbaar is.
    private void SetHtml(string html)
    {
        _pendingHtml = html;
        ShowPendingHtmlIfReady();
    }

    // Zoek/commentaar: Toont uitgestelde HTML in WebView2.
    private void ShowPendingHtmlIfReady()
    {
        if (_browserFailed || IsDisposed || _browser.IsDisposed || _pendingHtml is null || _browser.CoreWebView2 is null)
            return;

        var html = _pendingHtml;
        _pendingHtml = null;
        try
        {
            _browser.NavigateToString(html);
        }
        catch (COMException)
        {
            _browserFailed = true;
        }
        catch (ObjectDisposedException)
        {
            _browserFailed = true;
        }
    }
}
