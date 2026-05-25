#nullable enable
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Tiedragon.Help;

// Zoek/commentaar: Type-overzicht: record NodHelpPage bevat één onderwerp in de NOD help.
public sealed record NodHelpPage(string Id, string Title, string Html);

public enum HelpNavigationIcon
{
    None,
    Previous,
    Home,
    Next,
    Close
}

// Zoek/commentaar: Type-overzicht: class NodHelpForm toont de uitgebreide HTML-help voor NOD.
public sealed class NodHelpForm : Form
{
    private readonly IReadOnlyList<NodHelpPage> _pages;
    private readonly HelpTopicsList _topics;
    private readonly WebView2 _browser;
    private readonly TextBox _searchBox;
    private readonly Button _searchPreviousButton;
    private readonly Button _searchNextButton;
    private readonly Button _searchClearButton;
    private readonly Button _homeButton;
    private readonly Button _previousButton;
    private readonly Button _nextButton;
    private readonly CoreWebView2PreferredColorScheme? _preferredColorScheme;
    private string? _pendingHtml;
    private bool _browserFailed;
    private int _currentSearchIndex = -1;
    private bool _suppressSearchReset;
    private bool IsDarkTheme => _preferredColorScheme == CoreWebView2PreferredColorScheme.Dark;
    private Color ShellBackColor => IsDarkTheme ? Color.FromArgb(15, 23, 42) : Color.FromArgb(248, 250, 252);
    private Color PanelBackColor => IsDarkTheme ? Color.FromArgb(17, 24, 39) : Color.White;
    private Color NavigationBackColor => IsDarkTheme ? Color.FromArgb(15, 23, 42) : Color.FromArgb(241, 245, 249);
    private Color BorderColor => IsDarkTheme ? Color.FromArgb(51, 65, 85) : Color.FromArgb(203, 213, 225);
    private Color TextColor => IsDarkTheme ? Color.FromArgb(229, 231, 235) : Color.FromArgb(31, 41, 55);
    private Color SearchBackColor => IsDarkTheme ? Color.FromArgb(31, 41, 55) : Color.White;

    // Zoek/commentaar: Constructor: maakt en initialiseert NodHelpForm.
    public NodHelpForm(
        string title,
        IReadOnlyList<NodHelpPage> pages,
        string? selectedPageId = null,
        string homeText = "Home",
        string previousText = "Previous",
        string nextText = "Next",
        bool okOnly = false,
        string okText = "OK",
        bool showTopics = true,
        CoreWebView2PreferredColorScheme? preferredColorScheme = null,
        bool showSignedPackageBadge = false,
        string signedPackageBadgeText = "Signed package verified",
        HelpSignedPackageInformation? signedPackageInformation = null)
    {
        _pages = pages;
        _preferredColorScheme = preferredColorScheme;
        var topicPaneWidth = showTopics ? CalculateTopicPaneWidth(pages) : 0;

        Text = title;
        Width = showTopics ? Math.Max(1040, topicPaneWidth + 720) : 840;
        Height = 720;
        MinimumSize = showTopics ? new Size(900, 560) : new Size(720, 520);
        StartPosition = FormStartPosition.CenterParent;
        if (okOnly)
            ControlBox = false;
        KeyPreview = true;
        KeyDown += NodHelpForm_KeyDown;

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterWidth = 1,
            BorderStyle = BorderStyle.None,
            BackColor = BorderColor
        };
        split.SizeChanged += (_, _) => ApplyTopicPaneWidth(split, topicPaneWidth);
        Shown += (_, _) => ApplyTopicPaneWidth(split, topicPaneWidth);
        split.Panel1Collapsed = !showTopics;

        _topics = new HelpTopicsList
        {
            Dock = DockStyle.Fill,
            BackColor = PanelBackColor,
            ForeColor = TextColor,
            DarkMode = IsDarkTheme
        };
        foreach (var page in _pages)
            _topics.Items.Add(page);
        _topics.CollapseAllGroups();
        _topics.SelectedIndexChanged += (_, _) =>
        {
            if (!_suppressSearchReset)
                _currentSearchIndex = -1;

            ShowSelectedPage();
            UpdateNavigationState();
        };

