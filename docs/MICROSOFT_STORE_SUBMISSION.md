# Microsoft Store Production Submission

Syscalculator uses the Microsoft Store only for production releases. Daily and Beta builds stay outside the Store.

## Recommended Route

Use the current Inno Setup `.exe` installer first. Microsoft supports listing an existing Win32 EXE/MSI app by giving Partner Center a versioned HTTPS download URL. That fits the current Syscalculator release flow better than converting to MSIX immediately.

Official Microsoft references:

- Get started: https://learn.microsoft.com/en-us/windows/apps/publish/faq/get-started-with-the-microsoft-store
- Distribute Win32 apps: https://learn.microsoft.com/en-us/windows/apps/distribute-through-store/how-to-distribute-your-win32-app-through-microsoft-store
- MSI/EXE package upload: https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msi/upload-app-packages
- MSI/EXE package requirements: https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msi/app-package-requirements

## Production Rules

- Channel: `production`
- Do not submit Daily or Beta installers.
- Use a versioned HTTPS installer URL, not a mutable generic file name.
- Keep the installer version date-only when possible, for example `2.0.2026.05.17`.
- Keep Store release notes user-facing and short.
- Do not mention internal build numbers in Store text.
- Sign the installer and bundled executable files before Store submission.
- Production Store installers are self-contained so the Store silent install does not fail when the .NET Desktop Runtime is missing.

## Helper Commands

```bat
STORE_RELEASE.bat
STORE_RELEASE.bat hash
STORE_RELEASE.bat build
```

`STORE_RELEASE.bat` uses the production channel. It is intentionally separate from `BETA_RELEASE.bat`.

## Standard Install Scenario

Use the production installer, not Daily or Beta.

Install command:

```bat
Syscalculator-2.0-production-2.0.2026.05.18.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
```

Uninstall command:

```bat
"%LOCALAPPDATA%\Programs\Syscalculator\unins000.exe" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
```

Daily and Beta installers are framework-dependent and can stop when Microsoft .NET Desktop Runtime is missing. Production installers are built self-contained to avoid the Store error `Installation cancelled by user`.

## Partner Center Checklist

- Create or use a Microsoft Partner Center developer account.
- Reserve the product name `Syscalculator`.
- Create an MSI/EXE submission for a Win32 desktop app.
- Add the versioned HTTPS installer URL.
- Add screenshots, description, privacy/support links, category, and age rating.
- Use the public privacy page, for example `https://www.tiedragon.com/privacy/syscalculator-en.html`.
- Submit for certification only after the same installer has passed local production smoke tests.

## Production Smoke Test

- Clean install on Windows 11.
- Start menu shortcut opens the app.
- A second launch activates the existing app window.
- About shows production/stable wording, not Beta or Daily.
- Feedback sends successfully and validates e-mail addresses.
- Updater does not offer a Beta or Daily package unless the user explicitly switches channel.
- Uninstall removes the app cleanly.
