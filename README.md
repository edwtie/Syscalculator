# NOD System 2.0 Full Tested Source

Deze broncode werkt het NOD-systeem verder uit met:

- NOD 1.0 legacy: `chg`, `trans`, `math ans + - * /`
- NOD 2.0 Math: complexe sommen met `ans`, `e`, `pi`, `ln`, `log`, `abs`, `sqrt`, `pow`
- NOD 2.0 Equation: `given`, `equation`, `solve`, `constraint`
- NOD Data: `table`, `field`, `phoneformat`, `output`, `preview`, `backup`, `lookup` als model
- In-memory DataTransformEngine voor testbare veldtransformaties
- Graph2D en Graph3D als Tiedragon graph-libraries
- Testproject zonder externe packages

## Ontwikkellijnen

```text
Syscalculator 1.74 = VB6 onderhoudslijn voor NOD 1.0 legacy fixes
Syscalculator 2.0 beta 1 (preview) = C#/.NET opvolger met NOD 1.0 compatibility en NOD 2.0 functies
```

Zie `docs/SYSCALCULATOR_1_74_MAINTENANCE.md` voor de onderhoudsregels voor de oude VB6-lijn.
Zie `docs/RELEASE_PLAN.md` voor de releaseplanning van 1.74, 2.0 Daily, Beta en Production.
Zie `docs/ARCHITECTURE_INDEX.md` voor de actuele softwarearchitectuur, Help-architectuur en NOD systeem-architectuur in Markdown met SVG-tekeningen.

## Projecten

```text
src/Tiedragon.NodSystem.Core   NOD parser, engine, math, equation en data core
src/Tiedragon.Graph            gedeelde graph-basis, formatting, pijlen en overlay-stijl
src/Tiedragon.Graph.2D         Graph2D API en WinForms-rendering, namespace Tiedragon.Graph.G2D
src/Tiedragon.Graph.3D         Graph3D API, projectie, grid, camera en kompas, namespace Tiedragon.Graph.G3D
src/Tiedragon.Help             gedeelde Help/HTML helpers
src/Tiedragon.ToolEditor       zelfstandige ToolEditor-app voor taalpackages, help-HTML en media
src/Tiedragon.ClipboardConvert clipboard/data conversie helpers
src/NodSystem.Demo             demo-console
src/NodSystem.Tests            simpele test-console zonder NuGet
src/syscalculator              WinForms app voor Syscalculator 2.0 daily/beta/production
src/Syscalculator.Updater      updater helper
src/Syscalculator.UI.Uwp       UWP placeholder/experiment
```

Zie ook `docs/TIEDRAGON_GRAPH_ARCHITECTURE.svg` voor de actuele Tiedragon Graph architectuur.

## Run demo

```bash
dotnet run --project src/NodSystem.Demo
```

## Run tests

```bash
dotnet run --project src/NodSystem.Tests
```

Verwachte testuitkomst:

```text
All tests passed.
```

## NuGet dependencies

NuGet package versions are managed centrally in `Directory.Packages.props`.

Current external package:

```text
Microsoft.Web.WebView2  WinForms HTML/help/formula preview support
```

Most code stays in internal project references. The test console remains without external packages unless a future test genuinely needs one.

## Ontwerpregel

- NOD 1.0 `MathStep10` accepteert alleen echte getallen.
- Alles met `e`, `pi`, `^`, `ln`, `log`, `abs`, `sqrt` gaat naar NOD 2.0 expression parser.
- Complexe vergelijkingen gebruiken `mode equation`.
- Database/document/field-operaties blijven aparte lagen, zodat NOD eenvoudig blijft.


## Trigonometrie en auto-reverse

Deze versie ondersteunt ook:

```text
sin/cos/tan
asin/acos/atan
sind/cosd/tand
asind/acosd/atand
rad/deg
```

En simpele automatische reverse:

```text
sind(ans) -> asind(ans)
ans * e^2 -> ans / e^2
```

Complexe formules blijven expliciete `reverse` nodig hebben.


## Calculation Trace

Deze versie bevat NOD Calculation Trace:

