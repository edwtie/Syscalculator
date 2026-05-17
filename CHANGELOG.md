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

#### 2.0.2026.05.17 Daily Build

**Date:** 2026-05-17

**Fixes:**
- Fixed graph preview rendering and synchronization for richer 2D surfaces
- Fixed NOD math parsing/evaluation for advanced functions and statistics workflows
- Fixed Windows shell identity for main app, NOD Editor and NOD Template Wizard shortcuts
- Fixed the Feedback Mail and OK buttons by reading WebView2 feedback messages with case-insensitive JSON property names.
- Fixed the Feedback system information checkbox so it renders and toggles as a normal checkbox.
- Removed the visible Mail text from the Feedback send button so only the envelope icon is shown.
- Fixed missing localized Feedback dialog labels across bundled language files.
- Fixed missing About release channel label in German, French, Indonesian, Italian, Portuguese, Spanish and Simplified Chinese.

**Additions:**
- Added a feedback/support information window with bundled artwork
- Added expanded formula card content and solver step polish
- Added advanced and statistics NOD help pages plus Dutch help text updates
- Added Feedback menu and dialog translations for English, Dutch, German, French, Italian, Portuguese, Spanish, Indonesian and Simplified Chinese.
- Published the daily installer as `Syscalculator-2.0-daily-2.0.2026.05.17.exe`.

#### 2.0.2026.05.16 Daily Build

**Date:** 2026-05-16

**Fixes:**
- Fixed the Help architecture split by moving Help code into the separate `Tiedragon.Help` project.
- Fixed hardcoded Help content paths by loading editable runtime HTML, CSS, JavaScript and content resources.
- Fixed NOD Editor help artwork by replacing the bitmap screenshot mock with a sharp SVG mock.
- Fixed help and architecture diagram readability by removing unnecessary connector labels, shortening visible block names and keeping arrows attached to colored group boundaries.
- Fixed Help content maintainability by adding writer-friendly examples, warnings, details blocks, tables, screenshots and command-link patterns.

**Additions:**
- Added English Markdown architecture documentation for the overall software architecture, Help architecture and NOD system architecture.
- Added SVG architecture diagrams for Syscalculator, Help and NOD system layers.
- Added runtime Help content groups for main Help, NOD full Help, NOD command Help, popup Help, snippets and Help authoring guidance.
- Added language-key based Help content support with English fallback behavior.
- Added SVG-based Help mock support so localized screenshots can be generated from editable vector text instead of maintaining multiple PNG files.
- Added reusable Help rendering helpers for templates, topic content, text lookup and screenshot embedding.
- Added `AppWindowIcon` helper so application windows consistently use the Syscalculator icon.
- Published the daily installer as `Syscalculator-2.0-daily-2.0.2026.05.16.exe`.
- Promoted this tested daily line to Beta 1 later the same day.

#### 2.0.2026.05.12 Daily Build

**Date:** 2026-05-12

**Fixes:**
- Fixed NOD Editor undo/redo so text edits use an editor-owned history stack instead of the RichTextBox formatting history.
- Fixed undo/redo redraw jitter by adding an anti-shock redraw/suggestion suppression path around editor history operations.
- Fixed Restore behavior in the NOD Editor so it returns the current tab to the last opened or saved text instead of running line repair.
- Fixed the NOD command tip popup so it is hosted inside the editor, hides during menu/editor actions, and sizes itself to the rendered help text.
- Fixed NOD command tip shadow rendering so the bubble keeps a clear edge without hiding text underneath.
- Fixed recent NOD file suggestions so duplicate file names are deduplicated by converter-relative identity.
- Fixed the template wizard preview so template text is shown as readable dark code with syntax coloring.
- Fixed NOD text normalization for pasted or concatenated legacy metadata lines such as `URLN`, `Result`, `Resfou`, `indoprint` and `indoend`.
- Fixed NOD help language labels, including the English `History` title and the English VB6-.NET history card title.

