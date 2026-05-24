# Localization / Language Package Design

## Purpose

Language Package 1.0 is the Tiedragon localization package format used first by
Syscalculator 2.0. It replaces the old "only loose `.lng` files" model with one
checked `.lngpdk` package that can contain:

- UI language keys;
- HTML help;
- NOD help;
- formula cards;
- ToolEditor help;
- legal documents such as license agreement and privacy statement;
- trusted help scripts and CSS;
- screenshots and package media;
- manifest metadata;
- SHA-256 checks for release/update workflows.

The goal is not only translation. The package is a controlled help/content
container for Tiedragon apps. Syscalculator remains the first consumer, but the
compiler/editor are designed so they can later move to a separate
`Tiedragon.Localization` repository and be reused by other apps.

## Current Implementation

Current projects:

```text
src/Tiedragon.LanguagePackage   command-line compiler, validator and inspector
src/Tiedragon.ToolEditor        WinForms concept editor for packages and media
src/Tiedragon.Graph.Image       image metadata helper for ToolEditor previews
src/syscalculator               runtime package consumer and fallback integration
```

Current status:

- `.lngpdk` compilation, validation and inspection are implemented.
- Agent-friendly JSON compile commands are implemented.
- Basispackage merge is implemented.
- Syscalculator loads UI text, main help, NOD help, formula cards and legal/help
  pages from active packages.
- ToolEditor can create/open/save concept packages, edit source/HTML, preview
  content, manage media and compile packages.
- Daily/Beta release can ship `.lngpdk` packages under `LanguagePackages`.
- Loose `.lng` files remain supported as fallback and manual compatibility.

## Single Pipeline Architecture

Language Package 1.0 must use one content pipeline. The pipeline is:

```text
source package -> parser/render pipeline -> compiled distribution package
```

The source package is the only editable source of truth. The compiled package is
the only release artifact. ToolEditor and the command-line compiler must share
the same package model, the same security policy and the same template/parser
rules.

Definitions:

| Term | Meaning |
| --- | --- |
| Source package | Editable folder/package with `manifest.json`, `language/*.lng`, templates, HTML topics, JS/CSS and assets. |
| Basispackage | Complete reference source package used to create or fill a translated source package. |
| Parser/render pipeline | The deterministic step that expands placeholders, applies templates, resolves media and creates final HTML. |
| Distribution package | Compiled `.lngpdk` with final HTML, language file, allowed runtime scripts/CSS, assets, manifest and checksums. |
| Legacy archive | Old Syscalculator/VB6 help or other compatibility material. It may be read for history or manual migration, but it is not an active source package. |

The pipeline has these rules:

- ToolEditor opens a source package or basispackage. It must not silently build a
  different package from unrelated fallback sources.
- Source mode shows editable template/source text.
- Edit mode shows parsed/rendered HTML for editing, based on the same source
  package and `.lng` file.
- Preview shows what the compiled result will look like, including resolved
  media and package links.
- Compile writes final HTML. A release `.lngpdk` must not contain unresolved
  source placeholders, concept banners or authoring-only templates.
- Every generated file must have a traceable origin: source path, package path,
  type and transformation step.

This avoids the previous failure mode where help was assembled from old manual
files, `.lng` fragments, hardcoded script output and ToolEditor fallbacks at the
same time. If a developer asks "where is this SVG?" or "where is this text?",
the answer must be visible in the source package or in the compile report.

## Package Kinds

The file extension must show whether a package is editable source material or a
compiled distribution artifact.

| Kind | Extension | Created by | Used for | Editable |
| --- | --- | --- | --- | --- |
| Object/source package | `.objpdk` | ToolEditor, generator or basispackage builder | Design/concept package, basispackage, translator work, AI-agent work | Yes |
| Language distribution package | `.lngpdk` | Compiler | Runtime loading, installer/updater, release, signing/checksum verification | No |

Rules:

- `.objpdk` is the authoring package. It may contain `source/templates/`,
  placeholders, concept state, editable HTML and assets. It is the normal file
  format for ToolEditor work.
- `.lngpdk` is the compiled output. It contains definitive HTML and validated
  runtime files. It is the format Syscalculator should load at runtime.
- The language generator creates `.objpdk`. It does not create release
  `.lngpdk` files directly.
- The compiler takes `.objpdk` as its canonical input and creates `.lngpdk` as
  its canonical output.
