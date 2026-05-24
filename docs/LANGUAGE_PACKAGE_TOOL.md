# Localization / Language Package 1.0

`Language Package 1.0` is the small agent-friendly compiler in
`Tiedragon.LanguagePackage`. It builds and checks `.lngpdk` language packages
for Tiedragon apps. Syscalculator is the first supported app, but the package
identity comes from `product` and `softwareId` in `manifest.json`.

This is also a developer technique for people who want to build their own app
with structured localization. A developer can define an app identity, create a
basispackage with default language/help/media, and let translators or agents
compile checked language packages without changing the app itself.

This belongs to the `Tiedragon.Localization` domain. The compiler and
`Tiedragon.ToolEditor` are standalone enough to move to their own git repository
later; Syscalculator should only consume the compiled `.lngpdk` packages and
keep its app-specific loading policy.

The graphical editor is `Tiedragon.ToolEditor`. It is a standalone WinForms app
for editing concept language packages, HTML help, formula cards and media. It
can still be opened from Syscalculator, but it also runs independently:

```powershell
dotnet run --project src/Tiedragon.ToolEditor
```

## Commands

```text
compile <input-folder-or-objpdk> <output.lngpdk>
pack-language <input-folder-or-objpdk> <output.lngpdk>
compile-signed <input-folder-or-objpdk> <output.lngpdk> <private-key.pem> <key-id>
agent-compile <input-folder-or-objpdk> <output.lngpdk>
agent-compile-signed <input-folder-or-objpdk> <output.lngpdk> <private-key.pem> <key-id>
agent-compile-with-base <base-folder-or-package> <input-folder-or-objpdk> <output.lngpdk>
agent-compile-with-base-signed <base-folder-or-package> <input-folder-or-objpdk> <output.lngpdk> <private-key.pem> <key-id>
create-signing-key <private-key.pem> <trusted-key.json> <key-id>
validate <package.lngpdk>
inspect <package.lngpdk>
```

`compile` and `pack-language` are equivalent. `agent-compile` is the same
compiler with JSON output, intended for AI agents, scripts and release
automation. `agent-compile-with-base` first merges a basispackage into the
concept source before compiling. That source should normally be `.objpdk`;
folder input remains supported for scripts and migration. The signed variants add `rsa-pss-sha256`
signature metadata to the wrapper header. `create-signing-key` creates a local
private key and a matching trusted public-key JSON fragment.

Keep private signing keys outside source control. Only the trusted public-key
JSON belongs in app/tool output when signed packages should be accepted.

PowerShell helper:

```powershell
tools/Compile-LanguagePackage.ps1 <concept-folder-or-objpdk> <output.lngpdk>
tools/Compile-LanguagePackage.ps1 <concept-folder-or-objpdk> <output.lngpdk> -BasePackage <base-folder-or-package>
```

