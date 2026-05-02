# Automatic NOD normalization

Deze versie normaliseert `.nod`-tekst automatisch vóór parsing.

## Nieuwe core class

```text
NodTextNormalizer.cs
```

## Waar wordt dit gebruikt?

```text
NodParser.Parse(...)
NodCatalogService.LoadDocument(...)
MainForm.LoadConverter(...)
NodEditorForm.LoadTextFromFile(...)
```

## Effect

Een oud/geplakt bestand zoals:

```text
Name Celsius naar Fahrenheitinput1 Celsiusinput2 Fahrenheitformat ##.00math ans * 1,8math ans + 32end
```

wordt automatisch hersteld naar:

```nod
Name Celsius naar Fahrenheit
input1 Celsius
input2 Fahrenheit
format ##.00
math ans * 1,8
math ans + 32
end
```

## Belangrijk

De normalisatie gebeurt nu vóórdat het bestand in de parser komt.
Daardoor werkt het niet alleen in de editor, maar ook bij gewoon laden vanuit de converterlijst.
