# Inno Setup Installer

The old `broncode.zip` contains historical Inno Setup scripts for the VB6 release. Those scripts are useful as reference, but they still point to `C:\broncode`, `freesyscal.exe`, and other 1.x files.

Syscalculator 2.0 uses the modern installer script:

```bat
BUILD_INSTALLER.bat daily
BUILD_INSTALLER.bat beta
BUILD_INSTALLER.bat production
```

The script first runs `dotnet publish` for `win-x64`, then calls `ISCC.exe` to build the installer from `installer\Syscalculator.iss`.

Output is written to:

```text
artifacts\installer
```

The installer includes:

- Syscalculator shortcut
- NOD Editor shortcut using `-tool`
- NOD Template Wizard shortcut using `-wizardtool`
- `.nod` file association
- grouped `.nod` converter directories based on the legacy `broncode.zip` structure
- uninstall support

`ISCC.exe` must be available in PATH. Install Inno Setup 6 if the command is missing.
