# Syscalculator 1.74 Maintenance

Syscalculator 1.74 is de onderhoudslijn voor de oude VB6/NOD 1.0 codebase.
Deze lijn is bedoeld voor stabiliteit en compatibiliteit, niet voor grote nieuwe functies.

## Doel

- Bestaande Syscalculator 1.x gebruikers blijven ondersteunen.
- Oude `.nod` bestanden blijven exact werken.
- Kleine fouten herstellen zonder het gedrag van NOD 1.0 te veranderen.
- Syscalculator 2.0 vrijhouden voor nieuwe NOD 2.0 functies.

## Bron

De historische VB6-bron staat in `broncode.zip`.
Gebruik deze zip alleen gericht als referentie. Pak niet blind alles uit, omdat de map oude output/archiefbestanden bevat die niets met de onderhoudsbron te maken hebben.

Belangrijke VB6-bestanden:

```text
Project1.vbp
Form1.frm
Module1.bas
Freesyscal.bas
Languare.bas
WizardExpress.frm
editor.frm
EditorMod1.bas
```

Belangrijke legacy engine-functies:

```text
OpenNOD
trans
chg
math
transMath
ChgMath
MathExecute
Convertkomma
ConvertPunt
puntDigit
nulnul
```

## Onderhoudsregels

- Geen NOD 2.0 syntax toevoegen aan 1.74.
- Geen UI-herbouw in VB6.
- Geen gedrag wijzigen dat bestaande `.nod` bestanden kan breken.
- Fixes moeten klein, uitlegbaar en terug te testen zijn.
- 1.74 blijft VB6/NOD 1.0.
- 2.0 blijft C#/.NET/NOD 2.0 met legacy compatibility.

## Kandidaten Voor Fixes

- Windows 11 startup controleren wanneer oude HKLM-registrywaarden ontbreken en `freesyscal.cfg` nog niet in AppData staat.
- `chg` en `trans` legacy-volgorde vergelijken met Syscalculator 2.0.
- Decimalen, komma/punt en digit grouping controleren.
- Taalbestanden nalopen op kapotte tekens.
- Windows-compatibiliteit controleren bij paden en configuratiebestanden.
- WizardExpress VB6-output vergelijken met de nieuwe WizardExpress.
- Oude editor-functies alleen fixen als ze bestanden beschadigen of verkeerd opslaan.

## Niet Doen

- Geen oude output-bestanden, installers of archiefrommel uit `corr/Output` gebruiken.
- Geen verdachte oude `.exe` bestanden aanraken of uitvoeren.
- Geen brede refactor van VB6.
- Geen productienaam of 2.0-beta branding terugporten naar 1.74.

## Releasevorm

```text
Syscalculator 1.74.x = onderhoud / legacy fix
Syscalculator 2.0 beta = moderne opvolger
```

### Euro NOD Candidate

Nieuwe euro-landen kunnen eerst als data-only candidate voor 1.74 worden klaargezet. Dit verandert geen VB6-code en voegt geen NOD 2.0 syntax toe.

Noem dit pas een release nadat de 1.74-bron getest en gecompileerd is.

Gebruik:

```bat
BUILD_SYSCALCULATOR174_EURO_NOD_CANDIDATE.bat
```

De batch maakt:

```text
artifacts/legacy/Syscalculator174-EuroNodCandidate/
artifacts/legacy/Syscalculator174-EuroNodCandidate.zip
```

Deze candidate bewaart de oude groepen uit `broncode.zip`:

```text
Distance
euro
Mass
Pressure
Temperature
Volume
```

En voegt de nieuwere euro-`.nod` bestanden toe:

```text
BGN
CYP
EEK
HRK
LTL
LVL
MTL
SIT
SKK
```

Compile-stap voor release:

```text
legacy/Syscalculator174.VB6/Syscalculator174.local.vbp
```

Als `VB6.EXE` niet beschikbaar is, blijft dit een candidate en geen release.

Compile-check via batch:

```bat
BUILD_SYSCALCULATOR174_VB6_CANDIDATE.bat
```

Of met expliciet VB6-pad:

```bat
BUILD_SYSCALCULATOR174_VB6_CANDIDATE.bat -Vb6Path "C:\Program Files (x86)\Microsoft Visual Studio\VB98\VB6.EXE"
```

De batch gebruikt de klassieke VB6 command line compile:

```text
VB6.EXE /MAKE Syscalculator174.local.vbp /OUT artifacts\legacy\vb6-compile\Syscalculator174.compile.log
```

### Windows 11 Startup Fix Candidate

De 1.74-bron bevat een candidate-fix voor starten op nieuwere Windows 11 installaties:

- lees `first` eerst uit HKCU en pas daarna uit HKLM
- behandel ontbrekende `Migration` als eerste start wanneer `freesyscal.cfg` ontbreekt
- maak `freesyscal.cfg` automatisch in `%APPDATA%\Syscalculator`

Ook hiervoor geldt: eerst testen en `Syscalculator174.local.vbp` compileren voordat dit release mag heten.

De changelog moet per fix duidelijk vermelden:

```text
Lijn: 1.74 maintenance
Bestand:
Probleem:
Fix:
Compatibiliteitsrisico:
Test:
```
