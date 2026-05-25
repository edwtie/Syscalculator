# Syscalculator 2.0 UI Architecture

Deze UI-laag is bewust gescheiden van `Tiedragon.NodSystem.Core`.

## Projecten

```text
Tiedragon.NodSystem.Core
= engine/library

Tiedragon reusable libraries
  Tiedragon.ClipboardConvert
  = gedeelde clipboard/copy/paste/convert library voor tekst, TSV, CSV en HTML clipboard formats

  Tiedragon.Graph2D
  = gedeelde graph library/DLL voor Syscalculator en later GeoSyscal

  Tiedragon.ToolEditor
  = gedeelde editor library/DLL voor toolbar, tabs, tips/ballonnen en kleuren

Syscalculator.UI.WinForms
= hoofdscherm, menu, WizardExpress, editor, catalogus, trace viewer
```

## Drie lagen

```text
Laag 1: Core / Engine
  Tiedragon.NodSystem.Core.dll
  = parser, conversies, berekeningen, NOD engine

Laag 2: Tiedragon reusable libraries
  Tiedragon.ClipboardConvert.dll
  = clipboard lezen, copy/paste, TSV/CSV/HTML clipboard formats en tab/regel conversie

  Tiedragon.Graph2D.dll
  = graph renderer, assen, rooster, zoom/view, point table en graph UI

  Tiedragon.ToolEditor.dll
  = editor toolbar, tabs, iconen, kleuren en tips/ballonnen

Laag 3: Applicatie
  Syscalculator.exe
  = schermen, menu, workflow en gebruikerslogica
```

```text
Syscalculator.exe
  gebruikt Tiedragon.NodSystem.Core.dll
  gebruikt Tiedragon.ClipboardConvert.dll
  gebruikt Tiedragon.Graph2D.dll
  gebruikt Tiedragon.ToolEditor.dll
```

De Tiedragon-libraries zijn bedoeld om later ook buiten Syscalculator te
gebruiken, bijvoorbeeld in `GeoSyscal.exe` of een toekomstige webversie.

```text
GeoSyscal.exe
  gebruikt Tiedragon.Graph2D.dll
  gebruikt eventueel Tiedragon.ToolEditor.dll
```

## Oude projectindeling

```text
Tiedragon.NodSystem.Core
= engine/library

Tiedragon.ClipboardConvert
= gedeelde clipboard/copy/paste/convert library voor tekst, TSV, CSV en HTML clipboard formats

Tiedragon.Graph2D
= gedeelde graph library/DLL voor Syscalculator en later GeoSyscal

Tiedragon.ToolEditor
= gedeelde editor library/DLL voor toolbar, tabs, tips/ballonnen en kleuren

Syscalculator.UI.WinForms
= hoofdscherm, menu, WizardExpress, editor, catalogus, trace viewer
```

## Forms

| Form | Oude VB6-rol | Nieuwe rol |
|---|---|---|
| MainForm | Form1.frm | hoofdcalculator |
| WizardExpressForm | WizardExpress.frm | clipboard/table conversie |
| NodEditorForm | editor.frm/Zeditor.frm | .nod editor + validator |
| CatalogManagerForm | Form3/Form4 | converterlijst beheren |
| TraceViewerForm | nieuw | Calculation Trace bekijken |

## Datastroom

```text
freesyscal.cfg
  ↓
NodCatalogService
  ↓
MainForm ComboBox
  ↓
NodParser
  ↓
NodEngine
  ↓
Resultaat / Trace
```

## Graph Surface API

De graph UI is opgebouwd als een kleine herbruikbare API-laag. Het doel is dat
`GraphPreviewForm`, de embedded graph in `NodEditorForm`, en toekomstige
applicaties dezelfde graph-onderdelen kunnen gebruiken zonder losse knop-,
tabel- en rendercode te kopieren.

### Hoofd API

`GraphSurfaceApi` is de eenvoudige ingang voor schermen die een graph tonen.
Deze facade roept intern de kleinere API-onderdelen aan.

