# NOD Editor line repair

Probleem uit screenshot:

```text
Name Celsius naar Fahrenheitinput1 Celsiusinput2 FahrenheitSymb3 C...
```

De editor zag dit als één lange regel.

## Oorzaak

Oude `.nod`-bestanden of gegenereerde bestanden kunnen line endings missen of commands aan elkaar plakken.

## Fix

`NodEditorForm` heeft nu:

```text
NormalizeNodText(...)
RepairConcatenatedNod(...)
Tools > Regels herstellen
```

Bij openen wordt automatisch geprobeerd te herstellen als de tekst op één regel staat maar meerdere NOD-commando's bevat.

## Voorbeeld

Van:

```text
Name Celsius naar Fahrenheitinput1 Celsiusinput2 Fahrenheitformat ##.00math ans * 1,8math ans + 32end
```

Naar:

```nod
Name Celsius naar Fahrenheit
input1 Celsius
input2 Fahrenheit
format ##.00
math ans * 1,8
math ans + 32
end
```
