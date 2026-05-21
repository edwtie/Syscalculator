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

function Read-LanguageMap {
    param([string]$Path)

    $map = @{}
    foreach ($line in [System.IO.File]::ReadAllLines($Path, [System.Text.Encoding]::UTF8)) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith("#")) {
            continue
        }

        $separator = $line.IndexOf("=")
        if ($separator -le 0) {
            continue
        }

        $key = $line.Substring(0, $separator).Trim()
        $value = $line.Substring($separator + 1)
        if ($key.Length -gt 0 -and -not $map.ContainsKey($key)) {
            $map[$key] = $value
        }
    }

    return $map
}

function Set-ToolEditorHelpContent {
    param(
        [string]$TargetRoot,
        [string]$LanguageCode
    )

    $toolEditorRoot = Join-Path $TargetRoot "help/Content/tool-editor"
    New-Item -ItemType Directory -Path $toolEditorRoot -Force | Out-Null

    if ($LanguageCode -eq "ned") {
        return
    }

    $pages = @{
        "overview.html" = @"
<p>ToolEditor is the workspace for Tiedragon language packages. You edit translations, user help, NOD help, formula cards and images without hunting through loose folders.</p>

<div class="notice">
  <b>Work model:</b> the package tree is on the left, the active editor is in the middle, and the preview shows how help text and images will appear in Syscalculator.
</div>

<h2>Layout</h2>
<ul>
  <li><b>Translation</b> contains the language entries in the package.</li>
  <li><b>User help</b> contains normal user manuals.</li>
  <li><b>NOD for developers</b> contains technical NOD topics.</li>
  <li><b>Formula card</b> contains calculation and formula help.</li>
  <li><b>Media and images</b> contains only allowed images such as png, jpg and svg.</li>
</ul>

<h2>Tabs</h2>
<p>A topic remains visible in the tree when you close its tab. This lets you clean up tabs without losing the package structure. Right-click a tab to close all tabs, tabs on the left or tabs on the right. Changed tabs are checked before they close.</p>
"@
        "package-workflow.html" = @"
<p>A language package always starts from a base package. This prevents empty or incomplete packages and keeps the fixed structure in place.</p>

<h2>Main actions</h2>
<table>
  <tr><th>Action</th><th>Purpose</th></tr>
  <tr><td>New package</td><td>Creates a new package from the English base structure.</td></tr>
  <tr><td>Open language package</td><td>Opens an existing concept or compiled language package.</td></tr>
  <tr><td>Save concept language package</td><td>Saves your work as a concept. Concepts are not official releases.</td></tr>
  <tr><td>Compile language package</td><td>Creates the final .lngpdk file after validation.</td></tr>
</table>

<div class="warning-sign">
  <div><b>A concept is not an official package.</b><br>Use a concept for editing and testing. Only a compiled package belongs in daily/release distribution.</div>
</div>

<h2>Manifest</h2>
<p>The manifest describes language, producer, product, software-id, package version and fallback language. ToolEditor shows this as package properties so you do not have to edit raw JSON directly.</p>
"@
        "html-editor.html" = @"
<p>HTML documents can be edited in two modes.</p>

<h2>Source</h2>
<p>Source shows the HTML source with line numbers. Use this mode for precise control, tables, links, code blocks and when you need to see the exact tags stored in the package.</p>

<h2>Edit</h2>
<p>Edit shows the page as editable help text. This is useful for normal text changes. When you return to Source, the edited HTML is written back to the source.</p>

<h2>Help blocks</h2>
<table>
  <tr><th>Button</th><th>Use</th></tr>
  <tr><td>H1 / H2 / P</td><td>Headings and text blocks.</td></tr>
  <tr><td>Info / Tip / Warn</td><td>Standard help messages.</td></tr>
  <tr><td>Code / Kbd</td><td>Code examples and keys.</td></tr>
  <tr><td>Link / Img</td><td>References to topics and media inside the package.</td></tr>
</table>

<div class="notice">
  <b>Following links:</b> internal links work in preview like the real help. In Edit, right-click a link to jump to the linked topic.
</div>
"@
        "media.html" = @"
<p>Media is the file manager for images used by help documents.</p>

<h2>Allowed files</h2>
<ul>
  <li>png</li>
  <li>jpg and jpeg</li>
  <li>svg</li>
</ul>

<p>Unknown files do not belong in a language package. This keeps the package predictable, safer and easier to validate.</p>

<h2>Preview</h2>
<p>The image preview uses a full canvas. You can zoom in, zoom out, return to canvas 100% and drag the image with the mouse to inspect details.</p>

<h2>HTML links</h2>
<p>An image belongs in the package only when a help document links to it. Check HTML links before compiling.</p>
"@
        "compile.html" = @"
<p>Compile only when the content is complete. ToolEditor checks the package structure before the final .lngpdk file is created.</p>

<h2>Validate</h2>
<ul>
  <li>Checks required language and help files.</li>
  <li>Checks internal HTML links.</li>
  <li>Checks that images exist.</li>
  <li>Rejects unknown scripts.</li>
  <li>Checks accents and suspicious mojibake in language files.</li>
</ul>

<h2>Compile</h2>
<p>Compile creates a language package with a fixed file list and SHA-256 checks. If validation fails, the package is rejected and you get error codes that are also usable in batch or agent workflows.</p>

<div class="notice">
  <b>Scripts:</b> only basis.js, nod.js and formula.js are allowed in the base package.
</div>
"@
    }

    foreach ($page in $pages.GetEnumerator()) {
        [System.IO.File]::WriteAllText(
            (Join-Path $toolEditorRoot $page.Key),
            $page.Value.Trim() + [Environment]::NewLine,
            [System.Text.UTF8Encoding]::new($false))
    }
}

