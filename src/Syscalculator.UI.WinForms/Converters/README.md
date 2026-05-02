# Converter Groups

The main converter groups follow the original structure found in `broncode.zip`:

```text
Distance
euro
Mass
Pressure
Temperature
Volume
```

The `euro` group is expanded for Syscalculator 2.0. Besides the legacy files, it also includes newer country/currency files such as `BGN`, `CYP`, `EEK`, `HRK`, `LTL`, `LVL`, `MTL`, `SIT`, and `SKK`. These were missing from the old `broncode.zip` release set, so users could search for them without finding them.

Syscalculator 2.0 adds:

```text
Math
Text
```

Do not place generated or temporary `.nod` files in this tree. `Converters/Output` is local output and is excluded from publish/install.
