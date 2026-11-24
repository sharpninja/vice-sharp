#Requires -Version 7.4
<#
.SYNOPSIS
    Payton, 2026-10-19: "Make it so" (the Codex, Grok and Cline v2 reinstalls on LEGION2). This script does Codex and
    Grok; Cline v2 needs an npm build of dist/ and is reported, not done.

    Backups first, originals retained (profile: copy, verify, retain the original until cleared to delete):
      tar + SHA256 + entry count of the Codex cache mcpserver-codex-plugin/mcpserver/1.118.0 (8 hand-edited files),
      each of the 35 disabled mcpserver-local caches (1.85.0 to 1.119.0), and the Grok installed clone. Nothing
      is deleted. Tars go outside the scanned plugin folders.

    Codex: the local marketplace source F:\GitHub\mcpserver-codex-plugin is switched from its clean cursor branch to
    main and fast-forwarded to origin/main (90301034); then codex plugin add mcpserver@mcpserver-codex-plugin.
    Grok: the local main ref of F:\GitHub\mcpserver-grok-plugin (the installed clone's origin) is fast-forwarded to
    origin/main (455590f0) without touching its checkout; then grok plugin update mcpserver.
    Both: the installed lib hashes must equal the published fix. Writes codex-grok-reinstall-legion2.json.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Root = 'F:\GitHub',
    [string]$CodexHead = '90301034',
    [string]$GrokHead = '455590f0',
    [string]$ExpectedInvokeSha = 'AF6D1E5B686E88DE53C280EB1D7A9B10711201362614AF08C58B49C9118A0830',
    [string]$ExpectedReplSha = '93AE956EEECB13D2561621A69C88D86A1CDE32026F81445761D75536BA4EE90E',
    [string]$ReceiptPath = (Join-Path $PSScriptRoot 'codex-grok-reinstall-legion2.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$u = $env:USERPROFILE
$stamp = Get-Date -Format 'yyyyMMddTHHmmss'
$receipt = [ordered]@{ startedAt = (Get-Date -Format s); backups = @() }
$cx = Join-Path $Root 'mcpserver-codex-plugin'
$gk = Join-Path $Root 'mcpserver-grok-plugin'
$codexCache = Join-Path $u '.codex\plugins\cache'
$grokInstalled = @(Get-ChildItem -LiteralPath (Join-Path $u '.grok\installed-plugins') -Directory -Filter 'mcpserver-grok-plugin-*')
if ($grokInstalled.Count -ne 1) { throw "expected one Grok installed clone, found $($grokInstalled.Count)" }
$grokClone = $grokInstalled[0].FullName

foreach ($p in $cx, $gk, $grokClone) { if (@(git -C $p status --porcelain --untracked-files=no).Count) { throw "tracked changes in $p" } }
if ((git -C $cx rev-parse --short=8 origin/main).Trim() -ne $CodexHead) { throw "codex origin/main is not $CodexHead" }
if ((git -C $gk rev-parse --short=8 origin/main).Trim() -ne $GrokHead) { throw "grok origin/main is not $GrokHead" }
$targets = @(
    [pscustomobject]@{ Label = 'mcpserver-codex-plugin-mcpserver'; Base = (Join-Path $codexCache 'mcpserver-codex-plugin\mcpserver'); Name = '1.118.0'; Dest = (Join-Path $u '.codex\plugins\cache-backups') }
) + @(Get-ChildItem -LiteralPath (Join-Path $codexCache 'mcpserver-local\mcpserver') -Directory | ForEach-Object { [pscustomobject]@{ Label = 'mcpserver-local-mcpserver'; Base = $_.Parent.FullName; Name = $_.Name; Dest = (Join-Path $u '.codex\plugins\cache-backups\mcpserver-local') } }) + @(
    [pscustomobject]@{ Label = 'grok-installed'; Base = (Split-Path $grokClone -Parent); Name = (Split-Path $grokClone -Leaf); Dest = (Join-Path $u '.grok\plugin-backups') })
Write-Information ('backups planned: {0} (codex 1.118.0, {1} mcpserver-local caches, grok clone {2})' -f $targets.Count, ($targets.Count - 2), (Split-Path $grokClone -Leaf)) -InformationAction Continue
if (-not $PSCmdlet.ShouldProcess('Codex and Grok plugins on PAYTON-LEGION2', 'tar backups, Codex switch/ff + plugin add, Grok main ff + plugin update')) { return }

foreach ($t in $targets) {
    New-Item -ItemType Directory -Force -Path $t.Dest | Out-Null
    $tar = Join-Path $t.Dest ('{0}-{1}-{2}.tar' -f $t.Label, $t.Name, $stamp)
    tar -cf $tar -C $t.Base $t.Name
    if ($LASTEXITCODE -ne 0) { throw "tar failed for $($t.Base)\$($t.Name)" }
    # tar lists the top folder itself plus every file and subfolder; Get-ChildItem -Recurse lists all but the top.
    $src = @(Get-ChildItem -LiteralPath (Join-Path $t.Base $t.Name) -Recurse -Force).Count
    $entries = @(tar -tf $tar).Count - 1
    if ($entries -ne $src) { throw "tar entry count $entries does not match source item count $src for $tar" }
    $receipt.backups += [ordered]@{ source = (Join-Path $t.Base $t.Name); tar = $tar; sha256 = (Get-FileHash -LiteralPath $tar).Hash; entries = $entries; originalRetained = (Test-Path -LiteralPath (Join-Path $t.Base $t.Name)) }
}
Write-Information ('backups done: {0}, all originals retained' -f $receipt.backups.Count) -InformationAction Continue

# Codex
$receipt.codexCheckoutBefore = '{0}@{1}' -f (git -C $cx rev-parse --abbrev-ref HEAD).Trim(), (git -C $cx rev-parse --short=8 HEAD).Trim()
git -C $cx switch main 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'codex: git switch main failed' }
git -C $cx merge --ff-only origin/main 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'codex: fast-forward failed' }
$receipt.codexCheckoutAfter = 'main@{0}' -f (git -C $cx rev-parse --short=8 HEAD).Trim()
$receipt.codexAdd = (codex plugin add mcpserver@mcpserver-codex-plugin 2>&1 | Out-String).Trim()
$receipt.codexAddExit = $LASTEXITCODE
$cdirs = @(Get-ChildItem -LiteralPath (Join-Path $codexCache 'mcpserver-codex-plugin\mcpserver') -Directory | Select-Object -ExpandProperty Name)
$receipt.codexCacheVersions = $cdirs
$new = Join-Path $codexCache 'mcpserver-codex-plugin\mcpserver\1.121.0'
$receipt.codexInvokeSha = if (Test-Path -LiteralPath "$new\lib\Invoke-McpPlugin.ps1") { (Get-FileHash -LiteralPath "$new\lib\Invoke-McpPlugin.ps1").Hash } else { 'missing' }
$receipt.codexReplSha = if (Test-Path -LiteralPath "$new\lib\repl-invoke.ps1") { (Get-FileHash -LiteralPath "$new\lib\repl-invoke.ps1").Hash } else { 'missing' }
$receipt.codexHashesMatch = ($receipt.codexInvokeSha -eq $ExpectedInvokeSha -and $receipt.codexReplSha -eq $ExpectedReplSha)

