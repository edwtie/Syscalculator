param(
    [ValidateSet("rc1", "candidate", "production")]
    [string]$Channel = "rc1",
    [string]$Vb6Path = "",
    [switch]$Compile
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$repoRoot = Split-Path -Parent (Split-Path -Parent $projectDir)
$compileScript = Join-Path $scriptDir "CompileSyscalculator174Candidate.ps1"
$installerScript = Join-Path $repoRoot "installer\Syscalculator174.iss"
$projectPath = Join-Path $projectDir "Project1.vbp"
$appExe = Join-Path $projectDir "freesyscal.exe"

if ($Compile) {
    if ([string]::IsNullOrWhiteSpace($Vb6Path)) {
        & $compileScript
    }
    else {
        & $compileScript -Vb6Path $Vb6Path
    }
}

if (-not (Test-Path -LiteralPath $appExe)) {
    throw "Legacy executable not found: $appExe. Compile Syscalculator 1.74 first or provide an existing freesyscal.exe."
}

$isccCommand = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
$isccPath = if ($isccCommand) { $isccCommand.Source } else { $null }

if (-not $isccPath) {
    $candidatePaths = @(
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe")
    )

    $isccPath = $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $isccPath) {
    throw "Inno Setup compiler ISCC.exe was not found. Install Inno Setup 6 first."
}

$projectText = Get-Content -LiteralPath $projectPath -Raw
$majorMatch = [regex]::Match($projectText, "(?m)^MajorVer=(?<value>\d+)")
$minorMatch = [regex]::Match($projectText, "(?m)^MinorVer=(?<value>\d+)")
$revisionMatch = [regex]::Match($projectText, "(?m)^RevisionVer=(?<value>\d+)")

if (-not $majorMatch.Success -or -not $minorMatch.Success -or -not $revisionMatch.Success) {
    throw "Could not read Syscalculator 1.74 version from $projectPath."
}

$env:SYSCALC174_INSTALL_VERSION = "$($majorMatch.Groups['value'].Value).$($minorMatch.Groups['value'].Value).$($revisionMatch.Groups['value'].Value)"
$env:SYSCALC174_INSTALL_CHANNEL = $Channel

& $isccPath $installerScript

Write-Host "Syscalculator 1.74 installer created in artifacts\legacy\installer for channel '$Channel' version '$env:SYSCALC174_INSTALL_VERSION'."