Run language-package scripts with PowerShell 7 or newer:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/Generate-LanguagePackages.ps1
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/Test-LanguagePackageIntegration.ps1
```

The generation, translation and integration smoke scripts reject Windows
PowerShell 5.1. This keeps `.lng`, HTML, JSON and docs on predictable UTF-8
handling and avoids mojibake during release work.

The helper runs `agent-compile`, so callers can parse `success`, `code`,
`packageSha256`, `payloadSha256`, `languageCode`, `packageKey` and `entryCount`
without scraping human-readable text.

## One Pipeline Rule

The language-package workflow uses one pipeline:

```text
source package -> parser/render pipeline -> compiled .lngpdk
```

ToolEditor edits the source package. The compiler creates the final package.
There must not be a second hidden generator that pulls content from old manuals,
fallback strings or unrelated resources without reporting it.

There are two package files:

| Extension | Meaning |
| --- | --- |
| `.objpdk` | Editable object/source package: concept, basispackage, templates, placeholders, assets and translator work. |
| `.lngpdk` | Compiled language distribution package: final HTML, validated runtime files, hashes and optional signature. |

Canonical production chain:

```text
language generator -> .objpdk
compiler           -> .lngpdk
```

The generator creates `.objpdk` source packages. The compiler reads `.objpdk`
packages and writes `.lngpdk` distribution packages. Folder-based compile input
can remain as a development compatibility path, but the release workflow should
use `.objpdk -> .lngpdk`.

ToolEditor should save normal work as `.objpdk`. It may open `.lngpdk` files for
inspection or to create a new concept, but edits must be saved into a separate
`.objpdk`. A compiled `.lngpdk` is release output; editing it in place would
break its SHA-256 and signature.

Signed `.lngpdk` rule:

- Open is allowed for inspection.
- "Save as `.objpdk`" is allowed for rework.
- Direct save back to the signed `.lngpdk` is not allowed.
- After any edit, the package must be compiled again from `.objpdk`.
- The compiler must create a new payload SHA-256, package SHA-256 and signature.
- A reader must reject the package if the signature is stale or mismatched.
- ToolEditor shows a green signed-status bar after a signed `.lngpdk` has been
  opened and verified successfully.

Practical meaning:

- HTML topics, templates, runtime JS/CSS and media live in the source package.
- `.lng` contains UI strings and localized fragments used by placeholders.
- SVG/PNG/JPG/WebP assets live under `assets/`; generated SVG strings in scripts
  are only temporary recovery code and must not be the normal release source.
- Source HTML may contain placeholders such as
  `[editor.nod_help.full.commands.basic]`.
- The compiled `.lngpdk` contains definitive HTML with those placeholders
  expanded.
- `source/templates/` is authoring input and should not be shipped as runtime
  content.
- Old Syscalculator 1.x/VB6 help is legacy reference material. It may be used
  for manual migration, but it must not be silently imported into release
  packages.

Normal flow:

```text
generate/open .objpdk -> edit -> validate -> compile -> sign/check -> .lngpdk
```

Inspection/rework flow:

```text
open .lngpdk -> save as .objpdk -> edit -> compile new .lngpdk
```

For release builds the generator/compiler should write a report that maps every
file:

```text
source path -> package path -> transform -> sha256
```

This is required so a maintainer can answer where a text, SVG or HTML page came
from before a package is activated.

## Authoring Policy

Concept packages are edited before `.lngpdk` compilation. At that stage HTML
must be language-independent:

- source HTML is for structure, links, media and reusable layout;
- visible text belongs in `language/<code>.lng`;
- source HTML should use placeholders such as
  `[editor.nod_help.full.commands.basic]`;
- translated `.lng` values may contain full HTML fragments when a help page is
  language-specific;
- generated concept output and compiled `.lngpdk` packages may contain the real
  expanded HTML.

This keeps English, Dutch and later languages on the same package structure.
It also prevents the old problem where NOD help or formula help looked partly
English and partly Dutch because text was stored directly in HTML.

Release checks should reject packages with unresolved placeholders, mojibake,
broken links, missing images or encoded HTML markup that should have been
expanded from `.lng`.

## Syscalculator Activation

Syscalculator consumes compiled packages from the application folder:

```text
LanguagePackages/
  Syscalculator.Language.eng.lngpdk
  Syscalculator.Language.ned.lngpdk
  language-packages.json
```

The active language configuration can point at a package:

```ini
language=ned.lng
languagePackage=ned
```

The app first reads `language/<code>.lng` and help content from the package. If
the package is missing, invalid or not intended for `tiedragon.syscalculator`,
the app falls back to the loose `.lng` files and built-in help files. This keeps
daily builds usable while the package system grows.

Practical difference:

- `.lng` is only the language text file with `key=value` entries.
- `.objpdk` is the editable package source/concept for ToolEditor and agents.
- `.lngpdk` is the checked package that contains that `.lng` plus help HTML,
  NOD help, formula cards, images/media, metadata and SHA-256 checks.
- Use `.lng` for fallback/manual compatibility. Use `.objpdk` for authoring and
  basispackages. Use `.lngpdk` for release, updater and installer distribution.

Advantages and disadvantages:

- `.lng` is fast, readable and easy to repair manually. The disadvantage is that
  it cannot describe complete help packages, media, manifest identity or release
  checks. It also becomes awkward for long help text or HTML because links,
  markup, escaping and media references are hard to maintain inside language
  values.
- `.objpdk` is best for maintenance because it keeps templates, placeholders,
  assets and HTML editable. The disadvantage is that it is not a runtime release
  artifact and still needs compilation.
- `.lngpdk` is safer for distribution because the compiler validates structure,
  links, media, scripts and checksums. The disadvantage is that it needs a
  compile step, and once signed it must be treated as immutable release output.

The installer build runs package generation before publish, includes the
packages in update zips and writes the matching package key for the selected
installer language.

## Basispackage Merge

Use a basispackage when a concept package should be checked against a complete
reference package and automatically filled before compile:

```powershell
tools/Compile-LanguagePackage.ps1 .\concept .\out.lngpdk -BasePackage .\base.lngpdk
```

The basispackage can be a concept folder or an existing `.lngpdk`. The compiler:

- requires the same `producer`, `product` and `softwareId`;
- copies missing non-manifest files from the base package;
- maps missing language keys from `language/<base-code>.lng` into
  `language/<target-code>.lng`;
- never overwrites existing concept files or existing language keys;
- reports `basePackage`, `addedEntries` and `addedLanguageKeys` in agent JSON.

## Input Folder

Minimum:

```text
manifest.json
language/<code>.lng
```

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
  "packageVersion": "2026.05.21.001",
  "fallbackLanguage": "eng"
}
```

