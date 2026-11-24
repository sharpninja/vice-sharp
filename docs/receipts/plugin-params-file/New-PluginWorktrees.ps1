#Requires -Version 7.4
<#
.SYNOPSIS
    Creates one worktree per mcpserver plugin repo, on a new branch from origin's default branch, for the
    params-file fix. Never touches the repo's main checkout. Reports base commit and whether .worktrees is
    ignored. Writes worktrees.json.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Root = 'F:\GitHub',
    [string]$Branch = 'fix/params-file-20261019',
    [string]$OutPath = (Join-Path $PSScriptRoot 'worktrees.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repos = @('mcpserver-claude-code-plugin', 'mcpserver-codex-plugin', 'mcpserver-cline-plugin', 'mcpserver-cline-v2-plugin', 'mcpserver-copilot-plugin', 'mcpserver-grok-plugin', 'mcpserver-opencode-plugin', 'mcpserver-grok-bot-plugin', 'mcpserver-claude-cowork-plugin')

$rows = foreach ($name in $repos) {
    $repo = Join-Path $Root $name
    $default = (git -c color.ui=never -C $repo symbolic-ref --short refs/remotes/origin/HEAD).Trim()
    $wt = Join-Path $repo ".worktrees\$($Branch -replace '^fix/', '')"
    $exists = Test-Path -LiteralPath $wt
    if (-not $exists -and $PSCmdlet.ShouldProcess($wt, "git worktree add -b $Branch from $default")) {
        $out = git -c color.ui=never -C $repo worktree add -b $Branch $wt $default 2>&1
        if ($LASTEXITCODE -ne 0) { throw "worktree add failed for ${name}: $out" }
    }
    git -C $repo check-ignore -q '.worktrees/probe' 2>$null
    $ignored = ($LASTEXITCODE -eq 0)
    $head = if (Test-Path -LiteralPath $wt) { (git -c color.ui=never -C $wt rev-parse --short=8 HEAD).Trim() } else { $null }
    [pscustomobject]@{ repo = $name; defaultBranch = $default; baseHead = (git -c color.ui=never -C $repo rev-parse --short=8 $default).Trim(); worktree = $wt; head = $head; worktreeDirIgnored = $ignored }
}
$rows | ConvertTo-Json -Depth 3 | Out-File -LiteralPath $OutPath -Encoding utf8NoBOM -WhatIf:$false
$rows | Format-Table repo, defaultBranch, baseHead, head, worktreeDirIgnored -AutoSize | Out-String -Width 200
