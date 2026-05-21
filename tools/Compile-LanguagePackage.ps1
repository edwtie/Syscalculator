param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$InputFolder,

    [Parameter(Mandatory = $true, Position = 1)]
    [string]$OutputPackage
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$project = Join-Path $repoRoot "src/Tiedragon.LanguagePackage/Tiedragon.LanguagePackage.csproj"

dotnet run --project $project --no-restore -- agent-compile $InputFolder $OutputPackage
