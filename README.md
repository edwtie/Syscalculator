# Syscalculator 2.0 Daily Source

Syscalculator is de nieuwe C#/.NET lijn van de oude Syscalculator/NOD software. De repository bevat nu meer dan alleen de NOD-engine: de WinForms app, Tiedragon graph-libraries, de nieuwe language-package toolchain, help/HTML tooling, updater en de onderhoudslijn voor Syscalculator 1.74.

De daily-lijn is bedoeld als actieve ontwikkelbron voor Beta 2. Belangrijke onderdelen zijn al beschikbaar, maar daily blijft een testkanaal.

Historisch loopt de lijn terug naar Tiedos/Nodelistomzetter, Nodomzet en
Hong-technologie uit 1997: gegevens converteren of omzetten als kernidee. Zie
`docs/SYSCALCULATOR_HISTORY.md` voor de teruggevonden Wayback-bron en de relatie
met MoneyCalculator, Syscalculator 1.74 en Syscalculator 2.0.

## Wat zit erin

- NOD 1.0 compatibility voor bestaande converters, zoals `chg`, `trans` en `math ans + - * /`.
- NOD 2.0 math met `ans`, `e`, `pi`, `ln`, `log`, `abs`, `sqrt`, `pow`, trigonometrie, modulo en numerieke calculus.
- Equation/solver basis met `given`, `equation`, `solve` en `constraint`.
- NOD Data als model voor `table`, `field`, `phoneformat`, `output`, `preview`, `backup` en `lookup`.
- Graph2D en Graph3D via Tiedragon graph-libraries.
- Graph3D/geometry basis voor X/Y/Z-ruimte, 2D/3D-modus, grids, assen, labels, puntentabel, rode punten, vectorpijlen, camera/navigator, zoom, rotatie en kompas.
- Language Package 1.0 met `.lngpdk`, SHA-256 controle, quality gates, ToolEditor en HTML-help/media beheer.
- Syscalculator laadt taal, help, NOD-help en formulekaart nu package-aware uit gebundelde `.lngpdk` packages, met fallback naar ingebouwde bestanden.
- WinForms UI voor Syscalculator 2.0 daily/beta/production.
- Test- en buildscripts voor daily, beta, production en de oude 1.74-lijn.

## Ontwikkellijnen

```text
Syscalculator 1.74        VB6 onderhoudslijn voor NOD 1.0 legacy fixes
Syscalculator 2.0 Daily   actieve C#/.NET ontwikkellijn
Syscalculator 2.0 Beta 2  volgende beta met Graph3D/geometry en Language Package 1.0
```

Zie `docs/SYSCALCULATOR_1_74_MAINTENANCE.md` voor de onderhoudsregels van de oude VB6-lijn. Zie `docs/RELEASE_PLAN.md` voor de releaseplanning van 1.74, 2.0 Daily, Beta en Production.

## Projecten

```text
src/syscalculator              WinForms app voor Syscalculator 2.0
src/Tiedragon.NodSystem.Core   NOD parser, engine, math, equation en data core
src/Tiedragon.Graph            gedeelde graph-basis, formatting, pijlen en overlay-stijl
src/Tiedragon.Graph.2D         Graph2D API en WinForms-rendering, namespace Tiedragon.Graph.G2D
src/Tiedragon.Graph.3D         Graph3D API, projectie, grid, camera, kompas, namespace Tiedragon.Graph.G3D
src/Tiedragon.Help             gedeelde Help/HTML helpers
src/Tiedragon.LanguagePackage  zelfstandige compiler/validator voor .lngpdk taalpackages
src/Tiedragon.ToolEditor       zelfstandige ToolEditor-app voor taalpackages, help-HTML en media
src/Tiedragon.ClipboardConvert clipboard/data conversie helpers
src/Syscalculator.Updater      updater helper
src/NodSystem.Demo             demo-console
src/NodSystem.Tests            test-console zonder NuGet testframework
src/Syscalculator.UI.Uwp       UWP placeholder/experiment
```

De actuele architectuur staat in:

- `docs/TECHNICAL_DETAILS.md`
- `docs/ARCHITECTURE_INDEX.md`
- `docs/SYSCALCULATOR_HISTORY.md`
- `docs/TIEDRAGON_GRAPH_ARCHITECTURE.svg`
- `docs/LANGUAGE_PACKAGE_DESIGN.md`
- `docs/LNGPDK_PACKAGE_MODEL.md`
- `docs/LANGUAGE_PACKAGE_TOOL.md`

## Graph2D en Graph3D

Graph Preview is de 2D graph-workflow. Graph3D bouwt hierop door met dezelfde gebruikerslogica waar dat kan: bereik, stap, lijnen, grid, tabel, punten en grote preview.

De graph-namespaces zijn:

```text
Tiedragon.Graph       gedeelde graph primitives
Tiedragon.Graph.G2D   Graph2D public API en renderer
Tiedragon.Graph.G3D   Graph3D public API, projectie, camera, grid en compass
```

Graph3D is in daily beschikbaar als foundation voor 3D-geometrie. Echte surface plots zoals `z = f(x,y)` blijven roadmapwerk en worden gevolgd in GitHub issue #8: "Roadmap: continue 3D graph and surface visualization for advanced NOD math".

## Language Package 1.0

De oude losse `.lng` aanpak groeit door naar `.objpdk` bronpackages en `.lngpdk` distributiepackages. Een package bevat taalteksten, HTML-help, NOD-help, formulekaarten, toegestane media en package metadata. Daily publiceert de gecompileerde `.lngpdk` mee onder `LanguagePackages`, zodat Syscalculator direct de packageversie kan gebruiken.

Kort verschil:

