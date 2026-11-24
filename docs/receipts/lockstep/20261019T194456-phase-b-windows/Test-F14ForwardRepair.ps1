#Requires -Version 7.4
<#
.SYNOPSIS
    Read-only re-verification of F-14 (Phase B gap; OMAR-VICE# Phase C brief 2026-10-19T19:52). The Oct 8 disposition:
    "Eleven full corrective decision action/dialog pairs persisted on current corrective turn with source linkage; all
    four immutable completed historical turns unchanged." Queries client.SessionLog.QueryAsync (agent Codex) through
    plugin 1.121.0 and reports, for session Codex-20261007T135626Z-plugin-session: the four historical turns (status,
    action types, count of design_decision actions) and every turn carrying design_decision actions (count, and how
    many cite one of the four historical request ids). Writes f14-reverify.json here.
#>
[CmdletBinding()]
param(
    [string]$SessionId = 'Codex-20261007T135626Z-plugin-session',
    [int]$Limit = 200,
    [string]$PluginRoot = (Join-Path $env:USERPROFILE '.claude\plugins\cache\mcpserver-local\mcpserver\1.121.0')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module powershell-yaml
$historical = @('req-20261008T135637Z-prompt-5be7', 'req-20261008T142219Z-prompt-bc0b', 'req-20261008T151514Z-prompt-9817', 'req-20261008T161608Z-prompt-c06c')
$pp = Join-Path $env:TEMP 'f14-query.yaml'
ConvertTo-Yaml -Data ([ordered]@{ agent = 'Codex'; limit = $Limit }) | Out-File -LiteralPath $pp -Encoding utf8NoBOM
$env:CLAUDE_PLUGIN_ROOT = $PluginRoot
$env:PLUGIN_AGENT_NAME = 'Claude'
try {
    $raw = & pwsh.exe -NoProfile -NonInteractive -File (Join-Path $PluginRoot 'lib\Invoke-McpPlugin.ps1') -Command Invoke -Method 'client.SessionLog.QueryAsync' -ParamsPath $pp -WorkspacePath 'F:\GitHub\vice-sharp' 2>&1
    $exit = $LASTEXITCODE
}
finally { Remove-Item Env:CLAUDE_PLUGIN_ROOT, Env:PLUGIN_AGENT_NAME -ErrorAction SilentlyContinue }
if ($exit -ne 0) { throw "QueryAsync exit $exit" }
$doc = ConvertFrom-Yaml (($raw | Out-String) -replace '(?m)^---\s*$', '')
$sessions = @($doc.payload.result.items)
$s = @($sessions | Where-Object { $_.sessionId -eq $SessionId })
if ($s.Count -ne 1) { throw "session $SessionId found $($s.Count) times in $($sessions.Count) Codex sessions (limit $Limit)" }
$turns = @($s[0].turns)

function Get-Action($Turn) { if ($Turn.Contains('actions') -and $Turn['actions']) { @($Turn['actions']) } else { @() } }
function Test-Decision($Action) { ([string]$Action['type']) -match '(?i)design_?decision' }

$hist = foreach ($id in $historical) {
    $t = @($turns | Where-Object { $_.requestId -eq $id })
    [ordered]@{
        requestId = $id; found = $t.Count; status = $(if ($t.Count) { [string]$t[0].status } else { $null })
        actionTypes = $(if ($t.Count) { @(Get-Action $t[0] | ForEach-Object { [string]$_['type'] } | Sort-Object -Unique) } else { @() })
        designDecisionActions = $(if ($t.Count) { @(Get-Action $t[0] | Where-Object { Test-Decision $_ }).Count } else { $null })
    }
}
$carriers = foreach ($t in $turns) {
    $d = @(Get-Action $t | Where-Object { Test-Decision $_ })
    if ($d.Count) {
        $text = ($d | ConvertTo-Json -Depth 8 -Compress)
        [ordered]@{ requestId = [string]$t.requestId; status = [string]$t.status; designDecisionActions = $d.Count; citingHistoricalTurns = @($d | Where-Object { $j = ($_ | ConvertTo-Json -Depth 8 -Compress); @($historical | Where-Object { $j.Contains($_) }).Count }).Count; citesAny = @($historical | Where-Object { $text.Contains($_) }) }
    }
}
$out = [ordered]@{ at = (Get-Date -Format s); sessionId = $SessionId; turns = $turns.Count; historical = @($hist); decisionCarrierTurns = @($carriers) }
$out | ConvertTo-Json -Depth 8 | Out-File -LiteralPath (Join-Path $PSScriptRoot 'f14-reverify.json') -Encoding utf8NoBOM
'session {0}: {1} turns' -f $SessionId, $turns.Count
foreach ($h in $hist) { '  historical {0}: found {1}, status {2}, design_decision actions {3}, action types {4}' -f $h.requestId, $h.found, $h.status, $h.designDecisionActions, (@($h.actionTypes) -join ',') }
foreach ($c in @($carriers)) { '  decision carrier {0} ({1}): {2} design_decision actions, {3} cite a historical turn ({4})' -f $c.requestId, $c.status, $c.designDecisionActions, $c.citingHistoricalTurns, (@($c.citesAny) -join ',') }
