[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Read', 'Lines', 'Inventory', 'Progress', 'Files', 'Pins', 'Search')]
    [string]$Mode = 'Read',
    [string[]]$Path,
    [int]$Start = 1,
    [int]$Count = 200,
    [int]$Percent = 2,
    [string]$Pattern,
    [Nullable[int]]$EtaSeconds = $null
)
$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess(($Path -join ', '), $Mode)) { return }
switch ($Mode) {
    'Search' { rg -n --hidden --glob '!AGENTS-README-FIRST.yaml' --glob '!**/.git/**' --glob '!**/plugin-cache/**' -- $Pattern @Path }
    'Files' { foreach ($item in $Path) { rg --files --hidden $item } }
    'Pins' {
        $pins = Get-Content -LiteralPath $Path[0] -Raw | ConvertFrom-Json
        $results = foreach ($pin in $pins) {
            if ($pin.path -match '(?i)AGENTS-README-FIRST|[.]creds|api[-_]?key') { throw 'Credential-bearing pin denied.' }
            $hash = (Get-FileHash -LiteralPath $pin.path -Algorithm SHA256).Hash
            [ordered]@{ path = $pin.relativePath; expectedSha256 = $pin.sha256; observedSha256 = $hash; expectedBytes = $pin.bytes; observedBytes = (Get-Item -LiteralPath $pin.path -Force).Length; matches = ($hash -eq $pin.sha256) }
        }
        $receipt = [ordered]@{ timestampLocal = (Get-Date -Format s); pinCount = $pins.Count; driftCount = @($results | Where-Object { -not $_.matches }).Count; pins = @($results) }
        $receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'input-pins.verified.json')
        $receipt | ConvertTo-Json -Depth 8
    }
    'Progress' {
        [ordered]@{ timestampLocal = (Get-Date -Format s); completionPercent = $Percent; etaSeconds = $EtaSeconds } | ConvertTo-Json -Compress
    }
    'Inventory' {
        foreach ($item in $Path) { Get-ChildItem -LiteralPath $item -File | Select-Object Name, Length }
    }
    default {
        foreach ($item in $Path) {
            if ($item -match '(?i)AGENTS-README-FIRST|[.]creds|api[-_]?key|session-state|current-turn') { throw 'Credential-bearing path denied.' }
            Write-Output ('FILE: ' + $item)
            if ($Mode -eq 'Lines') {
                $lines = Get-Content -LiteralPath $item
                for ($index = $Start - 1; $index -lt [Math]::Min($lines.Count, $Start - 1 + $Count); $index++) {
                    Write-Output ('{0}: {1}' -f ($index + 1), $lines[$index])
                }
            } else { Get-Content -LiteralPath $item -Raw }
        }
    }
}
