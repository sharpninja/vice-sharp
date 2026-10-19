#Requires -Version 7.4
<#
.SYNOPSIS
    Read-only drift check: for every core-synced lib file (plugins/core/lib-ps/*.ps1|psm1 in McpServer), compare
    the git blob in McpServer origin/main and origin/develop with the blob of lib/<file> on each plugin repo's
    origin default branch. Reports, per file, which plugin copies equal which core ref. Writes core-drift.json.
#>
[CmdletBinding()]
param(
    [string]$Root = 'F:\GitHub',
    [string]$OutPath = (Join-Path $PSScriptRoot 'core-drift.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$core = Join-Path $Root 'McpServer'
$refs = @('origin/main', 'origin/develop')
$repos = @('mcpserver-claude-code-plugin', 'mcpserver-codex-plugin', 'mcpserver-cline-plugin', 'mcpserver-cline-v2-plugin', 'mcpserver-copilot-plugin', 'mcpserver-grok-plugin', 'mcpserver-opencode-plugin', 'mcpserver-grok-bot-plugin', 'mcpserver-claude-cowork-plugin')
$files = @(git -C $core ls-tree --name-only origin/main -- plugins/core/lib-ps/ | ForEach-Object { Split-Path -Leaf $_ } | Where-Object { $_ -match '\.(ps1|psm1)$' })

function Get-Blob { param([string]$Repo, [string]$Ref, [string]$Path) $b = git -C $Repo rev-parse --verify --quiet "${Ref}:$Path" 2>$null; if ($LASTEXITCODE -ne 0) { return $null }; return ([string]$b).Substring(0, 10) }

$rows = foreach ($f in $files) {
    $coreBlobs = [ordered]@{}
    foreach ($r in $refs) { $coreBlobs[$r] = Get-Blob $core $r "plugins/core/lib-ps/$f" }
    $plugins = [ordered]@{}
    foreach ($name in $repos) {
        $p = Join-Path $Root $name
        $def = (git -C $p symbolic-ref --short refs/remotes/origin/HEAD).Trim()
        $b = Get-Blob $p $def "lib/$f"
        $match = @($refs | Where-Object { $coreBlobs[$_] -and $coreBlobs[$_] -eq $b })
        $plugins[$name] = if ($null -eq $b) { 'absent' } elseif ($match.Count) { "= $($match -join ',')" } else { "differs ($b)" }
    }
    [pscustomobject]@{ file = $f; core = $coreBlobs; plugins = $plugins }
}
$rows | ConvertTo-Json -Depth 5 | Out-File -LiteralPath $OutPath -Encoding utf8NoBOM
foreach ($row in $rows) {
    Write-Information ('{0}: core main={1} develop={2}' -f $row.file, $row.core['origin/main'], $row.core['origin/develop']) -InformationAction Continue
    $groups = $row.plugins.GetEnumerator() | Group-Object Value
    foreach ($g in $groups) { Write-Information ('    {0}: {1}' -f $g.Name, (($g.Group | ForEach-Object { $_.Key -replace '^mcpserver-|-plugin$', '' }) -join ', ')) -InformationAction Continue }
}
