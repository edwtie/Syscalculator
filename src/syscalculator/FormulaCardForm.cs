using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Net;
using System.Runtime.InteropServices;
using Tiedragon.Help;
using Tiedragon.NodSystem.Core;

namespace Syscalculator.UI.WinForms;

internal sealed class FormulaCardForm : Form
{
    private static readonly string[] TopicTags =
    {
        "Meetkunde",
        "Goniometrie",
        "Meetkunde met coordinaten",
        "Analytische meetkunde",
        "Algebra",
        "Statistiek",
        "Kansrekening",
        "Calculus",
        "Vectoren",
        "Lineaire algebra",
        "Physics",
        "PWS",
        "CAS-light",
        "Limited vector",
        "Limited matrix"
    };

    private static readonly string[] LevelDisplayTags =
    {
        "HAVO",
        "HAVO A",
        "HAVO B",
        "VWO",
        "VWO A",
        "VWO B",
        "VWO D",
        "Wiskunde D verdieping",
        "Propedeuse",
        "Universiteit"
    };

    private readonly TreeView _cardsTree;
    private readonly WebView2 _browser;
    private readonly Label _countLabel;
    private readonly TextBox _searchBox;
    private readonly Button _searchPreviousButton;
    private readonly Button _searchNextButton;
    private readonly Button _searchClearButton;
    private readonly Button _previousButton;
    private readonly Button _homeButton;
    private readonly Button _nextButton;
    private readonly IReadOnlyList<FormulaCard> _cards;
    private readonly LanguageCatalog _language;
    private readonly LanguageCatalog _englishLanguage;
    private readonly bool _enableFormulaFilmExperiment;
    private bool _browserFailed;
    private string? _pendingHtml;
    private int _currentSearchIndex = -1;

    public FormulaCardForm(LanguageCatalog language, bool enableFormulaFilmExperiment = false)
    {
        _language = language;
        _englishLanguage = LanguageCatalog.Load(AppContext.BaseDirectory, "eng.lng");
        _enableFormulaFilmExperiment = enableFormulaFilmExperiment;
        _cards = FormulaCardCatalog.GetDefaultCards();

        Text = T("formula_card.title", "Formula cards");
        AppWindowIcon.ApplyTo(this);
        Width = 1180;
        Height = 580;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(920, 560);
        BackColor = Color.FromArgb(248, 250, 252);
        KeyPreview = true;
        KeyDown += FormulaCardForm_KeyDown;

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterWidth = 1,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(226, 232, 240)
        };

