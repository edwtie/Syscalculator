param(
    [string]$Vb6Path = "",
    [switch]$SkipCompile
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
$repoRoot = Split-Path -Parent (Split-Path -Parent $projectDir)
$artifactDir = Join-Path $repoRoot "artifacts\legacy\vb6-compile"
$projectPath = Join-Path $projectDir "Project1.vbp"
$editorProjectPath = Join-Path $projectDir "SyscalEditor.vbp"
$projectBuilds = @(
    @{
        Name = "Syscalculator 1.74 candidate"
        ProjectPath = $projectPath
        LogPath = Join-Path $artifactDir "Syscalculator174.compile.log"
    },
    @{
        Name = "Syscalculator 1.74 editor"
        ProjectPath = $editorProjectPath
        LogPath = Join-Path $artifactDir "SyscalEditor174.compile.log"
    }
)

New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null

foreach ($build in $projectBuilds) {
    if (-not (Test-Path -LiteralPath $build.ProjectPath)) {
        throw "VB6 project not found: $($build.ProjectPath)"
    }
}

if ($SkipCompile) {
    foreach ($build in $projectBuilds) {
        Write-Host "Validated VB6 project path: $($build.ProjectPath)"
    }
    Write-Host "Skipping VB6 compile because -SkipCompile was supplied."
    exit 0
}

if ([string]::IsNullOrWhiteSpace($Vb6Path)) {
    $cmd = Get-Command "VB6.EXE" -ErrorAction SilentlyContinue
    if ($cmd) {
        $Vb6Path = $cmd.Source
    }
}

if ([string]::IsNullOrWhiteSpace($Vb6Path)) {
    $knownVb6Paths = @(
        "C:\Program Files (x86)\Microsoft Visual Studio\VB98\VB6.EXE",
        "C:\Program Files\Microsoft Visual Studio\VB98\VB6.EXE",
        "C:\Program Files (x86)\Microsoft Visual Studio 6.0\VB98\VB6.EXE",
        "C:\Program Files\Microsoft Visual Studio 6.0\VB98\VB6.EXE",
        "C:\VB98\VB6.EXE"
    )

    foreach ($knownPath in $knownVb6Paths) {
        if (Test-Path -LiteralPath $knownPath) {
            $Vb6Path = $knownPath
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($Vb6Path) -or -not (Test-Path -LiteralPath $Vb6Path)) {
    throw "VB6.EXE was not found. Install Visual Basic 6 or pass -Vb6Path `"C:\Path\VB6.EXE`"."
}

$workspaceBackupDir = Join-Path $artifactDir "vbw-backup"
$workspaceFiles = @("Project1.vbw", "SyscalEditor.vbw")
$movedWorkspaceFiles = @()

if (Test-Path -LiteralPath $workspaceBackupDir) {
    Remove-Item -LiteralPath $workspaceBackupDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $workspaceBackupDir | Out-Null

foreach ($workspaceFile in $workspaceFiles) {
    $workspacePath = Join-Path $projectDir $workspaceFile
    if (Test-Path -LiteralPath $workspacePath) {
        $backupPath = Join-Path $workspaceBackupDir $workspaceFile
        Move-Item -LiteralPath $workspacePath -Destination $backupPath -Force
        $movedWorkspaceFiles += @{
            Original = $workspacePath
            Backup = $backupPath
        }
    }
}

try {
    foreach ($build in $projectBuilds) {
        Write-Host "Compiling $($build.Name)..."
        Write-Host "Project: $($build.ProjectPath)"
        Write-Host "Log: $($build.LogPath)"

        if (Test-Path -LiteralPath $build.LogPath) {
            Remove-Item -LiteralPath $build.LogPath -Force
        }

        $arguments = "/MAKE `"$($build.ProjectPath)`" /OUT `"$($build.LogPath)`""
        $previousCompatLayer = [Environment]::GetEnvironmentVariable("__COMPAT_LAYER", "Process")
        try {
            [Environment]::SetEnvironmentVariable("__COMPAT_LAYER", "RunAsInvoker", "Process")
            $process = Start-Process -FilePath $Vb6Path -ArgumentList $arguments -WindowStyle Hidden -PassThru
        }
        finally {
            [Environment]::SetEnvironmentVariable("__COMPAT_LAYER", $previousCompatLayer, "Process")
        }
        if ($process) {
            $process.WaitForExit()
        }

        $compileLog = ""
        for ($attempt = 0; $attempt -lt 120; $attempt++) {
            $compileLog = if (Test-Path -LiteralPath $build.LogPath) { Get-Content -LiteralPath $build.LogPath -Raw } else { "" }
            if ($compileLog -match "(?i)succeeded") {
                break
            }
            Start-Sleep -Milliseconds 500
        }
        Start-Sleep -Milliseconds 1000
        $compileLog = if (Test-Path -LiteralPath $build.LogPath) { Get-Content -LiteralPath $build.LogPath -Raw } else { "" }
        $compileSucceeded = "$compileLog" -match "(?i)succeeded"
        if (-not $compileSucceeded) {
            throw "VB6 compile failed with exit code $LASTEXITCODE. See $($build.LogPath)"
        }
    }
}
finally {
    foreach ($workspaceFile in $movedWorkspaceFiles) {
        if (Test-Path -LiteralPath $workspaceFile.Backup) {
            Move-Item -LiteralPath $workspaceFile.Backup -Destination $workspaceFile.Original -Force
        }
    }

    if (Test-Path -LiteralPath $workspaceBackupDir) {
        Remove-Item -LiteralPath $workspaceBackupDir -Recurse -Force
    }
}

Write-Host "VB6 compile completed."
