#Requires -Version 7.0
<#
.SYNOPSIS
PLAN-NATIVERESTORE-001 D4 (A03, A04, F07, F08, R1 B, R4): restores the tracked files of the native/vice submodule to
the pinned commit from a D0 preservation receipt, one listed path at a time, after proving identity and state.

.DESCRIPTION
Before the first overwrite the script verifies: the receipt exists and parses; the receipt host equals -HostName;
the receipt's submodule path equals the resolved submodule path; the submodule has a git dir; the superproject's
recorded gitlink and the submodule's checked-out HEAD both equal the receipt's SHAs; every preserved copy re-hashes to
the receipt; the submodule index is clean; and every listed path is either untouched (live bytes hash to the preserved
copy, so the restore is the one planned) or already restored (git diff reports it unchanged). Any other state stops
without overwriting anything (a changed live delta, staged drift, a missing or corrupt receipt, the wrong repository,
the wrong host, a moved gitlink). The untracked inventory must equal the receipt before the restore starts.

The restore is "git -C <submodule> checkout -- <path>" for each untouched path (never reset --hard, never clean).
Afterwards it proves: git diff --exit-code and git diff --cached --exit-code empty; status --porcelain=v1 -uall equal
to the receipt's untracked entries by path, hash and length (none new, deleted or changed); the superproject status
shows the submodule clean; the gitlink equals the checked-out SHA; and the preserved copies still re-hash. Resume
rule: a rerun after an interruption classifies each path from verified bytes and restores only the untouched ones.
Supports -WhatIf (classification and proofs run, nothing is written).
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string] $SuperprojectRoot,
    [Parameter(Mandatory)][string] $SubmoduleRelativePath,
    [Parameter(Mandatory)][string] $ReceiptDirectory,
    [Parameter(Mandatory)][string] $HostName,
    [string] $ReportPath
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-GitOutput {
    param([string] $Root, [string[]] $Arguments)
    $output = & git -C $Root @Arguments 2>&1 | ForEach-Object { [string]$_ }
    if ($LASTEXITCODE -ne 0) { throw ('git ' + ($Arguments -join ' ') + ' failed in ' + $Root + ': ' + ($output -join ' | ')) }
    return @($output)
}

