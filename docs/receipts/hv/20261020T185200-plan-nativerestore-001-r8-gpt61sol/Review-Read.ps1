[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][ValidateSet('Bootstrap', 'Read', 'Range', 'Search', 'FileList', 'Progress', 'Pins', 'PluginDocs')][string]$Mode,
    [string[]]$Paths = @(),
    [int]$Start = 1,
    [int]$End = 200,
    [string]$Pattern = '',
    [int]$Percent = 2,
    [Nullable[int]]$Eta = $null
)
$ErrorActionPreference = 'Stop'
$Paths = @($Paths | ForEach-Object { $_ -split '\|' })
function Write-SafeFile {
    param([string]$Path, [int]$First = 1, [int]$Last = [int]::MaxValue)
    if ($Path -match 'AGENTS-README-FIRST|\.creds|api[-_]?key|credentials') { throw 'Credential-bearing path prohibited.' }
    $content = @(Get-Content -LiteralPath $Path)
    Write-Output ('FILE: ' + $Path + ' LINES: ' + $content.Count)
    for ($index = [Math]::Max(0, $First - 1); $index -lt [Math]::Min($content.Count, $Last); $index++) {
        Write-Output ('{0}: {1}' -f ($index + 1), $content[$index])
    }
}
if ($WhatIfPreference) { Write-Output ('WhatIf: read-only ' + $Mode); return }
switch ($Mode) {
    'Bootstrap' {
        Write-SafeFile '/home/sharpninja/.codex/plugins/cache/mcpserver-codex-plugin/mcpserver/1.121.0/skills/enforcement/SKILL.md'
        Write-SafeFile '/home/sharpninja/.codex/plugins/cache/mcpserver-codex-plugin/mcpserver/1.121.0/skills/workflow/SKILL.md'
        Write-SafeFile (Join-Path $PSScriptRoot 'request.jsonl')
        Write-SafeFile (Join-Path $PSScriptRoot 'input-pins.json')
        Get-ChildItem -LiteralPath '/home/sharpninja/github/mcpserver-codex-plugin' -File | Select-Object Name
    }
    'Read' { foreach ($path in $Paths) { Write-SafeFile $path } }
    'Range' { foreach ($path in $Paths) { Write-SafeFile $path $Start $End } }
    'Search' {
        foreach ($path in $Paths) {
            if ($path -match 'AGENTS-README-FIRST|\.creds|api[-_]?key|credentials') { throw 'Credential-bearing path prohibited.' }
            & rg --line-number --glob '!AGENTS-README-FIRST.yaml' --glob '!*.db' --glob '!session-state.yaml' --glob '!*.key' --glob '!*.env' -- $Pattern $path
        }
    }
    'Progress' { [ordered]@{timestampLocal=(Get-Date -Format s);completionPercent=$Percent;etaSeconds=$Eta} | ConvertTo-Json -Compress }
    'FileList' { foreach ($path in $Paths) { & rg --files -- $path | Where-Object { $_ -match $Pattern } } }
    'Pins' {
        $pins = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'input-pins.json') -Raw | ConvertFrom-Json
        $results = @(foreach ($pin in $pins) {
            $hash = (Get-FileHash -LiteralPath $pin.path -Algorithm SHA256).Hash
            [ordered]@{path=$pin.relativePath;expected=$pin.sha256;actual=$hash;bytes=(Get-Item -Force -LiteralPath $pin.path).Length;match=($hash -eq $pin.sha256)}
        })
        $receipt = [ordered]@{timestampLocal=(Get-Date -Format s);pins=$results;driftCount=@($results | Where-Object { -not $_.match }).Count}
        $receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'pins.initial.json')
        $receipt | ConvertTo-Json -Depth 8 -Compress
    }
    'PluginDocs' {
        Write-SafeFile '/home/sharpninja/github/mcpserver-codex-plugin/Invoke-CodexMcpPlugin.ps1'
        Write-SafeFile '/home/sharpninja/github/mcpserver-codex-plugin/skills/session/SKILL.md'
        & rg --files '/home/sharpninja/github/mcpserver-codex-plugin/lib'
    }
}
