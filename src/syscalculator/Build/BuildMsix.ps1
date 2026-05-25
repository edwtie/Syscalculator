param(
    [ValidateSet('daily', 'beta', 'production')]
    [string]$Channel = 'production',

    [string]$PackageVersion = '',

    [string]$IdentityName = 'Tiedragon.Syscalculator',

    [string]$Publisher = 'CN=6A1868B5-5224-4E03-A092-F0E73E8310E6',

    [string]$PublisherDisplayName = 'Tiedragon',

    [string]$DisplayName = 'Syscalculator',

    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$project = Join-Path $repoRoot 'src\syscalculator\Syscalculator.UI.WinForms.csproj'
$updaterProject = Join-Path $repoRoot 'src\Syscalculator.Updater\Syscalculator.Updater.csproj'
$publishDir = Join-Path $repoRoot 'artifacts\publish\Syscalculator\win-x64'
$updaterPublishDir = Join-Path $publishDir 'Updater'
$layoutDir = Join-Path $repoRoot 'artifacts\msix\layout'
$installerDir = Join-Path $repoRoot 'artifacts\installer'
$isSelfContained = $true

if ([string]::IsNullOrWhiteSpace($PackageVersion)) {
    $today = Get-Date
    $PackageVersion = '2.0.{0}.0' -f ($today.Year + 1)
}

if ($PackageVersion -notmatch '^\d+\.\d+\.\d+\.\d+$') {
    throw "MSIX package version must have four numeric parts, for example 2.0.2027.0. Current value: $PackageVersion"
}

$versionParts = $PackageVersion.Split('.') | ForEach-Object { [int]$_ }
if ($versionParts[3] -ne 0) {
    throw "Microsoft Store MSIX packages must use revision 0. Increment the third version part instead, for example 2.0.2027.0. Current value: $PackageVersion"
}

function Find-MakeAppx {
    $command = Get-Command 'makeappx.exe' -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $kitRoot = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
    if (Test-Path -LiteralPath $kitRoot) {
        $candidate = Get-ChildItem -LiteralPath $kitRoot -Recurse -Filter 'makeappx.exe' -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -like '*\x64\makeappx.exe' } |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($candidate) {
            return $candidate.FullName
        }
    }

    throw 'makeappx.exe was not found. Install the Windows SDK first.'
}

function Get-Sha256Hash {
    param(
        [Parameter(Mandatory = $true)][string]$Path
    )

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $stream = [System.IO.File]::OpenRead($Path)
    try {
        $hashBytes = $sha256.ComputeHash($stream)
        return -join ($hashBytes | ForEach-Object { $_.ToString('x2') })
    }
    finally {
        $stream.Dispose()
        $sha256.Dispose()
    }
}

function New-MsixLogo {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][int]$Width,
        [Parameter(Mandatory = $true)][int]$Height
    )

    Add-Type -AssemblyName System.Drawing

    $bitmap = New-Object System.Drawing.Bitmap $Width, $Height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::FromArgb(0, 84, 180))

        $shortSide = [Math]::Min($Width, $Height)
        $fontSize = [Math]::Max(12, [int]($shortSide * 0.55))
        $font = New-Object System.Drawing.Font 'Segoe UI Semibold', $fontSize, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
        $brush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
        $format = New-Object System.Drawing.StringFormat
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        $rect = New-Object System.Drawing.RectangleF 0, 0, $Width, $Height

        $graphics.DrawString('S', $font, $brush, $rect, $format)
    }
    finally {
        if ($brush) { $brush.Dispose() }
        if ($font) { $font.Dispose() }
        if ($format) { $format.Dispose() }
        $graphics.Dispose()
    }

    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

$makeAppx = Find-MakeAppx
$identityNameXml = [System.Security.SecurityElement]::Escape($IdentityName)
$publisherXml = [System.Security.SecurityElement]::Escape($Publisher)
$publisherDisplayNameXml = [System.Security.SecurityElement]::Escape($PublisherDisplayName)
$displayNameXml = [System.Security.SecurityElement]::Escape($DisplayName)

