# Vergelijking oude Syscalculator 1.72-source vs Syscalculator 2.0 prototype

## Korte samenvatting

Syscalculator 1.72 was de laatste oude VB6-lijn. De 2.0-lijn is geen gewone update, maar een moderne herbouw in C#/.NET.

```text
Syscalculator 1.72
= VB6-applicatie
= UI en engine sterk verweven
= NOD-parser/uitvoering direct gekoppeld aan forms/modules

Syscalculator 2.0
= C#/.NET 8
= losse NodSystem.Core engine
= WinForms UI-laag
= tests, documentatie en uitbreidbare architectuur
```

---

## 1. Taal en platform

| Onderdeel | Syscalculator 1.72 | Syscalculator 2.0 |
|---|---|---|
| Programmeertaal | Visual Basic 6 | C# |
| Runtime | VB6 runtime | .NET 8 |
| UI | VB6 Forms | WinForms |
| Projectvorm | klassieke VB6-projectstructuur | meerdere .NET-projecten |
| Testbaarheid | beperkt | aparte testprojecten mogelijk |
| Windows-focus | Windows 9x/XP/7-achtig | moderne Windows/.NET |

---

## 2. Oude broncode-onderdelen

In de oude 1.72-lijn zaten onderdelen zoals:

```text
Form1.frm
WizardExpress.frm
editor.frm
Form3.frm
Form4.frm
tray.frm
frmAbout.frm
Sysmsgbox.frm
Module1.bas
Freesyscal.bas
Languare.bas
WindowsAPI.bas
freesyscal.cfg
*.nod
```

In de 2.0-lijn zijn die conceptueel verdeeld over:

```text
src/NodSystem.Core
src/Syscalculator.UI.WinForms
src/NodSystem.Tests
src/NodSystem.Demo
docs/
examples/
```

---

## 3. Architectuurverschil

### 1.72

De oude lijn werkte grofweg zo:

```text
VB6 Form1
  ↓
OpenNOD / Getans / globale arrays
  ↓
math / chg / trans
  ↓
resultaat terug naar UI
```

Veel toestand zat in globale variabelen of module-level arrays.

Voorbeelden van oude concepten:

```text
Smath()
chgo()
chgn()
transo()
transn()
vraag1 / vraag2
symbool1 / symbool2 / symbool3 / symbool4
Appsnaam
```

### 2.0

De nieuwe lijn werkt zo:

```text
.nod tekst
  ↓
NodTextNormalizer
  ↓
NodParser
  ↓
NodDocument
  ↓
NodEngine
  ↓
NodResult / NodTraceResult
```

UI en engine zijn gescheiden:

```text
NodSystem.Core
= parser, documentmodel, engine, math, reverse, trace, data, SQL-preview

Syscalculator.UI.WinForms
= MainForm, NOD Editor, WizardExpress, Calculator, tray, catalogus
```

---

## 4. Mapping van oude bestanden naar nieuwe bestanden

| Oud 1.72-bestand/concept | Nieuwe 2.0-tegenhanger |
|---|---|
| Form1.frm | MainForm.cs |
| WizardExpress.frm | WizardExpressForm.cs |
| editor.frm / Zeditor.frm | NodEditorForm.cs |
| Form3.frm / Form4.frm | CatalogManagerForm.cs |
| tray.frm | NotifyIcon-code in MainForm.cs |
| frmAbout.frm | About_Click in MainForm.cs |
| Sysmsgbox.frm | MessageBox / IntroDialogForm.cs |
| Freesyscal.bas | NodCatalog.cs / NodEngine.cs |
| Module1.bas | verdeeld over NodSystem.Core |
| Languare.bas | nog niet volledig gemigreerd |
| WindowsAPI.bas | meestal niet meer nodig in .NET |
| freesyscal.cfg | behouden via NodCatalogService |
| .nod-bestanden | behouden en uitgebreid |

---

## 5. NOD 1.0-compatibiliteit

Oude NOD-regels blijven belangrijk:

```nod
Name Celsius naar Fahrenheit
input1 Celsius
input2 Fahrenheit
Symb3 C
Symb4 F
format ##.00
math ans * 1,8
math ans + 32
end
```

2.0 ondersteunt opnieuw:

```text
Name
input1
input2
Symb1
Symb2
Symb3
Symb4
format
math
chg
trans
end
```

Ook oude geplakte of kapotte line endings worden hersteld via:

```text
NodTextNormalizer.cs
```

---

## 6. Nieuwe NOD 2.0-mogelijkheden

Syscalculator 2.0 gaat verder dan 1.72.

Nieuw of sterk uitgebreid:

```text
e en pi
log / ln
sin / cos / tan
asin / acos / atan
gradenfuncties zoals sind/cosd/tand
mod / rem / %
diff
integral
limit
auto-reverse
calculation trace
equation mode
data/field mode
SQL-preview
report prototype
document batch prototype
```

Voorbeelden:

```nod
math ans * e^2
math pi * ans^2
math log(ans,2)
math sind(ans)
math mod(ans,2)
math diff ans^2
math integral 0,1 ans^2
math limit 0 sin(ans)/ans
```

