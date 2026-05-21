param(
    [string]$Purpose = "this script",
    [string]$ScriptPath = $PSCommandPath
)

if ($PSVersionTable.PSVersion.Major -ge 7) {
    return
}

$pwsh = @(
    "C:\Program Files\PowerShell\7\pwsh.exe",
    "C:\Program Files (x86)\PowerShell\7\pwsh.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($pwsh) {
    throw "PowerShell 7 is required for $Purpose. Run with: `"$pwsh`" -NoProfile -ExecutionPolicy Bypass -File $ScriptPath"
}

throw "PowerShell 7 is required for $Purpose. Install it first, for example with: winget install --id Microsoft.PowerShell --source winget"