```csharp
var chrome = GraphSurfaceApi.CreateChrome(
    GraphOverlayButtonDensity.Normal,
    "Points",
    GetPointDetailText,
    ClosePointTable,
    PointTableMouseDown,
    PointTableMouseMove,
    PointTableMouseUp,
    "Home",
    (_, _) => ResetGraphView(),
    "Zoom in",
    (_, _) => ZoomGraph(0.8f),
    "Zoom out",
    (_, _) => ZoomGraph(1.25f));
```

Belangrijkste functies:

| API | Verantwoordelijkheid |
|---|---|
| `GraphSurfaceApi.CreateChrome(...)` | Maakt in een keer de standaard graph chrome: navigatieknoppen en point-table overlay. |
| `GraphSurfaceApi.Draw(...)` | Tekent een enkele graph via de gedeelde renderer. |
| `GraphSurfaceApi.GetPlotRectangle(...)` | Geeft het tekengebied binnen de canvas terug. |
| `GraphSurfaceApi.ScreenToGraph(...)` | Zet muis/schermcoordinaten om naar graphcoordinaten. |

`GraphSurfaceChrome` is de returnwaarde van `CreateChrome(...)` en bevat de
onderdelen die een form meestal moet plaatsen of vullen:

```text
NavigationPanel
PointTablePanel
PointTableTitleBar
PointTable
```

### Interne API-onderdelen

| API | Rol |
|---|---|
| `GraphOverlayButton.Create(...)` | Maakt een losse icon-only graph knop. |
| `GraphOverlayButton.CreateNavigationGroup(...)` | Maakt de vaste groep: home, zoom in, zoom out. |
| `GraphPointTableOverlay.Create(...)` | Maakt een draggable tabel-overlay met titelbalk, sluitknop en `DataGridView`. |
| `GraphPlotRenderer.Draw(...)` | Tekent grid, assen, labels, lijn en highlight-punten. |
| `GraphPlotRenderer.DrawMulti(...)` | Tekent meerdere graph-series, gebruikt o.a. door solver/steps views. |

### Compact en normal

Graph chrome gebruikt `GraphOverlayButtonDensity`:

| Density | Gebruik |
|---|---|
| `Compact` | Embedded graph preview, bijvoorbeeld in de editor-tab. |
| `Normal` | Standalone graph preview window. |

De renderer heeft daarnaast `GraphPlotDensity` voor de dichtheid van grid,
labels en micro-stappen. Daardoor blijft het visuele gedrag centraal geregeld:
bij inzoomen verschijnen kleine labels zoals `0.5` of `0,5`, afhankelijk van de
regio-instelling van Windows.

### Verantwoordelijkheidsgrens

De API-laag beheert:

- graph chrome: knoppen, table overlay, icon drawing en compacte/normale maten;
- rendering: grid, assen, tick labels, curves en highlight-punten;
- coordinaten: conversie tussen schermpunten en graphwaarden.

De forms blijven verantwoordelijk voor:

- NOD-data lezen en punten berekenen;
- huidige view state, zoom, pan en home range;
- events koppelen aan applicatielogica;
- rijen vullen in de point table.

Zo blijft de graph API bruikbaar buiten Syscalculator, vergelijkbaar met het
idee achter `Tiedragon.NodSystem.Core`: kleine, duidelijke functies die later makkelijker
naar een gedeelde library kunnen verhuizen.

### Toekomstige DLL

De afgesproken korte DLL-naam voor de graph-laag is:

```text
Tiedragon.Graph2D.dll
```

Deze DLL bevat in de eerste versie zowel de 2D graph-motor als de WinForms graph UI:

```text
Tiedragon.Graph2D.dll
= 2D graph renderer, rooster, assen, punten, zoom/view, point table en graph chrome
```

Deze naam is bewust niet `NodGraph.dll`, omdat de graph ook buiten NOD bruikbaar
moet zijn. De eerste beoogde gebruikers zijn:

