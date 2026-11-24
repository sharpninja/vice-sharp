#Requires -Version 7.4
<#
.SYNOPSIS
    Contract assertions G01-G18 for the R1 requirement-delta coordinator, derived from
    20261011T080155-0500-capacitor-guard-remediation/R0-R1-GUARD-CONTRACT.md and the API supplement in
    20261011T102600-0500-requirements-api-guard/README.md. Each Assert-R1Gnn throws "Gnn: ..." on a
    violation. They inspect a run result: State, Journal, Calls, Store (final), Export, ExportFailed.
    Expected values come from the packet (requirements), never from coordinator output.
#>
Set-StrictMode -Version Latest

$script:Volatile = @('createdAt', 'updatedAt')
$script:Mutating = @('UpdateBatch', 'UpsertMapping')
$script:Forbidden = @('DeleteRecord', 'CreateRecord', 'ReplayQueue', 'SetDone', 'DeleteMapping')

function Get-R1Entry {
    <# Reads a store entry from an ordered dictionary or a deserialized object alike. #>
    param($Map, [string]$Key)
    if ($Map -is [System.Collections.IDictionary]) { return $Map[$Key] }
    $p = $Map.PSObject.Properties[$Key]
    if ($p) { return $p.Value }
    return $null
}

function Get-R1Scenario {
    <# Strict-mode-safe scenario lookup: an absent key is no defect. #>
    param($Context, [string]$Name)
    $s = $Context.Scenario
    if ($s -is [System.Collections.IDictionary]) { if ($s.Contains($Name)) { return $s[$Name] } return $null }
    $p = $s.PSObject.Properties[$Name]
    if ($p) { return $p.Value }
    return $null
}

function ConvertTo-R1Canonical { param($Value) return ($Value | ConvertTo-Json -Depth 30 -Compress) }

function Get-R1TypedFieldName { param([string]$Name) if ($Name -in 'body', 'condition') { return 'description' } return $Name }

function Get-R1ExpectedAfterStore {
    <# Oracle: initial store with every approved changed field set to the packet after-value. #>
    param([Parameter(Mandatory)]$Packet, [Parameter(Mandatory)]$InitialStore)
    $records = [ordered]@{}
    foreach ($k in $InitialStore.Records.Keys) { $records[$k] = ($InitialStore.Records[$k] | ConvertTo-Json -Depth 30 | ConvertFrom-Json) }
    foreach ($r in $Packet.records) {
        $rec = $records["$($r.kind.ToLowerInvariant())|$($r.id)"]
        foreach ($f in $r.changedFields) {
            $rec | Add-Member -NotePropertyName (Get-R1TypedFieldName $f) -NotePropertyValue ($r.after.$f | ConvertTo-Json -Depth 30 | ConvertFrom-Json) -Force
        }
    }
    $mappings = [ordered]@{}
    foreach ($k in $InitialStore.Mappings.Keys) { $mappings[$k] = $InitialStore.Mappings[$k] }
    foreach ($m in $Packet.mappingsAfter) { $mappings[$m.frId] = [pscustomobject]@{ frId = $m.frId; trIds = @($m.trIds); testIds = @($m.testIds) } }
    return [pscustomobject]@{ Records = $records; Mappings = $mappings }
}

function Get-R1RecordDiff {
    <# Field-by-field presence and value diff, excluding the read-synthesized timestamps. #>
    param($Expected, $Actual)
    $diffs = [System.Collections.Generic.List[string]]::new()
    $names = @($Expected.PSObject.Properties.Name + $Actual.PSObject.Properties.Name | Sort-Object -Unique | Where-Object { $_ -notin $script:Volatile })
    foreach ($n in $names) {
        $eHas = $Expected.PSObject.Properties.Name -contains $n
        $aHas = $Actual.PSObject.Properties.Name -contains $n
        if ($eHas -ne $aHas) { $diffs.Add("$n presence expected=$eHas actual=$aHas"); continue }
        if ((ConvertTo-R1Canonical $Expected.$n) -cne (ConvertTo-R1Canonical $Actual.$n)) { $diffs.Add("$n value") }
    }
    return , $diffs
}

function Get-R1EdgeSet { param($Row) return (@(@($Row.trIds | ForEach-Object { "tr|$_" }) + @($Row.testIds | ForEach-Object { "test|$_" })) | Sort-Object -Unique -CaseSensitive) }

