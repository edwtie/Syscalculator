# Inno Setup Installers

The old `broncode.zip` contains historical Inno Setup scripts for the VB6 release. Those scripts are useful as reference, but they still point to `C:\broncode`, `freesyscal.exe`, and other 1.x files.

## Syscalculator 2.0 Beta 1 Preview

Syscalculator 2.0 beta 1 (preview) uses the modern installer script:

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
- Microsoft .NET 10 Desktop Runtime x64 prerequisite check
- grouped `.nod` converter directories based on the legacy `broncode.zip` structure
- uninstall support

Because the 2.0 installer is framework-dependent, it checks for `Microsoft.WindowsDesktop.App` 10.x before setup continues. When the runtime is missing, setup offers to open the official Microsoft .NET 10 Desktop Runtime download page and then stops so the user can install the runtime first.

`ISCC.exe` must be available in PATH. Install Inno Setup 6 if the command is missing.

## Syscalculator 1.74 RC2

The old VB6 line has a separate Inno Setup 6 script:

Syscalculator 1.74 RC2 is planned as the final Syscalculator 1.x legacy release candidate for the old VB6 line. It keeps the classic NOD converter workflow available while reducing the risks around modern Windows installation and help files.

The 1.74 legacy package also carries the newer euro countries, including recent euro adoptions such as Croatia 2023 and Bulgaria 2026.

```text
installer\Syscalculator174.iss
```

Build it with:

```bat
BUILD_SYSCALCULATOR174_INSTALLER.bat -Channel rc2
```

The script creates the installer from the current 1.74 files. Use the separate VB6 compile task first when you need a fresh `freesyscal.exe`.

```bat
BUILD_SYSCALCULATOR174_VB6_CANDIDATE.bat
```

Syscalculator 1.74 is a VB6 application. The installer checks for the Visual Basic 6 Runtime (`msvbvm60.dll`) and, when it is missing, offers to open the official Microsoft download page before stopping setup. The VB6 runtime is outdated, so 1.74 includes the last compatibility fixes for that old runtime:

```text
https://support.microsoft.com/en-us/topic/description-of-the-cumulative-update-rollup-for-the-visual-basic-6-0-service-pack-6-runtime-extended-files-e02acc79-217b-fc0a-6edc-540403af2081
```

Output is written to:

```text
artifacts\legacy\installer
```

This installer uses a separate install directory:

```text
%LOCALAPPDATA%\Programs\Syscalculator 1.74
```

It is intentionally separate from the Syscalculator 2.0 beta 1 preview installer.