```csharp
NodEngine.ConvertForwardWithTrace(...)
NodEngine.ConvertReverseFromTrace(...)
```

Daarmee kan Syscalculator 2.0 terugrekenen via opgeslagen tussenstappen.
Dit maakt complexe sommen praktischer zonder volledige algebra-oplosser.


## Calculation Trace: log en trig

Auto-reverse ondersteunt nu ook:

```text
log(ans,2)  -> pow(2,ans)
ln(ans)     -> exp(ans)
sin(ans)    -> asin(ans)
tan(ans)    -> atan(ans)
sind(ans)   -> asind(ans)
tand(ans)   -> atand(ans)
rad(ans)    -> deg(ans)
```

Let op: bij trigonometrie geeft de inverse functie de hoofdwaarde.


## Enterprise prototype modules

Toegevoegd:

```text
SqlPreviewGenerator.cs
ReportBuilder.cs
EnterpriseModel.cs
DocumentBatchEngine.cs
```

Deze modules maken SQL-preview, previewrapporten, safety context en documentbatch op in-memory tekst mogelijk.


## Syscalculator 2.0 Beta 1 Preview

WinForms UI-project voor Syscalculator 2.0 beta 1 (preview):

```text
src/syscalculator
```

Starten op Windows:

```bash
dotnet run --project src/syscalculator
```

Onderdelen:

```text
MainForm
WizardExpressForm
NodEditorForm
CatalogManagerForm
TraceViewerForm
NodCatalogService
NodUiMetadata
```

Dit is de WinForms UI bovenop `Tiedragon.NodSystem.Core` en de gedeelde Tiedragon graph/help/tooling-libraries.

Graph3D is beschikbaar in de daily-lijn als native foundation voor X/Y/Z-ruimte, camera, grids, vectorpijlen en puntvisualisatie. Echte surface sampling zoals `z = f(x,y)` is nog roadmapwerk en wordt gevolgd in GitHub issue #8: “Roadmap: continue 3D graph and surface visualization for advanced NOD math”.

## Installers

Syscalculator 2.0 beta 1 (preview) gebruikt Inno Setup 6 via `installer/Syscalculator.iss`.
De build publiceert eerst de WinForms-app voor `win-x64` en maakt daarna de installer.

```bat
BUILD_INSTALLER.bat daily
BUILD_INSTALLER.bat beta
BUILD_INSTALLER.bat production
```

Output:

```text
artifacts\installer
```

In VS Code kunnen dezelfde builds via **Terminal > Run Task** worden gestart:

```text
build installer daily
build installer beta
build installer production
```

De oude VB6-lijn heeft een aparte Inno Setup installer via `installer/Syscalculator174.iss`.
Deze installer gebruikt een eigen installatiemap en eigen output, zodat hij los staat van Syscalculator 2.0 beta 1 (preview).

```bat
BUILD_SYSCALCULATOR174_INSTALLER.bat -Channel rc2
```

Output:

```text
artifacts\legacy\installer
```

Voor de oude VB6-lijn zijn er aparte 1.74 RC2 VS Code-taken:

```text
test Syscalculator 1.74 legacy
build Syscalculator 1.74 Euro NOD RC2
compile Syscalculator 1.74 VB6 RC2
build Syscalculator 1.74 installer RC2
validate Syscalculator 1.74 RC2
```

Zie `docs/INNO_SETUP_INSTALLER.md` voor de installer-details.

## Changelog

Zie `CHANGELOG.md` voor de overgang van Syscalculator 1.72 naar Syscalculator 2.0 Alpha 1.


## Modulo

Toegevoegd:

```nod
math mod(ans,2)
math rem(ans,2)
math ans % 2
math ans mod 2
```

Modulo is niet automatisch omkeerbaar.


## Calculus

Toegevoegd:

```nod
diff ans^2
integral 0,1 ans^2
limit 0 sin(ans)/ans
```

Dit is numeriek, niet symbolisch.

## Vergelijking met Syscalculator 1.72

Zie `docs/COMPARE_WITH_SYSCALCULATOR_1_72.md` voor de vergelijking tussen de oude VB6-source en de nieuwe C#/.NET 2.0-lijn.
