# Localization / Language Package Design

## Goal

Localization is a Tiedragon domain, not only a Syscalculator feature.
Syscalculator help and manuals are growing beyond a small set of loose `.lng`
files, so a language package groups all language-dependent material in one
installable `.lngpdk` file while keeping the current simple fallback behavior.
The same technique is intended for developers who want to build their own app
with a clean separation between program code and localized help, UI text,
formula cards and media.

The design must support:

- UI labels and messages;
- NOD help pages;
- formula cards;
- manuals and writer content;
- screenshots/SVG help assets when they are language-specific;
- fallback to English when a translation is incomplete;
- manual installation by copying one `.lngpdk` file;
- future update/download through the updater.

For app developers, the model is:

- define a stable app identity in `manifest.json`;
- publish an official basispackage for the app;
- let language packages inherit/check against that basispackage;
- keep package validation in the app reader strict and fail-closed.

## Current State

Today the WinForms app loads language files from:

```text
<app>\eng.lng
<app>\ned.lng
<app>\deu.lng
<app>\Languages\*.lng
```

Help content is copied as normal files:

```text
Resources/Help
Resources/Help/Content
```

The Help layer already supports `[some.lng.key]` placeholders and falls back to
English through `LanguageCatalog` and `HelpApi`.

## Repository Boundary

`Tiedragon.Localization` is the logical owner for language packages, help
content, formula cards and package media. Inside this repository the current
projects are:

```text
src/Tiedragon.LanguagePackage
src/Tiedragon.ToolEditor
src/syscalculator/LanguagePackageService.cs
```

The first two projects are intentionally standalone. They can later move to a
separate git repository for `Tiedragon.Localization`, because the package
compiler and editor are useful for other Tiedragon apps as well. Syscalculator
should then stay a consumer: it loads trusted `.lngpdk` packages and keeps only
the app-specific reader/fallback integration.

## Package Format

Language packages use a Syscalculator-specific extension:

```text
Syscalculator.Language.<code>.lngpdk
```

The current reader supports two `.lngpdk` forms:

- wrapped Syscalculator packages with a `SYSCALC-LNGPDK` header;
- legacy direct archives, where the file itself is a ZIP or 7z-compatible
  archive.

Legacy `.zip` files remain readable for compatibility, but new packages should
use:

```text
*.lngpdk
```

The contents should stay plain text and editable:

```text
manifest.json
language/<code>.lng
help/Content/...
manual/...
assets/...
```

For wrapped packages, the outer file has a Syscalculator header and the payload
is the archive that contains these files:

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
  "payloadSha256": "64 lowercase or uppercase hex characters",
  "encrypted": false,
  "signed": false
}
```

The reader verifies `softwareId`, `packageType`, payload size and
`payloadSha256` before reading content from the payload archive. Release and
update metadata should also publish the SHA-256 checksum of the whole `.lngpdk`
file as `packageSha256`; that value is external to the package because adding it
inside the file would change the file hash.

Example:

```text
Syscalculator.Language.ned.lngpdk
├─ manifest.json
├─ language/
│  └─ ned.lng
├─ help/
│  └─ Content/
│     ├─ main/
│     ├─ nod/full/
│     ├─ nod/command/
│     └─ nod/popup/
├─ manual/
│  ├─ index.html
│  ├─ user-guide.html
│  └─ release-notes.html
└─ assets/
   ├─ screenshots/
   └─ svg/
```

## Manifest

`manifest.json` describes the package before the app loads content from it.

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
  "packageVersion": "2026.05.21.001",
  "fallbackLanguage": "eng",
  "contains": {
    "ui": true,
    "help": true,
    "manual": true,
    "assets": true
  }
}
```

Rules:

- `format` is the package format version, not the app version.
- `key` is the package identity used by `language.cfg`; it is read from inside
  the package, so the package file name does not matter.
- `id` is a longer publisher/package identifier and remains supported as a
  fallback when older packages do not contain `key`.
