# Wat Niet Doen

Liever niet:

- Zet geen eigen `<script>` in helpteksten.
- Gebruik geen ingewikkelde HTML als een gewone alinea genoeg is.
- Gebruik geen waarschuwing voor gewone tips.
- Verander `{NodEditorScreenshot}` niet in tekst; dat vult het programma zelf.
- Zet geen vertaling in `class="..."`; `class` is alleen opmaak.

Goed:

```html
<p class="shot-caption">[help.example.screenshot_caption]</p>
```

Niet goed:

```html
<p class="[help.example.screenshot_caption]">Tekst</p>
```
