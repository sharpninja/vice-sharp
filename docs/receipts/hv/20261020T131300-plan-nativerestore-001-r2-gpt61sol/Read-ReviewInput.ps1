[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string[]] $LiteralPath,
    [int] $StartLine = 1,
    [int] $LineCount = 0
)
$ErrorActionPreference = 'Stop'
foreach ($inputPath in $LiteralPath) {
    if ([IO.Path]::GetFileName($inputPath) -eq 'AGENTS-README-FIRST.yaml' -or $inputPath -match '(?i)(\.creds|api[-_]?key|session-state|settings\.json|config\.toml)') {
        throw 'Credential-bearing input prohibited.'
    }
    $contentLines = @(Get-Content -LiteralPath $inputPath)
    Write-Output ('READ ' + $inputPath + ' TOTAL_LINES=' + $contentLines.Count)
    $lastLine = $contentLines.Count
    if ($LineCount -gt 0) { $lastLine = [Math]::Min($lastLine, $StartLine + $LineCount - 1) }
    for ($lineIndex = $StartLine - 1; $lineIndex -lt $lastLine; $lineIndex++) {
        Write-Output ('{0}: {1}' -f ($lineIndex + 1), $contentLines[$lineIndex])
    }
}
Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffzzz'
