param(
    [string]$WinScpSession = "edwtie@tiedragon.nl",
    [switch]$SkipNl,
    [switch]$SkipCom
)

$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "Assert-PowerShell7.ps1") -Purpose "deploying the Syscalculator update manifest" -ScriptPath $PSCommandPath

$repoRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $repoRoot "web/updates/syscalculator.json"
$updaterPath = Join-Path $repoRoot "web/updates/updater.php"

if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "Update manifest not found: $manifestPath"
}

if (-not (Test-Path -LiteralPath $updaterPath)) {
    throw "Updater PHP endpoint not found: $updaterPath"
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$expectedDaily = $manifest.channels.daily
if (-not $expectedDaily.version -or -not $expectedDaily.packageId -or -not $expectedDaily.sha256) {
    throw "Daily channel in web/updates/syscalculator.json is incomplete."
}

$winScp = @(
    (Join-Path ${env:ProgramFiles(x86)} "WinSCP/WinSCP.com"),
    (Join-Path $env:ProgramFiles "WinSCP/WinSCP.com"),
    "WinSCP.com"
) | Where-Object {
    if ($_ -eq "WinSCP.com") {
        $null -ne (Get-Command $_ -ErrorAction SilentlyContinue)
    }
    else {
        Test-Path -LiteralPath $_
    }
} | Select-Object -First 1

if (-not $winScp) {
    throw "WinSCP.com was not found. Install WinSCP or add it to PATH."
}

$targets = @()
if (-not $SkipCom) {
    $targets += [pscustomobject]@{
        Name = "tiedragon.com"
        RemotePath = "/home/edwtie/domains/tiedragon.com/public_html/api/syscalculator-updates"
        Url = "https://www.tiedragon.com/api/syscalculator-updates/syscalculator.json"
    }
}

if (-not $SkipNl) {
    $targets += [pscustomobject]@{
        Name = "tiedragon.nl"
        RemotePath = "/home/edwtie/domains/tiedragon.nl/public_html/api/syscalculator-updates"
        Url = "https://tiedragon.nl/api/syscalculator-updates/syscalculator.json"
    }
}

if ($targets.Count -eq 0) {
    throw "Nothing to deploy. Both targets were skipped."
}

$scriptLines = New-Object System.Collections.Generic.List[string]
$scriptLines.Add("option batch abort")
$scriptLines.Add("option confirm off")
$scriptLines.Add("open `"$WinScpSession`"")

foreach ($target in $targets) {
    $scriptLines.Add("cd $($target.RemotePath)")
    $scriptLines.Add("put `"$manifestPath`" syscalculator.json")
    $scriptLines.Add("put `"$updaterPath`" updater.php")
}

$scriptLines.Add("exit")

$tempScript = Join-Path ([IO.Path]::GetTempPath()) ("syscalculator-winscp-deploy-" + [Guid]::NewGuid().ToString("N") + ".txt")
try {
    Set-Content -LiteralPath $tempScript -Value $scriptLines -Encoding ascii
    & $winScp "/script=$tempScript"
    if ($LASTEXITCODE -ne 0) {
        throw "WinSCP failed with exit code $LASTEXITCODE."
    }
}
finally {
    Remove-Item -LiteralPath $tempScript -Force -ErrorAction SilentlyContinue
}

foreach ($target in $targets) {
    $live = Invoke-RestMethod -Uri $target.Url -Headers @{ "Cache-Control" = "no-cache" }
    $liveDaily = $live.channels.daily

    if ($liveDaily.version -ne $expectedDaily.version) {
        throw "$($target.Name) version mismatch. Expected $($expectedDaily.version), got $($liveDaily.version)."
    }

    if ($liveDaily.packageId -ne $expectedDaily.packageId) {
        throw "$($target.Name) packageId mismatch. Expected $($expectedDaily.packageId), got $($liveDaily.packageId)."
    }

    if ($liveDaily.sha256 -ne $expectedDaily.sha256) {
        throw "$($target.Name) sha256 mismatch. Expected $($expectedDaily.sha256), got $($liveDaily.sha256)."
    }

    Write-Host "$($target.Name): OK $($liveDaily.version) $($liveDaily.packageId)"
}

Write-Host "Update manifest deploy completed."
