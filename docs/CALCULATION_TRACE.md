# NOD Calculation Trace

Calculation Trace is het rekengeheugen van NOD System 2.0.

## Doel

Niet elke complexe formule moet als algebra worden opgelost. De engine kan tijdens vooruit rekenen de stappen bewaren:

```text
start = 3
step 1: ans * e^2
step 2: ans + 10
step 3: sqrt(ans)
```

Daarna kan terugrekenen via dezelfde stappen achteruit:

```text
sqrt(ans) -> ans^2
ans + 10  -> ans - 10
ans * e^2 -> ans / e^2
```

## Voorbeeld

```nod
Name Trace demo
format ##.00
math ans * e^2
math ans + 10
math sqrt(ans)
end
```

Vooruit:

```text
3 -> 5.67
```

Terug via trace:

```text
5.67 -> ongeveer 3.00
```

## Belangrijk

Trace werkt het beste binnen dezelfde sessie, omdat de engine de stappen kent.

Zonder trace geldt nog steeds:

- simpele automatische reverse kan
- expliciete `reverse` kan
- complexe algebra wordt niet automatisch gegokt
