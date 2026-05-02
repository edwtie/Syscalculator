# Editor live preview and URLN

Toegevoegd aan NOD Editor Plus:

```text
Live preview pane
URLN support
input1/input2 live display
Symb1/Symb2/Symb3/Symb4 live display
format display
sample output based on test input
```

## URLN

`URLN` wordt gebruikt als dialoog-/vensternaam wanneer aanwezig.

Voorbeeld:

```nod
Name Celsius naar Fahrenheit
URLN Celsius converter
input1 Celsius
input2 Fahrenheit
Symb3 C
Symb4 F
format ##.00
math ans * 1,8
math ans + 32
end
```

Preview:

```text
Dialoog: Celsius converter
Input1: Celsius
Input2: Fahrenheit
Voorbeeld: Celsius: 22C
Resultaat: Fahrenheit: 71.60F
```

Als `URLN` ontbreekt, wordt `Name` gebruikt.
