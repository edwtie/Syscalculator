# Help-architectuur

<img src="TIEDRAGON_HELP_ARCHITECTURE.svg" alt="Help-architectuur" width="150%">

## Doel

De help-code is verplaatst naar een aparte Help-laag. De inhoud blijft zoveel
mogelijk bewerkbaar als gewone bestanden: HTML, CSS, JavaScript, SVG en
taalbestanden.

Hierdoor kan iemand die goed schrijft, maar geen programmeur is, helpteksten
aanpassen zonder C# code te wijzigen.

## Hoofdlagen

| Laag | Verantwoordelijkheid |
|---|---|
| Syscalculator App | Start help vanuit hoofdscherm, NOD editor en formulekaart. |
| Help | Bouwt HTML, vult placeholders, laadt content, toont dialoog en regelt navigatie. |
| Runtime resources | Bewerkbare helpcontent, templates, scripts, CSS, SVG mockups en `.lng` bestanden. |

## Contentmodel

Helpcontent staat in kleine, gerichte bestanden. De renderer vult daarna de
templates met content en taalteksten.

```text
Resources/Help
  templates, CSS en JavaScript

Resources/Help/Content
  main
  nod/full
  nod/command
  nod/snippet

*.lng
  taalteksten met eng.lng als fallback
```

Voor grotere taalsets is er een apart ontwerp voor ZIP-taalpakketten:

```text
docs/LANGUAGE_PACKAGE_DESIGN.md
```

Dat ontwerp bundelt `.lng`, helpcontent, manualbestanden en taalafhankelijke
assets in een installeerbaar pakket, zonder de huidige losse `.lng` bestanden
te breken.

## Taalafspraak

Schrijvers kunnen tekst taalafhankelijk maken met tokens:

```html
<h2>[help.nod.title]</h2>
<p>[help.nod.intro]</p>
```

De Help-laag vervangt deze tokens met de actieve taal. Als een vertaling mist,
valt de tekst terug op Engels.

Voor language packages geldt een strengere afspraak: bron-HTML blijft
taalonafhankelijk. Zet zichtbare tekst in `.lng` en laat bron-HTML alleen
structuur, links, media en placeholders bevatten. Pas bij generatie of compile
naar `.lngpdk` mag de echte uitgeklapte HTML in het pakket staan.

## SVG mockups

Voor screenshots en helpbeelden is SVG de voorkeursvorm.

Redenen:

- scherp op elke schaal;
- beter onderhoudbaar dan meerdere PNG's;
- taal kan via `[lng.key]` tokens;
- kleine visuele correcties zijn direct in broncode te doen;
- dezelfde mock kan voor meerdere talen gebruikt worden.

## Schrijversafspraak

- Bewerk helpinhoud als gewone HTML-bestanden, maar houd bron-HTML
  taalonafhankelijk voor language packages.
- Gebruik eenvoudige tags voor koppen, alinea's, tabellen, waarschuwingen en voorbeelden.
- Zet taalafhankelijke tekst tussen `[lng.key]`.
- Laat styling in CSS staan.
- Laat gedrag in JavaScript staan.
- Gebruik SVG voor helpbeelden die exact bij de applicatie moeten passen.