**Additions:**
- Added NOD Editor toolbar and menu support for Undo, Redo and Restore, including new shared ToolEditor icons.
- Added a NOD Editor guide page to both the main help and the NOD Editor help, including a real editor screenshot.
- Added book-style NOD help chapters: NOD introduction, using the NOD Editor, and from classic NOD to NOD 2.0.
- Added localized help screenshot lookup, for example `NodEditorHelp.ned.png` or `NodEditorHelp.eng.svg`, with fallback to the shared image.
- Added SVG support for help screenshots so language-specific help artwork can use sharp editable text.
- Added blue navigation button styling and clearer icon placement for help-style navigation buttons.
- Added localized NOD Editor menu/toolbar/help labels across the bundled language files.

#### 2.0.2026.05.09 Daily Build

**Date:** 2026-05-09

**Fixes:**
- Fixed .NET 10 WinForms analyzer compatibility by marking runtime-only custom control properties with explicit designer serialization metadata.
- Fixed the language selection dialog so the currently selected language is shown first while the remaining languages stay alphabetical.
- Fixed catalog manager column headers so technical property names are replaced with localized labels.
- Fixed the main Help menu so user help is available with the F1 shortcut.
- Fixed the main Edit menu so Cut, Copy, Paste and Select all expose standard Ctrl shortcuts for the active field.
- Fixed the main Edit menu so Delete is available with the Del shortcut and acts on the active field selection.
- Fixed Copy input/output so empty fields no longer cause clipboard copy problems.
- Fixed the main Edit menu enabled states so unavailable clipboard actions are disabled like the text box context menu.
- Hid unused IME context-menu entries for non-CJK input languages while preserving the default Windows IME menu for Chinese, Japanese and Korean input.
- Fixed Help dialog topic-pane sizing so long localized subject titles remain readable.
- Fixed the first Help topic title so every bundled language starts with `Start`.
- Fixed Help dialog splitter initialization so opening Help no longer throws a `SplitterDistance` exception.
- Fixed the Help dialog default size so it opens compactly while keeping the topic list readable.
- Fixed the 2.0 installer so it detects the required Microsoft .NET 10 Desktop Runtime x64 before setup continues.
- Fixed the 2.0 installer build script so a failed `dotnet publish` stops before Inno Setup runs.

**Additions:**
- Upgraded the Syscalculator 2.0 daily development line from .NET 8 to .NET 10.
- Updated project targets for the WinForms app, reusable helper projects, demo and test console to .NET 10.
- Added localized catalog manager column labels for all bundled language files.
- Expanded the main user help with practical explanations of the main window, field editing, clipboard actions, IME context menus and daily tools.
- Expanded localized in-app help pages for all bundled languages with practical daily-use topics and limitations.
- Verified the .NET 10 migration with restore, 80/80 NOD system tests, Release build and publish smoke test.

#### 2.0.2026.05.08 Daily Build

**Date:** 2026-05-08

**Fixes:**
- Added visible release information to the main Syscalculator About window.
- Updated the main window title so daily builds show the build label, for example `Syscalculator 2.0 daily build 2026.05.08.006`.
- Added English and Dutch language labels for the release channel field.
- Fixed formula card layout overflow and duplicated formula card action buttons.
- Improved Syscalculator help layout, search and navigation polish.

**Additions:**
- Published the first GitHub daily installer releases for Syscalculator 2.0: `v2.0.2026.05.08.003-daily` and `v2.0.2026.05.08.006-daily`.
- Added GitHub release assets for `Syscalculator-2.0-daily-2.0.2026.05.08.003.exe` and `Syscalculator-2.0-daily-2.0.2026.05.08.006.exe`.
- Restored legacy `Configuratie -> Start` behavior for starting with Windows.
- Restored legacy `Configuratie -> Overschakelen` behavior for reversing converter direction.
- Added reusable help-style navigation API and formula/help navigation controls.
- Expanded the Syscalculator 2.0 installer language support to English, Dutch, German, French, Italian, Portuguese, Spanish, Indonesian and Simplified Chinese.
- Continued separating reusable code into `Tiedragon.NodSystem.Core`, `Tiedragon.Graph2D`, `Tiedragon.ToolEditor` and `Tiedragon.ClipboardConvert`.
- Verified the daily release packaging with the NOD/System test runner: 80 passed, 0 failed, solution build and Inno Setup 6.7.1 installer builds.

