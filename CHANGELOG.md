# Changelog

## Development Lines

```text
Syscalculator 1.74
= VB6 maintenance line
= NOD 1.0 legacy behavior
= bugfixes only

Syscalculator 2.0 Beta
= C#/.NET successor
= NOD 1.0 compatibility plus NOD 2.0 features
= active development line
```

Zie ook: `docs/SYSCALCULATOR_1_74_MAINTENANCE.md`.

## Syscalculator 2.0 Beta

### Daily Releases

#### 2.0.2026.05.02 Daily Build

**Date:** 2026-05-02

**Fixes:**
- Changed changelog to daily build grouping
- Added build/changelog batch procedure.
- Restored discoverability for newer euro converter files that were missing from the old `broncode.zip` release set.

**Additions:**
- Added separate additions list support
- Daily build changelog format: one section per day with fixes and additions.
- Added expanded euro converter set: BGN, CYP, EEK, HRK, LTL, LVL, MTL, SIT and SKK.

### Beta Releases

#### 2.0.2026.05.02 Beta Release

**Date:** 2026-05-02 12:11

**Fixes:**
- Defined beta release changelog branch

**Additions:**
- Added daily beta production channel support

### Production Releases

Nog geen production release entry voor deze dag.

**Status:** beta in voorbereiding  
**Technologie:** C# / .NET 8 / WinForms  
**Basis:** moderne opvolger van Syscalculator 1.72/1.74

Syscalculator 2.0 Beta is geen automatische VB6-conversie meer, maar een herbouw rond een aparte NOD-engine, moderne WinForms UI en betere tooling voor converterontwikkeling.

### Belangrijkste Beta-Sprong

- Zichtbare productnaam opgeschoond naar `Syscalculator`.
- Hoofdvenster dichter bij de klassieke Syscalculator-look gebracht.
- NOD Editor uitgebreid met help, suggesties, syntax highlighting, live preview en simulator.
- Graph Preview toegevoegd voor numerieke/math-converters.
- Calculator live gekoppeld aan `input1` en `input2`.
- `trans` en `chg` blijven legacy-compatible.
- NOD 2.0 patroon-`chg` met `x`-capture toegevoegd.
- Taalwisseling werkt zonder herstartmelding.
- Oude VB6/Syscal.NET conversiepoging is vervangen door een gecontroleerde herbouw.

### Beta Fixronde: Editor-Polish, Simulator en Taalconsistentie

**Aantal fixes:** 36 zichtbare UI-, taal- en paneel-fixes.

Deze fixronde maakte de NOD Editor meer een professionele tool-editor:

- Tabbladen opnieuw gepolijst als echt tabpapier.
- Tabrand, editorpapier, regelnummers en sluitknop visueel gelijkgetrokken.
- Sluitknop rustiger gemaakt met blauwe hover in plaats van rode waarschuwing.
- Statusbalk opgeschoond: geen dubbele bestandsnaam meer onderin.
- Statusbalk vertaald per taal, zoals `Regel`, `Kolom` en `Regels`.
- `Weergave`-menu voorzien van vinkjes voor zichtbare panelen.
- Lege onderbalk verdwijnt wanneer alle panelen verborgen zijn.
- `Panelen herstellen` toegevoegd voor snelle terugkeer naar standaardweergave.
- Testpaneel, Live preview en Simulator kunnen losgemaakt en teruggedockt worden.
- Los venster: `-` dockt terug, `X` sluit/verbergt.
- Overbodige maximize-knop verwijderd uit losse toolvensters.
- Simulator-preview tekstueel gelijkgetrokken met de echte converter.
- Simulator toont de echte converternaam in plaats van `current.nod`.
- Simulator-combobox schaalt mee met lange namen.
- `Bestand`, `Config`, `Cijfergroepen` en `Decimalen` gekoppeld aan dezelfde taalbronnen.
- Graph Preview en Convertertest verder vertaald in alle taalbestanden.

### NOD Editor Plus

De editor is nu de centrale werkplaats voor NOD-bestanden.

Toegevoegd en verbeterd:

- Meerdere editor-tabs.
- Eigen tabpapier-weergave met sluitknop.
- Line-number gutter.
- Syntax highlighting.
- Zoek/vervang.
- Validatie.
- Testpaneel.
- Formule- en berekening-preview.
- Live metadata-preview.
- Converter-simulator.
- Graph Preview.
- Rechterklik-hulp per NOD-command.
- NOD help in meerdere talen.
- Automatische reparatie van samengeplakte NOD-regels.
- Automatische normalisatie voor parsing.

### Simulator en Preview

De simulator is bedoeld als voorvertoning van hoe een NOD eruitziet in de echte converterinterface.

Belangrijke ontwerpkeuze:

- Simulator en echte converter gebruiken dezelfde termen.
- Simulator toont `Name`/`URLN` als converternaam.
- Labels zoals `input1`, `input2`, symbolen en `format` worden direct zichtbaar.
- `Cijfergroepen` en `Decimalen` komen uit dezelfde taalbronnen als in de echte converter.
- Panelen kunnen gedockt of los gebruikt worden.

