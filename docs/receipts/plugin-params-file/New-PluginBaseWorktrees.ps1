#Requires -Version 7.4
<#
.SYNOPSIS
    Creates a detached worktree per plugin repo at origin's default-branch head (the pre-fix base), for failure
    attribution of the plugin test suites. Read-only use; removed after the comparison.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Root = 'F:\GitHub',
    [string]$WorktreeName = 'base-20261019'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repos = @('mcpserver-claude-code-plugin', 'mcpserver-codex-plugin', 'mcpserver-cline-plugin', 'mcpserver-cline-v2-plugin', 'mcpserver-copilot-plugin', 'mcpserver-grok-plugin', 'mcpserver-opencode-plugin', 'mcpserver-grok-bot-plugin', 'mcpserver-claude-cowork-plugin')
foreach ($r in $repos) {
    $repo = Join-Path $Root $r
    $def = (git -c color.ui=never -C $repo symbolic-ref --short refs/remotes/origin/HEAD).Trim()
    $wt = Join-Path $repo ".worktrees\$WorktreeName"
    if (-not (Test-Path -LiteralPath $wt) -and $PSCmdlet.ShouldProcess($wt, "detached worktree at $def")) {
        $o = git -c color.ui=never -C $repo worktree add --detach $wt $def 2>&1
        if ($LASTEXITCODE -ne 0) { throw "worktree add failed for ${r}: $o" }
    }
    Write-Information ('{0}: {1} at {2}' -f $r, $wt, (git -C $wt rev-parse --short=8 HEAD 2>$null)) -InformationAction Continue
}
