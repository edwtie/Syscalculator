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

function Read-LegacyText {
    param([string]$Path)

    $bytes = [IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -ge 2 -and $bytes[0] -eq 255 -and $bytes[1] -eq 254) {
        return [Text.Encoding]::Unicode.GetString($bytes)
    }

    return [Text.Encoding]::GetEncoding(1252).GetString($bytes)
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
    Assert-True ($readme.Contains("Syscalculator  1.74 RC2")) "readme.txt still has the wrong version."
    Assert-True ($readme.Contains("What's new in 1.74 RC2?")) "readme.txt has no 1.74 RC2 changelog."
    Assert-True ($readme.Contains("After 20 years")) "readme.txt has no 20-year release note."
    Assert-True ($readme.Contains("finally released")) "readme.txt does not announce the 1.74 release."
    Assert-True ($readme.Contains("tested this 1.74 release on Windows 11")) "readme.txt does not mention Windows 11 testing."
    Assert-True ($readme.Contains("final Syscalculator 1.x legacy release")) "readme.txt does not announce the final 1.x legacy release."
    Assert-True ($readme.Contains("VB6 runtime is outdated")) "readme.txt does not warn that the VB6 runtime is outdated."
    Assert-True ($readme.Contains("classic NOD converter workflow")) "readme.txt does not explain the legacy workflow purpose."
    Assert-True ($readme.Contains("modern Windows installation and help files")) "readme.txt does not mention the RC2 installation/help focus."
    Assert-True ($readme.Contains("recent euro adoptions")) "readme.txt does not mention recent euro adoptions."
    Assert-True ($readme.Contains("Croatia 2023")) "readme.txt does not mention Croatia 2023."
    Assert-True ($readme.Contains("Bulgaria 2026")) "readme.txt does not mention Bulgaria 2026."
    Assert-True ($readme.Contains("Windows 7 and later")) "readme.txt does not mention the Windows 7+ startup/configuration fix."
    Assert-True ($readme.Contains("CHM start topics")) "readme.txt does not mention the RC2 CHM Start topic fix."
    Assert-True ($readme.Contains("Visual Basic 6 Runtime is required")) "readme.txt does not clearly mention the required VB6 runtime."
    Assert-True ($pad.Contains("<Program_Version>1.74</Program_Version>")) "PAD metadata still has the wrong version."
    Assert-True ($pad.Contains("<Program_Release_Year>2026</Program_Release_Year>")) "PAD metadata still has the wrong release year."
    Assert-True ($pad.Contains("<Char_Desc_2000>") -and $pad.Contains("</Char_Desc_2000>") -and $pad -notmatch "<Char_Desc_2000\s*/>") "PAD metadata has no long description."
    Assert-True ($pad.Contains("final Syscalculator 1.x legacy release")) "PAD long description does not mention the final 1.x legacy release."
    Assert-True ($pad.Contains("Windows 11")) "PAD long description does not mention Windows 11 testing."
    Assert-True ($pad.Contains("VB6 Runtime") -or $pad.Contains("Visual Basic 6 Runtime")) "PAD long description does not mention the VB6 runtime."
    Assert-True ($pad.Contains("localized CHM help")) "PAD long description does not mention localized CHM help."
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
    $buildInstaller = Read-Text (Join-Path $projectDir "Build\BuildInstaller174.ps1")

    Assert-True ($installer.Contains("function IsVb6RuntimeInstalled")) "Modern installer has no VB6 runtime check function."
    Assert-True ($installer.Contains("msvbvm60.dll")) "Modern installer does not check for msvbvm60.dll."
    Assert-True ($installer.Contains("function InitializeSetup")) "Modern installer does not run the VB6 runtime check before setup."
    Assert-True ($installer.Contains("Vb6RuntimeMissing")) "Modern installer has no VB6 runtime missing message."
    Assert-True ($installer.Contains("ShellExec('open'")) "Modern installer does not offer to open the VB6 runtime download page."
    Assert-True ($installer.Contains("support.microsoft.com")) "Modern installer does not use an official Microsoft VB6 runtime download page."
    Assert-True ($installerDoc.Contains("installer checks for the Visual Basic 6 Runtime")) "Installer documentation does not describe the VB6 runtime check."
    Assert-True ($installerDoc.Contains("support.microsoft.com")) "Installer documentation does not include the Microsoft VB6 runtime download page."
    Assert-True ($buildInstaller.Contains("Get-VersionFromExecutable")) "Legacy installer build script does not read the version from the compiled executable."
    Assert-True ($buildInstaller.Contains("SyscalEditor.exe")) "Legacy installer build script does not require the editor executable."
    Assert-True ($buildInstaller.Contains("SYSCALC174_INSTALL_VERSION")) "Legacy installer build script does not pass the version to Inno Setup."
}