For another Tiedragon app, keep `producer` as `Tiedragon` and set `product` and
`softwareId` to that app. The compiler writes the same `softwareId` into the
package header. Each app reader remains responsible for accepting only its own
trusted `softwareId`.

For a new app, the recommended developer flow is:

1. Choose a stable `softwareId`, for example `tiedragon.myapp`.
2. Create an English basispackage with all required help topics, formula cards,
   UI keys and media.
3. Create translated concept packages from that basispackage.
4. Compile with `agent-compile-with-base` so missing files and missing keys are
   detected and added before release.
5. In the app, load only packages with the expected `softwareId` and reject
   packages that fail validation.

Agent error JSON example:

```json
{
  "success": false,
  "code": "E_FILE_BLOCKED",
  "error": "Blocked file type: scripts/build.bat"
}
```

Stable agent error codes:

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

Optional:

```text
help/Content/...
manual/...
assets/...
```

## Package Output

`compile`, `pack-language` and `agent-compile` create a wrapped `.lngpdk` file:

```text
SYSCALC-LNGPDK
format
headerLength
header JSON
ZIP payload
```

The header contains `softwareId`, `packageType`, `payloadFormat`,
`payloadSha256`, `encrypted`, `signed` and optional signature metadata fields.
The payload is currently written as ZIP, while the app reader can read ZIP and
7z-compatible payloads through the same `.lngpdk` extension.

The tool also prints `packageSha256`, the SHA-256 checksum of the complete
`.lngpdk` file. Use this value in release manifests, updater metadata or manual
checksum lists. `payloadSha256` protects the archive inside the wrapper;
`packageSha256` protects the distributed package file itself.

Agent JSON example:

```json
{
  "success": true,
  "outputPath": "C:\\packages\\Tiedragon.App.Language.ned.lngpdk",
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

Signed package flow:

```powershell
dotnet run --project src/Tiedragon.LanguagePackage -- create-signing-key `
  .\private\syscalculator-language-private.pem `
  .\language-package-trusted-keys.json `
  tiedragon.syscalculator.daily.2026

dotnet run --project src/Tiedragon.LanguagePackage -- compile-signed `
  .\concept `
  .\Syscalculator.Language.ned.lngpdk `
  .\private\syscalculator-language-private.pem `
  tiedragon.syscalculator.daily.2026
```

The reader verifies signed packages with `language-package-trusted-keys.json`.
No matching trusted key means reject fail-closed.

## Validation

The tool checks:

- manifest format, producer, product, software-id, package key, language code
  and display name;
- required `language/<code>.lng`;
- allowed file extensions from
  `src/Tiedragon.LanguagePackage/language-package-policy.ini`;
- blocked executable/script extensions from the same policy file;
- restricted JavaScript names from `package.allowedScripts` in the policy file;
- strict package paths from `package.allowedPrefixes` and
  `package.allowedRootHelpFiles` in the policy file;
- mojibake/encoding damage in `.lng`, HTML, JSON, CSS or JS;
- stored concept warning/banner markup inside help documents;
- broken internal HTML links;
- missing or non-image media references;
- release activation status based on required language-key coverage;
- unsafe paths such as absolute paths or `..`;
- maximum file count and file sizes;
- wrapper payload SHA-256;
- whole-package SHA-256 checksum reporting.
- signature verification for signed packages against trusted public keys.

## Failure Policy

Validation is fail-closed. If any required identity field, checksum, manifest
field, path rule, file type rule or size limit fails, the package must be
rejected. An app should not load partial content from a failed package; it
should continue with the previous valid package, loose `.lng` files or its
fallback language.

Common failures:

- wrong `producer`, unsafe `product`, unsafe `softwareId` or wrong `packageType`;
- mismatching `payloadSha256` or external `packageSha256`;
- missing `manifest.json` or `language/<code>.lng`;
- blocked executable/script files or unsupported script names;
- absolute paths or `..` path traversal;
- too many files or files that exceed package limits;
- encrypted packages before encryption support is implemented;
- signed packages without a trusted matching public key.
