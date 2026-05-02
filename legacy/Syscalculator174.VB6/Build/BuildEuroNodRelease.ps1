$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$legacyRoot = Join-Path $repoRoot 'legacy\Syscalculator174.VB6'
$modernEuroRoot = Join-Path $repoRoot 'src\Syscalculator.UI.WinForms\Converters\euro'
$releaseRoot = Join-Path $repoRoot 'artifacts\legacy\Syscalculator174-EuroNodRelease'
$zipPath = Join-Path $repoRoot 'artifacts\legacy\Syscalculator174-EuroNodRelease.zip'

$legacyGroups = @(
    'Distance',
    'euro',
    'Mass',
    'Pressure',
    'Temperature',
    'Volume'
)

$newEuroFiles = @(
    'BGN.nod',
    'CYP.nod',
    'EEK.nod',
    'HRK.nod',
    'LTL.nod',
    'LVL.nod',
    'MTL.nod',
    'SIT.nod',
    'SKK.nod'
)

if (Test-Path $releaseRoot) {
    Remove-Item -LiteralPath $releaseRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $releaseRoot | Out-Null

foreach ($group in $legacyGroups) {
    $sourceGroup = Join-Path $legacyRoot $group
    $targetGroup = Join-Path $releaseRoot $group

    if (-not (Test-Path $sourceGroup)) {
        throw "Legacy group '$group' was not found at $sourceGroup."
    }

    New-Item -ItemType Directory -Force -Path $targetGroup | Out-Null
    Get-ChildItem $sourceGroup -Filter '*.nod' | Copy-Item -Destination $targetGroup
}

foreach ($file in $newEuroFiles) {
    $sourceFile = Join-Path $modernEuroRoot $file
    $targetFile = Join-Path $releaseRoot "euro\$file"

    if (-not (Test-Path $sourceFile)) {
        throw "New euro converter '$file' was not found at $sourceFile."
    }

    Copy-Item -LiteralPath $sourceFile -Destination $targetFile
}

$manifestPath = Join-Path $releaseRoot 'MANIFEST.txt'
$readmePath = Join-Path $releaseRoot 'README_1.74_EURO_NOD_RELEASE.txt'

$allNodFiles = Get-ChildItem $releaseRoot -Recurse -Filter '*.nod' |
    Sort-Object FullName |
    ForEach-Object { $_.FullName.Substring($releaseRoot.Length + 1).Replace('\', '/') }

$manifest = @(
    'Syscalculator 1.74 Euro NOD Maintenance Release',
    '===============================================',
    '',
    'Release type: data-only .nod converter update',
    'Line: Syscalculator 1.74 maintenance / NOD 1.0',
    'Purpose: add newer euro country/currency converters while keeping the original grouped directory layout.',
    '',
    'New euro files added:',
    ($newEuroFiles | ForEach-Object { "- euro/$_" }),
    '',
    'All packaged .nod files:',
    ($allNodFiles | ForEach-Object { "- $_" })
)

Set-Content -Path $manifestPath -Value $manifest -Encoding UTF8

$readme = @(
    'Syscalculator 1.74 Euro NOD Maintenance Release',
    '',
    'This package is for the old Syscalculator 1.74 line.',
    'It does not rebuild the VB6 application and does not add NOD 2.0 syntax.',
    '',
    'Install/update idea:',
    '1. Keep the existing Syscalculator 1.74 program files.',
    '2. Copy these grouped .nod directories next to the 1.74 application.',
    '3. Replace existing .nod files only in the listed converter groups.',
    '',
    'Groups included:',
    '- Distance',
    '- euro',
    '- Mass',
    '- Pressure',
    '- Temperature',
    '- Volume',
    '',
    'New euro converters:',
    '- BGN Bulgarian lev',
    '- CYP Cyprus pound',
    '- EEK Estonian kroon',
    '- HRK Croatian kuna',
    '- LTL Lithuanian litas',
    '- LVL Latvian lats',
    '- MTL Maltese lira',
    '- SIT Slovenian tolar',
    '- SKK Slovak koruna'
)

Set-Content -Path $readmePath -Value $readme -Encoding UTF8

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $releaseRoot '*') -DestinationPath $zipPath

Write-Host "Created Syscalculator 1.74 Euro NOD release:"
Write-Host "  $releaseRoot"
Write-Host "  $zipPath"
