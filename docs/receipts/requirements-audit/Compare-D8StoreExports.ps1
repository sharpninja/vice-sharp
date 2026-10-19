#Requires -Version 7.4
<#
.SYNOPSIS
    Whole-store diff of two requirement exports (Export-RequirementStore.ps1 output) around the D8
    correction. Every FR, TR and TEST record and every mapping row is compared field by field.
    Allowed differences: createdAt and updatedAt (read-synthesized, observation only), and, for
    records named in the apply plan, status (to the planned value) and acceptanceCriteria[].isSatisfied
    (true to false). Every other difference is reported as unexpected. Read-only.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$BeforeDir,
    [Parameter(Mandatory)][string]$AfterDir,
    [Parameter(Mandatory)][string[]]$PlanPath,
    [Parameter(Mandatory)][string]$OutPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$volatile = @('createdAt', 'updatedAt')
$planned = @{}
foreach ($pp in $PlanPath) {
    foreach ($p in @(Get-Content -LiteralPath $pp -Raw | ConvertFrom-Json)) { $planned["$($p.kind)|$($p.id)"] = $p }
}

$unexpected = [System.Collections.Generic.List[object]]::new()
$expected = [System.Collections.Generic.List[object]]::new()
$counts = [ordered]@{}

function Compare-Value {
    param($A, $B)
    return (($A | ConvertTo-Json -Depth 20 -Compress) -ceq ($B | ConvertTo-Json -Depth 20 -Compress))
}

foreach ($kind in 'fr', 'tr', 'test') {
    $before = @{}; foreach ($r in @(Get-Content -LiteralPath (Join-Path $BeforeDir "$kind.json") -Raw | ConvertFrom-Json)) { $before[$r.id] = $r }
    $after = @{}; foreach ($r in @(Get-Content -LiteralPath (Join-Path $AfterDir "$kind.json") -Raw | ConvertFrom-Json)) { $after[$r.id] = $r }
    $counts[$kind] = [ordered]@{ before = $before.Count; after = $after.Count; identical = 0; changedAsPlanned = 0 }
    foreach ($id in ($before.Keys + $after.Keys | Sort-Object -Unique)) {
        if (-not $after.ContainsKey($id)) { $unexpected.Add([pscustomobject]@{ kind = $kind; id = $id; field = '(record)'; detail = 'missing after' }); continue }
        if (-not $before.ContainsKey($id)) { $unexpected.Add([pscustomobject]@{ kind = $kind; id = $id; field = '(record)'; detail = 'new after' }); continue }
        $b = $before[$id]; $a = $after[$id]
        $plan = $planned["$kind|$id"]
        $fields = @($b.PSObject.Properties.Name + $a.PSObject.Properties.Name | Sort-Object -Unique | Where-Object { $_ -notin $volatile })
        $recordDiffs = 0
        foreach ($f in $fields) {
            $bHas = $b.PSObject.Properties.Name -contains $f
            $aHas = $a.PSObject.Properties.Name -contains $f
            if ($bHas -ne $aHas) { $unexpected.Add([pscustomobject]@{ kind = $kind; id = $id; field = $f; detail = "presence before=$bHas after=$aHas" }); $recordDiffs++; continue }
            if (Compare-Value $b.$f $a.$f) { continue }
            $recordDiffs++
            if ($f -eq 'status' -and $plan -and $b.status -eq $plan.statusBefore -and $a.status -eq $plan.statusAfter) {
                $expected.Add([pscustomobject]@{ kind = $kind; id = $id; field = 'status'; detail = "$($b.status) -> $($a.status)" }); continue
            }
            if ($f -eq 'acceptanceCriteria' -and $plan) {
                $ba = @($b.acceptanceCriteria); $aa = @($a.acceptanceCriteria)
                $ok = $ba.Count -eq $aa.Count
                for ($i = 0; $ok -and $i -lt $ba.Count; $i++) {
                    $x = $ba[$i]; $y = $aa[$i]
                    $xn = @($x.PSObject.Properties.Name | Where-Object { $_ -ne 'isSatisfied' } | Sort-Object)
                    $yn = @($y.PSObject.Properties.Name | Where-Object { $_ -ne 'isSatisfied' } | Sort-Object)
                    if (-not (Compare-Value $xn $yn)) { $ok = $false; break }
                    foreach ($n in $xn) { if (-not (Compare-Value $x.$n $y.$n)) { $ok = $false } }
                    if ($y.isSatisfied -ne $false) { $ok = $false }
                }
                if ($ok) { $expected.Add([pscustomobject]@{ kind = $kind; id = $id; field = 'acceptanceCriteria'; detail = 'isSatisfied cleared only' }); continue }
            }
            $unexpected.Add([pscustomobject]@{ kind = $kind; id = $id; field = $f; detail = 'value changed' })
        }
        if ($recordDiffs -eq 0) { $counts[$kind].identical++ } elseif ($plan) { $counts[$kind].changedAsPlanned++ }
    }
}

$normalizeMapping = {
    param([string]$Path)
    @(Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json) |
        ForEach-Object { $_ | Select-Object -Property * -ExcludeProperty $volatile | ConvertTo-Json -Depth 10 -Compress } |
        Sort-Object
}
$mb = & $normalizeMapping (Join-Path $BeforeDir 'mapping.json')
$ma = & $normalizeMapping (Join-Path $AfterDir 'mapping.json')
$mappingsEqual = (($mb -join "`n") -ceq ($ma -join "`n"))
if (-not $mappingsEqual) {
    foreach ($d in @(Compare-Object $mb $ma)) { $unexpected.Add([pscustomobject]@{ kind = 'mapping'; id = '*'; field = $d.SideIndicator; detail = $d.InputObject }) }
}

$report = [ordered]@{
    comparedAt          = (Get-Date -Format s)
    beforeDir           = $BeforeDir
    afterDir            = $AfterDir
    excludedVolatile    = $volatile
    counts              = $counts
    mappingRowsEqualExcludingVolatile = $mappingsEqual
    mappingRows         = [ordered]@{ before = $mb.Count; after = $ma.Count }
    expectedDiffs       = $expected.Count
    unexpectedDiffs     = $unexpected.Count
    unexpected          = $unexpected
    expected            = $expected
}
$report | ConvertTo-Json -Depth 8 | Out-File -LiteralPath $OutPath -Encoding utf8NoBOM
[ordered]@{ counts = $counts; mappingRowsEqualExcludingVolatile = $mappingsEqual; mappingRows = "$($mb.Count)/$($ma.Count)"; expectedDiffs = $expected.Count; unexpectedDiffs = $unexpected.Count; firstUnexpected = @($unexpected | Select-Object -First 10) } | ConvertTo-Json -Depth 6
