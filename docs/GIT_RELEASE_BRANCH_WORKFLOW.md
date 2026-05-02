# Git Release Branch Workflow

This project uses three logical release lines. They match the changelog channels.

## Branches

```text
daily
= active development
= every working build can land here
= changelog channel: daily

beta
= tester-ready builds
= promoted from daily
= changelog channel: beta

production
= stable public release
= promoted from beta
= changelog channel: production
```

## First Git Setup

Only run this once, from the repository root:

```bat
git init
git add .
git commit -m "Initial Syscalculator 2.0 source import"
git branch -M daily
git branch beta
git branch production
```

## Daily Development

Work on `daily`:

```bat
git switch daily
BUILD_FIX_CHANGELOG.bat -Channel daily -Fix "Fixed NOD Editor dirty tab state"
git add .
git commit -m "Daily: fix NOD Editor dirty tab state"
```

## Promote Daily To Beta

When a daily build is ready for testers:

```bat
git switch beta
git merge daily
BUILD_FIX_CHANGELOG.bat -Channel beta -Fix "Promoted tested daily fixes" -Addition "Beta build for NOD Editor tooling"
git add .
git commit -m "Beta release: NOD Editor tooling"
```

## Promote Beta To Production

When beta is stable:

```bat
git switch production
git merge beta
BUILD_FIX_CHANGELOG.bat -Channel production -Fix "Production stabilization" -Addition "Production release from beta"
git add .
git commit -m "Production release: Syscalculator 2.0"
```

## Rules

- New work starts in `daily`.
- `beta` only receives merges from `daily`.
- `production` only receives merges from `beta`.
- Do not commit `bin/`, `obj/`, `.vs/`, or generated build artifacts.
- Internal compile build numbers may increase often; changelog entries stay grouped by channel and date.