Test-Case "Legacy help can be compiled as main and localized CHM files" {
    $installer = Read-Text (Join-Path $repoRoot "installer\Syscalculator174.iss")
    $buildHelp = Read-Text (Join-Path $projectDir "Build\BuildHelp174.ps1")
    $helpProject = Read-Text (Join-Path $projectDir "help\Syscalculator174.hhp")
    $helpContents = Read-Text (Join-Path $projectDir "help\Syscalculator174.hhc")
    $helpProjectEn = Read-Text (Join-Path $projectDir "help\Syscalculator174-en.hhp")
    $helpProjectNl = Read-Text (Join-Path $projectDir "help\Syscalculator174-nl.hhp")
    $helpProjectEs = Read-Text (Join-Path $projectDir "help\Syscalculator174-es.hhp")
    $helpProjectCat = Read-Text (Join-Path $projectDir "help\Syscalculator174-cat.hhp")
    $english = Read-LegacyText (Join-Path $projectDir "eng.lng")
    $dutch = Read-LegacyText (Join-Path $projectDir "ned.lng")
    $spanish = Read-LegacyText (Join-Path $projectDir "esp.lng")
    $catalan = Read-LegacyText (Join-Path $projectDir "cat.lng")
    $form = Read-Text (Join-Path $projectDir "Form1.frm")
    $module = Read-Text (Join-Path $projectDir "Freesyscal.bas")

    Assert-True ($helpProject.Contains("Compiled file=Syscalculator174.chm")) "CHM project does not build Syscalculator174.chm."
    Assert-True ($helpProject.Contains("Default Window=main")) "Main CHM project has no compact default window."
    Assert-True ($helpProject.Contains("[90,80,850,620]")) "Main CHM default window is not the expected compact size."
    Assert-True ($helpProject.Contains('",,,,,,0x63520,,0x10000c,[90,80,850,620],0x80000,,,,,,0')) "Main CHM window definition does not use the stable compact HHP format."
    Assert-True ($helpProject -notmatch '"index\.htm","index\.htm"') "Main CHM window definition duplicates default and home topics."
    Assert-True ($helpProject.Contains("Default topic=index.htm")) "Main CHM project does not use index.htm as contents start page."
    Assert-True ($helpContents.Contains("index.htm")) "Main CHM contents does not point to the help contents page."
    Assert-True ($helpContents.Contains("Convert a Value") -and $helpContents.Contains("help_en_convert.htm")) "Main CHM contents does not list English help articles."
    Assert-True ($helpContents.Contains("Waarde Converteren") -and $helpContents.Contains("help_nl_converteren.htm")) "Main CHM contents does not list Dutch help articles."
    Assert-True ($helpContents.Contains("Convertir un Valor") -and $helpContents.Contains("help_es_convertir.htm")) "Main CHM contents does not list Spanish help articles."
    Assert-True ($helpContents.Contains("Camps, Edicio i Porta-retalls") -and $helpContents.Contains("help_cat_camps.htm")) "Main CHM contents does not list Catalan help articles."
    Assert-True ($helpProjectEn.Contains("Compiled file=Syscalculator174-en.chm") -and $helpProjectEn.Contains("Default topic=index_en.htm")) "English CHM project is not configured correctly."
    Assert-True ($helpProjectNl.Contains("Compiled file=Syscalculator174-nl.chm") -and $helpProjectNl.Contains("Default topic=index_nl.htm")) "Dutch CHM project is not configured correctly."
    Assert-True ($helpProjectEs.Contains("Compiled file=Syscalculator174-es.chm") -and $helpProjectEs.Contains("Default topic=index_es.htm")) "Spanish CHM project is not configured correctly."
    Assert-True ($helpProjectCat.Contains("Compiled file=Syscalculator174-cat.chm") -and $helpProjectCat.Contains("Default topic=index_cat.htm")) "Catalan CHM project is not configured correctly."
    Assert-True ($helpProjectEn.Contains("[90,80,850,620]") -and $helpProjectNl.Contains("[90,80,850,620]") -and $helpProjectEs.Contains("[90,80,850,620]") -and $helpProjectCat.Contains("[90,80,850,620]")) "Localized CHM projects do not use the compact default window size."
    Assert-True ($buildHelp.Contains("hhc.exe")) "CHM build script does not look for hhc.exe."
    Assert-True ($buildHelp.Contains("Syscalculator174-en.hhp") -and $buildHelp.Contains("Syscalculator174-nl.hhp") -and $buildHelp.Contains("Syscalculator174-es.hhp") -and $buildHelp.Contains("Syscalculator174-cat.hhp")) "CHM build script does not compile all localized projects."
    Assert-True ($installer.Contains("help\*.chm")) "Installer does not include all compiled CHM files."
    Assert-True ($english.Contains("[App]\help\Syscalculator174-en.chm")) "English help does not point to the English CHM."
    Assert-True ($dutch.Contains("[App]\help\Syscalculator174-nl.chm")) "Dutch help does not point to the Dutch CHM."
    Assert-True ($spanish.Contains("[App]\help\Syscalculator174-es.chm")) "Spanish help does not point to the Spanish CHM."
    Assert-True ($catalan.Contains("[App]\help\Syscalculator174-cat.chm")) "Catalan help does not point to the Catalan CHM."
    Assert-True ($english -notmatch "\[App\]\\help\\index_") "English help still points to loose HTML."
    Assert-True ($dutch -notmatch "\[App\]\\help\\index_") "Dutch help still points to loose HTML."
    Assert-True ($spanish -notmatch "\[App\]\\help\\index_") "Spanish help still points to loose HTML."
    Assert-True ($catalan -notmatch "\[App\]\\help\\index_") "Catalan help still points to loose HTML."
    Assert-True ($form.Contains("OpenConfiguredHelp Me.Hwnd")) "Main form does not use the CHM-aware help launcher."
    Assert-True ($module.Contains("hh.exe")) "Help launcher does not use hh.exe for CHM files."
    Assert-True ($module.Contains('Environ$("WINDIR")')) "Help launcher does not use the full Windows hh.exe path."
    Assert-True ($module.Contains("LaunchHtmlHelp")) "Help launcher does not use the shared HTML Help launch routine."
    Assert-True ($module.Contains("fallbackTarget")) "Help launcher does not fall back to loose HTML."
    Assert-True ((Read-Text (Join-Path $projectDir "WindowsAPI.bas")).Contains("Global Const SW_SHOWNORMAL = 1")) "Main project leaves ShellExecute windows hidden because SW_SHOWNORMAL is not defined."
}

