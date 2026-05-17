param(
    [ValidateSet("check", "build", "hash", "help")]
    [string] $Action = "check",

    [ValidateSet("daily", "beta", "stable", "production")]
    [string] $Channel = "daily"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$appVersion = "2.0"
$manifest = Join-Path $root "web\updates\syscalculator.json"
$buildInstaller = Join-Path $root "src\syscalculator\Build\BuildInstaller.ps1"
$solution = Join-Path $root "Syscalculator20_UI_Prototype_OldModelConverterLook_BuildFix.sln"

$manifestChannelName = if ($Channel -eq "production") { "stable" } else { $Channel }

function Write-Title([string] $Text) {
    Write-Host ""
    Write-Host "== $Text =="
}

function Get-ManifestInfo {
    if (-not (Test-Path $manifest)) {
        return $null
    }

    $json = Get-Content $manifest -Raw | ConvertFrom-Json
    return $json.channels.PSObject.Properties[$manifestChannelName].Value
}

$channelInfo = Get-ManifestInfo
$dateVersion = if ($channelInfo -and $channelInfo.date) {
    [DateTime]::Parse($channelInfo.date).ToString("yyyy.MM.dd")
} elseif ($channelInfo -and $channelInfo.displayVersion) {
    [string] $channelInfo.displayVersion
} else {
    Get-Date -Format "yyyy.MM.dd"
}
$releaseChannelName = if ($Channel -eq "stable") { "production" } else { $Channel }
$tag = "v$appVersion.$dateVersion-$releaseChannelName"
$installer = Join-Path $root "artifacts\installer\Syscalculator-$appVersion-$releaseChannelName-$appVersion.$dateVersion.exe"
$updateZip = Join-Path $root "artifacts\updates\Syscalculator-$appVersion-$releaseChannelName-$appVersion.$dateVersion.zip"

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

    if ($channelInfo) {
        Write-Host ""
        Write-Host "Manifest channel:"
        Write-Host "  version:        $($channelInfo.version)"
        Write-Host "  displayVersion: $($channelInfo.displayVersion)"
        Write-Host "  date:           $($channelInfo.date)"
        Write-Host "  title:          $($channelInfo.title)"
        Write-Host "  packageId:      $($channelInfo.packageId)"
        Write-Host "  sha256:         $($channelInfo.sha256)"
        Write-Host "  downloadUrl:    $($channelInfo.downloadUrl)"
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

    $updateStatePath = Join-Path $root "src\syscalculator\update-state.cfg"
    $originalUpdateState = if (Test-Path $updateStatePath) { Get-Content $updateStatePath -Raw } else { $null }

    try {
        if ($channelInfo -and $channelInfo.packageId) {
            $markerText = "# Installed update package marker.`r`n# Used by the updater so same-date daily packages can still be detected without showing a build number.`r`npackageId=$($channelInfo.packageId)`r`n"
            [System.IO.File]::WriteAllText($updateStatePath, $markerText, [System.Text.UTF8Encoding]::new($false))
            Write-Host "Using package marker: $($channelInfo.packageId)"
        }

        powershell -NoProfile -ExecutionPolicy Bypass -File $buildInstaller -Channel $releaseChannelName
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    finally {
        if ($null -ne $originalUpdateState) {
            [System.IO.File]::WriteAllText($updateStatePath, $originalUpdateState, [System.Text.UTF8Encoding]::new($false))
        }
    }

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
    Write-Host "  BETA_RELEASE.bat                    Shortcut for beta check"
    Write-Host "  BETA_RELEASE.bat hash               Shortcut for beta hashes"
    Write-Host "  STORE_RELEASE.bat                   Shortcut for production Store release check"
    Write-Host "  STORE_RELEASE.bat build             Build production installer for Microsoft Store submission"
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