- `producer`, `product` and `softwareId` bind the package to the trusted
  Tiedragon Syscalculator package line.
- `languageCode` stays compatible with current file names such as `ned.lng`.
- `fallbackLanguage` should normally be `eng`.
- `packageVersion` can follow the daily/beta release number or a separate
  language-package number.

## Install Location

Recommended runtime location for packages that remain compressed:

```text
<app>\LanguagePackages\<package-key>.lngpdk
<app>\LanguagePackages\Cache\<package-key>.lngpdk
```

Extracted folders are still supported for compatibility and manual editing:

```text
<app>\LanguagePackages\<package-id>\
```

The package cache location is:

```text
<app>\LanguagePackages\Cache\
```

Compressed package example:

```text
LanguagePackages/
|-- ned.lngpdk
`-- Cache/
    `-- ned.lngpdk
```

Extracted compatibility example:

```text
LanguagePackages/
├─ Cache/
│  └─ Syscalculator.Language.ned.lngpdk
└─ syscalculator.language.ned/
   ├─ manifest.json
   ├─ language/ned.lng
   ├─ help/Content/...
   └─ manual/...
```

## Loading Order

The current loose-file system should keep working. New package support should
extend the lookup order instead of replacing it.

Recommended lookup order for UI text:

```text
1. selected `.lngpdk` language package: language/<code>.lng
2. <app>\Languages\<code>.lng
3. <app>\<code>.lng
4. selected extracted language package: language/<code>.lng
5. selected package fallback: language/eng.lng if included
6. <app>\eng.lng
7. built-in fallback string from code
```

Recommended lookup order for help/manual content:

```text
1. selected language package help/manual file
2. current Resources/Help/Content file
3. language key override from .lng
4. English fallback content
5. built-in fallback string from code
```

## Safety Rules

Language packages are content, not code.

Allowed:

- `.lng`
- `.html`
- `.css`
- `.js` from trusted Syscalculator package only
- `.svg`
- `.png`, `.jpg`, `.webp`
- `.json`

Blocked:

- `.exe`
- `.dll`
- `.bat`
- `.cmd`
- `.ps1`
- absolute paths
- `..` path traversal
- files that would escape `LanguagePackages` if extracted later

Package reading and optional extraction must validate every entry path before
use.

Current limits:

- maximum header size: 64 KiB;
- maximum payload size: 192 MiB;
- maximum files per package: 2048;
- maximum single file size: 16 MiB;
- maximum total uncompressed file size: 128 MiB.

## Failure Policy

Syscalculator uses a fail-closed policy for language packages. If a package does
not pass validation, the package is rejected and its content is not loaded. The
app should keep using the current loose `.lng` files, another valid selected
language package, or the built-in English fallback.

The package is rejected when:

- the wrapper header is incomplete, malformed or uses an unsupported format;
- `softwareId`, `packageType`, `producer` or `product` do not match the trusted
  Tiedragon Syscalculator values;
- `payloadSha256` does not match the payload archive;
- release/update metadata supplies a `packageSha256` that does not match the
  downloaded `.lngpdk` file;
- `manifest.json` is missing, malformed or contains an unsafe package key or
  language code;
- `language/<code>.lng` is missing;
- the package contains blocked file types such as `.exe`, `.dll`, `.bat`,
  `.cmd` or `.ps1`;
- an entry uses an absolute path, `..` traversal or another path that could
  escape the package boundary;
- the package exceeds file count, entry size, payload size or total
  decompressed-size limits;
- the package declares encryption while encrypted language packages are not yet
  supported.

On failure, package content must not be partially trusted. A failed package is a
data problem or a security problem, so the correct behavior is to skip it and use
a known-good fallback.

## Zip Library

`.lngpdk` can be a ZIP container or a 7z-style archive internally. ZIP support
comes from .NET:

```csharp
System.IO.Compression.ZipArchive
System.IO.Compression.ZipFile
```

7z-compatible reading is handled in managed code through `SharpCompress`, so the
app does not need a separate `7z.exe` or native 7z DLL for language packages.