        var topicsHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ShellBackColor,
            Padding = new Padding(6, 0, 5, 6)
        };
        var topicsBorder = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BorderColor,
            Padding = new Padding(1)
        };
        topicsBorder.Controls.Add(_topics);
        topicsHost.Controls.Add(topicsBorder);

        var navigationHost = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = NavigationBackColor,
            Padding = new Padding(0)
        };
        navigationHost.Controls.Add(new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = BorderColor
        });

        var navigation = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 10, 16, 10),
            ColumnCount = 3,
            RowCount = 1,
            BackColor = NavigationBackColor
        };
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        navigation.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _previousButton = CreateNavigationButton(previousText, HelpNavigationIcon.Previous, AnchorStyles.Left);
        _homeButton = CreateNavigationButton(homeText, HelpNavigationIcon.Home, AnchorStyles.None);
        _nextButton = CreateNavigationButton(nextText, HelpNavigationIcon.Next, AnchorStyles.Right);
        _homeButton.Click += (_, _) => SelectFirstPage();
        _previousButton.Click += (_, _) => SelectRelativePage(-1);
        _nextButton.Click += (_, _) => SelectRelativePage(1);
        if (okOnly)
        {
            var okButton = CreateNavigationButton(okText, HelpNavigationIcon.None, AnchorStyles.None);
            okButton.DialogResult = DialogResult.OK;
            okButton.Click += (_, _) => Close();
            AcceptButton = okButton;
            CancelButton = okButton;
            navigation.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = NavigationBackColor }, 0, 0);
            navigation.Controls.Add(okButton, 1, 0);
            navigation.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = NavigationBackColor }, 2, 0);
        }
        else
        {
            navigation.Controls.Add(_previousButton, 0, 0);
            navigation.Controls.Add(_homeButton, 1, 0);
            navigation.Controls.Add(_nextButton, 2, 0);
        }
        navigationHost.Controls.Add(navigation);

        _browser = new WebView2
        {
            Dock = DockStyle.Fill,
            AllowExternalDrop = false,
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = GetWebView2UserDataFolder()
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
            if (_preferredColorScheme.HasValue)
                _browser.CoreWebView2.Profile.PreferredColorScheme = _preferredColorScheme.Value;
            _browser.CoreWebView2.WebMessageReceived += Browser_WebMessageReceived;
            ShowPendingHtmlIfReady();
        };
        _ = InitializeBrowserAsync();

        var searchHost = new Panel
        {
            Dock = DockStyle.Top,
            Height = 38,
            BackColor = ShellBackColor,
            Padding = new Padding(10, 6, 10, 6)
        };
        var searchLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 1,
            BackColor = ShellBackColor
        };
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, showSignedPackageBadge ? 32 : 0));
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
        searchLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        searchLayout.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Zoek",
            Font = new Font("Segoe UI", 9),
            ForeColor = TextColor,
            Margin = new Padding(0, 4, 8, 0)
        }, 0, 0);

        _searchBox = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9),
            BackColor = SearchBackColor,
            ForeColor = TextColor,
            Margin = new Padding(0, 0, 8, 0)
        };
        _searchBox.TextChanged += (_, _) => SearchCurrentPage(resetIndex: true);
        _searchBox.KeyDown += SearchBox_KeyDown;
        searchLayout.Controls.Add(_searchBox, 1, 0);

        if (showSignedPackageBadge)
        {
            var signedBadge = new HelpSignedPackageBadge
            {
                Dock = DockStyle.Fill,
                BackColor = ShellBackColor,
                Margin = new Padding(0, 0, 8, 0),
                Verified = signedPackageInformation?.Verified ?? true
            };
            signedBadge.Click += (_, _) =>
            {
                if (signedPackageInformation is not null)
                    HelpSignedPackageInformationDialog.ShowPopover(signedBadge, signedPackageInformation, IsDarkTheme);
            };
            var signedToolTip = new ToolTip();
            signedToolTip.SetToolTip(signedBadge, signedPackageBadgeText);
            searchLayout.Controls.Add(signedBadge, 2, 0);
        }

        _searchPreviousButton = CreateSearchButton("\u25b2", "Vorige zoekresultaat");
        _searchNextButton = CreateSearchButton("\u25bc", "Volgende zoekresultaat");
        _searchClearButton = CreateSearchClearButton("Zoektekst wissen");
        _searchPreviousButton.Click += (_, _) => MoveSearchResult(-1);
        _searchNextButton.Click += (_, _) => MoveSearchResult(1);
        _searchClearButton.Click += (_, _) => _searchBox.Clear();
        searchLayout.Controls.Add(_searchPreviousButton, 3, 0);
        searchLayout.Controls.Add(_searchNextButton, 4, 0);
        searchLayout.Controls.Add(_searchClearButton, 5, 0);
        searchHost.Controls.Add(searchLayout);

        var contentOuter = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ShellBackColor,
            Padding = new Padding(0)
        };
        var contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(1),
            BackColor = BorderColor
        };
        var contentInner = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 7, 11, 0),
            BackColor = PanelBackColor
        };
        contentInner.Controls.Add(_browser);
        contentInner.Controls.Add(searchHost);
        contentHost.Controls.Add(contentInner);
        contentOuter.Controls.Add(contentHost);

        if (showTopics)
            split.Panel1.Controls.Add(topicsHost);
        split.Panel2.Padding = new Padding(0);
        split.Panel2.Controls.Add(contentOuter);
        split.Panel2.Controls.Add(navigationHost);
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
        UpdateNavigationState();
    }

    // Zoek/commentaar: Maakt duidelijke helpnavigatie-knoppen.
    private Button CreateNavigationButton(string text, HelpNavigationIcon icon, AnchorStyles anchor)
    {
        return new HelpNavigationButton(text, icon)
        {
            Width = 148,
            Height = 36,
            Margin = new Padding(0),
            Anchor = anchor,
            DarkMode = IsDarkTheme
        };
    }

    // Zoek/commentaar: Maakt compacte knoppen voor zoeken binnen de helptekst.
    private Button CreateSearchButton(string text, string tooltip)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(2, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = IsDarkTheme ? Color.FromArgb(17, 24, 39) : Color.White,
            ForeColor = IsDarkTheme ? Color.FromArgb(191, 219, 254) : Color.FromArgb(15, 63, 143),
            Font = new Font("Segoe UI", 9),
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = IsDarkTheme ? Color.FromArgb(51, 65, 85) : Color.FromArgb(203, 213, 225);
        button.FlatAppearance.MouseOverBackColor = IsDarkTheme ? Color.FromArgb(30, 41, 59) : Color.FromArgb(239, 246, 255);
        button.FlatAppearance.MouseDownBackColor = IsDarkTheme ? Color.FromArgb(37, 99, 235) : Color.FromArgb(219, 234, 254);
        new ToolTip().SetToolTip(button, tooltip);
        return button;
    }

    private Button CreateSearchClearButton(string tooltip)
    {
        var button = new HelpNavigationButton(string.Empty, HelpNavigationIcon.Close)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(2, 0, 0, 0),
            DarkMode = IsDarkTheme
        };
        new ToolTip().SetToolTip(button, tooltip);
        return button;
    }

    private static string GetWebView2UserDataFolder()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var path = Path.Combine(root, "Syscalculator", "WebView2");
        Directory.CreateDirectory(path);
        return path;
    }

    // Zoek/commentaar: Sneltoetsen voor eenvoudige helpnavigatie.
    private void NodHelpForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.F)
        {
            _searchBox.Focus();
            _searchBox.SelectAll();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.Home && !e.Alt && !e.Control && !e.Shift)
        {
            SelectFirstPage();
            e.Handled = true;
            return;
        }

        if (e.Alt && e.KeyCode == Keys.Left)
        {
            SelectRelativePage(-1);
            e.Handled = true;
            return;
        }

        if (e.Alt && e.KeyCode == Keys.Right)
        {
            SelectRelativePage(1);
            e.Handled = true;
        }
    }

    // Zoek/commentaar: Enter en Shift+Enter bladeren door zoekresultaten.
    private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
            return;

        MoveSearchResult(e.Shift ? -1 : 1);
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    // Zoek/commentaar: Selecteert de eerste help-pagina.
    private void SelectFirstPage()
    {
        _topics.SelectFirstVisible();
    }

    // Zoek/commentaar: Gaat naar de vorige of volgende help-pagina.
    private void SelectRelativePage(int offset)
    {
        _topics.SelectRelativeVisible(offset);
    }

    // Zoek/commentaar: Zet navigatieknoppen aan/uit op begin en einde.
    private void UpdateNavigationState()
    {
        var index = _topics.SelectedIndex;
        var hasPages = _topics.VisibleItemCount > 0;
        _homeButton.Enabled = hasPages && _topics.HasPreviousVisible;
        _previousButton.Enabled = hasPages && _topics.HasPreviousVisible;
        _nextButton.Enabled = hasPages && index >= 0 && _topics.HasNextVisible;
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

        var slashIndex = pageId.LastIndexOf('/');
        if (slashIndex >= 0 && slashIndex < pageId.Length - 1)
            SelectPage(pageId[(slashIndex + 1)..]);
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
            SearchCurrentPage(resetIndex: true);
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

    private static int CalculateTopicPaneWidth(IReadOnlyList<NodHelpPage> pages)
    {
        using var font = new Font("Segoe UI", 9);
        var widest = pages.Count == 0
            ? 0
            : pages.Max(page => TextRenderer.MeasureText(
                page.Title.TrimStart(),
                font,
                Size.Empty,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width);

        return Math.Clamp(widest + 46, 260, 330);
    }

    private static void ApplyTopicPaneWidth(SplitContainer split, int preferredPanel1Width)
    {
        if (split.Width <= split.Panel1MinSize + split.Panel2MinSize)
            return;

        var validMaxDistance = split.Width - split.Panel2MinSize;
        var maxDistance = Math.Min(validMaxDistance, Math.Max(split.Panel1MinSize, split.Width - 480));
        var distance = Math.Clamp(preferredPanel1Width, split.Panel1MinSize, maxDistance);
        if (split.SplitterDistance != distance)
            split.SplitterDistance = distance;
    }

    // Zoek/commentaar: Markeert zoekresultaten in de huidige HTML-pagina.
    private async void SearchCurrentPage(bool resetIndex)
    {
        if (_browserFailed || IsDisposed || _browser.IsDisposed || _browser.CoreWebView2 is null)
            return;

        if (resetIndex && _currentSearchIndex != int.MaxValue)
            _currentSearchIndex = -1;

        var query = _searchBox.Text.Trim();
        try
        {
            await EnsureSearchScriptAsync();
            var script = "window.syscalHelpSearch && window.syscalHelpSearch(" +
                WebText.JavaScriptString(query) + ", " + _currentSearchIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + ");";
            var result = await _browser.CoreWebView2.ExecuteScriptAsync(script);
            if (int.TryParse(result, out var index))
                _currentSearchIndex = index;

            if (_currentSearchIndex == int.MaxValue)
                MoveSearchResult(-1);
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

    // Zoek/commentaar: Gaat naar vorige/volgende gemarkeerde zoekmatch.
    private async void MoveSearchResult(int offset)
    {
        if (_browserFailed || IsDisposed || _browser.IsDisposed || _browser.CoreWebView2 is null)
            return;

        var query = _searchBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
            return;

        try
        {
            await EnsureSearchScriptAsync();
            var script = "window.syscalHelpMoveSearch && window.syscalHelpMoveSearch(" +
                WebText.JavaScriptString(query) + ", " + offset.ToString(System.Globalization.CultureInfo.InvariantCulture) + ");";
            var result = await _browser.CoreWebView2.ExecuteScriptAsync(script);
            if (int.TryParse(result, out var index) && index >= 0)
            {
                _currentSearchIndex = index;
                return;
            }

            SelectTopicWithSearchMatch(query, offset);
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

    // Zoek/commentaar: Springt naar een ander onderwerp wanneer de huidige pagina geen zoekmatch heeft.
    private void SelectTopicWithSearchMatch(string query, int offset)
    {
        if (_topics.Items.Count == 0)
            return;

        var startIndex = _topics.SelectedIndex < 0 ? 0 : _topics.SelectedIndex;
        var direction = offset < 0 ? -1 : 1;
        for (var step = 1; step <= _topics.Items.Count; step++)
        {
            var index = (startIndex + direction * step) % _topics.Items.Count;
            if (index < 0)
                index += _topics.Items.Count;

            if (_topics.Items[index] is not NodHelpPage page || !PageContains(page, query))
                continue;

            _currentSearchIndex = direction < 0 ? int.MaxValue : 0;
            _suppressSearchReset = true;
            try
            {
                _topics.SelectedIndex = index;
            }
            finally
            {
                _suppressSearchReset = false;
            }

            return;
        }
    }

    private static bool PageContains(NodHelpPage page, string query)
    {
        return page.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
            page.Html.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    // Zoek/commentaar: Injecteert compacte zoeklogica in de WebView-pagina.
    private async Task EnsureSearchScriptAsync()
    {
        if (_browser.CoreWebView2 is null)
            return;

        await _browser.CoreWebView2.ExecuteScriptAsync("""
(() => {
  if (window.syscalHelpSearchInstalled) return;
  window.syscalHelpSearchInstalled = true;
  window.syscalHelpSearchMarks = [];
  window.syscalHelpSearchIndex = -1;
  const style = document.createElement('style');
  style.textContent = 'mark.syscal-search{background:#fde68a;color:#111827;border-radius:3px;padding:0 2px}mark.syscal-search-current{background:#f59e0b;color:#111827}';
  document.head.appendChild(style);
  function clearMarks() {
    for (const mark of Array.from(document.querySelectorAll('mark.syscal-search'))) {
      const text = document.createTextNode(mark.textContent || '');
      mark.replaceWith(text);
      text.parentNode && text.parentNode.normalize();
    }
    window.syscalHelpSearchMarks = [];
    window.syscalHelpSearchIndex = -1;
  }
  function walk(node, query) {
    if (!node || !query) return;
    if (node.nodeType === Node.TEXT_NODE) {
      const text = node.nodeValue || '';
      const index = text.toLocaleLowerCase().indexOf(query);
      if (index < 0) return;
      const after = node.splitText(index);
      const tail = after.splitText(query.length);
      const mark = document.createElement('mark');
      mark.className = 'syscal-search';
      mark.textContent = after.nodeValue;
      after.replaceWith(mark);
      window.syscalHelpSearchMarks.push(mark);
      walk(tail, query);
      return;
    }
    if (node.nodeType !== Node.ELEMENT_NODE) return;
    const tag = node.tagName;
    if (tag === 'SCRIPT' || tag === 'STYLE' || tag === 'MARK') return;
    for (const child of Array.from(node.childNodes)) walk(child, query);
  }
  function select(index) {
    const marks = window.syscalHelpSearchMarks;
    for (const mark of marks) mark.classList.remove('syscal-search-current');
    if (!marks.length) return -1;
    const bounded = ((index % marks.length) + marks.length) % marks.length;
    const current = marks[bounded];
    current.classList.add('syscal-search-current');
    current.scrollIntoView({ block: 'center', inline: 'nearest' });
    window.syscalHelpSearchIndex = bounded;
    return bounded;
  }
  window.syscalHelpSearch = (query, preferredIndex) => {
    clearMarks();
    const q = (query || '').trim().toLocaleLowerCase();
    if (!q) return -1;
    walk(document.body, q);
    return select(preferredIndex >= 0 ? preferredIndex : 0);
  };
  window.syscalHelpMoveSearch = (query, offset) => {
    const q = (query || '').trim();
    if (!q) return -1;
    if (!window.syscalHelpSearchMarks.length) window.syscalHelpSearch(q, 0);
    return select(window.syscalHelpSearchIndex + offset);
  };
})();
""");
    }

}

public sealed class HelpSignedPackageBadge : Control
{
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Verified { get; set; } = true;

    public HelpSignedPackageBadge()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        TabStop = false;
        Cursor = Cursors.Hand;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var color = Verified ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68);
        HelpSignedLockSvg.Draw(e.Graphics, new Rectangle(2, 1, Width - 4, Height - 2), color);
    }
}

internal sealed class HelpSignedPackageInformationDialog : Form
{
    private readonly bool _dark;
    private readonly Color _windowBack;
    private readonly Color _panelBack;
    private readonly Color _text;
    private readonly Color _muted;
    private readonly Color _border;
    private readonly Color _success = Color.FromArgb(34, 197, 94);
    private readonly Color _danger = Color.FromArgb(239, 68, 68);

    private HelpSignedPackageInformationDialog(HelpSignedPackageInformation information, bool dark)
    {
        _dark = dark;
        _windowBack = dark ? Color.FromArgb(18, 24, 32) : Color.FromArgb(246, 248, 252);
        _panelBack = dark ? Color.FromArgb(31, 41, 55) : Color.White;
        _text = dark ? Color.FromArgb(226, 232, 240) : Color.FromArgb(20, 30, 46);
        _muted = dark ? Color.FromArgb(148, 163, 184) : Color.DimGray;
        _border = dark ? Color.FromArgb(71, 85, 105) : Color.FromArgb(226, 232, 240);

        Text = information.Title;
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(440, 500);
        BackColor = _windowBack;
        ForeColor = _text;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        Deactivate += (_, _) => Close();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14),
            BackColor = _windowBack
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = _windowBack };
        var badge = new HelpSignedPackageBadge { BackColor = _windowBack, Location = new Point(0, 6), Size = new Size(44, 44), Verified = information.Verified };
        var name = new Label
        {
            AutoSize = false,
            Text = information.Name,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = _text,
            BackColor = _windowBack,
            Location = new Point(58, 8),
            Size = new Size(260, 24)
        };
        var status = new Label
        {
            AutoSize = false,
            Text = information.Status,
            ForeColor = information.Verified ? _success : _danger,
            BackColor = _windowBack,
            Location = new Point(59, 32),
            Size = new Size(260, 22)
        };
        header.Controls.Add(badge);
        header.Controls.Add(name);
        header.Controls.Add(status);
        header.Controls.Add(new Label
        {
            AutoSize = false,
            Text = information.Tip,
            ForeColor = _text,
            BackColor = _panelBack,
            Location = new Point(0, 62),
            Size = new Size(410, 34),
            Padding = new Padding(10, 7, 10, 0)
        });
        root.Controls.Add(header, 0, 0);

        var details = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = Math.Min(information.Rows.Count, 7),
            BackColor = _panelBack,
            Padding = new Padding(10),
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var visibleRows = information.Rows.ToList();
        for (var rowIndex = 0; rowIndex < visibleRows.Count; rowIndex++)
        {
            var row = visibleRows[rowIndex];
            details.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
            details.Controls.Add(new Label { Text = row.Label, Dock = DockStyle.Fill, ForeColor = _muted, BackColor = _panelBack, TextAlign = ContentAlignment.MiddleLeft }, 0, rowIndex);
            details.Controls.Add(new Label { Text = row.Value, Dock = DockStyle.Fill, ForeColor = _text, BackColor = _panelBack, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true }, 1, rowIndex);
        }

        var borderPanel = new Panel { Dock = DockStyle.Fill, BackColor = _border, Padding = new Padding(1) };
        borderPanel.Controls.Add(details);
        root.Controls.Add(borderPanel, 0, 1);

        var footer = new Label
        {
            Dock = DockStyle.Fill,
            Text = information.Title,
            ForeColor = dark ? Color.FromArgb(191, 219, 254) : Color.FromArgb(29, 78, 216),
            BackColor = _windowBack,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(4, 8, 0, 0)
        };
        root.Controls.Add(footer, 0, 2);

        Controls.Add(root);
    }

    public static void Show(IWin32Window owner, HelpSignedPackageInformation information, bool dark)
    {
        var dialog = new HelpSignedPackageInformationDialog(information, dark);
        if (owner is Form ownerForm && ownerForm.Icon is not null)
            dialog.Icon = (Icon)ownerForm.Icon.Clone();
        if (owner is Control control)
        {
            var screen = control.PointToScreen(new Point(24, 24));
            dialog.Location = screen;
        }
        else
        {
            dialog.StartPosition = FormStartPosition.CenterParent;
        }
        dialog.Show(owner);
    }

    public static void ShowPopover(Control anchor, HelpSignedPackageInformation information, bool dark)
    {
        var dialog = new HelpSignedPackageInformationDialog(information, dark);
        var owner = anchor.FindForm();
        if (owner?.Icon is not null)
            dialog.Icon = (Icon)owner.Icon.Clone();

        var screenPoint = anchor.PointToScreen(new Point(anchor.Width - 18, anchor.Height + 8));
        var workingArea = Screen.FromControl(anchor).WorkingArea;
        var x = Math.Min(Math.Max(workingArea.Left + 8, screenPoint.X - dialog.Width + 24), workingArea.Right - dialog.Width - 8);
        var y = Math.Min(Math.Max(workingArea.Top + 8, screenPoint.Y), workingArea.Bottom - dialog.Height - 8);
        dialog.Location = new Point(x, y);
        dialog.Show(owner);
    }

    private static void ApplyNativeDarkTitleBar(Form form, bool dark)
    {
        if (!dark || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            return;

        try
        {
            var value = 1;
            _ = DwmSetWindowAttribute(form.Handle, 20, ref value, sizeof(int));
        }
        catch
        {
            // Title bar theming is cosmetic.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
}

internal static class HelpSignedLockSvg
{
    private const float SvgLeft = 5485f;
    private const float SvgTop = 545f;
    private const float SvgWidth = 1059f;
    private const float SvgHeight = 1411f;
    private static readonly Regex SvgPathTokenRegex = new(@"[A-Za-z]|[-+]?(?:\d*\.\d+|\d+)(?:[eE][-+]?\d+)?", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly SvgPathPart[] SourceSvgPaths =
    [
        new(true, "M 5597.109375 1096.890625 L 6431.089844 1096.890625 C 6492.738281 1096.890625 6543.171875 1147.328125 6543.171875 1208.96875 L 6543.171875 1842.980469 C 6543.171875 1904.621094 6492.738281 1955.054688 6431.089844 1955.054688 L 5597.109375 1955.054688 C 5535.46875 1955.054688 5485.039062 1904.621094 5485.039062 1842.980469 L 5485.039062 1208.96875 C 5485.039062 1147.328125 5535.46875 1096.890625 5597.109375 1096.890625"),
        new(true, "M 6014.101562 545.269531 C 6236.058594 545.269531 6417.660156 726.859375 6417.660156 948.828125 L 6417.660156 1120.898438 L 6267.699219 1120.898438 L 6267.699219 948.828125 C 6267.699219 809.339844 6153.589844 695.230469 6014.101562 695.230469 C 5874.609375 695.230469 5760.488281 809.339844 5760.488281 948.828125 L 5760.488281 1120.898438 L 5610.53125 1120.898438 L 5610.53125 948.828125 C 5610.53125 726.859375 5792.128906 545.269531 6014.101562 545.269531"),
        new(false, "M 5791.21875 1488.179688 C 5812.539062 1466.859375 5847.410156 1466.859375 5868.738281 1488.179688 L 5952.078125 1571.523438 L 6159.460938 1364.140625 C 6180.78125 1342.820312 6215.660156 1342.820312 6236.980469 1364.140625 C 6258.300781 1385.460938 6258.300781 1420.339844 6236.980469 1441.660156 L 5990.839844 1687.804688 C 5969.519531 1709.125 5934.640625 1709.125 5913.320312 1687.804688 L 5791.21875 1565.695312 C 5769.898438 1544.375 5769.898438 1509.496094 5791.21875 1488.179688")
    ];
    private static readonly Lazy<IReadOnlyList<SvgPathShape>> SvgShapes = new(() => SourceSvgPaths
        .Select(part => new SvgPathShape(part.UseBadgeColor, BuildSvgPath(part.Data)))
        .ToArray());

    public static void Draw(Graphics graphics, Rectangle bounds, Color badgeColor)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var state = graphics.Save();
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var scale = Math.Min(bounds.Width / SvgWidth, bounds.Height / SvgHeight);
        var left = bounds.Left + (bounds.Width - SvgWidth * scale) / 2f;
        var top = bounds.Top + (bounds.Height - SvgHeight * scale) / 2f;
        graphics.TranslateTransform(left, top);
        graphics.ScaleTransform(scale, scale);
        graphics.TranslateTransform(-SvgLeft, -SvgTop);

        foreach (var shape in SvgShapes.Value)
        {
            using var brush = new SolidBrush(shape.UseBadgeColor ? badgeColor : Color.White);
            graphics.FillPath(brush, shape.Path);
        }

        graphics.Restore(state);
    }

    private static GraphicsPath BuildSvgPath(string data)
    {
        var tokens = SvgPathTokenRegex.Matches(data).Select(match => match.Value).ToArray();
        var path = new GraphicsPath(FillMode.Alternate);
        var index = 0;
        var command = '\0';
        var current = PointF.Empty;
        var figureStart = PointF.Empty;

        while (index < tokens.Length)
        {
            if (IsCommand(tokens[index]))
                command = tokens[index++][0];
            if (command == '\0')
                break;

            var relative = char.IsLower(command);
            switch (char.ToUpperInvariant(command))
            {
                case 'M':
                    current = ReadPoint(tokens, ref index, current, relative);
                    figureStart = current;
                    path.StartFigure();
                    command = relative ? 'l' : 'L';
                    break;
                case 'L':
                    while (CanReadNumbers(tokens, index, 2))
                    {
                        var next = ReadPoint(tokens, ref index, current, relative);
                        path.AddLine(current, next);
                        current = next;
                    }
                    break;
                case 'C':
                    while (CanReadNumbers(tokens, index, 6))
                    {
                        var control1 = ReadPoint(tokens, ref index, current, relative);
                        var control2 = ReadPoint(tokens, ref index, current, relative);
                        var end = ReadPoint(tokens, ref index, current, relative);
                        path.AddBezier(current, control1, control2, end);
                        current = end;
                    }
                    break;
                case 'Z':
                    path.CloseFigure();
                    current = figureStart;
                    command = '\0';
                    break;
                default:
                    index++;
                    break;
            }
        }

        return path;
    }

    private static PointF ReadPoint(string[] tokens, ref int index, PointF current, bool relative)
    {
        var x = float.Parse(tokens[index++], CultureInfo.InvariantCulture);
        var y = float.Parse(tokens[index++], CultureInfo.InvariantCulture);
        return relative ? new PointF(current.X + x, current.Y + y) : new PointF(x, y);
    }

    private static bool CanReadNumbers(string[] tokens, int index, int count)
    {
        if (index + count > tokens.Length)
            return false;

        for (var i = 0; i < count; i++)
        {
            if (IsCommand(tokens[index + i]))
                return false;
        }

        return true;
    }

    private static bool IsCommand(string token)
    {
        return token.Length == 1 && char.IsLetter(token[0]);
    }

    private sealed record SvgPathPart(bool UseBadgeColor, string Data);

    private sealed record SvgPathShape(bool UseBadgeColor, GraphicsPath Path);
}

public sealed class HelpNavigationButton : Button
{
    private readonly HelpNavigationIcon _icon;
    private bool _hover;
    private bool _pressed;

    public HelpNavigationButton(string text, HelpNavigationIcon icon)
    {
        _icon = icon;
        Text = text;
        Font = new Font("Segoe UI Semibold", 9.2f, FontStyle.Regular);
        Cursor = Cursors.Hand;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DarkMode { get; set; }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var g = pevent.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? SystemColors.Control);

        var rect = new RectangleF(0.5f, 0.5f, ClientSize.Width - 1, ClientSize.Height - 1);
        var backgroundColor = !Enabled
            ? (DarkMode ? Color.FromArgb(30, 41, 59) : Color.FromArgb(232, 240, 254))
            : _pressed
                ? (DarkMode ? Color.FromArgb(37, 99, 235) : Color.FromArgb(191, 219, 254))
                : _hover
                    ? (DarkMode ? Color.FromArgb(30, 64, 175) : Color.FromArgb(219, 234, 254))
                    : (DarkMode ? Color.FromArgb(17, 24, 39) : Color.FromArgb(239, 246, 255));
        var borderColor = Enabled
            ? (DarkMode ? Color.FromArgb(96, 165, 250) : Color.FromArgb(59, 130, 246))
            : (DarkMode ? Color.FromArgb(51, 65, 85) : Color.FromArgb(147, 197, 253));
        var iconColor = Enabled
            ? (DarkMode ? Color.FromArgb(147, 197, 253) : Color.FromArgb(29, 78, 216))
            : (DarkMode ? Color.FromArgb(100, 116, 139) : Color.FromArgb(75, 105, 150));
        var textColor = Enabled
            ? (DarkMode ? Color.FromArgb(191, 219, 254) : Color.FromArgb(15, 63, 143))
            : (DarkMode ? Color.FromArgb(100, 116, 139) : Color.FromArgb(75, 105, 150));

        using var background = new SolidBrush(backgroundColor);
        using var borderPen = new Pen(borderColor, 1f);
        using var path = RoundedRect(rect, 7f);
        g.FillPath(background, path);
        g.DrawPath(borderPen, path);

        using var iconPen = new Pen(iconColor, 2.4f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        using var iconFill = new SolidBrush(DarkMode ? Color.FromArgb(17, 24, 39) : Enabled ? Color.FromArgb(239, 246, 255) : Color.FromArgb(241, 245, 249));
        if (_icon != HelpNavigationIcon.None)
        {
            var iconRect = _icon switch
            {
                _ when string.IsNullOrEmpty(Text) => new RectangleF((ClientSize.Width - 20) / 2f, (ClientSize.Height - 20) / 2f, 20, 20),
                HelpNavigationIcon.Next => new RectangleF(ClientSize.Width - 42, 5, 24, 24),
                _ => new RectangleF(18, 5, 24, 24)
            };
            DrawIcon(g, iconPen, iconFill, iconRect);
        }

        var textRect = _icon switch
        {
            HelpNavigationIcon.None => new Rectangle(10, 0, ClientSize.Width - 20, ClientSize.Height),
            HelpNavigationIcon.Previous => new Rectangle(52, 0, ClientSize.Width - 62, ClientSize.Height),
            HelpNavigationIcon.Next => new Rectangle(10, 0, ClientSize.Width - 62, ClientSize.Height),
            _ => new Rectangle(52, 0, ClientSize.Width - 62, ClientSize.Height)
        };
        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
        flags |= _icon switch
        {
            HelpNavigationIcon.None => TextFormatFlags.HorizontalCenter,
            HelpNavigationIcon.Next => TextFormatFlags.Right,
            _ => TextFormatFlags.Left
        };
        TextRenderer.DrawText(g, Text, Font, textRect, textColor, flags);

        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(ClientRectangle, -4, -4));
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        if (mevent.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }

        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }

    private void DrawIcon(Graphics g, Pen pen, Brush fill, RectangleF rect)
    {
        switch (_icon)
        {
            case HelpNavigationIcon.None:
                break;
            case HelpNavigationIcon.Previous:
                DrawArrow(g, pen, rect, -1);
                break;
            case HelpNavigationIcon.Next:
                DrawArrow(g, pen, rect, 1);
                break;
            case HelpNavigationIcon.Home:
                DrawHome(g, pen, fill, rect);
                break;
            case HelpNavigationIcon.Close:
                DrawClose(g, pen, rect);
                break;
        }
    }

    private static void DrawArrow(Graphics g, Pen pen, RectangleF rect, int direction)
    {
        var cy = rect.Top + rect.Height / 2f;
        var cx = rect.Left + rect.Width / 2f;
        var halfWidth = 5.5f;
        var halfHeight = 8f;
        var triangle = direction < 0
            ? new[]
            {
                new PointF(cx - halfWidth, cy),
                new PointF(cx + halfWidth, cy - halfHeight),
                new PointF(cx + halfWidth, cy + halfHeight)
            }
            : new[]
            {
                new PointF(cx + halfWidth, cy),
                new PointF(cx - halfWidth, cy - halfHeight),
                new PointF(cx - halfWidth, cy + halfHeight)
            };

        using var brush = new SolidBrush(pen.Color);
        g.FillPolygon(brush, triangle);
    }

    private static void DrawHome(Graphics g, Pen pen, Brush fill, RectangleF rect)
    {
        var roof = new[]
        {
            new PointF(rect.Left + rect.Width / 2f, rect.Top + 3),
            new PointF(rect.Left + 3, rect.Top + 11),
            new PointF(rect.Left + 3, rect.Bottom - 2),
            new PointF(rect.Right - 3, rect.Bottom - 2),
            new PointF(rect.Right - 3, rect.Top + 11)
        };
        g.FillPolygon(fill, roof);
        g.DrawLine(pen, rect.Left + rect.Width / 2f, rect.Top + 3, rect.Left + 3, rect.Top + 11);
        g.DrawLine(pen, rect.Left + rect.Width / 2f, rect.Top + 3, rect.Right - 3, rect.Top + 11);
        g.DrawLine(pen, rect.Left + 5, rect.Top + 10, rect.Left + 5, rect.Bottom - 2);
        g.DrawLine(pen, rect.Left + 5, rect.Bottom - 2, rect.Right - 5, rect.Bottom - 2);
        g.DrawLine(pen, rect.Right - 5, rect.Bottom - 2, rect.Right - 5, rect.Top + 10);
        g.DrawLine(pen, rect.Left + 10, rect.Bottom - 2, rect.Left + 10, rect.Bottom - 9);
        g.DrawLine(pen, rect.Left + 10, rect.Bottom - 9, rect.Left + 15, rect.Bottom - 9);
        g.DrawLine(pen, rect.Left + 15, rect.Bottom - 9, rect.Left + 15, rect.Bottom - 2);
    }

    private static void DrawClose(Graphics g, Pen pen, RectangleF rect)
    {
        var inset = 6;
        g.DrawLine(pen, rect.Left + inset, rect.Top + inset, rect.Right - inset, rect.Bottom - inset);
        g.DrawLine(pen, rect.Right - inset, rect.Top + inset, rect.Left + inset, rect.Bottom - inset);
    }

    private static GraphicsPath RoundedRect(RectangleF rect, float radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class HelpTopicsList : Control
{
    private const int ItemHeight = 27;
    private const int ScrollbarWidth = 15;
    private readonly List<NodHelpPage> _items = [];
    private readonly HashSet<string> _collapsedGroups = new(StringComparer.OrdinalIgnoreCase);
    private int _selectedIndex = -1;
    private int _firstVisibleIndex;
    private bool _scrollbarHover;
    private bool _scrollbarDragging;
    private int _dragStartY;
    private int _dragStartFirstVisibleIndex;

    public HelpTopicsList()
    {
        Font = new Font("Segoe UI", 9);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
    }

    public List<NodHelpPage> Items => _items;

    public event EventHandler? SelectedIndexChanged;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool DarkMode { get; set; }

    private Color SelectedBackColor => DarkMode ? Color.FromArgb(30, 64, 175) : Color.FromArgb(219, 234, 254);
    private Color GroupBackColor => DarkMode ? Color.FromArgb(15, 23, 42) : Color.FromArgb(248, 250, 252);
    private Color ChildBackColor => DarkMode ? Color.FromArgb(17, 24, 39) : Color.FromArgb(252, 253, 255);
    private Color SelectedTextColor => DarkMode ? Color.FromArgb(239, 246, 255) : Color.FromArgb(15, 63, 143);
    private Color GroupTextColor => DarkMode ? Color.FromArgb(147, 197, 253) : Color.FromArgb(15, 63, 143);
    private Color BorderColor => DarkMode ? Color.FromArgb(51, 65, 85) : Color.FromArgb(203, 213, 225);
    private Color IconBackColor => DarkMode ? Color.FromArgb(15, 23, 42) : Color.White;
    private Color IconBorderColor => DarkMode ? Color.FromArgb(100, 116, 139) : Color.FromArgb(148, 163, 184);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var next = _items.Count == 0 ? -1 : Math.Clamp(value, 0, _items.Count - 1);
            if (_selectedIndex == next)
                return;

            _selectedIndex = next;
            EnsureSelectedGroupExpanded();
            EnsureSelectedVisible();
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public NodHelpPage? SelectedItem =>
        _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;

    public int VisibleItemCount => GetVisibleIndices().Count;

    public bool HasPreviousVisible
    {
        get
        {
            var visible = GetVisibleIndices();
            return visible.IndexOf(_selectedIndex) > 0;
        }
    }

    public bool HasNextVisible
    {
        get
        {
            var visible = GetVisibleIndices();
            var index = visible.IndexOf(_selectedIndex);
            return index >= 0 && index < visible.Count - 1;
        }
    }

    public void SelectFirstVisible()
    {
        var visible = GetVisibleIndices();
        if (visible.Count > 0)
            SelectedIndex = visible[0];
    }

    public void SelectRelativeVisible(int offset)
    {
        var visible = GetVisibleIndices();
        if (visible.Count == 0)
            return;

        var current = visible.IndexOf(_selectedIndex);
        if (current < 0)
            current = 0;

        var next = Math.Clamp(current + offset, 0, visible.Count - 1);
        SelectedIndex = visible[next];
    }

    public void CollapseAllGroups()
    {
        _collapsedGroups.Clear();
        for (var i = 0; i < _items.Count; i++)
        {
            if (IsGroupHeader(i))
                _collapsedGroups.Add(GetGroupTitle(_items[i].Title));
        }

        _firstVisibleIndex = Math.Clamp(_firstVisibleIndex, 0, MaxFirstVisibleIndex);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.Clear(BackColor);

        var listWidth = NeedsScrollbar ? ClientSize.Width - ScrollbarWidth : ClientSize.Width;
        var visibleIndices = GetVisibleIndices();
        var visibleCount = VisibleCount;
        for (var row = 0; row < visibleCount; row++)
        {
            var visibleIndex = _firstVisibleIndex + row;
            if (visibleIndex < 0 || visibleIndex >= visibleIndices.Count)
                break;

            var index = visibleIndices[visibleIndex];
            DrawItem(g, index, new Rectangle(0, row * ItemHeight, listWidth, ItemHeight));
        }

        if (NeedsScrollbar)
            DrawScrollbar(g, new Rectangle(ClientSize.Width - ScrollbarWidth, 0, ScrollbarWidth, ClientSize.Height));
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();
        if (NeedsScrollbar && e.X >= ClientSize.Width - ScrollbarWidth)
        {
            var thumb = GetThumbRectangle();
            if (thumb.Contains(e.Location))
            {
                _scrollbarDragging = true;
                _dragStartY = e.Y;
                _dragStartFirstVisibleIndex = _firstVisibleIndex;
                Capture = true;
                Invalidate();
                return;
            }

            ScrollBy(e.Y < thumb.Top ? -VisibleCount : VisibleCount);
            return;
        }

        var visible = GetVisibleIndices();
        var visibleIndex = _firstVisibleIndex + e.Y / ItemHeight;
        if (visibleIndex >= 0 && visibleIndex < visible.Count)
        {
            var index = visible[visibleIndex];
            var rowRect = new Rectangle(0, (visibleIndex - _firstVisibleIndex) * ItemHeight, ClientSize.Width, ItemHeight);
            if (IsGroupHeader(index)
                && (GetToggleRectangle(rowRect).Contains(e.Location) || e.X <= rowRect.Left + 44))
            {
                ToggleGroup(GetGroupTitle(_items[index].Title));
                return;
            }

            SelectedIndex = index;
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_scrollbarDragging)
        {
            var maxFirst = MaxFirstVisibleIndex;
            var trackHeight = Math.Max(1, ClientSize.Height - GetThumbRectangle().Height);
            var deltaItems = (int)Math.Round((e.Y - _dragStartY) * maxFirst / (double)trackHeight);
            _firstVisibleIndex = Math.Clamp(_dragStartFirstVisibleIndex + deltaItems, 0, maxFirst);
            Invalidate();
            return;
        }

        var hover = NeedsScrollbar && e.X >= ClientSize.Width - ScrollbarWidth;
        if (_scrollbarHover != hover)
        {
            _scrollbarHover = hover;
            Invalidate();
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_scrollbarDragging)
        {
            _scrollbarDragging = false;
            Capture = false;
            Invalidate();
        }

        base.OnMouseUp(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (!_scrollbarDragging && _scrollbarHover)
        {
            _scrollbarHover = false;
            Invalidate();
        }

        base.OnMouseLeave(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        ScrollBy(e.Delta < 0 ? 3 : -3);
        base.OnMouseWheel(e);
    }

    protected override bool IsInputKey(Keys keyData)
    {
        return keyData is Keys.Up or Keys.Down or Keys.PageUp or Keys.PageDown or Keys.Home or Keys.End || base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Up:
                SelectRelativeVisible(-1);
                e.Handled = true;
                break;
            case Keys.Down:
                SelectRelativeVisible(1);
                e.Handled = true;
                break;
            case Keys.PageUp:
                SelectRelativeVisible(-VisibleCount);
                e.Handled = true;
                break;
            case Keys.PageDown:
                SelectRelativeVisible(VisibleCount);
                e.Handled = true;
                break;
            case Keys.Home:
                SelectFirstVisible();
                e.Handled = true;
                break;
            case Keys.End:
                var visible = GetVisibleIndices();
                if (visible.Count > 0)
                    SelectedIndex = visible[^1];
                e.Handled = true;
                break;
            case Keys.Left:
                CollapseSelectedGroup();
                e.Handled = true;
                break;
            case Keys.Right:
                ExpandSelectedGroup();
                e.Handled = true;
                break;
        }

        base.OnKeyDown(e);
    }

    protected override void OnResize(EventArgs e)
    {
        _firstVisibleIndex = Math.Clamp(_firstVisibleIndex, 0, MaxFirstVisibleIndex);
        EnsureSelectedVisible();
        base.OnResize(e);
    }

    private bool NeedsScrollbar => VisibleItemCount > VisibleCount;

    private int VisibleCount => Math.Max(1, ClientSize.Height / ItemHeight);

    private int MaxFirstVisibleIndex => Math.Max(0, VisibleItemCount - VisibleCount);

    private void DrawItem(Graphics g, int index, Rectangle rect)
    {
        var selected = index == _selectedIndex;
        var titleParts = _items[index].Title.Split('/', 2, StringSplitOptions.TrimEntries);
        var isChild = titleParts.Length == 2;
        var isGroup = IsGroupHeader(index);
        var groupTitle = GetGroupTitle(_items[index].Title);
        var collapsed = isGroup && _collapsedGroups.Contains(groupTitle);
        var text = isChild ? titleParts[1] : titleParts[0];
        var backgroundColor = selected
            ? SelectedBackColor
            : isGroup ? GroupBackColor
            : isChild ? ChildBackColor : BackColor;
        var textColor = selected
            ? SelectedTextColor
            : isGroup ? GroupTextColor : ForeColor;

        using (var background = new SolidBrush(backgroundColor))
            g.FillRectangle(background, rect);

        if (isGroup)
        {
            using var separator = new Pen(BorderColor);
            g.DrawLine(separator, rect.Left + 8, rect.Bottom - 1, rect.Right - 8, rect.Bottom - 1);
        }

        if (isChild)
        {
            using var guide = new Pen(BorderColor, 1);
            var x = rect.Left + 24;
            var hasNextSibling = index + 1 < _items.Count
                && IsSameChildGroup(_items[index].Title, _items[index + 1].Title);
            var midY = rect.Top + rect.Height / 2;
            g.DrawLine(guide, x, rect.Top, x, hasNextSibling ? rect.Bottom : midY);
            g.DrawLine(guide, x, midY, x + 12, midY);
        }

        if (selected)
        {
            using var accent = new SolidBrush(DarkMode ? Color.FromArgb(96, 165, 250) : Color.FromArgb(37, 99, 235));
            g.FillRectangle(accent, rect.Left, rect.Top + 3, 3, rect.Height - 6);
        }

        if (isGroup)
            DrawToggle(g, GetToggleRectangle(rect), collapsed);

        if (isGroup)
            DrawFolder(g, new Rectangle(rect.Left + 27, rect.Top + 7, 15, 13), collapsed);
        else if (isChild)
            DrawLeaf(g, new Rectangle(rect.Left + 38, rect.Top + 8, 10, 11));

        using var groupFont = isGroup ? new Font(Font, FontStyle.Bold) : null;
        var drawFont = groupFont ?? Font;
        var textLeft = isChild ? rect.Left + 53 : isGroup ? rect.Left + 48 : rect.Left + 10;
        var textRect = new Rectangle(textLeft, rect.Top, rect.Width - textLeft - 8, rect.Height);
        TextRenderer.DrawText(
            g,
            text,
            drawFont,
            textRect,
            textColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }

    private static bool IsSameChildGroup(string currentTitle, string nextTitle)
    {
        var current = currentTitle.Split('/', 2, StringSplitOptions.TrimEntries);
        var next = nextTitle.Split('/', 2, StringSplitOptions.TrimEntries);
        return current.Length == 2
            && next.Length == 2
            && current[0].Equals(next[0], StringComparison.OrdinalIgnoreCase);
    }

    private List<int> GetVisibleIndices()
    {
        var visible = new List<int>(_items.Count);
        for (var i = 0; i < _items.Count; i++)
        {
            var title = _items[i].Title;
            if (IsChildTitle(title) && _collapsedGroups.Contains(GetGroupTitle(title)))
                continue;

            visible.Add(i);
        }

        return visible;
    }

    private bool IsGroupHeader(int index)
    {
        if (index < 0 || index >= _items.Count - 1)
            return false;

        var group = GetGroupTitle(_items[index].Title);
        return IsChildTitle(_items[index + 1].Title)
            && GetGroupTitle(_items[index + 1].Title).Equals(group, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsChildTitle(string title) => title.Split('/', 2, StringSplitOptions.TrimEntries).Length == 2;

    private static string GetGroupTitle(string title) => title.Split('/', 2, StringSplitOptions.TrimEntries)[0];

    private static Rectangle GetToggleRectangle(Rectangle rect) => new(rect.Left + 8, rect.Top + 6, 14, 14);

    private void ToggleGroup(string groupTitle)
    {
        if (!_collapsedGroups.Add(groupTitle))
            _collapsedGroups.Remove(groupTitle);

        if (_selectedIndex >= 0
            && IsChildTitle(_items[_selectedIndex].Title)
            && GetGroupTitle(_items[_selectedIndex].Title).Equals(groupTitle, StringComparison.OrdinalIgnoreCase)
            && _collapsedGroups.Contains(groupTitle))
        {
            var groupIndex = _items.FindIndex(page => GetGroupTitle(page.Title).Equals(groupTitle, StringComparison.OrdinalIgnoreCase) && !IsChildTitle(page.Title));
            if (groupIndex >= 0)
                _selectedIndex = groupIndex;
        }

        _firstVisibleIndex = Math.Clamp(_firstVisibleIndex, 0, MaxFirstVisibleIndex);
        EnsureSelectedVisible();
        Invalidate();
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    private void EnsureSelectedGroupExpanded()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
            return;

        var title = _items[_selectedIndex].Title;
        if (IsChildTitle(title))
            _collapsedGroups.Remove(GetGroupTitle(title));
    }

    private void CollapseSelectedGroup()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
            return;

        var group = GetGroupTitle(_items[_selectedIndex].Title);
        if (!_collapsedGroups.Contains(group))
            ToggleGroup(group);
    }

    private void ExpandSelectedGroup()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
            return;

        var group = GetGroupTitle(_items[_selectedIndex].Title);
        if (_collapsedGroups.Contains(group))
            ToggleGroup(group);
    }

    private void DrawToggle(Graphics g, Rectangle rect, bool collapsed)
    {
        using var border = new Pen(DarkMode ? Color.FromArgb(100, 116, 139) : Color.FromArgb(96, 120, 160), 1);
        using var fill = new SolidBrush(IconBackColor);
        g.FillRectangle(fill, rect);
        g.DrawRectangle(border, rect);

        using var pen = new Pen(DarkMode ? Color.FromArgb(203, 213, 225) : Color.FromArgb(71, 85, 105), 1.4f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        var midX = rect.Left + rect.Width / 2;
        var midY = rect.Top + rect.Height / 2;
        g.DrawLine(pen, rect.Left + 3, midY, rect.Right - 3, midY);
        if (collapsed)
            g.DrawLine(pen, midX, rect.Top + 3, midX, rect.Bottom - 3);
    }

    private void DrawFolder(Graphics g, Rectangle rect, bool collapsed)
    {
        using var fill = new SolidBrush(DarkMode
            ? collapsed ? Color.FromArgb(30, 41, 59) : Color.FromArgb(30, 64, 175)
            : collapsed ? Color.FromArgb(239, 246, 255) : Color.FromArgb(219, 234, 254));
        using var border = new Pen(DarkMode ? Color.FromArgb(96, 165, 250) : Color.FromArgb(37, 99, 235), 1);
        using var path = new GraphicsPath();
        path.AddLine(rect.Left, rect.Top + 4, rect.Left + 5, rect.Top + 4);
        path.AddLine(rect.Left + 7, rect.Top + 1, rect.Left + 12, rect.Top + 1);
        path.AddLine(rect.Right, rect.Top + 5, rect.Right, rect.Bottom);
        path.AddLine(rect.Left, rect.Bottom, rect.Left, rect.Top + 4);
        path.CloseFigure();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.FillPath(fill, path);
        g.DrawPath(border, path);
        g.SmoothingMode = SmoothingMode.None;
    }

    private void DrawLeaf(Graphics g, Rectangle rect)
    {
        using var fill = new SolidBrush(IconBackColor);
        using var border = new Pen(IconBorderColor, 1);
        g.FillRectangle(fill, rect);
        g.DrawRectangle(border, rect);
        using var line = new Pen(IconBorderColor, 1);
        g.DrawLine(line, rect.Left + 3, rect.Top + 4, rect.Right - 2, rect.Top + 4);
        g.DrawLine(line, rect.Left + 3, rect.Top + 7, rect.Right - 2, rect.Top + 7);
    }

    private void DrawScrollbar(Graphics g, Rectangle rect)
    {
        using var track = new SolidBrush(DarkMode ? Color.FromArgb(15, 23, 42) : Color.FromArgb(248, 250, 252));
        using var trackBorder = new Pen(DarkMode ? Color.FromArgb(51, 65, 85) : Color.FromArgb(215, 221, 231));
        g.FillRectangle(track, rect);
        g.DrawLine(trackBorder, rect.Left, rect.Top, rect.Left, rect.Bottom);

        var thumb = GetThumbRectangle();
        using var thumbBrush = new SolidBrush(_scrollbarHover || _scrollbarDragging
            ? DarkMode ? Color.FromArgb(100, 116, 139) : Color.FromArgb(107, 114, 128)
            : DarkMode ? Color.FromArgb(71, 85, 105) : Color.FromArgb(139, 148, 158));
        using var path = RoundedRect(thumb, thumb.Width / 2f);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.FillPath(thumbBrush, path);
        g.SmoothingMode = SmoothingMode.None;
    }

    private Rectangle GetThumbRectangle()
    {
        var visible = VisibleCount;
        var visibleItems = Math.Max(1, VisibleItemCount);
        var height = Math.Max(44, (int)Math.Round(ClientSize.Height * visible / (double)visibleItems));
        height = Math.Min(ClientSize.Height, height);
        var top = MaxFirstVisibleIndex == 0
            ? 0
            : (int)Math.Round((ClientSize.Height - height) * _firstVisibleIndex / (double)MaxFirstVisibleIndex);
        return new Rectangle(ClientSize.Width - ScrollbarWidth + 3, top + 3, ScrollbarWidth - 6, Math.Max(8, height - 6));
    }

    private void EnsureSelectedVisible()
    {
        if (_selectedIndex < 0)
            return;

        var visibleIndex = GetVisibleIndices().IndexOf(_selectedIndex);
        if (visibleIndex < 0)
            return;

        if (visibleIndex < _firstVisibleIndex)
            _firstVisibleIndex = visibleIndex;
        else if (visibleIndex >= _firstVisibleIndex + VisibleCount)
            _firstVisibleIndex = visibleIndex - VisibleCount + 1;

        _firstVisibleIndex = Math.Clamp(_firstVisibleIndex, 0, MaxFirstVisibleIndex);
    }

    private void ScrollBy(int delta)
    {
        _firstVisibleIndex = Math.Clamp(_firstVisibleIndex + delta, 0, MaxFirstVisibleIndex);
        Invalidate();
    }

    private static GraphicsPath RoundedRect(Rectangle rect, float radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2f;
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
