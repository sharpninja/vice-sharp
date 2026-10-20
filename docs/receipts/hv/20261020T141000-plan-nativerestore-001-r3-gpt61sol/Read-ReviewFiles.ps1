[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string[]]$Path, [int]$Start = 1, [int]$Count = 0)
$ErrorActionPreference = 'Stop'
foreach ($reviewPath in $Path) {
    if ([IO.Path]::GetFileName($reviewPath) -eq 'AGENTS-README-FIRST.yaml' -or $reviewPath -match '/\.creds/|/session-state\.json$') { throw 'Credential-bearing path refused.' }
    if (-not $PSCmdlet.ShouldProcess($reviewPath, 'Read review evidence')) { continue }
    $reviewText = [IO.File]::ReadAllText($reviewPath)
    if ($reviewText -match '(?im)(?:api[_-]?key|authorization)\s*[:=]\s*["'']?(?:sk-|Bearer\s+)[a-z0-9_-]{12,}|-----BEGIN [A-Z ]*PRIVATE KEY-----') { throw 'Possible credential content refused before output.' }
    $reviewLines = $reviewText -split '\r?\n'
    Write-Output ('FILE: ' + $reviewPath + ' LINES: ' + $reviewLines.Count)
    $reviewEnd = if ($Count -gt 0) { [Math]::Min($reviewLines.Count, $Start - 1 + $Count) } else { $reviewLines.Count }
    for ($reviewIndex = $Start - 1; $reviewIndex -lt $reviewEnd; $reviewIndex++) { Write-Output ('{0}: {1}' -f ($reviewIndex + 1), $reviewLines[$reviewIndex]) }
}
