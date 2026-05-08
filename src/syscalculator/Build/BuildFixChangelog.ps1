param(
    [ValidateSet("daily", "beta", "production")]
    [string]$Channel = "daily",
    [string]$Fix = "",
    [string]$Addition = "",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$projectDir = Split-Path -Parent $PSScriptRoot
$repoRoot = Resolve-Path -LiteralPath (Join-Path $projectDir "..\..")
$projectPath = Join-Path $projectDir "Syscalculator.UI.WinForms.csproj"
$versionPath = Join-Path $projectDir "AppVersionInfo.Generated.cs"
$changelogPath = Join-Path $repoRoot "CHANGELOG.md"

if (-not $NoBuild) {
    Write-Host "Building Syscalculator UI..."
    dotnet build $projectPath
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed. Changelog was not changed."
    }
}

$versionText = Get-Content -LiteralPath $versionPath -Raw
$match = [regex]::Match($versionText, 'BuildNumber\s*=\s*"(?<date>\d{4}\.\d{2}\.\d{2})\.(?<build>\d{3})"')
if (-not $match.Success) {
    throw "Build number not found in $versionPath"
}

$buildDate = $match.Groups["date"].Value
$dailyVersion = "2.0.$buildDate"
$channelTitle = switch ($Channel) {
    "daily" { "Daily Releases" }
    "beta" { "Beta Releases" }
    "production" { "Production Releases" }
}

$entryHeading = switch ($Channel) {
    "daily" { "#### $dailyVersion Daily Build" }
    "beta" { "#### 2.0.$buildDate Beta Release" }
    "production" { "#### 2.0.$buildDate Production Release" }
}

if ([string]::IsNullOrWhiteSpace($Fix) -and [string]::IsNullOrWhiteSpace($Addition)) {
    $Fix = Read-Host "Describe today's fix for the changelog"
}

if ([string]::IsNullOrWhiteSpace($Fix) -and [string]::IsNullOrWhiteSpace($Addition)) {
    $Fix = "Release entry created; details still need to be filled in."
}

function Convert-ToListItems([string]$Text) {
    return $Text -split "\s*[;|]\s*" |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { "- " + $_.Trim() }
}

function Add-ItemsToSection([string]$Text, [string]$Heading, [string[]]$Items) {
    if ($Items.Count -eq 0) {
        return $Text
    }

    $Text = $Text.Replace("- None yet." + [Environment]::NewLine, "")
    $Text = $Text.Replace("- None yet.`n", "")

    $sectionIndex = $Text.IndexOf($Heading, [StringComparison]::Ordinal)
    if ($sectionIndex -lt 0) {
        return $Text.TrimEnd() + [Environment]::NewLine + [Environment]::NewLine + $Heading + [Environment]::NewLine + ($Items -join [Environment]::NewLine) + [Environment]::NewLine
    }

    $insertIndex = $Text.IndexOf("`n", $sectionIndex + $Heading.Length)
    if ($insertIndex -lt 0) {
        $insertIndex = $Text.Length
    }
    else {
        $insertIndex += 1
    }

    while ($insertIndex -lt $Text.Length -and ($Text[$insertIndex] -eq "`r" -or $Text[$insertIndex] -eq "`n")) {
        $insertIndex++
    }

    $block = ($Items -join [Environment]::NewLine) + [Environment]::NewLine
    return $Text.Insert($insertIndex, $block)
}

function Ensure-ChannelSection([string]$Text, [string]$MainMarker, [string]$SectionTitle) {
    $sectionHeading = "### $SectionTitle"
    if ($Text.IndexOf($sectionHeading, [StringComparison]::Ordinal) -ge 0) {
        return $Text
    }

    $mainIndex = $Text.IndexOf($MainMarker, [StringComparison]::Ordinal)
    if ($mainIndex -lt 0) {
        throw "Could not find '$MainMarker' in CHANGELOG.md"
    }

    $insertIndex = $Text.IndexOf("`n", $mainIndex)
    if ($insertIndex -lt 0) {
        $insertIndex = $Text.Length
    }
    else {
        $insertIndex += 1
    }

    $sectionBlock = [Environment]::NewLine + $sectionHeading + [Environment]::NewLine + [Environment]::NewLine
    return $Text.Insert($insertIndex, $sectionBlock)
}

$fixItems = @(Convert-ToListItems $Fix)
$additionItems = @(Convert-ToListItems $Addition)
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm"
$mainMarker = "## Syscalculator 2.0 Beta"
$channelHeading = "### $channelTitle"

$newEntry = @"
$entryHeading

**Date:** $timestamp

**Fixes:**
$(if ($fixItems.Count -gt 0) { $fixItems -join [Environment]::NewLine } else { "- None yet." })

**Additions:**
$(if ($additionItems.Count -gt 0) { $additionItems -join [Environment]::NewLine } else { "- None yet." })

"@

$changelog = Get-Content -LiteralPath $changelogPath -Raw
$changelog = Ensure-ChannelSection $changelog $mainMarker $channelTitle

$channelIndex = $changelog.IndexOf($channelHeading, [StringComparison]::Ordinal)
$nextChannel = [regex]::Match($changelog.Substring($channelIndex + $channelHeading.Length), '(?m)^###\s+')
$channelEnd = if ($nextChannel.Success) {
    $channelIndex + $channelHeading.Length + $nextChannel.Index
}
else {
    $changelog.Length
}

$beforeChannel = $changelog.Substring(0, $channelIndex)
$channelSection = $changelog.Substring($channelIndex, $channelEnd - $channelIndex)
$afterChannel = $changelog.Substring($channelEnd)

$entryIndex = $channelSection.IndexOf($entryHeading, [StringComparison]::Ordinal)
if ($entryIndex -lt 0) {
    $insertIndex = $channelSection.IndexOf("`n", $channelHeading.Length)
    if ($insertIndex -lt 0) {
        $insertIndex = $channelSection.Length
    }
    else {
        $insertIndex += 1
    }

    $channelSection = $channelSection.Insert($insertIndex, [Environment]::NewLine + $newEntry)
}
else {
    $nextEntry = [regex]::Match($channelSection.Substring($entryIndex + $entryHeading.Length), '(?m)^####\s+')
    $entryEnd = if ($nextEntry.Success) {
        $entryIndex + $entryHeading.Length + $nextEntry.Index
    }
    else {
        $channelSection.Length
    }

    $beforeEntry = $channelSection.Substring(0, $entryIndex)
    $entrySection = $channelSection.Substring($entryIndex, $entryEnd - $entryIndex)
    $afterEntry = $channelSection.Substring($entryEnd)

    $entrySection = Add-ItemsToSection $entrySection "**Fixes:**" $fixItems
    $entrySection = Add-ItemsToSection $entrySection "**Additions:**" $additionItems
    $channelSection = $beforeEntry + $entrySection + $afterEntry
}

$updated = $beforeChannel + $channelSection + $afterChannel
Set-Content -LiteralPath $changelogPath -Value $updated -NoNewline -Encoding UTF8

Write-Host "Release channel: $Channel"
Write-Host "Release entry: $($entryHeading.TrimStart('#').Trim())"
Write-Host "Changelog updated: $changelogPath"
if ($fixItems.Count -gt 0) {
    Write-Host ""
    Write-Host "Fixes:"
    $fixItems | ForEach-Object { Write-Host $_ }
}
if ($additionItems.Count -gt 0) {
    Write-Host ""
    Write-Host "Additions:"
    $additionItems | ForEach-Object { Write-Host $_ }
}