#### 2.0.2026.05.07 Daily Build

**Date:** 2026-05-07

**Fixes:**
- Fixed Syscalculator help layout details: clearer pane borders, aligned topic/content borders, improved scrollbar spacing and more consistent WebView scrollbar styling.
- Fixed WizardExpress help structure so Excel/Microsoft 365 clipboard warnings are shown in the WizardExpress workflow and linked to the applications section.
- Fixed formula card layout overflow where the MathML/formula panel could run outside its group.
- Fixed duplicate formula card action buttons by keeping copy actions inside the relevant formula sections instead of repeating them in the bottom bar.
- Fixed the formula card LaTeX section by adding the missing in-page `Kopieer LaTeX` action.
- Fixed formula card tree border styling so the topic list no longer shows a doubled vertical border.

**Additions:**
- Added the legacy `Configuratie -> Start` option to Syscalculator 2.0 so users can enable or disable starting with Windows.
- Added the legacy `Configuratie -> Overschakelen` behavior to Syscalculator 2.0, including swapping the converter direction and `input1`/`input2` values.
- Added a reusable help-style navigation API with large Previous, Home, Next and Close buttons for help-style windows.
- Added help-style navigation to the formula card window, including Previous, Home, Next and Close buttons in one consistent bottom row.
- Added search and navigation polish to the help window for a more browser-like help experience.
- Added search to the formula card window, including previous/next result navigation and Ctrl+F focus.
- Updated the Syscalculator 1.72/1.74 versus 2.0 comparison to include restored legacy options such as Start, Overschakelen, introductions and formula/help differences.
- Expanded the Syscalculator 2.0 Inno Setup installer from English/Dutch to nine installer languages: English, Dutch, German, French, Italian, Portuguese, Spanish, Indonesian and Simplified Chinese.

#### 2.0.2026.05.06 Daily Build

**Date:** 2026-05-06

**Fixes:**
- Fixed ClipboardConvert row counting so trailing empty lines are not shown as extra rows after conversion.
- Fixed duplicate blank-line output in converted clipboard text.
- Fixed the clipboard diagnosis preview so the visible row count matches the Excel-like grid.
- Fixed formula reporting so users see converter variable names such as `C` and `F` instead of only internal `ans` expressions.
- Fixed HTML e-mail report generation by opening a generated `.eml` message instead of sending the whole report through `mailto` or the clipboard first.
- Fixed `mailto` URI parsing risk for support reports.
- Fixed duplicated formula display in HTML reports by using the rendered formula as primary output and plain text only as fallback.
- Fixed PDF overflow by allowing report content, especially clipboard formats, to continue on a second page.
- Fixed PDF report layout for `Voor / Na`, preview tables and formula blocks so the report is cleaner on A4.
- Fixed report date formatting so PDF and e-mail reports follow the selected language culture instead of forcing a Dutch or system-default date.
- Fixed the Chinese clipboard diagnosis language entries, which still contained English report labels.
- Shortened the displayed build label to `Syscalculator 2.0 build yyyy.MM.dd.xxx`.

**Additions:**
- Added `Tiedragon.ClipboardConvert` as a reusable ClipboardConvert API group for copy, paste, conversion, diagnosis and report generation.
- Added clipboard diagnosis tools in WizardExpress advanced mode with Excel-like cell preview and a structured details view.
- Compared with Syscalculator 1.74, WizardExpress is now expanded beyond conversion with a new clipboard diagnosis workflow.
- Added clipboard report API support for summary, formula, conversion details, before/after data, preview and clipboard formats.
- Added HTML support report output for e-mail clients.
- Added PDF diagnosis report output with Tiedragon logo, Dutch formal date and multi-page page template.
- Added MathML/HTML formula rendering reuse in reports so WizardExpress, NOD Editor style preview, e-mail and PDF stay visually closer together.
- Added PDF embedding of a rendered formula image when MathML is available, with fallback text rendering when capture is not possible.
- Added localized clipboard diagnosis/report labels through the `.lng` language files.
- Routed clipboard diagnosis and report texts through the language system so PDF, e-mail and details views can follow the selected UI language.
- Added per-language report date culture and format keys for clipboard diagnosis output.
- Added API documentation direction for reusable groups such as `Tiedragon.Graph2D`, `Tiedragon.ToolEditor`, `Tiedragon.NodSystem.Core` and `Tiedragon.ClipboardConvert`.

