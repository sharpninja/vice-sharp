#Requires -Version 7.4
<#
.SYNOPSIS
    Read-only, independent check of the OMAR-VICE# R0 mirror: compares the LAB-OMARCHY post-write readbacks
    (apply/022-024 list responses and 025-033 mapping readbacks, fetched over SFTP by Get-LabMirrorReceipts.ps1) with
    the LEGION2 final readback (r0-final-records.json, r0-final-mappings.json). Every field is compared after a
    key-sorted canonical JSON form, except createdAt, updatedAt and workspaceId (store-local). Prints one line per
    record and row and a total; exits 1 on any difference or missing item.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$FetchDir,
    [string]$ReadbackDir = (Join-Path $PSScriptRoot '20261019T161658-apply\final-readback')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module powershell-yaml
$skip = @('createdAt', 'updatedAt', 'workspaceId')

function ConvertTo-Canonical($o) {
    if ($null -eq $o) { return $null }
    if ($o -is [System.Collections.IDictionary]) {
        $h = [ordered]@{}
        foreach ($k in @($o.Keys | Sort-Object)) { $h[[string]$k] = ConvertTo-Canonical $o[$k] }
        return $h
    }
    if ($o -is [System.Management.Automation.PSCustomObject]) {
        $h = [ordered]@{}
        foreach ($k in @($o.PSObject.Properties.Name | Sort-Object)) { $h[$k] = ConvertTo-Canonical $o.$k }
        return $h
    }
    if ($o -is [System.Collections.IEnumerable] -and $o -isnot [string]) { return , @($o | ForEach-Object { ConvertTo-Canonical $_ }) }
    return [string]$o
}

function Get-Result([string]$Path) {
    $docs = @(ConvertFrom-Yaml -Yaml ([System.IO.File]::ReadAllText($Path)) -AllDocuments)
    $doc = $docs[0]
    if ($doc['type'] -ne 'result') { throw "$Path is not a result envelope (type=$($doc['type']))" }
    $doc['payload']['result']
}

$records = Get-Content -LiteralPath (Join-Path $ReadbackDir 'r0-final-records.json') -Raw | ConvertFrom-Json
$rows = Get-Content -LiteralPath (Join-Path $ReadbackDir 'r0-final-mappings.json') -Raw | ConvertFrom-Json
$lab = @{}
foreach ($f in @('022-list-fr', '023-list-tr', '024-list-test')) {
    foreach ($i in @((Get-Result (Join-Path $FetchDir "apply\$f.response.yaml"))['items'])) { $lab[[string]$i['id']] = $i }
}

$diffs = 0
foreach ($r in $records) {
    $l = $lab[$r.id]
    if ($null -eq $l) { "MISSING record $($r.id)"; $diffs++; continue }
    $names = @(@($r.PSObject.Properties.Name) + @($l.Keys) | Sort-Object -Unique | Where-Object { $_ -notin $skip })
    $bad = @(foreach ($n in $names) {
            $a = ConvertTo-Json -InputObject (ConvertTo-Canonical $r.$n) -Depth 20 -Compress
            $b = ConvertTo-Json -InputObject (ConvertTo-Canonical $l[$n]) -Depth 20 -Compress
            if ($a -cne $b) { $n }
        })
    if ($bad.Count) { $diffs++; 'DIFF record {0}: {1}' -f $r.id, ($bad -join ', ') } else { 'equal record {0} ({1} fields)' -f $r.id, $names.Count }
}
foreach ($m in $rows) {
    $file = @(Get-ChildItem -LiteralPath (Join-Path $FetchDir 'apply') -Filter "*-readback-mapping-$($m.frId).response.yaml")
    if ($file.Count -ne 1) { "MISSING row $($m.frId)"; $diffs++; continue }
    $l = Get-Result $file[0].FullName
    $bad = @(foreach ($n in @('frId', 'trIds', 'testIds')) {
            if ((ConvertTo-Json -InputObject (ConvertTo-Canonical $m.$n) -Compress) -cne (ConvertTo-Json -InputObject (ConvertTo-Canonical $l[$n]) -Compress)) { $n }
        })
    if ($bad.Count) { $diffs++; 'DIFF row {0}: {1}' -f $m.frId, ($bad -join ', ') } else { 'equal row {0} (tr {1}, test {2})' -f $m.frId, @($m.trIds).Count, @($m.testIds).Count }
}
'total: records {0}, rows {1}, differences {2}' -f @($records).Count, @($rows).Count, $diffs
if ($diffs) { exit 1 }
exit 0
