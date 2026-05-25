param(
    [string]$LanguageDirectory = "src/syscalculator",
    [string]$HelpDirectory = "src/syscalculator/Resources/Help",
    [string]$HelpContentDirectory = "src/syscalculator/Resources/Help/Content",
    [string]$OutputDirectory = "web/packages/languages",
    [string]$ConceptDirectory = "artifacts/language-package-concepts",
    [string]$ObjectPackageDirectory = "artifacts/language-package-objects",
    [string]$PackageVersion = (Get-Date -Format "yyyy.MM.dd") + ".001",
    [string]$SigningPrivateKey = "",
    [string]$SigningKeyId = ""
)

$ErrorActionPreference = "Stop"

# Encoding rule:
# - Run this script with PowerShell 7+ (`pwsh`) to avoid Windows PowerShell 5.1
#   encoding surprises.
# - Generated package files are written as UTF-8 without BOM so package hashes
#   remain stable.
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptRoot "Assert-PowerShell7.ps1") -Purpose "language packages" -ScriptPath $PSCommandPath
$repoRoot = Split-Path -Parent $scriptRoot
$languageRoot = Join-Path $repoRoot $LanguageDirectory
$helpRoot = Join-Path $repoRoot $HelpDirectory
$helpContentRoot = Join-Path $repoRoot $HelpContentDirectory
$outputRoot = Join-Path $repoRoot $OutputDirectory
$conceptRoot = Join-Path $repoRoot $ConceptDirectory
$objectPackageRoot = Join-Path $repoRoot $ObjectPackageDirectory
$project = Join-Path $repoRoot "src/Tiedragon.LanguagePackage/Tiedragon.LanguagePackage.csproj"
$policyPath = Join-Path $repoRoot "src/Tiedragon.LanguagePackage/language-package-policy.ini"
$signingEnabled = -not [string]::IsNullOrWhiteSpace($SigningPrivateKey) -or -not [string]::IsNullOrWhiteSpace($SigningKeyId)

if ($signingEnabled) {
    if ([string]::IsNullOrWhiteSpace($SigningPrivateKey) -or [string]::IsNullOrWhiteSpace($SigningKeyId)) {
        throw "Signing requires both -SigningPrivateKey and -SigningKeyId."
    }

    $signingPrivateKeyPath = Join-Path $repoRoot $SigningPrivateKey
    if (-not (Test-Path $signingPrivateKeyPath)) {
        throw "Signing private key not found: $signingPrivateKeyPath"
    }
}

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

