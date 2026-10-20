#Requires -Version 7.4
<#
.SYNOPSIS
    Payton, 2026-10-19 19:27: "Yes" to removing the embedded old section heading from the 10 FR records with duplicate
    headings and regenerating docs/Project. Applies the fix on the LEGION2 store through plugin 1.121.0.

    Guards: the mock gate (FrEmbeddedHeading.Tests.ps1) must be green now; every live record must equal before.json
    on every field (no drift since the snapshot). Write: one workflow.requirements.updateBatch of description-only
    patches {kind = fr; id; description}. Readback: every record's description equals the planned value and every
    other field except updatedAt equals before.json. Then workflow.requirements.generateDocument.
    Writes plan.json, after.json and result.json here. -WhatIf writes plan.json only.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$PluginRoot = (Join-Path $env:USERPROFILE '.claude\plugins\cache\mcpserver-local\mcpserver\1.121.0')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($env:MCP_PLUGIN_ROOT -or $env:MCP_CACHE_DIR_OVERRIDE) { throw 'polluted environment' }
$here = $PSScriptRoot
Import-Module (Join-Path $here 'FrEmbeddedHeading.psm1') -Force

$cfg = New-PesterConfiguration
$cfg.Run.Path = Join-Path $here 'FrEmbeddedHeading.Tests.ps1'
$cfg.Run.PassThru = $true
$cfg.Output.Verbosity = 'None'
$gate = Invoke-Pester -Configuration $cfg
if ($gate.FailedCount -or $gate.PassedCount -ne $gate.TotalCount -or $gate.TotalCount -lt 20) { throw "mock gate not green: $($gate.PassedCount)/$($gate.TotalCount)" }

$before = @(Get-Content -LiteralPath (Join-Path $here 'before.json') -Raw | ConvertFrom-Json)
$plan = @(foreach ($r in $before) { [ordered]@{ kind = 'fr'; id = $r.id; description = (Remove-FrEmbeddedHeading -Id $r.id -Description $r.description) } })
$plan | ConvertTo-Json -Depth 5 | Out-File -LiteralPath (Join-Path $here 'plan.json') -Encoding utf8NoBOM -WhatIf:$false
Write-Information ('mock gate {0}/{0}; plan: {1} description-only patches' -f $gate.TotalCount, $plan.Count) -InformationAction Continue
if (-not $PSCmdlet.ShouldProcess('LEGION2 store: 10 FR descriptions', 'updateBatch, readback, generateDocument')) { return }

Import-Module 'F:\GitHub\vice-sharp\docs\receipts\requirements-recovery\20261019T142329-r1-coordinator\R1PluginClient.psm1' -Force -Global
$client = New-R1PluginClient -WorkDir (Join-Path $env:TEMP 'fr-embedded-apply-client') -PluginRoot $PluginRoot
function Get-Live([string]$Id) {
    $g = @(& $client.GetRecord 'fr' $Id)
    if ($g.Count -eq 1 -and $g[0] -is [array]) { $g = @($g[0]) }
    if ($g.Count -ne 1) { throw "$Id returned $($g.Count) records" }
    $g[0]
}
function ConvertTo-FieldJson($Record, [string]$Name) { ConvertTo-Json -InputObject $Record.$Name -Depth 20 -Compress }

# createdAt and updatedAt are excluded: this read path returns the time of the read for both (observed 19:28:17 and
# 19:31:12 on consecutive reads of FR-CFG-002; OMAR-VICE# saw the same in the R0 mirror), so they carry no stored value.
$volatile = @('createdAt', 'updatedAt')
foreach ($r in $before) {
    $l = Get-Live $r.id
    foreach ($n in @($r.PSObject.Properties.Name | Where-Object { $_ -notin $volatile })) {
        if ((ConvertTo-FieldJson $l $n) -cne (ConvertTo-FieldJson $r $n)) { throw "drift on $($r.id).$n since before.json; nothing written" }
    }
}

$batch = & $client.UpdateBatch @($plan | ForEach-Object { [pscustomobject]$_ })
$after = @(foreach ($r in $before) { Get-Live $r.id })
[System.IO.File]::WriteAllText((Join-Path $here 'after.json'), (ConvertTo-Json -InputObject $after -Depth 20), [System.Text.UTF8Encoding]::new($false))
$rows = foreach ($r in $before) {
    $a = $after | Where-Object id -EQ $r.id
    $p = $plan | Where-Object { $_.id -eq $r.id }
    $other = @(@($r.PSObject.Properties.Name) | Where-Object { $_ -notin (@('description') + $volatile) -and (ConvertTo-FieldJson $a $_) -cne (ConvertTo-FieldJson $r $_) })
    [ordered]@{ id = $r.id; descriptionAsPlanned = ([string]$a.description -ceq $p.description); otherFieldsChanged = $other; beforeChars = ([string]$r.description).Length; afterChars = ([string]$a.description).Length }
}
$rows = @($rows)
$ok = -not @($rows | Where-Object { -not $_.descriptionAsPlanned -or $_.otherFieldsChanged.Count }).Count
$export = if ($ok) { & $client.Export } else { $null }
$result = [ordered]@{ at = (Get-Date -Format s); gate = "$($gate.PassedCount)/$($gate.TotalCount)"; batch = ($batch | ConvertTo-Json -Depth 6 -Compress); readbackOk = $ok; records = $rows; exportExit = $(if ($export) { $export.ExitCode } else { 'not run' }) }
$result | ConvertTo-Json -Depth 6 | Out-File -LiteralPath (Join-Path $here 'result.json') -Encoding utf8NoBOM
$rows | ForEach-Object { '{0}: asPlanned={1} otherChanged={2} chars {3} -> {4}' -f $_.id, $_.descriptionAsPlanned, (@($_.otherFieldsChanged) -join ',' ), $_.beforeChars, $_.afterChars }
"readbackOk=$ok exportExit=$($result.exportExit)"
if (-not $ok) { throw 'readback mismatch; see result.json' }
