#Requires -Version 7.4
<#
.SYNOPSIS
    Read-only: reads the 10 FR records whose descriptions embed an old rendered copy of their section (getFr through
    plugin 1.121.0) and writes before.json (full records, ConvertTo-Json -Depth 20). Prints per record: title,
    description length, line count and the first lines of the description, to design the fix.
#>
[CmdletBinding()]
param(
    [string]$OutDir = $PSScriptRoot,
    [string]$PluginRoot = (Join-Path $env:USERPROFILE '.claude\plugins\cache\mcpserver-local\mcpserver\1.121.0'),
    [int]$HeadLines = 14
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($env:MCP_PLUGIN_ROOT -or $env:MCP_CACHE_DIR_OVERRIDE) { throw 'polluted environment' }
$ids = @('FR-CFG-002', 'FR-INP-006', 'FR-MED-002', 'FR-MED-003', 'FR-ROMM-LAUNCH-001', 'FR-SID-013', 'FR-SNP-001', 'FR-SNP-002', 'FR-UI-003', 'FR-VIC20-002')
Import-Module 'F:\GitHub\vice-sharp\docs\receipts\requirements-recovery\20261019T142329-r1-coordinator\R1PluginClient.psm1' -Force -Global
$client = New-R1PluginClient -WorkDir (Join-Path $env:TEMP 'fr-embedded-client') -PluginRoot $PluginRoot
$records = foreach ($id in $ids) {
    $got = @(& $client.GetRecord 'fr' $id)
    if ($got.Count -eq 1 -and $got[0] -is [array]) { $got = @($got[0]) }
    if ($got.Count -ne 1) { throw "$id returned $($got.Count) records" }
    $got[0]
}
$records = @($records)
[System.IO.File]::WriteAllText((Join-Path $OutDir 'before.json'), (ConvertTo-Json -InputObject $records -Depth 20), [System.Text.UTF8Encoding]::new($false))
foreach ($r in $records) {
    $lines = @(([string]$r.description) -split "`r?`n")
    '===== {0} | title "{1}" | description {2} chars, {3} lines | fields: {4}' -f $r.id, $r.title, ([string]$r.description).Length, $lines.Count, (@($r.PSObject.Properties.Name) -join ',')
    $lines | Select-Object -First $HeadLines | ForEach-Object { '  | ' + $_ }
}
