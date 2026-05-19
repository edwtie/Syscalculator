#nullable enable
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Tiedragon.Help;

// Zoek/commentaar: Type-overzicht: record NodHelpPage bevat één onderwerp in de NOD help.
public sealed record NodHelpPage(string Id, string Title, string Html);

public enum HelpNavigationIcon
{
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
    private string? _pendingHtml;
    private bool _browserFailed;
    private int _currentSearchIndex = -1;
    private bool _suppressSearchReset;

    // Zoek/commentaar: Constructor: maakt en initialiseert NodHelpForm.
    public NodHelpForm(
        string title,
        IReadOnlyList<NodHelpPage> pages,
        string? selectedPageId = null,
        string homeText = "Home",
        string previousText = "Previous",
        string nextText = "Next")
    {
        _pages = pages;
        var topicPaneWidth = CalculateTopicPaneWidth(pages);

        Text = title;
        Width = Math.Max(1040, topicPaneWidth + 720);
        Height = 720;
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;
        KeyDown += NodHelpForm_KeyDown;

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterWidth = 1,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(226, 232, 240)
        };
        split.SizeChanged += (_, _) => ApplyTopicPaneWidth(split, topicPaneWidth);
        Shown += (_, _) => ApplyTopicPaneWidth(split, topicPaneWidth);

