# Language Package Design

## Goal

Syscalculator help and manuals are growing beyond a small set of loose `.lng`
files. A language package should group all language-dependent material in one
installable ZIP file, while keeping the current simple fallback behavior.

The design must support:

- UI labels and messages;
- NOD help pages;
- formula cards;
- manuals and writer content;
- screenshots/SVG help assets when they are language-specific;
- fallback to English when a translation is incomplete;
- manual installation by copying one ZIP file;
- future update/download through the updater.

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

## Package Format

Language packages should use a normal ZIP archive with a clear extension:

```text
Syscalculator.Language.<code>.zip
```

Optional friendly extension for later:

```text
*.syslang
```

The contents should stay plain text and editable:

```text
manifest.json
language/<code>.lng
help/Content/...
manual/...
assets/...
```

Example:

```text
Syscalculator.Language.ned.zip
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
  "id": "syscalculator.language.ned",
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
- `languageCode` stays compatible with current file names such as `ned.lng`.
- `fallbackLanguage` should normally be `eng`.
- `packageVersion` can follow the daily/beta release number or a separate
  language-package number.

## Install Location

Recommended runtime location for packages that remain compressed:

```text
<app>\LanguagePackages\<package-id>.zip
<app>\LanguagePackages\Cache\<package-id>.zip
```

Extracted folders are still supported for compatibility and manual editing:

```text
<app>\LanguagePackages\<package-id>\
```

The ZIP cache location is:

```text
<app>\LanguagePackages\Cache\
```

Compressed package example:

```text
LanguagePackages/
|-- syscalculator.language.ned.zip
`-- Cache/
    `-- syscalculator.language.ned.zip
```

Extracted compatibility example:

```text
LanguagePackages/
├─ Cache/
│  └─ Syscalculator.Language.ned.zip
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
1. selected language package ZIP: language/<code>.lng
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

ZIP reading and optional extraction must validate every entry path before use.

## Zip Library

.NET already includes the required ZIP API:

```csharp
System.IO.Compression.ZipArchive
System.IO.Compression.ZipFile
```

No new NuGet package is needed for the first implementation.

This matches the existing updater direction, which already extracts ZIP update
packages.

## Implemented Components

Current code:

```text
src/syscalculator/LanguagePackageService.cs
src/syscalculator/LanguageCatalog.cs
src/syscalculator/LanguageSelectionForm.cs
```

Responsibilities:

- discover installed packages;
- read `manifest.json`;
- validate package paths;
- read language packages directly from ZIP archives;
- keep extracted language package folders compatible;
- block package code files such as `.exe`, `.dll`, `.bat`, `.cmd`, `.ps1`;
- return the active `.lng` content;
- keep loose `.lng` files compatible.

Not implemented yet:

- help/manual content overrides;
- package removal UI;
- package install button in the language dialog.

Later, if this grows, it can move to:

```text
src/Tiedragon.Help
```

or a new:

```text
src/Tiedragon.Localization
```

## Language Configuration

Current config:

```text
language=ned.lng
```

Compatible extension:

```text
language=ned.lng
languagePackage=syscalculator.language.ned
```

If `languagePackage` is absent, Syscalculator behaves exactly as today.

## Versioning and Updates

Language packages can update separately from the app.

Useful fields:

```text
appMinVersion
appMaxVersion
packageVersion
contentBuild
```

Daily release can ship built-in `.lng` files and optional ZIP packages. Beta and
production can later download or install language packages through the updater.

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

### Phase 4: Package installer

- Add “Install language package...” button.
- Validate ZIP.
- Copy the ZIP into `LanguagePackages\Cache` without requiring extraction.
- Save `languagePackage=...` in `language.cfg`.
- Status: ZIP validation and direct ZIP reading exist; UI button is still roadmap.

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

1. Use `.zip`.
2. Use built-in `System.IO.Compression`.
3. Keep loose `.lng` support.
4. Add package loading only for UI text first.
5. Add help/manual overrides after the language dialog can list packages.

This keeps the current daily line stable while making the help/manual system
ready for larger multilingual content.