# Grok
$receipt.grokMainBefore = (git -C $gk rev-parse --short=8 main).Trim()
git -C $gk fetch . refs/remotes/origin/main:refs/heads/main 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'grok: fast-forward of the local main ref failed' }
$receipt.grokMainAfter = (git -C $gk rev-parse --short=8 main).Trim()
$receipt.grokUpdate = (grok plugin update mcpserver 2>&1 | Out-String).Trim()
$receipt.grokUpdateExit = $LASTEXITCODE
$after = @(Get-ChildItem -LiteralPath (Join-Path $u '.grok\installed-plugins') -Directory -Filter 'mcpserver-grok-plugin-*' | Select-Object -ExpandProperty FullName)
$receipt.grokInstalled = $after
$gc = $after[0]
$receipt.grokCloneHead = (git -C $gc rev-parse --short=8 HEAD).Trim()
$receipt.grokInvokeSha = (Get-FileHash -LiteralPath "$gc\lib\Invoke-McpPlugin.ps1").Hash
$receipt.grokReplSha = (Get-FileHash -LiteralPath "$gc\lib\repl-invoke.ps1").Hash
$receipt.grokHashesMatch = ($receipt.grokInvokeSha -eq $ExpectedInvokeSha -and $receipt.grokReplSha -eq $ExpectedReplSha)
$receipt.grokDirty = @(git -C $gc status --porcelain --untracked-files=no).Count
$receipt.finishedAt = Get-Date -Format s
$receipt | ConvertTo-Json -Depth 5 | Out-File -LiteralPath $ReceiptPath -Encoding utf8NoBOM
$receipt | ConvertTo-Json -Depth 5
