# Build Fix Changelog Procedure

Use this procedure when you compile a new Syscalculator 2.0 beta build and want the changelog to show today's fixes and additions.

The internal compile build number still increments on every build, but `CHANGELOG.md` is grouped by release channel.

There are three release lines:

- `daily`: daily development builds
- `beta`: beta releases for testers
- `production`: production releases

See also: `docs/GIT_RELEASE_BRANCH_WORKFLOW.md`.

Daily builds are grouped per day. A day gets one section, for example:

```text
#### 2.0.2026.05.02 Daily Build
```

## Normal Use

From the repository root:

```bat
BUILD_FIX_CHANGELOG.bat "Fixed NOD Editor close handling"
```

That is the same as:

```bat
BUILD_FIX_CHANGELOG.bat -Channel daily -Fix "Fixed NOD Editor close handling"
```

For multiple fix lines, separate them with `;`:

```bat
BUILD_FIX_CHANGELOG.bat "Fixed NOD Editor close handling; Added -wizardtool startup mode; Improved dirty tab state"
```

To write additions separately:

```bat
BUILD_FIX_CHANGELOG.bat -Fix "Fixed close dialog" -Addition "Added -wizardtool startup mode; Added -help output"
```

For a beta release entry:

```bat
BUILD_FIX_CHANGELOG.bat -Channel beta -Fix "Fixed editor close bugs" -Addition "Added NOD template wizard"
```

For a production release entry:

```bat
BUILD_FIX_CHANGELOG.bat -Channel production -Fix "Stability fixes" -Addition "Release-ready NOD Editor tooling"
```

## What The Batch Does

1. Builds `src\Syscalculator.UI.WinForms\Syscalculator.UI.WinForms.csproj`.
2. The project build automatically increments `AppVersionInfo.Generated.cs`.
3. The script reads the build date.
4. The script creates or updates the matching channel section in `CHANGELOG.md`.
   Daily entries are `2.0.yyyy.MM.dd Daily Build`.
   Beta entries are `2.0.yyyy.MM.dd Beta Release`.
   Production entries are `2.0.yyyy.MM.dd Production Release`.
5. If the build fails, `CHANGELOG.md` is not changed.

## Interactive Use

If you run the batch without text:

```bat
BUILD_FIX_CHANGELOG.bat
```

PowerShell asks:

```text
Describe the fix for the changelog:
```

## Version Display

The build number is visible in:

- `Syscalculator.exe -help`
- About Syscalculator

Example:

```text
Syscalculator 2.0 2.0.0.0 build 2026.05.02.005
```
