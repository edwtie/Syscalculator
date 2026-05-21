# Language Package Tool

`Syscalculator.PackageTool` builds and checks Syscalculator `.lngpdk` language
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

## Validation

The tool checks:

- manifest format, package key, language code and display name;
- required `language/<code>.lng`;
- allowed file extensions;
- blocked executable/script extensions;
- unsafe paths such as absolute paths or `..`;
- maximum file count and file sizes;
- wrapper payload SHA-256.
