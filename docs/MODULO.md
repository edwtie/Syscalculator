# Modulo / restwaarde

NOD System 2.0 ondersteunt nu modulo.

## Functie

```nod
math mod(ans,2)
```

Voorbeeld:

```text
9 mod 2 = 1
```

## Alias

```nod
math rem(ans,2)
```

## Operator

```nod
math ans % 2
```

## Oude leesbare stijl

Ook toegestaan:

```nod
math ans mod 2
```

Intern wordt dit gelezen als:

```nod
math mod(ans,2)
```

## Belangrijk voor terugrekenen

Modulo is meestal niet veilig automatisch terug te rekenen.

Voorbeeld:

```text
9 mod 2 = 1
7 mod 2 = 1
5 mod 2 = 1
```

Daarom komt `mod` niet in automatische reverse. Als terugrekenen nodig is, moet de maker expliciet context of een andere formule geven.
