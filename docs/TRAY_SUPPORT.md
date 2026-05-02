# Tray support

Syscalculator heeft nu systeemvak/tray-ondersteuning.

## Gedrag

```text
minimaliseren -> naar systeemvak
venster sluiten met X -> naar systeemvak, als optie aan staat
dubbelklik tray icon -> herstellen
rechterklik tray icon -> menu
```

## Tray-menu

```text
Toon Syscalculator
Verberg naar systeemvak
WizardExpress
Calculator
Afsluiten
```

## Instelling

In menu `Tools`:

```text
Minimaliseren naar systeemvak
```

Deze optie staat standaard aan.

## Technisch

Aangepast in `MainForm.cs`:

```text
NotifyIcon
ContextMenuStrip
OnResize
OnFormClosing
CreateTrayIcon()
HideToTray()
RestoreFromTray()
```