function Test-R1MutatingCall { param($Result) return @($Result.Calls | Where-Object { $_.Method -in $script:Mutating }).Count -gt 0 }

function Assert-R1Blocked {
    param($Result, [string]$Group, [string]$Why)
    if ($Result.State -ne 'blocked-before-write') { throw "${Group}: expected blocked-before-write ($Why), got '$($Result.State)'" }
    if (Test-R1MutatingCall $Result) { throw "${Group}: mutating call issued although $Why" }
}

function Assert-R1G01 {
    <# ExactPacketAndApproval. #>
    param($Result, $Context)
    if ((Get-R1Scenario $Context 'ApprovalDefect')) { Assert-R1Blocked $Result 'G01' "approval defect '$((Get-R1Scenario $Context 'ApprovalDefect'))'"; return }
    if (-not @($Result.Journal | Where-Object { $_.State -eq 'approval-verified' }).Count) { throw 'G01: no approval-verified journal entry on a valid binding' }
}

function Assert-R1G02 {
    <# UniqueOrdinalTypedIdentity. #>
    param($Result, $Context)
    if ((Get-R1Scenario $Context 'IdentityDefect')) { Assert-R1Blocked $Result 'G02' "identity defect '$((Get-R1Scenario $Context 'IdentityDefect'))'"; return }
    # Amendment 2026-10-19T14:39: exact reads are required once the run passed identity (any mutation, or an
    # identity-verified entry). A run that stops earlier (approval defect) legitimately reads nothing.
    $pastIdentity = (Test-R1MutatingCall $Result) -or @($Result.Journal | Where-Object { $_.State -eq 'identity-verified' }).Count -gt 0
    if (-not $pastIdentity) { return }
    foreach ($r in $Context.Packet.records) {
        $reads = @($Result.Calls | Where-Object { $_.Method -eq 'GetRecord' -and $_.Kind -ceq $r.kind.ToLowerInvariant() -and $_.Id -ceq $r.id })
        if ($reads.Count -eq 0) { throw "G02: $($r.kind)/$($r.id) never read with exact identity" }
    }
}

function Assert-R1G03 {
    <# PresenceAndType: a presence or type change on an approved field blocks. #>
    param($Result, $Context)
    if ((Get-R1Scenario $Context 'PresenceDefect')) { Assert-R1Blocked $Result 'G03' "presence/type defect '$((Get-R1Scenario $Context 'PresenceDefect'))'" }
}

function Assert-R1G04 {
    <# StableMetadata: every field that is not an approved change survives exactly. #>
    param($Result, $Context)
    foreach ($r in $Context.Packet.records) {
        $key = "$($r.kind.ToLowerInvariant())|$($r.id)"
        $changed = @($r.changedFields | ForEach-Object { Get-R1TypedFieldName $_ })
        $init = $Context.InitialStore.Records[$key]
        $final = (Get-R1Entry $Result.Store.Records $key)
        foreach ($n in @($init.PSObject.Properties.Name + $final.PSObject.Properties.Name | Sort-Object -Unique)) {
            if ($n -in $changed -or $n -in $script:Volatile) { continue }
            $iHas = $init.PSObject.Properties.Name -contains $n
            $fHas = $final.PSObject.Properties.Name -contains $n
            if ($iHas -ne $fHas) { throw "G04: $($r.id).$n presence changed ($iHas -> $fHas)" }
            if ((ConvertTo-R1Canonical $init.$n) -cne (ConvertTo-R1Canonical $final.$n)) { throw "G04: $($r.id).$n stable value changed" }
        }
    }
}

function Assert-R1G05 {
    <# VolatileReadTimestamps: synthesized timestamps never block and never enter a payload. #>
    param($Result, $Context)
    if ((Get-R1Scenario $Context 'VolatileDrift') -and $Result.State -eq 'blocked-before-write') { throw 'G05: volatile timestamp drift blocked the run' }
    foreach ($c in @($Result.Calls | Where-Object Method -eq 'UpdateBatch')) {
        foreach ($u in @($c.Records)) { foreach ($v in $script:Volatile) { if ($u.PSObject.Properties.Name -contains $v) { throw "G05: $v sent in update for $($u.id)" } } }
    }
}