Test-Case "Syscalculator 1.74 Help menu opens localized CHM files" {
    $form = Read-Text (Join-Path $projectDir "Form1.frm")
    $formData = Read-Text (Join-Path $projectDir "Form1Data.frm")
    $editor = Read-Text (Join-Path $projectDir "editor.frm")
    $zEditor = Read-Text (Join-Path $projectDir "Zeditor.frm")
    $module = Read-Text (Join-Path $projectDir "Freesyscal.bas")
    $editorModule = Read-Text (Join-Path $projectDir "EditorMod.bas")
    $language = Read-Text (Join-Path $projectDir "Languare.bas")
    $languageEditor = Read-Text (Join-Path $projectDir "Languarex.bas")
    $windowsApi = Read-Text (Join-Path $projectDir "WindowsAPI.bas")
    $editorApi = Read-Text (Join-Path $projectDir "WeditorApi.bas")
    $english = Read-LegacyText (Join-Path $projectDir "eng.lng")
    $dutch = Read-LegacyText (Join-Path $projectDir "ned.lng")
    $dutchAlt = Read-LegacyText (Join-Path $projectDir "ned1.lng")
    $spanish = Read-LegacyText (Join-Path $projectDir "esp.lng")
    $catalan = Read-LegacyText (Join-Path $projectDir "cat.lng")
    $directChmBranch = 'If LCase$(Right$(helpTarget, 4)) = ".chm" Then'

    Assert-True ($form.Contains("Private Sub Help2_Click()") -and $form.Contains("OpenConfiguredHelp Me.Hwnd")) "Main form Help menu is not wired to OpenConfiguredHelp."
    Assert-True ($formData.Contains("Private Sub Help2_Click()") -and $formData.Contains("OpenConfiguredHelp Me.Hwnd")) "Stored main form data is not wired to OpenConfiguredHelp."
    Assert-True ($editor.Contains("Private Sub Help2_Click()") -and $editor.Contains("OpenConfiguredHelp Me.Hwnd")) "Editor Help menu is not wired to OpenConfiguredHelp."
    Assert-True ($zEditor.Contains("Private Sub Help2_Click()") -and $zEditor.Contains("OpenConfiguredHelp Me.Hwnd")) "Zeditor Help menu is not wired to OpenConfiguredHelp."
    Assert-True ($module.Contains($directChmBranch)) "Main help launcher does not open direct CHM paths through hh.exe."
    Assert-True ($editorModule.Contains($directChmBranch)) "Editor help launcher does not open direct CHM paths through hh.exe."
    Assert-True ($module.Contains('Dir$(helpTarget)') -and $editorModule.Contains('Dir$(helpTarget)')) "Help launchers do not verify the CHM file before hh.exe."
    Assert-True ($module.Contains('"hh.exe"') -and $editorModule.Contains('"hh.exe"')) "Help launchers do not call hh.exe."
    Assert-True ($module.Contains('Environ$("WINDIR")') -and $editorModule.Contains('Environ$("WINDIR")')) "Help launchers do not try the full Windows hh.exe path."
    Assert-True ($module.Contains("iRet > 32") -and $editorModule.Contains("iRet > 32")) "Help launchers do not fall through when hh.exe launch fails."
    Assert-True ($windowsApi.Contains("Global Const SW_SHOWNORMAL = 1") -and $editorApi.Contains("Public Const SW_SHOWNORMAL = 1")) "ShellExecute show mode is not defined for both main app and editor."
    Assert-True ($language.Contains('If Left$(url, 5) = "[App]" Then url = App.Path + Mid$(url, 6)')) "Main language loader does not resolve [App] help paths."
    Assert-True ($languageEditor.Contains('If Left$(url, 5) = "[App]" Then url = App.Path + Mid$(url, 6)')) "Editor language loader does not resolve [App] help paths."
    Assert-True ($english.Contains('[App]\help\Syscalculator174-en.chm')) "English language file is not connected to the English CHM."
    Assert-True ($dutch.Contains('[App]\help\Syscalculator174-nl.chm')) "Dutch language file is not connected to the Dutch CHM."
    Assert-True ($dutchAlt.Contains('[App]\help\Syscalculator174-nl.chm')) "Alternate Dutch language file is not connected to the Dutch CHM."
    Assert-True ($spanish.Contains('[App]\help\Syscalculator174-es.chm')) "Spanish language file is not connected to the Spanish CHM."
    Assert-True ($catalan.Contains('[App]\help\Syscalculator174-cat.chm')) "Catalan language file is not connected to the Catalan CHM."

    foreach ($chm in @("Syscalculator174-en.chm", "Syscalculator174-nl.chm", "Syscalculator174-es.chm", "Syscalculator174-cat.chm")) {
        Assert-True (Test-Path -LiteralPath (Join-Path $projectDir "help\$chm")) "Compiled localized CHM is missing: $chm"
    }
}

