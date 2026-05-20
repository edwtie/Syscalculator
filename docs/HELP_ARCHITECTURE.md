# Help-architectuur

![Help-architectuur](TIEDRAGON_HELP_ARCHITECTURE.svg)

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

## SVG mockups

Voor screenshots en helpbeelden is SVG de voorkeursvorm.

Redenen:

- scherp op elke schaal;
- beter onderhoudbaar dan meerdere PNG's;
- taal kan via `[lng.key]` tokens;
- kleine visuele correcties zijn direct in broncode te doen;
- dezelfde mock kan voor meerdere talen gebruikt worden.

## Schrijversafspraak

- Bewerk inhoud in HTML of Markdown-achtige contentbestanden.
- Gebruik eenvoudige tags voor koppen, alinea's, tabellen, waarschuwingen en voorbeelden.
- Zet taalafhankelijke tekst tussen `[lng.key]`.
- Laat styling in CSS staan.
- Laat gedrag in JavaScript staan.
- Gebruik SVG voor helpbeelden die exact bij de applicatie moeten passen.
