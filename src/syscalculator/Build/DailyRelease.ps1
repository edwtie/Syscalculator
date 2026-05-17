param(
    [ValidateSet("check", "build", "hash", "help")]
    [string] $Action = "check",

    [ValidateSet("daily", "beta", "production")]
    [string] $Channel = "daily"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$appVersion = "2.0"
$dateVersion = Get-Date -Format "yyyy.MM.dd"
$tag = "v$appVersion.$dateVersion-$Channel"
$installer = Join-Path $root "artifacts\installer\Syscalculator-$appVersion-$Channel-$appVersion.$dateVersion.exe"
$updateZip = Join-Path $root "artifacts\updates\Syscalculator-$appVersion-$Channel-$appVersion.$dateVersion.zip"
$manifest = Join-Path $root "web\updates\syscalculator.json"
$buildInstaller = Join-Path $root "src\syscalculator\Build\BuildInstaller.ps1"
$solution = Join-Path $root "Syscalculator20_UI_Prototype_OldModelConverterLook_BuildFix.sln"

function Write-Title([string] $Text) {
    Write-Host ""
    Write-Host "== $Text =="
}

function Get-ManifestInfo {
    if (-not (Test-Path $manifest)) {
        return $null
    }

    $json = Get-Content $manifest -Raw | ConvertFrom-Json
    return $json.channels.$Channel
}

function Write-ReleaseInfo {
    Write-Title "Syscalculator $Channel release"
    Write-Host "Root:        $root"
    Write-Host "Tag:         $tag"
    Write-Host "Manifest:    $manifest"
    Write-Host "Installer:   $installer"
    Write-Host "Update zip:  $updateZip"
    Write-Host "Release:     https://github.com/edwtie/Syscalculator/releases/tag/$tag"

    $gitHead = git -C $root rev-parse --short HEAD 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Git HEAD:    $gitHead"
    }

    $status = git -C $root status --short 2>$null
    if ($LASTEXITCODE -eq 0) {
        if ($status) {
            Write-Host ""
            Write-Host "Working tree has changes:"
            $status | ForEach-Object { Write-Host "  $_" }
        } else {
            Write-Host "Git status:  clean"
        }
    }

    $channelInfo = Get-ManifestInfo
    if ($channelInfo) {
        Write-Host ""
        Write-Host "Manifest channel:"
        Write-Host "  displayVersion: $($channelInfo.displayVersion)"
        Write-Host "  packageId:      $($channelInfo.packageId)"
        Write-Host "  sha256:         $($channelInfo.sha256)"
        Write-Host "  packageUrl:     $($channelInfo.packageUrl)"
    }

    if (Test-Path $updateZip) {
        $hash = (Get-FileHash $updateZip -Algorithm SHA256).Hash.ToLowerInvariant()
        Write-Host ""
        Write-Host "Local update zip SHA256:"
        Write-Host "  $hash"
    }

    if (Test-Path $installer) {
        $installerHash = (Get-FileHash $installer -Algorithm SHA256).Hash.ToLowerInvariant()
        Write-Host ""
        Write-Host "Local installer SHA256:"
        Write-Host "  $installerHash"
    }
}

function Invoke-Build {
    Write-Title "Build"
    dotnet build $solution
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    powershell -NoProfile -ExecutionPolicy Bypass -File $buildInstaller -Channel $Channel
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Title "Package hash"
    if (Test-Path $updateZip) {
        Get-FileHash $updateZip -Algorithm SHA256 | Format-List
    } else {
        Write-Host "Update zip not found: $updateZip"
        exit 1
    }
}

function Write-HashOnly {
    Write-Title "Hashes"
    if (Test-Path $updateZip) {
        Get-FileHash $updateZip -Algorithm SHA256 | Format-List
    } else {
        Write-Host "Update zip not found: $updateZip"
    }

    if (Test-Path $installer) {
        Get-FileHash $installer -Algorithm SHA256 | Format-List
    } else {
        Write-Host "Installer not found: $installer"
    }
}

function Write-Help {
    Write-Host "Usage:"
    Write-Host "  DAILY_RELEASE.bat check             Show release paths, marker, hashes and git status"
    Write-Host "  DAILY_RELEASE.bat build             Run dotnet build and BuildInstaller.ps1 for daily"
    Write-Host "  DAILY_RELEASE.bat hash              Show local installer/update zip hashes"
    Write-Host "  DAILY_RELEASE.bat check beta        Same check for beta channel"
    Write-Host ""
    Write-Host "Notes:"
    Write-Host "  This helper does not store FTP credentials."
    Write-Host "  Publish steps still use gh/curl manually after the package marker and release notes are chosen."
}

switch ($Action) {
    "check" { Write-ReleaseInfo }
    "build" { Invoke-Build }
    "hash" { Write-HashOnly }
    "help" { Write-Help }
}
