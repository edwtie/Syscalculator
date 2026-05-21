# Tiedragon LanguagePackage Tool

`Tiedragon.LanguagePackage` builds and checks Syscalculator `.lngpdk` language
packages.

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
validate <package.lngpdk>
inspect <package.lngpdk>
```

`compile` and `pack-language` are equivalent. `agent-compile` is the same
compiler with JSON output, intended for AI agents, scripts and release
automation.

PowerShell helper:

```powershell
tools/Compile-LanguagePackage.ps1 <concept-folder> <output.lngpdk>
```

The helper runs `agent-compile`, so callers can parse `success`, `code`,
`packageSha256`, `payloadSha256`, `languageCode`, `packageKey` and `entryCount`
without scraping human-readable text.

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
E_REQUIRED_FILE
E_PATH_UNSAFE
E_FILE_BLOCKED
E_FILE_UNSUPPORTED
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
  "outputPath": "C:\\packages\\Syscalculator.Language.ned.lngpdk",
  "packageKey": "ned",
  "languageCode": "ned",
  "displayName": "Nederlands",
  "entryCount": 120,
  "packageSha256": "64 hex characters",
  "payloadSha256": "64 hex characters",
  "encrypted": false,
  "signed": false
}
```

## Validation

The tool checks:

- manifest format, producer, product, software-id, package key, language code
  and display name;
- required `language/<code>.lng`;
- allowed file extensions;
- blocked executable/script extensions;
- unsafe paths such as absolute paths or `..`;
- maximum file count and file sizes;
- wrapper payload SHA-256;
- whole-package SHA-256 checksum reporting.

## Failure Policy

Validation is fail-closed. If any required identity field, checksum, manifest
field, path rule, file type rule or size limit fails, the package must be
rejected. Syscalculator should not load partial content from a failed package;
it should continue with the previous valid package, loose `.lng` files or the
English fallback.

Common failures:

- wrong `producer`, `product`, `softwareId` or `packageType`;
- mismatching `payloadSha256` or external `packageSha256`;
- missing `manifest.json` or `language/<code>.lng`;
- blocked executable/script files;
- absolute paths or `..` path traversal;
- too many files or files that exceed package limits;
- encrypted packages before encryption support is implemented.
