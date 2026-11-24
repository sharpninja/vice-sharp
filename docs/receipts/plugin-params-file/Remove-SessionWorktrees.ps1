#Requires -Version 7.4
<#
.SYNOPSIS
    Payton, 2026-10-19: "Make it so" (worktree cleanup). Removes only the worktrees this session created, by exact
    name: McpServer .worktrees\core-baseline-A-f66c39f9, core-baseline-B-f50cf525 and plugin-params-file-20261019, and
    in each of the nine plugin repos .worktrees\params-file-20261019 and .worktrees\base-20261019. Any other worktree
    (for example McpServer unit-green-baseline-20261019, owned by lab-wsman) is never touched.

    A worktree is removed only when it has 0 tracked changes, 0 untracked files, 0 ignored files and, on a branch,
    0 commits that are on no remote. Removal is git worktree remove without --force, so git itself also refuses a
    dirty tree. Branches are kept (all are pushed). Also reports the temp probe folder mcpcore-probe-f66c39f9.
    Writes worktree-cleanup-legion2.json. -WhatIf reports the checks only.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Root = 'F:\GitHub',
    [string]$ReceiptPath = (Join-Path $PSScriptRoot 'worktree-cleanup-legion2.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$plugins = @('mcpserver-claude-code-plugin', 'mcpserver-codex-plugin', 'mcpserver-cline-plugin', 'mcpserver-cline-v2-plugin', 'mcpserver-copilot-plugin', 'mcpserver-grok-plugin', 'mcpserver-opencode-plugin', 'mcpserver-grok-bot-plugin', 'mcpserver-claude-cowork-plugin')
$targets = @('core-baseline-A-f66c39f9', 'core-baseline-B-f50cf525', 'plugin-params-file-20261019' | ForEach-Object { [pscustomobject]@{ Repo = (Join-Path $Root 'McpServer'); Path = (Join-Path $Root "McpServer\.worktrees\$_") } })
$targets += @(foreach ($p in $plugins) { foreach ($n in 'params-file-20261019', 'base-20261019') { [pscustomobject]@{ Repo = (Join-Path $Root $p); Path = (Join-Path $Root "$p\.worktrees\$n") } } })

$rows = foreach ($t in $targets) {
    $row = [ordered]@{ path = $t.Path; existed = (Test-Path -LiteralPath $t.Path) }
    if (-not $row.existed) { $row.result = 'absent'; [pscustomobject]$row; continue }
    $registered = @(git -C $t.Repo worktree list --porcelain | Where-Object { $_ -like 'worktree *' } | ForEach-Object { ($_.Substring(9)) -replace '/', '\' })
    if ($t.Path -notin $registered) { $row.result = 'not a registered worktree; left alone'; [pscustomobject]$row; continue }
    $row.branch = (git -C $t.Path rev-parse --abbrev-ref HEAD).Trim()
    $row.head = (git -C $t.Path rev-parse --short=8 HEAD).Trim()
    $row.tracked = @(git -C $t.Path status --porcelain --untracked-files=no).Count
    $row.untracked = @(git -C $t.Path ls-files --others --exclude-standard).Count
    $row.ignored = @(git -C $t.Path ls-files --others --ignored --exclude-standard).Count
    $row.unpushed = if ($row.branch -eq 'HEAD') { 0 } else { @(git -C $t.Path log --oneline $row.branch --not --remotes).Count }
    if ($row.tracked -or $row.untracked -or $row.ignored -or $row.unpushed) { $row.result = 'kept: not clean'; [pscustomobject]$row; continue }
    if ($PSCmdlet.ShouldProcess($t.Path, 'git worktree remove')) {
        $o = git -C $t.Repo worktree remove $t.Path 2>&1 | Out-String
        $row.result = if ($LASTEXITCODE -eq 0 -and -not (Test-Path -LiteralPath $t.Path)) { 'removed' } else { "remove failed: $($o.Trim())" }
    }
    else { $row.result = 'would remove' }
    [pscustomobject]$row
}
$rows = @($rows)
foreach ($r in @($rows | Select-Object -ExpandProperty path -Unique | ForEach-Object { Split-Path (Split-Path $_ -Parent) -Parent } | Sort-Object -Unique)) { if (-not $WhatIfPreference) { git -C $r worktree prune } }
$probe = Join-Path $env:TEMP 'mcpcore-probe-f66c39f9'
$probeInfo = [ordered]@{ path = $probe; exists = (Test-Path -LiteralPath $probe) }
if ($probeInfo.exists) {
    $probeInfo.isGit = Test-Path -LiteralPath (Join-Path $probe '.git')
    $probeInfo.files = @(Get-ChildItem -LiteralPath $probe -Recurse -File -Force).Count
}
$rows | ForEach-Object { '{0}: {1}' -f $_.path, $_.result }
'probe: ' + ($probeInfo | ConvertTo-Json -Compress)
if (-not $WhatIfPreference) {
    [ordered]@{ at = (Get-Date -Format s); worktrees = $rows; probe = $probeInfo } | ConvertTo-Json -Depth 4 | Out-File -LiteralPath $ReceiptPath -Encoding utf8NoBOM
}
