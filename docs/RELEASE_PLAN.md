# Syscalculator Release Plan

## Release Lines

```text
Syscalculator 1.74
= VB6 legacy maintenance line
= NOD 1.0 compatibility and small Windows fixes
= release candidates first, then final legacy release

Syscalculator 2.0 Daily
= active C#/.NET 10 development line
= frequent builds for internal testing
= may contain experimental features

Syscalculator 2.0 Beta
= tester-ready builds promoted from daily
= feature set should be stable enough for wider feedback

Syscalculator 2.0 Production
= stable public release promoted from beta
= installer and release notes must be complete
```

## Current Priorities

### 1.74 Legacy

- Keep the 1.74 line limited to legacy maintenance.
- Test the 1.74 RC on Windows 11 first.
- Windows 7 and Windows 10 are older target systems and still need separate testing.
- Release notes must say clearly that the VB6 runtime is old and no longer actively maintained by Microsoft.
- Do not mix 2.0 features into 1.74.

### 2.0 Daily

- Use `daily` for active work.
- Daily builds may include experimental or hidden work, such as the solver/formula animation path.
- Keep release notes honest: daily builds are for testing, not final production use.
- Keep issue links updated for visible work:
  - runtime detection
  - NuGet/dependency management
  - SQL connector design
  - 3D graph roadmap
  - solve/diff/integral animation

### 2.0 Beta

- Promote from `daily` after a daily build has been tested.
- Beta should include only features with a clear user path and documented limitations.
- Solver animation may stay experimental unless the UI and tests are stable.
- SQL connector should stay design/prototype unless connection safety and error handling are ready.

### 2.0 Production

- Promote from `beta` only after install, startup, About, help and core NOD workflows are checked.
- Production notes must mention runtime requirements:
  - .NET 10 Desktop Runtime
  - Microsoft Edge WebView2 Runtime when needed
- Production should not depend on hidden local build state.

## Branch Flow

```text
daily -> beta -> production
```

Rules:

- New work starts in `daily`.
- `beta` receives merges from `daily`.
- `production` receives merges from `beta`.
- Build artifacts stay out of git.
- Version and release notes must match the channel.

See also: `docs/GIT_RELEASE_BRANCH_WORKFLOW.md`.

## Build And Release Checklist

### Before Build

- Check git status.
- Confirm current branch and release channel.
- Review open issues that affect the release.
- Update `CHANGELOG.md` with user-facing fixes and additions.
- Confirm NuGet restore works from a clean checkout.
- Avoid unnecessary builds when only inspecting, because the WinForms project increments the build number before compile.

### Build

For Syscalculator 2.0 installer:

```powershell
.\src\syscalculator\Build\BuildInstaller.ps1 -Channel daily
```

Use `-Channel beta` or `-Channel production` only when promoting that release line.

### Smoke Test

- Start Syscalculator.
- Check About Syscalculator:
  - product name
  - release channel
  - build number
  - release date
- Open NOD Editor.
- Run one legacy converter.
- Run one math converter.
- Run one equation/solve example.
- Open Formula Card.
- Open Help.
- Check WebView2-based content.
- Run WizardExpress clipboard flow.
- Confirm installer starts on a clean machine or VM.

### Release Notes

Each GitHub release should include:

- What this release is.
- Who should install it.
- Main fixes.
- Main additions.
- Known limitations.
- Runtime requirements.
- Testing status.

For 1.74, mention legacy VB6/runtime status.

For 2.0 Daily, mention that it is a test build.

For 2.0 Beta, mention remaining known risks.

For 2.0 Production, keep the notes short, clear and user-facing.

## Promotion Criteria

### Daily To Beta

- Daily installer builds successfully.
- About screen shows correct channel/build information.
- Changelog has a clear entry.
- Core NOD 1.0 compatibility still works.
- NOD 2.0 math/equation basics still work.
- No known blocker issue is open for startup, install or data loss.

### Beta To Production

- Beta has been tested on the intended Windows versions.
- Installer prerequisites are clear.
- Release notes are final.
- No experimental feature is presented as finished.
- Known limitations are documented.

## Dependency Policy

- NuGet versions are centralized in `Directory.Packages.props`.
- Current external package: `Microsoft.Web.WebView2`.
- Future SQL connector dependencies should be chosen deliberately and documented before implementation.
- Prefer direct packages such as `Microsoft.Data.SqlClient` over broad frameworks unless there is a clear need.
- Keep test projects free of external packages unless required.

## Known Release Risks

- WebView2 runtime availability on target machines still needs a final installer/runtime policy.
- WebView2 package dependency is not the same as WebView2 Runtime availability on user machines.
- Older Windows 7/10 testing is not complete.
- Syscalculator 1.74 depends on old VB6-era runtime technology.
- Solver/formula animation is still experimental.
- SQL connector and 3D graph work belong to enterprise/future phases, not a rushed production promise.

## Suggested Next Releases

1. Syscalculator 2.0 Daily
   - Include NuGet central package management.
   - Mention dependency cleanup and release planning.
   - Keep solver animation as experimental.

2. Syscalculator 1.74 Final
   - Promote only after Windows 11 smoke test and any available Windows 7/10 checks.
   - Keep notes focused on legacy maintenance.

3. Syscalculator 2.0 Beta
   - Promote after daily testing and installer prerequisite checks.
   - Include clean changelog and known limitations.

4. Syscalculator 2.0 Production
   - Promote only after beta feedback and runtime/installer validation.
