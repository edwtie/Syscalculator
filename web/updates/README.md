# Syscalculator Update Endpoint

Upload these files to the PHP-capable update endpoint folder, for example:

```text
/api/syscalculator-updates/updater.php
/api/syscalculator-updates/syscalculator.json
```

The public manifest URL used by Syscalculator is:

```text
https://www.tiedragon.com/api/syscalculator-updates/syscalculator.json
```

If the host maps `syscalculator.json` to PHP through routing, use `updater.php` as
the target. The PHP file deliberately reads `syscalculator.json` instead of
duplicating daily metadata, so `packageId` and `sha256` cannot drift apart.

After uploading, verify:

```powershell
curl.exe -L https://www.tiedragon.com/api/syscalculator-updates/syscalculator.json
```

The daily channel must show the same `version`, `packageId`, and `sha256` as
`web/updates/syscalculator.json` in this repository.

The project has a WinSCP-based deploy helper for the live endpoints:

```cmd
DEPLOY_UPDATE_MANIFEST.bat
```

It uses the saved WinSCP session `edwtie@tiedragon.nl`, uploads
`syscalculator.json` and `updater.php` to both `tiedragon.com` and
`tiedragon.nl`, and then validates the live daily `version`, `packageId` and
`sha256`.
