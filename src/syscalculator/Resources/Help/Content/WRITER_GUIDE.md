# Helptekst Bewerken Voor Schrijvers

Je hoeft geen programmeur te zijn om de helptekst te verbeteren.
Denk aan Wikipedia-bron bewerken: je ziet blokken met kopjes, alinea's en lijstjes.

Wil je waarschuwingen, tips, uitklapblokken of codevoorbeelden gebruiken?
Zie dan [WRITER_TOOLS.md](WRITER_TOOLS.md).

## Een Gewone Alinea

In een helpbestand zie je bijvoorbeeld:

```html
<p>[help.main.nodeditor.intro]</p>
```

Dat betekent:

- `<p>` en `</p>` maken er een gewone alinea van.
- `[help.main.nodeditor.intro]` is de naam van de tekst.
- De echte tekst staat in een taalbestand, bijvoorbeeld `ned.lng` of `eng.lng`.

In `ned.lng` staat dan bijvoorbeeld:

```ini
help.main.nodeditor.intro=De NOD Editor is de werkruimte voor converters.
```

Wil je de woorden verbeteren? Verander alleen de tekst achter `=`.

## Wat Laat Je Staan?

Laat deze dingen meestal staan:

```html
<p>
</p>
<h2>
</h2>
<ul>
<li>
class="shot-caption"
```

Dat is de vorm van de pagina: alinea, titel, lijst, onderschrift.

Laat ook dit staan:

```html
{NodEditorScreenshot}
{WizardExpressScreenshot}
```

Daar vult het programma zelf iets in.

## Commentaar Voor Schrijvers

Dit zie je in het bestand, maar niet op de help-pagina:

```html
<!-- Dit is alleen een notitie voor de schrijver. -->
```

Voorbeeld:

```html
<!-- Screenshotdeel -->
<h2>[help.section.screenshot]</h2>

{NodEditorScreenshot}

<p class="shot-caption">[help.main.nodeditor.screenshot_caption]</p>
```

## Nieuwe Tekst Toevoegen

Stap 1: zet in het HTML-bestand een nieuwe alinea:

```html
<p>[help.main.nodeditor.extra_tip]</p>
```

Stap 2: zet in `eng.lng` de Engelse standaardtekst:

```ini
help.main.nodeditor.extra_tip=Use short examples when explaining a command.
```

Stap 3: zet in `ned.lng` de Nederlandse tekst:

```ini
help.main.nodeditor.extra_tip=Gebruik korte voorbeelden wanneer je een command uitlegt.
```

Als een vertaling nog ontbreekt, gebruikt het programma tijdelijk Engels.

## Handige Tags

Gewone alinea:

```html
<p>[tekst.naam]</p>
```

Kopje:

```html
<h2>[tekst.naam]</h2>
```

Lijst:

```html
<ul>
  <li>[tekst.naam]</li>
  <li>[tekst.naam]</li>
</ul>
```

Vetgedrukt:

```html
<b>[tekst.naam]</b>
```

Command of codewoord:

```html
<code>Name</code>
```

Onderschrift bij screenshot:

```html
<p class="shot-caption">[tekst.naam]</p>
```

## Simpele Regel

- Tekst verbeteren? Wijzig de zin achter `=` in `.lng`.
- Tekst verplaatsen? Verplaats de hele regel in het HTML-bestand.
- Nieuwe alinea? Voeg `<p>[nieuwe.tekst.naam]</p>` toe en zet die naam in `eng.lng`.
- Notitie voor schrijvers? Gebruik `<!-- jouw notitie -->`.
- Niet zeker? Laat alles tussen `<` en `>` staan.
