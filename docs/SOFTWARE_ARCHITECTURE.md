# Softwarearchitectuur

![Syscalculator totale architectuur](SYSCALCULATOR_ARCHITECTURE.svg)

## Doel

Deze architectuur laat zien hoe Syscalculator is opgebouwd uit de hoofdapp,
herbruikbare Tiedragon libraries, runtime resources, NOD core, tests, demo en
installer/documentatie.

De tekening is bedoeld als overzicht voor ontwikkelaars en voor documentatie.
De namen in de blokken zijn bewust kort gehouden, zodat de tekening rustig
blijft en geen technische bestandsuitgang zoals `.dll` hoeft te tonen.

## Hoofdlagen

| Laag | Verantwoordelijkheid |
|---|---|
| Platform | Windows, .NET, WebView2, clipboard en file system. |
| Syscalculator App | Schermen, workflow, menu, catalogus, editor, formulekaart en preview. |
| Runtime data en resources | `.nod` converters, configuratie, taalbestanden, helpcontent, SVG en media. |
| Tiedragon Library | Herbruikbare onderdelen zoals ClipboardConvert, Graph2D, ToolEditor, Help en NodSystem Core. |
| NOD engine laag | Parser, engine, berekeningen, data-transformatie, solverstappen en rapportage. |
| Overige projecten | UWP prototype, demo, tests, installer en documentatie. |

## Runtime hoofdlijn

```text
Gebruiker
  -> app schermen
  -> Tiedragon Library
  -> NOD core
  -> resources/config/.lng
  -> resultaat, graph, trace, help of clipboard
```

## Ontwerpafspraken

- De app gebruikt herbruikbare libraries via projectreferenties.
- Help, Graph2D, ToolEditor en ClipboardConvert blijven los van de hoofdapp.
- NOD logica blijft in NodSystem Core en heeft geen UI-afhankelijkheid.
- Runtime content zoals help, taal en `.nod` bestanden blijft bewerkbaar zonder C# build.
- Diagrammen gebruiken SVG voor scherpte en onderhoudbaarheid.

## Belangrijke paden

| Pad | Inhoud |
|---|---|
| `src/syscalculator` | WinForms hoofdapp en runtime resources. |
| `src/Tiedragon.NodSystem.Core` | Parser, engine, modellen en NOD routes. |
| `src/Tiedragon.Help` | Gedeelde help-laag. |
| `src/Tiedragon.Graph2D` | Graph renderer en graph UI bouwstenen. |
| `src/Tiedragon.ToolEditor` | Editor toolbar, tabs, tips en iconen. |
| `src/Tiedragon.ClipboardConvert` | Clipboard en tabelconversies. |
| `src/NodSystem.Demo` | Console demo rond NOD core. |
| `src/NodSystem.Tests` | Tests voor parser, engine en gedrag. |
