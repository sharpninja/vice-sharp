#Requires -Version 7.4
<#
.SYNOPSIS
    Read-only: reads the 16 R0 records (getFr/getTr/getTest) and the 9 R0 mapping rows (client.Requirements.GetMappingAsync,
    full rows) from the LEGION2 store through the 1.121.0 plugin and writes r0-final-records.json and
    r0-final-mappings.json (ConvertTo-Json -Depth 10, UTF-8 without BOM) for the LAB-OMARCHY mirror. Prints path,
    byte length and SHA256 of each file.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$OutDir,
    [string]$PluginRoot = (Join-Path $env:USERPROFILE '.claude\plugins\cache\mcpserver-local\mcpserver\1.121.0')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($env:MCP_PLUGIN_ROOT -or $env:MCP_CACHE_DIR_OVERRIDE) { throw 'polluted environment (MCP_PLUGIN_ROOT or MCP_CACHE_DIR_OVERRIDE set)' }
Import-Module (Join-Path $PSScriptRoot 'R1PluginClient.psm1') -Force -Global
$packet = Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\20261013T231703-r0-anchor-remediation\proposed-field-delta.json') -Raw | ConvertFrom-Json
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$client = New-R1PluginClient -WorkDir (Join-Path $OutDir 'client') -PluginRoot $PluginRoot

$records = foreach ($r in $packet.records) {
    $got = @(& $client.GetRecord $r.kind.ToLowerInvariant() $r.id)
    if ($got.Count -eq 1 -and $got[0] -is [array]) { $got = @($got[0]) }
    if ($got.Count -ne 1) { throw "readback for $($r.id) returned $($got.Count) records" }
    $got[0]
}
$rows = foreach ($m in $packet.mappingsAfter) { & $client.GetMapping $m.frId }

$utf8 = [System.Text.UTF8Encoding]::new($false)
foreach ($pair in @(@('r0-final-records.json', @($records)), @('r0-final-mappings.json', @($rows)))) {
    $path = Join-Path $OutDir $pair[0]
    [System.IO.File]::WriteAllText($path, (ConvertTo-Json -InputObject $pair[1] -Depth 10), $utf8)
    '{0} bytes={1} sha256={2} items={3}' -f $path, (Get-Item -LiteralPath $path).Length, (Get-FileHash -LiteralPath $path).Hash, @($pair[1]).Count
}