- ToolEditor may open `.lngpdk` for inspection or as the base for a new concept,
  but editing it must create/save a separate `.objpdk`.
- A signed `.lngpdk` must never be edited in place. Changing any byte invalidates
  its payload hash and signature. The correct flow is:

```text
language generator -> .objpdk -> compiler -> .lngpdk -> sign/check
open .lngpdk -> save/edit as .objpdk -> compiler -> new .lngpdk -> sign/check
```

Signed package rule:

- ToolEditor may inspect a signed `.lngpdk`.
- ToolEditor may derive a new editable `.objpdk` from a signed `.lngpdk`.
- ToolEditor must not save changes back into the same signed `.lngpdk`.
- Any content change must pass through the compiler again.
- Any content change requires a new package SHA-256 and a new signature.
- Runtime readers must reject a signed `.lngpdk` when the payload hash or
  signature no longer matches.

Basispackage rule:

- Basispackages should normally be stored as `.objpdk` because they are source
  material. A compiled English `.lngpdk` can still be used as a merge base for
  compatibility, but it is not the preferred authoring source.

## Source Of Truth

The intended source package layout is:

```text
manifest.json
language/
  <code>.lng
help/
  basis.js
  main-help.css
  main-help.js
  nod.js
  nod-popup.css
  nod-popup.js
  formula.js
  document-body.html
  document-topic.html
  content/
    main/
    nod/
      full/
      command/
      popup/
      snippet/
    legal/
    tool-editor/
source/
  templates/
    formula/
    nod/
    manual/
formula/
assets/
```

Responsibilities:

- `language/<code>.lng` contains UI strings, short labels and localized help
  fragments referenced by placeholders.
- `help/content/...` contains the help topic structure and package links.
- `source/templates/...` contains authoring templates used by parser/generator
  steps.
- `help/*.js` and `help/*.css` are runtime files and must be allowlisted by
  policy.
- `assets/` contains image/media files. SVG files are real assets, not hidden
  strings inside generation scripts.
- `manifest.json` identifies the package and app identity.

The source package may contain placeholders such as
`[editor.nod_help.full.commands.basic]`. During compile these are expanded with
the active `language/<code>.lng`. The output package contains the resulting HTML
fragment, not the unresolved placeholder.

## Legacy Boundary

Old Syscalculator 1.x/VB6 help is compatibility and history material. It must
not be imported automatically into new `.lngpdk` packages as active `manual/`
content.

Allowed use:

- compare old and new help during migration;
- manually copy useful text into the modern source package;
- keep screenshots/history in documentation;
- build a separate legacy reference package for inspection only.

Forbidden for release packages:

- silently generating `manual/*.html` from `legacy/Syscalculator174.VB6/help`;
- mixing old HTML pages with modern language-package help without explicit
  source mapping;
- using hardcoded generator strings as the only source for visible help text;
- hiding SVG/media generation inside scripts when a real asset file should
  exist under `assets/`.

The release pipeline should fail when legacy paths are used as active source
without an explicit migration step and report.

## Package Names

Official Syscalculator packages should use:

```text
Syscalculator.Language.<code>.lngpdk
```

Editable source/basis packages should use:

```text
Syscalculator.Language.<code>.objpdk
Syscalculator.Language.Base.<code>.objpdk
```

The file name is not trusted as identity. Runtime identity comes from
`manifest.json` and the package wrapper header.

The active language configuration can contain:

```ini
language=ned.lng
languagePackage=ned
```

`languagePackage` is the manifest `key`.

## `.lng` vs `.objpdk` vs `.lngpdk`

`.lng`, `.objpdk` and `.lngpdk` solve different layers.

| Item | `.lng` | `.objpdk` | `.lngpdk` |
| --- | --- | --- | --- |
| Role | Loose text fallback | Editable package source/concept | Compiled language/help distribution package |
| Content | `key=value` UI/help text entries | Manifest, language file, source templates, HTML topics, assets, authoring state | Manifest, language file, final HTML, NOD help, formula cards, legal docs, media, trusted scripts and metadata |
| Editing | Simple text editor | ToolEditor concept workflow | Read-only/inspect; edits must become `.objpdk` |
| Validation | Runtime parsing only | Authoring validation and policy checks | Strict file policy, links, media, encoding, JS allowlist, SHA-256 and optional signature |
| Release use | Compatibility and emergency override | Never directly loaded as release runtime | Preferred Daily/Beta package format |
| Failure | Missing keys fall back per lookup | Cannot compile until fixed | Invalid package is rejected fail-closed |