if (-not $SkipPublish) {
    if (Test-Path -LiteralPath $publishDir) {
        Remove-Item -LiteralPath $publishDir -Recurse -Force
    }

    dotnet publish $project -c Release -r win-x64 --self-contained $isSelfContained -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    dotnet publish $updaterProject -c Release -r win-x64 --self-contained $isSelfContained -o $updaterPublishDir
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish updater failed with exit code $LASTEXITCODE."
    }
}

$appExe = Join-Path $publishDir 'Syscalculator.exe'
if (-not (Test-Path -LiteralPath $appExe)) {
    throw "Publish output is missing Syscalculator.exe: $appExe"
}

if (Test-Path -LiteralPath $layoutDir) {
    Remove-Item -LiteralPath $layoutDir -Recurse -Force
}

New-Item -ItemType Directory -Path $layoutDir | Out-Null
New-Item -ItemType Directory -Path $installerDir -Force | Out-Null
Get-ChildItem -LiteralPath $publishDir -Force |
    Copy-Item -Destination $layoutDir -Recurse -Force

Get-ChildItem -LiteralPath $layoutDir -Filter 'unins000.*' -Recurse -ErrorAction SilentlyContinue |
    Remove-Item -Force

$assetsDir = Join-Path $layoutDir 'Assets'
New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null
New-MsixLogo -Path (Join-Path $assetsDir 'StoreLogo.png') -Width 50 -Height 50
New-MsixLogo -Path (Join-Path $assetsDir 'Square44x44Logo.png') -Width 44 -Height 44
New-MsixLogo -Path (Join-Path $assetsDir 'Square150x150Logo.png') -Width 150 -Height 150
New-MsixLogo -Path (Join-Path $assetsDir 'Wide310x150Logo.png') -Width 310 -Height 150
New-MsixLogo -Path (Join-Path $assetsDir 'SplashScreen.png') -Width 620 -Height 300

$manifestPath = Join-Path $layoutDir 'AppxManifest.xml'
$manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap rescap">
  <Identity Name="$identityNameXml" Publisher="$publisherXml" Version="$PackageVersion" ProcessorArchitecture="x64" />
  <Properties>
    <DisplayName>$displayNameXml</DisplayName>
    <PublisherDisplayName>$publisherDisplayNameXml</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>
  <Resources>
    <Resource Language="en-us" />
    <Resource Language="nl-nl" />
  </Resources>
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.26100.0" />
  </Dependencies>
  <Applications>
    <Application Id="App" Executable="Syscalculator.exe" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements
        DisplayName="$displayNameXml"
        Description="Practical conversion and calculation tool."
        BackgroundColor="transparent"
        Square150x150Logo="Assets\Square150x150Logo.png"
        Square44x44Logo="Assets\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png" />
        <uap:SplashScreen Image="Assets\SplashScreen.png" />
      </uap:VisualElements>
    </Application>
  </Applications>
  <Capabilities>
    <Capability Name="internetClient" />
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
"@

Set-Content -LiteralPath $manifestPath -Value $manifest -Encoding UTF8

$packageName = 'Syscalculator-2.0-{0}-{1}.msix' -f $Channel, $PackageVersion
$packagePath = Join-Path $installerDir $packageName
if (Test-Path -LiteralPath $packagePath) {
    Remove-Item -LiteralPath $packagePath -Force
}

& $makeAppx pack /d $layoutDir /p $packagePath /o
if ($LASTEXITCODE -ne 0) {
    throw "makeappx pack failed with exit code $LASTEXITCODE."
}

$hash = Get-Sha256Hash -Path $packagePath
Write-Host "MSIX created: $packagePath"
Write-Host "MSIX version: $PackageVersion"
Write-Host "MSIX sha256: $hash"
Write-Host "Store note: this MSIX is unsigned locally; Microsoft Store signs it after package submission."