```text
Syscalculator.exe
= calculator-app met graph preview

GeoSyscal.exe
= education/geometry-app met dezelfde Graph2D basis
```

De migratie is gestart en kan verder stap voor stap gebeuren:

1. `GraphPlotRenderer`, `GraphPlotView` en `GraphPlotDensity` staan in `Graph2D`;
2. `GraphSurfaceApi`, `GraphOverlayButton` en `GraphPointTableOverlay` staan in `Graph2D`;
3. volgende stap: forms verder beperken tot data, events, zoom/pan state en applicatielogica.

De actuele Graph-indeling staat in `TIEDRAGON_GRAPH_ARCHITECTURE.svg` en in de
Graph-secties van deze architectuurdocumentatie.

## Tiedragon.ClipboardConvert API

`Tiedragon.ClipboardConvert.dll` is de gedeelde API-laag voor clipboard-acties.
De eerste versie centraliseert copy/paste en tab/regel conversie voor
`WizardExpressForm` en eenvoudige copy/paste voor `CalculatorForm`.

Belangrijkste functies:

| API | Verantwoordelijkheid |
|---|---|
| `ClipboardConvertApi.ReadSnapshot(...)` | Leest clipboardtekst, beschikbare formats en eigenaar/debug-info. |
| `ClipboardConvertApi.TryGetText(...)` | Leest gewone tekst voor paste-acties. |
| `ClipboardConvertApi.SetText(...)` | Kopieert tekst naar clipboard met tekst, TSV, CSV en HTML table formats. |
| `ClipboardConvertApi.ConvertDelimitedText(...)` | Loopt door regels en tab-cellen en roept een conversiefunctie per cel aan. |
| `ClipboardConvertApi.NormalizeText(...)` | Normaliseert line endings voor clipboardgebruik. |
| `ClipboardDebugApi.CreateReport(...)` | Maakt een nette debugger-rapportage met owner, rows, columns, formats en preview. |

De eerste aangesloten gebruikers zijn:

```text
WizardExpressForm
= clipboard lezen, cellen converteren, resultaat terugzetten als tekst/TSV/CSV/HTML

CalculatorForm
= copy/paste via dezelfde clipboard API
```

## Tiedragon.ToolEditor API

`Tiedragon.ToolEditor.dll` is de gedeelde API-laag voor editor-tools. De eerste versie
centraliseert de hoofdtoolbar en de editor-tabheaders van `NodEditorForm`.

Belangrijkste functies:

| API | Verantwoordelijkheid |
|---|---|
| `ToolEditorApi.CreateToolbar(...)` | Maakt een standaard editor-toolbar met vaste padding, image size en kleurinstelling. |
| `ToolEditorApi.CreateButton(...)` | Maakt een toolbar-knop met tekst, icoon, tooltip en click-handler. |
| `ToolEditorApi.CreateTipBalloon(...)` | Maakt een herbruikbare balloon tooltip voor korte help/tip meldingen. |
| `ToolEditorApi.CreateIcon(...)` | Tekent standaard editor-iconen centraal. |
| `ToolEditorTabsApi.CreateStrip(...)` | Maakt de visuele editor-tabstrip. |
| `ToolEditorTabsApi.CreateHeader(...)` | Maakt een tabheader met titel, dirty marker en sluitknop. |
| `ToolEditorTabsApi.SetHeaderState(...)` | Past selected/dirty/title state toe op een tabheader. |

De kleuren zitten in `ToolEditorPalette`. Daardoor kan later een ander thema
worden gekozen zonder alle editor-knoppen apart aan te passen.

De eerste aangesloten gebruiker is:

```text
NodEditorForm
= hoofdtoolbar en editor-tabs gebruiken Tiedragon.ToolEditor API
```

## Starten

```bash
dotnet run --project src/syscalculator
```

Let op: dit vereist Windows vanwege WinForms.