#### 2.0.2026.05.02 Daily Build

**Date:** 2026-05-02

**Fixes:**
- Changed changelog to daily build grouping
- Added build/changelog batch procedure.
- Restored discoverability for newer euro converter files that were missing from the old `broncode.zip` release set.
- Documented `WizardExpress` clipboard behavior: works in Notepad and LibreOffice Calc, while Microsoft Excel remains a special Office clipboard case because Excel-specific `BIFF` formats are replaced by the classic clipboard reset/write-back flow.

**Additions:**
- Added separate additions list support
- Daily build changelog format: one section per day with fixes and additions.
- Added expanded euro converter set: BGN, CYP, EEK, HRK, LTL, LVL, MTL, SIT and SKK.

### Beta Releases

#### 2.0.2026.05.16 Beta 1

**Date:** 2026-05-16

**Release type:** Beta 1

**Upgrade path:**
- Replaces the old public Syscalculator 1.72 download as the current Syscalculator 2.0 Beta 1 test build.
- Keeps a single fixed beta download name: `syscal20beta1.exe`.

**Fixes:**
- Promoted the tested daily line to the first Syscalculator 2.0 Beta 1 release.
- Fixed Help maintainability by moving Help code into the separate `Tiedragon.Help` project.
- Fixed hardcoded Help content by loading editable HTML, CSS, JavaScript, SVG and content resources at runtime.
- Fixed NOD Editor help artwork by replacing the bitmap screenshot mock with a sharp SVG mock.
- Fixed architecture and Help diagrams so names are shorter, connector labels are removed and arrows stay attached to colored groups.
- Fixed writer-facing Help content organization with clear examples for warnings, notices, details blocks, tables, screenshots and command links.
- Fixed Graph Preview and mini preview range markers so X/Y fit, line markers and zoom state stay consistent
- Fixed mini preview recalculation drift by separating sampling range from viewport
- Added localized graph line marker label for all UI languages

**Additions:**
- Added Markdown architecture documentation for the overall software architecture, Help architecture and NOD system architecture.
- Added SVG architecture diagrams for Syscalculator, Help and NOD system layers.
- Added editable Help content groups for main Help, NOD full Help, NOD command Help, popup Help and snippets.
- Added language-key based Help content support with English fallback behavior.
- Added SVG-based Help mock support so localized screenshots can be generated from editable vector text instead of maintaining multiple PNG files.
- Added reusable Help rendering helpers for templates, topic content, text lookup and screenshot embedding.
- Published the beta installer as `Syscalculator-2.0-beta-2.0.2026.05.16.exe`.
- Updated the fixed beta download link to `https://www.tiedragon.com/files/syscal20beta1.exe`.
- Updated the NL fixed beta download link to `https://tiedragon.nl/downloads/syscal20beta1.exe`.

**Main Beta 1 features since Syscalculator 1.72:**
- Modern C# / .NET 10 WinForms rebuild instead of the old VB6 application line.
- Separate `NodSystem.Core` engine for parsing, calculation, tracing and converter execution.
- NOD Editor with tabs, syntax highlighting, validation, search/replace and line numbers.
- Live formula preview, converter simulator and metadata preview for NOD files.
- Graph Preview for numeric/math converters, including zoom, pan and point table.
- Editable Help system with runtime HTML/CSS/JavaScript/SVG resources.
- Multi-language UI updates for Dutch, English, German, French, Italian, Spanish, Portuguese, Indonesian and Chinese.
- Legacy NOD compatibility for commands such as `chg`, `trans` and `math`.

**Known limitations:**
- This is a tester-ready beta, not a production release.
- Solver/formula animation remains experimental.
- SQL connector and 3D graph work remain future/prototype areas.
- Target machines need the .NET 10 Desktop Runtime x64 and may need the Microsoft Edge WebView2 Runtime.