function Get-Sha256 {
    param([string] $Path)
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Get-UntrackedInventory {
    param([string] $Root)
    $rows = @(& git -C $Root status --porcelain=v1 -uall --untracked-files=all | Where-Object { $_ -ne '' })
    if ($LASTEXITCODE -ne 0) { throw "git status failed in $Root" }
    $tracked = @(); $untracked = @{}
    foreach ($row in $rows) {
        $code = $row.Substring(0, 2); $path = $row.Substring(3)
        if ($path.StartsWith('"') -and $path.EndsWith('"')) { $path = $path.Substring(1, $path.Length - 2) }
        if ($code -eq '??') {
            $full = [IO.Path]::Combine($Root, $path)
            $sha = $null; $length = 0
            if (Test-Path -LiteralPath $full -PathType Leaf) { $length = (Get-Item -LiteralPath $full -Force).Length; $sha = Get-Sha256 -Path $full }
            $untracked[$path] = [pscustomobject]@{ sha256 = $sha; length = $length }
        }
        else { $tracked += [pscustomobject]@{ path = $path; status = $code } }
    }
    return [pscustomobject]@{ tracked = $tracked; untracked = $untracked }
}

function Compare-UntrackedInventory {
    param([hashtable] $Baseline, [hashtable] $Current)
    $added = @($Current.Keys | Where-Object { -not $Baseline.ContainsKey($_) } | Sort-Object)
    $deleted = @($Baseline.Keys | Where-Object { -not $Current.ContainsKey($_) } | Sort-Object)
    $changed = @()
    foreach ($k in ($Current.Keys | Where-Object { $Baseline.ContainsKey($_) } | Sort-Object)) {
        if ([string]$Baseline[$k].sha256 -ne [string]$Current[$k].sha256 -or [long]$Baseline[$k].length -ne [long]$Current[$k].length) { $changed += $k }
    }
    return [pscustomobject]@{ added = $added; deleted = $deleted; changed = $changed; equal = ($added.Count -eq 0 -and $deleted.Count -eq 0 -and $changed.Count -eq 0) }
}

$stops = [Collections.Generic.List[string]]::new()
$paths = [Collections.Generic.List[object]]::new()
$superRoot = [IO.Path]::GetFullPath($SuperprojectRoot)
$subRoot = [IO.Path]::GetFullPath([IO.Path]::Combine($superRoot, $SubmoduleRelativePath))
$manifestPath = [IO.Path]::Combine($ReceiptDirectory, 'manifest.json')
$manifest = $null

# Identity and receipt checks (A04), each recorded, none skipped.
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { $stops.Add('receipt missing: ' + $manifestPath) }
else {
    try { $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json }
    catch { $stops.Add('receipt corrupt: ' + $_.Exception.Message) }
}
if ($null -ne $manifest) {
    foreach ($required in @('host', 'submodulePath', 'gitlinkSha', 'checkedOutSha', 'entries', 'preserved')) {
        if (-not $manifest.PSObject.Properties[$required]) { $stops.Add('receipt corrupt: missing ' + $required) }
    }
}
if ($stops.Count -eq 0) {
    if ([string]$manifest.host -cne $HostName) { $stops.Add("receipt host '$($manifest.host)' is not the running host '$HostName'") }
    $receiptSub = [IO.Path]::GetFullPath([string]$manifest.submodulePath).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if ($receiptSub -ne $subRoot.TrimEnd([IO.Path]::DirectorySeparatorChar)) { $stops.Add("receipt submodule path '$receiptSub' is not the resolved '$subRoot' (wrong repository)") }
    if (-not (Test-Path -LiteralPath $subRoot -PathType Container)) { $stops.Add('submodule directory missing: ' + $subRoot) }
    else {
        $gitDir = @(& git -C $subRoot rev-parse --git-dir 2>&1 | ForEach-Object { [string]$_ })
        if ($LASTEXITCODE -ne 0) { $stops.Add('submodule has no git dir: ' + ($gitDir -join ' ')) }
    }
}
$gitlink = $null; $head = $null
if ($stops.Count -eq 0) {
    $lsTree = @(Invoke-GitOutput -Root $superRoot -Arguments @('ls-tree', 'HEAD', '--', $SubmoduleRelativePath))
    if ($lsTree.Count -ne 1 -or $lsTree[0] -notmatch '^160000 commit ([0-9a-f]{40})') { $stops.Add('superproject gitlink not found for ' + $SubmoduleRelativePath) }
    else { $gitlink = $Matches[1] }
    $head = ([string]@(Invoke-GitOutput -Root $subRoot -Arguments @('rev-parse', 'HEAD'))[0]).Trim()
    if ($gitlink -and $gitlink -ne [string]$manifest.gitlinkSha) { $stops.Add("gitlink $gitlink is not the receipt's $($manifest.gitlinkSha)") }
    if ($head -ne [string]$manifest.checkedOutSha) { $stops.Add("checked-out HEAD $head is not the receipt's $($manifest.checkedOutSha) (gitlink or checkout moved)") }
    if ($gitlink -and $gitlink -ne $head) { $stops.Add("gitlink $gitlink differs from checked-out HEAD $head") }
}

# Preserved copies re-hash before anything else (corrupt receipt control).
$preservedChecks = @()
if ($stops.Count -eq 0) {
    foreach ($p in @($manifest.preserved)) {
        $copy = [IO.Path]::Combine($ReceiptDirectory, ([string]$p.copyPath).Replace('/', [IO.Path]::DirectorySeparatorChar))
        $ok = $false; $actual = $null
        if (Test-Path -LiteralPath $copy -PathType Leaf) { $actual = Get-Sha256 -Path $copy; $ok = ($actual -eq [string]$p.sha256) -and ((Get-Item -LiteralPath $copy).Length -eq [long]$p.length) }
        $preservedChecks += [pscustomobject]@{ path = $p.path; copyPath = $p.copyPath; expected = $p.sha256; actual = $actual; ok = $ok }
        if (-not $ok) { $stops.Add('preserved copy does not match the receipt: ' + $p.copyPath) }
    }
}

# Index and untracked inventory before any overwrite (F07, F08).
$baselineUntracked = @{}
$inventoryBefore = $null
if ($stops.Count -eq 0) {
    & git -C $subRoot diff --cached --exit-code --quiet 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { $stops.Add('submodule index is not clean (staged drift)') }
    foreach ($e in @($manifest.entries)) { if ([string]$e.status -eq '??') { $baselineUntracked[[string]$e.path] = [pscustomobject]@{ sha256 = $e.sha256; length = $e.length } } }
    $current = Get-UntrackedInventory -Root $subRoot
    $inventoryBefore = Compare-UntrackedInventory -Baseline $baselineUntracked -Current $current.untracked
    if (-not $inventoryBefore.equal) { $stops.Add('untracked inventory differs from the receipt before the restore: added ' + $inventoryBefore.added.Count + ', deleted ' + $inventoryBefore.deleted.Count + ', changed ' + $inventoryBefore.changed.Count) }
    foreach ($t in $current.tracked) {
        if ($t.status.Trim() -ne 'M') { $stops.Add('unexpected tracked status ' + $t.status + ' on ' + $t.path) }
        elseif (@($manifest.preserved | Where-Object { [string]$_.path -eq $t.path }).Count -eq 0) { $stops.Add('modified tracked path not in the receipt: ' + $t.path) }
    }
}

# Per-path classification from verified bytes (F07, resume rule).
if ($stops.Count -eq 0) {
    foreach ($p in @($manifest.preserved)) {
        $live = [IO.Path]::Combine($subRoot, ([string]$p.path).Replace('/', [IO.Path]::DirectorySeparatorChar))
        & git -C $subRoot diff --exit-code --quiet -- $p.path 2>&1 | Out-Null
        $unchanged = ($LASTEXITCODE -eq 0)
        $liveSha = if (Test-Path -LiteralPath $live -PathType Leaf) { Get-Sha256 -Path $live } else { $null }
        $state = if ($unchanged) { 'restored' } elseif ($liveSha -eq [string]$p.sha256) { 'untouched' } else { 'unexpected' }
        $paths.Add([pscustomobject]@{ path = $p.path; state = $state; liveSha256 = $liveSha; preservedSha256 = $p.sha256 })
        if ($state -eq 'unexpected') { $stops.Add('live bytes of ' + $p.path + ' match neither the preserved copy nor the pinned commit (changed live delta)') }
    }
}

$restored = @()
$proofs = [ordered]@{}
if ($stops.Count -eq 0) {
    $toRestore = @($paths | Where-Object { $_.state -eq 'untouched' } | ForEach-Object { $_.path })
    if ($PSCmdlet.ShouldProcess($subRoot, ('git checkout -- ' + ($toRestore -join ', ')))) {
        foreach ($rel in $toRestore) {
            Invoke-GitOutput -Root $subRoot -Arguments @('checkout', '--', $rel) | Out-Null
            $restored += $rel
        }
        # Proofs after the restore.
        & git -C $subRoot diff --exit-code --quiet 2>&1 | Out-Null; $proofs.diffEmpty = ($LASTEXITCODE -eq 0)
        & git -C $subRoot diff --cached --exit-code --quiet 2>&1 | Out-Null; $proofs.cachedDiffEmpty = ($LASTEXITCODE -eq 0)
        $after = Get-UntrackedInventory -Root $subRoot
        $inventoryAfter = Compare-UntrackedInventory -Baseline $baselineUntracked -Current $after.untracked
        $proofs.trackedEntriesAfter = @($after.tracked).Count
        $proofs.untrackedEqual = $inventoryAfter.equal
        $proofs.untrackedAdded = $inventoryAfter.added; $proofs.untrackedDeleted = $inventoryAfter.deleted; $proofs.untrackedChanged = $inventoryAfter.changed
        $superStatus = @(Invoke-GitOutput -Root $superRoot -Arguments @('status', '--porcelain=v1', '--', $SubmoduleRelativePath))
        $proofs.superprojectSubmoduleClean = ($superStatus.Count -eq 0)
        $proofs.superprojectStatusLines = $superStatus
        $headAfter = ([string]@(Invoke-GitOutput -Root $subRoot -Arguments @('rev-parse', 'HEAD'))[0]).Trim()
        $proofs.gitlinkEqualsHead = ($gitlink -eq $headAfter)
        $proofs.preservedStillMatch = (@($manifest.preserved | Where-Object { (Get-Sha256 -Path ([IO.Path]::Combine($ReceiptDirectory, ([string]$_.copyPath).Replace('/', [IO.Path]::DirectorySeparatorChar)))) -ne [string]$_.sha256 }).Count -eq 0)
        foreach ($k in @('diffEmpty', 'cachedDiffEmpty', 'untrackedEqual', 'superprojectSubmoduleClean', 'gitlinkEqualsHead', 'preservedStillMatch')) {
            if (-not $proofs[$k]) { $stops.Add('post-restore proof failed: ' + $k) }
        }
        if ($proofs.trackedEntriesAfter -ne 0) { $stops.Add('post-restore proof failed: tracked entries remain ' + $proofs.trackedEntriesAfter) }
    }
    else {
        $proofs.dryRun = $true
    }
}

$outcome = if ($stops.Count -eq 0) { if ($proofs.Contains('dryRun')) { 'DryRun' } else { 'Complete' } } else { 'Stopped' }
$report = [pscustomobject]@{
    host = $HostName
    superprojectRoot = $superRoot
    submodulePath = $subRoot
    receiptDirectory = $ReceiptDirectory
    gitlink = $gitlink
    checkedOutSha = $head
    preservedChecks = @($preservedChecks)
    inventoryBefore = $inventoryBefore
    paths = @($paths)
    restored = @($restored)
    proofs = $proofs
    stops = @($stops)
    outcome = $outcome
}
if ($ReportPath -and $PSCmdlet.ShouldProcess($ReportPath, 'write report')) {
    New-Item -ItemType Directory -Path (Split-Path -Parent $ReportPath) -Force | Out-Null
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $ReportPath -Encoding utf8
}
Write-Output ('restore ' + $outcome + ': paths ' + $paths.Count + ' (untouched ' + @($paths | Where-Object { $_.state -eq 'untouched' }).Count + ', restored before ' + @($paths | Where-Object { $_.state -eq 'restored' }).Count + ', unexpected ' + @($paths | Where-Object { $_.state -eq 'unexpected' }).Count + '), restored now ' + $restored.Count + ', stops ' + $stops.Count)
foreach ($p in $paths) { Write-Output ('  ' + $p.state.PadRight(10) + ' ' + $p.path) }
foreach ($s in $stops) { Write-Output ('  STOP ' + $s) }
foreach ($k in $proofs.Keys) { if ($k -notlike 'superprojectStatusLines' -and $k -notlike 'untracked*ed') { Write-Output ('  proof ' + $k + ' = ' + $proofs[$k]) } }
Write-Output $report
if ($stops.Count -gt 0) { throw 'Restore stopped; nothing beyond the listed restored paths was written.' }
