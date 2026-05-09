param(
    [string]$Vb6Path = "",
    [switch]$SkipCompile
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$repoRoot = Split-Path -Parent (Split-Path -Parent $projectDir)
$artifactDir = Join-Path $repoRoot "artifacts\legacy\vb6-compile"
$projectPath = Join-Path $projectDir "Project1.vbp"
$logPath = Join-Path $artifactDir "Syscalculator174.compile.log"

New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "VB6 project not found: $projectPath"
}

if ($SkipCompile) {
    Write-Host "Validated VB6 project path: $projectPath"
    Write-Host "Skipping VB6 compile because -SkipCompile was supplied."
    exit 0
}

if ([string]::IsNullOrWhiteSpace($Vb6Path)) {
    $cmd = Get-Command "VB6.EXE" -ErrorAction SilentlyContinue
    if ($cmd) {
        $Vb6Path = $cmd.Source
    }
}

if ([string]::IsNullOrWhiteSpace($Vb6Path)) {
    $knownVb6Paths = @(
        "C:\Program Files (x86)\Microsoft Visual Studio\VB98\VB6.EXE",
        "C:\Program Files\Microsoft Visual Studio\VB98\VB6.EXE",
        "C:\Program Files (x86)\Microsoft Visual Studio 6.0\VB98\VB6.EXE",
        "C:\Program Files\Microsoft Visual Studio 6.0\VB98\VB6.EXE",
        "C:\VB98\VB6.EXE"
    )

    foreach ($knownPath in $knownVb6Paths) {
        if (Test-Path -LiteralPath $knownPath) {
            $Vb6Path = $knownPath
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($Vb6Path) -or -not (Test-Path -LiteralPath $Vb6Path)) {
    throw "VB6.EXE was not found. Install Visual Basic 6 or pass -Vb6Path `"C:\Path\VB6.EXE`"."
}

Write-Host "Compiling Syscalculator 1.74 candidate..."
Write-Host "Project: $projectPath"
Write-Host "Log: $logPath"

if (Test-Path -LiteralPath $logPath) {
    Remove-Item -LiteralPath $logPath -Force
}

$arguments = "/MAKE `"$projectPath`" /OUT `"$logPath`""
$previousCompatLayer = [Environment]::GetEnvironmentVariable("__COMPAT_LAYER", "Process")
try {
    [Environment]::SetEnvironmentVariable("__COMPAT_LAYER", "RunAsInvoker", "Process")
    $process = Start-Process -FilePath $Vb6Path -ArgumentList $arguments -PassThru
}
finally {
    [Environment]::SetEnvironmentVariable("__COMPAT_LAYER", $previousCompatLayer, "Process")
}
if ($process) {
    $process.WaitForExit()
}

$compileLog = ""
for ($attempt = 0; $attempt -lt 120; $attempt++) {
    $compileLog = if (Test-Path -LiteralPath $logPath) { Get-Content -LiteralPath $logPath -Raw } else { "" }
    if ($compileLog -match "(?i)succeeded") {
        break
    }
    Start-Sleep -Milliseconds 500
}
Start-Sleep -Milliseconds 1000
$compileLog = if (Test-Path -LiteralPath $logPath) { Get-Content -LiteralPath $logPath -Raw } else { "" }
$compileSucceeded = "$compileLog" -match "(?i)succeeded"
if (-not $compileSucceeded) {
    throw "VB6 compile failed with exit code $LASTEXITCODE. See $logPath"
}

Write-Host "VB6 compile completed."
