[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Pins','Discovery','State','Clock')][string] $Mode = 'Clock')
$ErrorActionPreference = 'Stop'
$receiptPath = $PSScriptRoot
switch ($Mode) {
    'Clock' { Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffzzz' }
    'Discovery' {
        & rg --files '/home/sharpninja/github/mcpserver-codex-plugin' -g '*CodexMcpPlugin*' -g '*repl-invoke*' -g '*session-start*' -g '*triage*' -g '*failsafe*'
        Get-Module -ListAvailable PSScriptAnalyzer,Pester | Select-Object Name,Version,Path
    }
    'Pins' {
        $pins = Get-Content -Raw -LiteralPath (Join-Path $receiptPath 'input-pins.json') | ConvertFrom-Json
        $results = foreach ($pin in $pins) {
            if ([IO.Path]::GetFileName($pin.path) -eq 'AGENTS-README-FIRST.yaml') { throw 'Forbidden pin input.' }
            $item = Get-Item -Force -LiteralPath $pin.path
            $hash = (Get-FileHash -LiteralPath $pin.path -Algorithm SHA256).Hash
            [pscustomobject]@{path=$pin.relativePath; expectedSha256=$pin.sha256; actualSha256=$hash; expectedBytes=$pin.bytes; actualBytes=$item.Length; match=($hash -eq $pin.sha256 -and $item.Length -eq $pin.bytes)}
        }
        if ($PSCmdlet.ShouldProcess((Join-Path $receiptPath 'pin-verification.json'), 'Write pin verification')) {
            $results | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $receiptPath 'pin-verification.json') -Encoding utf8
        }
        $results | ConvertTo-Json -Depth 8
    }
    'State' {
        & git branch --show-current
        & git rev-parse HEAD
        & git log -4 --format='%h %s'
        & git status --porcelain=v1 -uall
        $nativeStatus = @(& git -C native/vice status --porcelain=v1 -uall)
        [pscustomobject]@{tracked=@($nativeStatus | Where-Object { -not $_.StartsWith('?? ') }).Count; untracked=@($nativeStatus | Where-Object { $_.StartsWith('?? ') }).Count} | ConvertTo-Json
        $nativeStatus | Where-Object { -not $_.StartsWith('?? ') }
        & git -C native/vice diff --stat
        & git submodule status
    }
}