### Graph Preview

Graph Preview toont numerieke converters als grafiek.

Ondersteund:

- `math`-converters.
- Legacy math-stappen.
- NOD 2.0 expressies.
- Sampling over X-min, X-max en stapgrootte.
- Puntentabel.
- Kopieren van punten.
- Zoom en pan.
- Los groot grafiekvenster.

Beperkingen:

- Niet bedoeld voor `chg`-converters.
- Niet bedoeld voor `trans`-converters.
- Alleen bruikbaar wanneer de converter numerieke output geeft.

### Taal en Lokalisatie

De WinForms UI is verder gelijkgetrokken over meerdere taalbestanden:

- Nederlands
- Engels
- Duits
- Frans
- Italiaans
- Spaans
- Portugees
- Indonesisch
- Chinees

Verbeterd:

- Statusbalklabels.
- Convertertest.
- Simulator.
- Graph Preview.
- Weergave-menu.
- Panelen herstellen/docken/losmaken.
- Korte menuwoorden zoals `Config` om smalle menubalken bruikbaar te houden.

## NodSystem.Core

De oude NOD-logica is losgetrokken uit de UI en ondergebracht in:

```text
NodSystem.Core
```

Belangrijkste onderdelen:

```text
NodParser
NodDocument
NodEngine
NodExpressionEvaluator
NodReverse
CalculationTrace
EquationEngine
DataTransformEngine
SqlPreviewGenerator
ReportBuilder
DocumentBatchEngine
```

### NOD 1.0 Compatibility

Oude NOD-bestanden blijven herkenbaar en uitvoerbaar.

Ondersteund:

```nod
chg 03402,03060
trans yes,ja
math ans * 1,8
math ans + 32
```

Compatibel met:

- `Name`
- `URLN`
- `input1`
- `input2`
- `Result`
- `Resfou`
- `Symb1` t/m `Symb4`
- `format`
- `math`
- `chg`
- `trans`
- `reverse`
- `indoprint` / `indoend`
- `end`
- bestaande catalogusstructuur zoals `freesyscal.cfg`

### NOD 2.0 Math

NOD 2.0 voegt een rijkere expressielaag toe bovenop legacy math.

Voorbeelden:

```nod
math ans^2
math e^2
math ans * e^2
math pi * ans^2
math sqrt(ans)
math abs(ans)
math |ans|
math ln(ans)
math log(ans,2)
```

Toegevoegd:

- haakjes
- machten
- constante `e`
- constante `pi`
- functies
- meerdere argumenten
- komma- en puntdecimalen
- combinatie van legacy math en expressie-math in juiste volgorde

### Trigonometrie

Ondersteund:

```nod
math sin(ans)
math cos(ans)
math tan(ans)
math asin(ans)
math acos(ans)
math atan(ans)
math sind(ans)
math cosd(ans)
math tand(ans)
math rad(ans)
math deg(ans)
```

### Kansrekening en Discrete Math

Toegevoegd aan de math-expressies:

```nod
math ans!
math fact(ans)
math comb(ans,2)
math perm(ans,2)
math expected(p,x)
```

Ondersteund:

- faculteit
- combinaties
- permutaties
- verwachtwaarde
- constante `e`

### Calculus

Ondersteund als NOD 2.0-laag:

```nod
diff ans^2
integral 0,1 ans^2
limit 0 sin(ans)/ans
```

Fixes:

- parsing van `diff`
- parsing van `integral`
- parsing van `limit`
- foutmeldingen met regelnummers
- ondersteuning in Graph Preview waar numeriek zinvol

### Reverse en Calculation Trace

Reverse is verbeterd voor zowel klassieke als modernere math.

Ondersteund:

- legacy automatische reverse
- expliciete `reverse`-regel
- reverse trace
- stap-voor-stap calculation trace
- herkenning van math-stappen in oude en nieuwe stijl

Belangrijke fix:

- legacy `math ans * 1,8` en NOD 2.0 `math ans * e^2` worden nu in originele volgorde uitgevoerd.

### Data-, Field- en Batchlaag

Prototypefuncties voor enterprise/data-scenario's:

- `mode data`
- `table`
- `field`
- `output`
- `phoneformat`
- `lookup`
- `match`
- documentbatch op in-memory strings
- rapportageprototype
- SQL-preview generator

Status:

- bruikbaar als concept/prototype
- nog niet als volledige SQL connector uitgewerkt

## Syscalculator.UI.WinForms

De UI-laag bevat:

- `MainForm`
- `NodEditorForm`
- `WizardExpressForm`
- `CalculatorForm`
- `CatalogManagerForm`
- `TraceViewerForm`
- `GraphPreviewForm`
- `AboutForm`
- `FloatingToolForm`

### MainForm