        _topics = new HelpTopicsList
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(31, 41, 55)
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
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(6, 0, 5, 6)
        };
        var topicsBorder = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(203, 213, 225),
            Padding = new Padding(1)
        };
        topicsBorder.Controls.Add(_topics);
        topicsHost.Controls.Add(topicsBorder);

        var navigationHost = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.FromArgb(241, 245, 249),
            Padding = new Padding(0)
        };
        navigationHost.Controls.Add(new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = Color.FromArgb(203, 213, 225)
        });

        var navigation = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 10, 16, 10),
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.FromArgb(241, 245, 249)
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
        navigation.Controls.Add(_previousButton, 0, 0);
        navigation.Controls.Add(_homeButton, 1, 0);
        navigation.Controls.Add(_nextButton, 2, 0);
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
            _browser.CoreWebView2.WebMessageReceived += Browser_WebMessageReceived;
            ShowPendingHtmlIfReady();
        };
        _ = InitializeBrowserAsync();

        var searchHost = new Panel
        {
            Dock = DockStyle.Top,
            Height = 38,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(10, 6, 10, 6)
        };
        var searchLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = Color.FromArgb(248, 250, 252)
        };
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
        searchLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        searchLayout.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Zoek",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(31, 41, 55),
            Margin = new Padding(0, 4, 8, 0)
        }, 0, 0);

        _searchBox = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9),
            Margin = new Padding(0, 0, 8, 0)
        };
        _searchBox.TextChanged += (_, _) => SearchCurrentPage(resetIndex: true);
        _searchBox.KeyDown += SearchBox_KeyDown;
        searchLayout.Controls.Add(_searchBox, 1, 0);

        _searchPreviousButton = CreateSearchButton("\u25b2", "Vorige zoekresultaat");
        _searchNextButton = CreateSearchButton("\u25bc", "Volgende zoekresultaat");
        _searchClearButton = CreateSearchButton("\u00d7", "Zoektekst wissen");
        _searchPreviousButton.Click += (_, _) => MoveSearchResult(-1);
        _searchNextButton.Click += (_, _) => MoveSearchResult(1);
        _searchClearButton.Click += (_, _) => _searchBox.Clear();
        searchLayout.Controls.Add(_searchPreviousButton, 2, 0);
        searchLayout.Controls.Add(_searchNextButton, 3, 0);
        searchLayout.Controls.Add(_searchClearButton, 4, 0);
        searchHost.Controls.Add(searchLayout);

        var contentOuter = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(0)
        };
        var contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(1),
            BackColor = Color.FromArgb(203, 213, 225)
        };
        var contentInner = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 7, 11, 0),
            BackColor = Color.White
        };
        contentInner.Controls.Add(_browser);
        contentInner.Controls.Add(searchHost);
        contentHost.Controls.Add(contentInner);
        contentOuter.Controls.Add(contentHost);

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
    private static Button CreateNavigationButton(string text, HelpNavigationIcon icon, AnchorStyles anchor)
    {
        return new HelpNavigationButton(text, icon)
        {
            Width = 148,
            Height = 36,
            Margin = new Padding(0),
            Anchor = anchor,
        };
    }

    // Zoek/commentaar: Maakt compacte knoppen voor zoeken binnen de helptekst.
    private static Button CreateSearchButton(string text, string tooltip)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(2, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(15, 63, 143),
            Font = new Font("Segoe UI", 9),
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 246, 255);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(219, 234, 254);
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
                JavaScriptString(query) + ", " + _currentSearchIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + ");";
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
                JavaScriptString(query) + ", " + offset.ToString(System.Globalization.CultureInfo.InvariantCulture) + ");";
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

    private static string JavaScriptString(string value)
    {
        return "\"" + value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n") + "\"";
    }
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

    protected override void OnPaint(PaintEventArgs pevent)
    {
        var g = pevent.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? SystemColors.Control);

        var rect = new RectangleF(0.5f, 0.5f, ClientSize.Width - 1, ClientSize.Height - 1);
        var backgroundColor = !Enabled
            ? Color.FromArgb(232, 240, 254)
            : _pressed
                ? Color.FromArgb(191, 219, 254)
                : _hover
                    ? Color.FromArgb(219, 234, 254)
                    : Color.FromArgb(239, 246, 255);
        var borderColor = Enabled ? Color.FromArgb(59, 130, 246) : Color.FromArgb(147, 197, 253);
        var iconColor = Enabled ? Color.FromArgb(29, 78, 216) : Color.FromArgb(75, 105, 150);
        var textColor = Enabled ? Color.FromArgb(15, 63, 143) : Color.FromArgb(75, 105, 150);

        using var background = new SolidBrush(backgroundColor);
        using var borderPen = new Pen(borderColor, 1f);
        using var path = RoundedRect(rect, 7f);
        g.FillPath(background, path);
        g.DrawPath(borderPen, path);

        var iconRect = _icon switch
        {
            HelpNavigationIcon.Next => new RectangleF(ClientSize.Width - 42, 5, 24, 24),
            _ => new RectangleF(18, 5, 24, 24)
        };

        using var iconPen = new Pen(iconColor, 2.4f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        using var iconFill = new SolidBrush(Enabled ? Color.FromArgb(239, 246, 255) : Color.FromArgb(241, 245, 249));
        DrawIcon(g, iconPen, iconFill, iconRect);

        var textRect = _icon switch
        {
            HelpNavigationIcon.Previous => new Rectangle(52, 0, ClientSize.Width - 62, ClientSize.Height),
            HelpNavigationIcon.Next => new Rectangle(10, 0, ClientSize.Width - 62, ClientSize.Height),
            _ => new Rectangle(52, 0, ClientSize.Width - 62, ClientSize.Height)
        };
        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
        flags |= _icon == HelpNavigationIcon.Next ? TextFormatFlags.Right : TextFormatFlags.Left;
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
            ? Color.FromArgb(219, 234, 254)
            : isGroup ? Color.FromArgb(248, 250, 252)
            : isChild ? Color.FromArgb(252, 253, 255) : BackColor;
        var textColor = selected
            ? Color.FromArgb(15, 63, 143)
            : isGroup ? Color.FromArgb(15, 63, 143) : ForeColor;

        using (var background = new SolidBrush(backgroundColor))
            g.FillRectangle(background, rect);

        if (isGroup)
        {
            using var separator = new Pen(Color.FromArgb(226, 232, 240));
            g.DrawLine(separator, rect.Left + 8, rect.Bottom - 1, rect.Right - 8, rect.Bottom - 1);
        }

        if (isChild)
        {
            using var guide = new Pen(Color.FromArgb(203, 213, 225), 1);
            var x = rect.Left + 24;
            var hasNextSibling = index + 1 < _items.Count
                && IsSameChildGroup(_items[index].Title, _items[index + 1].Title);
            var midY = rect.Top + rect.Height / 2;
            g.DrawLine(guide, x, rect.Top, x, hasNextSibling ? rect.Bottom : midY);
            g.DrawLine(guide, x, midY, x + 12, midY);
        }

        if (selected)
        {
            using var accent = new SolidBrush(Color.FromArgb(37, 99, 235));
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

    private static void DrawToggle(Graphics g, Rectangle rect, bool collapsed)
    {
        using var border = new Pen(Color.FromArgb(96, 120, 160), 1);
        using var fill = new SolidBrush(Color.White);
        g.FillRectangle(fill, rect);
        g.DrawRectangle(border, rect);

        using var pen = new Pen(Color.FromArgb(71, 85, 105), 1.4f)
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

    private static void DrawFolder(Graphics g, Rectangle rect, bool collapsed)
    {
        using var fill = new SolidBrush(collapsed ? Color.FromArgb(239, 246, 255) : Color.FromArgb(219, 234, 254));
        using var border = new Pen(Color.FromArgb(37, 99, 235), 1);
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

    private static void DrawLeaf(Graphics g, Rectangle rect)
    {
        using var fill = new SolidBrush(Color.White);
        using var border = new Pen(Color.FromArgb(148, 163, 184), 1);
        g.FillRectangle(fill, rect);
        g.DrawRectangle(border, rect);
        using var line = new Pen(Color.FromArgb(148, 163, 184), 1);
        g.DrawLine(line, rect.Left + 3, rect.Top + 4, rect.Right - 2, rect.Top + 4);
        g.DrawLine(line, rect.Left + 3, rect.Top + 7, rect.Right - 2, rect.Top + 7);
    }

    private void DrawScrollbar(Graphics g, Rectangle rect)
    {
        using var track = new SolidBrush(Color.FromArgb(248, 250, 252));
        using var trackBorder = new Pen(Color.FromArgb(215, 221, 231));
        g.FillRectangle(track, rect);
        g.DrawLine(trackBorder, rect.Left, rect.Top, rect.Left, rect.Bottom);

        var thumb = GetThumbRectangle();
        using var thumbBrush = new SolidBrush(_scrollbarHover || _scrollbarDragging ? Color.FromArgb(107, 114, 128) : Color.FromArgb(139, 148, 158));
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