function Read-IniPackageValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Section,

        [Parameter(Mandatory = $true)]
        [string]$Key
    )

    if (-not (Test-Path $Path)) {
        return $null
    }

    $current = ""
    foreach ($rawLine in [System.IO.File]::ReadLines($Path)) {
        $line = $rawLine.Trim()
        if ($line.Length -eq 0 -or $line.StartsWith("#") -or $line.StartsWith(";")) {
            continue
        }

        if ($line.StartsWith("[") -and $line.EndsWith("]")) {
            $current = $line.Substring(1, $line.Length - 2).Trim()
            continue
        }

        if (-not $current.Equals($Section, [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        $separator = $line.IndexOf("=")
        if ($separator -le 0) {
            continue
        }

        $name = $line.Substring(0, $separator).Trim()
        if ($name.Equals($Key, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $line.Substring($separator + 1).Trim()
        }
    }

    return $null
}

$allowedHelpScriptsValue = Read-IniPackageValue -Path $policyPath -Section "package" -Key "allowedScripts"
if ([string]::IsNullOrWhiteSpace($allowedHelpScriptsValue)) {
    throw "Language package policy is missing package.allowedScripts: $policyPath"
}

$allowedHelpScripts = @(
    $allowedHelpScriptsValue.Split([char[]]@(","), [System.StringSplitOptions]::RemoveEmptyEntries) |
        ForEach-Object { $_.Trim() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
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

function Write-Utf8NoBomText {
    param(
        [string]$Path,
        [string]$Text
    )

    [System.IO.File]::WriteAllText($Path, $Text, $script:Utf8NoBom)
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

function Convert-LanguageHelpBody {
    param(
        [string]$Value,
        [hashtable]$LanguageMap
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    $text = $Value.Replace("\r\n", [Environment]::NewLine).Replace("\n", [Environment]::NewLine)
    $html = if ($text -match "<[a-zA-Z][^>]*>") {
        Expand-LanguageHelpTokens -Html $text.Trim() -LanguageMap $LanguageMap
    }
    else {
        Expand-LanguageHelpTokens `
            -Html ("<p>" + [System.Net.WebUtility]::HtmlEncode($text).Replace([Environment]::NewLine, "<br>") + "</p>").Trim() `
            -LanguageMap $LanguageMap
    }

    return Format-GeneratedHtmlSource -Html $html
}

function Format-GeneratedHtmlSource {
    param([string]$Html)

    if ([string]::IsNullOrWhiteSpace($Html)) {
        return ""
    }

    $text = $Html.Trim()
    $blockTags = "main|section|article|header|footer|nav|h1|h2|h3|h4|p|ul|ol|li|div|table|thead|tbody|tfoot|tr|th|td|details|summary|pre|blockquote|figure|figcaption|button"
    $text = [regex]::Replace($text, ">\s*<(?=(?:/?)(?:$blockTags)\b)", ">" + [Environment]::NewLine + "<", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $text = [regex]::Replace($text, "(</(?:$blockTags)>)\s*(?=<)", '$1' + [Environment]::NewLine, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $text = [regex]::Replace($text, "(</tr>)\s*(?=<tr\b)", '$1' + [Environment]::NewLine, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $text = [regex]::Replace($text, "(</li>)\s*(?=<li\b)", '$1' + [Environment]::NewLine, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $text = [regex]::Replace($text, "(\r?\n){2,}", [Environment]::NewLine)

    return $text.TrimEnd()
}

function Get-LanguageMapText {
    param(
        [hashtable]$LanguageMap,
        [string]$Key,
        [string]$Fallback
    )

    if ($LanguageMap.ContainsKey($Key) -and -not [string]::IsNullOrWhiteSpace([string]$LanguageMap[$Key])) {
        return [string]$LanguageMap[$Key]
    }

    return $Fallback
}

function Expand-LanguageHelpTokens {
    param(
        [string]$Html,
        [hashtable]$LanguageMap
    )

    if ([string]::IsNullOrWhiteSpace($Html)) {
        return ""
    }

    $nodAlt = [System.Net.WebUtility]::HtmlEncode((Get-LanguageMapText `
        -LanguageMap $LanguageMap `
        -Key "help.main.page.nodeditor.screenshot_alt" `
        -Fallback "NOD Editor screenshot"))
    $wizardAlt = [System.Net.WebUtility]::HtmlEncode((Get-LanguageMapText `
        -LanguageMap $LanguageMap `
        -Key "help.main.page.wizard.screenshot_alt" `
        -Fallback "WizardExpress screenshot"))

    return $Html.
        Replace("{WarningBoardSvg}", "<img class=""warning-board-icon"" src=""/assets/warning_board.svg"" alt="""">").
        Replace("{NodEditorScreenshot}", "<div class=""screenshot-frame""><img src=""/assets/NodEditorHelp.svg"" alt=""$nodAlt""></div>").
        Replace("{WizardExpressScreenshot}", "<div class=""screenshot-frame""><img src=""/assets/WizardExpressHelp.png"" alt=""$wizardAlt""></div>")
}

function Expand-LanguageTextPlaceholders {
    param(
        [string]$Text,
        [hashtable]$LanguageMap
    )

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return ""
    }

    return [regex]::Replace($Text, "\[(?<key>[A-Za-z0-9_.-]+)\]", {
        param($match)

        $key = $match.Groups["key"].Value
        if (-not $LanguageMap.ContainsKey($key)) {
            return $match.Value
        }

        $value = [string]$LanguageMap[$key]
        if ($value -match "<[A-Za-z][^>]*>") {
            return $value
        }

        return [System.Net.WebUtility]::HtmlEncode($value)
    })
}

function Write-LanguageHelpPage {
    param(
        [hashtable]$LanguageMap,
        [string]$Key,
        [string]$Path
    )

    if (-not $LanguageMap.ContainsKey($Key)) {
        return
    }

    $body = Convert-LanguageHelpBody -Value ([string]$LanguageMap[$Key]) -LanguageMap $LanguageMap
    if ([string]::IsNullOrWhiteSpace($body)) {
        return
    }

    New-Item -ItemType Directory -Path (Split-Path -Parent $Path) -Force | Out-Null
    Write-Utf8NoBomText -Path $Path -Text ($body + [Environment]::NewLine)
}

function Remove-AuthoringHelpContent {
    param([string]$TargetRoot)

    $contentRoot = Join-Path $TargetRoot "help/Content"
    $authoringFiles = @(
        "README.html",
        "WRITER_GUIDE.html",
        "WRITER_TEMPLATE.html",
        "WRITER_TOOLBOX.html",
        "WRITER_TOOLS.html"
    )

    foreach ($name in $authoringFiles) {
        $path = Join-Path $contentRoot $name
        if (Test-Path $path) {
            Remove-Item $path -Force
        }
    }

    foreach ($relativePath in @("HelpApi", "nod/snippet")) {
        $path = Join-Path $contentRoot $relativePath
        if (Test-Path $path) {
            Remove-Item $path -Recurse -Force
        }
    }
}

function Expand-AllPackageTextTokens {
    param(
        [string]$TargetRoot,
        [hashtable]$LanguageMap
    )

    Get-ChildItem $TargetRoot -Recurse -File | Where-Object {
        $_.Extension -in @(".html", ".svg")
    } | ForEach-Object {
        $text = [System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)
        $expanded = Expand-LanguageHelpTokens -Html $text -LanguageMap $LanguageMap
        $expanded = Expand-LanguageTextPlaceholders -Text $expanded -LanguageMap $LanguageMap
        if ($_.Extension.Equals(".html", [StringComparison]::OrdinalIgnoreCase)) {
            $expanded = Format-GeneratedHtmlSource -Html $expanded
        }
        if (-not $expanded.Equals($text, [StringComparison]::Ordinal)) {
            Write-Utf8NoBomText -Path $_.FullName -Text ($expanded.TrimEnd() + [Environment]::NewLine)
        }
    }
}

function Set-GeneratedHelpContentFromLanguageMap {
    param(
        [string]$TargetRoot,
        [hashtable]$LanguageMap
    )

    $mainRoot = Join-Path $TargetRoot "help/Content/main"
    $mainPages = @{
        "help.main.page.intro.body" = "intro.html"
        "help.main.page.main.body" = "main.html"
        "help.main.page.fields.body" = "fields.html"
        "help.main.page.wizard.body" = "wizard.html"
        "help.main.page.calculator.body" = "calculator.html"
        "help.main.page.applications.body" = "applications.html"
        "help.main.page.configuration.body" = "configuration.html"
        "help.main.page.updater.body" = "updater.html"
        "help.main.page.window.body" = "window.html"
        "help.main.page.nodfiles.body" = "nodfiles.html"
        "help.main.page.nodeditor.body" = "nodeditor.html"
        "help.main.page.support.body" = "support.html"
    }

    foreach ($page in $mainPages.GetEnumerator()) {
        Write-LanguageHelpPage `
            -LanguageMap $LanguageMap `
            -Key $page.Key `
            -Path (Join-Path $mainRoot $page.Value)
    }

    $nodRoot = Join-Path $TargetRoot "help/Content/nod/full"
    $nodPages = @{
        "editor.nod_help.full.intro" = "nod-intro.html"
        "editor.nod_help.full.guide" = "guide.html"
        "editor.nod_help.full.classic" = "classic.html"
        "editor.nod_help.full.classic_book" = "classic_book.html"
        "editor.nod_help.full.compatibility" = "compatibility.html"
        "editor.nod_help.full.history" = "history.html"
        "editor.nod_help.full.structure" = "structure.html"
        "editor.nod_help.full.workflow" = "workflow.html"
        "editor.nod_help.full.symbols" = "symbols.html"
        "editor.nod_help.full.graph_preview" = "graph-preview.html"
        "editor.nod_help.full.si_scale" = "si-scale.html"
        "editor.nod_help.full.commands" = "commands.html"
        "editor.nod_help.full.examples" = "examples.html"
        "editor.nod_help.full.troubleshooting" = "troubleshooting.html"
    }

    foreach ($page in $nodPages.GetEnumerator()) {
        Write-LanguageHelpPage `
            -LanguageMap $LanguageMap `
            -Key $page.Key `
            -Path (Join-Path $nodRoot $page.Value)
    }
}

function Add-RequiredHelpMedia {
    param([string]$TargetRoot)

    $assetsRoot = Join-Path $TargetRoot "assets"
    New-Item -ItemType Directory -Path $assetsRoot -Force | Out-Null

    $resourceRoot = Join-Path $repoRoot "src/syscalculator/Resources"
    $requiredMedia = @(
        "NodEditorHelp.svg",
        "WizardExpressHelp.png"
    )

    foreach ($name in $requiredMedia) {
        $source = Join-Path $resourceRoot $name
        if (Test-Path $source) {
            Copy-Item $source (Join-Path $assetsRoot $name) -Force
        }
    }

    $warningSvg = @"
<svg class="warning-board" viewBox="0 0 52 48" xmlns="http://www.w3.org/2000/svg" aria-hidden="true" focusable="false">
  <path d="M26 4 49 43H3L26 4Z" fill="#facc15" stroke="#b91c1c" stroke-width="4" stroke-linejoin="round"/>
  <path d="M26 17v13" stroke="#7f1d1d" stroke-width="5" stroke-linecap="round"/>
  <circle cx="26" cy="37" r="3" fill="#7f1d1d"/>
</svg>
"@
    Write-Utf8NoBomText -Path (Join-Path $assetsRoot "warning_board.svg") -Text ($warningSvg.Trim() + [Environment]::NewLine)
}

function Set-ToolEditorHelpContent {
    param(
        [string]$TargetRoot,
        [string]$LanguageCode
    )

    $toolEditorRoot = Join-Path $TargetRoot "help/Content/tool-editor"
    New-Item -ItemType Directory -Path $toolEditorRoot -Force | Out-Null

    if ($LanguageCode -eq "ned") {
        $sourceToolEditorRoot = Join-Path $helpContentRoot "tool-editor"
        if (Test-Path $sourceToolEditorRoot) {
            Copy-DirectoryContent -Source $sourceToolEditorRoot -Target $toolEditorRoot
        }

        return
    }

    $localizedPages = @{
        deu = @{
            "overview.html" = "<p>ToolEditor ist der Arbeitsbereich fuer Tiedragon-Sprachpakete. Links steht der Paketbaum, in der Mitte der Editor und rechts die Vorschau.</p><h2>Aufbau</h2><ul><li>Uebersetzung enthaelt die Spracheintraege.</li><li>Benutzerhilfe enthaelt normale Handbuchseiten.</li><li>NOD fuer Entwickler enthaelt technische Themen.</li><li>Formelkarte enthaelt Rechen- und Formelhilfe.</li><li>Medien und Bilder enthaelt nur erlaubte Bilder.</li></ul><p>Geschlossene Registerkarten bleiben im Baum sichtbar, damit die Struktur erhalten bleibt.</p>"
            "package-workflow.html" = "<p>Ein Sprachpaket beginnt immer mit einem Basispaket. Dadurch entsteht kein leeres Paket und die feste Struktur bleibt erhalten.</p><h2>Aktionen</h2><ul><li>Neues Paket erstellt ein Paket aus der englischen Basis.</li><li>Sprachpaket oeffnen oeffnet ein Konzept oder ein kompiliertes Paket.</li><li>Konzept speichern speichert die Arbeit ohne Release-Status.</li><li>Sprachpaket kompilieren erstellt die endgueltige .lngpdk-Datei.</li></ul><div class=""warning-sign""><div><b>Ein Konzept ist kein offizielles Paket.</b><br>Nur ein kompiliertes Paket gehoert in Daily oder Release.</div></div>"
            "html-editor.html" = "<p>HTML-Dokumente koennen als Quelle oder im Bearbeitungsmodus geaendert werden.</p><h2>Quelle</h2><p>Quelle zeigt HTML mit Zeilennummern und exakten Tags.</p><h2>Bearbeiten</h2><p>Bearbeiten zeigt die Seite als editierbaren Hilfetext.</p><h2>Bloecke</h2><p>H1, H2, P, Info, Tip, Warn, Code, Kbd, Link und Img fuegen Standardteile ein. Interne Links funktionieren in der Vorschau wie in der echten Hilfe.</p>"
            "media.html" = "<p>Medien ist der Dateimanager fuer Bilder im Hilfesystem.</p><h2>Erlaubt</h2><ul><li>png</li><li>jpg und jpeg</li><li>svg</li></ul><p>Unbekannte Dateien gehoeren nicht in ein Sprachpaket. Die Vorschau nutzt eine volle Flaeche; Sie koennen zoomen und das Bild mit der Maus verschieben.</p>"
            "compile.html" = "<p>Kompilieren Sie erst, wenn der Inhalt vollstaendig ist. ToolEditor prueft Struktur, Links, Bilder, Skripte und Akzente.</p><h2>Qualitaet</h2><ul><li>Alle Pflichtdateien muessen vorhanden sein.</li><li>Interne Links muessen funktionieren.</li><li>Bilder muessen existieren.</li><li>Nur basis.js, main-help.js, nod.js, nod-popup.js und formula.js sind erlaubt.</li><li>SHA-256-Pruefsummen sichern das Paket.</li></ul>"
        }
        fra = @{
            "overview.html" = "<p>ToolEditor est l'espace de travail des paquets de langue Tiedragon. L'arborescence est a gauche, l'editeur au centre et l'apercu a droite.</p><h2>Structure</h2><ul><li>Traduction contient les entrees de langue.</li><li>Aide utilisateur contient les pages du manuel.</li><li>NOD pour developpeurs contient les sujets techniques.</li><li>Carte de formules contient l'aide de calcul.</li><li>Medias et images contient seulement les images autorisees.</li></ul><p>Les onglets fermes restent visibles dans l'arborescence.</p>"
            "package-workflow.html" = "<p>Un paquet de langue commence toujours par un paquet de base. Cela evite les paquets vides et garde la structure fixe.</p><h2>Actions</h2><ul><li>Nouveau paquet cree un paquet depuis la base anglaise.</li><li>Ouvrir paquet de langue ouvre un concept ou un paquet compile.</li><li>Enregistrer concept garde le travail sans statut officiel.</li><li>Compiler paquet de langue cree le fichier .lngpdk final.</li></ul><div class=""warning-sign""><div><b>Un concept n'est pas un paquet officiel.</b><br>Seul un paquet compile appartient a daily ou release.</div></div>"
            "html-editor.html" = "<p>Les documents HTML peuvent etre modifies en mode Source ou Edition.</p><h2>Source</h2><p>Source montre le HTML avec numeros de ligne et balises exactes.</p><h2>Edition</h2><p>Edition affiche la page comme texte d'aide modifiable.</p><h2>Blocs</h2><p>H1, H2, P, Info, Tip, Warn, Code, Kbd, Link et Img ajoutent des elements standard. Les liens internes fonctionnent dans l'apercu comme dans l'aide reelle.</p>"
            "media.html" = "<p>Medias est le gestionnaire de fichiers pour les images de l'aide.</p><h2>Autorise</h2><ul><li>png</li><li>jpg et jpeg</li><li>svg</li></ul><p>Les fichiers inconnus n'ont pas leur place dans un paquet de langue. L'apercu utilise toute la zone; vous pouvez zoomer et deplacer l'image a la souris.</p>"
            "compile.html" = "<p>Compilez seulement quand le contenu est complet. ToolEditor verifie structure, liens, images, scripts et accents.</p><h2>Qualite</h2><ul><li>Tous les fichiers obligatoires doivent exister.</li><li>Les liens internes doivent fonctionner.</li><li>Les images doivent exister.</li><li>Seuls basis.js, main-help.js, nod.js, nod-popup.js et formula.js sont autorises.</li><li>Les sommes SHA-256 protegent le paquet.</li></ul>"
        }
        ind = @{
            "overview.html" = "<p>ToolEditor adalah ruang kerja untuk paket bahasa Tiedragon. Pohon paket ada di kiri, editor aktif di tengah, dan pratinjau di kanan.</p><h2>Struktur</h2><ul><li>Terjemahan berisi entri bahasa.</li><li>Bantuan pengguna berisi halaman manual.</li><li>NOD untuk pengembang berisi topik teknis.</li><li>Kartu formula berisi bantuan perhitungan.</li><li>Media dan gambar hanya berisi gambar yang diizinkan.</li></ul><p>Tab yang ditutup tetap terlihat di pohon.</p>"
            "package-workflow.html" = "<p>Paket bahasa selalu dimulai dari paket dasar. Ini mencegah paket kosong dan menjaga struktur tetap tetap.</p><h2>Aksi</h2><ul><li>Paket baru membuat paket dari basis Inggris.</li><li>Buka paket bahasa membuka konsep atau paket terkompilasi.</li><li>Simpan konsep menyimpan pekerjaan tanpa status resmi.</li><li>Kompilasi paket bahasa membuat file .lngpdk akhir.</li></ul><div class=""warning-sign""><div><b>Konsep bukan paket resmi.</b><br>Hanya paket terkompilasi yang masuk ke daily atau release.</div></div>"
            "html-editor.html" = "<p>Dokumen HTML dapat diedit dalam mode Source atau Edit.</p><h2>Source</h2><p>Source menampilkan HTML dengan nomor baris dan tag yang tepat.</p><h2>Edit</h2><p>Edit menampilkan halaman sebagai teks bantuan yang dapat diedit.</p><h2>Blok</h2><p>H1, H2, P, Info, Tip, Warn, Code, Kbd, Link dan Img menambahkan bagian standar. Link internal bekerja di pratinjau seperti bantuan asli.</p>"
            "media.html" = "<p>Media adalah pengelola file untuk gambar dalam bantuan.</p><h2>Diizinkan</h2><ul><li>png</li><li>jpg dan jpeg</li><li>svg</li></ul><p>File tidak dikenal tidak boleh ada dalam paket bahasa. Pratinjau memakai kanvas penuh; Anda dapat memperbesar dan menggeser gambar dengan mouse.</p>"
            "compile.html" = "<p>Kompilasi hanya saat konten lengkap. ToolEditor memeriksa struktur, link, gambar, skrip dan aksen.</p><h2>Kualitas</h2><ul><li>Semua file wajib harus ada.</li><li>Link internal harus bekerja.</li><li>Gambar harus ada.</li><li>Hanya basis.js, main-help.js, nod.js, nod-popup.js dan formula.js yang diizinkan.</li><li>Checksum SHA-256 melindungi paket.</li></ul>"
        }
        ita = @{
            "overview.html" = "<p>ToolEditor e l'area di lavoro per i pacchetti lingua Tiedragon. L'albero del pacchetto e a sinistra, l'editor al centro e l'anteprima a destra.</p><h2>Struttura</h2><ul><li>Traduzione contiene le voci lingua.</li><li>Aiuto utente contiene le pagine manuale.</li><li>NOD per sviluppatori contiene temi tecnici.</li><li>Scheda formule contiene aiuto di calcolo.</li><li>Media e immagini contiene solo immagini consentite.</li></ul><p>Le schede chiuse restano visibili nell'albero.</p>"
            "package-workflow.html" = "<p>Un pacchetto lingua inizia sempre da un pacchetto base. Cosi non nasce un pacchetto vuoto e la struttura resta fissa.</p><h2>Azioni</h2><ul><li>Nuovo pacchetto crea un pacchetto dalla base inglese.</li><li>Apri pacchetto lingua apre un concetto o un pacchetto compilato.</li><li>Salva concetto conserva il lavoro senza stato ufficiale.</li><li>Compila pacchetto lingua crea il file .lngpdk finale.</li></ul><div class=""warning-sign""><div><b>Un concetto non e un pacchetto ufficiale.</b><br>Solo un pacchetto compilato appartiene a daily o release.</div></div>"
            "html-editor.html" = "<p>I documenti HTML possono essere modificati in Source o Edit.</p><h2>Source</h2><p>Source mostra HTML con numeri di riga e tag esatti.</p><h2>Edit</h2><p>Edit mostra la pagina come testo guida modificabile.</p><h2>Blocchi</h2><p>H1, H2, P, Info, Tip, Warn, Code, Kbd, Link e Img aggiungono parti standard. I link interni funzionano nell'anteprima come nella guida reale.</p>"
            "media.html" = "<p>Media e il file manager per immagini nella guida.</p><h2>Consentiti</h2><ul><li>png</li><li>jpg e jpeg</li><li>svg</li></ul><p>File sconosciuti non appartengono a un pacchetto lingua. L'anteprima usa tutta l'area; puoi zoomare e trascinare l'immagine con il mouse.</p>"
            "compile.html" = "<p>Compila solo quando il contenuto e completo. ToolEditor controlla struttura, link, immagini, script e accenti.</p><h2>Qualita</h2><ul><li>Tutti i file obbligatori devono esistere.</li><li>I link interni devono funzionare.</li><li>Le immagini devono esistere.</li><li>Sono consentiti solo basis.js, main-help.js, nod.js, nod-popup.js e formula.js.</li><li>I checksum SHA-256 proteggono il pacchetto.</li></ul>"
        }
        por = @{
            "overview.html" = "<p>ToolEditor e o espaco de trabalho para pacotes de idioma Tiedragon. A arvore do pacote fica a esquerda, o editor no centro e a visualizacao a direita.</p><h2>Estrutura</h2><ul><li>Traducao contem as entradas de idioma.</li><li>Ajuda do usuario contem paginas do manual.</li><li>NOD para desenvolvedores contem topicos tecnicos.</li><li>Cartao de formulas contem ajuda de calculo.</li><li>Midia e imagens contem apenas imagens permitidas.</li></ul><p>Guias fechadas continuam visiveis na arvore.</p>"
            "package-workflow.html" = "<p>Um pacote de idioma sempre comeca a partir de um pacote base. Isso evita pacotes vazios e mantem a estrutura fixa.</p><h2>Acoes</h2><ul><li>Novo pacote cria um pacote a partir da base inglesa.</li><li>Abrir pacote de idioma abre um conceito ou pacote compilado.</li><li>Salvar conceito guarda o trabalho sem status oficial.</li><li>Compilar pacote de idioma cria o arquivo .lngpdk final.</li></ul><div class=""warning-sign""><div><b>Um conceito nao e pacote oficial.</b><br>Somente pacote compilado pertence ao daily ou release.</div></div>"
            "html-editor.html" = "<p>Documentos HTML podem ser editados em Source ou Edit.</p><h2>Source</h2><p>Source mostra HTML com numeros de linha e tags exatas.</p><h2>Edit</h2><p>Edit mostra a pagina como texto de ajuda editavel.</p><h2>Blocos</h2><p>H1, H2, P, Info, Tip, Warn, Code, Kbd, Link e Img adicionam partes padrao. Links internos funcionam na visualizacao como na ajuda real.</p>"
            "media.html" = "<p>Midia e o gerenciador de arquivos para imagens na ajuda.</p><h2>Permitidos</h2><ul><li>png</li><li>jpg e jpeg</li><li>svg</li></ul><p>Arquivos desconhecidos nao pertencem a um pacote de idioma. A visualizacao usa a area inteira; voce pode ampliar e arrastar a imagem com o mouse.</p>"
            "compile.html" = "<p>Compile somente quando o conteudo estiver completo. ToolEditor verifica estrutura, links, imagens, scripts e acentos.</p><h2>Qualidade</h2><ul><li>Todos os arquivos obrigatorios devem existir.</li><li>Links internos devem funcionar.</li><li>Imagens devem existir.</li><li>Apenas basis.js, main-help.js, nod.js, nod-popup.js e formula.js sao permitidos.</li><li>Checksums SHA-256 protegem o pacote.</li></ul>"
        }
        spa = @{
            "overview.html" = "<p>ToolEditor es el espacio de trabajo para paquetes de idioma Tiedragon. El arbol del paquete esta a la izquierda, el editor en el centro y la vista previa a la derecha.</p><h2>Estructura</h2><ul><li>Traduccion contiene las entradas de idioma.</li><li>Ayuda de usuario contiene paginas del manual.</li><li>NOD para desarrolladores contiene temas tecnicos.</li><li>Tarjeta de formulas contiene ayuda de calculo.</li><li>Medios e imagenes contiene solo imagenes permitidas.</li></ul><p>Las pestanas cerradas siguen visibles en el arbol.</p>"
            "package-workflow.html" = "<p>Un paquete de idioma siempre empieza desde un paquete base. Esto evita paquetes vacios y mantiene la estructura fija.</p><h2>Acciones</h2><ul><li>Nuevo paquete crea un paquete desde la base inglesa.</li><li>Abrir paquete de idioma abre un concepto o paquete compilado.</li><li>Guardar concepto conserva el trabajo sin estado oficial.</li><li>Compilar paquete de idioma crea el archivo .lngpdk final.</li></ul><div class=""warning-sign""><div><b>Un concepto no es un paquete oficial.</b><br>Solo un paquete compilado pertenece a daily o release.</div></div>"
            "html-editor.html" = "<p>Los documentos HTML pueden editarse en Source o Edit.</p><h2>Source</h2><p>Source muestra HTML con numeros de linea y etiquetas exactas.</p><h2>Edit</h2><p>Edit muestra la pagina como texto de ayuda editable.</p><h2>Bloques</h2><p>H1, H2, P, Info, Tip, Warn, Code, Kbd, Link e Img agregan partes estandar. Los enlaces internos funcionan en la vista previa como en la ayuda real.</p>"
            "media.html" = "<p>Medios es el gestor de archivos para imagenes de la ayuda.</p><h2>Permitidos</h2><ul><li>png</li><li>jpg y jpeg</li><li>svg</li></ul><p>Los archivos desconocidos no pertenecen a un paquete de idioma. La vista previa usa toda el area; puedes hacer zoom y arrastrar la imagen con el raton.</p>"
            "compile.html" = "<p>Compila solo cuando el contenido este completo. ToolEditor comprueba estructura, enlaces, imagenes, scripts y acentos.</p><h2>Calidad</h2><ul><li>Todos los archivos obligatorios deben existir.</li><li>Los enlaces internos deben funcionar.</li><li>Las imagenes deben existir.</li><li>Solo se permiten basis.js, main-help.js, nod.js, nod-popup.js y formula.js.</li><li>Las sumas SHA-256 protegen el paquete.</li></ul>"
        }
        zho = @{
            "overview.html" = "<p>ToolEditor 是 Tiedragon 语言包的工作区。左侧是包树，中间是编辑器，右侧是预览。</p><h2>结构</h2><ul><li>翻译包含语言条目。</li><li>用户帮助包含普通手册页面。</li><li>开发者 NOD 包含技术主题。</li><li>公式卡包含计算和公式帮助。</li><li>媒体和图片只包含允许的图片。</li></ul><p>关闭的标签页仍保留在树中，方便继续浏览结构。</p>"
            "package-workflow.html" = "<p>语言包始终从基础包开始。这样不会产生空包，并且固定结构保持一致。</p><h2>操作</h2><ul><li>新建包从英文基础结构创建包。</li><li>打开语言包打开概念包或已编译包。</li><li>保存概念语言包保存编辑工作，但不是正式发布。</li><li>编译语言包创建最终 .lngpdk 文件。</li></ul><div class=""warning-sign""><div><b>概念包不是正式包。</b><br>只有已编译包才属于 daily 或 release。</div></div>"
            "html-editor.html" = "<p>HTML 文档可以在 Source 或 Edit 模式中编辑。</p><h2>Source</h2><p>Source 显示带行号和精确标签的 HTML。</p><h2>Edit</h2><p>Edit 将页面显示为可编辑的帮助文本。</p><h2>块</h2><p>H1、H2、P、Info、Tip、Warn、Code、Kbd、Link 和 Img 会插入标准部分。内部链接在预览中像真实帮助一样工作。</p>"
            "media.html" = "<p>媒体是帮助图片的文件管理器。</p><h2>允许</h2><ul><li>png</li><li>jpg 和 jpeg</li><li>svg</li></ul><p>未知文件不属于语言包。预览使用完整画布；你可以缩放并用鼠标拖动图片。</p>"
            "compile.html" = "<p>只有内容完整后才编译。ToolEditor 会检查结构、链接、图片、脚本和编码。</p><h2>质量</h2><ul><li>所有必需文件必须存在。</li><li>内部链接必须工作。</li><li>图片必须存在。</li><li>只允许 basis.js、main-help.js、nod.js、nod-popup.js 和 formula.js。</li><li>SHA-256 校验保护包。</li></ul>"
        }
    }

    if ($localizedPages.ContainsKey($LanguageCode)) {
        foreach ($page in $localizedPages[$LanguageCode].GetEnumerator()) {
            Write-Utf8NoBomText `
                -Path (Join-Path $toolEditorRoot $page.Key) `
                -Text ($page.Value.Trim() + [Environment]::NewLine)
        }

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
  <b>Scripts:</b> only basis.js, main-help.js, nod.js, nod-popup.js and formula.js are allowed in the base package.
</div>
"@
    }

    foreach ($page in $pages.GetEnumerator()) {
        Write-Utf8NoBomText `
            -Path (Join-Path $toolEditorRoot $page.Key) `
            -Text ($page.Value.Trim() + [Environment]::NewLine)
    }
}

New-Item -ItemType Directory -Path $outputRoot, $conceptRoot, $objectPackageRoot -Force | Out-Null

$results = @()
$languageFiles = Get-ChildItem $languageRoot -Filter "*.lng" -File | Sort-Object Name
$englishLanguagePath = Join-Path $languageRoot "eng.lng"
if (-not (Test-Path $englishLanguagePath)) {
    throw "English base language file not found: $englishLanguagePath"
}

$englishLanguageMap = Read-LanguageMap -Path $englishLanguagePath
$requiredLanguageKeys = $englishLanguageMap.Keys |
    Where-Object { -not $_.StartsWith("formula_card.", [System.StringComparison]::OrdinalIgnoreCase) } |
    Sort-Object
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
    Remove-AuthoringHelpContent -TargetRoot $concept
    Set-GeneratedHelpContentFromLanguageMap -TargetRoot $concept -LanguageMap $languageMap
    Set-ToolEditorHelpContent -TargetRoot $concept -LanguageCode $code
    dotnet run --project $project --no-restore -- export-formula-cards (Join-Path $concept "formula") $languageFile.FullName | Out-Host
    Add-RequiredHelpMedia -TargetRoot $concept
    Expand-AllPackageTextTokens -TargetRoot $concept -LanguageMap $languageMap

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
    Write-Utf8NoBomText `
        -Path (Join-Path $concept "manifest.json") `
        -Text (($manifest | ConvertTo-Json -Depth 5) + [Environment]::NewLine)

    $objectPackage = Join-Path $objectPackageRoot ("Syscalculator.Language." + $code + ".objpdk")
    $objectPackageZip = $objectPackage + ".zip"
    if (Test-Path $objectPackage) {
        Remove-Item $objectPackage -Force
    }

    if (Test-Path $objectPackageZip) {
        Remove-Item $objectPackageZip -Force
    }

    Compress-Archive -Path (Join-Path $concept "*") -DestinationPath $objectPackageZip -Force
    Move-Item -Path $objectPackageZip -Destination $objectPackage -Force

    $outputPackage = Join-Path $outputRoot ("Syscalculator.Language." + $code + ".lngpdk")
    if ($signingEnabled) {
        $json = dotnet run --project $project --no-restore -- agent-compile-signed $objectPackage $outputPackage $signingPrivateKeyPath $SigningKeyId
    } else {
        $json = dotnet run --project $project --no-restore -- agent-compile $objectPackage $outputPackage
    }
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
$indexJson = $results |
    Select-Object packageKey, languageCode, displayName, packageSha256, payloadSha256, entryCount,
        signed, signatureAlgorithm, signatureKeyId,
        @{ Name = "fileName"; Expression = { [System.IO.Path]::GetFileName($_.outputPath) } },
        @{ Name = "downloadPath"; Expression = { "packages/languages/" + [System.IO.Path]::GetFileName($_.outputPath) } },
        active, translationComplete, missingRequiredKeyCount, missingRequiredKeys |
    ConvertTo-Json -Depth 5
Write-Utf8NoBomText -Path $manifestPath -Text ($indexJson + [Environment]::NewLine)

Write-Host "Generated $($results.Count) language packages in $outputRoot"
Write-Host "Manifest: $manifestPath"
