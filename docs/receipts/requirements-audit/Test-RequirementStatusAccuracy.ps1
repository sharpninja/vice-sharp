#Requires -Version 7.4
<#
.SYNOPSIS
    D8 status-accuracy audit (handoff v2 section 11), DRY RUN. Reads the fr/tr/test JSON
    written by Export-RequirementStore.ps1 and reports every record whose status or
    satisfied flags disagree with cited evidence. Writes report files only; never writes
    to the requirement store.

.DESCRIPTION
    Rules (from D8: "closed with unsatisfied criteria becomes pending; satisfied flags
    with no receipt are cleared and listed; no record is raised to completed without
    evidence"):
      R1  status completed or closed while one or more criteria are unsatisfied.
      R2  status completed or closed with zero acceptance criteria (done with nothing to prove).
      R3  status pending or in_progress while every criterion (one or more) is satisfied.
      R4  a criterion has isSatisfied true. Its optional `evidence` field is classified:
          none, receipt-path (cites docs/receipts, TestResults or a .trx), or
          reference-only (names a test or check without a run receipt). Correction
          2026-10-19T11:58: the first dry run ignored `evidence` and wrongly reported
          that no criterion carries one.
      R5  status closed, a value the plugin write schema does not accept.
    Proposed action per finding follows D8 and is a proposal only.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$AuditDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$receiptPattern = '(docs[\\/]receipts[\\/][^\s`)]+|TestResults[\\/][^\s`)]+|\.trx\b)'
$doneStates = @('completed', 'closed')
$openStates = @('pending', 'in_progress')

$findings = [System.Collections.Generic.List[object]]::new()
$flagRows = [System.Collections.Generic.List[object]]::new()
$counts = [ordered]@{}

foreach ($type in 'fr', 'tr', 'test') {
    $path = Join-Path $AuditDir "$type.json"
    $items = @(Get-Content -LiteralPath $path -Raw | ConvertFrom-Json)
    $counts[$type] = [ordered]@{ records = $items.Count; criteria = 0; satisfiedFlags = 0 }
    foreach ($rec in $items) {
        $ac = @()
        if ($rec.PSObject.Properties.Name -contains 'acceptanceCriteria' -and $null -ne $rec.acceptanceCriteria) { $ac = @($rec.acceptanceCriteria) }
        $sat = @($ac | Where-Object { $_.isSatisfied -eq $true })
        $counts[$type].criteria += $ac.Count
        $counts[$type].satisfiedFlags += $sat.Count
        $desc = [string]$rec.description
        $citations = @([regex]::Matches($desc, $receiptPattern) | ForEach-Object { $_.Value } | Sort-Object -Unique)
        $base = [ordered]@{ type = $type; id = $rec.id; status = $rec.status; criteria = $ac.Count; satisfied = $sat.Count }

        if ($doneStates -contains $rec.status -and $ac.Count -gt 0 -and $sat.Count -lt $ac.Count) {
            $findings.Add([pscustomobject]($base + [ordered]@{ rule = 'R1'; detail = "status $($rec.status) with $($ac.Count - $sat.Count) of $($ac.Count) criteria unsatisfied"; proposed = 'set status pending' }))
        }
        if ($doneStates -contains $rec.status -and $ac.Count -eq 0) {
            $findings.Add([pscustomobject]($base + [ordered]@{ rule = 'R2'; detail = "status $($rec.status) with zero acceptance criteria"; proposed = 'set status pending; criteria to be authored before any done claim' }))
        }
        if ($openStates -contains $rec.status -and $ac.Count -gt 0 -and $sat.Count -eq $ac.Count) {
            $findings.Add([pscustomobject]($base + [ordered]@{ rule = 'R3'; detail = "status $($rec.status) while all $($ac.Count) criteria are flagged satisfied"; proposed = 'keep status; clear uncited flags (R4); re-prove before any raise' }))
        }
        if ($rec.status -eq 'closed') {
            $findings.Add([pscustomobject]($base + [ordered]@{ rule = 'R5'; detail = 'status closed is not a value the plugin write schema accepts'; proposed = 'covered by R1 or R2 action (pending)' }))
        }
        foreach ($c in $sat) {
            $evidence = ''
            if ($c.PSObject.Properties.Name -contains 'evidence' -and $null -ne $c.evidence) { $evidence = [string]$c.evidence }
            $evidenceClass = if ([string]::IsNullOrWhiteSpace($evidence)) { 'none' } elseif ($evidence -match $receiptPattern) { 'receipt-path' } else { 'reference-only' }
            $proposed = switch ($evidenceClass) {
                'none' { 'clear isSatisfied (no evidence on criterion)' }
                'receipt-path' { 'keep; verify cited receipt exists' }
                default { 'evidence names a test or check but cites no run receipt; classification needed' }
            }
            $flagRows.Add([pscustomobject][ordered]@{
                    type                 = $type
                    id                   = $rec.id
                    status               = $rec.status
                    criterion            = $c.id
                    text                 = $c.text
                    evidence             = $evidence
                    evidenceClass        = $evidenceClass
                    descriptionCitations = ($citations -join '; ')
                    proposed             = $proposed
                })
        }
    }
}

$byRule = [ordered]@{}
foreach ($g in ($findings | Group-Object rule | Sort-Object Name)) { $byRule[$g.Name] = $g.Count }
$recordsWithFlags = @($flagRows | Select-Object -Property type, id -Unique).Count
$flagsWithDescCitation = @($flagRows | Where-Object { $_.descriptionCitations }).Count

$findings | ConvertTo-Json -Depth 5 | Out-File -LiteralPath (Join-Path $AuditDir 'findings.json') -Encoding utf8NoBOM
$flagRows | ConvertTo-Json -Depth 5 | Out-File -LiteralPath (Join-Path $AuditDir 'satisfied-flags.json') -Encoding utf8NoBOM
$summary = [ordered]@{
    auditedAt                     = (Get-Date -Format s)
    mode                          = 'dry-run (no store writes)'
    counts                        = $counts
    findingsByRule                = $byRule
    distinctRecordsWithFindings   = @($findings | Select-Object -Property type, id -Unique).Count
    satisfiedFlagsTotal           = $flagRows.Count
    recordsWithSatisfiedFlags     = $recordsWithFlags
    satisfiedFlagsWithDescriptionCitation = $flagsWithDescCitation
    satisfiedFlagsByEvidenceClass = [ordered]@{
        none           = @($flagRows | Where-Object evidenceClass -eq 'none').Count
        receiptPath    = @($flagRows | Where-Object evidenceClass -eq 'receipt-path').Count
        referenceOnly  = @($flagRows | Where-Object evidenceClass -eq 'reference-only').Count
    }
}
$summary | ConvertTo-Json -Depth 6 | Out-File -LiteralPath (Join-Path $AuditDir 'audit-summary.json') -Encoding utf8NoBOM
$summary | ConvertTo-Json -Depth 6
