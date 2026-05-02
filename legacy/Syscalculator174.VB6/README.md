# Syscalculator 1.74 VB6 Legacy Source

Dit is een veilige broncode-extractie uit `broncode.zip` voor onderhoud aan de oude Syscalculator 1.x-lijn.

## Doel

- `Project1.vbp` is het oude VB6 hoofdproject, exact uit de zip.
- `Syscalculator174.local.vbp` is een opgeschoonde lokale kopie om makkelijker in de VB6 IDE te openen.
- `SyscalEditor.vbp` is het oude editorproject.
- Deze map is alleen bedoeld voor Syscalculator 1.74 onderhoud en NOD 1.0 legacy-vergelijking.
- Syscalculator 2.0 blijft in `src/` en gebruikt C#/.NET.

## Veiligheid

De oude zip bevat ook uitvoerbestanden en oude build/output-mappen. Die zijn bewust niet uitgepakt.
Als later toch oude bestanden in `Output/` zijn toegevoegd, behandel die als archief en voer ze niet uit.

Niet doen:

- Geen bestanden uit `corr/Output` uitvoeren.
- Geen bestanden uit `Output/` uitvoeren.
- Geen oude `.exe`, cracks, installers of onbekende binaries starten.
- Geen VB6-code blind terugmengen in Syscalculator 2.0.

Wel doen:

- Alleen bronbestanden lezen en vergelijken.
- Bugfixes voor 1.74 apart houden.
- Gedrag uit VB6 gebruiken als referentie voor NOD 1.0 compatibiliteit.

## VB6 IDE / SDK status

Visual Basic 6.0 is geen moderne ondersteunde ontwikkelomgeving meer. De VB6 IDE en Visual Studio 6.0 IDE zijn sinds 8 april 2008 niet meer ondersteund door Microsoft. De runtime is een ander verhaal: bestaande VB6-apps kunnen nog draaien via de 32-bit runtime/WOW64 op ondersteunde Windows-versies, maar nieuw onderhoud in de IDE blijft legacy-werk.

Praktisch advies:

- Gebruik bij voorkeur een aparte VM voor VB6 onderhoud.
- Gebruik een gelicenseerde Visual Basic 6.0 / Visual Studio 6.0 installatie.
- Installeer Visual Basic 6.0 Service Pack 6 als dat nodig is voor de oude ontwikkelomgeving.
- Houd Syscalculator 1.74 bugfix-only; nieuwe features horen in Syscalculator 2.0.

## Bekende projectopmerking

`Project1.vbp` bevatte oorspronkelijk een oud absoluut/relatief pad:

```text
Form=..\Program Files\Microsoft Visual Studio\VB98\WizardExpress.frm
```

In deze extractie staat `WizardExpress.frm` lokaal in dezelfde map. Zowel `Project1.vbp` als `Syscalculator174.local.vbp` verwijzen nu naar de lokale file.

## Windows 7+ padfix

De oude code gebruikte op meerdere plekken `App.Path` voor `freesyscal.cfg`. Dat kan op Windows 7 en hoger fout gaan wanneer Syscalculator onder `Program Files` of een andere beschermde map draait.

Onderhoudsaanpassing:

- `freesyscal.cfg` wordt gelezen/geschreven via `%APPDATA%\Syscalculator`.
- Taalbestanden en meegeleverde `.nod`-bestanden blijven uit de programmamap komen.
- `[App]` in taalbestanden wordt opgelost via de programmamap.
- De helperlogica staat in `WindowsAPI.bas`: `GetDataFolder`, `UserDataFilePath`, `ResolveProgramFile`.

Dit moet oude fouten zoals Error 52 door ongeldige of beschermde bestandsnamen verminderen.

## Compatibility Test Matrix

Voor 1.74 candidate naar release:

```text
Windows XP 32-bit
Windows 7 32/64-bit
Windows 11 64-bit
```

Controle:

- start zonder crash
- converterlijst opent
- `freesyscal.cfg` werkt via `%APPDATA%\Syscalculator` op Windows 7/11
- oude euro `.nod` bestanden en nieuwe euro-bestanden openen
- geen NOD 2.0 syntax in 1.74 bestanden

## Branding

Zichtbare oude `Tcsoftware` teksten in menu's, taalbestanden en About zijn vervangen door `Tiedragon`.
De oude registry key `SOFTWARE\TCsoftware\Syscalculcator Euro Edition` blijft bewust staan voor compatibiliteit met bestaande 1.x instellingen.

## Versie

De VB6 projectbestanden staan nu op `1.74.1`:

- `Project1.vbp`: `Syscalculator 1.74`, output `Syscalculator174.exe`
- `Syscalculator174.local.vbp`: lokale onderhoudskopie, ook `1.74.1`
- `SyscalEditor.vbp`: `SyscalEditor 1.74`, output `SyscalEditor174.exe`

## Windows 11 Startup Candidate Fix

Nieuwste Windows 11 installaties kunnen starten zonder oude HKLM-registrywaarden of schrijfbare programmamap. `Form1.frm` maakt daarom nu bij ontbrekende `freesyscal.cfg` altijd eerst een gebruikersconfiguratie aan via `%APPDATA%\Syscalculator`.

Status: bronfix/candidate. Nog testen en compileren met VB6 voordat dit als 1.74 release beschreven wordt.

Compile-check:

```bat
BUILD_SYSCALCULATOR174_VB6_CANDIDATE.bat
```

Als VB6 niet automatisch gevonden wordt:

```bat
BUILD_SYSCALCULATOR174_VB6_CANDIDATE.bat -Vb6Path "C:\Program Files (x86)\Microsoft Visual Studio\VB98\VB6.EXE"
```

## Onderhoudscheck

Gecontroleerd:

- Projectverwijzingen in `Project1.vbp`, `Syscalculator174.local.vbp` en `SyscalEditor.vbp` wijzen naar bestaande bestanden.
- Autostart gebruikt nu `App.EXEName` in plaats van het oude hardcoded `Freesyscal.exe`.
- WizardExpress schrijft de symbool-instelling naar `HKEY_CURRENT_USER` in plaats van `HKEY_LOCAL_MACHINE`.

Nog opletten in VB6:

- Meerdere oude routines gebruiken nog `Open ... As #1`; bij geneste file-I/O kan dat later beter naar `FreeFile`.
- De oude HKLM-migratiecode blijft aanwezig om 1.x-instellingen te lezen, maar nieuwe schrijfacties moeten bij voorkeur naar HKCU.
- Test vooral taalwissel, config laden, WizardExpress en autostart in Windows 7+.

## Inhoud

Zie `EXTRACT_MANIFEST.txt` voor de lijst met uitgepakte veilige bronbestanden.
