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
$editorExe = Join-Path $projectDir "SyscalEditor.exe"

function Get-VersionFromExecutable {
    param([string]$Path)

    $versionInfo = (Get-Item -LiteralPath $Path).VersionInfo
    $versionText = $versionInfo.ProductVersion
    if ([string]::IsNullOrWhiteSpace($versionText)) {
        $versionText = $versionInfo.FileVersion
    }

    $versionMatch = [regex]::Match($versionText, "^(?<major>\d+)\.(?<minor>\d+)\.(?<revision>\d+)")
    if (-not $versionMatch.Success) {
        return $null
    }

    $major = [int]$versionMatch.Groups["major"].Value
    $minor = [int]$versionMatch.Groups["minor"].Value
    $revision = [int]$versionMatch.Groups["revision"].Value
    return "$major.$minor.$revision"
}

function Get-VersionFromProject {
    param([string]$Path)

    $projectText = Get-Content -LiteralPath $Path -Raw
    $majorMatch = [regex]::Match($projectText, "(?m)^MajorVer=(?<value>\d+)")
    $minorMatch = [regex]::Match($projectText, "(?m)^MinorVer=(?<value>\d+)")
    $revisionMatch = [regex]::Match($projectText, "(?m)^RevisionVer=(?<value>\d+)")

    if (-not $majorMatch.Success -or -not $minorMatch.Success -or -not $revisionMatch.Success) {
        return $null
    }

    return "$($majorMatch.Groups['value'].Value).$($minorMatch.Groups['value'].Value).$([int]$revisionMatch.Groups['value'].Value)"
}

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

if (-not (Test-Path -LiteralPath $editorExe)) {
    throw "Legacy editor executable not found: $editorExe. Compile SyscalEditor first or provide an existing SyscalEditor.exe."
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

$installerVersion = Get-VersionFromExecutable $appExe
if ([string]::IsNullOrWhiteSpace($installerVersion)) {
    $installerVersion = Get-VersionFromProject $projectPath
}
if ([string]::IsNullOrWhiteSpace($installerVersion)) {
    throw "Could not read Syscalculator 1.74 version from $projectPath."
}

$env:SYSCALC174_INSTALL_VERSION = $installerVersion
$env:SYSCALC174_INSTALL_CHANNEL = $Channel

& $isccPath $installerScript

Write-Host "Syscalculator 1.74 installer created in artifacts\legacy\installer for channel '$Channel' version '$env:SYSCALC174_INSTALL_VERSION'."
