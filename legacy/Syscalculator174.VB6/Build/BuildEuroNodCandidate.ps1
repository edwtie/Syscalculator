$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$repoRoot = Split-Path -Parent (Split-Path -Parent $projectDir)
$outDir = Join-Path $repoRoot "artifacts\legacy\Syscalculator174-EuroNodCandidate"
$zipPath = Join-Path $repoRoot "artifacts\legacy\Syscalculator174-EuroNodCandidate.zip"

if (Test-Path -LiteralPath $outDir) {
    Remove-Item -LiteralPath $outDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$items = @(
    "freesyscal.cfg",
    "freesysc.cfg",
    "eng.lng",
    "ned.lng",
    "esp.lng",
    "cat.lng",
    "euro",
    "Text"
)

foreach ($item in $items) {
    $source = Join-Path $projectDir $item
    if (-not (Test-Path -LiteralPath $source)) {
        Write-Warning "Missing optional candidate item: $source"
        continue
    }

    $destination = Join-Path $outDir $item
    if (Test-Path -LiteralPath $source -PathType Container) {
        Copy-Item -LiteralPath $source -Destination $destination -Recurse -Force
    }
    else {
        Copy-Item -LiteralPath $source -Destination $destination -Force
    }
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $outDir "*") -DestinationPath $zipPath -Force

Write-Host "Euro NOD candidate created:"
Write-Host $outDir
Write-Host $zipPath
