#Requires -Version 7.4
<#
.SYNOPSIS
    Read-only. Phase B step 1 (OMAR-VICE# brief 2026-10-19T19:43): repins the 826 original inputs of the Opus
    remediation packet (docs/receipts/hv/20261008T143538-0500-opus-remediation-packet/final-old-input-revalidation.json)
    against the current files. For every input: original hash (Oct 8 review pin), the Oct 8 revalidation hash, the
    current hash, and changed or unchanged against the original. For a changed input inside the repository: the commits
    since the packet (2026-10-08T14:35:38-05:00) that touched it, and whether the working tree differs from HEAD. Inputs
    outside the repository are reported with location only. Writes pin-diff.json and pin-diff.txt here.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = 'F:\GitHub\vice-sharp',
    [string]$Since = '2026-10-08T14:35:38-05:00'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$pins = @(Get-Content -LiteralPath (Join-Path $RepoRoot 'docs\receipts\hv\20261008T143538-0500-opus-remediation-packet\final-old-input-revalidation.json') -Raw | ConvertFrom-Json)
$head = (git -C $RepoRoot rev-parse HEAD).Trim()
$rootFull = [System.IO.Path]::GetFullPath($RepoRoot).TrimEnd('\') + '\'

$rows = foreach ($p in $pins) {
    $path = [string]$p.path
    $full = if ([System.IO.Path]::IsPathRooted($path)) { $path } else { Join-Path $RepoRoot $path }
    $inRepo = $full.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)
    $exists = Test-Path -LiteralPath $full -PathType Leaf
    $now = if ($exists) { (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash } else { $null }
    $row = [ordered]@{
        path = $path; inRepo = $inRepo; originalSha256 = $p.originalSha256; oct8Sha256 = $p.currentSha256
        currentSha256 = $now; currentBytes = $(if ($exists) { (Get-Item -LiteralPath $full).Length } else { $null })
        status = $(if (-not $exists) { 'missing' } elseif ($now -ceq $p.originalSha256) { 'unchanged' } else { 'changed' })
        changedSinceOct8 = ($exists -and $now -cne $p.currentSha256)
        commits = @(); workingTreeDiffersFromHead = $null
    }
    if ($inRepo -and $row.status -ne 'unchanged') {
        $rel = $full.Substring($rootFull.Length) -replace '\\', '/'
        $row.commits = @(git -c color.ui=never -C $RepoRoot log --format='%h %ad %s' --date=format:'%Y-%m-%dT%H:%M' --since=$Since -- $rel)
        $tracked = [bool](git -C $RepoRoot ls-files -- $rel)
        $row.workingTreeDiffersFromHead = if ($tracked) { [bool](git -c color.ui=never -C $RepoRoot status --porcelain=v1 -- $rel) } else { 'untracked' }
    }
    [pscustomobject]$row
}
$rows = @($rows)
$summary = [ordered]@{
    at = (Get-Date -Format s); head = $head; since = $Since; inputs = $rows.Count
    unchanged = @($rows | Where-Object status -EQ 'unchanged').Count
    changed = @($rows | Where-Object status -EQ 'changed').Count
    missing = @($rows | Where-Object status -EQ 'missing').Count
    changedSinceOct8Revalidation = @($rows | Where-Object changedSinceOct8).Count
    inRepo = @($rows | Where-Object inRepo).Count
}
[ordered]@{ summary = $summary; inputs = $rows } | ConvertTo-Json -Depth 5 | Out-File -LiteralPath (Join-Path $here 'pin-diff.json') -Encoding utf8NoBOM
$lines = @('Pin diff of the 826 original inputs against the current tree. ' + ($summary.GetEnumerator() | ForEach-Object { '{0}={1}' -f $_.Key, $_.Value }) -join '; ')
$lines += ''
foreach ($r in @($rows | Where-Object status -NE 'unchanged')) {
    $lines += '{0} [{1}{2}] original {3} -> current {4}' -f $r.path, $r.status, $(if ($r.changedSinceOct8) { ', also changed since the Oct 8 revalidation' } else { ', same as the Oct 8 revalidation' }), $r.originalSha256.Substring(0, 12), $(if ($r.currentSha256) { $r.currentSha256.Substring(0, 12) } else { 'none' })
    if (-not $r.inRepo) { $lines += '    outside the repository' }
    else {
        $lines += '    working tree differs from HEAD: {0}' -f $r.workingTreeDiffersFromHead
        if (@($r.commits).Count) { foreach ($c in $r.commits) { $lines += '    commit ' + $c } } else { $lines += '    no commit since the packet touched it (change predates the packet or is uncommitted)' }
    }
}
[System.IO.File]::WriteAllLines((Join-Path $here 'pin-diff.txt'), [string[]]$lines, [System.Text.UTF8Encoding]::new($false))
$lines
