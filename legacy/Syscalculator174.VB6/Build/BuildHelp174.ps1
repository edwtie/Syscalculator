param(
    [string]$HhcPath = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$helpDir = Join-Path $projectDir "help"
$projectFile = Join-Path $helpDir "Syscalculator174.hhp"
$outputFile = Join-Path $helpDir "Syscalculator174.chm"

if (-not (Test-Path -LiteralPath $projectFile)) {
    throw "HTML Help project file not found: $projectFile"
}

if ([string]::IsNullOrWhiteSpace($HhcPath)) {
    $hhcCommand = Get-Command "hhc.exe" -ErrorAction SilentlyContinue
    if ($hhcCommand) {
        $HhcPath = $hhcCommand.Source
    }
}

if ([string]::IsNullOrWhiteSpace($HhcPath)) {
    $candidatePaths = @(
        (Join-Path ${env:ProgramFiles(x86)} "HTML Help Workshop\hhc.exe"),
        (Join-Path $env:ProgramFiles "HTML Help Workshop\hhc.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Microsoft Help Workshop\hhc.exe"),
        (Join-Path $env:ProgramFiles "Microsoft Help Workshop\hhc.exe")
    )

    $HhcPath = $candidatePaths | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($HhcPath) -or -not (Test-Path -LiteralPath $HhcPath)) {
    throw "HTML Help compiler hhc.exe was not found. Install Microsoft HTML Help Workshop or pass -HhcPath."
}

Push-Location $helpDir
try {
    if (Test-Path -LiteralPath $outputFile) {
        Remove-Item -LiteralPath $outputFile -Force
    }

    & $HhcPath $projectFile

    if (-not (Test-Path -LiteralPath $outputFile)) {
        throw "HTML Help compiler finished but did not create: $outputFile"
    }
}
finally {
    Pop-Location
}

Write-Host "Syscalculator 1.74 CHM help created: $outputFile"
