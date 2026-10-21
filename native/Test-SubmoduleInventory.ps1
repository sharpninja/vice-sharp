#Requires -Version 7.0
<#
.SYNOPSIS
PLAN-NATIVERESTORE-001 D2 and D4 (F08): proves the submodule's porcelain inventory (every modified tracked and every
untracked path, hidden included, with SHA-256 and length) equals a baseline, and that the checked-out commit equals
the baseline's. Reports added, deleted and changed paths; exits nonzero on any difference.

.DESCRIPTION
With -WriteBaselinePath the current inventory is written in the same format (schema phase-d-inventory/1) instead of
being compared; the D0 preservation manifest (phase-d-preservation/1, entries with path, status, sha256, length)
is accepted as a baseline too. Read only apart from the optional baseline and report files.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $SubmodulePath,
    [string] $BaselineManifest,
    [string] $WriteBaselinePath,
    [string] $ReportPath
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-CurrentInventory {
    param([string] $Root)
    $rows = @(& git -C $Root status --porcelain=v1 -uall --untracked-files=all | Where-Object { $_ -ne '' })
    if ($LASTEXITCODE -ne 0) { throw "git status failed in $Root" }
    $entries = [Collections.Generic.List[object]]::new()
    foreach ($row in $rows) {
        $code = $row.Substring(0, 2)
        $path = $row.Substring(3)
        if ($path.StartsWith('"') -and $path.EndsWith('"')) { $path = $path.Substring(1, $path.Length - 2) }
        if ($path.Contains(' -> ')) { $path = $path.Split(' -> ')[1] }
        $full = [IO.Path]::Combine($Root, $path)
        $sha = $null; $length = 0
        if ((Test-Path -LiteralPath $full -PathType Leaf)) {
            $item = Get-Item -LiteralPath $full -Force
            $length = $item.Length
            $sha = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash
        }
        $entries.Add([pscustomobject]@{ path = $path; status = $code.Trim(); sha256 = $sha; length = $length })
    }
    $head = (& git -C $Root rev-parse HEAD).Trim()
    return [pscustomobject]@{ schema = 'phase-d-inventory/1'; checkedOutSha = $head; entries = @($entries | Sort-Object -Property path) }
}

$current = Get-CurrentInventory -Root $SubmodulePath

if ($WriteBaselinePath) {
    New-Item -ItemType Directory -Path (Split-Path -Parent $WriteBaselinePath) -Force | Out-Null
    $current | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $WriteBaselinePath -Encoding utf8
    Write-Output ('baseline written: ' + $WriteBaselinePath + ' entries ' + $current.entries.Count + ' head ' + $current.checkedOutSha)
    return
}
if (-not $BaselineManifest) { throw 'Either -BaselineManifest or -WriteBaselinePath is required.' }

$baseline = Get-Content -LiteralPath $BaselineManifest -Raw | ConvertFrom-Json
$baselineSha = if ($baseline.PSObject.Properties['checkedOutSha']) { $baseline.checkedOutSha } else { $null }
$baselineEntries = @{}
foreach ($e in @($baseline.entries)) { $baselineEntries[$e.path] = $e }
$currentEntries = @{}
foreach ($e in $current.entries) { $currentEntries[$e.path] = $e }

$added = @($currentEntries.Keys | Where-Object { -not $baselineEntries.ContainsKey($_) } | Sort-Object)
$deleted = @($baselineEntries.Keys | Where-Object { -not $currentEntries.ContainsKey($_) } | Sort-Object)
$changed = @()
foreach ($path in ($currentEntries.Keys | Where-Object { $baselineEntries.ContainsKey($_) } | Sort-Object)) {
    $b = $baselineEntries[$path]; $c = $currentEntries[$path]
    $bSha = if ($null -eq $b.sha256) { '' } else { [string]$b.sha256 }
    $cSha = if ($null -eq $c.sha256) { '' } else { [string]$c.sha256 }
    if ($bSha -ne $cSha -or [long]$b.length -ne [long]$c.length -or [string]$b.status -ne [string]$c.status) {
        $changed += [pscustomobject]@{ path = $path; baselineSha256 = $bSha; currentSha256 = $cSha; baselineLength = $b.length; currentLength = $c.length; baselineStatus = $b.status; currentStatus = $c.status }
    }
}
$headEqual = ($null -eq $baselineSha) -or ($baselineSha -eq $current.checkedOutSha)
$equal = ($added.Count -eq 0 -and $deleted.Count -eq 0 -and $changed.Count -eq 0 -and $headEqual)

$report = [pscustomobject]@{
    submodulePath = $SubmodulePath
    baselineManifest = $BaselineManifest
    baselineEntries = $baselineEntries.Count
    currentEntries = $currentEntries.Count
    baselineSha = $baselineSha
    currentSha = $current.checkedOutSha
    headEqual = $headEqual
    added = $added
    deleted = $deleted
    changed = @($changed)
    outcome = if ($equal) { 'Equal' } else { 'Different' }
}
if ($ReportPath) {
    New-Item -ItemType Directory -Path (Split-Path -Parent $ReportPath) -Force | Out-Null
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $ReportPath -Encoding utf8
}
Write-Output ('inventory ' + $report.outcome + ': baseline ' + $report.baselineEntries + ', current ' + $report.currentEntries + ', added ' + $added.Count + ', deleted ' + $deleted.Count + ', changed ' + $changed.Count + ', head equal ' + $headEqual)
foreach ($a in $added) { Write-Output ('  added   ' + $a) }
foreach ($d in $deleted) { Write-Output ('  deleted ' + $d) }
foreach ($c in $changed) { Write-Output ('  changed ' + $c.path + ' (' + $c.baselineSha256 + '/' + $c.baselineLength + ' -> ' + $c.currentSha256 + '/' + $c.currentLength + ')') }
if (-not $equal) { throw 'Submodule inventory differs from the baseline.' }
