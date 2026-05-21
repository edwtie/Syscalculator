# Tiedragon LanguagePackage Tool

`Tiedragon.LanguagePackage` builds and checks Syscalculator `.lngpdk` language
packages.

## Commands

```text
pack-language <input-folder> <output.lngpdk>
validate <package.lngpdk>
inspect <package.lngpdk>
```

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

Optional:

```text
help/Content/...
manual/...
assets/...
```

## Package Output

`pack-language` creates a wrapped `.lngpdk` file:

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
