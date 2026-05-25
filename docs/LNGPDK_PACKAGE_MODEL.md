# `.lngpdk` pakketmodel

Een `.lngpdk` is het gecompileerde taal- en contentpakket voor Syscalculator
2.0 en andere Tiedragon-apps. Het is geen los tekstbestand en geen algemene
plug-in. Het is een gecontroleerde distributie-eenheid voor taal, help,
formulekaarten, media en trust-informatie.

## Model In Een Zin

```text
.objpdk bronpakket -> compiler -> .lngpdk distributiepakket -> runtime verifier
```

`.objpdk` is het bewerkbare concept. `.lngpdk` is het pakket dat de app mag
laden. Als het pakket signed is, toont de app een groen slot. Als de app
terugvalt op een losse `.lng`, toont de app een rood/ongetekend vertrouwen.

## Buitenste Laag

Een `.lngpdk` bestaat uit een Tiedragon/Syscalculator wrapper met een payload.

```text
SYSCALC-LNGPDK
format
headerLength
headerJson
payloadArchive
```

De wrapper zegt wat de payload is en hoe die gecontroleerd moet worden.

Belangrijke headergegevens:

| Veld | Betekenis |
| --- | --- |
| `softwareId` | App-identiteit, bijvoorbeeld `tiedragon.syscalculator`. |
| `packageType` | Type pakket, voor taalpackages `language`. |
| `payloadFormat` | Normaal `zip`. |
| `payloadSha256` | SHA-256 van de payload. |
| `signed` | Of het pakket ondertekend is. |
| `signatureAlgorithm` | Bijvoorbeeld `rsa-pss-sha256`. |
| `signatureKeyId` | Vertrouwde sleutelnaam. |
| `signature` | Handtekening over de canonieke signing input. |

## Payload Laag

De payload is een archief met definitieve runtimebestanden.

```text
manifest.json
language/
  ned.lng
manual/
  index.html
help/
  basis.js
  main-help.css
  main-help.js
  nod.js
  nod-popup.css
  nod-popup.js
  formula.js
  formula-film.css
  formula-film.js
  content/
    main/
    nod/
      full/
      command/
      popup/
      snippet/
    legal/
    tool-editor/
formula/
  index.html
  *.html
assets/
  *.png
  *.jpg
  *.jpeg
  *.svg
  *.webp
  *.gif
  *.bmp
```

Niet elk pakket hoeft elk optioneel bestand te bevatten, maar een releasepakket
moet compleet genoeg zijn voor de app die het accepteert.

## Verplichte Kern

Een geldig Syscalculator-taalpakket heeft minimaal:

| Onderdeel | Waarom |
| --- | --- |
| `manifest.json` | Identiteit, taalcode, product, versie en fallback. |
| `language/<code>.lng` | UI-tekst, labels en helptekstsleutels. |
| Wrapper header | Hashes, signingstatus en appbinding. |
| Geverifieerde payload | De runtime mag geen half-vertrouwde inhoud laden. |

Voor signed packages is de taalset strenger: verplichte trust/info keys moeten
aanwezig zijn, zoals signed-status, package-info labels en `common.yes/no`.

## Contentlagen

### Taal

`language/<code>.lng` bevat sleutel/waarde-regels:

```text
language.name=Nederlands
menu.file=Bestand
help.signed_package_verified=Ondertekend pakket geverifieerd
```

De `.lng` blijft leesbaar, maar in `.lngpdk` reist hij samen met help, media,
metadata en trust-informatie.

### Help

`help/content/...` bevat de helpstructuur voor:

- hoofdhelp;
- NOD help;
- NOD popup/tips;
- ToolEditor help;
- juridische/help-pagina's.

De runtime toont deze HTML via de Help-laag en WebView2. De help is onderdeel
van hetzelfde pakket als de taalregels, zodat tekst en documentatie niet uit
elkaar lopen.

### Formulekaarten

