param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$InputFolder,

    [Parameter(Mandatory = $true, Position = 1)]
    [string]$OutputPackage,

    [Parameter(Mandatory = $false)]
    [string]$BasePackage
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptRoot "Assert-PowerShell7.ps1") -Purpose "language package compile helpers" -ScriptPath $PSCommandPath
$repoRoot = Split-Path -Parent $scriptRoot
$project = Join-Path $repoRoot "src/Tiedragon.LanguagePackage/Tiedragon.LanguagePackage.csproj"

if ([string]::IsNullOrWhiteSpace($BasePackage)) {
    dotnet run --project $project --no-restore -- agent-compile $InputFolder $OutputPackage
} else {
    dotnet run --project $project --no-restore -- agent-compile-with-base $BasePackage $InputFolder $OutputPackage
}
