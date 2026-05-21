param(
    [ValidateSet('daily', 'beta', 'production')]
    [string]$Channel = 'daily',

    [string]$InstallVersion = '',

    [string]$PackageId = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$project = Join-Path $repoRoot 'src\syscalculator\Syscalculator.UI.WinForms.csproj'
$updaterProject = Join-Path $repoRoot 'src\Syscalculator.Updater\Syscalculator.Updater.csproj'
$languagePackageScript = Join-Path $repoRoot 'tools\Generate-LanguagePackages.ps1'
$publishDir = Join-Path $repoRoot 'artifacts\publish\Syscalculator\win-x64'
$updaterPublishDir = Join-Path $publishDir 'Updater'
$installerScript = Join-Path $repoRoot 'installer\Syscalculator.iss'
$generatedVersionFile = Join-Path $repoRoot 'src\syscalculator\AppVersionInfo.Generated.cs'
$updatesDir = Join-Path $repoRoot 'artifacts\updates'
$isSelfContained = $Channel -in @('daily', 'beta', 'production')
$selfContainedArg = if ($isSelfContained) { 'true' } else { 'false' }

function Find-PowerShell7 {
    $command = Get-Command 'pwsh' -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidatePaths = @(
        (Join-Path $env:ProgramFiles 'PowerShell\7\pwsh.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'PowerShell\7\pwsh.exe')
    )

    return $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

$pwshPath = Find-PowerShell7
if (-not $pwshPath) {
    throw 'PowerShell 7 was not found. Install it first, for example with: winget install --id Microsoft.PowerShell --source winget'
}

$isccCommand = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
$isccPath = if ($isccCommand) { $isccCommand.Source } else { $null }

if (-not $isccPath) {
    $candidatePaths = @(
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
    )

    $isccPath = $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $isccPath) {
    throw 'Inno Setup compiler ISCC.exe was not found. Install Inno Setup 6 first.'
}

& $pwshPath -NoProfile -ExecutionPolicy Bypass -File $languagePackageScript
if ($LASTEXITCODE -ne 0) {
    throw "Language package generation failed with exit code $LASTEXITCODE."
}

if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

dotnet publish $project -c Release -r win-x64 --self-contained $selfContainedArg -o $publishDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

dotnet publish $updaterProject -c Release -r win-x64 --self-contained $selfContainedArg -o $updaterPublishDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish updater failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path -LiteralPath (Join-Path $publishDir 'Syscalculator.exe'))) {
    throw "Publish output is missing Syscalculator.exe: $publishDir"
}

$publishedLanguagePackages = Get-ChildItem -LiteralPath (Join-Path $publishDir 'LanguagePackages') -Filter '*.lngpdk' -File -ErrorAction SilentlyContinue
if ($publishedLanguagePackages.Count -lt 9) {
    throw "Publish output is missing bundled language packages in $publishDir\LanguagePackages."
}

$versionText = Get-Content $generatedVersionFile -Raw
if ($versionText -notmatch 'BuildNumber\s*=\s*"(?<build>[^"]+)"') {
    throw "Could not read build number from $generatedVersionFile."
}

$buildNumber = $Matches['build']
$installVersion = if (-not [string]::IsNullOrWhiteSpace($InstallVersion)) {
    $InstallVersion
}
elseif (($Channel -eq 'daily' -or $Channel -eq 'beta' -or $Channel -eq 'production') -and $buildNumber -match '^(?<date>\d{4}\.\d{2}\.\d{2})\.\d{3}$') {
    "2.0.$($Matches['date'])"
}
else {
    "2.0.$buildNumber"
}

$env:SYSCALC_INSTALL_VERSION = $installVersion
$env:SYSCALC_INSTALL_CHANNEL = $Channel
$env:SYSCALC_SELF_CONTAINED = $selfContainedArg
$env:SYSCALC_PACKAGE_ID = if (-not [string]::IsNullOrWhiteSpace($PackageId)) {
    $PackageId
}
elseif (-not [string]::IsNullOrWhiteSpace($env:SYSCALC_PACKAGE_ID)) {
    $env:SYSCALC_PACKAGE_ID
}
elseif ($Channel -eq 'production') {
    "production-$($installVersion -replace '^2\.0\.', '')-installer-001"
}
elseif ($Channel -eq 'beta') {
    "beta-$($installVersion -replace '^2\.0\.', '')-installer-001"
}
else {
    "daily-$($installVersion -replace '^2\.0\.', '')-installer-001"
}

$publishedUpdateStatePath = Join-Path $publishDir 'update-state.cfg'
[System.IO.File]::WriteAllText(
    $publishedUpdateStatePath,
    "# Installed update package marker.`r`n" +
    "# Used by the updater and About dialog to detect the installed release channel.`r`n" +
    "packageId=$env:SYSCALC_PACKAGE_ID`r`n",
    [System.Text.UTF8Encoding]::new($false))

if (-not (Test-Path $updatesDir)) {
    New-Item -ItemType Directory -Path $updatesDir | Out-Null
}

$packagePath = Join-Path $updatesDir "Syscalculator-2.0-$Channel-$installVersion.zip"
if (Test-Path $packagePath) {
    Remove-Item -LiteralPath $packagePath -Force
}

Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $packagePath -CompressionLevel Optimal
$packageHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $packagePath).Hash.ToLowerInvariant()

& $isccPath $installerScript

Write-Host "Installer created in artifacts\installer for channel '$Channel' version '$env:SYSCALC_INSTALL_VERSION'."
Write-Host "Self-contained publish: $selfContainedArg"
Write-Host "Package marker: $env:SYSCALC_PACKAGE_ID"
Write-Host "Updater package created: $packagePath"
Write-Host "Updater package sha256: $packageHash"
