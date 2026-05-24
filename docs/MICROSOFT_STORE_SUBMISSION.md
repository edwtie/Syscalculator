# Microsoft Store And MSIX Submission

Syscalculator uses MSIX from the Beta line onward. Daily builds stay outside MSIX.

- Daily: no MSIX package.
- Beta: MSIX test package, Inno Setup installer and updater zip.
- Release/production: MSIX package, Inno Setup installer and updater zip.

Production is the public Microsoft Store submission line. Beta MSIX packages use package flights when Store-based tester distribution is needed.

## Beta Package Flights

Use Microsoft Store package flights for Beta MSIX testing when testers should receive a Store-delivered package before production.

Official Microsoft reference:

- Package flights: https://learn.microsoft.com/nl-nl/windows/apps/publish/package-flights

Rules:

- Package flights are allowed for Beta MSIX packages.
- Daily builds are not submitted as package flights.
- A package flight still goes through the Microsoft certification approving procedure before testers receive it.
- Store listing text stays the same for all customers; only the package differs for the selected tester group.
- Testers must be added to a known user group / flight group in Partner Center.
- Regular customers keep receiving the non-flighted production package.
- Fix WACK/certification issues before using the package as a public production release, even if flighting allows some issues with notes.
- If the Beta package is approved and ready, the same package can later be copied into a non-flighted production submission.

## Recommended Production Route

Use the current production MSIX package for Microsoft Store package validation. Keep the Inno Setup `.exe` installer as the non-Store production route for users who do not want to install through the Microsoft Store. Keep the production updater zip available for the app's built-in update flow.

Official Microsoft references:

- Get started: https://learn.microsoft.com/en-us/windows/apps/publish/faq/get-started-with-the-microsoft-store
- Distribute Win32 apps: https://learn.microsoft.com/en-us/windows/apps/distribute-through-store/how-to-distribute-your-win32-app-through-microsoft-store
- MSI/EXE package upload: https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msi/upload-app-packages
- MSI/EXE package requirements: https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msi/app-package-requirements

## Production Rules

- Channel: `production`
- Do not submit Daily packages.
- Submit Beta MSIX only when using a deliberate tester/private-audience submission.
- Keep Appx/MSIX capabilities minimal. The UWP manifest declares no capabilities until code needs them.
- The WinForms MSIX generator keeps `internetClient` because updater and feedback use HTTPS.
- The WinForms MSIX generator keeps `runFullTrust` because `Syscalculator.exe` starts through `Windows.FullTrustApplication`; removing it creates a package that cannot run as the desktop app.
- Use a versioned HTTPS installer URL, not a mutable generic file name.
- Keep the installer version date-only when possible, for example `2.0.2026.05.17`.
- Keep Store release notes user-facing and short.
- Do not mention internal build numbers in Store text.
- Sign the installer and bundled executable files before Store submission.
- Store installers are self-contained so silent install and first startup do not fail when the .NET Desktop Runtime is missing or not detected. Runtime diagnostics can still show whether a local shared runtime is missing, equal to, older than or newer than the bundled runtime.

## Helper Commands

```bat
STORE_RELEASE.bat
STORE_RELEASE.bat hash
STORE_RELEASE.bat build
STORE_MSIX.bat
```

`STORE_RELEASE.bat` uses the production channel for the Inno Setup installer and updater zip. `STORE_MSIX.bat` creates the production MSIX package. They are intentionally separate from `BETA_RELEASE.bat` and `BETA_MSIX.bat`.

## Code Signing Check

Microsoft recommends signing MSI/EXE Store packages, and the MSI/EXE package requirements state that the installer binary and all bundled PE files must be digitally signed with a code signing certificate that chains to a CA in the Microsoft Trusted Root Program.

Current local status:

```powershell
Get-AuthenticodeSignature "artifacts\installer\Syscalculator-2.0-production-2.0.2026.05.18.exe"
```

Expected before Store submission:

```text
Status: Valid
```

Current result before signing:

```text
Status: NotSigned
```

Use the Windows SDK signing tool after a trusted code signing certificate is installed:

```bat
"C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe" sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /n "Tiedragon" "artifacts\installer\Syscalculator-2.0-production-2.0.2026.05.18.exe"
```

Then verify:

```bat
"C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe" verify /pa /all "artifacts\installer\Syscalculator-2.0-production-2.0.2026.05.18.exe"
```

Notes:

- A self-signed certificate is not acceptable for Store MSI/EXE submission.
- The installer must be re-uploaded to the versioned HTTPS URL after signing because signing changes the file hash.
- Do not modify the binary behind the submitted URL after Partner Center submission.

## Standard Install Scenario

Use the production installer, not Daily or Beta.

Install command:

```bat
Syscalculator-2.0-production-2.0.2026.05.18.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /RESTARTEXITCODE=3010
```

Standard install scenario return codes:

| Scenario | Return code | Documentation URL |
| --- | ---: | --- |
| Successful install | 0 | `https://www.tiedragon.com/install/exit-codes.html#0` |
| Installation cancelled by user | 5 | `https://www.tiedragon.com/install/exit-codes.html#5` |
| Application already exists | 1638 | `https://www.tiedragon.com/install/exit-codes.html#1638` |
| Another installation is already in progress | 1618 | `https://www.tiedragon.com/install/exit-codes.html#1618` |
| Disk space is full | 4 | `https://www.tiedragon.com/install/exit-codes.html#4` |
| Reboot required | 3010 | `https://www.tiedragon.com/install/exit-codes.html#3010` |
| Network failure | 1 | `https://www.tiedragon.com/install/exit-codes.html#1` |
| Package rejected during installation | 7 | `https://www.tiedragon.com/install/exit-codes.html#7` |
| Miscellaneous install failure scenarios | 3 | `https://www.tiedragon.com/install/exit-codes.html#3` |

Uninstall command:

```bat
"%LOCALAPPDATA%\Programs\Syscalculator\unins000.exe" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
```

Daily, Beta and Production installers are self-contained. Do not submit Daily or Beta to production Store validation, but keep their packaging runtime policy the same so testers do not hit a `.NET 10` startup prompt.

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