#### 2.0.2026.05.02 Beta Release

**Date:** 2026-05-02 12:11

**Fixes:**
- Defined beta release changelog branch

**Additions:**
- Added daily beta production channel support

### Production Releases

Nog geen production release entry voor deze dag.

**Status:** beta in voorbereiding  
**Technologie:** C# / .NET 10 / WinForms  
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
- Oude VB6-.NET conversiepoging is vervangen door een gecontroleerde herbouw.

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
**Technologie:** C# / .NET 10 / WinForms  
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
- automatische VB6-.NET conversiepoging leverde te veel ballast op

### Syscalculator 1.74

Onderhoudslijn voor legacy.

#### Syscalculator 1.74 RC2 Legacy Help Release

**Date:** 2026-05-09

**Status:** RC2 / legacy release candidate

**Package type:** VB6 legacy application and separate Inno Setup installer

**Fixes:**
- Fixed the 1.74 Help menu opening problem where `hh.exe` could start hidden because `SW_SHOWNORMAL` was not defined in the VB6 API modules.
- Connected Syscalculator 1.74 language files to their own localized CHM help files.
- Fixed localized CHM table-of-contents structure so each subject is a separate article instead of one long index page.
- Fixed missing localized help screenshots and added a warning icon for the limitations/support articles.
- Updated the 1.74 installer build so RC2 packages include all localized CHM help files.
- Hardened the VB6 compile script by moving old `.vbw` workspace files aside during command-line builds.

**Validation:**
- Legacy test runner: 25 passed, 0 failed.
- VB6 command-line compile completed for the main app and editor.
- Inno Setup 6.7.1 installer build completed.
- The real VB6 Help menu command was verified to open `Syscalculator 1.74 Help - Nederlands` visibly through `hh.exe`.

#### Syscalculator 1.74 RC1 Legacy Maintenance Release

**Date:** 2026-05-07

**Status:** RC1 / planned final 1.x legacy release

**Package type:** VB6 legacy application and separate Inno Setup installer

**Fixes:**
- Fixed first-run configuration fallback so the installed converter catalog and language files are found on Windows 7 and later.
- Fixed the startup/configuration failure that could show `Configuration is not found` after installation on newer Windows versions.
- Fixed the old Windows API error 52 risk on Windows 7 or newer by preventing invalid path results from breaking startup.
- Fixed installer language handoff so Dutch setup starts Syscalculator with `ned.lng` and English setup starts with `/lng eng.lng`.
- Fixed the 1.74 installer language coverage so Spanish and Catalan setup also start Syscalculator with `esp.lng` and `cat.lng`.
- Added Spanish and Catalan 1.74 help pages and linked F1/help menu entries to those pages.
- Expanded the Spanish and Catalan 1.74 help pages with practical daily-use guidance, editing actions, WizardExpress notes, NOD catalog information and legacy limitations.
- Expanded the English and Dutch 1.74 help pages with a practical daily-use quick guide for the main window, editing, clipboard copy commands, NOD catalogs and VB6 legacy context.
- Added a CHM help project and build script for the 1.74 help, with runtime fallback to the loose HTML help files when the compiled CHM is not present.
- Fixed legacy branding references from `tcsoftware` to `Tiedragon`.
- Fixed the main legacy Edit menu so Select All is available with Ctrl+A for the active input/output field.
- Fixed the main legacy Edit menu so Copy, Cut and Delete act on the selected text instead of the whole field.

**Additions:**
- Added the newer euro adopters to the legacy `euro` group: BGN, CYP, EEK, HRK, LTL, LVL, MTL, SIT and SKK.
- Added a modern separate Inno Setup installer for Syscalculator 1.74.
- Added VB6 runtime detection to the 1.74 installer, with a link to the official Microsoft runtime download when `msvbvm60.dll` is missing.
- Documented that WizardExpress works best with older Office/Word versions, that LibreOffice Calc was tested and works ok, and that Microsoft 365 / Office 365 is not reliable with this old VB6 clipboard integration.
- Updated the 1.74 readme to explain this release as the last legacy 1.x release before the modern Syscalculator 2.0 line.

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
