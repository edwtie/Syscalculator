# NOD System 2.0 Full Tested Source

Deze broncode werkt het NOD-systeem verder uit met:

- NOD 1.0 legacy: `chg`, `trans`, `math ans + - * /`
- NOD 2.0 Math: complexe sommen met `ans`, `e`, `pi`, `ln`, `log`, `abs`, `sqrt`, `pow`
- NOD 2.0 Equation: `given`, `equation`, `solve`, `constraint`
- NOD Data: `table`, `field`, `phoneformat`, `output`, `preview`, `backup`, `lookup` als model
- In-memory DataTransformEngine voor testbare veldtransformaties
- Testproject zonder externe packages

## Ontwikkellijnen

```text
Syscalculator 1.74 = VB6 onderhoudslijn voor NOD 1.0 legacy fixes
Syscalculator 2.0 beta 1 (preview) = C#/.NET opvolger met NOD 1.0 compatibility en NOD 2.0 functies
```

Zie `docs/SYSCALCULATOR_1_74_MAINTENANCE.md` voor de onderhoudsregels voor de oude VB6-lijn.
Zie `docs/RELEASE_PLAN.md` voor de releaseplanning van 1.74, 2.0 Daily, Beta en Production.

## Projecten

```text
src/NodSystem.Core   library
src/NodSystem.Demo   demo-console
src/NodSystem.Tests  simpele test-console zonder NuGet
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

Dit is een UI-prototype bovenop `NodSystem.Core`.

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
BUILD_SYSCALCULATOR174_INSTALLER.bat -Channel rc1
```

Output:

```text
artifacts\legacy\installer
```

Voor de oude VB6-lijn zijn er aparte 1.74 RC1 VS Code-taken:

```text
test Syscalculator 1.74 legacy
build Syscalculator 1.74 Euro NOD RC1
compile Syscalculator 1.74 VB6 RC1
build Syscalculator 1.74 installer RC1
validate Syscalculator 1.74 RC1
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