Test-Case "WizardExpress help documents the Office 365 limitation" {
    $helpEn = Read-Text (Join-Path $projectDir "help\help_en_wizardexpress.htm")
    $helpNl = Read-Text (Join-Path $projectDir "help\help_nl_wizardexpress.htm")
    $helpEs = Read-Text (Join-Path $projectDir "help\help_es_wizardexpress.htm")
    $helpCat = Read-Text (Join-Path $projectDir "help\help_cat_wizardexpress.htm")
    $classicHelp = Read-Text (Join-Path $projectDir "help\index.htm")
    $readme = Read-Text (Join-Path $projectDir "readme.txt")
    $pad = Read-Text (Join-Path $projectDir "pad_file.xml")

    Assert-True ($helpEn.Contains("older Office and Word versions")) "English help does not say WizardExpress is for older Office/Word versions."
    Assert-True ($helpNl.Contains("oudere Office- en Word-versies")) "Dutch help does not say WizardExpress is for older Office/Word versions."
    Assert-True ($helpEn.Contains("LibreOffice has been tested and works ok")) "English help does not document the LibreOffice test result."
    Assert-True ($helpNl.Contains("LibreOffice is getest en werkt ok")) "Dutch help does not document the LibreOffice test result."
    Assert-True ($helpEs.Contains("Microsoft 365 / Office 365")) "Spanish help has no Microsoft 365 / Office 365 warning."
    Assert-True ($helpCat.Contains("Microsoft 365 / Office 365")) "Catalan help has no Microsoft 365 / Office 365 warning."
    Assert-True ($classicHelp.Contains("Syscalculator 1.74 Help")) "Classic help index is not the help contents page."
    Assert-True ($classicHelp.Contains("index_en.htm")) "Classic help index does not link to the English overview."
    Assert-True ($classicHelp.Contains("index_nl.htm")) "Classic help index does not link to the Dutch overview."
    Assert-True ($classicHelp.Contains("index_es.htm")) "Classic help index does not link to the Spanish overview."
    Assert-True ($classicHelp.Contains("index_cat.htm")) "Classic help index does not link to the Catalan overview."
    Assert-True ($readme.Contains("Microsoft 365 / Office 365")) "readme.txt has no Microsoft 365 / Office 365 warning."
    Assert-True ($readme.Contains("LibreOffice has been tested and works ok")) "readme.txt does not document the LibreOffice test result."
    Assert-True ($pad.Contains("Microsoft 365 / Office 365 is not supported reliably")) "PAD metadata does not describe the Office 365 limitation."
    Assert-True ($pad.Contains("LibreOffice has been tested and works ok")) "PAD metadata does not document the LibreOffice test result."
}

