#Requires -Version 7.4
<#
.SYNOPSIS
    D8 status-accuracy corrections (handoff v2 section 11, Payton 2026-10-19 "Follow the rules")
    on the PAYTON-LEGION2 requirement store, through the mcpserver-claude-code-plugin wrapper.

.DESCRIPTION
    Rules applied, in order, to every FR, TR and TEST record:
      1. Every criterion with isSatisfied true and no run receipt is cleared (isSatisfied false).
         A run receipt is evidence citing docs/receipts, TestResults or a .trx. The dry run
         (20261019T115841-d8-status-accuracy-dryrun-v2) found 0 such receipts, so all flags
         clear. Criterion id, text and evidence text are preserved unchanged.
      2. A record whose status is completed or closed and that, after step 1, has any
         unsatisfied criterion or zero criteria is set to pending.
      3. No record is raised to completed.

    For each changed record: read the before-state with get<Kind> (saved to the journal), send a
    sparse update (id, status when changed, the full acceptanceCriteria list), read back with
    get<Kind>, and compare every criterion's id, text, isSatisfied and evidence, the status,
    title, description, priority and notes against the expected after-state.
    Stops at the first mismatch or failure. -WhatIf writes the plan only. -PilotId limits the
    run to one record.
#>
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'WorkspacePath', Justification = 'Used by nested function Invoke-Plugin through script scope; the analyzer does not trace nested-function use.')]
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$AuditDir,
    [Parameter(Mandatory)][string]$OutDir,
    [string]$PilotId,
    [string]$WorkspacePath = 'F:\GitHub\vice-sharp',
    [string]$PluginRoot = (Join-Path $env:USERPROFILE '.claude\plugins\cache\mcpserver-local\mcpserver\1.119.0')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module powershell-yaml -MinimumVersion 0.4.12 -ErrorAction Stop

$wrapper = Join-Path $PluginRoot 'lib\Invoke-McpPlugin.ps1'
$receiptPattern = '(docs[\\/]receipts[\\/][^\s`)]+|TestResults[\\/][^\s`)]+|\.trx\b)'
$kindMethod = @{ fr = 'Fr'; tr = 'Tr'; test = 'Test' }
New-Item -ItemType Directory -Force -Path $OutDir -WhatIf:$false | Out-Null
$journal = Join-Path $OutDir 'journal.jsonl'

function Write-Journal {
    param([Parameter(Mandatory)][hashtable]$Entry)
    $Entry['at'] = Get-Date -Format s
    ($Entry | ConvertTo-Json -Depth 20 -Compress) | Add-Content -LiteralPath $journal -Encoding utf8NoBOM
}

function Invoke-Plugin {
    param([Parameter(Mandatory)][string]$Method, [Parameter(Mandatory)][hashtable]$Params)
    $paramsPath = Join-Path $OutDir ('params-{0}.yaml' -f [guid]::NewGuid().ToString('N'))
    ConvertTo-Yaml -Data $Params | Out-File -LiteralPath $paramsPath -Encoding utf8NoBOM
    $env:CLAUDE_PLUGIN_ROOT = $PluginRoot
    $env:PLUGIN_AGENT_NAME = 'Claude'
    try {
        $raw = & pwsh.exe -NoProfile -NonInteractive -File $wrapper -Command Invoke -Method $Method -ParamsPath $paramsPath -WorkspacePath $WorkspacePath 2>&1
        $exit = $LASTEXITCODE
    }
    finally {
        Remove-Item Env:CLAUDE_PLUGIN_ROOT, Env:PLUGIN_AGENT_NAME -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $paramsPath -ErrorAction SilentlyContinue
    }
    $text = ($raw | Out-String)
    if ($exit -ne 0) { throw "$Method exit $exit`n$text" }
    $doc = ConvertFrom-Yaml -Yaml ($text -replace '(?m)^---\s*$', '') -Ordered
    if ($doc.type -ne 'result') { throw "$Method returned type '$($doc.type)'`n$text" }
    return $doc.payload.result
}

function Get-CriterionList {
    param($Record)
    if ($Record.Contains('acceptanceCriteria') -and $null -ne $Record['acceptanceCriteria']) { return @($Record['acceptanceCriteria']) }
    return @()
}

function Get-Field {
    param($Map, [string]$Name)
    if ($Map.Contains($Name)) { return [string]$Map[$Name] }
    return ''
}

