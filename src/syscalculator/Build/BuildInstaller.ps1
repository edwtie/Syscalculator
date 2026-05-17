param(
    [ValidateSet('daily', 'beta', 'production')]
    [string]$Channel = 'daily'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$project = Join-Path $repoRoot 'src\syscalculator\Syscalculator.UI.WinForms.csproj'
$updaterProject = Join-Path $repoRoot 'src\Syscalculator.Updater\Syscalculator.Updater.csproj'
$publishDir = Join-Path $repoRoot 'artifacts\publish\Syscalculator\win-x64'
$updaterPublishDir = Join-Path $publishDir 'Updater'
$installerScript = Join-Path $repoRoot 'installer\Syscalculator.iss'
$generatedVersionFile = Join-Path $repoRoot 'src\syscalculator\AppVersionInfo.Generated.cs'
$updatesDir = Join-Path $repoRoot 'artifacts\updates'

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
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

dotnet publish $updaterProject -c Release -r win-x64 --self-contained false -o $updaterPublishDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish updater failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path -LiteralPath (Join-Path $publishDir 'Syscalculator.exe'))) {
    throw "Publish output is missing Syscalculator.exe: $publishDir"
}

$versionText = Get-Content $generatedVersionFile -Raw
if ($versionText -notmatch 'BuildNumber\s*=\s*"(?<build>[^"]+)"') {
    throw "Could not read build number from $generatedVersionFile."
}

$buildNumber = $Matches['build']
$installVersion = if (($Channel -eq 'daily' -or $Channel -eq 'beta') -and $buildNumber -match '^(?<date>\d{4}\.\d{2}\.\d{2})\.\d{3}$') {
    "2.0.$($Matches['date'])"
}
else {
    "2.0.$buildNumber"
}

$env:SYSCALC_INSTALL_VERSION = $installVersion
$env:SYSCALC_INSTALL_CHANNEL = $Channel

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
Write-Host "Updater package created: $packagePath"
Write-Host "Updater package sha256: $packageHash"