`.lng` stays important because it is easy to inspect and repair. `.objpdk`
keeps large help/media work editable. `.lngpdk` is safer for distribution
because all related language content is checked, hashed and optionally signed
together.

## Wrapper Format

Compiled packages use a Syscalculator/Tiedragon wrapper:

```text
SYSCALC-LNGPDK
format: int32
headerLength: int32
header JSON
payload archive bytes
```

Header JSON:

```json
{
  "format": 1,
  "softwareId": "tiedragon.syscalculator",
  "packageType": "language",
  "payloadFormat": "zip",
  "payloadSha256": "64 hex characters",
  "encrypted": false,
  "signed": false,
  "signatureAlgorithm": "",
  "signatureKeyId": "",
  "signature": ""
}
```

The compiler writes ZIP payloads. The reader can inspect ZIP and
7z-compatible archives through SharpCompress, but new official packages should
be wrapped `.lngpdk` files.

The compiler also prints `packageSha256`, the SHA-256 of the complete
`.lngpdk` file. That value belongs in release/updater metadata. It is not stored
inside the package because changing the package would change the hash.

Encrypted payloads are reserved for later. If a package declares encryption
today, readers reject it fail-closed.

Signing uses a verifier gate:

- unsigned official packages use `signed: false`;
- signature metadata is not allowed when `signed` is false;
- `compile-signed` writes `rsa-pss-sha256` signatures over a stable canonical
  signing input that includes identity, payload format, payload SHA-256,
  algorithm and key id;
- readers verify signed packages against `language-package-trusted-keys.json`;
- if no matching trusted public key exists, the package is rejected fail-closed;
- supported algorithms are `rsa-pss-sha256` and `rsa-sha256`.

This prevents a package from pretending to be official before Syscalculator
knows which publisher keys it should trust.

## Manifest

