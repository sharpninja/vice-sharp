#Requires -Version 7.4
<#
.SYNOPSIS
    Read-only: line-ending state of every mcpserver plugin clone on this machine (the nine checkouts under Root and the
    installed Grok clone): core.autocrlf at local and global scope, the committed .gitattributes, how many index blobs are
    CRLF or mixed (git ls-files --eol), and how many working-tree files show as modified.
#>
[CmdletBinding()]
param([string]$Root = 'F:\GitHub')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$clones = @('mcpserver-claude-code-plugin', 'mcpserver-codex-plugin', 'mcpserver-cline-plugin', 'mcpserver-cline-v2-plugin', 'mcpserver-copilot-plugin', 'mcpserver-grok-plugin', 'mcpserver-opencode-plugin', 'mcpserver-grok-bot-plugin', 'mcpserver-claude-cowork-plugin' | ForEach-Object { Join-Path $Root $_ })
$clones += @(Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE '.grok\installed-plugins') -Directory -Filter 'mcpserver-*' -ErrorAction SilentlyContinue | ForEach-Object FullName)
Write-Information ('global core.autocrlf={0}' -f (git config --global --get core.autocrlf)) -InformationAction Continue
foreach ($p in $clones) {
    $eol = @(git -c color.ui=never -C $p ls-files --eol)
    $crlf = @($eol | Where-Object { $_ -match '^i/crlf' }).Count
    $mixed = @($eol | Where-Object { $_ -match '^i/mixed' }).Count
    $attr = 'none'
    git -C $p cat-file -e 'HEAD:.gitattributes' 2>$null
    if ($LASTEXITCODE -eq 0) { $attr = (@(git -C $p show 'HEAD:.gitattributes') -join ' | ') }
    $dirty = @(git -c color.ui=never -C $p status --porcelain --untracked-files=no).Count
    Write-Information ('{0}: local autocrlf={1}; files={2}; crlf blobs={3}; mixed blobs={4}; tracked-dirty={5}; .gitattributes={6}' -f $p, (git -C $p config --local --get core.autocrlf), $eol.Count, $crlf, $mixed, $dirty, $attr) -InformationAction Continue
}