Test-Case "Localized help pages include screenshots" {
    $helpEn = (Read-Text (Join-Path $projectDir "help\help_en_convert.htm")) + (Read-Text (Join-Path $projectDir "help\help_en_wizardexpress.htm")) + (Read-Text (Join-Path $projectDir "help\help_en_configuration.htm")) + (Read-Text (Join-Path $projectDir "help\help_en_nod_catalog.htm"))
    $helpNl = (Read-Text (Join-Path $projectDir "help\help_nl_converteren.htm")) + (Read-Text (Join-Path $projectDir "help\help_nl_wizardexpress.htm")) + (Read-Text (Join-Path $projectDir "help\help_nl_configuratie.htm")) + (Read-Text (Join-Path $projectDir "help\help_nl_nod_catalogus.htm"))
    $helpEs = (Read-Text (Join-Path $projectDir "help\help_es_convertir.htm")) + (Read-Text (Join-Path $projectDir "help\help_es_wizardexpress.htm")) + (Read-Text (Join-Path $projectDir "help\help_es_configuracion.htm")) + (Read-Text (Join-Path $projectDir "help\help_es_nod_catalogo.htm"))
    $helpCat = (Read-Text (Join-Path $projectDir "help\help_cat_convertir.htm")) + (Read-Text (Join-Path $projectDir "help\help_cat_wizardexpress.htm")) + (Read-Text (Join-Path $projectDir "help\help_cat_configuracio.htm")) + (Read-Text (Join-Path $projectDir "help\help_cat_nod_cataleg.htm"))
    $requiredImages = @("syscal_1.png", "syscal_2.png", "Syscal_3.png", "Syscal_4.png", "triconvert_3.jpg")

    foreach ($image in $requiredImages) {
        Assert-True ($helpEn.Contains($image)) "English help does not reference $image."
        Assert-True ($helpNl.Contains($image)) "Dutch help does not reference $image."
        Assert-True ($helpEs.Contains($image)) "Spanish help does not reference $image."
        Assert-True ($helpCat.Contains($image)) "Catalan help does not reference $image."
        Assert-True (Test-Path -LiteralPath (Join-Path $projectDir "help\$image")) "Help image is missing: $image"
    }

    Assert-True ($helpNl -notmatch "triconv_3\.png") "Dutch help still links to a missing triconv_3.png file."
}

Test-Case "Localized limitation articles show warning triangle" {
    $supportArticles = @(
        "help_en_support.htm",
        "help_nl_support.htm",
        "help_es_soporte.htm",
        "help_cat_suport.htm"
    )

    foreach ($article in $supportArticles) {
        $text = Read-Text (Join-Path $projectDir "help\$article")
        Assert-True ($text.Contains('class="warning"')) "$article has no warning box."
        Assert-True ($text.Contains('warning_triangle.png')) "$article has no warning triangle image."
        Assert-True ($text.Contains('alt="!"')) "$article has no warning exclamation fallback."
    }

    Assert-True (Test-Path -LiteralPath (Join-Path $projectDir "help\warning_triangle.png")) "Warning triangle image is missing."
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
    $compileScriptText = Read-Text $compileScript
    Assert-True ($compileScriptText.Contains("Project1.vbw") -and $compileScriptText.Contains("SyscalEditor.vbw")) "Compile script does not move VB6 workspace files out of the way."
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
