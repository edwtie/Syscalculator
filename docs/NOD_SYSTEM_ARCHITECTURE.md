# NOD systeem-architectuur

![NOD systeem-architectuur](NOD_SYSTEM_ARCHITECTURE.svg)

## Doel

Het NOD systeem zet `.nod` tekst om naar een gestructureerd document en voert
dat document daarna uit via de juiste route: math, tekst, data, vergelijking,
calculus, trace of rapportage.

De kern staat in `src/Tiedragon.NodSystem.Core` en is los van de UI.

## Hoofdlagen

| Laag | Verantwoordelijkheid |
|---|---|
| Invoer en UI | Editor, convertercatalogus, app workflows, resources, demo en tests. |
| Parser en model | Normaliseren, parsen en opslaan als `NodDocument`. |
| Uitvoering | `NodEngine` kiest de juiste route en roept sub-engines aan. |
| Resultaten | Resultaattekst, numerieke waarde, veldtransformatie, equation result, trace, stappenrapport, SQL preview en graph info. |

## Parserflow

```text
.nod tekst
  -> NodTextNormalizer
  -> NodParser
  -> NodDocument
  -> NodEngine
```

`NodParser` leest onder andere:

```text
name
format
mode
input
output
reverse
chg
trans
math
table
field
phoneformat
lookup
given
equation
solve
constraint
diff
integral
limit
end
```

## Engine routes

| Route | Gebruikte onderdelen |
|---|---|
| Math | `MathStep10`, `NodExpressionEvaluator`, `NodReverse`, `CalculationTrace`. |
| Tekst | `chg` en `trans` regels voor prefix- en waardeomzetting. |
| Data | `DataTransformEngine`, `DataPlanDescriber`, `SqlPreviewGenerator`. |
| Vergelijking | `EquationEngine`, `EquationDefinition`, `ConstraintRule`. |
| Calculus | `CalculusEngine`, `CalculusStep`, `CalculusOperation`. |
| Uitleg | `SolverStepBuilder`, `ReportBuilder`, `SolverStepReport`. |

## Resultaten

| Resultaat | Gebruik |
|---|---|
| `NodResult` | Conversieresultaat met tekst en optionele numerieke waarde. |
| `NodTraceResult` | Resultaat met tussenstappen voor terugrekenen of uitleg. |
| `FieldTransformResult` | Resultaat van data-/veldtransformatie. |
| `EquationResult` | Opgeloste variabele, waarde en uitleg. |
| `SolverStepReport` | Stappen, MathML, rekenkaart en graph informatie. |
| `NodPreviewReport` | Previewrapport voor data/document workflows. |

## Ontwerpafspraken

- Parser voert geen conversies uit; parser bouwt alleen het documentmodel.
- `NodDocument` bevat data, maar geen UI.
- `NodEngine` is de centrale uitvoerder.
- Sub-engines blijven klein en gericht.
- Legacy NOD 1.0 blijft ondersteund naast NOD 2.0 expressies.
- Tests gebruiken dezelfde core als de app.
