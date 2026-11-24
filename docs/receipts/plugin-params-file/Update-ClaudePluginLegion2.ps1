#Requires -Version 7.4
<#
.SYNOPSIS
    Reinstalls the Claude Code mcpserver plugin on PAYTON-LEGION2 from the fixed default branch (main ed3586c0):
      1. tar backup (with SHA256) of the current cache mcpserver-local/mcpserver/1.119.0, which stays in place because
         this session's hooks run from it (Payton: "Back up the 1.119.0 cache (tar plus hash) before replacing it").
      2. The directory marketplace source F:\GitHub\mcpserver-claude-code-plugin is switched from its clean
         cursor/qbrain-ai-rebrand-phase2-91dd checkout to main and fast-forwarded to origin/main (the cursor branch is
         left untouched); refuses when the checkout has tracked changes.
      3. claude plugin marketplace update mcpserver-local; claude plugin update mcpserver@mcpserver-local.
      4. Verifies the installed version and the cache's lib/Invoke-McpPlugin.ps1 and lib/repl-invoke.ps1 hashes.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Checkout = 'F:\GitHub\mcpserver-claude-code-plugin',
    [string]$ExpectedHead = 'ed3586c0',
    [string]$ExpectedInvokeSha = 'AF6D1E5B686E88DE53C280EB1D7A9B10711201362614AF08C58B49C9118A0830',
    [string]$ExpectedReplSha = '93AE956EEECB13D2561621A69C88D86A1CDE32026F81445761D75536BA4EE90E',
    [string]$BackupDir = (Join-Path $env:USERPROFILE '.claude\plugins\cache-backups'),
    [string]$ReceiptPath = (Join-Path $PSScriptRoot 'claude-reinstall-legion2.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$cacheRoot = Join-Path $env:USERPROFILE '.claude\plugins\cache\mcpserver-local\mcpserver'
$old = Join-Path $cacheRoot '1.119.0'
$receipt = [ordered]@{ startedAt = (Get-Date -Format s) }

$tracked = @(git -C $Checkout status --porcelain --untracked-files=no)
if ($tracked.Count) { throw "marketplace checkout has tracked changes: $($tracked -join '; ')" }
$receipt.checkoutBranchBefore = (git -C $Checkout rev-parse --abbrev-ref HEAD).Trim()
$receipt.checkoutHeadBefore = (git -C $Checkout rev-parse --short=8 HEAD).Trim()
if ((git -C $Checkout rev-parse --short=8 origin/main).Trim() -ne $ExpectedHead) { throw "origin/main is not $ExpectedHead; fetch first" }

if (-not $PSCmdlet.ShouldProcess($Checkout, "backup $old, switch to main at $ExpectedHead, claude plugin update")) {
    Write-Information ('WhatIf: backup {0}; {1} {2}@{3} -> main@{4}; marketplace + plugin update' -f $old, $Checkout, $receipt.checkoutBranchBefore, $receipt.checkoutHeadBefore, $ExpectedHead) -InformationAction Continue
    return
}

New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
$tar = Join-Path $BackupDir ('mcpserver-local-mcpserver-1.119.0-{0}.tar' -f (Get-Date -Format 'yyyyMMddTHHmmss'))
tar -cf $tar -C $cacheRoot '1.119.0'
if ($LASTEXITCODE -ne 0) { throw "tar failed ($LASTEXITCODE)" }
$listed = @(tar -tf $tar).Count
$receipt.backup = [ordered]@{ path = $tar; sha256 = (Get-FileHash -LiteralPath $tar).Hash; entries = $listed; sourceFiles = @(Get-ChildItem -LiteralPath $old -Recurse -File -Force).Count }

git -C $Checkout switch main 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'git switch main failed' }
git -C $Checkout merge --ff-only origin/main 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'fast-forward to origin/main failed' }
$receipt.checkoutHeadAfter = (git -C $Checkout rev-parse --short=8 HEAD).Trim()
if ($receipt.checkoutHeadAfter -ne $ExpectedHead) { throw "checkout HEAD $($receipt.checkoutHeadAfter) is not $ExpectedHead" }

$receipt.marketplaceUpdate = (claude plugin marketplace update mcpserver-local 2>&1 | Out-String).Trim()
$receipt.pluginUpdate = (claude plugin update mcpserver@mcpserver-local 2>&1 | Out-String).Trim()
$receipt.pluginUpdateExit = $LASTEXITCODE

$installed = (Get-Content -LiteralPath (Join-Path $env:USERPROFILE '.claude\plugins\installed_plugins.json') -Raw | ConvertFrom-Json -AsHashtable)
$entry = @($installed['plugins']['mcpserver@mcpserver-local'])[0]
$receipt.installedVersion = $entry['version']
$receipt.installPath = $entry['installPath']
$receipt.installedSha = $entry['gitCommitSha']
$new = $entry['installPath']
$receipt.invokeSha = (Get-FileHash -LiteralPath (Join-Path $new 'lib\Invoke-McpPlugin.ps1')).Hash
$receipt.replSha = (Get-FileHash -LiteralPath (Join-Path $new 'lib\repl-invoke.ps1')).Hash
$receipt.hashesMatch = ($receipt.invokeSha -eq $ExpectedInvokeSha -and $receipt.replSha -eq $ExpectedReplSha)
$receipt.oldCacheRetained = Test-Path -LiteralPath $old
$receipt.finishedAt = Get-Date -Format s
$receipt | ConvertTo-Json -Depth 5 | Out-File -LiteralPath $ReceiptPath -Encoding utf8NoBOM
$receipt | ConvertTo-Json -Depth 5
