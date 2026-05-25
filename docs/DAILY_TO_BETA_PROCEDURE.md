# Daily To Beta Procedure

Use this procedure when a tested Daily build becomes a Beta build.

## Goal

Promote a known Daily state into the Beta release line with three Beta artifacts:

- Inno Setup installer
- standalone updater zip
- MSIX package for Microsoft Store package flight testing

Daily artifacts must not be reused directly as Beta artifacts. Build the Beta packages from the selected source state so About, update markers, manifest metadata and package names all say Beta.

## 1. Choose The Daily Candidate

- Pick the Daily date/version that has passed local testing.
- Confirm the intended fixes are in `CHANGELOG.md`.
- Confirm no blocker remains for startup, install, updater, feedback, About, Help or core NOD workflows.
- Confirm the working tree only contains changes that belong in the Beta candidate.

Useful checks:

```bat
DAILY_RELEASE.bat check
DAILY_RELEASE.bat hash
git status --short
```

## 2. Freeze Beta Notes

- Add or update the Beta entry in `CHANGELOG.md`.
- Update `docs/BETA_ROADMAP.md` with what changed from the previous Beta.
- State clearly whether this Beta has the updater package enabled.
- State known limitations and what testers should focus on.
- Keep `README.md` channel-specific. It is marked `merge=ours` in `.gitattributes`;
  when merging Daily into Beta, the Beta README must stay the Beta front page.

## 3. Update Beta Manifest

Update `web/updates/syscalculator.json` for the `beta` channel:

- `version`
- `displayVersion`
- `date`
- `title`
- `summary`
- `downloadUrl`
- `releaseNotesUrl`
- `packageUrl`
- `packageId`
- `sha256`

The `packageId` must change when the Beta package changes, even when the visible date/version stays the same.

## 4. Build Beta Inno Setup And Updater

```bat
BETA_RELEASE.bat build
BETA_RELEASE.bat hash
```

Expected artifacts:

- `artifacts\installer\Syscalculator-2.0-beta-2.0.<date>.exe`
- `artifacts\updates\Syscalculator-2.0-beta-2.0.<date>.zip`

Open the updater zip and verify `update-state.cfg` contains a Beta marker:

```text
packageId=beta-...
```

## 5. Build Beta MSIX

```bat
BETA_MSIX.bat
```

Expected artifact:

- `artifacts\installer\Syscalculator-2.0-beta-<version>.msix`

Use the Beta MSIX only for Store package flight testing. Daily must not be submitted as a package flight.

## 6. Smoke Test Beta

- Install the Beta Inno Setup package.
- Start Syscalculator.
- Confirm About shows Beta, not Daily.
- Confirm License/About text uses Beta wording.
- Confirm update channel is Beta.
- Confirm update check does not loop on the same package.
- Confirm the updater progress reaches 100% visually.
- Confirm Feedback validates e-mail and can send.
- Open Help and check there is no Daily-only wording in normal Beta screens.
- Run at least one NOD converter and one math/equation example.

## 7. Publish Beta Artifacts

Only after smoke testing:

- Upload the Beta Inno Setup installer.
- Upload the Beta updater zip.
- Upload or attach Beta release assets on GitHub.
- Update the hosted update manifest on Tiedragon.
- Verify the hosted manifest with HTTPS after upload.

Do not publish a Beta MSIX publicly. Use it for Microsoft Store package flights.

## 8. Microsoft Store Package Flight

For Store-based Beta testing:

- Create or update a Beta package flight in Partner Center.
- Add the tester group.
- Upload the Beta MSIX package.
- Submit the flight for the Microsoft certification approving procedure.
- Wait for approval before telling testers to install through the Store.

Package flights are only for Beta builds. Production releases use the normal Store release path.

## 9. GitHub Release

Create or update the Beta GitHub release:

- Mark as pre-release.
- Mention that it is for testers.
- Mention whether the updater is included.
- Link known limitations.
- Attach the Beta Inno Setup installer and updater zip when ready.

## 10. Final Checks

- Hosted manifest points to the intended Beta package.
- SHA256 in manifest matches the hosted updater zip.
- About shows Beta after install/update.
- GitHub release tag matches the Beta date/version.
- No Daily package URL is used in the Beta manifest.
- No Production package URL is used in the Beta manifest.
