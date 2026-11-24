#Requires -Version 7.4
<#
.SYNOPSIS
    Commits the params-file fix in each of the nine plugin worktrees (branch fix/params-file-20261019), pushes that
    branch, then fast-forwards origin's default branch to it (plain push: git refuses a non-fast-forward; never force).
    Only the expected paths may be staged; anything else stops the run. -WhatIf lists the staged paths per repo.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Root = 'F:\GitHub',
    [string]$WorktreeName = 'params-file-20261019',
    [string]$Branch = 'fix/params-file-20261019',
    [string]$ReceiptDir = $PSScriptRoot,
    # Commit and push the fix branch only; leave origin's default branch for a reviewed merge.
    [switch]$BranchOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repos = @('mcpserver-claude-code-plugin', 'mcpserver-codex-plugin', 'mcpserver-cline-plugin', 'mcpserver-cline-v2-plugin', 'mcpserver-copilot-plugin', 'mcpserver-grok-plugin', 'mcpserver-opencode-plugin', 'mcpserver-grok-bot-plugin', 'mcpserver-claude-cowork-plugin')
$allowed = '^(lib/|hooks/scripts/memory-context\.ps1$|tests/ReplParamsLaunch\.Tests\.ps1$|CORE-MANIFEST\.yaml$|\.version$|(\.claude-plugin/|\.codex-plugin/|\.grok-plugin/)?plugin\.json$|package\.json$|Invoke-CodexMcpPlugin\.ps1$|Invoke-CopilotMcpPlugin\.ps1$|skills/handoff/)'
$summary = @{}
foreach ($line in @(Get-Content -LiteralPath (Join-Path $ReceiptDir 'plugin-tests\summary.txt') | Where-Object { $_ -match '\[fix\]' })) { $summary[($line -split ' ')[1]] = ($line -split ' ', 2)[1] }
$template = [System.IO.File]::ReadAllText((Join-Path $ReceiptDir 'commit-msg-plugin-template.txt'))

foreach ($r in $repos) {
    $w = Join-Path $Root "$r\.worktrees\$WorktreeName"
    $default = ((git -c color.ui=never -C (Join-Path $Root $r) symbolic-ref --short refs/remotes/origin/HEAD).Trim()) -replace '^origin/', ''
    $changed = @(git -c color.ui=never -C $w status --porcelain=v1 --untracked-files=all | ForEach-Object { $_.Substring(3).Trim('"') -replace '\\', '/' })
    $bad = @($changed | Where-Object { $_ -notmatch $allowed })
    if ($bad.Count) { throw "${r}: unexpected changed paths: $($bad -join ', ')" }
    Write-Information ('{0} ({1}): {2} paths: {3}' -f $r, $default, $changed.Count, ($changed -join ', ')) -InformationAction Continue
    if (-not $PSCmdlet.ShouldProcess($r, "commit, push $Branch, fast-forward origin/$default")) { continue }

    git -C $w add -A -- .
    git -C $w diff --cached --check
    if ($LASTEXITCODE -ne 0) { throw "${r}: git diff --check failed" }
    $dash = @(git -c color.ui=never -C $w diff --cached -U0 | Select-String -Pattern '^\+.*[\u2013\u2014]')
    if ($dash.Count) { throw "${r}: added lines contain em or en dashes" }
    $msgPath = Join-Path $ReceiptDir "commit-msg-$r.txt"
    $tests = if ($summary.ContainsKey($r)) { $summary[$r] } else { 'not run' }
    [System.IO.File]::WriteAllText($msgPath, $template.Replace('{{REPO}}', $r).Replace('{{TESTS}}', $tests), [System.Text.UTF8Encoding]::new($false))
    git -C $w commit -q -F $msgPath
    if ($LASTEXITCODE -ne 0) { throw "${r}: commit failed" }
    $sha = (git -C $w rev-parse --short=8 HEAD).Trim()
    $o = git -C $w push -u origin $Branch 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw "${r}: push $Branch failed: $o" }
    if ($BranchOnly) { Write-Information ('{0}: committed {1}; pushed {2}; origin/{3} unchanged' -f $r, $sha, $Branch, $default) -InformationAction Continue; continue }
    $o = git -C $w push origin "${Branch}:$default" 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw "${r}: fast-forward of origin/$default failed (not a fast-forward?): $o" }
    $remote = ((git -C $w ls-remote origin "refs/heads/$default") -split '\s+')[0].Substring(0, 8)
    Write-Information ('{0}: committed {1}; origin/{2} now {3}' -f $r, $sha, $default, $remote) -InformationAction Continue
}