function Assert-R1G06 {
    <# ExactChangedFields: each patch carries exactly kind, id and the approved fields not already at after-value. #>
    param($Result, $Context)
    $skip = @{}
    foreach ($a in @($Context.Attributions)) { $skip["$($a.Kind)|$($a.Id)|$($a.Field)"] = $true }
    foreach ($c in @($Result.Calls | Where-Object Method -eq 'UpdateBatch')) {
        foreach ($u in @($c.Records)) {
            $r = $Context.Packet.records | Where-Object { $_.id -ceq $u.id }
            if (-not $r) { throw "G06: update for unapproved record $($u.id)" }
            $want = @('kind', 'id') + @($r.changedFields | Where-Object { -not $skip.ContainsKey("$($r.kind.ToLowerInvariant())|$($r.id)|$_") } | ForEach-Object { Get-R1TypedFieldName $_ })
            $got = @($u.PSObject.Properties.Name)
            if ((ConvertTo-R1Canonical @($want | Sort-Object)) -cne (ConvertTo-R1Canonical @($got | Sort-Object))) { throw "G06: $($u.id) patch fields [$($got -join ',')] expected [$($want -join ',')]" }
            foreach ($f in $r.changedFields) {
                $tn = Get-R1TypedFieldName $f
                if ($got -notcontains $tn) { continue }
                if ((ConvertTo-R1Canonical $u.$tn) -cne (ConvertTo-R1Canonical $r.after.$f)) { throw "G06: $($u.id).$tn value differs from approved after" }
            }
        }
    }
}

function Assert-R1G07 {
    <# AcIdentityAndMeaning: final criteria equal the approved after-criteria exactly, in order; none satisfied. #>
    param($Result, $Context)
    if ($Result.State -notin 'verified-data-awaiting-gates', 'verified-data-export-failed') { return }
    foreach ($r in $Context.Packet.records) {
        $final = (Get-R1Entry $Result.Store.Records "$($r.kind.ToLowerInvariant())|$($r.id)")
        if ((ConvertTo-R1Canonical @($final.acceptanceCriteria)) -cne (ConvertTo-R1Canonical @($r.after.acceptanceCriteria))) { throw "G07: $($r.id) criteria differ from approved after" }
        if (@($final.acceptanceCriteria | Where-Object { $_.isSatisfied -eq $true }).Count) { throw "G07: $($r.id) has a satisfied criterion" }
    }
}

function Assert-R1G08 {
    <# MappingExistenceAndPreservation. #>
    param($Result, $Context)
    if ((Get-R1Scenario $Context 'MissingTarget')) { Assert-R1Blocked $Result 'G08' "mapping target missing '$((Get-R1Scenario $Context 'MissingTarget'))'"; return }
    foreach ($m in $Context.Packet.mappingsBefore) {
        $final = (Get-R1Entry $Result.Store.Mappings $m.frId)
        $finalSet = Get-R1EdgeSet $final
        foreach ($e in (Get-R1EdgeSet $m)) { if ($finalSet -cnotcontains $e) { throw "G08: $($m.frId) lost old edge $e" } }
        $all = @(@($final.trIds) + @($final.testIds))
        if (@($all | Sort-Object -Unique -CaseSensitive).Count -ne $all.Count) { throw "G08: $($m.frId) has a duplicate edge" }
    }
    foreach ($c in @($Result.Calls | Where-Object Method -eq 'UpsertMapping')) {
        $after = $Context.Packet.mappingsAfter | Where-Object { $_.frId -ceq $c.FrId }
        if (-not $after) { throw "G08: upsert for unapproved row $($c.FrId)" }
        if ((ConvertTo-R1Canonical (Get-R1EdgeSet $c)) -cne (ConvertTo-R1Canonical (Get-R1EdgeSet $after))) { throw "G08: $($c.FrId) upsert is not the full approved union" }
    }
}

function Assert-R1G09 {
    <# DriftBeforeFirstWrite. #>
    param($Result, $Context)
    if ((Get-R1Scenario $Context 'Drift')) { Assert-R1Blocked $Result 'G09' "baseline drift '$((Get-R1Scenario $Context 'Drift'))'" }
}

