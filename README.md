# Syscalculator Production Source

Deze branch is de production-lijn van Syscalculator. Production is bedoeld als
stabiele bron voor een geteste publieke release. Nieuwe ontwikkeling begint in
Daily, wordt samengebracht in Beta, en komt pas daarna in Production.

Syscalculator bestaat nu uit vier duidelijke wegen:

```text
Syscalculator 2.0 Daily       actieve ontwikkel- en testlijn
Syscalculator 2.0 Beta        prerelease voor testers
Syscalculator 2.0 Production  stabiele publieke 2.0-lijn
Syscalculator 1.74            VB6 legacy-onderhoudslijn
```

De vierde weg, Syscalculator 1.74, is dus geen oude tekst in deze README maar
een aparte onderhoudslijn voor bestaande VB6/NOD 1.0 gebruikers. Die lijn blijft
alleen voor legacy fixes, CHM-help, installeronderhoud en compatibiliteit.

Production richt zich op Syscalculator 2.0:

- WinForms app voor converteren, calculator, NOD Editor en help.
- NOD 1.0 compatibility voor bestaande converters zoals `chg`, `trans` en `math ans + - * /`.
- NOD 2.0 Math met `ans`, `e`, `pi`, `ln`, `log`, `abs`, `sqrt`, `pow`, trigonometrie en auto-reverse.
- NOD 2.0 Equation met `given`, `equation`, `solve` en `constraint`.
- NOD Data als model voor `table`, `field`, `phoneformat`, `output`, `preview`, `backup` en `lookup`.
- Testbare engine-lagen voor parser, berekening, trace en document/data-transformaties.

## Ontwikkellijnen

```text
Daily      = snelste ontwikkellijn, mag breken tijdens werk
Beta       = geteste prerelease voor gebruikers/testers
Production = stabiele publieke 2.0-lijn
1.74       = aparte VB6 onderhoudslijn voor oude installaties
```

Zie `docs/SYSCALCULATOR_1_74_MAINTENANCE.md` voor de onderhoudsregels van de
1.74 legacy-lijn. Zie `docs/GIT_RELEASE_BRANCH_WORKFLOW.md` voor de branch- en
releaseworkflow.

## Projecten

```text
src/Syscalculator.UI.WinForms  production-app
src/NodSystem.Core             NOD parser, engine en modellen
src/NodSystem.Demo             demo-console
src/NodSystem.Tests            simpele test-console zonder NuGet
src/Syscalculator.UI.Uwp       UWP placeholder/experiment
```

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


## Syscalculator

WinForms UI-project voor de beta:

```text
src/Syscalculator.UI.WinForms
```

Starten op Windows:

```bash
dotnet run --project src/Syscalculator.UI.WinForms
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

Dit is een UI-prototype bovenop `NodSystem.Core`.

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
