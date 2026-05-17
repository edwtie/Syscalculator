# Syscalculator 2.0 Beta Roadmap

## Beta 1 vs Beta 2

| Area | Beta 1 - 2026-05-16 | Beta 2 - 2026-05-17 |
| --- | --- | --- |
| Release goal | First cleaned tester build after the early Beta 1 refreshes. | Promote the fixed Daily work into a tester-ready Beta 2. |
| Version display | Date-only installer version `2.0.2026.05.16`. | Date-only installer version `2.0.2026.05.17`. |
| Distribution | Installer only. Public download path used `syscal20beta1.exe`. | Installer plus zip updater package. GitHub release assets and hosted update package are both available. |
| Update channel | Beta existed as a channel, but without the full zip updater package flow. | Beta channel has manifest `packageUrl`, `packageId`, and SHA256 for same-date update detection. |
| About screen | Release channel could still show Daily because the app used a hardcoded channel. | Release channel is detected from `update-state.cfg`, so installed beta packages show Beta. |
| Help text | Some help pages still mentioned Daily build/version wording. | Help text uses channel-neutral test-build wording. |
| Graph preview | Included the promoted 2026-05-16 graph preview and mini preview fixes. | Adds live mini preview to graph sync, precision fixes, Planck-scale labels, ultra-small line thickening, and pico-scale detail-grid cutoff. |
| Feedback | Basic feedback entry point existed. | Feedback UI polished, hosted feedback delivery added, e-mail validation fixed, thank-you card added, and OK close behavior fixed. |
| Updater | Installer-centric beta delivery. | Standalone zip updater, localized updater text, progress fixes, automatic/manual update checks, and Daily/Beta channel selection. |
| Startup behavior | Earlier single-instance behavior was not final. | Second app launch activates the existing window instead of opening another instance. |
| Tray behavior | Tray support existed. | Repeated Windows tray balloon notification removed. |

## Beta 2 Current State

- Release: `v2.0.2026.05.17-beta`
- Installer: `Syscalculator-2.0-beta-2.0.2026.05.17.exe`
- Update zip: `Syscalculator-2.0-beta-2.0.2026.05.17.zip`
- Package marker: `beta-2026.05.17-beta2-help-001`
- Update zip SHA256: `54a39b441fb2d4f55b53bf12d031549674f0d2bdff8ffba0f27da830c07db1c3`
- Status: tester-ready, but still not production.

## Roadmap

### Beta 2 Stabilization

- Smoke test install, update, and uninstall on the main Windows 11 machine.
- Confirm About shows `Beta` after installing through the beta updater.
- Confirm Config -> Update channel can switch between Daily and Beta without repeated prompts.
- Confirm the beta updater reaches 100% visually and then restarts cleanly.
- Recheck Feedback delivery from the hosted endpoint and invalid e-mail validation.
- Recheck Help wording in Dutch and English after install, not only from source.

### Beta 3 Candidate

- Add a small in-app indicator when an update is already installed for the selected channel.
- Make beta release notes visible from About or Help with the current channel context.
- Add a simple diagnostics export for updater state: channel, installed package marker, manifest URL, latest package marker.
- Add focused regression tests around update manifest parsing and package marker comparison.
- Review all bundled language files for missing About/update/help keys.
- Decide whether beta should keep both installer and zip package, or whether the zip updater becomes the primary beta path.

### Production Readiness

- Freeze user-facing labels for About, updater, Feedback, Help, and Config.
- Confirm no help page describes Daily-only behavior unless the user is actually in Daily channel documentation.
- Check clean install on a fresh Windows profile.
- Check update from Beta 1 to Beta 2/Beta 3.
- Check update from Daily to Beta does not downgrade or loop.
- Verify .NET Desktop Runtime and WebView2 runtime requirements are documented in release notes.
- Keep experimental NOD 2.1 ideas clearly marked as future/research.

### Production Release Gate

Production can be considered when:

- Installer succeeds on the intended Windows targets.
- Beta updater succeeds repeatedly without hash mismatch or repeated prompts.
- About, Help, Feedback, WizardExpress, NOD Editor, Calculator, and Graph Preview all pass smoke tests.
- No open blocker exists for startup, update, installer, data loss, or feedback delivery.
- Release notes are short, user-facing, and do not mention internal build numbers.

## Helper Commands

```bat
BETA_RELEASE.bat
BETA_RELEASE.bat hash
BETA_RELEASE.bat build
DAILY_RELEASE.bat check
```

Use `BETA_RELEASE.bat` before publishing to verify the beta manifest, marker, artifact names, and hashes.
