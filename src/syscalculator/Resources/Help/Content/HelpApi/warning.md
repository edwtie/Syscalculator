# Waarschuwing

Gebruik een waarschuwing alleen als de gebruiker echt iets verkeerd kan doen.

## Gewone Help

Voor bestanden onder `main`:

```html
<div class="warning">
  <b>[help.warning.title]</b> [help.warning.text]
</div>
```

## NOD-Help

Voor bestanden onder `nod/full` of `nod/command`:

```html
<div class="warning-sign">
  <div><b>[help.warning.title]</b> [help.warning.text]</div>
</div>
```

De tekst staat in `.lng`:

```ini
help.warning.title=Let op:
help.warning.text=Gebruik deze instelling alleen wanneer je weet wat je doet.
```
