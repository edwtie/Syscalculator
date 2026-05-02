param(
    [string]$Vb6Path = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$legacyRoot = Join-Path $repoRoot 'legacy\Syscalculator174.VB6'
$projectPath = Join-Path $legacyRoot 'Syscalculator174.local.vbp'
$artifactsRoot = Join-Path $repoRoot 'artifacts\legacy\vb6-compile'
$compileLog = Join-Path $artifactsRoot 'Syscalculator174.compile.log'

function Resolve-Vb6Path {
    param([string]$ExplicitPath)

    if ($ExplicitPath) {
        if (Test-Path $ExplicitPath) {
            return (Resolve-Path $ExplicitPath).Path
        }

        throw "VB6.EXE was not found at explicit path: $ExplicitPath"
    }

    $pathCommand = Get-Command 'VB6.EXE' -ErrorAction SilentlyContinue
    if ($pathCommand) {
        return $pathCommand.Source
    }

    $registryRoots = @(
        'Registry::HKEY_CLASSES_ROOT\VisualBasic.Project\shell\Make\command',
        'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Classes\VisualBasic.Project\shell\Make\command',
        'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Classes\VisualBasic.Project\shell\Make\command'
    )

    foreach ($registryPath in $registryRoots) {
        if (Test-Path $registryPath) {
            $command = (Get-Item -Path $registryPath).GetValue('')

            if ($command -and $command -match '"(?<path>[^"]*VB6\.EXE)"') {
                $candidate = $Matches['path']
                if (Test-Path $candidate) {
                    return (Resolve-Path $candidate).Path
                }
            }
        }
    }

    $candidatePaths = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\VB98\VB6.EXE'),
        (Join-Path $env:ProgramFiles 'Microsoft Visual Studio\VB98\VB6.EXE'),
        (Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\VB98\VB6.exe'),
        (Join-Path $env:ProgramFiles 'Microsoft Visual Studio\VB98\VB6.exe'),
        'C:\Program Files (x86)\Microsoft Visual Studio\VB98\VB6.EXE',
        'C:\Program Files\Microsoft Visual Studio\VB98\VB6.EXE'
    )

    foreach ($candidate in $candidatePaths) {
        if ($candidate -and (Test-Path $candidate)) {
            return (Resolve-Path $candidate).Path
        }
    }

    return $null
}

if (-not (Test-Path $projectPath)) {
    throw "VB6 project was not found: $projectPath"
}

New-Item -ItemType Directory -Force -Path $artifactsRoot | Out-Null

$resolvedVb6Path = Resolve-Vb6Path -ExplicitPath $Vb6Path
if (-not $resolvedVb6Path) {
    Write-Host 'VB6.EXE was not found on this machine.'
    Write-Host 'Install a licensed Visual Basic 6.0 / Visual Studio 6.0 environment, preferably in a VM, then run:'
    Write-Host '  BUILD_SYSCALCULATOR174_VB6_CANDIDATE.bat'
    Write-Host 'or with an explicit path:'
    Write-Host '  BUILD_SYSCALCULATOR174_VB6_CANDIDATE.bat -Vb6Path "C:\Program Files (x86)\Microsoft Visual Studio\VB98\VB6.EXE"'
    exit 2
}

Write-Host "Using VB6 compiler: $resolvedVb6Path"
Write-Host "Compiling project: $projectPath"

$arguments = @('/MAKE', $projectPath, '/OUT', $compileLog)
$process = Start-Process -FilePath $resolvedVb6Path -ArgumentList $arguments -WorkingDirectory $legacyRoot -Wait -PassThru -WindowStyle Hidden

if ($process.ExitCode -ne 0) {
    Write-Host "VB6 compile failed with exit code $($process.ExitCode)."
    if (Test-Path $compileLog) {
        Write-Host "Compile log: $compileLog"
        Get-Content $compileLog
    }

    exit $process.ExitCode
}

Write-Host 'VB6 compile completed.'
Write-Host "Expected executable: $(Join-Path $legacyRoot 'Syscalculator174.exe')"
if (Test-Path $compileLog) {
    Write-Host "Compile log: $compileLog"
}