function Assert-R1G10 {
    <# BatchSuccessNeedsReadback: every record is re-read after the batch before the first mapping write. #>
    param($Result, $Context)
    $calls = @($Result.Calls)
    $batchAt = [Array]::FindIndex([object[]]$calls, [Predicate[object]] { param($c) $c.Method -eq 'UpdateBatch' })
    $firstUpsert = [Array]::FindIndex([object[]]$calls, [Predicate[object]] { param($c) $c.Method -eq 'UpsertMapping' })
    if ($firstUpsert -lt 0) { return }
    if ($batchAt -ge 0) {
        foreach ($r in $Context.Packet.records) {
            $hit = $false
            for ($i = $batchAt + 1; $i -lt $firstUpsert; $i++) { if ($calls[$i].Method -eq 'GetRecord' -and $calls[$i].Id -ceq $r.id) { $hit = $true; break } }
            if (-not $hit) { throw "G10: $($r.id) not re-read between batch and first mapping write" }
        }
    }
    if (-not @($Result.Journal | Where-Object State -eq 'records-confirmed-after').Count) { throw 'G10: mapping stage began without records-confirmed-after' }
}

function Assert-R1G11 {
    <# BatchRejectedNoMutation. #>
    param($Result, $Context)
    if ((Get-R1Scenario $Context 'Fault') -ne 'reject') { return }
    if ($Result.State -ne 'records-confirmed-before') { throw "G11: rejected batch ended in '$($Result.State)'" }
    if (@($Result.Calls | Where-Object Method -eq 'UpsertMapping').Count) { throw 'G11: mapping write after rejected batch' }
    foreach ($k in $Context.InitialStore.Records.Keys) { if ((Get-R1RecordDiff $Context.InitialStore.Records[$k] (Get-R1Entry $Result.Store.Records $k)).Count) { throw "G11: $k changed after rejected batch" } }
}

function Assert-R1G12 {
    <# LostAcknowledgementApplied: exact after-state is recorded as applied; the write is never repeated. #>
    param($Result, $Context)
    if ((Get-R1Scenario $Context 'Fault') -ne 'timeout-applied') { return }
    if (@($Result.Calls | Where-Object Method -eq 'UpdateBatch').Count -ne 1) { throw 'G12: batch repeated after lost acknowledgement' }
    if (-not @($Result.Journal | Where-Object State -eq 'records-confirmed-after').Count) { throw 'G12: applied outcome not recorded as records-confirmed-after' }
}

function Assert-R1G13 {
    <# LostAcknowledgementUnapplied: exact before-state stops; no retry. #>
    param($Result, $Context)
    if ((Get-R1Scenario $Context 'Fault') -ne 'timeout-unapplied') { return }
    if (@($Result.Calls | Where-Object Method -eq 'UpdateBatch').Count -ne 1) { throw 'G13: batch retried' }
    if ($Result.State -ne 'records-confirmed-before') { throw "G13: unapplied outcome ended in '$($Result.State)'" }
    if (@($Result.Calls | Where-Object Method -eq 'UpsertMapping').Count) { throw 'G13: mapping write after unapplied batch' }
}

function Assert-R1G14 {
    <# UnknownOrMixedOutcome: stop with uncertainty; no further writes, no compensation. #>
    param($Result, $Context)
    $want = switch ((Get-R1Scenario $Context 'Fault')) { 'read-unavailable' { 'submitted-outcome-unknown' } 'partial' { 'mixed-or-drift-stop' } 'concurrent' { 'mixed-or-drift-stop' } default { $null } }
    if (-not $want) { return }
    if ($Result.State -ne $want) { throw "G14: expected $want, got '$($Result.State)'" }
    if (@($Result.Calls | Where-Object Method -eq 'UpdateBatch').Count -gt 1) { throw 'G14: compensating or repeated batch issued' }
    if (@($Result.Calls | Where-Object Method -eq 'UpsertMapping').Count) { throw 'G14: mapping write after unknown or mixed outcome' }
}

function Assert-R1G15 {
    <# PartialMappingSequence. #>
    param($Result, $Context)
    if ((Get-R1Scenario $Context 'Fault') -ne 'mapping-fail') { return }
    $n = (Get-R1Scenario $Context 'FailAtRow')
    if ($Result.State -ne 'mapping-partial-stop') { throw "G15: expected mapping-partial-stop, got '$($Result.State)'" }
    $ups = @($Result.Calls | Where-Object Method -eq 'UpsertMapping')
    if ($ups.Count -ne $n) { throw "G15: $($ups.Count) upserts issued, expected exactly $n (stop at the failing row)" }
    if (@($ups.FrId | Sort-Object -Unique -CaseSensitive).Count -ne $ups.Count) { throw 'G15: a row was upserted twice' }
    $confirmed = @($Result.Journal | Where-Object State -eq 'mapping-row-confirmed')
    if ($confirmed.Count -lt ($n - 1)) { throw "G15: only $($confirmed.Count) confirmed rows journaled before the failure" }
}