        _countLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"{T("formula_card.count", "Formula cards")} ({_cards.Count})",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 45, 65),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.FromArgb(248, 250, 252)
        };

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
        _searchBox.TextChanged += (_, _) => SearchCurrentCard(resetIndex: true);
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

        _cardsTree = new TreeView
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            Font = new Font(Font.FontFamily, 9.2f),
            HideSelection = false,
            Margin = new Padding(0),
            ShowLines = true,
            ShowPlusMinus = true,
            ShowRootLines = true
        };
        BuildCardTree();
        _cardsTree.AfterSelect += (_, _) =>
        {
            ShowSelectedCard();
            UpdateNavigationButtons();
        };

        _browser = new WebView2
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = WebView2UserDataFolder.GetPath()
            }
        };
        _browser.CoreWebView2InitializationCompleted += (_, e) =>
        {
            if (!e.IsSuccess || _browser.CoreWebView2 is null)
            {
                _browserFailed = true;
                return;
            }

            _browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            _browser.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            _browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            _browser.CoreWebView2.WebMessageReceived += Browser_WebMessageReceived;
            _browser.CoreWebView2.NavigationStarting += Browser_NavigationStarting;
            ShowPendingHtmlIfReady();
        };

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
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.FromArgb(241, 245, 249)
        };
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        navigation.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _previousButton = CreateNavigationButton(T("help.nav.previous", "Vorige"), HelpNavigationIcon.Previous, AnchorStyles.Left);
        _homeButton = CreateNavigationButton(T("help.nav.home", "Home"), HelpNavigationIcon.Home, AnchorStyles.None);
        _nextButton = CreateNavigationButton(T("help.nav.next", "Volgende"), HelpNavigationIcon.Next, AnchorStyles.Right);
        var closeButton = CreateNavigationButton(T("dialog.close", "Sluiten"), HelpNavigationIcon.Close, AnchorStyles.Right);
        _previousButton.Click += (_, _) => SelectRelativeCard(-1);
        _homeButton.Click += (_, _) => SelectFirstCardNode();
        _nextButton.Click += (_, _) => SelectRelativeCard(1);
        closeButton.Click += (_, _) => Close();

        navigation.Controls.Add(_previousButton, 0, 0);
        navigation.Controls.Add(_homeButton, 1, 0);
        navigation.Controls.Add(_nextButton, 2, 0);
        navigation.Controls.Add(closeButton, 3, 0);
        navigationHost.Controls.Add(navigation);

        var cardsHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(6, 0, 5, 6)
        };
        var cardsHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 38,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(0, 6, 0, 6)
        };
        cardsHeader.Controls.Add(_countLabel);

        var treeBorder = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(203, 213, 225),
            Padding = new Padding(1)
        };
        treeBorder.Controls.Add(_cardsTree);

        cardsHost.Controls.Add(treeBorder);
        cardsHost.Controls.Add(cardsHeader);

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

        split.Panel1.Controls.Add(cardsHost);
        split.Panel2.Padding = new Padding(0);
        split.Panel2.Controls.Add(contentOuter);
        split.Panel2.Controls.Add(navigationHost);
        Controls.Add(split);
        Shown += (_, _) => ApplyFormulaCardSplitWidth(split);
        Resize += (_, _) => ApplyFormulaCardSplitWidth(split);
        Load += async (_, _) => await InitializeBrowserAsync();

        SelectFirstCardNode();
        UpdateNavigationButtons();
    }

    private static void ApplyFormulaCardSplitWidth(SplitContainer split)
    {
        const int minimumLeftWidth = 250;
        const int preferredLeftWidth = 285;
        const int minimumRightWidth = 640;

        if (split.Width <= minimumLeftWidth + minimumRightWidth)
            return;

        var desiredWidth = Math.Min(preferredLeftWidth, split.Width - minimumRightWidth);
        desiredWidth = Math.Clamp(desiredWidth, minimumLeftWidth, split.Width - minimumRightWidth);
        if (split.SplitterDistance != desiredWidth)
            split.SplitterDistance = desiredWidth;
    }

    private void BuildCardTree()
    {
        _cardsTree.BeginUpdate();
        _cardsTree.Nodes.Clear();

        foreach (var topicGroup in _cards.GroupBy(GetMenuTopic).OrderBy(group => GetMenuTopicOrder(group.Key)).ThenBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase))
        {
            var topicNode = new TreeNode(topicGroup.Key);
            foreach (var subtopicGroup in topicGroup.GroupBy(GetSubtopic).OrderBy(group => GetSubtopicOrder(group.Key)).ThenBy(group => GetSubtopicTitle(group.Key), StringComparer.CurrentCultureIgnoreCase))
            {
                var subtopicNode = new TreeNode(GetSubtopicTitle(subtopicGroup.Key));
                foreach (var card in subtopicGroup.OrderBy(GetCardOrder).ThenBy(card => card.Title, StringComparer.CurrentCultureIgnoreCase))
                {
                    subtopicNode.Nodes.Add(new TreeNode(GetCardMenuText(card)) { Tag = card });
                }

                topicNode.Nodes.Add(subtopicNode);
            }

            topicNode.Expand();
            _cardsTree.Nodes.Add(topicNode);
        }

        if (_enableFormulaFilmExperiment)
        {
            var experimentNode = new TreeNode(T("formula_card.topic_experiment", "2.1 experiment"));
            experimentNode.Nodes.Add(new TreeNode(T("formula_card.formula_film", "Formula film")) { Tag = "formula-film" });
            experimentNode.Expand();
            _cardsTree.Nodes.Add(experimentNode);
        }

        _cardsTree.EndUpdate();
    }

    private void SelectFirstCardNode()
    {
        foreach (TreeNode topicNode in _cardsTree.Nodes)
        {
            foreach (TreeNode subtopicNode in topicNode.Nodes)
            {
                if (subtopicNode.Nodes.Count == 0)
                    continue;

                _cardsTree.SelectedNode = subtopicNode.Nodes[0];
                subtopicNode.Expand();
                return;
            }
        }
    }

    private async Task InitializeBrowserAsync()
    {
        try
        {
            await _browser.EnsureCoreWebView2Async();
        }
        catch (COMException ex)
        {
            _browserFailed = true;
            ShowFallbackText(T("formula_card.webview_error", "WebView2 kon niet starten.") + Environment.NewLine + ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _browserFailed = true;
            ShowFallbackText(T("formula_card.webview_error", "WebView2 kon niet starten.") + Environment.NewLine + ex.Message);
        }
    }

    private static Button CreateButton(string text, EventHandler click)
    {
        var button = new Button
        {
            AutoSize = true,
            Height = 34,
            Text = text,
            Margin = new Padding(6, 6, 0, 0)
        };
        button.Click += click;
        return button;
    }

    private static HelpNavigationButton CreateNavigationButton(string text, HelpNavigationIcon icon, AnchorStyles anchor)
    {
        return new HelpNavigationButton(text, icon)
        {
            Width = 148,
            Height = 36,
            Anchor = anchor,
            Margin = new Padding(0)
        };
    }

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

    private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
            return;

        MoveSearchResult(e.Shift ? -1 : 1);
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    private void FormulaCardForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!e.Control || e.KeyCode != Keys.F)
            return;

        _searchBox.Focus();
        _searchBox.SelectAll();
        e.Handled = true;
    }

    private string T(string key, string fallback) => HelpApi.Text(ResolveHelpLanguageText, key, fallback);

    private void ShowSelectedCard()
    {
        if (_cardsTree.SelectedNode?.Tag is string special &&
            special.Equals("formula-film", StringComparison.OrdinalIgnoreCase))
        {
            SetHtml(BuildFormulaFilmHtml());
            return;
        }

        if (_cardsTree.SelectedNode?.Tag is not FormulaCard card)
            return;

        SetHtml(BuildCardHtml(card));
    }

    private string BuildFormulaFilmHtml()
    {
        var css = HelpHtml.Css("formula-film.css", FormulaFilmFallbackCss());
        var body = HelpHtml.RenderTemplate("formula-film.html", new Dictionary<string, string?>
        {
            ["title"] = Html(T("formula_card.formula_film_title", "Formula film 2.1 experiment")),
            ["subtitle"] = Html(T("formula_card.formula_film_subtitle", "Research for moving formula cards and teaching films"))
        });
        return HelpHtml.WrapBodyPage(body, css, bodyTail: HelpHtml.FormulaFilmScript());
    }

    private static string FormulaFilmFallbackCss()
    {
        return "body { margin:0; padding:20px; font-family:Segoe UI, Arial, sans-serif; background:#f5f7fa; color:#1e2d41; } .page { max-width:760px; margin:0 auto; } .film-stage { position:relative; height:190px; border:1px solid #d8e5f5; border-radius:9px; background:#eef6ff; overflow:hidden; } .actor { position:absolute; left:var(--x); top:var(--y); color:#004aad; font-family:Cambria Math, serif; font-size:30px; font-weight:700; opacity:0; } .actor.show { opacity:1; transition:opacity .35s ease, transform 1.25s ease, left 1.25s ease, top 1.25s ease; }";
    }

    private void Browser_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (_cardsTree.SelectedNode?.Tag is not FormulaCard card)
            return;

        var message = e.TryGetWebMessageAsString();
        switch (message)
        {
            case "copy:nod":
                Clipboard.SetText(CardExampleNod(card));
                break;
            case "copy:latex":
                Clipboard.SetText(card.Latex);
                break;
            case "copy:mathml":
                Clipboard.SetText(card.MathMl);
                break;
            case "copy:text":
                Clipboard.SetText(CardPlainText(card));
                break;
        }
    }

    private void Browser_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!e.Uri.StartsWith("formula-card://", StringComparison.OrdinalIgnoreCase))
            return;

        e.Cancel = true;
        var id = Uri.UnescapeDataString(e.Uri["formula-card://".Length..]);
        SelectCard(id);
    }

    private void SelectCard(string id)
    {
        var node = FindCardNode(_cardsTree.Nodes, id);
        if (node is null)
            return;

        node.Parent?.Expand();
        node.Parent?.Parent?.Expand();
        _cardsTree.SelectedNode = node;
        node.EnsureVisible();
    }

    private void SelectRelativeCard(int delta)
    {
        var nodes = GetSelectableNodes().ToList();
        if (nodes.Count == 0)
            return;

        var selected = _cardsTree.SelectedNode;
        var index = selected is null ? -1 : nodes.IndexOf(selected);
        var nextIndex = Math.Clamp(index + delta, 0, nodes.Count - 1);
        _cardsTree.SelectedNode = nodes[nextIndex];
        nodes[nextIndex].EnsureVisible();
    }

    private void UpdateNavigationButtons()
    {
        var nodes = GetSelectableNodes().ToList();
        var selected = _cardsTree.SelectedNode;
        var index = selected is null ? -1 : nodes.IndexOf(selected);

        _previousButton.Enabled = index > 0;
        _homeButton.Enabled = nodes.Count > 0 && index != 0;
        _nextButton.Enabled = index >= 0 && index < nodes.Count - 1;
    }

    private IEnumerable<TreeNode> GetSelectableNodes()
    {
        foreach (TreeNode node in WalkNodes(_cardsTree.Nodes))
        {
            if (node.Tag is FormulaCard or string)
                yield return node;
        }
    }

    private static IEnumerable<TreeNode> WalkNodes(TreeNodeCollection nodes)
    {
        foreach (TreeNode node in nodes)
        {
            yield return node;
            foreach (var child in WalkNodes(node.Nodes))
                yield return child;
        }
    }

    private static TreeNode? FindCardNode(TreeNodeCollection nodes, string id)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Tag is FormulaCard card &&
                card.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                return node;

            var child = FindCardNode(node.Nodes, id);
            if (child is not null)
                return child;
        }

        return null;
    }

    private void CopySelected(Func<FormulaCard, string> selector)
    {
        if (_cardsTree.SelectedNode?.Tag is FormulaCard card)
            Clipboard.SetText(selector(card));
    }

    private void SetHtml(string html)
    {
        _pendingHtml = ApplyHelpLanguagePlaceholders(html);
        ShowPendingHtmlIfReady();
    }

    private void ShowPendingHtmlIfReady()
    {
        if (_browserFailed || _pendingHtml is null || _browser.CoreWebView2 is null)
            return;

        var html = _pendingHtml;
        _pendingHtml = null;
        _browser.NavigateToString(html);
        SearchCurrentCard(resetIndex: true);
    }

    private void ShowFallbackText(string text)
    {
        var box = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text = text
        };

        var parent = _browser.Parent;
        if (parent is null)
            return;

        var row = parent.Controls.GetChildIndex(_browser);
        parent.Controls.Remove(_browser);
        parent.Controls.Add(box);
        parent.Controls.SetChildIndex(box, row);
    }

    private async void SearchCurrentCard(bool resetIndex)
    {
        if (_browserFailed || IsDisposed || _browser.IsDisposed || _browser.CoreWebView2 is null)
            return;

        if (resetIndex && _currentSearchIndex != int.MaxValue)
            _currentSearchIndex = -1;

        var query = _searchBox.Text.Trim();
        try
        {
            await EnsureSearchScriptAsync();
            var script = "window.syscalFormulaSearch && window.syscalFormulaSearch(" +
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
            var script = "window.syscalFormulaMoveSearch && window.syscalFormulaMoveSearch(" +
                WebText.JavaScriptString(query) + ", " + offset.ToString(System.Globalization.CultureInfo.InvariantCulture) + ");";
            var result = await _browser.CoreWebView2.ExecuteScriptAsync(script);
            if (int.TryParse(result, out var index) && index >= 0)
            {
                _currentSearchIndex = index;
                return;
            }

            SelectCardWithSearchMatch(query, offset);
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

    private void SelectCardWithSearchMatch(string query, int offset)
    {
        var nodes = GetSelectableNodes().ToList();
        if (nodes.Count == 0)
            return;

        var selected = _cardsTree.SelectedNode;
        var startIndex = selected is null ? 0 : Math.Max(0, nodes.IndexOf(selected));
        var direction = offset < 0 ? -1 : 1;
        for (var step = 1; step <= nodes.Count; step++)
        {
            var index = (startIndex + direction * step) % nodes.Count;
            if (index < 0)
                index += nodes.Count;

            if (!NodeContains(nodes[index], query))
                continue;

            _currentSearchIndex = direction < 0 ? int.MaxValue : 0;
            _cardsTree.SelectedNode = nodes[index];
            nodes[index].EnsureVisible();
            return;
        }
    }

    private bool NodeContains(TreeNode node, string query)
    {
        if (node.Tag is FormulaCard card)
        {
            return CardTitle(card).Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                CardFormulaText(card).Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                CardDescription(card).Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                card.Latex.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                card.MathMl.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                CardExampleNod(card).Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                CardPlainText(card).Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                CardTags(card).Any(tag => tag.Contains(query, StringComparison.CurrentCultureIgnoreCase));
        }

        return node.Text.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    private async Task EnsureSearchScriptAsync()
    {
        if (_browser.CoreWebView2 is null)
            return;

        await _browser.CoreWebView2.ExecuteScriptAsync(ApplyHelpLanguagePlaceholders(HelpHtml.FormulaSearchScript()));
    }

    // [some.lng.key] in bewerkbare formulekaart-HTML/JS komt uit de actieve taal, met eng.lng als default.
    private string ApplyHelpLanguagePlaceholders(string template)
    {
        return HelpApi.ApplyLanguagePlaceholders(ResolveHelpLanguageText, template);
    }

    private string? ResolveHelpLanguageText(string key)
    {
        if (_language.TryText(key, out var value))
            return value;

        return _englishLanguage.TryText(key, out var englishValue)
            ? englishValue
            : null;
    }

    private string BuildCardHtml(FormulaCard card)
    {
        var tags = string.Join("", CardTags(card)
            .Select(DisplayTag)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .Select(tag => $"<span class=\"tag\">{Html(tag)}</span>"));
        var visual = BuildVisualHtml(card);
        var nodMath = BuildNodMathHtml(card);
        var studySteps = BuildStudyStepsHtml(card);
        var overview = BuildOverviewHtml(card, _cards);

        var css = """
        * {
          box-sizing: border-box;
        }
        :root {
          color-scheme: light;
          font-family: "Segoe UI", Arial, sans-serif;
          background: #f5f7fa;
          color: #1e2d41;
        }
        body {
          margin: 0;
          padding: 20px;
          background: #f5f7fa;
        }
        .page {
          max-width: 760px;
          margin: 0 auto;
        }
        h1 {
          margin: 0 0 8px;
          font-size: 30px;
          font-weight: 650;
        }
        .subtitle {
          color: #5d6c7e;
          margin-bottom: 18px;
          font-size: 14px;
        }
        .tags {
          display: flex;
          flex-wrap: wrap;
          gap: 6px;
          margin: 0 0 18px;
        }
        .tag {
          border: 1px solid #bad1ea;
          background: #e8f2ff;
          color: #1b4d7a;
          border-radius: 999px;
          padding: 4px 9px;
          font-size: 12px;
          font-weight: 600;
        }
        .overview-table {
          width: 100%;
          table-layout: fixed;
          border-collapse: separate;
          border-spacing: 0;
          border: 1px solid #d9e2ec;
          border-radius: 8px;
          overflow: hidden;
          background: #ffffff;
        }
        .overview-table th,
        .overview-table td {
          padding: 8px 10px;
          border-bottom: 1px solid #e5edf5;
          text-align: left;
          vertical-align: top;
          font-size: 13px;
          min-width: 0;
          overflow-wrap: anywhere;
        }
        .overview-table th:nth-child(1),
        .overview-table td:nth-child(1) {
          width: 22%;
        }
        .overview-table th:nth-child(2),
        .overview-table td:nth-child(2) {
          width: 36%;
        }
        .overview-table th:nth-child(3),
        .overview-table td:nth-child(3) {
          width: 30%;
        }
        .overview-table th:nth-child(4),
        .overview-table td:nth-child(4) {
          width: 12%;
        }
        .overview-table tr:last-child td {
          border-bottom: 0;
        }
        .overview-table th {
          background: #eaf2ff;
          color: #0f3f8f;
          font-weight: 700;
        }
        .overview-table a {
          color: #0f3f8f;
          font-weight: 700;
          text-decoration: none;
        }
        .overview-table a:hover {
          text-decoration: underline;
        }
        .overview-summary {
          margin: 0 0 12px;
          color: #526173;
          font-size: 13px;
          line-height: 1.45;
        }
        .overview-formula math {
          font-family: Cambria Math, "STIX Two Math", "Times New Roman", serif;
          display: block;
          max-width: 100%;
          overflow: hidden;
          font-size: 18px;
          line-height: 1.35;
        }
        .formula-caption {
          margin-top: 4px;
          color: #607086;
          font-size: 12px;
        }
        .method-board {
          margin: 12px 0 18px;
          border: 1px solid #ccd7e6;
          background: #ffffff;
          border-radius: 8px;
          overflow: hidden;
        }
        .method-board-header {
          display: flex;
          align-items: center;
          gap: 12px;
          padding: 12px 14px;
          border-bottom: 1px solid #e3ebf4;
          background: linear-gradient(90deg, #f8fffb, #f8fbff);
        }
        .summary-stamp {
          border: 3px solid #16a34a;
          color: #116a33;
          background: #ecfdf3;
          font-weight: 800;
          font-size: 18px;
          padding: 4px 12px;
          transform: rotate(-3deg);
        }
        .method-board-title {
          font-size: 20px;
          font-weight: 700;
          color: #1e2d41;
        }
        .standard-form {
          display: inline-flex;
          margin: 12px auto 10px;
          padding: 7px 18px;
          border: 2px solid #16a34a;
          color: #1e2d41;
          font-family: "Cambria Math", "STIX Two Math", serif;
          font-size: 21px;
          font-weight: 700;
        }
        .method-grid {
          display: grid;
          grid-template-columns: repeat(2, minmax(0, 1fr));
          gap: 10px;
          padding: 0 12px 14px;
        }
        .method-card {
          border: 1px solid #d8e2ed;
          background: #fbfdff;
          border-radius: 8px;
          padding: 10px 12px;
          min-height: 168px;
        }
        .method-card.wide {
          grid-column: span 2;
        }
        .method-row {
          display: grid;
          grid-template-columns: repeat(3, minmax(0, 1fr));
          gap: 10px;
        }
        .method-label {
          display: inline-block;
          border: 2px solid #38bdf8;
          background: #f0f9ff;
          color: #0f3f8f;
          font-weight: 700;
          padding: 4px 8px;
          margin-bottom: 8px;
        }
        .method-label.red {
          border-color: #fb7185;
          background: #fff1f2;
          color: #be123c;
        }
        .method-label.brown {
          border-color: #a16207;
          background: #fefce8;
          color: #854d0e;
        }
        .method-chip {
          display: inline-block;
          background: #eef2f7;
          color: #334155;
          padding: 4px 8px;
          margin-bottom: 9px;
          font-weight: 650;
        }
        .method-steps {
          font-family: "Cambria Math", "STIX Two Math", serif;
          font-size: 18px;
          line-height: 1.45;
          color: #111827;
        }
        .method-steps div {
          margin: 2px 0;
        }
        .method-note {
          color: #607086;
          font-size: 12px;
          margin-top: 8px;
        }
        .relation-card {
          margin: 10px 0 14px;
          border: 1px solid #dbe7f3;
          border-radius: 8px;
          background: #fbfdff;
          overflow: hidden;
        }
        .relation-title {
          padding: 9px 11px;
          background: #eef6ff;
          border-bottom: 1px solid #e5edf5;
          color: #24476a;
          font-size: 13px;
          font-weight: 700;
        }
        .power-map {
          position: relative;
          height: 190px;
          margin: 10px 11px 14px;
        }
        .power-svg {
          position: absolute;
          inset: 0;
          width: 100%;
          height: 100%;
        }
        .power-svg .diff-path {
          stroke: #0f66c2;
          stroke-width: 2.6;
          fill: none;
        }
        .power-svg .primitive-path {
          stroke: #c45a12;
          stroke-width: 2.3;
          fill: none;
          stroke-dasharray: 6 4;
        }
        .relation-box {
          position: absolute;
          width: 180px;
          border: 1px solid #dce8f5;
          border-radius: 8px;
          background: #ffffff;
          padding: 9px 10px;
          box-shadow: 0 1px 2px rgba(30,45,65,0.06);
        }
        .relation-box.function {
          left: 24px;
          top: 12px;
        }
        .relation-box.derivative {
          right: 24px;
          top: 12px;
        }
        .relation-box.primitive {
          left: 50%;
          bottom: 12px;
          transform: translateX(-50%);
        }
        .relation-label {
          color: #607086;
          font-size: 12px;
          margin-bottom: 5px;
        }
        .power-map math,
        .trig-cycle math {
          font-family: Cambria Math, "STIX Two Math", "Times New Roman", serif;
          font-size: 19px;
        }
        .trig-cycle {
          padding: 0 11px 12px;
        }
        .cycle-title {
          color: #24476a;
          font-size: 13px;
          font-weight: 700;
          margin: 0 0 8px;
        }
        .cycle-wheel {
          position: relative;
          height: 300px;
          margin: 0 auto;
          max-width: 520px;
        }
        .cycle-svg {
          position: absolute;
          inset: 0;
          width: 100%;
          height: 100%;
        }
        .cycle-svg .diff-path {
          stroke: #0f66c2;
          stroke-width: 2.4;
          fill: none;
        }
        .cycle-svg .primitive-path {
          stroke: #c45a12;
          stroke-width: 2;
          fill: none;
          stroke-dasharray: 5 4;
        }
        .cycle-node {
          position: absolute;
          width: 132px;
          border: 1px solid #dce8f5;
          border-radius: 8px;
          background: #ffffff;
          padding: 8px;
          text-align: center;
          box-shadow: 0 1px 2px rgba(30,45,65,0.06);
        }
        .cycle-node.top-left {
          left: 50%;
          top: 10px;
          transform: translateX(-50%);
        }
        .cycle-node.top-right {
          right: 18px;
          top: 112px;
        }
        .cycle-node.bottom-right {
          left: 50%;
          bottom: 10px;
          transform: translateX(-50%);
        }
        .cycle-node.bottom-left {
          left: 18px;
          top: 112px;
        }
        .cycle-legend {
          display: flex;
          gap: 12px;
          flex-wrap: wrap;
          color: #607086;
          font-size: 12px;
          margin: 8px 0 10px;
        }
        .legend-dot {
          display: inline-block;
          width: 10px;
          height: 10px;
          border-radius: 50%;
          margin-right: 5px;
        }
        .legend-dot.diff {
          background: #0f66c2;
        }
        .legend-dot.primitive {
          background: #c45a12;
        }
        .cycle-caption {
          color: #607086;
          font-size: 12px;
          margin-top: 7px;
        }
        .trig-cycle math {
          font-family: Cambria Math, "STIX Two Math", "Times New Roman", serif;
          font-size: 19px;
        }
        @media (max-width: 680px) {
          .power-map {
            height: 260px;
          }
          .relation-box {
            width: 145px;
          }
          .relation-box.function {
            left: 8px;
          }
          .relation-box.derivative {
            right: 8px;
          }
          .cycle-wheel {
            height: 330px;
          }
          .cycle-node {
            width: 120px;
          }
        }
        .current-row td {
          background: #fff8e1;
        }
        .subtopic-row td {
          background: #f4f7fb;
          color: #3c4f66;
          font-size: 12px;
          font-weight: 700;
          text-transform: uppercase;
          letter-spacing: 0;
        }
        .section {
          background: #ffffff;
          border: 1px solid #d9e2ec;
          border-radius: 8px;
          padding: 15px 16px;
          margin: 12px 0;
          box-shadow: 0 1px 2px rgba(30,45,65,0.05);
        }
        .section h2 {
          margin: 0 0 10px;
          font-size: 15px;
          color: #31455f;
        }
        .study-grid {
          display: grid;
          gap: 10px;
        }
        .study-step {
          border: 1px solid #dbe6f3;
          background: #f8fbff;
          border-radius: 8px;
          padding: 11px 12px;
        }
        .study-label {
          display: block;
          color: #0f3f8f;
          font-weight: 800;
          margin-bottom: 7px;
        }
        .study-step .mathml-card {
          margin: 0;
          min-height: 0;
        }
        .study-step p {
          margin: 0;
        }
        .study-step p + p,
        .study-step .mathml-card + .mathml-card {
          margin-top: 8px;
        }
        .formula {
          font-family: Cambria Math, "Times New Roman", serif;
          font-size: 18px;
          line-height: 1.35;
          color: #101820;
        }
        .mathml-card {
          display: block;
          width: 100%;
          box-sizing: border-box;
          overflow-x: auto;
          padding: 10px 12px;
          margin-bottom: 10px;
          border: 1px solid #dbe7f3;
          border-radius: 8px;
          background: #f7fbff;
          color: #101820;
        }
        .mathml-card math {
          font-family: Cambria Math, "STIX Two Math", "Times New Roman", serif;
          font-size: 25px;
          line-height: 1.5;
        }
        .plain-formula {
          color: #526173;
          font-size: 14px;
        }
        .math-note {
          margin-top: 9px;
          color: #687789;
          font-size: 12.5px;
        }
        .vector-frame {
          display: block;
          width: 100%;
          max-width: 420px;
          height: auto;
          border: 1px solid #d7e2ee;
          border-radius: 8px;
          background: #f9fbfe;
        }
        .axis {
          stroke: #6b7c90;
          stroke-width: 1.4;
        }
        .grid {
          stroke: #dce8f5;
          stroke-width: 1;
        }
        .vector {
          stroke: #2563eb;
          stroke-width: 4;
          stroke-linecap: round;
        }
        .helper {
          stroke: #7aa7e8;
          stroke-width: 1.6;
          stroke-dasharray: 5 5;
        }
        .point {
          fill: #2563eb;
        }
        .graph-label {
          fill: #31455f;
          font: 13px "Segoe UI", Arial, sans-serif;
        }
        p {
          margin: 0;
          line-height: 1.5;
        }
        pre {
          margin: 0;
          white-space: pre-wrap;
          font: 13px Consolas, "Cascadia Mono", monospace;
          background: #101827;
          border: 1px solid #24324d;
          border-radius: 8px;
          padding: 13px 14px;
          color: #e5eefc;
          box-shadow: 0 8px 20px rgba(15,23,42,0.10);
          overflow: auto;
        }
        .actions {
          display: flex;
          flex-wrap: wrap;
          gap: 8px;
          margin-top: 14px;
        }
        button {
          border: 1px solid #b8c7d8;
          background: #ffffff;
          color: #1e2d41;
          border-radius: 6px;
          padding: 7px 11px;
          font: 13px "Segoe UI", Arial, sans-serif;
          cursor: pointer;
        }
        button:hover {
          background: #eef5ff;
          border-color: #83aeda;
        }
        """;
        var body = HelpHtml.RenderTemplate("formula-card.html", new Dictionary<string, string?>
        {
            ["title"] = Html(CardTitle(card)),
            ["subtitle"] = Html(T("formula_card.subtitle", "Formula cards for learning, PWS and export")),
            ["tags"] = tags,
            ["overview"] = overview,
            ["section_formula"] = Html(T("formula_card.section_formula", "Formule")),
            ["mathml_card"] = card.MathMl,
            ["copy_text"] = Html(T("formula_card.copy_text", "Copy text")),
            ["copy_latex"] = Html(T("formula_card.copy_latex", "Copy LaTeX")),
            ["copy_mathml"] = Html(T("formula_card.copy_mathml", "Copy MathML")),
            ["study_steps"] = studySteps,
            ["nod_math"] = nodMath,
            ["visual"] = visual,
            ["section_explanation"] = Html(T("formula_card.section_explanation", "Uitleg")),
            ["description"] = Html(CardDescription(card)),
            ["latex"] = Html(card.Latex),
            ["mathml_pre"] = Html(card.MathMl),
            ["section_example_nod"] = Html(T("formula_card.section_example_nod", "Example NOD")),
            ["example_nod"] = Html(CardExampleNod(card)),
            ["copy_nod"] = Html(T("formula_card.copy_nod", "Copy NOD"))
        });

        return HelpHtml.WrapBodyPage(body, css, bodyTail: HelpHtml.FormulaCardCopyButtonsScript());
    }

    private string BuildOverviewHtml(FormulaCard current, IReadOnlyList<FormulaCard> cards)
    {
        var topic = GetPrimaryTopic(current);
        if (topic is null)
            return "";

        if (topic is "Differentiatie" or "Integralen")
        {
            return BuildTaggedOverviewHtml(
                current,
                cards.Where(card => card.LevelTags.Contains("Differentiatie") || card.LevelTags.Contains("Integralen")),
                "Differentiatie en integralen overzicht",
                "Onderwerp");
        }

        return BuildTaggedOverviewHtml(
            current,
            cards.Where(card => IsInTopic(card, topic)),
            $"{GetTopicTitle(topic)} overzicht",
            "Onderwerp");
    }

    private string BuildStudyStepsHtml(FormulaCard card)
    {
        var step = GetFormulaStudyStep(card);
        var (nodRule, _) = GetStudyNodRule(card);

        return $$"""
          <section class="section">
            <h2>Uitgewerkte formulekaart</h2>
            <div class="study-grid">
              <div class="study-step">
                <span class="study-label">Formule</span>
                <div class="mathml-card">{{card.MathMl}}</div>
              </div>
              <div class="study-step">
                <span class="study-label">Gegevens</span>
                {{step.DataHtml}}
              </div>
              <div class="study-step">
                <span class="study-label">Oplossing</span>
                {{step.SolutionHtml}}
              </div>
              <div class="study-step">
                <span class="study-label">NOD</span>
                <pre>{{Html(nodRule)}}</pre>
              </div>
              <div class="study-step">
                <span class="study-label">Resultaat</span>
                {{step.ResultHtml}}
              </div>
            </div>
          </section>
        """;
    }

    private sealed record FormulaStudyStep(string DataHtml, string SolutionHtml, string ResultHtml);

    private static FormulaStudyStep GetFormulaStudyStep(FormulaCard card)
    {
        return card.Id switch
        {
            "pythagoras" => new(
                "<p>Zijde <code>a = 3</code> en zijde <code>b = 4</code>.</p>",
                MathBlock("<mrow><mi>c</mi><mo>=</mo><msqrt><mrow><msup><mn>3</mn><mn>2</mn></msup><mo>+</mo><msup><mn>4</mn><mn>2</mn></msup></mrow></msqrt><mo>=</mo><msqrt><mn>25</mn></msqrt><mo>=</mo><mn>5</mn></mrow>"),
                "<p><code>c = 5</code>.</p>"),
            "quadratic-formula" => new(
                "<p>Vergelijking <code>x^2 - 3x + 2 = 0</code>, dus <code>a = 1</code>, <code>b = -3</code>, <code>c = 2</code>.</p>",
                MathBlock("<mrow><mi>D</mi><mo>=</mo><msup><mrow><mo>-</mo><mn>3</mn></mrow><mn>2</mn></msup><mo>-</mo><mn>4</mn><mo>&#x00D7;</mo><mn>1</mn><mo>&#x00D7;</mo><mn>2</mn><mo>=</mo><mn>1</mn></mrow>") +
                MathBlock("<mrow><mi>x</mi><mo>=</mo><mfrac><mrow><mn>3</mn><mo>&#x00B1;</mo><msqrt><mn>1</mn></msqrt></mrow><mn>2</mn></mfrac></mrow>"),
                "<p><code>x = 1</code> of <code>x = 2</code>.</p>"),
            "exponential-growth" => new(
                "<p>Beginwaarde <code>N0 = 100</code>, groeifactor <code>g = 1,04</code>, tijd <code>t = 5</code>.</p>",
                MathBlock("<mrow><mi>N</mi><mo>(</mo><mn>5</mn><mo>)</mo><mo>=</mo><mn>100</mn><mo>&#x00D7;</mo><msup><mn>1.04</mn><mn>5</mn></msup><mo>&#x2248;</mo><mn>121.67</mn></mrow>"),
                "<p>Na 5 stappen is de waarde ongeveer <code>121,67</code>.</p>"),
            "statistics-mean" => new(
                "<p>Dataset <code>2, 4, 4, 4, 5, 5, 7, 9</code>. Er zijn <code>8</code> waarden.</p>",
                MathBlock("<mrow><mover><mi>x</mi><mo>&#x00AF;</mo></mover><mo>=</mo><mfrac><mrow><mn>2</mn><mo>+</mo><mn>4</mn><mo>+</mo><mn>4</mn><mo>+</mo><mn>4</mn><mo>+</mo><mn>5</mn><mo>+</mo><mn>5</mn><mo>+</mo><mn>7</mn><mo>+</mo><mn>9</mn></mrow><mn>8</mn></mfrac><mo>=</mo><mfrac><mn>40</mn><mn>8</mn></mfrac><mo>=</mo><mn>5</mn></mrow>"),
                "<p>Het gemiddelde is <code>5</code>.</p>"),
            "statistics-median" => new(
                "<p>Gesorteerde dataset <code>2, 4, 4, 4, 5, 5, 7, 9</code>. Er zijn <code>8</code> waarden.</p>",
                MathBlock("<mrow><mi>Me</mi><mo>=</mo><mfrac><mrow><mn>4</mn><mo>+</mo><mn>5</mn></mrow><mn>2</mn></mfrac><mo>=</mo><mn>4.5</mn></mrow>"),
                "<p>Bij een even aantal waarden neem je het gemiddelde van de twee middelste waarden.</p>"),
            "statistics-stdev-population" => new(
                "<p>Dataset <code>2, 4, 4, 4, 5, 5, 7, 9</code>, gemiddelde <code>5</code>.</p>",
                MathBlock("<mrow><mi>&#x03C3;</mi><mo>=</mo><msqrt><mfrac><mn>32</mn><mn>8</mn></mfrac><mo>=</mo><msqrt><mn>4</mn></msqrt><mo>=</mo><mn>2</mn></mrow>"),
                "<p>De populatie-standaardafwijking is <code>2</code>. Voor een steekproef gebruik je delen door <code>n - 1</code>.</p>"),
            "probability-combinations" => new(
                "<p>Kies <code>r = 2</code> uit <code>n = 5</code>, volgorde telt niet mee.</p>",
                MathBlock("<mrow><mfenced><mfrac linethickness=\"0\"><mn>5</mn><mn>2</mn></mfrac></mfenced><mo>=</mo><mfrac><mrow><mn>5</mn><mo>!</mo></mrow><mrow><mn>2</mn><mo>!</mo><mn>3</mn><mo>!</mo></mrow></mfrac><mo>=</mo><mfrac><mn>120</mn><mn>12</mn></mfrac><mo>=</mo><mn>10</mn></mrow>"),
                "<p>Er zijn <code>10</code> combinaties.</p>"),
            "probability-expected-value" => new(
                "<p>Uitkomsten <code>0</code> en <code>10</code>, elk met kans <code>0,5</code>.</p>",
                MathBlock("<mrow><mi>E</mi><mo>(</mo><mi>X</mi><mo>)</mo><mo>=</mo><mn>0</mn><mo>&#x00D7;</mo><mn>0.5</mn><mo>+</mo><mn>10</mn><mo>&#x00D7;</mo><mn>0.5</mn><mo>=</mo><mn>5</mn></mrow>"),
                "<p>De verwachte waarde is <code>5</code>.</p>"),
            "trig-right-triangle" => new(
                "<p>Hoek <code>theta = 30 graden</code>. Voor sinus gebruik je overstaande zijde gedeeld door schuine zijde.</p>",
                MathBlock("<mrow><mi>sin</mi><mo>(</mo><mn>30</mn><mo>&#x00B0;</mo><mo>)</mo><mo>=</mo><mfrac><mn>1</mn><mn>2</mn></mfrac><mo>=</mo><mn>0.5</mn></mrow>"),
                "<p><code>sin(30 graden) = 0,5</code>.</p>"),
            "linear-function" => new(
                "<p>Helling <code>a = 2</code>, startwaarde <code>b = 3</code>, invoer <code>x = 5</code>.</p>",
                MathBlock("<mrow><mi>y</mi><mo>=</mo><mn>2</mn><mo>&#x00D7;</mo><mn>5</mn><mo>+</mo><mn>3</mn><mo>=</mo><mn>13</mn></mrow>"),
                "<p><code>y = 13</code>.</p>"),
            "distance-between-points" => new(
                "<p>Punt <code>P(1,2)</code> en punt <code>Q(4,6)</code>.</p>",
                MathBlock("<mrow><mi>d</mi><mo>=</mo><msqrt><mrow><msup><mrow><mo>(</mo><mn>4</mn><mo>-</mo><mn>1</mn><mo>)</mo></mrow><mn>2</mn></msup><mo>+</mo><msup><mrow><mo>(</mo><mn>6</mn><mo>-</mo><mn>2</mn><mo>)</mo></mrow><mn>2</mn></msup></mrow></msqrt><mo>=</mo><msqrt><mn>25</mn></msqrt><mo>=</mo><mn>5</mn></mrow>"),
                "<p>De afstand is <code>5</code>.</p>"),
            "midpoint" => new(
                "<p>Punt <code>P(1,2)</code> en punt <code>Q(5,8)</code>.</p>",
                MathBlock("<mrow><mi>M</mi><mo>=</mo><mo>(</mo><mfrac><mrow><mn>1</mn><mo>+</mo><mn>5</mn></mrow><mn>2</mn></mfrac><mo>,</mo><mfrac><mrow><mn>2</mn><mo>+</mo><mn>8</mn></mrow><mn>2</mn></mfrac><mo>)</mo><mo>=</mo><mo>(</mo><mn>3</mn><mo>,</mo><mn>5</mn><mo>)</mo></mrow>"),
                "<p>Het middenpunt is <code>(3,5)</code>.</p>"),
            "triangle-area" => new(
                "<p>Basis <code>b = 8</code> en hoogte <code>h = 6</code>.</p>",
                MathBlock("<mrow><mi>A</mi><mo>=</mo><mfrac><mn>1</mn><mn>2</mn></mfrac><mo>&#x00D7;</mo><mn>8</mn><mo>&#x00D7;</mo><mn>6</mn><mo>=</mo><mn>24</mn></mrow>"),
                "<p>De oppervlakte is <code>24</code>.</p>"),
            "sine-rule" => new(
                "<p>Bijvoorbeeld <code>a = 10</code>, <code>A = 30 graden</code>, <code>B = 45 graden</code>.</p>",
                MathBlock("<mrow><mi>b</mi><mo>=</mo><mfrac><mrow><mn>10</mn><mo>&#x00D7;</mo><mi>sin</mi><mo>(</mo><mn>45</mn><mo>&#x00B0;</mo><mo>)</mo></mrow><mrow><mi>sin</mi><mo>(</mo><mn>30</mn><mo>&#x00B0;</mo><mo>)</mo></mrow></mfrac><mo>&#x2248;</mo><mn>14.14</mn></mrow>"),
                "<p>De berekende zijde is ongeveer <code>14,14</code>.</p>"),
            "cosine-rule" => new(
                "<p>Zijden <code>a = 3</code>, <code>b = 4</code>, ingesloten hoek <code>C = 90 graden</code>.</p>",
                MathBlock("<mrow><msup><mi>c</mi><mn>2</mn></msup><mo>=</mo><msup><mn>3</mn><mn>2</mn></msup><mo>+</mo><msup><mn>4</mn><mn>2</mn></msup><mo>-</mo><mn>2</mn><mo>&#x00D7;</mo><mn>3</mn><mo>&#x00D7;</mo><mn>4</mn><mo>&#x00D7;</mo><mi>cos</mi><mo>(</mo><mn>90</mn><mo>&#x00B0;</mo><mo>)</mo><mo>=</mo><mn>25</mn></mrow>") +
                MathBlock("<mrow><mi>c</mi><mo>=</mo><msqrt><mn>25</mn></msqrt><mo>=</mo><mn>5</mn></mrow>"),
                "<p><code>c = 5</code>.</p>"),
            "circle-equation" => new(
                "<p>Middelpunt <code>(0,0)</code>, straal <code>5</code>, punt <code>(3,4)</code>.</p>",
                MathBlock("<mrow><msup><mn>3</mn><mn>2</mn></msup><mo>+</mo><msup><mn>4</mn><mn>2</mn></msup><mo>=</mo><mn>25</mn><mo>=</mo><msup><mn>5</mn><mn>2</mn></msup></mrow>"),
                "<p>Het punt ligt op de cirkel.</p>"),
            "vector-2d-arrow" => new(
                "<p>Vector <code>v = (3, 4)</code>.</p>",
                MathBlock("<mrow><mo stretchy=\"false\">&#x2016;</mo><mi mathvariant=\"bold-italic\">v</mi><mo stretchy=\"false\">&#x2016;</mo><mo>=</mo><msqrt><mrow><msup><mn>3</mn><mn>2</mn></msup><mo>+</mo><msup><mn>4</mn><mn>2</mn></msup></mrow></msqrt><mo>=</mo><mn>5</mn></mrow>"),
                "<p>De vectorlengte is <code>5</code>. In NOD kan dat kort met <code>math vec 3 4</code> of volledig met <code>math length(vec(3,4))</code>.</p>"),
            "vector-length-3d" => new(
                "<p>Vector <code>v = (3, 4, 12)</code>.</p>",
                MathBlock("<mrow><mo stretchy=\"false\">&#x2016;</mo><mi mathvariant=\"bold-italic\">v</mi><mo stretchy=\"false\">&#x2016;</mo><mo>=</mo><msqrt><mrow><msup><mn>3</mn><mn>2</mn></msup><mo>+</mo><msup><mn>4</mn><mn>2</mn></msup><mo>+</mo><msup><mn>12</mn><mn>2</mn></msup></mrow></msqrt><mo>=</mo><msqrt><mn>169</mn></msqrt><mo>=</mo><mn>13</mn></mrow>"),
                "<p>De 3D-vectorlengte is <code>13</code>. In Graph 3D zie je de volledige pijl naar <code>(3,4,12)</code>; in Graph 2D kan deze als perspectiefprojectie worden getoond: <code>(3/12,4/12)</code>.</p>"),
            "vector-dot-angle" => new(
                "<p>Vectoren <code>a = (1,0)</code> en <code>b = (0,1)</code>.</p>",
                MathBlock("<mrow><mi>a</mi><mo>&#x22C5;</mo><mi>b</mi><mo>=</mo><mn>1</mn><mo>&#x00D7;</mo><mn>0</mn><mo>+</mo><mn>0</mn><mo>&#x00D7;</mo><mn>1</mn><mo>=</mo><mn>0</mn></mrow>") +
                MathBlock("<mrow><mi>&#x03B8;</mi><mo>=</mo><mi>arccos</mi><mo>(</mo><mn>0</mn><mo>)</mo><mo>=</mo><mn>90</mn><mo>&#x00B0;</mo></mrow>"),
                "<p>Het inproduct is <code>0</code> en de hoek is <code>90 graden</code>.</p>"),
            "vector-cross-z" => new(
                "<p>Vectoren <code>a = (1,0,0)</code> en <code>b = (0,1,0)</code>.</p>",
                MathBlock("<mrow><msub><mrow><mo>(</mo><mi>a</mi><mo>&#x00D7;</mo><mi>b</mi><mo>)</mo></mrow><mi>z</mi></msub><mo>=</mo><mn>1</mn><mo>&#x00D7;</mo><mn>1</mn><mo>-</mo><mn>0</mn><mo>&#x00D7;</mo><mn>0</mn><mo>=</mo><mn>1</mn></mrow>"),
                "<p>De z-component van het kruisproduct is <code>1</code>.</p>"),
            "matrix-2x2-determinant" => new(
                "<p>Matrix met rij 1: <code>1, 2</code> en rij 2: <code>3, 4</code>.</p>",
                MathBlock("<mrow><mi>det</mi><mo>=</mo><mn>1</mn><mo>&#x00D7;</mo><mn>4</mn><mo>-</mo><mn>2</mn><mo>&#x00D7;</mo><mn>3</mn><mo>=</mo><mn>4</mn><mo>-</mo><mn>6</mn><mo>=</mo><mo>-</mo><mn>2</mn></mrow>"),
                "<p><code>det = -2</code>. Extra: <code>mget(..., 2, 1)</code> geeft <code>3</code>; <code>x(mat2(...) * vec(5,6))</code> geeft <code>17</code>.</p>"),
            _ => new(
                "<p>Kies eerst bekende waarden voor de symbolen in de formule.</p>",
                "<p>Vul de gekozen waarden in de formule in en werk van binnen naar buiten uit. Gebruik haakjes bij machten, wortels en breuken.</p>",
                "<p>Het resultaat hangt af van de gekozen waarden.</p>")
        };
    }

    private static string MathBlock(string mathBody)
    {
        return $$"""<div class="mathml-card"><math xmlns="http://www.w3.org/1998/Math/MathML" display="inline">{{mathBody}}</math></div>""";
    }

    private (string Rule, string Note) GetStudyNodRule(FormulaCard card)
    {
        return card.Id switch
        {
            "pythagoras" => ("math sqrt(ans(x)^2 + ans(y)^2)", "2D/multi-input notatie voor NOD 2.1. In oude NOD blijft ans de huidige waarde."),
            "quadratic-formula" => ("math (-ans(b) + sqrt(ans(b)^2 - 4*ans(a)*ans(c))) / (2*ans(a))", "Geeft de plus-oplossing. Voor de min-oplossing gebruik je -sqrt(...)."),
            "exponential-growth" => ("math ans(N0) * ans(g)^ans(t)", "Named inputs zijn bedoeld voor NOD 2.1, zodat meer waarden duidelijk blijven."),
            "statistics-mean" => ("math mean(2,4,4,4,5,5,7,9)", "Gemiddelde van een dataset. Dit is Wiskunde A: centrummaat."),
            "statistics-median" => ("math median(2,4,4,4,5,5,7,9)", "Mediaan na sorteren. Bij een even aantal waarden neemt NOD het gemiddelde van de twee middelste waarden."),
            "statistics-stdev-population" => ("math stdev(2,4,4,4,5,5,7,9)\r\nmath samplestdev(2,4,4,4,5,5,7,9)", "stdev gebruikt delen door n. samplestdev gebruikt delen door n-1 voor een steekproef."),
            "probability-combinations" => ("math comb(ans,2)\r\nmath ncr(ans,2)", "Combinaties: volgorde telt niet mee. ncr is dezelfde schrijfwijze."),
            "probability-expected-value" => ("math expected(0,0.5,10,0.5)", "Verwachtingswaarde leest paren: waarde, kans, waarde, kans."),
            "trig-right-triangle" => ("math sind(ans(theta))", "Voor graden gebruik je sind/cosd/tand. Voor radialen gebruik je sin/cos/tan."),
            "linear-function" => ("math ans(a) * ans(x) + ans(b)", "Lineaire formule met named inputs voor helling a, waarde x en startwaarde b."),
            "distance-between-points" => ("math distance(vec(1,2), vec(4,6))", "Afstand tussen punten kun je zien als lengte van het verschil tussen twee vectoren."),
            "midpoint" => ("math (1 + 5) / 2\r\nmath (2 + 8) / 2", "NOD geeft hier de x- en y-coordinaat als twee losse getallen; een vectorresultaat is geen eindwaarde."),
            "triangle-area" => ("math 0.5 * ans * 6", "Bij input basis 8 en vaste hoogte 6 geeft dit oppervlakte 24."),
            "sine-rule" => ("math ans(a) * sind(ans(B)) / sind(ans(A))", "Voorbeeld: bereken zijde b uit a, A en B door de sinusregel om te vormen."),
            "cosine-rule" => ("math sqrt(ans(a)^2 + ans(b)^2 - 2*ans(a)*ans(b)*cosd(ans(C)))", "Voorbeeld met graden: berekent zijde c met de cosinusregel."),
            "trig-identities" => ("math sind(ans)^2 + cosd(ans)^2", "Controleert numeriek de identiteit sin^2(x)+cos^2(x)=1 bij hoek ans in graden."),
            "exact-trig-values" => ("math sind(30)", "Voor exacte waarden toont de formulekaart de tabel; NOD-math kan een waarde numeriek testen."),
            "circle-equation" => ("math (ans(x) - ans(a))^2 + (ans(y) - ans(b))^2", "Vergelijk met r^2 om te controleren of een punt op de cirkel ligt."),
            "special-right-triangles" => ("math sqrt(3)", "Tabelkaart voor verhoudingen; NOD-math kan de wortelwaarden numeriek tonen."),
            "kinetic-energy" => ("math 0.5 * ans(m) * ans(v)^2", "Natuurkunde-kaart met named inputs voor massa en snelheid."),
            "derivative-power" => ("math diff ans^2", "Dit werkt als numerieke afgeleide bij de huidige inputwaarde."),
            "derivative-sum-rule" => ("math diff (ans^2 + ans)", "Voorbeeld van somregel. NOD berekent numeriek; de formulekaart toont de symbolische regel."),
            "derivative-constant-factor" => ("math diff 3*ans^2", "Voorbeeld van constante factorregel. Het getal voor de functie blijft in de symbolische regel staan."),
            "derivative-product-rule" => ("math diff (ans^2 * sin(ans))", "Voorbeeld van productregel. De formulekaart legt de symbolische regel uit; de rekenregel is numeriek."),
            "derivative-quotient-rule" => ("math diff (ans^2 / (ans + 1))", "Voorbeeld van quotientregel. Test altijd numeriek met F6."),
            "derivative-chain-rule" => ("math diff sin(ans^2)", "Voorbeeld van kettingregel: buitenfunctie en binnenfunctie."),
            "derivative-trig-basic" => ("math diff sin(ans)", "NOD rekent numeriek; de kaart toont dat sin(x) als afgeleide cos(x) heeft."),
            "derivative-exp-log" => ("math diff e^ans", "Voor exponentiele groei. Voor ln(x) kun je math diff ln(ans) testen."),
            "integral-power-rule" => ("math integral 0,1 ans^2", "De formulekaart toont de symbolische primitieve; de rekenregel gebruikt een numerieke integraal."),
            "integral-sum-rule" => ("math integral 0,1 (ans^2 + ans)", "Numerieke integraal van een som. Symbolisch splits je de delen apart."),
            "integral-constant-factor" => ("math integral 0,1 3*ans^2", "Het vaste getal blijft buiten de primitieve-regel staan."),
            "integral-definite-area" => ("math integral 0,1 ans^2", "Bepaalde integraal als numerieke oppervlakte tussen twee grenzen."),
            "vector-2d-arrow" => ("math vec 3 4\r\nmath length(vec(3,4))\r\nmath dot(vec(1,2), vec(3,4))\r\nmath angled(vec(1,0), vec(0,1))", "Gebruik math vec 3 4 of length(vec(3,4)) voor vectorlengte. dot en angled zijn verwante vectorbewerkingen."),
            "vector-length-3d" => ("mode geometry\r\ninput x X component\r\ninput y Y component\r\ninput z Z component\r\nmath length(vec(x,y,z))\r\nmath vec (3,4,12)", Vector3DProjectionNote()),
            "vector-dot-angle" => ("math dot(vec(1,2), vec(3,4))\r\nmath angled(vec(1,0), vec(0,1))", "dot geeft het inproduct. angled geeft de hoek in graden."),
            "vector-cross-z" => ("math z(cross(vec(1,0,0), vec(0,1,0)))", "cross geeft een vector; met z(...) kies je de z-component als eindgetal."),
            "point-line-distance" => ("math |ans(a)*ans(xp) + ans(b)*ans(yp) - ans(c)| / sqrt(ans(a)^2 + ans(b)^2)", "2D analytische meetkunde met named inputs."),
            "matrix-2x2-determinant" => ("math det(mat2(1,2,3,4))\r\nmath det2(1,2,3,4)\r\nmath mget(mat2(1,2,3,4), 2, 1)\r\nmath x(mat2(1,2,3,4) * vec(5,6))", "det en det2 geven de determinant. mget leest een cel. Matrix maal vector kan met x(...) of y(...) naar een component worden omgezet."),
            "circle-integral" => ("math line-integral(F, path)", "Conceptregel voor later. Kringintegraal blijft hier een uitlegkaart, geen runtime-engine."),
            _ => ExtractFirstMathLine(CardExampleNod(card))
        };
    }

    private static string? GetPrimaryTopic(FormulaCard card)
    {
        return TopicTags.FirstOrDefault(card.LevelTags.Contains);
    }

    private static string GetMenuTopic(FormulaCard card)
    {
        return card.Id switch
        {
            _ when IsCalculusCard(card) => "Differentiatie en integralen",
            "statistics-mean" or "statistics-median" or "statistics-stdev-population" => "Statistiek",
            "probability-combinations" or "probability-expected-value" => "Kansrekening",
            "pythagoras" => "Meetkunde",
            "triangle-area" => "Meetkunde",
            "distance-between-points" or "midpoint" => "Meetkunde met coordinaten",
            "vector-2d-arrow" or "vector-length-3d" or "vector-dot-angle" or "vector-cross-z" => "Vectoren",
            "matrix-2x2-determinant" => "Lineaire algebra",
            _ => GetTopicTitle(GetPrimaryTopic(card) ?? "Algemeen")
        };
    }

    private static int GetMenuTopicOrder(string topic)
    {
        return topic switch
        {
            "Differentiatie en integralen" => 0,
            "Algebra" => 1,
            "Goniometrie" => 2,
            "Meetkunde" => 3,
            "Meetkunde met coordinaten" => 4,
            "Vectoren" => 5,
            "Statistiek" => 6,
            "Kansrekening" => 7,
            "Lineaire algebra" => 8,
            "PWS" => 9,
            _ => 20
        };
    }

    private static bool IsCalculusCard(FormulaCard card)
    {
        return card.LevelTags.Contains("Differentiatie") || card.LevelTags.Contains("Integralen");
    }

    private static string GetCardMenuText(FormulaCard card)
    {
        return card.Title;
    }

    private static bool IsInTopic(FormulaCard card, string topic)
    {
        if (card.LevelTags.Contains(topic))
            return true;

        return topic == "Meetkunde met coordinaten"
            && card.LevelTags.Contains("Analytische meetkunde");
    }

    private static string GetTopicTitle(string topic)
    {
        return topic switch
        {
            "Analytische meetkunde" => "Meetkunde met coordinaten",
            "Calculus" => "Integralen",
            _ => topic
        };
    }

    private static string DisplayTag(string tag)
    {
        return tag switch
        {
            "Physics" => "natuurkunde",
            "Limited vector" => "beperkte vector",
            "Limited matrix" => "beperkte matrix",
            "Linear algebra" => "lineaire algebra",
            "Lineaire algebra" => "lineaire algebra",
            "2D" => "2D-tekening",
            "2D graph" => "2D grafiek",
            "HAVO" => "havo",
            "HAVO A" => "havo wiskunde A",
            "HAVO B" => "havo wiskunde B",
            "VWO" => "vwo",
            "VWO A" => "vwo wiskunde A",
            "VWO B" => "vwo wiskunde B",
            "VWO D" => "vwo wiskunde D",
            "Wiskunde D verdieping" => "wiskunde D verdieping",
            "Propedeuse" => "propedeuse",
            "Universiteit" => "universiteit",
            "Examenbasis" => "examenbasis",
            "Meetkunde met coordinaten" => "meetkunde met coordinaten",
            "Analytische meetkunde" => "analytische meetkunde",
            _ => tag
        };
    }

    private string BuildTaggedOverviewHtml(FormulaCard current, IEnumerable<FormulaCard> cards, string title, string firstColumn)
    {
        var summary = BuildOverviewSummary(title);
        var rows = string.Join("", cards.GroupBy(GetSubtopic).Select(group =>
        {
            var heading = $$"""
            <tr class="subtopic-row"><td colspan="4">{{Html(GetStudentSubtopic(group.Key))}}</td></tr>
            """;
            var body = string.Join("", group.Select(card =>
            {
                var currentClass = card.Id.Equals(current.Id, StringComparison.OrdinalIgnoreCase) ? " class=\"current-row\"" : "";
                return $"""
                <tr{currentClass}>
                  <td><a href="formula-card://{Uri.EscapeDataString(card.Id)}">{Html(GetStudentTitle(card))}</a></td>
                  <td class="overview-formula">{GetStudentFormulaHtml(card)}</td>
                  <td>{Html(GetStudentUse(card))}</td>
                  <td>{Html(string.Join(", ", card.LevelTags.Where(LevelDisplayTags.Contains).Select(DisplayTag)))}</td>
                </tr>
                """;
            }));

            return heading + body;
        }));

        return $$"""
          <section class="section">
            <h2>{{Html(title)}}</h2>
            {{summary}}
            <table class="overview-table">
              <tr><th>{{Html(firstColumn)}}</th><th>{{Html(T("formula_card.table_formula", "Formule"))}}</th><th>{{Html(T("formula_card.table_use", "Wanneer?"))}}</th><th>{{Html(T("formula_card.table_level", "Niveau"))}}</th></tr>
              {{rows}}
            </table>
          </section>
        """;
    }

    private string BuildOverviewSummary(string title)
    {
        string? text = title switch
        {
            var value when value.StartsWith("Differentiatie en integralen", StringComparison.OrdinalIgnoreCase) =>
                "Differentieren en integreren horen bij elkaar. Differentieren gaat vooruit naar de afgeleide; primitiveren gaat terug naar een functie. Gebruik de kleuren en pijlen hieronder als geheugensteun.",
            var value when value.StartsWith("Differentiatie", StringComparison.OrdinalIgnoreCase) =>
                T("formula_card.summary_differentiation", "Kort gezegd: differentieren laat zien hoe snel iets verandert. Denk aan de helling van een grafiek. Kies eerst de regel die past bij de vorm van je formule."),
            var value when value.StartsWith("Integralen", StringComparison.OrdinalIgnoreCase) =>
                T("formula_card.summary_integrals", "Kort gezegd: integreren gebruik je voor oppervlakte onder een grafiek of om terug te gaan van afgeleide naar functie. Primitiveren controleer je vaak door weer te differentieren."),
            _ => null
        };

        if (text is null)
            return "";

        var relation = title.StartsWith("Differentiatie", StringComparison.OrdinalIgnoreCase) ||
            title.StartsWith("Integralen", StringComparison.OrdinalIgnoreCase)
                ? BuildDiffIntegralRelationHtml()
                : "";

        return $"""<p class="overview-summary">{Html(text)}</p>{relation}""";
    }

    private static string BuildDiffIntegralRelationHtml()
    {
        return """
        <div class="relation-card">
          <div class="relation-title">Onthoud: primitiveren is terugrekenen van een afgeleide</div>
          <div class="power-map">
            <svg class="power-svg" viewBox="0 0 520 190" aria-hidden="true">
              <defs>
                <marker id="power-diff-arrow" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto">
                  <path d="M0,0 L8,4 L0,8 Z" fill="#0f66c2"></path>
                </marker>
                <marker id="power-primitive-arrow" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto">
                  <path d="M0,0 L8,4 L0,8 Z" fill="#c45a12"></path>
                </marker>
              </defs>
              <path class="diff-path" marker-end="url(#power-diff-arrow)" d="M185 48 C240 18, 280 18, 335 48"></path>
              <path class="primitive-path" marker-end="url(#power-primitive-arrow)" d="M337 82 C310 142, 210 142, 183 82"></path>
            </svg>
            <div class="relation-box function">
              <div class="relation-label">functie</div>
              <math xmlns="http://www.w3.org/1998/Math/MathML"><msup><mi>x</mi><mn>2</mn></msup></math>
            </div>
            <div class="relation-box derivative">
              <div class="relation-label">differentieren geeft</div>
              <math xmlns="http://www.w3.org/1998/Math/MathML"><mn>2</mn><mi>x</mi></math>
            </div>
            <div class="relation-box primitive">
              <div class="relation-label">primitieve van 2x</div>
              <math xmlns="http://www.w3.org/1998/Math/MathML"><msup><mi>x</mi><mn>2</mn></msup><mo>+</mo><mi>C</mi></math>
            </div>
          </div>
          <div class="trig-cycle">
            <div class="cycle-title">Sinus/cosinus-cirkel bij differentieren</div>
            <div class="cycle-legend"><span><span class="legend-dot diff"></span>blauw: differentieren</span><span><span class="legend-dot primitive"></span>oranje: primitiveren terug</span></div>
            <div class="cycle-wheel">
              <svg class="cycle-svg" viewBox="0 0 520 300" aria-hidden="true">
                <defs>
                  <marker id="diff-arrow" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto">
                    <path d="M0,0 L8,4 L0,8 Z" fill="#0f66c2"></path>
                  </marker>
                  <marker id="primitive-arrow" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto">
                    <path d="M0,0 L8,4 L0,8 Z" fill="#c45a12"></path>
                  </marker>
                </defs>
                <path class="diff-path" marker-end="url(#diff-arrow)" d="M260 62 C360 60, 425 88, 450 132"></path>
                <path class="diff-path" marker-end="url(#diff-arrow)" d="M450 168 C425 212, 360 240, 260 238"></path>
                <path class="diff-path" marker-end="url(#diff-arrow)" d="M260 238 C160 240, 95 212, 70 168"></path>
                <path class="diff-path" marker-end="url(#diff-arrow)" d="M70 132 C95 88, 160 60, 260 62"></path>
                <path class="primitive-path" marker-end="url(#primitive-arrow)" d="M260 82 C175 82, 118 104, 92 138"></path>
                <path class="primitive-path" marker-end="url(#primitive-arrow)" d="M92 162 C118 196, 175 218, 260 218"></path>
                <path class="primitive-path" marker-end="url(#primitive-arrow)" d="M260 218 C345 218, 402 196, 428 162"></path>
                <path class="primitive-path" marker-end="url(#primitive-arrow)" d="M428 138 C402 104, 345 82, 260 82"></path>
              </svg>
              <div class="cycle-node top-left">
                <div class="relation-label">start</div>
                <math xmlns="http://www.w3.org/1998/Math/MathML"><mi>sin</mi><mo>(</mo><mi>x</mi><mo>)</mo></math>
              </div>
              <div class="cycle-node top-right">
                <div class="relation-label">afgeleide</div>
                <math xmlns="http://www.w3.org/1998/Math/MathML"><mi>cos</mi><mo>(</mo><mi>x</mi><mo>)</mo></math>
              </div>
              <div class="cycle-node bottom-right">
                <div class="relation-label">nog eens</div>
                <math xmlns="http://www.w3.org/1998/Math/MathML"><mo>-</mo><mi>sin</mi><mo>(</mo><mi>x</mi><mo>)</mo></math>
              </div>
              <div class="cycle-node bottom-left">
                <div class="relation-label">nog eens</div>
                <math xmlns="http://www.w3.org/1998/Math/MathML"><mo>-</mo><mi>cos</mi><mo>(</mo><mi>x</mi><mo>)</mo></math>
              </div>
            </div>
            <div class="cycle-caption">Met differentieren volg je de blauwe cirkel. Met primitiveren ga je via de oranje pijlen terug.</div>
          </div>
        </div>
        """;
    }
    private static string GetStudentSubtopic(string subtopic)
    {
        return subtopic switch
        {
            "Differentiatie: basisregels" => "Basisregels: begin hiermee",
            "Differentiatie: combinatieregels" => "Combinatieregels: als formules door elkaar staan",
            "Differentiatie: standaardfuncties" => "Standaardfuncties: vaak uit je formulekaart leren",
            "Integralen: primitiveren" => "Primitiveren: terug van afgeleide naar functie",
            "Integralen: oppervlakte" => "Bepaalde integraal: oppervlakte tussen grenzen",
            "Integralen: lijn- en kringintegralen" => "Verdieping: lijn- en kringintegralen",
            _ => subtopic
        };
    }

    private string GetStudentTitle(FormulaCard card)
    {
        return card.Id switch
        {
            "derivative-power" => "Macht afleiden",
            "derivative-sum-rule" => "Plus of min afleiden",
            "derivative-constant-factor" => "Getal voor de formule",
            "derivative-product-rule" => "Keer afleiden",
            "derivative-quotient-rule" => "Breuk afleiden",
            "derivative-chain-rule" => "Formule in formule",
            "derivative-trig-basic" => "Sinus/cosinus afleiden",
            "derivative-exp-log" => "e en ln afleiden",
            "integral-power-rule" => "Macht integreren",
            "integral-sum-rule" => "Plus of min integreren",
            "integral-constant-factor" => "Getal voor de integraal",
            "integral-definite-area" => "Oppervlakte met grenzen",
            "circle-integral" => "Kringintegraal",
            _ => CardTitle(card)
        };
    }

    private static string GetStudentFormulaHtml(FormulaCard card)
    {
        return card.Id switch
        {
            "derivative-power" => FormulaWithCaption(
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><msup><mi>x</mi><mi>n</mi></msup><mo>&#x2192;</mo><mi>n</mi><msup><mi>x</mi><mrow><mi>n</mi><mo>-</mo><mn>1</mn></mrow></msup></mrow></math>""",
                "Bijvoorbeeld: x² wordt 2x"),
            "derivative-sum-rule" => FormulaWithCaption(
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>f</mi><mo>+</mo><mi>g</mi><mo>&#x2192;</mo><msup><mi>f</mi><mo>&#x2032;</mo></msup><mo>+</mo><msup><mi>g</mi><mo>&#x2032;</mo></msup></mrow></math>""",
                "Elk stukje apart afleiden"),
            "derivative-constant-factor" => FormulaWithCaption(
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mn>3</mn><mi>f</mi><mo>&#x2192;</mo><mn>3</mn><msup><mi>f</mi><mo>&#x2032;</mo></msup></mrow></math>""",
                "Het vaste getal blijft staan"),
            "derivative-product-rule" => FormulaWithCaption(
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>f</mi><mi>g</mi><mo>&#x2192;</mo><msup><mi>f</mi><mo>&#x2032;</mo></msup><mi>g</mi><mo>+</mo><mi>f</mi><msup><mi>g</mi><mo>&#x2032;</mo></msup></mrow></math>""",
                "Voor twee factoren die allebei kunnen veranderen"),
            "derivative-quotient-rule" => FormulaWithCaption(
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mfrac><mi>f</mi><mi>g</mi></mfrac><mo>&#x2192;</mo><mfrac><mrow><msup><mi>f</mi><mo>&#x2032;</mo></msup><mi>g</mi><mo>-</mo><mi>f</mi><msup><mi>g</mi><mo>&#x2032;</mo></msup></mrow><msup><mi>g</mi><mn>2</mn></msup></mfrac></mrow></math>""",
                "Voor een breuk met functies"),
            "derivative-chain-rule" => FormulaWithCaption(
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>f</mi><mo>(</mo><mi>g</mi><mo>(</mo><mi>x</mi><mo>)</mo><mo>)</mo><mo>&#x2192;</mo><msup><mi>f</mi><mo>&#x2032;</mo></msup><mo>(</mo><mi>g</mi><mo>(</mo><mi>x</mi><mo>)</mo><mo>)</mo><mo>&#x00B7;</mo><msup><mi>g</mi><mo>&#x2032;</mo></msup><mo>(</mo><mi>x</mi><mo>)</mo></mrow></math>""",
                "Eerst buitenkant, daarna binnenkant"),
            "derivative-trig-basic" => FormulaWithCaption(
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>sin</mi><mo>(</mo><mi>x</mi><mo>)</mo><mo>&#x2192;</mo><mi>cos</mi><mo>(</mo><mi>x</mi><mo>)</mo><mo>,</mo><mi>cos</mi><mo>(</mo><mi>x</mi><mo>)</mo><mo>&#x2192;</mo><mo>-</mo><mi>sin</mi><mo>(</mo><mi>x</mi><mo>)</mo></mrow></math>""",
                "Voor golven en periodieke grafieken"),
            "derivative-exp-log" => FormulaWithCaption(
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><msup><mi>e</mi><mi>x</mi></msup><mo>&#x2192;</mo><msup><mi>e</mi><mi>x</mi></msup><mo>,</mo><mi>ln</mi><mo>(</mo><mi>x</mi><mo>)</mo><mo>&#x2192;</mo><mfrac><mn>1</mn><mi>x</mi></mfrac></mrow></math>""",
                "Voor groei en logaritmen"),
            "integral-power-rule" => FormulaWithCaption(
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><msup><mi>x</mi><mi>n</mi></msup><mo>&#x2192;</mo><mfrac><msup><mi>x</mi><mrow><mi>n</mi><mo>+</mo><mn>1</mn></mrow></msup><mrow><mi>n</mi><mo>+</mo><mn>1</mn></mrow></mfrac><mo>+</mo><mi>C</mi></mrow></math>""",
                "Bijvoorbeeld: x² wordt x³/3 + C"),
            "integral-sum-rule" => FormulaWithCaption(
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>f</mi><mo>+</mo><mi>g</mi><mo>&#x2192;</mo><mo>&#x222B;</mo><mi>f</mi><mi>d</mi><mi>x</mi><mo>+</mo><mo>&#x222B;</mo><mi>g</mi><mi>d</mi><mi>x</mi></mrow></math>""",
                "Elk stukje apart primitiveren"),
            "integral-constant-factor" => FormulaWithCaption(
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>c</mi><mi>f</mi><mo>&#x2192;</mo><mi>c</mi><mo>&#x222B;</mo><mi>f</mi><mi>d</mi><mi>x</mi></mrow></math>""",
                "Het vaste getal blijft staan"),
            "integral-definite-area" => FormulaWithCaption(
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><msubsup><mo>&#x222B;</mo><mi>a</mi><mi>b</mi></msubsup><mi>f</mi><mo>(</mo><mi>x</mi><mo>)</mo><mi>d</mi><mi>x</mi></mrow></math>""",
                "Oppervlakte tussen a en b"),
            "circle-integral" => FormulaWithCaption(
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mo>&oint;</mo><mi>F</mi><mo>&#x22C5;</mo><mi>d</mi><mi>r</mi></mrow></math>""",
                "Integraal rond een gesloten pad"),
            "statistics-median" => FormulaWithCaption(
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><mi>Me</mi><mo>=</mo><mtext>middelste waarde</mtext></mrow></math>""",
                "Na sorteren; bij even aantal het gemiddelde van twee middelste waarden"),
            _ => $"""<div>{card.MathMl}</div>"""
        };
    }

    private static string FormulaWithCaption(string mathMl, string caption)
    {
        return $"""<div>{mathMl}</div><div class="formula-caption">{Html(caption)}</div>""";
    }

    private string GetStudentUse(FormulaCard card)
    {
        return card.Id switch
        {
            "derivative-power" => "Bij x², x³, wortels en machten.",
            "derivative-sum-rule" => "Als er plus of min tussen termen staat.",
            "derivative-constant-factor" => "Als er een vast getal voor staat, zoals 5x².",
            "derivative-product-rule" => "Als twee formules met × vermenigvuldigd worden.",
            "derivative-quotient-rule" => "Als je een breuk met x hebt.",
            "derivative-chain-rule" => "Als er haakjes of een binnenfunctie zijn, zoals sin(x²).",
            "derivative-trig-basic" => "Bij sinus- en cosinusgrafieken.",
            "derivative-exp-log" => "Bij exponentiele groei en logaritmen.",
            "integral-power-rule" => "Bij oppervlaktes en primitiveren van machten.",
            "integral-sum-rule" => "Als er plus of min tussen termen staat.",
            "integral-constant-factor" => "Als er een vast getal voor staat, zoals 4x².",
            "integral-definite-area" => "Als je oppervlakte tussen twee grenzen zoekt.",
            "circle-integral" => "Voor PWS, Wiskunde D of propedeuse; niet basis.",
            _ => CardDescription(card)
        };
    }

    private string CardTitle(FormulaCard card)
    {
        return T($"formula_card.card.{card.Id}.title", card.Title);
    }

    private string CardDescription(FormulaCard card)
    {
        return T($"formula_card.card.{card.Id}.description", card.Description);
    }

    private string CardFormulaText(FormulaCard card)
    {
        return T($"formula_card.card.{card.Id}.formula_text", card.Formula);
    }

    private string CardPlainText(FormulaCard card)
    {
        return T($"formula_card.card.{card.Id}.plain_text", card.PlainText);
    }

    private string CardExampleNod(FormulaCard card)
    {
        return T($"formula_card.card.{card.Id}.example_nod", card.ExampleNod);
    }

    private IReadOnlyList<string> CardTags(FormulaCard card)
    {
        var value = T($"formula_card.card.{card.Id}.tags", string.Join("|", card.LevelTags));
        return value
            .Split(['|', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToArray();
    }

    private string Vector3DProjectionNote()
    {
        return T(
            "formula_card.vector3d_projection_note",
            "Graph 3D shows the full XYZ arrow. Graph 2D uses (x/z,y/z), and falls back to (x,y) when z = 0.");
    }

    private static string GetSubtopic(FormulaCard card)
    {
        return card.Id switch
        {
            "derivative-power" or "derivative-sum-rule" or "derivative-constant-factor" => "Differentiatie: basisregels",
            "derivative-product-rule" or "derivative-quotient-rule" or "derivative-chain-rule" => "Differentiatie: combinatieregels",
            "derivative-trig-basic" or "derivative-exp-log" => "Differentiatie: standaardfuncties",
            "integral-power-rule" or "integral-sum-rule" or "integral-constant-factor" => "Integralen: primitiveren",
            "integral-definite-area" => "Integralen: oppervlakte",
            "circle-integral" => "Integralen: lijn- en kringintegralen",
            "trig-right-triangle" or "sine-rule" or "cosine-rule" or "special-right-triangles" => "Goniometrie: driehoeken",
            "trig-identities" or "exact-trig-values" => "Goniometrie: identiteiten en exacte waarden",
            "pythagoras" or "triangle-area" => "Meetkunde: basisfiguren",
            "linear-function" or "circle-equation" or "point-line-distance" or "distance-between-points" or "midpoint" => "Meetkunde: coordinaten",
            "vector-2d-arrow" or "vector-length-3d" => "Vectoren: lengte en richting",
            "vector-dot-angle" => "Vectoren: inproduct en hoek",
            "vector-cross-z" => "Vectoren: kruisproduct",
            "matrix-2x2-determinant" => "Lineaire algebra: 2x2 matrices",
            "statistics-mean" or "statistics-median" => "Statistiek: centrummaat",
            "statistics-stdev-population" => "Statistiek: spreiding",
            "probability-combinations" => "Kansrekening: tellen",
            "probability-expected-value" => "Kansrekening: verwachting",
            "quadratic-formula" or "exponential-growth" => "Algebra: vergelijkingen en functies",
            _ => $"{GetMenuTopic(card)}: algemeen"
        };
    }

    private static int GetSubtopicOrder(string subtopic)
    {
        return subtopic switch
        {
            "Differentiatie: basisregels" => 0,
            "Differentiatie: combinatieregels" => 1,
            "Differentiatie: standaardfuncties" => 2,
            "Integralen: primitiveren" => 3,
            "Integralen: oppervlakte" => 4,
            "Integralen: lijn- en kringintegralen" => 5,
            "Algebra: vergelijkingen en functies" => 0,
            "Goniometrie: driehoeken" => 0,
            "Goniometrie: identiteiten en exacte waarden" => 1,
            "Meetkunde: basisfiguren" => 0,
            "Meetkunde: coordinaten" => 0,
            "Vectoren: lengte en richting" => 0,
            "Vectoren: inproduct en hoek" => 1,
            "Vectoren: kruisproduct" => 2,
            "Lineaire algebra: 2x2 matrices" => 0,
            "Statistiek: centrummaat" => 0,
            "Statistiek: spreiding" => 1,
            "Kansrekening: tellen" => 0,
            "Kansrekening: verwachting" => 1,
            var value when value.EndsWith(": algemeen", StringComparison.OrdinalIgnoreCase) => 10,
            _ => 20
        };
    }

    private static int GetCardOrder(FormulaCard card)
    {
        return card.Id switch
        {
            "derivative-power" => 0,
            "derivative-sum-rule" => 1,
            "derivative-constant-factor" => 2,
            "derivative-product-rule" => 3,
            "derivative-quotient-rule" => 4,
            "derivative-chain-rule" => 5,
            "derivative-trig-basic" => 6,
            "derivative-exp-log" => 7,
            "integral-power-rule" => 8,
            "integral-sum-rule" => 9,
            "integral-constant-factor" => 10,
            "integral-definite-area" => 11,
            "circle-integral" => 12,
            _ => 50
        };
    }

    private static string GetSubtopicTitle(string subtopic)
    {
        var separator = subtopic.IndexOf(':');
        return separator < 0 ? subtopic : subtopic[(separator + 1)..].Trim();
    }

    private string BuildNodMathHtml(FormulaCard card)
    {
        var (rule, note) = card.Id switch
        {
            "pythagoras" => (
                "math sqrt(ans(x)^2 + ans(y)^2)",
                "2D/multi-input notatie voor NOD 2.1. In oude NOD blijft ans de huidige waarde."),
            "quadratic-formula" => (
                "math (-ans(b) + sqrt(ans(b)^2 - 4*ans(a)*ans(c))) / (2*ans(a))",
                "Geeft de plus-oplossing. Voor de min-oplossing gebruik je -sqrt(...)."),
            "exponential-growth" => (
                "math ans(N0) * ans(g)^ans(t)",
                "Named inputs zijn bedoeld voor NOD 2.1, zodat meer waarden duidelijk blijven."),
            "statistics-mean" => (
                "math mean(2,4,4,4,5,5,7,9)",
                "Gemiddelde van een dataset. Dit is Wiskunde A: centrummaat."),
            "statistics-median" => (
                "math median(2,4,4,4,5,5,7,9)",
                "Mediaan na sorteren. Bij een even aantal waarden neemt NOD het gemiddelde van de twee middelste waarden."),
            "statistics-stdev-population" => (
                "math stdev(2,4,4,4,5,5,7,9)\r\nmath samplestdev(2,4,4,4,5,5,7,9)",
                "stdev gebruikt delen door n. samplestdev gebruikt delen door n-1 voor een steekproef."),
            "probability-combinations" => (
                "math comb(ans,2)\r\nmath ncr(ans,2)",
                "Combinaties: volgorde telt niet mee. ncr is dezelfde schrijfwijze."),
            "probability-expected-value" => (
                "math expected(0,0.5,10,0.5)",
                "Verwachtingswaarde leest paren: waarde, kans, waarde, kans."),
            "trig-right-triangle" => (
                "math sind(ans(theta))",
                "Voor graden gebruik je sind/cosd/tand. Voor radialen gebruik je sin/cos/tan."),
            "linear-function" => (
                "math ans(a) * ans(x) + ans(b)",
                "Lineaire formule met named inputs voor helling a, waarde x en startwaarde b."),
            "distance-between-points" => (
                "math distance(vec(1,2), vec(4,6))",
                "Afstand tussen punten kun je zien als lengte van het verschil tussen twee vectoren."),
            "midpoint" => (
                "math (1 + 5) / 2\r\nmath (2 + 8) / 2",
                "NOD geeft hier de x- en y-coordinaat als twee losse getallen; een vectorresultaat is geen eindwaarde."),
            "triangle-area" => (
                "math 0.5 * ans * 6",
                "Bij input basis 8 en vaste hoogte 6 geeft dit oppervlakte 24."),
            "sine-rule" => (
                "math ans(a) * sind(ans(B)) / sind(ans(A))",
                "Voorbeeld: bereken zijde b uit a, A en B door de sinusregel om te vormen."),
            "cosine-rule" => (
                "math sqrt(ans(a)^2 + ans(b)^2 - 2*ans(a)*ans(b)*cosd(ans(C)))",
                "Voorbeeld met graden: berekent zijde c met de cosinusregel."),
            "trig-identities" => (
                "math sind(ans)^2 + cosd(ans)^2",
                "Controleert numeriek de identiteit sin^2(x)+cos^2(x)=1 bij hoek ans in graden."),
            "exact-trig-values" => (
                "math sind(30)",
                "Voor exacte waarden toont de formulekaart de tabel; NOD-math kan een waarde numeriek testen."),
            "circle-equation" => (
                "math (ans(x) - ans(a))^2 + (ans(y) - ans(b))^2",
                "Vergelijk met r^2 om te controleren of een punt op de cirkel ligt."),
            "special-right-triangles" => (
                "math sqrt(3)",
                "Tabelkaart voor verhoudingen; NOD-math kan de wortelwaarden numeriek tonen."),
            "kinetic-energy" => (
                "math 0.5 * ans(m) * ans(v)^2",
                "Natuurkunde-kaart met named inputs voor massa en snelheid."),
            "derivative-power" => (
                "math diff ans^2",
                "Dit werkt als numerieke afgeleide bij de huidige inputwaarde."),
            "derivative-sum-rule" => (
                "math diff (ans^2 + ans)",
                "Voorbeeld van somregel. NOD berekent numeriek; de formulekaart toont de symbolische regel."),
            "derivative-constant-factor" => (
                "math diff 3*ans^2",
                "Voorbeeld van constante factorregel. Het getal voor de functie blijft in de symbolische regel staan."),
            "derivative-product-rule" => (
                "math diff (ans^2 * sin(ans))",
                "Voorbeeld van productregel. De formulekaart legt de symbolische regel uit; de rekenregel is numeriek."),
            "derivative-quotient-rule" => (
                "math diff (ans^2 / (ans + 1))",
                "Voorbeeld van quotientregel. Test altijd numeriek met F6."),
            "derivative-chain-rule" => (
                "math diff sin(ans^2)",
                "Voorbeeld van kettingregel: buitenfunctie en binnenfunctie."),
            "derivative-trig-basic" => (
                "math diff sin(ans)",
                "NOD rekent numeriek; de kaart toont dat sin(x) als afgeleide cos(x) heeft."),
            "derivative-exp-log" => (
                "math diff e^ans",
                "Voor exponentiele groei. Voor ln(x) kun je math diff ln(ans) testen."),
            "integral-power-rule" => (
                "math integral 0,1 ans^2",
                "De formulekaart toont de symbolische primitieve; de rekenregel gebruikt een numerieke integraal."),
            "integral-sum-rule" => (
                "math integral 0,1 (ans^2 + ans)",
                "Numerieke integraal van een som. Symbolisch splits je de delen apart."),
            "integral-constant-factor" => (
                "math integral 0,1 3*ans^2",
                "Het vaste getal blijft buiten de primitieve-regel staan."),
            "integral-definite-area" => (
                "math integral 0,1 ans^2",
                "Bepaalde integraal als numerieke oppervlakte tussen twee grenzen."),
            "vector-2d-arrow" => (
                "math vec 3 4\r\nmath length(vec(3,4))\r\nmath dot(vec(1,2), vec(3,4))\r\nmath angled(vec(1,0), vec(0,1))",
                "Gebruik math vec 3 4 of length(vec(3,4)) voor vectorlengte. dot en angled zijn verwante vectorbewerkingen."),
            "vector-length-3d" => (
                "mode geometry\r\ninput x X component\r\ninput y Y component\r\ninput z Z component\r\nmath length(vec(x,y,z))\r\nmath vec (3,4,12)",
                Vector3DProjectionNote()),
            "vector-dot-angle" => (
                "math dot(vec(1,2), vec(3,4))\r\nmath angled(vec(1,0), vec(0,1))",
                "dot geeft het inproduct. angled geeft de hoek in graden."),
            "vector-cross-z" => (
                "math z(cross(vec(1,0,0), vec(0,1,0)))",
                "cross geeft een vector; met z(...) kies je de z-component als eindgetal."),
            "point-line-distance" => (
                "math |ans(a)*ans(xp) + ans(b)*ans(yp) - ans(c)| / sqrt(ans(a)^2 + ans(b)^2)",
                "2D analytische meetkunde met named inputs."),
            "matrix-2x2-determinant" => (
                "math det(mat2(1,2,3,4))\r\nmath det2(1,2,3,4)\r\nmath mget(mat2(1,2,3,4), 2, 1)\r\nmath x(mat2(1,2,3,4) * vec(5,6))",
                "det en det2 geven de determinant. mget leest een cel. Matrix maal vector kan met x(...) of y(...) naar een component worden omgezet."),
            "circle-integral" => (
                "math line-integral(F, path)",
                "Conceptregel voor later. Kringintegraal blijft hier een uitlegkaart, geen runtime-engine."),
            _ => ExtractFirstMathLine(CardExampleNod(card))
        };

        return $$"""
          <section class="section">
            <h2>NOD-math</h2>
            <pre>{{Html(rule)}}</pre>
            <p class="math-note">{{Html(note)}}</p>
          </section>
        """;
    }

    private static (string Rule, string Note) ExtractFirstMathLine(string nod)
    {
        foreach (var line in nod.Split(["\r\n", "\n"], StringSplitOptions.None))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("math ", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("equation ", StringComparison.OrdinalIgnoreCase))
                return (trimmed, "Math-regel uit de voorbeeld-NOD.");
        }

        return ("math ans", "Deze kaart is vooral bedoeld als formule/uitlegkaart.");
    }

    private static string BuildVisualHtml(FormulaCard card)
    {
        if (card.Id.Equals("quadratic-formula", StringComparison.OrdinalIgnoreCase))
            return """
              <section class="section">
                <h2>Oplosmethoden kwadratische vergelijkingen</h2>
                <div class="method-board" role="img" aria-label="Samenvatting van oplosmethoden voor kwadratische vergelijkingen">
                  <div class="method-board-header">
                    <span class="summary-stamp">Samenvatting</span>
                    <span class="method-board-title">Oplosroutes</span>
                  </div>
                  <div style="text-align:center">
                    <div class="standard-form">ax² + bx + c = 0</div>
                  </div>
                  <div class="method-grid">
                    <div class="method-card">
                      <span class="method-label">b = 0</span>
                      <div class="method-chip">herleid tot x² = getal</div>
                      <div class="method-steps">
                        <div>-4x² + 12 = 0</div>
                        <div>-4x² = -12</div>
                        <div>x² = 3</div>
                        <div>x = sqrt(3) of x = -sqrt(3)</div>
                      </div>
                      <div class="method-note">Gebruik dit als de x-term ontbreekt.</div>
                    </div>
                    <div class="method-card">
                      <span class="method-label brown">c = 0</span>
                      <div class="method-chip">x buiten haakjes halen</div>
                      <div class="method-steps">
                        <div>2x² + 10x = 0</div>
                        <div>x(2x + 10) = 0</div>
                        <div>x = 0 of 2x + 10 = 0</div>
                        <div>x = 0 of x = -5</div>
                      </div>
                      <div class="method-note">Gebruik dit als de losse term ontbreekt.</div>
                    </div>
                    <div class="method-card wide">
                      <span class="method-label red">a, b, c ≠ 0</span>
                      <div class="method-row">
                        <div>
                          <div class="method-chip">product-som-methode</div>
                          <div class="method-steps">
                            <div>x² + 2x - 8 = 0</div>
                            <div>(x - 2)(x + 4) = 0</div>
                            <div>x = 2 of x = -4</div>
                          </div>
                        </div>
                        <div>
                          <div class="method-chip">abc-formule</div>
                          <div class="method-steps">
                            <div>D = b^2 - 4ac</div>
                            <div>x = (-b ± sqrt(D)) / 2a</div>
                            <div>werkt altijd als D ≥ 0</div>
                          </div>
                        </div>
                        <div>
                          <div class="method-chip">kwadraat afsplitsen</div>
                          <div class="method-steps">
                            <div>x² - 6x + 7 = 0</div>
                            <div>(x - 3)^2 - 9 + 7 = 0</div>
                            <div>(x - 3)^2 = 2</div>
                          </div>
                        </div>
                      </div>
                      <div class="method-note">Bij drie termen kies je de methode die het snelst inzicht geeft.</div>
                    </div>
                  </div>
                </div>
              </section>
            """;

        if (!card.Id.Equals("vector-2d-arrow", StringComparison.OrdinalIgnoreCase))
            return "";

        return """
          <section class="section">
            <h2>Graph arrow</h2>
            <svg class="vector-frame" viewBox="0 0 420 260" role="img" aria-label="2D vector arrow from origin to point x y">
              <defs>
                <pattern id="grid-vector-2d" width="20" height="20" patternUnits="userSpaceOnUse">
                  <path d="M 20 0 L 0 0 0 20" fill="none" class="grid"/>
                </pattern>
                <marker id="arrow-vector-2d" markerWidth="10" markerHeight="10" refX="8" refY="3" orient="auto" markerUnits="strokeWidth">
                  <path d="M0,0 L0,6 L9,3 z" fill="#2563eb"/>
                </marker>
              </defs>
              <rect width="420" height="260" fill="url(#grid-vector-2d)"/>
              <line x1="30" y1="220" x2="390" y2="220" class="axis"/>
              <line x1="70" y1="240" x2="70" y2="25" class="axis"/>
              <text x="382" y="238" class="graph-label">x</text>
              <text x="48" y="35" class="graph-label">y</text>
              <line x1="70" y1="220" x2="270" y2="220" class="helper"/>
              <line x1="270" y1="220" x2="270" y2="90" class="helper"/>
              <line x1="70" y1="220" x2="270" y2="90" class="vector" marker-end="url(#arrow-vector-2d)"/>
              <circle cx="70" cy="220" r="4" class="point"/>
              <circle cx="270" cy="90" r="5" class="point"/>
              <text x="82" y="214" class="graph-label">(0,0)</text>
              <text x="282" y="86" class="graph-label">(x,y)</text>
              <text x="178" y="137" class="graph-label">v</text>
            </svg>
          </section>
        """;
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
