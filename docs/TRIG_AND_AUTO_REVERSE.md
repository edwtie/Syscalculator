# Trigonometrie en automatisch terugrekenen

Toegevoegd aan NOD 2.0:

## Radialen

```nod
math sin(ans)
math cos(ans)
math tan(ans)
math asin(ans)
math acos(ans)
math atan(ans)
```

## Graden

```nod
math sind(ans)
math cosd(ans)
math tand(ans)
math asind(ans)
math acosd(ans)
math atand(ans)
```

## Omrekenen

```nod
math rad(ans)   // graden naar radialen
math deg(ans)   // radialen naar graden
```

## Automatisch reverse

Voor simpele één-op-één functies kan de engine automatisch terugrekenen:

```text
sin(ans)  -> asin(ans)
sind(ans) -> asind(ans)
rad(ans)  -> deg(ans)
deg(ans)  -> rad(ans)
ln(ans)   -> exp(ans)
exp(ans)  -> ln(ans)
```

Ook simpele vormen:

```text
ans * e^2 -> ans / e^2
ans / e^2 -> ans * e^2
ans + 32  -> ans - 32
ans - 32  -> ans + 32
```

Voor complexe formules blijft een expliciete `reverse`-regel nodig.
