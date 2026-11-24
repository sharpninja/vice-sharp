#Requires -Version 7.4
<#
.SYNOPSIS
    Prepares the nine mcpserver plugin worktrees (branch fix/params-file-20261019) for the params-file fix:
      1. Syncs McpServer plugins/core at the core worktree HEAD (8768de8a) with sync-plugin-core.ps1.
      2. grok-bot: re-applies its GrokBot identity fork on top of the new core with git merge-file
         (base = pre-sync standard copy, other = pre-sync grok-bot copy, current = synced core); a conflict stops.
      3. Moves the host launch sites to the params-file transfer (Update-HostLaunchSites.ps1).
      4. Adds tests/ReplParamsLaunch.Tests.ps1.
      5. Bumps the plugin version (1.120.0 -> 1.121.0; grok-bot 1.118.0 -> 1.119.0) in every version file, editing
         JSON through ConvertFrom-Json/ConvertTo-Json objects and .version as a single value.
    -WhatIf reports the plan and the fork merge inputs without changing a worktree.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Root = 'F:\GitHub',
    [string]$CoreWorktree = 'F:\GitHub\McpServer\.worktrees\plugin-params-file-20261019',
    [string]$WorktreeName = 'params-file-20261019',
    [string]$ReceiptDir = $PSScriptRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repos = @('mcpserver-claude-code-plugin', 'mcpserver-codex-plugin', 'mcpserver-cline-plugin', 'mcpserver-cline-v2-plugin', 'mcpserver-copilot-plugin', 'mcpserver-grok-plugin', 'mcpserver-opencode-plugin', 'mcpserver-grok-bot-plugin', 'mcpserver-claude-cowork-plugin')
$forkFiles = @('Invoke-McpPlugin.ps1', 'plugin-hook.ps1', 'repl-invoke.ps1', 'agent-runtime-header.ps1', 'plugin-env.ps1', 'resolve-cache-dir.ps1')
$sync = Join-Path $CoreWorktree 'plugins\core\sync\sync-plugin-core.ps1'
$coreHead = (git -C $CoreWorktree rev-parse --short=8 HEAD).Trim()
$wt = @{}; foreach ($r in $repos) { $wt[$r] = Join-Path $Root "$r\.worktrees\$WorktreeName" }

# Fork inputs captured before anything is synced.
$forkDir = Join-Path $ReceiptDir 'grok-bot-fork'
if ($PSCmdlet.ShouldProcess($forkDir, 'capture grok-bot fork merge inputs')) {
    foreach ($kind in 'base-standard', 'other-grokbot') { New-Item -ItemType Directory -Force -Path (Join-Path $forkDir $kind) | Out-Null }
    foreach ($f in $forkFiles) {
        $std = Join-Path $wt['mcpserver-claude-code-plugin'] "lib\$f"
        $gb = Join-Path $wt['mcpserver-grok-bot-plugin'] "lib\$f"
        if (-not (Test-Path (Join-Path $forkDir "base-standard\$f"))) { Copy-Item -LiteralPath $std -Destination (Join-Path $forkDir "base-standard\$f") }
        if (-not (Test-Path (Join-Path $forkDir "other-grokbot\$f"))) { Copy-Item -LiteralPath $gb -Destination (Join-Path $forkDir "other-grokbot\$f") }
    }
}

$report = [System.Collections.Generic.List[object]]::new()
foreach ($r in $repos) {
    $w = $wt[$r]
    $row = [ordered]@{ repo = $r; core = $coreHead; versionFrom = (Get-Content -LiteralPath (Join-Path $w '.version') -Raw).Trim() }
    $row.versionTo = if ($r -eq 'mcpserver-grok-bot-plugin') { '1.119.0' } else { '1.121.0' }
    if (-not $PSCmdlet.ShouldProcess($w, "sync core $coreHead, host sites, test, version $($row.versionFrom) -> $($row.versionTo)")) { $report.Add([pscustomobject]$row); continue }

    $out = & (Get-Command pwsh).Source -NoLogo -NoProfile -NonInteractive -File $sync -PluginRoot $w 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw "sync failed for ${r}: $out" }
    $row.sync = $out.Trim()

    if ($r -eq 'mcpserver-grok-bot-plugin') {
        $merged = @()
        foreach ($f in $forkFiles) {
            $current = Join-Path $w "lib\$f"
            git merge-file -L synced-core -L standard-before -L grokbot-before $current (Join-Path $forkDir "base-standard\$f") (Join-Path $forkDir "other-grokbot\$f")
            if ($LASTEXITCODE -ne 0) { throw "grok-bot fork merge conflict in $f (exit $LASTEXITCODE)" }
            $merged += $f
        }
        $row.forkMerged = $merged
    }

    Copy-Item -LiteralPath (Join-Path $ReceiptDir 'ReplParamsLaunch.Tests.ps1') -Destination (Join-Path $w 'tests\ReplParamsLaunch.Tests.ps1') -Force

    $versionFiles = @(git -C $w ls-files -- '.claude-plugin/plugin.json' '.codex-plugin/plugin.json' '.grok-plugin/plugin.json' 'plugin.json' 'package.json')
    foreach ($vf in $versionFiles) {
        $p = Join-Path $w $vf
        $obj = Get-Content -LiteralPath $p -Raw | ConvertFrom-Json -Depth 50
        if ([string]$obj.version -ne $row.versionFrom) { throw "$r ${vf}: version $($obj.version), expected $($row.versionFrom)" }
        $obj.version = $row.versionTo
        [System.IO.File]::WriteAllText($p, (($obj | ConvertTo-Json -Depth 50) + "`n"), [System.Text.UTF8Encoding]::new($false))
    }
    [System.IO.File]::WriteAllText((Join-Path $w '.version'), $row.versionTo + "`n", [System.Text.UTF8Encoding]::new($false))
    $row.versionFiles = $versionFiles
    $report.Add([pscustomobject]$row)
}

if ($PSCmdlet.ShouldProcess($ReceiptDir, 'host launch sites')) { & (Join-Path $ReceiptDir 'Update-HostLaunchSites.ps1') -Root $Root -WorktreeName $WorktreeName }
$report | ConvertTo-Json -Depth 5 | Out-File -LiteralPath (Join-Path $ReceiptDir 'sync-report.json') -Encoding utf8NoBOM -WhatIf:$false
$report | ForEach-Object { '{0}: core {1}, version {2} -> {3}' -f $_.repo, $_.core, $_.versionFrom, $_.versionTo }
