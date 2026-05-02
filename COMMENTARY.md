# Source commentary

Deze versie bevat extra commentaar in de belangrijkste bestanden.

## Belangrijkste bestanden

| Bestand | Rol |
|---|---|
| `NodDocument.cs` | Modelclasses voor NOD-document, math, chg/trans, equation en data |
| `NodParser.cs` | Leest `.nod`-regels en scheidt NOD 1.0 van NOD 2.0 |
| `NodExpressionEvaluator.cs` | Voert complexe NOD 2.0 math uit |
| `NodEngine.cs` | Hoofdengine voor conversie |
| `EquationEngine.cs` | Eerste equation/solve-laag |
| `DataTransformEngine.cs` | In-memory data/field-transformaties |
| `NodSystem.Tests/Program.cs` | Tests zonder externe packages |
| `NodSystem.Demo/Program.cs` | Demo-uitvoer |

## Kernregel

```text
NOD 1.0 = simpel, historisch, betrouwbaar
NOD 2.0 = uitgebreid, maar veilig gescheiden
```

## Belangrijkste bugfix

```text
math ans * e^2
```

mag niet naar `MathStep10`. Het moet naar de NOD 2.0 expression parser.

Alleen dit is NOD 1.0 legacy math:

```text
math ans * 1,8
math ans / 2,20371
```