New-Item -ItemType Directory -Path $outputRoot, $conceptRoot -Force | Out-Null

$results = @()
$languageFiles = Get-ChildItem $languageRoot -Filter "*.lng" -File | Sort-Object Name
$englishLanguagePath = Join-Path $languageRoot "eng.lng"
if (-not (Test-Path $englishLanguagePath)) {
    throw "English base language file not found: $englishLanguagePath"
}

$englishLanguageMap = Read-LanguageMap -Path $englishLanguagePath
$requiredLanguageKeys = $englishLanguageMap.Keys | Sort-Object
foreach ($languageFile in $languageFiles) {
    $code = [System.IO.Path]::GetFileNameWithoutExtension($languageFile.Name).ToLowerInvariant()
    $names = $displayNames[$code]
    $displayName = if ($names) { $names.display } else { $code }
    $nativeName = if ($names) { $names.native } else { $displayName }
    Assert-NoMojibake -Value $displayName -Label "$code displayName"
    Assert-NoMojibake -Value $nativeName -Label "$code nativeName"
    $languageText = [System.IO.File]::ReadAllText($languageFile.FullName, [System.Text.Encoding]::UTF8)
    Assert-NoMojibake -Value $languageText -Label $languageFile.Name

    $languageMap = Read-LanguageMap -Path $languageFile.FullName
    $missingRequiredKeys = @($requiredLanguageKeys | Where-Object {
        -not $languageMap.ContainsKey($_) -or [string]::IsNullOrWhiteSpace([string]$languageMap[$_])
    })
    $translationComplete = $missingRequiredKeys.Count -eq 0
    if (-not $translationComplete) {
        Write-Warning "$code is incomplete and will not be active in the release language index. Missing keys: $($missingRequiredKeys.Count)"
    }

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
    Set-ToolEditorHelpContent -TargetRoot $concept -LanguageCode $code

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
    $result | Add-Member -NotePropertyName translationComplete -NotePropertyValue $translationComplete
    $result | Add-Member -NotePropertyName active -NotePropertyValue ($code -eq "eng" -or $translationComplete)
    $result | Add-Member -NotePropertyName missingRequiredKeyCount -NotePropertyValue $missingRequiredKeys.Count
    $result | Add-Member -NotePropertyName missingRequiredKeys -NotePropertyValue $missingRequiredKeys

    dotnet run --project $project --no-restore -- validate $outputPackage | Out-Host
    $results += $result
}

$manifestPath = Join-Path $outputRoot "language-packages.json"
$results |
    Select-Object packageKey, languageCode, displayName, packageSha256, payloadSha256, entryCount,
        @{ Name = "fileName"; Expression = { [System.IO.Path]::GetFileName($_.outputPath) } },
        @{ Name = "downloadPath"; Expression = { "packages/languages/" + [System.IO.Path]::GetFileName($_.outputPath) } },
        active, translationComplete, missingRequiredKeyCount, missingRequiredKeys |
    ConvertTo-Json -Depth 5 |
    Set-Content -Encoding UTF8 $manifestPath

Write-Host "Generated $($results.Count) language packages in $outputRoot"
Write-Host "Manifest: $manifestPath"
