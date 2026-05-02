# Calculation Trace: log en trigonometrie

Deze versie breidt automatische reverse uit.

## Logaritmen

```nod
math log(ans,2)
```

Vooruit:

```text
8 -> 3
```

Terug via trace:

```text
3 -> pow(2,3) -> 8
```

Reverse-regel intern:

```text
log(ans,2) -> pow(2,ans)
```

## Natuurlijke logaritme

```text
ln(ans) -> exp(ans)
exp(ans) -> ln(ans)
```

## Trigonometrie radialen

```text
sin(ans) -> asin(ans)
cos(ans) -> acos(ans)
tan(ans) -> atan(ans)
```

Let op: trigonometrie kan meerdere oplossingen hebben. De inverse functies geven de hoofdwaarde.

## Trigonometrie graden

```text
sind(ans) -> asind(ans)
cosd(ans) -> acosd(ans)
tand(ans) -> atand(ans)
```

## Omrekenen graden/radialen

```text
rad(ans) -> deg(ans)
deg(ans) -> rad(ans)
```

## Belangrijk

Calculation Trace is bedoeld voor direct terugrekenen in dezelfde sessie. Het is geen volledige algebra-oplosser.