Minimum manifest:

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
  "appMinVersion": "2.0",
  "packageVersion": "2026.05.24.001",
  "fallbackLanguage": "eng"
}
```

Rules:

- `format` is the package format version.
- `key` is the runtime package identity, for example `ned`.
- `id` is a longer publisher/package identifier.
- `producer`, `product` and `softwareId` bind the package to the trusted app
  line.
- `languageCode` must match `language/<code>.lng`.
- `fallbackLanguage` should normally be `eng`.

For another Tiedragon app, keep `producer` as `Tiedragon` and set `product` and
`softwareId` to that app. The app reader must only accept its expected
`softwareId`.

## Package Structure

Typical compiled package:

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

Concept packages may additionally contain source-only templates:

```text
source/templates/formula/index.html
source/templates/formula/card.html
source/templates/nod/full-page.html
source/templates/nod/command-page.html
source/templates/nod/popup.html
source/templates/nod/snippet.html
templates/
```

`source/templates/` and legacy `templates/` are compile inputs. They are not
written into the final payload. Formula HTML is regenerated during compile when
explicit `formula/*.html` entries are missing.

Compiled distribution packages should contain definitive runtime content:

- expanded HTML under `manual/`, `help/content/...` and `formula/`;
- no `source/templates/`;
- no authoring-only examples such as `WRITER_*.html`;
- no concept warning markup;
- no unresolved `[key]` placeholders;
- no dependency on `legacy/Syscalculator174.VB6/help`.

## Language-Independent HTML Policy

Before compiling, source HTML should stay language-independent:

- visible translated text belongs in `language/<code>.lng`;
- source HTML should use placeholders such as
  `[editor.nod_help.full.commands.basic]`;
- invariant command names, code examples, CSS classes, JS identifiers and media
  paths may stay in source HTML;
- generated concept output and compiled `.lngpdk` may contain expanded real
  localized HTML;
- release packages must not contain unresolved `[key]` placeholders;
- release packages must not contain encoded localized markup such as
  `&lt;h1&gt;` where real HTML was expected.

Wrong source HTML:

```html
<h2>Basis en velden</h2>
```

Correct source HTML:

```html
[editor.nod_help.full.commands.basic]
```

The `.lng` value may contain the full localized HTML fragment:

```text
editor.nod_help.full.commands.basic=<h1>Basis en velden</h1><p>...</p>
```

## ToolEditor

`Tiedragon.ToolEditor` is the graphical concept-package editor. It can run as a
standalone app or be opened from Syscalculator.

Main workflow:

- New package: creates a basis concept package for the configured language.
- Open: opens a concept folder/package.
- Save concept: writes editable concept content.
- Validate: checks paths, links, media, scripts and quality gates.
- Compile: produces a `.lngpdk`.
- Preview: shows rendered HTML with package media resolved.
- Compile report: lists source path, package path, file type and generation step
  for every emitted file.

Editor layout:

- left tree shows package sections, not raw file-system noise;
- translation, help topics, NOD help, formula card, ToolEditor help, legal docs
  and media are grouped separately;
- HTML documents have Source and Edit modes;
- source mode shows HTML source;
- edit mode uses WebView2/contenteditable for visual editing;
- concept warning is displayed as an editor/preview banner, not stored in
  release documents;
- closing tabs or the editor must respect dirty state.

ToolEditor must not be a second generator with different rules. It may create a
new package from the official basispackage, but that basispackage remains the
source of truth. Any fallback page is only a recovery message for a broken
checkout; it must not become release content.

Media manager:

- supports drag/drop, copy, cut, paste, rename and delete;
- only allows configured image extensions;
- lists file name, type, dimensions, size, date/source and package path;
- previews package images through data URIs.

ToolEditor uses `Tiedragon.Graph.Image` for image metadata. That module reads
only what ToolEditor needs: dimensions, a simple source label and common EXIF
date fields.

ExifTool is a reference source for metadata formats and file coverage:

```text
https://exiftool.org/
```

ExifTool is not a runtime dependency. Syscalculator does not execute external
metadata tools while opening `.lngpdk` packages.

## Media Formats

Allowed image extensions are defined by policy:

```text
.png
.jpg
.jpeg
.svg
.webp
.gif
.bmp
```

Preview MIME mapping:

```text
.png   image/png
.jpg   image/jpeg
.jpeg  image/jpeg
.svg   image/svg+xml
.webp  image/webp
.gif   image/gif
.bmp   image/bmp
```

WebP/GIF/BMP dimensions are read from headers where possible, so metadata does
not have to rely entirely on GDI+/Windows image support.

## Security Policy

The strict file policy lives in:

```text
src/Tiedragon.LanguagePackage/language-package-policy.ini
```

It is used by both ToolEditor and the command-line compiler.

Current important policy fields:

```ini
allowedExtensions=.bmp,.css,.gif,.html,.jpg,.jpeg,.js,.json,.lng,.png,.svg,.webp
blockedExtensions=.bat,.cmd,.com,.dll,.exe,.msi,.ps1,.scr,.vbs
imageExtensions=.bmp,.gif,.jpg,.jpeg,.png,.svg,.webp
textExtensions=.css,.html,.js
allowedScripts=basis.js,formula.js,main-help.js,nod.js,nod-popup.js
```

Allowed prefixes:

```text
language/:lng
assets/:image
manual/:text
help/content/main/:text
help/main/:text
help/content/nod/:text
help/nod/:text
nod/:text
help/content/legal/:text
help/legal/:text
help/content/tool-editor/:text
help/tool-editor/:text
tool-editor/:text
help/content/HelpApi/:text
source/templates/:text
templates/:text
formula/:text
```

Allowed root help files:

```text
basis.js
document-body.html
document-topic.html
formula-card.html
formula-film.css
formula-film.html
formula-film.js
formula.js
main-help.css
main-help.js
nod-popup.css
nod-popup.js
nod.js
```

ToolEditor has a security settings dialog for inspecting/editing this policy.
Changes must be applied explicitly. The compiler is still the final authority:
if the policy rejects a file, compile fails.

## Quality Gates

Compile/validate must reject distribution-breaking packages:

- mojibake or encoding damage in `.lng`, HTML, JSON, CSS or JS;
- unknown package paths;
- blocked executable/script file types;
- unsupported JavaScript names;
- concept warning/banner markup stored in release help documents;
- unresolved `[lng.key]` placeholders;
- encoded localized markup such as `&lt;h1&gt;`;
- broken internal HTML links;
- missing media references;
- media references that do not point to allowed image types;
- JavaScript links inside HTML;
- unsafe paths such as absolute paths or `..`;
- file count, single-file, payload or decompressed-size limit violations;
- wrong manifest identity;
- checksum mismatch;
- encrypted package flags before encryption support exists.
- signed package flags before a trusted signature verifier/key store exists.
- active release content generated from legacy manual paths without an explicit
  migration report.

Release indexes should mark a package active only when it contains every
required language key from the English base file. Incomplete packages may be
compiled for inspection, but they should not be offered as active release
packages because users would see fallback English.

## Compile Report

The compiler or generation script should produce a machine-readable report for
release builds. Minimum fields per emitted file:

```json
{
  "packagePath": "help/content/main/nodeditor.html",
  "sourcePath": "help/content/main/nodeditor.html",
  "type": "html",
  "transform": "language-placeholders",
  "language": "ned",
  "sha256": "..."
}
```

For generated files, `sourcePath` should point at the template or asset that
caused the output. For copied files, `transform` should be `copy`. For localized
HTML, `transform` should name the parser step, for example
`language-placeholders`, `formula-template` or `nod-template`.

The report is part of the quality process. It is how maintainers verify where
SVG files, HTML pages and translated text came from before a package is marked
active.

## Limits

Current limits:

```text
max header size: 64 KiB
max payload size: 192 MiB
max files per package: 2048
max single file size: 16 MiB
max total uncompressed size: 128 MiB
```

## Compiler Commands

`Tiedragon.LanguagePackage` commands:

```text
compile <input-folder-or-objpdk> <output.lngpdk>
pack-language <input-folder-or-objpdk> <output.lngpdk>
compile-signed <input-folder-or-objpdk> <output.lngpdk> <private-key.pem> <key-id>
agent-compile <input-folder-or-objpdk> <output.lngpdk>
agent-compile-signed <input-folder-or-objpdk> <output.lngpdk> <private-key.pem> <key-id>
agent-compile-with-base <base-folder-or-package> <input-folder-or-objpdk> <output.lngpdk>
agent-compile-with-base-signed <base-folder-or-package> <input-folder-or-objpdk> <output.lngpdk> <private-key.pem> <key-id>
create-signing-key <private-key.pem> <trusted-key.json> <key-id>
export-formula-cards <output-folder> [language-file.lng]
export-formula-template <output.lng> [language-file.lng]
export-html-templates <output-folder>
validate <package.lngpdk>
inspect <package.lngpdk>
```

`compile` and `pack-language` are equivalent. `agent-compile` prints JSON for
AI agents, scripts and release automation. `agent-compile-with-base` first
merges a basispackage into the concept folder.

Use `create-signing-key` to create a local RSA-PSS signing key and a matching
trusted-key JSON fragment. Keep the private key outside source control. Copy the
trusted public key data into `language-package-trusted-keys.json` for apps or
tools that may accept signed packages.

PowerShell helper:

```powershell
tools/Compile-LanguagePackage.ps1 <concept-folder-or-objpdk> <output.lngpdk>
tools/Compile-LanguagePackage.ps1 <concept-folder-or-objpdk> <output.lngpdk> -BasePackage <base-folder-or-package>
```

Generation scripts should run under PowerShell 7 or newer:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/Generate-LanguagePackages.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/Test-LanguagePackageIntegration.ps1
```

PowerShell 7 avoids old Windows PowerShell encoding problems and keeps `.lng`,
HTML, JSON and docs predictable UTF-8.

## Basispackage Merge

A basispackage is a complete reference package, normally English. It lets
translators or agents start from a known complete package.

The merge can use either a concept folder, a `.objpdk` basispackage or an
existing `.lngpdk`:

```powershell
tools/Compile-LanguagePackage.ps1 .\concept .\out.lngpdk -BasePackage .\base.lngpdk
```

The compiler:

- requires matching `producer`, `product` and `softwareId`;
- copies missing non-manifest files from the base package;
- maps missing language keys from `language/<base-code>.lng` into
  `language/<target-code>.lng`;
- never overwrites existing concept files or existing language keys;
- reports `basePackage`, `addedEntries` and `addedLanguageKeys` in agent JSON.

## Agent Output

Successful compile JSON contains:

```json
{
  "success": true,
  "outputPath": "C:\\packages\\Syscalculator.Language.ned.lngpdk",
  "packageKey": "ned",
  "product": "Syscalculator",
  "softwareId": "tiedragon.syscalculator",
  "languageCode": "ned",
  "displayName": "Nederlands",
  "entryCount": 120,
  "packageSha256": "64 hex characters",
  "payloadSha256": "64 hex characters",
  "encrypted": false,
  "signed": false,
  "signatureAlgorithm": "",
  "signatureKeyId": "",
  "basePackage": "",
  "addedEntries": [],
  "addedLanguageKeys": []
}
```

Stable agent error codes include:

```text
E_ARGS
E_INPUT_NOT_FOUND
E_FILE_NOT_FOUND
E_IO
E_OUTPUT_EXTENSION
E_MANIFEST_MISSING
E_MANIFEST_JSON
E_MANIFEST_INVALID
E_BASE_PACKAGE
E_REQUIRED_FILE
E_PATH_UNSAFE
E_FILE_BLOCKED
E_FILE_UNSUPPORTED
E_QUALITY_GATE
E_LIMIT_FILE_COUNT
E_LIMIT_SIZE
E_CHECKSUM
E_ENCRYPTED
E_SIGNATURE
E_UNSUPPORTED_FORMAT
E_PACKAGE_INVALID
E_UNKNOWN
```

## Runtime Loading

Recommended lookup order for UI text:

```text
1. active `.lngpdk`: language/<code>.lng
2. <app>\Languages\<code>.lng
3. <app>\<code>.lng
4. extracted compatibility package: language/<code>.lng
5. active package fallback: language/eng.lng, if present
6. <app>\eng.lng
7. built-in fallback string
```

Recommended lookup order for help/manual content:

```text
1. active language package content
2. built-in Resources/Help content
3. language key override from .lng
4. English fallback content
5. built-in fallback string
```

Package content must not be partially trusted. If validation fails, the reader
skips the package and falls back to the previous known-good content.

## Install Locations

Compressed packages:

```text
<app>\LanguagePackages\Syscalculator.Language.ned.lngpdk
<app>\LanguagePackages\Cache\Syscalculator.Language.ned.lngpdk
```

Release index:

```text
<app>\LanguagePackages\language-packages.json
```

Extracted compatibility folders may still be read for manual/development work:

```text
<app>\LanguagePackages\syscalculator.language.ned\
```

## Release Workflow

Daily/Beta release should:

1. Generate packages from concept/basis packages.
2. Validate packages.
3. Check language-key coverage against English.
4. Compute `packageSha256`.
5. Copy active packages to `LanguagePackages`.
6. Write/update package metadata for installer/updater.
7. Keep loose `.lng` fallback files available.

The updater should never activate a package whose checksum or validation fails.

## Failure Policy

Syscalculator uses a fail-closed policy:

- wrong identity: reject;
- unsafe path: reject;
- blocked extension: reject;
- unsupported JS: reject;
- checksum mismatch: reject;
- encrypted package before support: reject;
- signed package without a trusted matching public key: reject;
- missing required language file: reject;
- broken distribution quality gate: reject for release activation.

On failure, the app continues with the previous package, loose `.lng`, built-in
resources or English fallback.

## Signing Verification

Implemented now:

- wrapper header fields for `signed`, `signatureAlgorithm`, `signatureKeyId`
  and `signature`;
- shared `LanguagePackageSignatureVerifier`;
- `create-signing-key` for RSA-PSS private/public key creation;
- `compile-signed`, `agent-compile-signed` and
  `agent-compile-with-base-signed`;
- trusted public keys loaded from `language-package-trusted-keys.json`;
- fail-closed rejection for signed packages without trusted keys;
- rejection when signature fields exist but `signed` is false;
- payload SHA-256 is checked before signature handling.

Future work:

- key rotation policy;
- detached release metadata signatures;
- optional external certificate chain policy for official release builds.

## Open Work

Still future work:

- encrypted package payloads;
- richer package removal/repair UI;
- package update/download UI beyond release bundling;
- generalized non-Syscalculator app templates;
- optional stronger metadata reading if a future embedded library is approved.

## Recommendation

For current Syscalculator releases:

1. Keep loose `.lng` fallback.
2. Ship `.lngpdk` as the preferred Daily/Beta language format.
3. Keep package policy strict and shared between ToolEditor and compiler.
4. Use basispackage merge for translated packages.
5. Treat ExifTool as metadata reference, not runtime dependency.
6. Reject broken packages fail-closed.
