#Requires -Version 7.4
<#
.SYNOPSIS
    Read-only (git fetch only): for McpServer and the nine plugin repos on this machine, reports the local default
    branch and, for McpServer, fix/plugin-params-file-20261019, against origin: commits ahead and behind.
#>
[CmdletBinding()]
param([string]$Root = 'F:\GitHub')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repos = @('McpServer', 'mcpserver-claude-code-plugin', 'mcpserver-codex-plugin', 'mcpserver-cline-plugin', 'mcpserver-cline-v2-plugin', 'mcpserver-copilot-plugin', 'mcpserver-grok-plugin', 'mcpserver-opencode-plugin', 'mcpserver-grok-bot-plugin', 'mcpserver-claude-cowork-plugin')
foreach ($r in $repos) {
    $p = Join-Path $Root $r
    git -C $p fetch -q origin 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { "${r}: fetch failed"; continue }
    $default = ((git -c color.ui=never -C $p symbolic-ref --short refs/remotes/origin/HEAD).Trim()) -replace '^origin/', ''
    $branches = @($default)
    if ($r -eq 'McpServer') { $branches += 'fix/plugin-params-file-20261019' }
    foreach ($b in $branches) {
        git -C $p rev-parse -q --verify "refs/heads/$b" 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) { '{0} {1}: no local branch (origin {2})' -f $r, $b, (git -C $p rev-parse --short=8 "origin/$b"); continue }
        $counts = (git -C $p rev-list --left-right --count "$b...origin/$b").Trim() -split '\s+'
        '{0} {1}: local {2} origin {3} ahead {4} behind {5}' -f $r, $b, (git -C $p rev-parse --short=8 $b), (git -C $p rev-parse --short=8 "origin/$b"), $counts[0], $counts[1]
    }
}
