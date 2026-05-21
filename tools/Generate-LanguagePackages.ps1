param(
    [string]$LanguageDirectory = "src/syscalculator",
    [string]$HelpDirectory = "src/syscalculator/Resources/Help",
    [string]$HelpContentDirectory = "src/syscalculator/Resources/Help/Content",
    [string]$OutputDirectory = "web/packages/languages",
    [string]$ConceptDirectory = "artifacts/language-package-concepts",
    [string]$PackageVersion = (Get-Date -Format "yyyy.MM.dd") + ".001"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$languageRoot = Join-Path $repoRoot $LanguageDirectory
$helpRoot = Join-Path $repoRoot $HelpDirectory
$helpContentRoot = Join-Path $repoRoot $HelpContentDirectory
$outputRoot = Join-Path $repoRoot $OutputDirectory
$conceptRoot = Join-Path $repoRoot $ConceptDirectory
$project = Join-Path $repoRoot "src/Tiedragon.LanguagePackage/Tiedragon.LanguagePackage.csproj"

if (-not (Test-Path $languageRoot)) {
    throw "Language directory not found: $languageRoot"
}

if (-not (Test-Path $helpContentRoot)) {
    throw "Help content directory not found: $helpContentRoot"
}

if (-not (Test-Path $helpRoot)) {
    throw "Help directory not found: $helpRoot"
}

$displayNames = @{
    eng = @{ display = "English"; native = "English" }
    ned = @{ display = "Nederlands"; native = "Nederlands" }
    deu = @{ display = "Deutsch"; native = "Deutsch" }
    fra = @{ display = ("Fran" + [char]0x00E7 + "ais"); native = ("Fran" + [char]0x00E7 + "ais") }
    ind = @{ display = "Indonesia"; native = "Bahasa Indonesia" }
    ita = @{ display = "Italiano"; native = "Italiano" }
    por = @{ display = ("Portugu" + [char]0x00EA + "s"); native = ("Portugu" + [char]0x00EA + "s") }
    spa = @{ display = ("Espa" + [char]0x00F1 + "ol"); native = ("Espa" + [char]0x00F1 + "ol") }
    zho = @{ display = ([string]([char]0x4E2D) + [string]([char]0x6587)); native = ([string]([char]0x4E2D) + [string]([char]0x6587)) }
}

$allowedHelpScripts = @(
    "basis.js",
    "formula.js",
    "nod.js"
)

function Test-Mojibake {
    param([string]$Text)

    $markers = @(
        [string][char]0x00C3,
        [string][char]0x00C2,
        [string][char]0x00E2,
        ([string][char]0x00E4 + [string][char]0x00B8),
        ([string][char]0x00E6 + [string][char]0x2013)
    )

    foreach ($marker in $markers) {
        if ($Text.Contains($marker)) {
            return $true
        }
    }

    return $false
}

function Assert-NoMojibake {
    param(
        [string]$Value,
        [string]$Label
    )

    if (Test-Mojibake -Text $Value) {
        throw "Encoding/accent check failed for $Label`: $Value"
    }
}

function Copy-DirectoryContent {
    param(
        [string]$Source,
        [string]$Target
    )

    $sourceRoot = (Resolve-Path $Source).Path.TrimEnd('\', '/')
    Get-ChildItem $sourceRoot -Recurse -File | ForEach-Object {
        $relative = $_.FullName.Substring($sourceRoot.Length).TrimStart('\', '/')
        $targetPath = Join-Path $Target $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $targetPath) -Force | Out-Null
        Copy-Item $_.FullName $targetPath -Force
    }
}

New-Item -ItemType Directory -Path $outputRoot, $conceptRoot -Force | Out-Null

$results = @()
$languageFiles = Get-ChildItem $languageRoot -Filter "*.lng" -File | Sort-Object Name
foreach ($languageFile in $languageFiles) {
    $code = [System.IO.Path]::GetFileNameWithoutExtension($languageFile.Name).ToLowerInvariant()
    $names = $displayNames[$code]
    $displayName = if ($names) { $names.display } else { $code }
    $nativeName = if ($names) { $names.native } else { $displayName }
    Assert-NoMojibake -Value $displayName -Label "$code displayName"
    Assert-NoMojibake -Value $nativeName -Label "$code nativeName"

    $concept = Join-Path $conceptRoot $code
    if (Test-Path $concept) {
        Remove-Item $concept -Recurse -Force
    }

    New-Item -ItemType Directory -Path (Join-Path $concept "language"), (Join-Path $concept "help"), (Join-Path $concept "help/Content") -Force | Out-Null
    Copy-Item $languageFile.FullName (Join-Path $concept ("language/" + $code + ".lng")) -Force
    Get-ChildItem $helpRoot -File | Where-Object {
        $_.Extension -in @(".css", ".html") -or
            ($_.Extension -ieq ".js" -and $allowedHelpScripts -contains $_.Name)
    } | ForEach-Object {
        Copy-Item $_.FullName (Join-Path $concept ("help/" + $_.Name)) -Force
    }
    Copy-DirectoryContent -Source $helpContentRoot -Target (Join-Path $concept "help/Content")

    $manifest = [ordered]@{
        format = 1
        key = $code
        id = "tiedragon.language." + $code
        producer = "Tiedragon"
        product = "Syscalculator"
        softwareId = "tiedragon.syscalculator"
        languageCode = $code
        displayName = $displayName
        nativeName = $nativeName
        packageVersion = $PackageVersion
        fallbackLanguage = "eng"
    }
    $manifest | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 (Join-Path $concept "manifest.json")

    $outputPackage = Join-Path $outputRoot ("Syscalculator.Language." + $code + ".lngpdk")
    $json = dotnet run --project $project --no-restore -- agent-compile $concept $outputPackage
    $result = $json | ConvertFrom-Json
    if (-not $result.success) {
        throw "Language package compile failed for $code`: $json"
    }

    Assert-NoMojibake -Value ([string]$result.displayName) -Label "$code compiled displayName"

    dotnet run --project $project --no-restore -- validate $outputPackage | Out-Host
    $results += $result
}

$manifestPath = Join-Path $outputRoot "language-packages.json"
$results |
    Select-Object packageKey, languageCode, displayName, packageSha256, payloadSha256, entryCount, outputPath |
    ConvertTo-Json -Depth 5 |
    Set-Content -Encoding UTF8 $manifestPath

Write-Host "Generated $($results.Count) language packages in $outputRoot"
Write-Host "Manifest: $manifestPath"