function Assert-R1G16 {
    <# FinalWholeScopeReadback: final store equals the oracle on every record and row. #>
    param($Result, $Context)
    if ($Result.State -notin 'verified-data-awaiting-gates', 'verified-data-export-failed') { return }
    $expected = Get-R1ExpectedAfterStore -Packet $Context.Packet -InitialStore $Context.InitialStore
    foreach ($r in $Context.Packet.records) {
        $key = "$($r.kind.ToLowerInvariant())|$($r.id)"
        $d = Get-R1RecordDiff $expected.Records[$key] (Get-R1Entry $Result.Store.Records $key)
        if ($d.Count) { throw "G16: $($r.id) final differs: $($d -join '; ')" }
    }
    foreach ($m in $Context.Packet.mappingsAfter) {
        if ((ConvertTo-R1Canonical (Get-R1EdgeSet (Get-R1Entry $Result.Store.Mappings $m.frId))) -cne (ConvertTo-R1Canonical (Get-R1EdgeSet $m))) { throw "G16: mapping $($m.frId) final differs" }
    }
    if (-not @($Result.Journal | Where-Object State -eq 'final-readback-verified').Count) { throw 'G16: no final-readback-verified journal entry' }
}

function Assert-R1G17 {
    <# PersistenceAndExportEvidence. #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Context', Justification = 'Uniform Assert-R1Gnn signature; Assert-R1All passes Context to every group.')]
    param($Result, $Context)
    if ($Result.State -notin 'verified-data-awaiting-gates', 'verified-data-export-failed') { return }
    if ($null -eq $Result.Export -or $null -eq $Result.Export.ExitCode -or [string]::IsNullOrEmpty([string]$Result.Export.Output)) { throw 'G17: export evidence missing' }
    if ($Result.Export.ExitCode -ne 0 -and ($Result.State -ne 'verified-data-export-failed' -or -not $Result.ExportFailed)) { throw 'G17: failed export not surfaced' }
    if ($Result.Export.ExitCode -eq 0 -and $Result.State -ne 'verified-data-awaiting-gates') { throw 'G17: successful export not reflected in state' }
    foreach ($j in @($Result.Journal)) { if (-not ($j.PSObject.Properties.Name -contains 'Persistence') -or $j.Persistence -notin 'journal-primary', 'queued') { throw 'G17: journal entry without confirmed or queued persistence label' } }
}

function Assert-R1G18 {
    <# NoUnapprovedRecoveryOrDone. #>
    param($Result, $Context)
    foreach ($c in @($Result.Calls)) { if ($c.Method -in $script:Forbidden) { throw "G18: forbidden call $($c.Method)" } }
    foreach ($c in @($Result.Calls | Where-Object Method -eq 'UpdateBatch')) {
        foreach ($u in @($c.Records)) {
            if ($u.PSObject.Properties.Name -contains 'status' -and $u.status -in 'completed', 'closed', 'done') { throw "G18: done status sent for $($u.id)" }
            if ($u.PSObject.Properties.Name -contains 'acceptanceCriteria' -and @($u.acceptanceCriteria | Where-Object { $_.isSatisfied -eq $true }).Count) { throw "G18: satisfied criterion sent for $($u.id)" }
        }
    }
    if ((Get-R1Scenario $Context 'Fault') -and @($Result.Calls | Where-Object Method -eq 'UpdateBatch').Count -gt 1) { throw 'G18: recovery batch issued without approval' }
}

function Assert-R1All {
    param($Result, $Context)
    foreach ($n in 1..18) { & "Assert-R1G$('{0:00}' -f $n)" -Result $Result -Context $Context }
}

Export-ModuleMember -Function ConvertTo-R1Canonical, Get-R1TypedFieldName, Get-R1ExpectedAfterStore, Get-R1RecordDiff, Get-R1EdgeSet, Assert-R1G*, Assert-R1All
