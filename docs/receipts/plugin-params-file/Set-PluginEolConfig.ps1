#Requires -Version 7.4
<#
.SYNOPSIS
    Payton, 2026-10-19: "Configure git on all plugins to ignore line endings". Sets the local (per-repository) git
    config core.autocrlf=input in every mcpserver plugin clone on this machine: the nine checkouts under Root (their
    worktrees share that config), the installed Grok clone, and the Codex plugin cache clone.

    Why input: the churn is i/lf w/crlf (LF blobs, CRLF working files). With input, git normalizes CRLF to LF when it
    compares or stages, so those files read as clean and a CRLF rewrite can never be committed; checkout writes the
    blobs as committed (no LF to CRLF rewrite). Blobs already committed with CRLF or mixed endings are left alone by
    git's safer-autocrlf rule. Global and system config are not touched; nothing is committed or checked out.

    Writes eol-config-<host>.json (before and after per repo: local core.autocrlf, tracked-dirty count). -WhatIf
    reports the before state only.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Root = 'F:\GitHub',
    [string]$Value = 'input',
    [string]$ReceiptPath = (Join-Path $PSScriptRoot ('eol-config-{0}.json' -f $env:COMPUTERNAME))
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repos = @('mcpserver-claude-code-plugin', 'mcpserver-codex-plugin', 'mcpserver-cline-plugin', 'mcpserver-cline-v2-plugin', 'mcpserver-copilot-plugin', 'mcpserver-grok-plugin', 'mcpserver-opencode-plugin', 'mcpserver-grok-bot-plugin', 'mcpserver-claude-cowork-plugin' | ForEach-Object { Join-Path $Root $_ })
$repos += @(Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE '.grok\installed-plugins') -Directory -Filter 'mcpserver-*' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName)
$repos += @(Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE '.codex\plugins\cache\mcpserver-codex-plugin\mcpserver') -Directory -ErrorAction SilentlyContinue | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName '.git') } | Select-Object -ExpandProperty FullName)

function Get-EolState([string]$Path) {
    [ordered]@{
        autocrlf     = [string](git -C $Path config --local --get core.autocrlf)
        trackedDirty = @(git -c color.ui=never -C $Path status --porcelain --untracked-files=no).Count
        eolDirty     = @(git -c color.ui=never -C $Path ls-files --eol -m).Count
    }
}

$rows = foreach ($p in $repos) {
    if (-not (Test-Path -LiteralPath (Join-Path $p '.git'))) { throw "not a git clone: $p" }
    $row = [ordered]@{ path = $p; before = (Get-EolState $p) }
    Write-Information ('{0}: before autocrlf={1} trackedDirty={2}' -f $p, $row.before.autocrlf, $row.before.trackedDirty) -InformationAction Continue
    if ($PSCmdlet.ShouldProcess($p, "git config --local core.autocrlf $Value")) {
        git -C $p config --local core.autocrlf $Value
        if ($LASTEXITCODE -ne 0) { throw "git config failed in $p" }
        git -C $p update-index -q --refresh 2>&1 | Out-Null
        $row.after = Get-EolState $p
        Write-Information ('{0}: after autocrlf={1} trackedDirty={2}' -f $p, $row.after.autocrlf, $row.after.trackedDirty) -InformationAction Continue
    }
    [pscustomobject]$row
}
if ($WhatIfPreference) { return }
[ordered]@{ host = $env:COMPUTERNAME; value = $Value; globalAutocrlf = [string](git config --global --get core.autocrlf); at = (Get-Date -Format s); repos = @($rows) } |
    ConvertTo-Json -Depth 5 | Out-File -LiteralPath $ReceiptPath -Encoding utf8NoBOM
Write-Information "receipt: $ReceiptPath" -InformationAction Continue
