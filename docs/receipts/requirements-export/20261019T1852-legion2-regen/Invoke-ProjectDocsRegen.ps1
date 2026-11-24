#Requires -Version 7.4
<#
.SYNOPSIS
    Payton, 2026-10-19 (relayed by OMAR-VICE#): "1: A (regenerate docs/Project/*.md from the LEGION2 store and present it
    as its own commit for review; do not push until Payton approves)".

    1. Copies the current docs/Project/*.md (the uncommitted 16:22 export) into before-regen/, hash-verified.
    2. Regenerates docs/Project through the 1.121.0 plugin (workflow.requirements.generateDocument).
    3. Section-level analysis against the last committed projection (BaseCommit, default 9221488): each file is split
       into sections at headings that start with "## <ID>"; ids whose section text was added, removed or changed are
       reported and classed as today's write (D8 journals, the R0 packet, the step 6 capture) or an earlier store
       change. Also lists ids with more than one heading in a file.
    Writes regen-analysis.json and regen-analysis.txt here. Commits nothing. -WhatIf stops before any change.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$RepoRoot = 'F:\GitHub\vice-sharp',
    [string]$BaseCommit = '9221488',
    [string]$PluginRoot = (Join-Path $env:USERPROFILE '.claude\plugins\cache\mcpserver-local\mcpserver\1.121.0')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($env:MCP_PLUGIN_ROOT -or $env:MCP_CACHE_DIR_OVERRIDE) { throw 'polluted environment (MCP_PLUGIN_ROOT or MCP_CACHE_DIR_OVERRIDE set)' }
$here = $PSScriptRoot
$proj = Join-Path $RepoRoot 'docs\Project'
$script:idRx = '(?:FR|TR|TEST)-[A-Z0-9]+(?:-[A-Z0-9]+)*-\d{3}'

$files = @(Get-ChildItem -LiteralPath $proj -Filter '*.md' -File)
Write-Information ('regenerate {0} files in {1}; base {2}' -f $files.Count, $proj, $BaseCommit) -InformationAction Continue
if (-not $PSCmdlet.ShouldProcess($proj, 'back up and regenerate docs/Project')) { return }

$before = Join-Path $here 'before-regen'
New-Item -ItemType Directory -Force -Path $before | Out-Null
foreach ($f in $files) {
    $dest = Join-Path $before $f.Name
    Copy-Item -LiteralPath $f.FullName -Destination $dest
    if ((Get-FileHash -LiteralPath $f.FullName).Hash -cne (Get-FileHash -LiteralPath $dest).Hash) { throw "backup hash mismatch: $($f.Name)" }
}

Import-Module (Join-Path $RepoRoot 'docs\receipts\requirements-recovery\20261019T142329-r1-coordinator\R1PluginClient.psm1') -Force -Global
$client = New-R1PluginClient -WorkDir (Join-Path $env:TEMP 'legion2-regen-client') -PluginRoot $PluginRoot -WorkspacePath $RepoRoot
$export = & $client.Export
if ($export.ExitCode -ne 0) { throw "generateDocument failed: $($export.Output)" }

function Get-Section([string]$Text) {
    $map = @{}
    $dups = @{}
    $cur = $null
    $sb = $null
    foreach ($line in ($Text -split "`r?`n")) {
        $m = [regex]::Match($line, "^##\s+($script:idRx)\b")
        if ($m.Success) {
            if ($cur) { $map[$cur] = $map[$cur] + $sb.ToString() }
            $cur = $m.Groups[1].Value
            if ($map.ContainsKey($cur)) { $dups[$cur] = 1 + [int]$dups[$cur] } else { $map[$cur] = '' }
            $sb = [System.Text.StringBuilder]::new()
            continue
        }
        if ($line -match '^#{1,2}\s' -and $line -notmatch $script:idRx) {
            if ($cur) { $map[$cur] = $map[$cur] + $sb.ToString() }
            $cur = $null
            continue
        }
        if ($cur) { [void]$sb.AppendLine($line) }
    }
    if ($cur) { $map[$cur] = $map[$cur] + $sb.ToString() }
    [pscustomobject]@{ Map = $map; Dups = $dups }
}

$today = [System.Collections.Generic.HashSet[string]]::new()
foreach ($j in @(Get-ChildItem -LiteralPath (Join-Path $RepoRoot 'docs\receipts\requirements-audit') -Recurse -Filter 'journal.jsonl')) {
    foreach ($l in Get-Content -LiteralPath $j.FullName) { $o = $l | ConvertFrom-Json; if ($o.PSObject.Properties['id'] -and $o.id) { [void]$today.Add($o.id) } }
}
$pk = Get-Content -LiteralPath (Join-Path $RepoRoot 'docs\receipts\requirements-recovery\20261013T231703-r0-anchor-remediation\proposed-field-delta.json') -Raw | ConvertFrom-Json
foreach ($r in $pk.records) { [void]$today.Add($r.id) }
foreach ($m in $pk.mappingsAfter) { [void]$today.Add($m.frId) }
foreach ($x in 'TR-BUILD-001', 'TEST-BUILD-001') { [void]$today.Add($x) }

$report = foreach ($f in @(Get-ChildItem -LiteralPath $proj -Filter '*.md' -File)) {
    $rel = "docs/Project/$($f.Name)"
    $old = (git -C $RepoRoot show "${BaseCommit}:$rel" 2>$null) -join "`n"
    $new = [System.IO.File]::ReadAllText($f.FullName)
    $o = Get-Section $old
    $n = Get-Section $new
    $ids = @(@($o.Map.Keys) + @($n.Map.Keys) | Sort-Object -Unique)
    $changed = @(foreach ($id in $ids) {
            $a = if ($o.Map.ContainsKey($id)) { $o.Map[$id] -replace '\s+$', '' } else { $null }
            $b = if ($n.Map.ContainsKey($id)) { $n.Map[$id] -replace '\s+$', '' } else { $null }
            if ($null -eq $a) { [pscustomobject]@{ Id = $id; Change = 'added'; Today = $today.Contains($id) } }
            elseif ($null -eq $b) { [pscustomobject]@{ Id = $id; Change = 'removed'; Today = $today.Contains($id) } }
            elseif ($a -cne $b) { [pscustomobject]@{ Id = $id; Change = 'changed'; Today = $today.Contains($id) } }
        })
    $prev = Join-Path $before $f.Name
    [pscustomobject]@{
        File = $f.Name; Sha256 = (Get-FileHash -LiteralPath $f.FullName).Hash; Bytes = $f.Length
        SameAsBeforeRegen = ((Test-Path -LiteralPath $prev) -and (Get-FileHash -LiteralPath $f.FullName).Hash -ceq (Get-FileHash -LiteralPath $prev).Hash)
        SectionIdsOld = $o.Map.Count; SectionIdsNew = $n.Map.Count; Changed = $changed
        DuplicateHeadings = @($n.Dups.Keys | Sort-Object | ForEach-Object { [pscustomobject]@{ Id = $_; ExtraHeadings = $n.Dups[$_] } })
    }
}
$report = @($report)
[ordered]@{ at = (Get-Date -Format s); base = $BaseCommit; pluginRoot = $PluginRoot; export = $export.Output; todaysIds = $today.Count; files = $report } |
    ConvertTo-Json -Depth 6 | Out-File -LiteralPath (Join-Path $here 'regen-analysis.json') -Encoding utf8NoBOM
$lines = foreach ($r in $report) {
    $c = @($r.Changed)
    '{0}: sha {1} bytes {2} sameAsBeforeRegen {3}; section ids {4} -> {5}; changed {6} (today {7}, earlier {8}); duplicate-heading ids {9}' -f $r.File, $r.Sha256.Substring(0, 8), $r.Bytes, $r.SameAsBeforeRegen, $r.SectionIdsOld, $r.SectionIdsNew, $c.Count, @($c | Where-Object Today).Count, @($c | Where-Object { -not $_.Today }).Count, @($r.DuplicateHeadings).Count
    if ($c.Count) { '  today:   ' + ((@($c | Where-Object Today) | ForEach-Object { '{0}({1})' -f $_.Id, $_.Change }) -join ' ') }
    if ($c.Count) { '  earlier: ' + ((@($c | Where-Object { -not $_.Today }) | ForEach-Object { '{0}({1})' -f $_.Id, $_.Change }) -join ' ') }
    if (@($r.DuplicateHeadings).Count) { '  duplicate headings: ' + ((@($r.DuplicateHeadings) | ForEach-Object { '{0}(+{1})' -f $_.Id, $_.ExtraHeadings }) -join ' ') }
}
[System.IO.File]::WriteAllLines((Join-Path $here 'regen-analysis.txt'), [string[]]@($lines), [System.Text.UTF8Encoding]::new($false))
$lines
