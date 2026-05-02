# Calculator target input

De Calculator-knop weet nu welk veld actief is.

## Gedrag

```text
focus op input1 -> Calculator stuurt resultaat naar input1
focus op input2 -> Calculator stuurt resultaat naar input2
geen focus -> standaard input1
```

## Calculator-knoppen

```text
Use       = zet displaywaarde in actief veld
Use+Close = zet displaywaarde in actief veld en sluit calculator
Copy      = kopieert waarde naar clipboard
```

## Technisch

```text
MainForm._calculatorTargetTextBox
TextBox.Enter events
CalculatorForm(Action<string> sendResult, string? initialValue)
```