`formula/` bevat formulekaart-indexen en individuele HTML-kaarten. Dit maakt
het mogelijk om formuleuitleg, schoolniveau-tags, CAS-light info en exportbare
kaartinhoud per taal te leveren.

### Scripts En CSS

Alleen bekende runtimebestanden zijn toegestaan, bijvoorbeeld:

```text
basis.js
main-help.js
nod.js
nod-popup.js
formula.js
main-help.css
nod-popup.css
```

De compiler weigert onbekende JavaScriptnamen. Daardoor is `.lngpdk` geen
algemene code-plug-in.

### Media

`assets/` bevat afbeeldingen en SVG's die help/formulekaarten nodig hebben.
Media blijven echte bestanden, niet verborgen strings in generatorcode.

## Manifestmodel

Voorbeeld:

```json
{
  "format": 1,
  "key": "ned",
  "id": "tiedragon.language.ned",
  "producer": "Tiedragon",
  "product": "Syscalculator",
  "softwareId": "tiedragon.syscalculator",
  "languageCode": "ned",
  "displayName": "Nederlands",
  "nativeName": "Nederlands",
  "packageVersion": "2026.05.25.001",
  "fallbackLanguage": "eng"
}
```

Belangrijk:

- `softwareId` bindt het pakket aan de juiste appfamilie;
- `key` is de runtime package key;
- `languageCode` moet passen bij `language/<code>.lng`;
- `packageVersion` is de pakketversie, niet per se de appversie.

## Trustmodel

De app behandelt pakketinhoud fail-closed.

```text
open .lngpdk
  -> lees wrapper
  -> controleer softwareId/packageType
  -> controleer payloadSha256
  -> controleer signature als signed=true
  -> controleer trusted key
  -> lees manifest en taalbestand
  -> accepteer pakket of val terug
```

Truststatus in de UI:

| Situatie | UI |
| --- | --- |
| Signed package geldig | Groen slot, package verified. |
| Losse `.lng` fallback | Rood slot/ongetekende taal. |
| Signed package ongeldig | Pakket weigeren, fallback gebruiken indien mogelijk. |
| Verkeerde `softwareId` | Pakket weigeren. |

## Wat Er Niet In Hoort

Een release `.lngpdk` mag niet bevatten:

- `source/templates/`;
- authoring-only bestanden zoals `WRITER_*.html`;
- concept banners;
- unresolved placeholders zoals `[editor.some.key]`;
- legacy VB6 help als stille runtimebron;
- onbekende scripts;
- bestanden buiten de policy;
- private signing keys.

## Waarom Geen `.chm`, `.mo` Of Gewone ZIP?

`.chm` is goed voor statische help. `.mo/.po` is goed voor vertaalcatalogi.
Een ZIP is goed als generieke container. `.lngpdk` combineert precies wat
Syscalculator nodig heeft:

- taalregels;
- help;
- NOD command tips;
- formulekaarten;
- media;
- appidentiteit;
- package hashes;
- optional signing;
- runtime trust UI;
- fallback naar losse `.lng`.

Daarom is `.lngpdk` geen vervanging voor alle vertaal- of helpformaten. Het is
de releasevorm voor vertrouwde Tiedragon taal/contentpackages.

## Levenscyclus

```text
1. Maak of open .objpdk in ToolEditor.
2. Bewerk taal, HTML, formulekaarten en media.
3. Valideer source package.
4. Compileer naar .lngpdk.
5. Sign indien nodig.
6. Publiceer via installer, updater of LanguagePackages/.
7. Runtime verifieert en toont truststatus.
```

## Relatie Tot Andere Docs

- [Language Package Design](LANGUAGE_PACKAGE_DESIGN.md): volledig ontwerp en
  beleidsregels.
- [Language Package Tool](LANGUAGE_PACKAGE_TOOL.md): compilercommando's en
  scripts.
- [Architecture Index](ARCHITECTURE_INDEX.md): overzicht van architectuurdocs.
