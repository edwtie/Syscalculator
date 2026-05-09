param(
    [switch]$Compile,
    [string]$Vb6Path = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$repoRoot = Split-Path -Parent (Split-Path -Parent $projectDir)
$compileScript = Join-Path $scriptDir "CompileSyscalculator174Candidate.ps1"

$script:Passed = 0
$script:Failed = 0

function Test-Case {
    param(
        [string]$Name,
        [scriptblock]$Body
    )

    try {
        & $Body
        $script:Passed++
        Write-Host "[PASS] $Name"
    }
    catch {
        $script:Failed++
        Write-Host "[FAIL] $Name"
        Write-Host "       $($_.Exception.Message)"
    }
}

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Read-Text {
    param([string]$Path)
    return Get-Content -LiteralPath $Path -Raw
}

function Resolve-NodPath {
    param([string]$RelativePath)

    $candidate = Join-Path $projectDir $RelativePath
    if ([IO.Path]::GetExtension($candidate) -eq "") {
        $candidate = "$candidate.nod"
    }

    return $candidate
}

function Test-Catalog {
    param([string]$FileName)

    $cfgPath = Join-Path $projectDir $FileName
    Assert-True (Test-Path -LiteralPath $cfgPath) "$FileName is missing."

    $rows = Get-Content -LiteralPath $cfgPath | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ConvertFrom-Csv -Header Name,Path,Default
    Assert-True (@($rows | Where-Object { $_.Name -eq "[lang]" }).Count -eq 1) "$FileName has no [lang] row."
    Assert-True (@($rows | Where-Object { $_.Name -eq "Euro-NLG" }).Count -eq 1) "$FileName has no Euro-NLG row."
    Assert-True (@($rows | Where-Object { $_.Name -eq "BGN" }).Count -eq 1) "$FileName has no BGN row."
    Assert-True (@($rows | Where-Object { $_.Name -eq "HRK" }).Count -eq 1) "$FileName has no HRK row."

    foreach ($row in $rows) {
        if ($row.Name -eq "[lang]") {
            $langPath = Join-Path $projectDir $row.Path
            Assert-True (Test-Path -LiteralPath $langPath) "$FileName points to missing language file: $($row.Path)"
            continue
        }

        $nodPath = Resolve-NodPath $row.Path
        Assert-True (Test-Path -LiteralPath $nodPath) "$FileName points to missing NOD file: $($row.Path)"
    }
}

Write-Host "Testing Syscalculator 1.74 legacy VB6 source..."
Write-Host "Project dir: $projectDir"

Test-Case "Required VB6 source files exist" {
    $required = @(
        "Project1.vbp",
        "Form1.frm",
        "Freesyscal.bas",
        "Languare.bas",
        "Languarex.bas",
        "WizardExpress.frm",
        "Text\operatie_decibel_1995.nod",
        "euro\BGN.nod",
        "euro\CYP.nod",
        "euro\EEK.nod",
        "euro\HRK.nod",
        "euro\LTL.nod",
        "euro\LVL.nod",
        "euro\MTL.nod",
        "euro\SIT.nod",
        "euro\SKK.nod",
        "freesyscal.cfg",
        "freesysc.cfg"
    )

    foreach ($file in $required) {
        $path = Join-Path $projectDir $file
        Assert-True (Test-Path -LiteralPath $path) "Missing required file: $file"
    }
}

Test-Case "Project1.vbp uses 1.74 candidate settings" {
    $project = Read-Text (Join-Path $projectDir "Project1.vbp")

    Assert-True ($project -match "Form=WizardExpress\.frm") "WizardExpress.frm is not included."
    Assert-True ($project -match "Module=Module1; Freesyscal\.bas") "Freesyscal.bas module is not included."
    Assert-True ($project -match "MinorVer=74") "MinorVer is not 74."
    Assert-True ($project -match 'Command32="/lng eng\.lng"') "Default command line language is not eng.lng."
    Assert-True ($project -match "stdole2\.tlb") "OLE Automation reference does not point to stdole2.tlb."
}

Test-Case "Project1.vbp has no old source-drive references" {
    $project = Read-Text (Join-Path $projectDir "Project1.vbp")

    Assert-True ($project -notmatch "(?i)x:\\broncode") "Project still references x:\broncode."
    Assert-True ($project -notmatch "(?i)c:\\broncode") "Project still references C:\broncode."
    Assert-True ($project -notmatch "(?i)c:\\documents and settings") "Project still references old Documents and Settings paths."
}

Test-Case "VB6 source branding uses Tiedragon instead of TCsoftware" {
    $extensions = @("*.bas", "*.cls", "*.frm", "*.vbp", "*.iss", "*.lng", "*.lrg", "*.cfg", "*.xml", "*.htm", "*.txt")
    $files = foreach ($extension in $extensions) {
        Get-ChildItem -LiteralPath $projectDir -Recurse -File -Filter $extension |
            Where-Object { $_.FullName -notmatch "\\Output\\" }
    }

    foreach ($file in $files) {
        $text = Read-Text $file.FullName
        $relative = $file.FullName.Substring($projectDir.Length + 1)

        Assert-True ($text -notmatch "(?i)tc\s*software|tcsoftware|tc-software|tc_software|www\.tcsoftware\.com|software\\tcsoftware") "$relative still contains old TCsoftware branding."
    }
}

Test-Case "Legacy converter catalogs resolve to existing files" {
    Test-Catalog "freesyscal.cfg"
    Test-Catalog "freesysc.cfg"
}

Test-Case "Legacy help and metadata identify Syscalculator 1.74" {
    $helpEn = Read-Text (Join-Path $projectDir "help\index_en.htm")
    $helpNl = Read-Text (Join-Path $projectDir "help\index_nl.htm")
    $helpEs = Read-Text (Join-Path $projectDir "help\index_es.htm")
    $helpCat = Read-Text (Join-Path $projectDir "help\index_cat.htm")
    $classicHelp = Read-Text (Join-Path $projectDir "help\index.htm")
    $about = Read-Text (Join-Path $projectDir "frmAbout.frm")
    $readme = Read-Text (Join-Path $projectDir "readme.txt")
    $pad = Read-Text (Join-Path $projectDir "pad_file.xml")
    $installer = Read-Text (Join-Path $projectDir "syscalculcator.iss")

    Assert-True ($helpEn.Contains("Syscalculator 1.74")) "English help still has the wrong version."
    Assert-True ($helpNl.Contains("Syscalculator 1.74")) "Dutch help still has the wrong version."
    Assert-True ($helpEs.Contains("Syscalculator 1.74")) "Spanish help still has the wrong version."
    Assert-True ($helpCat.Contains("Syscalculator 1.74")) "Catalan help still has the wrong version."
    Assert-True ($readme.Contains("Syscalculator  1.74 RC1")) "readme.txt still has the wrong version."
    Assert-True ($readme.Contains("What's new in 1.74 RC1?")) "readme.txt has no 1.74 RC1 changelog."
    Assert-True ($readme.Contains("After 20 years")) "readme.txt has no 20-year release note."
    Assert-True ($readme.Contains("finally released")) "readme.txt does not announce the 1.74 release."
    Assert-True ($readme.Contains("tested this 1.74 release on Windows 11")) "readme.txt does not mention Windows 11 testing."
    Assert-True ($readme.Contains("final Syscalculator 1.x legacy release")) "readme.txt does not announce the final 1.x legacy release."
    Assert-True ($readme.Contains("new Syscalculator 2.0 line")) "readme.txt does not mention the new Syscalculator 2.0 line."
    Assert-True ($readme.Contains("outdated VB6 runtime")) "readme.txt does not warn that the VB6 runtime is outdated."
    Assert-True ($readme.Contains("reduce the risks")) "readme.txt does not explain why Syscalculator 2.0 is being developed."
    Assert-True ($readme.Contains("new features that the old 1.7x line does not have")) "readme.txt does not announce new 2.0 features beyond 1.7x."
    Assert-True ($readme.Contains("recent euro adoptions")) "readme.txt does not mention recent euro adoptions."
    Assert-True ($readme.Contains("Croatia 2023")) "readme.txt does not mention Croatia 2023."
    Assert-True ($readme.Contains("Bulgaria 2026")) "readme.txt does not mention Bulgaria 2026."
    Assert-True ($readme.Contains("Windows 7 and later")) "readme.txt does not mention the Windows 7+ startup/configuration fix."
    Assert-True ($readme.Contains("Visual Basic 6 Runtime is required")) "readme.txt does not clearly mention the required VB6 runtime."
    Assert-True ($pad.Contains("<Program_Version>1.74</Program_Version>")) "PAD metadata still has the wrong version."
    Assert-True ($pad.Contains("<Program_Release_Year>2026</Program_Release_Year>")) "PAD metadata still has the wrong release year."
    Assert-True ($pad.Contains("<Char_Desc_2000>") -and $pad.Contains("</Char_Desc_2000>") -and $pad -notmatch "<Char_Desc_2000\s*/>") "PAD metadata has no long description."
    Assert-True ($pad.Contains("final Syscalculator 1.x legacy release")) "PAD long description does not mention the final 1.x legacy release."
    Assert-True ($pad.Contains("Windows 11")) "PAD long description does not mention Windows 11 testing."
    Assert-True ($pad.Contains("VB6 Runtime") -or $pad.Contains("Visual Basic 6 Runtime")) "PAD long description does not mention the VB6 runtime."
    Assert-True ($pad.Contains("Syscalculator 2.0")) "PAD long description does not mention the Syscalculator 2.0 successor."
    Assert-True ($about.Contains("Copyright (C) 1996-2026")) "About form design-time copyright is not 1996-2026."
    Assert-True ($about.Contains('"Copyright') -and $about.Contains("1996-2026")) "About form runtime copyright is not 1996-2026."
    Assert-True ($helpEn.Contains("Copyright (c) 1996-2026")) "English help copyright is not 1996-2026."
    Assert-True ($helpNl.Contains("Copyright (c) 1996-2026")) "Dutch help copyright is not 1996-2026."
    Assert-True ($helpEs.Contains("Copyright (c) 1996-2026")) "Spanish help copyright is not 1996-2026."
    Assert-True ($helpCat.Contains("Copyright (c) 1996-2026")) "Catalan help copyright is not 1996-2026."
    Assert-True ($classicHelp.Contains("Copyright (c) 1996-2026")) "Classic help copyright is not 1996-2026."
    Assert-True ($installer.Contains("AppVerName=Syscalculator 1.74")) "Historical Inno script still has the wrong version."
    Assert-True ($helpEn -notmatch "1\.72" -and $helpNl -notmatch "1\.72" -and $readme -notmatch "1\.72" -and $pad -notmatch "1\.72") "Legacy help/readme/PAD still references 1.72."
}

Test-Case "Legacy switch menu describes reverse direction" {
    $english = Read-Text (Join-Path $projectDir "eng.lng")
    $dutch = [Text.Encoding]::Unicode.GetString([IO.File]::ReadAllBytes((Join-Path $projectDir "ned.lng")))
    $form = Read-Text (Join-Path $projectDir "Form1.frm")

    Assert-True ($english.Contains('1,205,"Reverse direction",""')) "English menu still uses the unclear Switch label."
    Assert-True ($dutch.Contains('1,205,"Richting omkeren",')) "Dutch menu still uses the unclear Overschakelen label."
    Assert-True ($form.Contains("tempText = Text1.Text")) "Switch menu does not keep the input value before swapping fields."
    Assert-True ($form.Contains("Text1.Text = Text2.Text")) "Switch menu does not move input 2 into input 1."
    Assert-True ($form.Contains("Text2.Text = tempText")) "Switch menu does not move input 1 into input 2."
}

Test-Case "Legacy startup menu describes Windows startup" {
    $english = Read-Text (Join-Path $projectDir "eng.lng")
    $dutch = [Text.Encoding]::Unicode.GetString([IO.File]::ReadAllBytes((Join-Path $projectDir "ned.lng")))
    $form = Read-Text (Join-Path $projectDir "Form1.frm")

    Assert-True ($english.Contains('1,204,"Start with Windows",""')) "English menu still uses the unclear Startup label."
    Assert-True ($dutch.Contains('1,204,"Starten met Windows",')) "Dutch menu still uses the unclear Start label."
    Assert-True ($form.Contains('SOFTWARE\Microsoft\Windows\CurrentVersion\Run')) "Startup menu does not write the Windows Run key."
    Assert-True ($form.Contains('Freesyscal.exe /tray')) "Startup menu does not start Syscalculator hidden in the tray."
}

Test-Case "Modern 1.74 installer checks for the VB6 runtime" {
    $installer = Read-Text (Join-Path $repoRoot "installer\Syscalculator174.iss")
    $installerDoc = Read-Text (Join-Path $repoRoot "docs\INNO_SETUP_INSTALLER.md")

    Assert-True ($installer.Contains("function IsVb6RuntimeInstalled")) "Modern installer has no VB6 runtime check function."
    Assert-True ($installer.Contains("msvbvm60.dll")) "Modern installer does not check for msvbvm60.dll."
    Assert-True ($installer.Contains("function InitializeSetup")) "Modern installer does not run the VB6 runtime check before setup."
    Assert-True ($installer.Contains("Vb6RuntimeMissing")) "Modern installer has no VB6 runtime missing message."
    Assert-True ($installer.Contains("ShellExec('open'")) "Modern installer does not offer to open the VB6 runtime download page."
    Assert-True ($installer.Contains("support.microsoft.com")) "Modern installer does not use an official Microsoft VB6 runtime download page."
    Assert-True ($installerDoc.Contains("installer checks for the Visual Basic 6 Runtime")) "Installer documentation does not describe the VB6 runtime check."
    Assert-True ($installerDoc.Contains("support.microsoft.com")) "Installer documentation does not include the Microsoft VB6 runtime download page."
}

Test-Case "WizardExpress help documents the Office 365 limitation" {
    $helpEn = Read-Text (Join-Path $projectDir "help\index_en.htm")
    $helpNl = Read-Text (Join-Path $projectDir "help\index_nl.htm")
    $helpEs = Read-Text (Join-Path $projectDir "help\index_es.htm")
    $helpCat = Read-Text (Join-Path $projectDir "help\index_cat.htm")
    $classicHelp = Read-Text (Join-Path $projectDir "help\index.htm")
    $readme = Read-Text (Join-Path $projectDir "readme.txt")
    $pad = Read-Text (Join-Path $projectDir "pad_file.xml")

    Assert-True ($helpEn.Contains("older Office and Word versions")) "English help does not say WizardExpress is for older Office/Word versions."
    Assert-True ($helpNl.Contains("oudere Office- en Word-versies")) "Dutch help does not say WizardExpress is for older Office/Word versions."
    Assert-True ($helpEn.Contains("LibreOffice has been tested and works ok")) "English help does not document the LibreOffice test result."
    Assert-True ($helpNl.Contains("LibreOffice is getest en werkt ok")) "Dutch help does not document the LibreOffice test result."
    Assert-True ($helpEs.Contains("Microsoft 365 / Office 365")) "Spanish help has no Microsoft 365 / Office 365 warning."
    Assert-True ($helpCat.Contains("Microsoft 365 / Office 365")) "Catalan help has no Microsoft 365 / Office 365 warning."
    Assert-True ($classicHelp.Contains("Microsoft 365 / Office 365")) "Classic help has no Microsoft 365 / Office 365 warning."
    Assert-True ($classicHelp.Contains("LibreOffice has been tested and works ok")) "Classic help does not document the LibreOffice test result."
    Assert-True ($readme.Contains("Microsoft 365 / Office 365")) "readme.txt has no Microsoft 365 / Office 365 warning."
    Assert-True ($readme.Contains("LibreOffice has been tested and works ok")) "readme.txt does not document the LibreOffice test result."
    Assert-True ($pad.Contains("Microsoft 365 / Office 365 is not supported reliably")) "PAD metadata does not describe the Office 365 limitation."
    Assert-True ($pad.Contains("LibreOffice has been tested and works ok")) "PAD metadata does not document the LibreOffice test result."
}

Test-Case "New euro adopters are included in the legacy catalog" {
    $newEuroCodes = @("SIT", "CYP", "MTL", "SKK", "EEK", "LVL", "LTL", "HRK", "BGN")
    $readme = Read-Text (Join-Path $projectDir "readme.txt")
    $catalog = Read-Text (Join-Path $projectDir "Freesyscal.bas")
    $form = Read-Text (Join-Path $projectDir "Form1.frm")

    foreach ($code in $newEuroCodes) {
        Assert-True (Test-Path -LiteralPath (Join-Path $projectDir "euro\$code.nod")) "Missing euro converter: $code"
        Assert-True ($readme.Contains("${code}:")) "readme.txt does not list $code."
        Assert-True ($catalog.Contains("`"euro\$code`"")) "Built-in catalog does not include $code."
        Assert-True ($form.Contains("`"euro\$code`"")) "First-run config does not write $code."
    }
}

Test-Case "First-run config writes the legacy catalog entries" {
    $form = Read-Text (Join-Path $projectDir "Form1.frm")

    Assert-True ($form.Contains('Write #1, "Euro-NLG", "euro\NLG", "*"')) "First-run config does not write the Euro-NLG default."
    Assert-True ($form.Contains('Write #1, "Operatie Decibel 1995", "Text\operatie_decibel_1995", ""')) "First-run config does not write Operatie Decibel."
    Assert-True ($form.Contains('Write #1, "ATS", "euro\ATS", ""')) "First-run config does not write the euro catalog."
    Assert-True ($form.Contains('Write #1, "BGN", "euro\BGN", ""')) "First-run config does not write the newer euro catalog."
}

Test-Case "Startup copies default config and falls back safely" {
    $form = Read-Text (Join-Path $projectDir "Form1.frm")
    $catalog = Read-Text (Join-Path $projectDir "Freesyscal.bas")
    $language = Read-Text (Join-Path $projectDir "Languare.bas")
    $languageEditor = Read-Text (Join-Path $projectDir "Languarex.bas")
    $windowsApi = Read-Text (Join-Path $projectDir "WindowsAPI.bas")
    $editor = Read-Text (Join-Path $projectDir "Zeditor.frm")

    Assert-True ($form.Contains('If Trim$(Apppaths) = "" Then Apppaths = App.Path')) "Form1 can still continue with an empty AppData path."
    Assert-True ($editor.Contains('If Trim$(Apppaths) = "" Then Apppaths = App.Path')) "Editor can still continue with an empty AppData path."
    Assert-True ($windowsApi.Contains('PathName = Environ$("APPDATA")')) "GetDataFolder does not fall back to APPDATA."
    Assert-True ($windowsApi.Contains('If Trim$(PhysicalPath) = "" Then Exit Sub')) "MakeDirectory can still try to create an empty path."
    Assert-True ($windowsApi.Contains("runtime error 52")) "Windows 7+ runtime error 52 guard is not documented in WindowsAPI.bas."
    Assert-True ($windowsApi -notmatch "Err\.Number = 453") "GetDataFolder still only handles the old missing-API fallback."
    Assert-True ($form.Contains('FileCopy App.Path + "\freesyscal.cfg", Apppaths + "\freesyscal.cfg"')) "Form1 does not copy default config to AppData."
    Assert-True ($catalog.Contains("Resume OpenConfig")) "Addcombo does not fall back from AppData config to install config."
    Assert-True ($catalog.Contains("Function LoadDefaultComboCatalog")) "Addcombo has no built-in catalog fallback."
    Assert-True ($catalog.Contains('Form1.Combo1.AddItem "Euro-NLG", 11')) "Built-in catalog fallback does not include Euro-NLG."
    Assert-True ($catalog.Contains('Form1.Combo1.AddItem "BGN", 12')) "Built-in catalog fallback does not include BGN."
    Assert-True ($language.Contains('ename = App.Path + "\eng.lng"')) "Languare.bas has no eng.lng fallback."
    Assert-True ($languageEditor.Contains('ename = App.Path + "\eng.lng"')) "Languarex.bas has no eng.lng fallback."
}

Test-Case "Language files have terminators" {
    $languages = @("eng.lng", "ned.lng", "esp.lng", "cat.lng")

    foreach ($language in $languages) {
        $path = Join-Path $projectDir $language
        Assert-True (Test-Path -LiteralPath $path) "Missing language file: $language"
        $text = Read-Text $path
        Assert-True ($text -match "(?m)^end,") "$language has no end terminator."
    }
}

Test-Case "Startup config delete is protected" {
    $form = Read-Text (Join-Path $projectDir "Form1.frm")
    $guard = 'If Dir$(App.Path + "\freesyscal.cfg") <> "" Then Kill App.Path + "\freesyscal.cfg"'
    Assert-True ($form.Contains($guard)) "Form1 can still delete freesyscal.cfg without checking it exists."
}

Test-Case "Combo index restore accepts valid zero-based indexes" {
    $module = Read-Text (Join-Path $projectDir "Freesyscal.bas")
    Assert-True ($module -match "old <= max And old > -1") "Freesyscal.bas does not contain the fixed combo index bounds check."
}

Test-Case "Language loader traps open errors" {
    $module = Read-Text (Join-Path $projectDir "Languarex.bas")
    $onErrorIndex = $module.IndexOf("On Error GoTo resfout")
    $openIndex = $module.IndexOf("Open ename For Input As #1")

    Assert-True ($onErrorIndex -ge 0) "Languarex.bas has no resfout error handler."
    Assert-True ($openIndex -ge 0) "Languarex.bas does not open the language file."
    Assert-True ($onErrorIndex -lt $openIndex) "Languarex.bas installs the error handler after opening the file."
}

Test-Case "Options OK applies configuration changes immediately" {
    $optionsForm = Read-Text (Join-Path $projectDir "Form3.frm")
    Assert-True ($optionsForm.Contains("Private Sub ApplyOptions()")) "Form3 has no shared ApplyOptions routine."
    Assert-True ($optionsForm.Contains("If old = -1 Then old = max: Call ApplyOptions: Form1.Show: Unload Me: Exit Sub")) "Options OK does not apply first configuration changes."
    Assert-True ($optionsForm -match "If Form1\.Visible = False Then Call form1cleanup: Form1\.Show\s+Call ApplyOptions\s+Unload Me") "Options OK does not apply changes before closing."
    Assert-True ($optionsForm.Contains("Call Languare(Lname, 1)")) "Options apply path does not refresh the main form language."
}

Test-Case "Legacy chg prefix replacement keeps the full suffix" {
    $mainModule = Read-Text (Join-Path $projectDir "Module1.bas")
    $editorModule = Read-Text (Join-Path $projectDir "EditorMod1.bas")

    Assert-True ($mainModule.Contains("ask = chgn(o) + Mid(ask, b + 1)")) "Module1 forward chg still drops or duplicates part of the suffix."
    Assert-True ($mainModule.Contains("ask = chgo(o) + Mid(ask, b + 1)")) "Module1 reverse chg still drops or duplicates part of the suffix."
    Assert-True ($editorModule.Contains('ask = chgn(o) + Mid$(ask, b + 1)')) "EditorMod1 forward chg still drops or duplicates part of the suffix."
    Assert-True ($editorModule.Contains('ask = chgo(o) + Mid$(ask, b + 1)')) "EditorMod1 reverse chg still drops or duplicates part of the suffix."
}

Test-Case "Operatie Decibel is VB6 1.74 compatible" {
    $decibelPath = Join-Path $projectDir "Text\operatie_decibel_1995.nod"
    $decibel = Read-Text $decibelPath

    Assert-True ($decibel -match "(?m)^Name Operatie Decibel 1995") "Operatie Decibel NOD has the wrong name."
    Assert-True ($decibel.Contains("chg 01751-,070-51")) "Operatie Decibel NOD is missing a known 01751 -> 070-51 rule."
    Assert-True ($decibel -notmatch "(?m)^chg `"") "Operatie Decibel NOD uses quoted chg rules, which old VB6 chg does not strip."
}

Test-Case "Compile script validates the VB6 project path" {
    & $compileScript -SkipCompile | Out-Host
}

if ($Compile) {
    Test-Case "VB6 command line compile succeeds" {
        if ([string]::IsNullOrWhiteSpace($Vb6Path)) {
            & $compileScript | Out-Host
        }
        else {
            & $compileScript -Vb6Path $Vb6Path | Out-Host
        }
    }
}
else {
    Write-Host "[SKIP] VB6 command line compile (pass -Compile to enable)"
}

Write-Host ""
Write-Host "Legacy VB6 tests: $script:Passed passed, $script:Failed failed."

if ($script:Failed -gt 0) {
    exit 1
}

exit 0
