# Tiedragon.Graph2D.dll Proposal

Dit document werkt het voorstel uit voor een gedeelde `Tiedragon.Graph2D.dll`.
De bedoeling is dat Syscalculator, GeoSyscal en later een webversie dezelfde
2D graph-basis kunnen gebruiken.

## Doel

`Tiedragon.Graph2D.dll` wordt een herbruikbare graph-library voor:

```text
Syscalculator.exe
= calculator met graph preview

GeoSyscal.exe
= education/geometry-app

Webversie
= browser UI met dezelfde graph concepten
```

De eerste versie mag WinForms bevatten, omdat de huidige graph al in WinForms
is gebouwd. De belangrijkste ontwerpkeuze is dat de graph-logica steeds meer
los komt van Syscalculator-forms.

## Korte naam

De afgesproken DLL-naam is:

```text
Tiedragon.Graph2D.dll
```

Niet:

```text
NodGraph.dll
```

Reden: de graph is niet alleen voor NOD. GeoSyscal en een webversie moeten hem
ook kunnen gebruiken.

## Laagindeling

Voor nu mag `Tiedragon.Graph2D.dll` motor en WinForms UI samen bevatten:

```text
Tiedragon.Graph2D.dll
  Graph2D model
  Graph2D view/zoom
  Graph2D renderer
  Graph2D WinForms chrome
```

Later kan dit eventueel worden gesplitst, maar dat hoeft nu niet.

## Onderdelen

### Graph motor

Deze onderdelen zijn algemeen en moeten als eerste naar `Tiedragon.Graph2D.dll`:

```text
GraphPlotView
GraphPlotDensity
GraphPlotRenderer.GetPlotRectangle(...)
GraphPlotRenderer.ScreenToGraph(...)
```

Verantwoordelijkheid:

- view range bewaren: min/max x en y;
- coordinaten omzetten tussen scherm en graph;
- grid/tick stappen bepalen;
- cultuur/regio gebruiken voor labels, bijvoorbeeld `0.5` of `0,5`.

### Renderer

Deze onderdelen tekenen de graph:

```text
GraphPlotRenderer.Draw(...)
GraphPlotRenderer.DrawMulti(...)
```

Verantwoordelijkheid:

- achtergrond;
- rooster;
- assen;
- labels;
- graph lijn;
- meerdere series;
- highlight-punten.

### WinForms chrome

Deze onderdelen mogen ook in `Tiedragon.Graph2D.dll`, zolang de DLL voor desktop bedoeld is:

```text
GraphSurfaceApi
GraphSurfaceChrome
GraphOverlayButton
GraphPointTableOverlay
```

Verantwoordelijkheid:

- home/zoom knoppen;
- compact en normal layout;
- point table overlay;
- eenvoudige facade voor forms.

## API-vorm

Forms gebruiken bij voorkeur een eenvoudige facade:

```csharp
var chrome = GraphSurfaceApi.CreateChrome(...);

GraphSurfaceApi.Draw(
    graphics,
    canvas,
    linePoints,
    highlightPoints,
    view,
    requestedMinX,
    requestedMaxX,
    requestedStep,
    disabledMessage,
    emptyMessage,
    GraphPlotDensity.Normal);
```

De forms blijven verantwoordelijk voor:

- data maken;
- events koppelen;
- pan/zoom state bewaren;
- rows in de table vullen.

De graph API blijft verantwoordelijk voor:

- tekenen;
- coordinaten;
- chrome;
- standaard gedrag.

## Webversie

Voor web moet de graph-logica gelijk blijven, maar de tekenlaag verandert.

Desktop:

```text
Graph2D model/view
  -> WinForms Graphics renderer
```

Web:

```text
Graph2D model/view
  -> Canvas/SVG renderer
```

Daarom is het belangrijk om later een kleine renderer-grens te maken:

```text
Graph2D core data
Graph2D drawing commands
Platform renderer
```

Voorbeeld:

```text
DrawLine(...)
DrawText(...)
DrawGridLine(...)
DrawPoint(...)
```

WinForms vertaalt dit naar `System.Drawing.Graphics`.
Web vertaalt dit naar Canvas of SVG.

## Uitvoerplan

### Stap 1: API stabiliseren in Syscalculator

Status: bezig.

Acties:

- `GraphSurfaceApi` gebruiken als hoofdingang;
- duplicatie in `GraphPreviewForm` en `NodEditorForm` beperken;
- documentatie bijhouden.

### Stap 2: project `Tiedragon.Graph2D` aanmaken

Status: uitgevoerd.

Acties:

- `src/Tiedragon.Graph2D/Tiedragon.Graph2D.csproj` is aangemaakt;
- assembly name is ingesteld op `Tiedragon.Graph2D`;
- `Syscalculator.UI.WinForms` heeft een project reference naar `Tiedragon.Graph2D`;
- de eerste graph API-bestanden zijn verplaatst naar `src/Tiedragon.Graph2D`.

### Stap 3: graph chrome verplaatsen

Status: eerste versie uitgevoerd.

Acties:

- `GraphSurfaceApi` is verplaatst;
- `GraphOverlayButton` is verplaatst;
- `GraphPointTableOverlay` is verplaatst;
- de app gebruikt nu `using Tiedragon.Graph2D`;
- vervolgstap: public/internal API later verder aanscherpen wanneer GeoSyscal of web erbij komt.

### Stap 4: GeoSyscal voorbereiden

Acties:

- maak GeoSyscal afhankelijk van `Tiedragon.Graph2D.dll`;
- gebruik dezelfde view, grid, zoom en point table;
- voeg education-specifieke tools toe buiten de basis-API.

### Stap 5: webvoorbereiding

Acties:

- haal platformonafhankelijke graph data uit de WinForms renderer;
- maak een renderer-interface of drawing command model;
- bouw later een Canvas/SVG renderer.

## Voorstel

Het beste pad is:

```text
Nu:
Tiedragon.Graph2D.dll bevat de eerste graph API

Binnenkort:
Graph2D API verder opschonen voor GeoSyscal

Later:
Tiedragon.Graph2D.dll bevat gedeelde graph basis
WinForms en web krijgen elk hun eigen tekenadapter
```

Zo blijft het nu praktisch en kort, maar groeit het ontwerp mee met GeoSyscal
en een webversie.