# Build the plan from the dry-run export.
$plan = [System.Collections.Generic.List[object]]::new()
foreach ($kind in 'fr', 'tr', 'test') {
    foreach ($rec in @(Get-Content -LiteralPath (Join-Path $AuditDir "$kind.json") -Raw | ConvertFrom-Json)) {
        if ($PilotId -and $rec.id -ne $PilotId) { continue }
        $ac = @()
        if ($rec.PSObject.Properties.Name -contains 'acceptanceCriteria' -and $null -ne $rec.acceptanceCriteria) { $ac = @($rec.acceptanceCriteria) }
        $clear = @($ac | Where-Object { $_.isSatisfied -eq $true -and -not (($_.PSObject.Properties.Name -contains 'evidence') -and ([string]$_.evidence -match $receiptPattern)) })
        $afterSatisfied = @($ac | Where-Object { $_.isSatisfied -eq $true }).Count - $clear.Count
        $newStatus = $rec.status
        if ($rec.status -in 'completed', 'closed' -and ($ac.Count -eq 0 -or $afterSatisfied -lt $ac.Count)) { $newStatus = 'pending' }
        if ($clear.Count -eq 0 -and $newStatus -eq $rec.status) { continue }
        $plan.Add([pscustomobject][ordered]@{ kind = $kind; id = $rec.id; statusBefore = $rec.status; statusAfter = $newStatus; criteria = $ac.Count; flagsCleared = $clear.Count })
    }
}
$plan | ConvertTo-Json -Depth 5 | Out-File -LiteralPath (Join-Path $OutDir 'plan.json') -Encoding utf8NoBOM -WhatIf:$false
$planSummary = [ordered]@{
    records       = $plan.Count
    statusChanges = @($plan | Where-Object { $_.statusBefore -ne $_.statusAfter }).Count
    flagsCleared  = ($plan | Measure-Object -Property flagsCleared -Sum).Sum
    byKind        = [ordered]@{}
}
foreach ($g in ($plan | Group-Object kind)) { $planSummary.byKind[$g.Name] = $g.Count }

if (-not $PSCmdlet.ShouldProcess("$($plan.Count) requirement records", 'Clear unreceipted satisfied flags and lower done states to pending')) {
    $planSummary | ConvertTo-Json -Depth 4
    return
}

$applied = 0
foreach ($p in $plan) {
    $suffix = $kindMethod[$p.kind]
    $before = Invoke-Plugin -Method "workflow.requirements.get$suffix" -Params @{ id = $p.id }
    if ((Get-Field $before 'status') -ne $p.statusBefore) { throw "$($p.id): live status '$($before['status'])' differs from dry-run '$($p.statusBefore)'; another writer may be active." }
    $newCriteria = foreach ($c in (Get-CriterionList $before)) {
        $n = [ordered]@{ id = [string]$c['id']; text = [string]$c['text']; isSatisfied = $false }
        $wasSat = [bool]$c['isSatisfied']
        $ev = if ($c.Contains('evidence')) { [string]$c['evidence'] } else { '' }
        if ($wasSat -and $ev -match $receiptPattern) { $n.isSatisfied = $true }
        if ($ev) { $n['evidence'] = $ev }
        $n
    }
    $update = [ordered]@{ id = $p.id; acceptanceCriteria = @($newCriteria) }
    if ($p.statusAfter -ne $p.statusBefore) { $update['status'] = $p.statusAfter }
    Write-Journal -Entry @{ phase = 'before'; kind = $p.kind; id = $p.id; record = $before; update = $update }
    $result = Invoke-Plugin -Method "workflow.requirements.update$suffix" -Params ([hashtable]$update)
    Write-Journal -Entry @{ phase = 'write'; kind = $p.kind; id = $p.id; result = $result }
    $after = Invoke-Plugin -Method "workflow.requirements.get$suffix" -Params @{ id = $p.id }
    Write-Journal -Entry @{ phase = 'after'; kind = $p.kind; id = $p.id; record = $after }

    $problems = [System.Collections.Generic.List[string]]::new()
    if ((Get-Field $after 'status') -ne $p.statusAfter) { $problems.Add("status '$($after['status'])' expected '$($p.statusAfter)'") }
    foreach ($f in 'title', 'description', 'priority', 'notes') { if ((Get-Field $after $f) -ne (Get-Field $before $f)) { $problems.Add("field $f changed") } }
    $ac = @(Get-CriterionList $after)
    if ($ac.Count -ne @($newCriteria).Count) { $problems.Add("criteria count $($ac.Count) expected $(@($newCriteria).Count)") }
    for ($i = 0; $i -lt [Math]::Min($ac.Count, @($newCriteria).Count); $i++) {
        $want = @($newCriteria)[$i]
        $got = $ac[$i]
        if ([string]$got['id'] -ne $want.id -or [string]$got['text'] -ne $want.text -or [bool]$got['isSatisfied'] -ne $want.isSatisfied) { $problems.Add("criterion $($want.id) mismatch") }
        $gotEv = if ($got.Contains('evidence')) { [string]$got['evidence'] } else { '' }
        $wantEv = if ($want.Contains('evidence')) { [string]$want['evidence'] } else { '' }
        if ($gotEv -ne $wantEv) { $problems.Add("criterion $($want.id) evidence changed") }
    }
    Write-Journal -Entry @{ phase = 'verify'; kind = $p.kind; id = $p.id; ok = ($problems.Count -eq 0); problems = @($problems) }
    if ($problems.Count -gt 0) { throw "$($p.id) readback mismatch: $($problems -join '; '). Stopped after $applied verified records." }
    $applied++
}

$planSummary['appliedVerified'] = $applied
$planSummary['finishedAt'] = Get-Date -Format s
$planSummary | ConvertTo-Json -Depth 4 | Out-File -LiteralPath (Join-Path $OutDir 'apply-summary.json') -Encoding utf8NoBOM
$planSummary | ConvertTo-Json -Depth 4
