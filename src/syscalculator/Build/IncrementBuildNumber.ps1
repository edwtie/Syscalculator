param(
    [Parameter(Mandatory = $true)]
    [string]$Path
)

$resolvedPath = Resolve-Path -LiteralPath $Path -ErrorAction Stop
$text = Get-Content -LiteralPath $resolvedPath.Path -Raw
$pattern = 'BuildNumber\s*=\s*"(?<date>\d{4}\.\d{2}\.\d{2})\.(?<number>\d{3})"'
$match = [regex]::Match($text, $pattern)

if (-not $match.Success) {
    throw "BuildNumber not found in $Path"
}

$today = Get-Date -Format 'yyyy.MM.dd'
$oldDate = $match.Groups['date'].Value
$oldNumber = [int]$match.Groups['number'].Value
$nextNumber = if ($oldDate -eq $today) { $oldNumber + 1 } else { 1 }
$newBuildNumber = $today + '.' + $nextNumber.ToString('000')
$replacement = 'BuildNumber = "' + $newBuildNumber + '"'
$updated = [regex]::Replace($text, $pattern, $replacement, 1)

Set-Content -LiteralPath $resolvedPath.Path -Value $updated -NoNewline -Encoding UTF8
Write-Host "Syscalculator build number: $newBuildNumber"
