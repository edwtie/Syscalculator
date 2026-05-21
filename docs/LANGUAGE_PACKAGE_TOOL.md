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
compile <input-folder> <output.lngpdk>
pack-language <input-folder> <output.lngpdk>
agent-compile <input-folder> <output.lngpdk>
agent-compile-with-base <base-folder-or-package> <input-folder> <output.lngpdk>
validate <package.lngpdk>
inspect <package.lngpdk>
```

`compile` and `pack-language` are equivalent. `agent-compile` is the same
compiler with JSON output, intended for AI agents, scripts and release
automation. `agent-compile-with-base` first merges a basispackage into the
concept folder before compiling.

PowerShell helper:

```powershell
tools/Compile-LanguagePackage.ps1 <concept-folder> <output.lngpdk>
tools/Compile-LanguagePackage.ps1 <concept-folder> <output.lngpdk> -BasePackage <base-folder-or-package>
```

The helper runs `agent-compile`, so callers can parse `success`, `code`,
`packageSha256`, `payloadSha256`, `languageCode`, `packageKey` and `entryCount`
without scraping human-readable text.

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

The header contains `softwareId`, `packageType`, `payloadFormat` and
`payloadSha256`. The payload is currently written as ZIP, while the app reader
can read ZIP and 7z-compatible payloads through the same `.lngpdk` extension.

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
  "basePackage": "",
  "addedEntries": [],
  "addedLanguageKeys": []
}
```

## Validation

The tool checks:

- manifest format, producer, product, software-id, package key, language code
  and display name;
- required `language/<code>.lng`;
- allowed file extensions;
- blocked executable/script extensions;
- restricted JavaScript names: only `basis.js`, `nod.js` and `formula.js`;
- strict package paths for language, help/manual/NOD/formula content and media;
- mojibake/encoding damage in `.lng`, HTML, JSON, CSS or JS;
- stored concept warning/banner markup inside help documents;
- broken internal HTML links;
- missing or non-image media references;
- release activation status based on required language-key coverage;
- unsafe paths such as absolute paths or `..`;
- maximum file count and file sizes;
- wrapper payload SHA-256;
- whole-package SHA-256 checksum reporting.

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
- encrypted packages before encryption support is implemented.
