param(
    [ValidateSet('daily', 'beta', 'production')]
    [string]$Channel = 'daily'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$project = Join-Path $repoRoot 'src\Syscalculator.UI.WinForms\Syscalculator.UI.WinForms.csproj'
$publishDir = Join-Path $repoRoot 'artifacts\publish\Syscalculator\win-x64'
$installerScript = Join-Path $repoRoot 'installer\Syscalculator.iss'
$generatedVersionFile = Join-Path $repoRoot 'src\Syscalculator.UI.WinForms\AppVersionInfo.Generated.cs'

if (-not (Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue)) {
    throw 'Inno Setup compiler ISCC.exe was not found in PATH. Install Inno Setup 6 and add it to PATH, or run this from the Inno Setup command prompt.'
}

dotnet publish $project -c Release -r win-x64 --self-contained false -o $publishDir

$versionText = Get-Content $generatedVersionFile -Raw
if ($versionText -notmatch 'BuildNumber\s*=\s*"(?<build>[^"]+)"') {
    throw "Could not read build number from $generatedVersionFile."
}

$env:SYSCALC_INSTALL_VERSION = "2.0.$($Matches['build'])"
$env:SYSCALC_INSTALL_CHANNEL = $Channel

ISCC.exe $installerScript

Write-Host "Installer created in artifacts\installer for channel '$Channel' version '$env:SYSCALC_INSTALL_VERSION'."