This matches the existing updater direction, which already extracts ZIP update
packages.

## Implemented Components

Current code:

```text
src/Tiedragon.LanguagePackage
src/syscalculator/LanguagePackageService.cs
src/syscalculator/LanguageCatalog.cs
src/syscalculator/LanguageSelectionForm.cs
```

Responsibilities:

- discover installed packages;
- read `manifest.json`;
- read and validate the optional `SYSCALC-LNGPDK` wrapper header;
- verify the payload SHA-256 hash when present;
- print the whole package SHA-256 checksum for release/update metadata;
- validate package paths;
- read language packages directly from `.lngpdk` archives, including ZIP and
  7z-compatible containers;
- enforce package size, file count and per-file limits;
- build, inspect and validate `.lngpdk` packages through
  `Tiedragon.LanguagePackage`;
- keep extracted language package folders compatible;
- block package code files such as `.exe`, `.dll`, `.bat`, `.cmd`, `.ps1`;
- return the active `.lng` content;
- keep loose `.lng` files compatible.

Not implemented yet:

- signing verification;
- encrypted package payloads;
- help/manual content overrides;
- package removal UI;
- package install button in the language dialog.

Logical package boundary:

```text
Tiedragon.Localization
|-- Tiedragon.LanguagePackage
|-- Tiedragon.ToolEditor
`-- package schemas and validation docs
```

Syscalculator boundary:

```text
syscalculator
|-- LanguagePackageService
|-- LanguageCatalog integration
`-- Help/Resources fallback integration
```

## Language Configuration

Current config:

```text
language=ned.lng
```

Compatible extension:

```text
language=ned.lng
languagePackage=ned
```

`languagePackage` is the manifest `key`, not the package file name. If
`languagePackage` is absent, Syscalculator behaves exactly as today.

## Versioning and Updates

Language packages can update separately from the app.

Useful fields:

```text
appMinVersion
appMaxVersion
packageVersion
contentBuild
```

Daily release can ship built-in `.lng` files and optional `.lngpdk` packages.
Beta and production can later download or install language packages through the
updater.

## Migration Plan

### Phase 1: Design and compatibility

- Keep all current `.lng` files.
- Add this design document.
- Add package manifest format.
- Status: done.

### Phase 2: Package reader

- Add `LanguagePackageService`.
- Read installed package manifests.
- List packages in the language dialog.
- Load `language/<code>.lng` from package when selected.
- Status: implemented for UI language text.

### Phase 3: Help/manual override

- Allow `HelpApi.Content` to ask the package service for content override.
- Keep `Resources/Help/Content` as fallback.
- Add package manual pages.
- Status: first runtime override is implemented for main help, NOD help and
  legal/help pages. The active language package is checked first, then the
  built-in help content remains the fallback.

### Phase 4: Package installer

- Add “Install language package...” button.
- Validate `.lngpdk`.
- Copy the `.lngpdk` into `LanguagePackages\Cache` without requiring extraction.
- Save `languagePackage=...` in `language.cfg`.
- Status: package validation and direct `.lngpdk` reading exist; UI button is
  implemented in the language selection dialog. Selecting the installed package
  uses the existing `languagePackage=...` configuration path.

### Phase 5: Release/update integration

- Publish official language packages next to daily/beta builds.
- Optionally let updater fetch package updates.

## Open Questions

- Should official packages be signed or checksum-verified before loading?
- Should user-installed packages allow JavaScript, or should JS only be allowed
  in built-in trusted packages?
- Should manuals be HTML-only, Markdown-only, or both?
- Should package content override individual help files or whole help sections?
- Should old loose `.lng` files remain forever for portable/manual editing?

## Recommendation

Start conservative:

1. Use `.lngpdk`.
2. Use built-in `System.IO.Compression`.
3. Keep loose `.lng` support.
4. Add package loading only for UI text first.
5. Add help/manual overrides after the language dialog can list packages.

This keeps the current daily line stable while making the help/manual system
ready for larger multilingual content.
