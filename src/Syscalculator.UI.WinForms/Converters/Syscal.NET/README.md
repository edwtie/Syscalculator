# Syscal.NET Experiment

Deze map is een oude automatische VB6-naar-.NET conversiepoging.

Status:

- Niet actief.
- Niet bouwen.
- Niet gebruiken als Syscalculator 2.0 broncode.
- Alleen bewaren als historisch vergelijkingsmateriaal.

## Onderzoek

Deze map komt uit een automatische Visual Studio 2008 VB6-upgrade van `Project1.vbp`.
De upgrade report meldt direct **24 compile errors**. Daarnaast staan er zeer veel `UPGRADE_WARNING`, `UPGRADE_ISSUE` en `UPGRADE_TODO` opmerkingen in de gegenereerde `.vb` bestanden.

Belangrijkste problemen:

- Afhankelijk van oude `Microsoft.VisualBasic.Compatibility`.
- Afhankelijk van `Microsoft.VisualBasic.PowerPacks`, inclusief oude shape/line-controls.
- VB6 `Load` / `Unload` is niet goed geconverteerd.
- VB6 `App.Title`, `App.PrevInstance`, `App.TaskVisible` is niet goed geconverteerd.
- Win32 API-declaraties met `As Any`, `VarPtr`, `LenB`, `CopyMemory`, `SendMessage` zijn niet correct gemigreerd.
- Clipboard-code is niet goed geconverteerd.
- Veel default properties uit VB6 zijn niet opgelost, waardoor gedrag stil anders kan zijn.
- Control arrays, `ItemData`, `ScaleMode`, `Line`, `Cls`, twips/pixels en layoutgedrag zijn onzeker.
- De map bevat oude `bin/` en `obj/` output uit het experiment.

Conclusie:

Deze code is geen bruikbare basis voor Syscalculator 2.0. Gebruik de VB6-bron in `legacy/Syscalculator174.VB6` als historische waarheid en gebruik de huidige C# code als echte opvolger.

De echte Syscalculator 2.0 code staat in:

- `src/NodSystem.Core`
- `src/Syscalculator.UI.WinForms`

De onderhouden VB6 1.74 bron staat in:

- `legacy/Syscalculator174.VB6`
