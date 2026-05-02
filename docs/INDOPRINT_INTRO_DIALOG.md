# indoprint / indoend intro dialog

Oude NOD-bestanden kunnen een introductietekst bevatten:

```nod
indoprint Old currency unit will be ended at 1 January 2002.
indoprint All of your amounts must convert to euro before that date.
indoprint
indoend
```

## Betekenis

```text
indoprint = regel in introductiedialoog
indoend   = einde van introblok
```

## UI-gedrag

Wanneer een converter wordt geopend en `indoprint`-regels bevat:

```text
MainForm toont IntroDialogForm vóór gebruik van de converter.
```

De titel komt van:

```text
URLN, anders Name
```

## Editor preview

De NOD Editor live preview toont nu ook het introblok.