---

## 7. Oude UI vs nieuwe UI

### Oude 1.72-look

De oude UI had:

```text
klassieke Windows-look
toolbar met WizardExpress, calculator/toetsenbord, open NOD
File-combobox
input1/input2 velden
symbolen naast velden
Digit group
Decimals
trayfunctie
WizardExpress
calculator
NOD editor
```

### Nieuwe 2.0 UI

De nieuwe UI heeft nu opnieuw:

```text
MainForm met toolbar
WizardExpress-knop
Calculator-knop
Open NOD-knop
File-combobox
input1/input2
radio buttons voor richting
Symb1/Symb2/Symb3/Symb4
Digit group
Decimals
Live convert
Trace
tray support
```

Daarnaast heeft de nieuwe NOD Editor meer ontwikkeltool-functies:

```text
tabs
syntax highlighting
zoek/vervang
live preview
simulator preview
detachable simulator tool window
introductie-preview
line numbers
statusbar
```

---

## 8. indoprint / indoend

Oude NOD-bestanden konden introductietekst bevatten:

```nod
indoprint Old currency unit will be ended at 1 January 2002.
indoprint All of your amounts must convert to euro before that date.
indoend
```

In 2.0 is dit opnieuw toegevoegd:

```text
NodUiMetadata.IntroLines
IntroDialogForm.cs
Editor preview
MainForm toont intro vóór gebruik
```

Belangrijk ontwerpprincipe:

```text
indoprint hoort bij een losse introductiedialoog,
niet in het convertervenster zelf.
```

---

## 9. Tray-vergelijking

### Oude 1.72

De oude lijn had een `tray.frm`-achtig concept.

### Nieuwe 2.0

Tray support zit nu in `MainForm.cs`:

```text
NotifyIcon
ContextMenuStrip
OnResize
OnFormClosing
HideToTray()
RestoreFromTray()
```

Tray-menu:

```text
Toon Syscalculator
Verberg naar systeemvak
WizardExpress
Calculator
Afsluiten
```

---

## 10. Calculator/toetsenbord

### Oude 1.72

De calculator was een apart hulpmiddel, bereikbaar via toolbar.

### Nieuwe 2.0

Nieuwe `CalculatorForm.cs`:

```text
cijfers
+ - * /
sqrt
1/x
%
MR / MC / M+ / MS
Copy
Use
Use+Close
```

Nieuw gedrag:

```text
als input1 actief is → calculator stuurt waarde naar input1
als input2 actief is → calculator stuurt waarde naar input2
```

---

## 11. WizardExpress

### Oude 1.72

WizardExpress werkte als hulpmiddel voor snelle/batchconversie.

### Nieuwe 2.0

`WizardExpressForm.cs`:

```text
clipboard/inputgebied
regel-voor-regel conversie
vooruit/terug
output kopiëren
```

Nog later te verbeteren:

```text
meer oude WizardExpress-layout
kolommen/cellen beter visualiseren
batch-preview
foutregels markeren
```

---

## 12. NOD Editor

### Oude 1.72

De oude editor was vooral een eenvoudige NOD-editor.

### Nieuwe 2.0

De nieuwe editor is meer een ontwikkelomgeving:

```text
tabbladen
line numbers
syntax highlighting
zoek/vervang
live metadata-preview
simulator-preview
detachable simulator
introductie-preview
automatische line repair
URLN-preview
```

Dit is functioneel veel verder dan de oude editor, maar moet qua look nog verder naar de klassieke Syscalculator-stijl worden gebracht als dat gewenst is.

---

## 13. Wat is nog niet volledig gemigreerd?

Nog niet volledig 1-op-1 overgezet:

```text
oude taalbestanden / language system
oude MenuXP-stijl
alle oude instellingen
alle oude toolbarbeelden exact
alle oude foutmeldingen
alle oude WizardExpress-details
oude WindowsAPI-specifieke functies
installer
helpbestand
volledige import van alle oude .nod-bestanden
```

---

## 14. Belangrijkste winst van 2.0

De oude 1.72-source was functioneel, maar moeilijker te onderhouden omdat UI en engine verweven waren.

De 2.0-lijn heeft nu:

```text
losse engine
betere parser
NOD-normalisatie
tests
documentatie
moderne math
trace
reverse
editor-tooling
tray
klassieke UI-elementen
```

Hierdoor kan Syscalculator later doorgroeien naar:

```text
Syscalculator 2.0 Alpha
Syscalculator 2.0 Beta
Syscalculator 2.0 Release
```

---

## 15. Conclusie

Syscalculator 2.0 is geen simpele port van 1.72.

Het is:

```text
1. een reconstructie van de oude NOD-gedachte
2. een moderne herbouw in C#/.NET
3. een uitbreiding van het oude systeem
4. een voorbereiding op een echte nieuwe Syscalculator-generatie
```

De oude 1.72-lijn blijft belangrijk als historische bron en referentie voor look, gedrag en compatibiliteit. De nieuwe 2.0-lijn maakt het systeem onderhoudbaar, testbaar en uitbreidbaar.
