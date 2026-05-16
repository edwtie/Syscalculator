# JavaScript En CSS

Meestal hoef je zelf geen JavaScript te kiezen.
Het programma kiest dat automatisch per soort help.

| Waar | JavaScript | Wat doet het? |
| --- | --- | --- |
| `main/*.html` | geen extra schrijvers-JS | Gewone gebruikershelp. |
| `nod/full/*.html` | `nod-copy-buttons.js` | Zet automatisch een kopieerknop bij elk `<pre>` codeblok. |
| `nod/command/*.html` | `nod-copy-buttons.js` | Zet automatisch een kopieerknop bij elk `<pre>` codeblok. |
| `nod/popup/*.html` | `nod-popup-height.js` | Past de hoogte van de kleine helpballon aan. |
| formulekaart | `formula-card-copy-buttons.js`, `formula-search.js`, `formula-film.js` | Alleen voor de formulekaart en formulefilm. |

Welke CSS hoort erbij:

| Waar | CSS |
| --- | --- |
| `main/*.html` | `main-help.css` |
| `nod/full/*.html` en `nod/command/*.html` | `nod-help.css` |
| `nod/popup/*.html` | `nod-popup.css` |

Kort:

- Gewone tekst? Geen JS nodig.
- Codeblok met `<pre>` in NOD-help? Kopieerknop komt vanzelf.
- Uitklapblok met `<details>`? Geen JS nodig.
- Zet zelf geen `<script>` in helpteksten.
