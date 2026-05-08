# NOD Legacy 1.74 vs NodSystem Core

Analyse van de oude VB6 Syscalculator 1.74 NOD-bron tegenover de huidige `Tiedragon.NodSystem.Core`.

## Hoofdconclusie

De huidige `Tiedragon.NodSystem.Core` dekt de normale NOD 1.0 conversies uit de oude VB6-bron:

- `name`
- `input1`
- `input2`
- `format`
- `math ans +|-|*|/ number`
- `chg old,new`
- `trans old,new`
- oude metadata zoals `urln`, `symb*`, `result`, `resfou`, `errres`, `indoprint`, `indoend`

Daarbovenop ondersteunt de moderne core NOD 2.0 functies die de oude VB6 niet kende:

- expression math zoals `ans * e^2`, `sqrt(ans)`, `log(ans,2)`, `sind(ans)`
- automatic/trace reverse voor simpele expressies
- calculus: `diff`, `integral`, `limit`
- equation mode
- data/sql transform mode
- formula cards en solver step reports

## Parser Vergelijking

Oude VB6:

- `OpenNOD` leest regel voor regel.
- Herkent commando's met `Left$`/`UCase`.
- Onbekende regels worden alleen streng afgekeurd bij status `3`; normaal gebruik is tolerant.
- Data wordt opgeslagen in globale arrays: `Smath`, `chgo/chgn`, `transo/transn`.

Nieuwe core:

- `NodParser.Parse` normaliseert tekst eerst.
- Bouwt een `NodDocument`.
- Scheidt oude `MathStep10` van nieuwe NOD 2.0 expressies.
- Is strenger bij onbekende commando's, maar accepteert de bekende oude metadata als no-op.

Risico:

- Oude `.nod` bestanden met eigen onbekende metadata kunnen in de moderne parser falen.
- Voor de bekende 1.74 bron is dit geen probleem.

## Engine Vergelijking

Oude VB6 `Getans` kiest een pipeline:

```text
math -> chg -> trans
```

Zodra `math` aanwezig is, worden `chg` en `trans` niet uitgevoerd. De moderne `NodEngine.ConvertForward` volgt dezelfde prioriteit:

```text
LegacyMathSteps -> MathExpressions20 -> CalculusSteps -> ChangeRules -> TranslateRules
```

Dit is compatibel met NOD 1.0 gedrag.

## Math

Oude VB6:

- Alleen simpele stappen: `math ans * 1,8`.
- Reverse wordt automatisch gedaan door stappen omgekeerd terug te lopen.
- Decimale komma/punt wordt handmatig aangepast met `ConvertPunt`, `Convertkomma`, `kommaPunt`, `nulnul`.

Nieuwe core:

- `MathStep10` bewaart dezelfde oude `+ - * /` stappen.
- Reverse van oude math loopt ook automatisch terug.
- `ParseFlexibleDecimal` ondersteunt komma en punt.
- NOD 2.0 expressies ondersteunen meer functies, maar hebben meestal expliciete reverse of trace nodig.

Compatibel:

- Celsius/Fahrenheit en euro/afstand/massa/volume/temperatuur converters uit 1.74 passen in het moderne model.

## chg en trans

Oude VB6:

- `chg` is prefixvervanging.
- `trans` is exacte tekstvergelijking, case-insensitive.
- Er is oude code voor `chg`, maar de meegeleverde 1.74 `.nod` bron gebruikt vooral `math`; moderne Decibel/tekst converters gebruiken later `chg/trans`.

Nieuwe core:

- `chg` blijft prefix-compatible.
- Extra: slimme compacte matching voor telefoonnummers/spaties/interpunctie.
- Extra: patroonregels met `x` captures.
- `trans` is exact plus compacte fallback.

Let op:

- Nieuwe `chg` regels kunnen meer dan VB6 1.74. Niet terugporten naar 1.74 als exact oud gedrag vereist is.

## Bestandsdekking

Oude legacy `.nod` bestanden in `legacy/Syscalculator174.VB6` gebruiken deze commando's:

```text
indoprint, math, result, input2, resfou, end, errres, input1,
name, format, indoend, urln, symb1..4, symba2
```

Moderne converters in `src/syscalculator/Converters` bevatten daarnaast:

```text
chg, trans, mode, equation, given, solve, constraint
```

Verschil in bestanden:

- Alleen legacy: `21.nod`, `21w.nod`
- Alleen modern: extra euro-landen, math demo's, text/Decibel demo's, `celsius_fahrenheit.nod`

## Aanbevolen Bugfix/Test Werk

1. Voeg een test toe die alle oude `.nod` files uit `legacy/Syscalculator174.VB6` door `NodParser.Parse` haalt.
2. Voeg golden tests toe voor enkele oude converters:
   - `Temperature/graden Fahrenheit.nod`: `22 -> 71.6`
   - `euro/NLG.nod`: gulden/euro koers
   - `Distance/km miles.nod`
3. Maak een expliciete compatibility policy:
   - NOD 1.0 files blijven tolerant.
   - NOD 2.0 files mogen strengere fouten geven.
4. Overweeg parser-waarschuwingen in plaats van harde fouten voor onbekende metadataregels in legacy mode.

## Status

De bestaande testset dekt al veel NOD 2.0 en legacy gedrag. De belangrijkste ontbrekende test is een brede parse-scan over alle echte VB6 1.74 `.nod` bestanden.