- `.lng` is een los tekstbestand met alleen vertaalregels in `key=value` vorm, bijvoorbeeld menu's, knoppen en korte UI-teksten.
- `.objpdk` is het bewerkbare bronpakket voor ToolEditor, generatoren en AI-agenten. Hierin zitten templates, HTML-bronnen, media en conceptwerk.
- `.lngpdk` is het gecompileerde taalpakket. Het bevat `language/<code>.lng` plus definitieve HTML-help, NOD-help, formulekaarten, media, manifestmetadata, SHA-256-controles en eventueel signing.
- `.lng` blijft fallback en snelle handmatige compatibiliteit. `.objpdk` is authoring. `.lngpdk` is de releasevorm voor Daily/Beta met validatie en quality gates.

Voordelen/nadelen:

- `.lng` is eenvoudig te lezen en snel handmatig te corrigeren, maar bevat geen help, media, metadata of sterke distributiecontrole. Grote teksten en HTML-help worden in `.lng` lastig te onderhouden, omdat je dan lange regels, escaping en losse link/media-afspraken krijgt.
- `.objpdk` houdt help, templates en media onderhoudbaar, maar is geen releasebestand. `.lngpdk` houdt alles bij elkaar en voorkomt ontbrekende help/media door validatie, maar vraagt een compileerstap en wordt geweigerd als de package niet klopt.

De package toolchain controleert onder andere:

- geen mojibake in `.lng`, HTML, JSON of docs;
- geen onbekende bestanden in packages;
- geen losse concepttekst in release-help;
- werkende interne links;
- bestaande en previewbare afbeeldingen;
- alleen allowlisted JavaScript: `basis.js`, `main-help.js`, `nod.js`,
  `nod-popup.js`, `formula.js`;
- SHA-256 checks voor package en payload.

Alle gegenereerde taalpackages in `web/packages/languages` zijn nu compleet en actief:

```text
deu, eng, fra, ind, ita, ned, por, spa, zho
```

ToolEditor is de zelfstandige editor voor deze packages. Daarmee kunnen vertaling, HTML-help, NOD-help, formulekaart en media beheerd worden zonder handmatig door pakketbestanden te zoeken.

`language.cfg` kan naast het oude taalbestand ook het package aanwijzen:

```ini
language=ned.lng
languagePackage=ned
```

Als het package ontbreekt of wordt geweigerd, valt Syscalculator terug op de losse `.lng` en ingebouwde helpbestanden.

## Build

WinForms app bouwen:

```bash
dotnet build src/syscalculator/Syscalculator.UI.WinForms.csproj
```

Language Package compiler bouwen:

```bash
dotnet build src/Tiedragon.LanguagePackage/Tiedragon.LanguagePackage.csproj
```

ToolEditor bouwen:

```bash
dotnet build src/Tiedragon.ToolEditor/Tiedragon.ToolEditor.csproj
```

## Run

Syscalculator starten op Windows:

```bash
dotnet run --project src/syscalculator
```

Demo-console:

```bash
dotnet run --project src/NodSystem.Demo
```

Tests:

```bash
dotnet run --project src/NodSystem.Tests
```

Verwachte testuitkomst:

```text
All tests passed.
```

## Packages

Taalpackages opnieuw genereren:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/Generate-LanguagePackages.ps1
```

Gebruik PowerShell 7 of nieuwer voor language-package scripts. Oude Windows
PowerShell 5.1 wordt geweigerd, omdat encoding anders te makkelijk mojibake in
`.lng`, HTML of JSON veroorzaakt.

Een package valideren:

```bash
dotnet run --project src/Tiedragon.LanguagePackage -- validate web/packages/languages/Syscalculator.Language.ned.lngpdk
```

De public index staat in:

```text
web/packages/languages/language-packages.json
```

## Installers

Syscalculator 2.0 gebruikt Inno Setup 6 via `installer/Syscalculator.iss`.
De installer-build gebruikt PowerShell 7, genereert de taalpackages opnieuw,
publiceert ze onder `LanguagePackages` en schrijft `languagePackage=...` in
`language.cfg`.

```bat
BUILD_INSTALLER.bat daily
BUILD_INSTALLER.bat beta
BUILD_INSTALLER.bat production
```

Output:

```text
artifacts\installer
```

In VS Code kunnen dezelfde builds via Terminal > Run Task worden gestart:

```text
build installer daily
build installer beta
build installer production
```

De oude VB6-lijn heeft een aparte installer via `installer/Syscalculator174.iss`:

```bat
BUILD_SYSCALCULATOR174_INSTALLER.bat -Channel rc2
```

Output:

```text
artifacts\legacy\installer
```

Zie `docs/INNO_SETUP_INSTALLER.md` voor installer-details.

## NOD functies

Voorbeelden van moderne NOD math:

```nod
math sin(ans)
math sind(ans)
math log(ans,2)
math mod(ans,2)
math ans % 2
diff ans^2
integral 0,1 ans^2
limit 0 sin(ans)/ans
```

Calculation Trace kan tussenstappen bewaren en terugrekenen:

```csharp
NodEngine.ConvertForwardWithTrace(...)
NodEngine.ConvertReverseFromTrace(...)
```

Complexe formules blijven expliciete `reverse` regels nodig hebben wanneer automatische reverse niet veilig genoeg is.

## Dependencies

NuGet package versions worden centraal beheerd in `Directory.Packages.props`.

Huidige externe package:

```text
Microsoft.Web.WebView2  WinForms HTML/help/formula preview support
```

De meeste code blijft in interne project references. De test-console blijft bewust licht zonder extern testframework.

## Changelog

Zie `CHANGELOG.md` voor de overgang van Syscalculator 1.72 naar de moderne Syscalculator 2.0-lijn.
