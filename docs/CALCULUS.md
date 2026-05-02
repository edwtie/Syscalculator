# NOD System 2.0 Calculus — fix

Calculus hoort onder `math`.

## Voorkeurssyntax

```nod
math diff ans
math diff ans^2
math integral 0,1 ans^2
math limit 0 sin(ans)/ans
```

## Voorbeelden

### Afgeleide van ans

```nod
Name Afgeleide van ans
format ##.00
math diff ans
end
```

Bij elke invoer:

```text
d/dx x = 1
```

### Afgeleide van ans^2

```nod
Name Afgeleide
format ##.00
math diff ans^2
end
```

Bij invoer `3`:

```text
d/dx x^2 bij x=3 ≈ 6
```

### Integraal

```nod
Name Integraal
format ##.00
math integral 0,1 ans^2
end
```

Uitkomst:

```text
∫ van 0 tot 1 x^2 dx ≈ 0,3333
```

### Limiet

```nod
Name Limiet
format ##.00
math limit 0 sin(ans)/ans
end
```

Uitkomst:

```text
lim x→0 sin(x)/x ≈ 1
```

## Technische fix

`ParseIntegral` en `ParseLimit` staan nu als helper-methoden in `NodParser.cs`.

`ParseMath(...)` controleert eerst:

```text
TryParseCalculusMath(...)
```

Daarna pas legacy math / NOD 2.0 expression math.

## Belangrijk

Dit is numerieke calculus, geen symbolische CAS.
