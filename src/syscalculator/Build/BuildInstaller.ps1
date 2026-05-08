param(
    [ValidateSet('daily', 'beta', 'production')]
    [string]$Channel = 'daily'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$project = Join-Path $repoRoot 'src\syscalculator\Syscalculator.UI.WinForms.csproj'
$publishDir = Join-Path $repoRoot 'artifacts\publish\Syscalculator\win-x64'
$installerScript = Join-Path $repoRoot 'installer\Syscalculator.iss'
$generatedVersionFile = Join-Path $repoRoot 'src\syscalculator\AppVersionInfo.Generated.cs'

$isccCommand = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
$isccPath = if ($isccCommand) { $isccCommand.Source } else { $null }

if (-not $isccPath) {
    $candidatePaths = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
    )

    $isccPath = $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $isccPath) {
    throw 'Inno Setup compiler ISCC.exe was not found. Install Inno Setup 6 first.'
}

if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

dotnet publish $project -c Release -r win-x64 --self-contained false -o $publishDir

$versionText = Get-Content $generatedVersionFile -Raw
if ($versionText -notmatch 'BuildNumber\s*=\s*"(?<build>[^"]+)"') {
    throw "Could not read build number from $generatedVersionFile."
}

$env:SYSCALC_INSTALL_VERSION = "2.0.$($Matches['build'])"
$env:SYSCALC_INSTALL_CHANNEL = $Channel

& $isccPath $installerScript

Write-Host "Installer created in artifacts\installer for channel '$Channel' version '$env:SYSCALC_INSTALL_VERSION'."