Verbeterd:

- klassieke converterlook
- moderne toolbar
- gekoppelde calculator
- live convert
- trace control
- taalwisseling
- catalogusbeheer
- tray support
- opties voor cijfergroepen en decimalen

### WizardExpressForm

Moderne versie van WizardExpress.

Ondersteund:

- tekstregels converteren
- tab-gescheiden cellen converteren
- eenvoudige batchachtige conversie

### Floating Tool Windows

Losse toolvensters voor editorpanelen.

Gedrag:

- dubbelklik op paneel maakt los
- `-` dockt terug
- `X` sluit/verbergt
- maximize is uitgeschakeld
- panelen blijven eigenaar van dezelfde live controls

Ondersteund:

- Testpaneel
- Live preview
- Simulator

## Syscalculator 2.0 Alpha 1

**Status:** eerste moderne herbouw / alpha-prototype  
**Technologie:** C# / .NET 8 / WinForms  
**Basis:** voortzetting van Syscalculator 1.72 en het oorspronkelijke NOD-systeem

Alpha 1 bewees dat het oude NOD-model losgemaakt kon worden van VB6 en opnieuw bruikbaar kon worden in een moderne .NET-architectuur.

### Belangrijkste Alpha-Resultaten

- Eerste aparte NOD core-library.
- Eerste parser en engine.
- Eerste WinForms hoofdvenster.
- Eerste editor.
- Eerste calculator-koppeling.
- Eerste traceviewer.
- Eerste data- en SQL-preview prototypes.
- Eerste regressietests.

### Bekende Alpha-Beperkingen

- UI was nog prototypeachtig.
- Editor was nog niet professioneel genoeg.
- Simulator bestond nog niet in huidige vorm.
- Graph Preview was nog niet volledig uitgewerkt.
- Taalconsistentie was beperkt.
- Losse toolvensters waren nog niet stabiel ontworpen.

## Syscalculator 1.72 / 1.74 Legacy

### Syscalculator 1.72

Oude VB6-lijn en historische basis van het project.

Aanwezige onderdelen:

- VB6 UI
- NOD 1.0 runtimegedrag
- klassieke converters
- oude catalogusbestanden
- calculator
- oude WizardExpress-achtige functies

Beperkingen:

- sterk gekoppeld aan VB6 UI
- moeilijk onderhoudbaar
- lastig te converteren naar moderne .NET
- automatische Syscal.NET conversiepoging leverde te veel ballast op

### Syscalculator 1.74

Onderhoudslijn voor legacy.

#### Syscalculator 1.74 Euro NOD Maintenance Candidate

**Date:** 2026-05-02

**Status:** candidate, not final release

**Package type:** data-only `.nod` maintenance package

**Fixes:**
- Fixed the 1.74 source tree by adding missing newer euro converters to the legacy `euro` group.
- Kept the original grouped directory layout from `broncode.zip`.

**Additions:**
- Added `BUILD_SYSCALCULATOR174_EURO_NOD_CANDIDATE.bat`.
- The candidate package adds BGN, CYP, EEK, HRK, LTL, LVL, MTL, SIT and SKK to the legacy `euro` group.

**Before final release:**
- Test the 1.74 source.
- Compile `Syscalculator174.local.vbp` with VB6.

Doel:

- compatibiliteit bewaren
- kleine bugfixes
- geen grote nieuwe architectuur

## Ontwerpkeuze

De belangrijkste keuze in 2.0:

```text
Niet blind converteren.
Wel opnieuw bouwen met het oude NOD-model als referentie.
```

Waarom:

- oude NOD-bestanden blijven bruikbaar
- core en UI zijn gescheiden
- tests kunnen het gedrag bewaken
- nieuwe functies passen bovenop het oude model
- de editor kan NOD begrijpelijk maken voor gebruikers zonder diepe ICT-kennis

## Tests

De testset controleert onder andere:

- legacy `chg`
- legacy `trans`
- legacy math
- NOD 2.0 expressies
- reverse
- calculation trace
- equation solving
- data transform
- phoneformat
- probability math
- calculus parsing
- automatische normalisatie
- combinatie van legacy math en NOD 2.0 math

Laatste bekende teststatus na parser- en probability-fixes:

```text
Passed 41/41 tests
All tests passed
```

## Open Punten

Nog niet volledig afgerond:

- echte SQL connector
- uitgebreidere enterprise datakoppelingen
- verdere taalreview op langere helpteksten
- visuele review op alle schermgroottes
- verdere documentatie voor studenten/teamopdracht
- eventueel automatiseren van screenshot/UI-regressietests

## Samenvatting

Syscalculator 2.0 Beta brengt het project van oude VB6-erfenis naar een moderne, testbare en uitbreidbare .NET-applicatie.

De kern blijft NOD: kleine, leesbare regels waarmee gebruikers converters kunnen beschrijven. De nieuwe editor maakt dat principe zichtbaar, testbaar en leerbaar.
