using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using NodSystem.Core;

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
        "2D"
    };

    private readonly TreeView _cardsTree;
    private readonly WebView2 _browser;
    private readonly Label _countLabel;
    private readonly IReadOnlyList<FormulaCard> _cards;
    private readonly LanguageCatalog _language;
    private readonly bool _enableFormulaFilmExperiment;
    private bool _browserFailed;
    private string? _pendingHtml;

    public FormulaCardForm(LanguageCatalog language, bool enableFormulaFilmExperiment = false)
    {
        _language = language;
        _enableFormulaFilmExperiment = enableFormulaFilmExperiment;
        _cards = FormulaCardCatalog.GetDefaultCards();

        Text = T("formula_card.title", "Formulekaart");
        Width = 900;
        Height = 620;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(245, 247, 250);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(12),
            BackColor = Color.FromArgb(245, 247, 250)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        _countLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"{T("formula_card.count", "Formulekaarten")} ({_cards.Count})",
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 45, 65),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var detailsHeader = new Label
        {
            Dock = DockStyle.Fill,
            Text = T("formula_card.details_header", "Details, export en voorbeeld-NOD"),
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 45, 65),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _cardsTree = new TreeView
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.White,
            Font = new Font(Font.FontFamily, 9.2f),
            HideSelection = false,
            ShowLines = true,
            ShowPlusMinus = true,
            ShowRootLines = true
        };
        BuildCardTree();
        _cardsTree.AfterSelect += (_, _) => ShowSelectedCard();

        _browser = new WebView2
        {
            Dock = DockStyle.Fill,
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

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.FromArgb(245, 247, 250)
        };
        buttons.Controls.Add(CreateButton(T("dialog.close", "Sluiten"), (_, _) => Close()));
        buttons.Controls.Add(CreateButton(T("formula_card.copy_mathml", "Kopieer MathML"), (_, _) => CopySelected(card => card.MathMl)));
        buttons.Controls.Add(CreateButton(T("formula_card.copy_latex", "Kopieer LaTeX"), (_, _) => CopySelected(card => card.Latex)));
        buttons.Controls.Add(CreateButton(T("formula_card.copy_nod", "Kopieer NOD"), (_, _) => CopySelected(card => card.ExampleNod)));

        root.Controls.Add(_countLabel, 0, 0);
        root.Controls.Add(detailsHeader, 1, 0);
        root.Controls.Add(_cardsTree, 0, 1);
        root.Controls.Add(_browser, 1, 1);
        root.Controls.Add(buttons, 0, 2);
        root.SetColumnSpan(buttons, 2);

        Controls.Add(root);
        Load += async (_, _) => await InitializeBrowserAsync();

        SelectFirstCardNode();
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
            experimentNode.Nodes.Add(new TreeNode(T("formula_card.formula_film", "Formulefilm")) { Tag = "formula-film" });
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
            Text = text,
            Margin = new Padding(6, 8, 0, 0)
        };
        button.Click += click;
        return button;
    }

    private string T(string key, string fallback) => _language.Text(key, fallback);

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
        return $$"""
        <!doctype html>
        <html>
        <head>
        <meta charset="utf-8">
        <style>
        :root { color-scheme: light; font-family: "Segoe UI", Arial, sans-serif; background: #f5f7fa; color: #1e2d41; }
        body { margin: 0; padding: 20px; background: #f5f7fa; }
        .page { max-width: 760px; margin: 0 auto; }
        h1 { margin: 0 0 8px; font-size: 30px; font-weight: 650; color: #10233f; }
        h2 { margin: 22px 0 10px; font-size: 18px; color: #0f3f8f; }
        h3 { margin: 0 0 10px; color: #0f3f8f; }
        p { line-height: 1.55; color: #43546a; }
        code { background: #eef4ff; border: 1px solid #d7e3f7; border-radius: 5px; padding: 1px 5px; color: #0f3f8f; }
        table { width: 100%; border-collapse: separate; border-spacing: 0; border: 1px solid #d9e2ec; border-radius: 8px; overflow: hidden; background: #fff; }
        th, td { padding: 8px 10px; border-bottom: 1px solid #e5edf5; text-align: left; vertical-align: top; font-size: 13px; }
        tr:last-child td { border-bottom: 0; }
        th { background: #eaf2ff; color: #0f3f8f; font-weight: 700; }
        .subtitle { color: #5d6c7e; margin-bottom: 18px; font-size: 14px; }
        .notice { background:#fff8e6; border:1px solid #f4d184; border-left:4px solid #d69400; border-radius:8px; padding:10px 12px; margin:10px 0 14px; color:#3f2f12; }
        .generator-card { border:1px solid #cbdcf2; border-radius:10px; background:#ffffff; padding:14px; margin:14px 0 18px; box-shadow:0 6px 18px rgba(15,23,42,.07); }
        .generator-toolbar { display:flex; flex-wrap:wrap; gap:8px; margin:10px 0 12px; }
        .generator-toolbar button { border:1px solid #b9d2f2; background:#eef6ff; color:#0f3f8f; border-radius:7px; padding:7px 11px; font-weight:700; cursor:pointer; }
        .generator-toolbar button:hover { background:#dfeeff; }
        .film-stage { position:relative; height:190px; border:1px solid #d8e5f5; border-radius:9px; background:linear-gradient(180deg,#f8fbff,#eef6ff); overflow:hidden; margin:10px 0 8px; }
        .actor { position:absolute; left:var(--x); top:var(--y); color:#004aad; font-family:"Cambria Math","STIX Two Math","Times New Roman",serif; font-size:30px; font-weight:700; opacity:0; transform:translate(0,0) scale(1); transform-origin:center bottom; transition:none; }
        .actor.small { font-size:15px; line-height:1; }
        .actor.red { color:#dc2626; }
        .actor.gray { color:#9aa9bd; }
        .actor.ink { color:#111827; }
        .actor.memory.show { opacity:0; }
        .actor.memory.visible.show { opacity:1; }
        .actor.soft { color:#93c5fd; filter:blur(.2px); }
        .actor.ghost-power { color:#b9d7ff; font-family:"Cambria Math","STIX Two Math","Times New Roman",serif; font-size:16px; font-weight:700; filter:blur(.15px); }
        .actor.note { color:#475569; font-family:"Segoe UI",Arial,sans-serif; font-size:13px; font-weight:650; }
        .actor.formula-copy { color:#004aad; font-family:"Cambria Math","STIX Two Math","Times New Roman",serif; font-size:18px; font-weight:700; }
        .actor.helper { font-size:15px; color:#5b7aa8; opacity:.78; }
        .actor.show { opacity:1; transition:opacity .35s ease, transform 1.25s ease, left 1.25s ease, top 1.25s ease, font-size 1.25s ease, color .45s ease; }
        .actor.fade { opacity:0; transition:opacity .42s ease, transform .42s ease; }
        .actor.pop { transform:translate(0,-8px) scale(.65); opacity:0; transition:opacity .55s ease, transform .55s ease; }
        .film-caption { color:#43546a; font-size:13px; min-height:22px; }
        .film-code { background:#101827; color:#e5eefc; border-radius:8px; padding:10px 12px; margin:10px 0 0; font-family:Consolas,"Cascadia Mono",monospace; white-space:pre-wrap; }
        </style>
        </head>
        <body>
        <main class="page">
          <h1>{{Html(T("formula_card.formula_film_title", "Formulefilm 2.1 experiment"))}}</h1>
          <div class="subtitle">{{Html(T("formula_card.formula_film_subtitle", "Onderzoek voor bewegende formulekaarten en onderwijsfilms"))}}</div>
          <div class="notice"><b>Status:</b> dit is geen gewone NOD 2.0 beta-functie. Het hoort bij Syscalculator 2.1 onderzoek: een formule-filmgenerator waarin getallen, machten, haakjes en symbolen als losse acteurs bewegen.</div>
          <p>De huidige Solver blijft nuttig voor eenvoudige stap-uitleg, maar kettingregel, productregel, quotientregel en integralen vragen een nieuwe technische animatie-API. Daarom staat dit onderwerp bij de formulekaart als demo en onderzoekspunt.</p>
          <div class="generator-card">
            <h3>Interactieve formulefilm-preview</h3>
            <p>Dit is nog geen echte AI-service. Het laat wel de technische richting zien: AI of regels leveren straks een scenescript, en de formulekaart speelt dat script als acteurs af.</p>
            <div class="generator-toolbar">
              <button type="button" onclick="playPower(2)">Genereer x^2</button>
              <button type="button" onclick="playPower(3)">Genereer x^3</button>
              <button type="button" onclick="playChainPreview()">Kettingregel preview</button>
            </div>
            <div id="filmStage" class="film-stage" aria-label="Formulefilm preview"></div>
            <div id="filmCaption" class="film-caption">Klik op een voorbeeld om de formulefilm lokaal te genereren.</div>
            <div id="filmCode" class="film-code">FormulaFilm.Generate("x^2")</div>
          </div>
          <h2>Onderzoeksvragen</h2>
          <table>
            <tr><th>Onderdeel</th><th>Waarom moeilijk?</th><th>2.1-richting</th></tr>
            <tr><td>Machtsregel</td><td>Exponent, coefficient en variabele moeten apart blijven bewegen.</td><td>Formule opsplitsen in acteurs.</td></tr>
            <tr><td>Somregel</td><td>Meerdere termen moeten tegelijk maar onafhankelijk werken.</td><td>Groepen per term.</td></tr>
            <tr><td>Kettingregel</td><td>Er is een buitenfunctie en binnenfunctie.</td><td>Geneste acteurs/groepen.</td></tr>
            <tr><td>Integralen</td><td>Primitiveren is terugrekenen en vaak gekoppeld aan differentiatie.</td><td>Animatie met heen- en terugrichting.</td></tr>
            <tr><td>Export</td><td>Live berekenen is zwaar en niet altijd nodig.</td><td>NOD + filmkaart/HTML/SVG/MP4.</td></tr>
          </table>
        </main>
        <script>
        const stage = document.getElementById('filmStage');
        const caption = document.getElementById('filmCaption');
        const code = document.getElementById('filmCode');
        let runId = 0;

        function clearStage() {
          runId++;
          stage.innerHTML = '';
          return runId;
        }

        function actor(text, x, y, cls = '') {
          const el = document.createElement('span');
          el.className = 'actor ' + cls;
          el.textContent = text;
          el.style.setProperty('--x', x + 'px');
          el.style.setProperty('--y', y + 'px');
          stage.appendChild(el);
          requestAnimationFrame(() => el.classList.add('show'));
          return el;
        }

        function move(el, x, y, scale = 1) {
          el.style.left = x + 'px';
          el.style.top = y + 'px';
          el.style.transform = 'scale(' + scale + ')';
        }

        function text(el, value) {
          el.textContent = value;
        }

        function setCaption(value) {
          caption.textContent = value;
        }

        function wait(ms, id) {
          return new Promise(resolve => setTimeout(() => resolve(id === runId), ms));
        }

        function showPowerStart(n) {
          clearStage();
          code.textContent = 'FormulaFilm.Generate("diff x^' + n + '")\\nklik op Genereer om de stappen rustig af te spelen';
          setCaption('Beginbeeld: eerst goed kijken naar de functie f(x) = x^' + n + '.');
          actor('f', 24, 58);
          actor('(', 44, 58);
          actor('x', 57, 58);
          actor(')', 72, 58);
          actor('=', 96, 58);
          actor('x', 134, 58);
          actor(String(n), 151, 47, 'small red');
        }

        async function playPower(n) {
          const id = clearStage();
          const next = n - 1;
          code.textContent = 'FormulaFilm.Generate("diff x^' + n + '")\\nactors: f, x, exponent ' + n + ', coefficient ' + n + ', exponent ' + next;
          setCaption('Beginbeeld: eerst goed kijken naar de functie f(x) = x^' + n + '.');

          const f = actor('f', 24, 58);
          const open = actor('(', 44, 58);
          const arg = actor('x', 57, 58);
          const close = actor(')', 72, 58);
          const eq = actor('=', 96, 58);
          const x = actor('x', 134, 58);
          const oldExp = actor(String(n), 151, 47, 'small gray memory');
          const exp = actor(String(n), 151, 47, 'small red');
          if (!await wait(1900, id)) return;

          setCaption('De macht ' + n + ' beweegt naar voren en wordt coefficient.');
          const prime = actor("'", 36, 47, 'red');
          if (!await wait(420, id)) return;
          prime.classList.remove('red');
          oldExp.classList.add('visible');
          exp.classList.remove('small');
          move(exp, 126, 58, 1);
          move(x, 158, 58, 1);
          move(oldExp, 175, 47, 1);
          if (!await wait(1500, id)) return;

          setCaption('De oude macht wordt kleiner: ' + n + ' - 1 = ' + next + '.');
          oldExp.classList.remove('gray');
          oldExp.classList.add('red');
          text(oldExp, n + ' - 1');
          if (!await wait(900, id)) return;
          text(oldExp, String(next));
          if (!await wait(850, id)) return;

          if (next === 1) {
            setCaption('x^1 is gewoon x. De 1 verdwijnt, maar x blijft meedoen.');
            oldExp.classList.add('pop');
            move(exp, 126, 58, 1);
            move(x, 158, 58, 1);
            if (!await wait(850, id)) return;
            setCaption("Eindbeeld: f'(x) = " + n + 'x');
          } else {
            setCaption('De macht blijft zichtbaar: dit wordt ' + n + 'x^' + next + '.');
            move(oldExp, 170, 47, 1);
            move(x, 154, 58, 1);
            if (!await wait(850, id)) return;
            setCaption("Eindbeeld: f'(x) = " + n + 'x^' + next);
          }
        }

        async function playChainPreview() {
          const id = clearStage();
          code.textContent = 'FormulaFilm.Generate("diff (x^2 - 1)^3")\\nactors: outer exponent, inner group, constant, inner derivative';
          setCaption('Kettingregel is 2.1 onderzoek: buitenfunctie en binnenfunctie worden aparte groepen.');
          actor('f', 20, 58);
          actor('(x)', 50, 58);
          actor('=', 100, 58);
          const openBlock = actor('(', 136, 58);
          const blockX = actor('x', 154, 58);
          const blockInnerExp = actor('2', 169, 47, 'small');
          const blockMinus = actor('- 1', 184, 58);
          const closeBlock = actor(')', 224, 58);
          const outerOldExp = actor('3', 242, 47, 'small gray memory');
          const exp = actor('3', 242, 47, 'small red');
          if (!await wait(1900, id)) return;
          const prime = actor("'", 32, 47, 'red');
          setCaption("We zoeken de afgeleide: f(x) wordt f'(x). Daarna gaat de buitenmacht 3 naar voren.");
          if (!await wait(650, id)) return;
          prime.classList.remove('red');
          setCaption('Buitenmacht 3 gaat eerst bovenlangs naar voren; zo blijft hij de buitenmacht.');
          outerOldExp.classList.add('visible');
          exp.classList.remove('small');
          move(exp, 198, 30, .78);
          if (!await wait(760, id)) return;
          setCaption('Nu wordt de buitenmacht de coefficient voor het hele binnenblok.');
          move(exp, 130, 60, .82);
          move(openBlock, 154, 58, 1);
          move(blockX, 172, 58, 1);
          move(blockInnerExp, 187, 47, 1);
          move(blockMinus, 202, 58, 1);
          move(closeBlock, 242, 58, 1);
          move(outerOldExp, 260, 47, 1);
          if (!await wait(1400, id)) return;
          exp.classList.remove('red');
          setCaption('De buitenmacht rekent terug: 3 - 1 wordt 2.');
          outerOldExp.classList.remove('gray');
          outerOldExp.classList.add('red');
          text(outerOldExp, '3 - 1');
          if (!await wait(850, id)) return;
          text(outerOldExp, '2');
          if (!await wait(700, id)) return;
          outerOldExp.classList.remove('red');
          const innerX = actor('x', 172, 58, 'ink');
          const innerOldExp = actor('2', 187, 47, 'small gray memory');
          const innerMovingExp = actor('2', 187, 47, 'small ink');
          const innerConstant = actor('- 1', 202, 58, 'ink');
          setCaption('Nu nemen we de binnenfunctie apart: x² - 1 komt uit het hoofdblok.');
          if (!await wait(250, id)) return;
          move(innerX, 210, 128, .72);
          move(innerOldExp, 222, 121, .86);
          move(innerMovingExp, 222, 121, .86);
          move(innerConstant, 246, 128, .72);
          if (!await wait(1050, id)) return;
          setCaption('Eerst valt de constante -1 weg.');
          innerConstant.classList.remove('ink');
          innerConstant.classList.add('red');
          if (!await wait(420, id)) return;
          innerConstant.classList.add('pop');
          if (!await wait(750, id)) return;
          innerConstant.remove();
          setCaption('Nu blijft x² over: dit is dezelfde basis als de knop Genereer x^2.');
          if (!await wait(750, id)) return;
          setCaption('Daarna beweegt die macht 2 naar voren als coefficient.');
          innerOldExp.classList.add('visible');
          innerMovingExp.classList.remove('ink');
          innerMovingExp.classList.add('red');
          innerMovingExp.classList.remove('small');
          move(innerMovingExp, 202, 128, .72);
          move(innerX, 220, 128, .72);
          move(innerOldExp, 232, 121, .86);
          if (!await wait(1100, id)) return;
          innerMovingExp.classList.remove('red');
          setCaption('De oude macht rekent terug: 2 - 1 wordt 1.');
          innerOldExp.classList.remove('gray');
          innerOldExp.classList.add('red');
          text(innerOldExp, '2 - 1');
          if (!await wait(800, id)) return;
          text(innerOldExp, '1');
          setCaption('2 - 1 wordt 1.');
          if (!await wait(360, id)) return;
          setCaption('x^1 schrijven we niet, dus de kleine macht verdwijnt.');
          innerOldExp.classList.add('pop');
          if (!await wait(520, id)) return;
          innerOldExp.remove();
          setCaption('Nu blijft x^2 over; met de machtsregel wordt dat 2x.');
          if (!await wait(1100, id)) return;
          const dot = actor('·', 278, 58);
          innerX.classList.remove('ink');
          innerX.classList.add('red');
          innerMovingExp.classList.add('red');
          move(innerMovingExp, 314, 58, 1);
          move(innerX, 333, 58, 1);
          if (!await wait(520, id)) return;
          innerMovingExp.classList.remove('red');
          innerX.classList.remove('red');
          move(outerOldExp, 254, 47, 1);
          move(dot, 276, 58, 1);
          setCaption("Preview-eindbeeld: f'(x) = 3(x² - 1)² · 2x. De constante -1 is weggevallen.");
        }

        window.addEventListener('DOMContentLoaded', () => {
          window.setTimeout(() => showPowerStart(2), 350);
        });
        </script>
        </body>
        </html>
        """;
    }

    private void Browser_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (_cardsTree.SelectedNode?.Tag is not FormulaCard card)
            return;

        var message = e.TryGetWebMessageAsString();
        switch (message)
        {
            case "copy:nod":
                Clipboard.SetText(card.ExampleNod);
                break;
            case "copy:latex":
                Clipboard.SetText(card.Latex);
                break;
            case "copy:mathml":
                Clipboard.SetText(card.MathMl);
                break;
            case "copy:text":
                Clipboard.SetText(card.PlainText);
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
        _pendingHtml = html;
        ShowPendingHtmlIfReady();
    }

    private void ShowPendingHtmlIfReady()
    {
        if (_browserFailed || _pendingHtml is null || _browser.CoreWebView2 is null)
            return;

        var html = _pendingHtml;
        _pendingHtml = null;
        _browser.NavigateToString(html);
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

    private string BuildCardHtml(FormulaCard card)
    {
        var tags = string.Join("", card.LevelTags.Select(tag => $"<span class=\"tag\">{Html(DisplayTag(tag))}</span>"));
        var visual = BuildVisualHtml(card);
        var nodMath = BuildNodMathHtml(card);
        var overview = BuildOverviewHtml(card, _cards);

        return $$"""
        <!doctype html>
        <html>
        <head>
        <meta charset="utf-8">
        <style>
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
          font-size: 20px;
          line-height: 1.5;
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
        .formula {
          font-family: Cambria Math, "Times New Roman", serif;
          font-size: 18px;
          line-height: 1.35;
          color: #101820;
        }
        .mathml-card {
          display: block;
          width: 100%;
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
        </style>
        </head>
        <body>
        <main class="page">
          <h1>{{Html(card.Title)}}</h1>
          <div class="subtitle">{{Html(T("formula_card.subtitle", "Formulekaart voor leren, PWS en export"))}}</div>
          <div class="tags">{{tags}}</div>

          {{overview}}

          <section class="section">
            <h2>{{Html(T("formula_card.section_formula", "Formule"))}}</h2>
            <div class="formula">
              <div class="mathml-card">{{card.MathMl}}</div>
              <div class="plain-formula">{{Html(card.Formula)}}</div>
            </div>
            <div class="actions">
              <button onclick="chrome.webview.postMessage('copy:text')">{{Html(T("formula_card.copy_text", "Kopieer tekst"))}}</button>
              <button onclick="chrome.webview.postMessage('copy:latex')">{{Html(T("formula_card.copy_latex", "Kopieer LaTeX"))}}</button>
              <button onclick="chrome.webview.postMessage('copy:mathml')">{{Html(T("formula_card.copy_mathml", "Kopieer MathML"))}}</button>
            </div>
          </section>

          {{nodMath}}

          {{visual}}

          <section class="section">
            <h2>{{Html(T("formula_card.section_explanation", "Uitleg"))}}</h2>
            <p>{{Html(card.Description)}}</p>
          </section>

          <section class="section">
            <h2>LaTeX</h2>
            <pre>{{Html(card.Latex)}}</pre>
          </section>

          <section class="section">
            <h2>MathML</h2>
            <pre>{{Html(card.MathMl)}}</pre>
            <div class="actions">
              <button onclick="chrome.webview.postMessage('copy:mathml')">{{Html(T("formula_card.copy_mathml", "Kopieer MathML"))}}</button>
            </div>
          </section>

          <section class="section">
            <h2>{{Html(T("formula_card.section_example_nod", "Voorbeeld-NOD"))}}</h2>
            <pre>{{Html(card.ExampleNod)}}</pre>
            <div class="actions">
              <button onclick="chrome.webview.postMessage('copy:nod')">{{Html(T("formula_card.copy_nod", "Kopieer NOD"))}}</button>
            </div>
          </section>
        </main>
        </body>
        </html>
        """;
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

    private static string? GetPrimaryTopic(FormulaCard card)
    {
        return TopicTags.FirstOrDefault(card.LevelTags.Contains);
    }

    private static string GetMenuTopic(FormulaCard card)
    {
        return card.Id switch
        {
            _ when IsCalculusCard(card) => "Differentiatie en integralen",
            "pythagoras" => "Meetkunde",
            "vector-2d-arrow" => "Vectoren",
            "matrix-2x2-determinant" => "Lineaire algebra",
            _ => GetTopicTitle(GetPrimaryTopic(card) ?? "Overig")
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
            "Lineaire algebra" => 6,
            "PWS" => 7,
            _ => 20
        };
    }

    private static bool IsCalculusCard(FormulaCard card)
    {
        return card.LevelTags.Contains("Differentiatie") || card.LevelTags.Contains("Integralen");
    }

    private static string GetCardMenuText(FormulaCard card)
    {
        var levels = string.Join(", ", card.LevelTags.Where(LevelDisplayTags.Contains).Select(DisplayTag).Take(2));
        return string.IsNullOrWhiteSpace(levels)
            ? card.Title
            : $"{card.Title}  [{levels}]";
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
            "Physics" => "Natuurkunde",
            "Limited vector" => "Beperkte vector",
            "Limited matrix" => "Beperkte matrix",
            "Linear algebra" => "Lineaire algebra",
            "2D graph" => "2D grafiek",
            "HAVO A" => "HAVO Wiskunde A",
            "HAVO B" => "HAVO Wiskunde B",
            "VWO A" => "VWO Wiskunde A",
            "VWO B" => "VWO Wiskunde B",
            "VWO D" => "VWO Wiskunde D",
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

    private static string GetStudentTitle(FormulaCard card)
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
            _ => card.Title
        };
    }

    private static string GetStudentFormulaHtml(FormulaCard card)
    {
        return card.Id switch
        {
            "derivative-power" => FormulaWithCaption(
                """<math xmlns="http://www.w3.org/1998/Math/MathML"><mrow><msup><mi>x</mi><mi>n</mi></msup><mo>&#x2192;</mo><mi>n</mi><msup><mi>x</mi><mrow><mi>n</mi><mo>-</mo><mn>1</mn></mrow></msup></mrow></math>""",
                "Bijvoorbeeld: x^2 wordt 2x"),
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
                "Bijvoorbeeld: x^2 wordt x^3/3 + C"),
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
            _ => $"""<div>{card.MathMl}</div>"""
        };
    }

    private static string FormulaWithCaption(string mathMl, string caption)
    {
        return $"""<div>{mathMl}</div><div class="formula-caption">{Html(caption)}</div>""";
    }

    private static string GetStudentUse(FormulaCard card)
    {
        return card.Id switch
        {
            "derivative-power" => "Bij x^2, x^3, wortels en machten.",
            "derivative-sum-rule" => "Als er plus of min tussen termen staat.",
            "derivative-constant-factor" => "Als er een vast getal voor staat, zoals 5x^2.",
            "derivative-product-rule" => "Als twee formules met * vermenigvuldigd worden.",
            "derivative-quotient-rule" => "Als je een breuk met x hebt.",
            "derivative-chain-rule" => "Als er haakjes of een binnenfunctie zijn, zoals sin(x^2).",
            "derivative-trig-basic" => "Bij sinus- en cosinusgrafieken.",
            "derivative-exp-log" => "Bij exponentiele groei en logaritmen.",
            "integral-power-rule" => "Bij oppervlaktes en primitiveren van machten.",
            "integral-sum-rule" => "Als er plus of min tussen termen staat.",
            "integral-constant-factor" => "Als er een vast getal voor staat, zoals 4x^2.",
            "integral-definite-area" => "Als je oppervlakte tussen twee grenzen zoekt.",
            "circle-integral" => "Voor PWS, Wiskunde D of propedeuse; niet basis.",
            _ => card.Description
        };
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
            "linear-function" or "circle-equation" or "point-line-distance" => "Meetkunde: coordinaten",
            "quadratic-formula" or "exponential-growth" => "Algebra: vergelijkingen en functies",
            _ => "Overig"
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
            "Meetkunde: coordinaten" => 0,
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

    private static string BuildNodMathHtml(FormulaCard card)
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
            "trig-right-triangle" => (
                "math sind(ans(theta))",
                "Voor graden gebruik je sind/cosd/tand. Voor radialen gebruik je sin/cos/tan."),
            "linear-function" => (
                "math ans(a) * ans(x) + ans(b)",
                "Lineaire formule met named inputs voor helling a, waarde x en startwaarde b."),
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
                "Voorbeeld van productregel. NOD 2.0 rekent numeriek; de kaart legt de symbolische regel uit."),
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
                "NOD 2.0 ondersteunt numerieke integraal; symbolische primitieve blijft formulekaart/CAS-light."),
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
                "math sqrt(ans(x)^2 + ans(y)^2)",
                "2D vectorlengte. Dit hoort bij de multi-input stap van NOD 2.1."),
            "point-line-distance" => (
                "math |ans(a)*ans(xp) + ans(b)*ans(yp) - ans(c)| / sqrt(ans(a)^2 + ans(b)^2)",
                "2D analytische meetkunde met named inputs."),
            "matrix-2x2-determinant" => (
                "math ans(a)*ans(d) - ans(b)*ans(c)",
                "Limited matrix 2x2 als formulekaart. Geen 3x3/matrix-engine in NOD 2.0."),
            "circle-integral" => (
                "math line-integral(F, path)",
                "Conceptregel voor later. Kringintegraal blijft in NOD 2.0 een uitlegkaart, geen runtime-engine."),
            _ => ExtractFirstMathLine(card.ExampleNod)
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
                    <div class="standard-form">ax^2 + bx + c = 0</div>
                  </div>
                  <div class="method-grid">
                    <div class="method-card">
                      <span class="method-label">b = 0</span>
                      <div class="method-chip">herleid tot x^2 = getal</div>
                      <div class="method-steps">
                        <div>-4x^2 + 12 = 0</div>
                        <div>-4x^2 = -12</div>
                        <div>x^2 = 3</div>
                        <div>x = sqrt(3) of x = -sqrt(3)</div>
                      </div>
                      <div class="method-note">Gebruik dit als de x-term ontbreekt.</div>
                    </div>
                    <div class="method-card">
                      <span class="method-label brown">c = 0</span>
                      <div class="method-chip">x buiten haakjes halen</div>
                      <div class="method-steps">
                        <div>2x^2 + 10x = 0</div>
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
                            <div>x^2 + 2x - 8 = 0</div>
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
                            <div>x^2 - 6x + 7 = 0</div>
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

