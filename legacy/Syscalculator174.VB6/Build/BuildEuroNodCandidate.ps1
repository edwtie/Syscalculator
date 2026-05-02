$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$legacyRoot = Join-Path $repoRoot 'legacy\Syscalculator174.VB6'
$candidateRoot = Join-Path $repoRoot 'artifacts\legacy\Syscalculator174-EuroNodCandidate'
$zipPath = Join-Path $repoRoot 'artifacts\legacy\Syscalculator174-EuroNodCandidate.zip'

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

if (Test-Path $candidateRoot) {
    Remove-Item -LiteralPath $candidateRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $candidateRoot | Out-Null

foreach ($group in $legacyGroups) {
    $sourceGroup = Join-Path $legacyRoot $group
    $targetGroup = Join-Path $candidateRoot $group

    if (-not (Test-Path $sourceGroup)) {
        throw "Legacy group '$group' was not found at $sourceGroup."
    }

    New-Item -ItemType Directory -Force -Path $targetGroup | Out-Null
    Get-ChildItem $sourceGroup -Filter '*.nod' | Copy-Item -Destination $targetGroup
}

foreach ($file in $newEuroFiles) {
    $sourceFile = Join-Path $legacyRoot "euro\$file"

    if (-not (Test-Path $sourceFile)) {
        throw "New euro converter '$file' was not fixed in the 1.74 source at $sourceFile."
    }
}

$allSourceNods = foreach ($group in $legacyGroups) {
    Get-ChildItem (Join-Path $legacyRoot $group) -Filter '*.nod'
}

$invalidFiles = foreach ($file in $allSourceNods) {
    $text = Get-Content $file.FullName -Raw
    $hasName = $text -match '(?im)^\s*Name\s+'
    $hasInput1 = $text -match '(?im)^\s*input1\s+'
    $hasInput2 = $text -match '(?im)^\s*input2\s+'
    $hasEnd = $text -match '(?im)^\s*end\s*$'

    if (-not ($hasName -and $hasInput1 -and $hasInput2 -and $hasEnd)) {
        $file.FullName
    }
}

if ($invalidFiles) {
    throw "NOD validation failed for: $($invalidFiles -join ', ')"
}

$vb6 = Get-Command 'VB6.EXE' -ErrorAction SilentlyContinue
$compileStatus = if ($vb6) {
    'VB6.EXE was found. Compile Syscalculator174.local.vbp manually before promoting this candidate to release.'
} else {
    'VB6.EXE was not found in PATH. This candidate is not compile-verified yet.'
}

$manifestPath = Join-Path $candidateRoot 'MANIFEST.txt'
$readmePath = Join-Path $candidateRoot 'README_1.74_EURO_NOD_CANDIDATE.txt'

$allNodFiles = Get-ChildItem $candidateRoot -Recurse -Filter '*.nod' |
    Sort-Object FullName |
    ForEach-Object { $_.FullName.Substring($candidateRoot.Length + 1).Replace('\', '/') }

$manifest = @(
    'Syscalculator 1.74 Euro NOD Maintenance Candidate',
    '================================================',
    '',
    'Status: candidate, not final release',
    'Package type: data-only .nod converter update',
    'Line: Syscalculator 1.74 maintenance / NOD 1.0',
    'Purpose: add newer euro country/currency converters while keeping the original grouped directory layout.',
    "Compile status: $compileStatus",
    '',
    'New euro files added:',
    ($newEuroFiles | ForEach-Object { "- euro/$_" }),
    '',
    'All packaged .nod files:',
    ($allNodFiles | ForEach-Object { "- $_" })
)

Set-Content -Path $manifestPath -Value $manifest -Encoding UTF8

$readme = @(
    'Syscalculator 1.74 Euro NOD Maintenance Candidate',
    '',
    'This is a candidate package for the old Syscalculator 1.74 line.',
    'Do not describe it as a release until the 1.74 source has been tested and compiled.',
    'It does not rebuild the VB6 application and does not add NOD 2.0 syntax.',
    '',
    "Compile status: $compileStatus",
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

Compress-Archive -Path (Join-Path $candidateRoot '*') -DestinationPath $zipPath

Write-Host "Created Syscalculator 1.74 Euro NOD candidate:"
Write-Host "  $candidateRoot"
Write-Host "  $zipPath"
Write-Host $compileStatus
