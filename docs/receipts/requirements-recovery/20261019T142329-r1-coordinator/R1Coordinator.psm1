#Requires -Version 7.4
<#
.SYNOPSIS
    R1 coordinator for the approved R0 requirement delta (16 records, 9 mapping rows). Implements the
    state machine of R0-R1-GUARD-CONTRACT.md and the API supplement (20261011T102600-0500-requirements-api-guard):
    approval binding, exact typed identity, per-field baseline classification, mapping preflight,
    one UpdateBatch with no blind retry, full readback, row-by-row full-union mapping writes with
    revalidation, final whole-scope readback and export. Written after the G01-G18 mock gate was green
    (mockgate-freeze.json). Talks only to a client exposing GetRecord, UpdateBatch, GetMapping,
    UpsertMapping, Export and Now.

    Journal states: approval-verified, identity-verified, baseline-verified, ready-not-written,
    submitted-outcome-unknown, records-confirmed-after, records-confirmed-before, mixed-or-drift-stop,
    mapping-row-unchanged, mapping-row-already-applied, mapping-row-confirmed, mapping-partial-stop,
    final-readback-verified, verified-data-awaiting-gates, verified-data-export-failed, blocked-before-write.
#>
Set-StrictMode -Version Latest

$script:Volatile = @('createdAt', 'updatedAt')
$script:KindPrefix = @{ fr = 'FR-'; tr = 'TR-'; test = 'TEST-' }

function ConvertTo-R1CanonicalValue {
    param($Value, [string]$Field)
    if ($Field -eq 'description' -and $Value -is [string]) { $Value = $Value -replace "`r`n", "`n" }
    return ($Value | ConvertTo-Json -Depth 30 -Compress)
}

function Get-R1Field {
    param($Object, [string]$Name)
    $p = $Object.PSObject.Properties[$Name]
    if ($p) { return [pscustomobject]@{ Present = $true; Value = $p.Value } }
    return [pscustomobject]@{ Present = $false; Value = $null }
}

function Test-R1FieldEqual {
    param($A, $B, [string]$Field)
    if ($A.Present -ne $B.Present) { return $false }
    return ((ConvertTo-R1CanonicalValue $A.Value $Field) -ceq (ConvertTo-R1CanonicalValue $B.Value $Field))
}

function Read-R1Record {
    <# Calls the client's GetRecord and returns a flat array of records, whether the client emits records or one array of records. #>
    param([hashtable]$Client, [string]$Kind, [string]$Id)
    $out = @(& $Client.GetRecord $Kind $Id)
    if ($out.Count -eq 1 -and $out[0] -is [array]) { $out = @($out[0]) }
    return , $out
}

function Get-R1RowEdgeSet {
    param($Row)
    if ($null -eq $Row) { return @() }
    return (@(@($Row.trIds | Where-Object { $_ } | ForEach-Object { "tr|$_" }) + @($Row.testIds | Where-Object { $_ } | ForEach-Object { "test|$_" })) | Sort-Object -Unique -CaseSensitive)
}

function Invoke-R1Coordinator {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'JournalPath', Justification = 'Used inside the $addJournal scriptblock; the analyzer does not trace scriptblock use.')]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$PacketPath,
        $Approval,
        [Parameter(Mandatory)][hashtable]$Client,
        [object[]]$Attributions = @(),
        [string]$JournalPath
    )

    $journal = [System.Collections.Generic.List[object]]::new()
    $result = [pscustomobject]@{ State = $null; Journal = $journal; Calls = @(); Export = $null; ExportFailed = $false; Records = @{}; Mappings = @{} }

    $addJournal = {
        param([string]$State, [string]$Detail = '', $Data = $null)
        $entry = [pscustomobject]@{ Seq = $journal.Count + 1; At = (Get-Date -Format s); State = $State; Detail = $Detail; Data = $Data; Persistence = 'journal-primary' }
        if ($JournalPath) {
            try { ($entry | ConvertTo-Json -Depth 30 -Compress) | Add-Content -LiteralPath $JournalPath -Encoding utf8NoBOM }
            catch { $entry.Persistence = 'queued' }
        }
        $journal.Add($entry)
    }
    $finish = {
        param([string]$State, [string]$Detail = '')
        & $addJournal $State $Detail
        $result.State = $State
        if ($Client.ContainsKey('State') -and $Client.State.PSObject.Properties['Calls']) { $result.Calls = @($Client.State.Calls) }
        return $result
    }
    $windowOpen = {
        $now = & $Client.Now
        return ($now -ge [datetime]$Approval.WriterWindow.StartLocal -and $now -le [datetime]$Approval.WriterWindow.ExpiryLocal)
    }

    # 1. Approval binding (G01).
    if ($null -eq $Approval) { return (& $finish 'blocked-before-write' 'approval absent') }
    $sha = (Get-FileHash -LiteralPath $PacketPath -Algorithm SHA256).Hash
    if ($Approval.PacketSha256 -cne $sha) { return (& $finish 'blocked-before-write' "packet hash $sha does not match approved $($Approval.PacketSha256)") }
    $packet = Get-Content -LiteralPath $PacketPath -Raw | ConvertFrom-Json
    if ($packet.recordCount -ne $Approval.RecordCount -or @($packet.records).Count -ne $Approval.RecordCount -or @($packet.mappingsAfter).Count -ne $Approval.MappingRowCount) {
        return (& $finish 'blocked-before-write' 'approval scope does not match packet scope')
    }
    if ($Approval.LockedDecision2DepartureAccepted -ne $true) { return (& $finish 'blocked-before-write' 'locked decision 2 departure not accepted') }
    if (-not (& $windowOpen)) { return (& $finish 'blocked-before-write' 'exclusive-writer window not open') }
    & $addJournal 'approval-verified' "packet $sha, $($Approval.RecordCount) records, $($Approval.MappingRowCount) rows, window $($Approval.WriterWindow.StartLocal)..$($Approval.WriterWindow.ExpiryLocal)"

    # Packet invariants: nothing satisfied, nothing done.
    foreach ($r in $packet.records) {
        if (@($r.after.acceptanceCriteria | Where-Object { $_.isSatisfied -eq $true }).Count) { return (& $finish 'blocked-before-write' "$($r.id) after-state has a satisfied criterion") }
        if ($r.after.status -in 'completed', 'closed', 'done') { return (& $finish 'blocked-before-write' "$($r.id) after-state is done") }
    }

    # 2. Identity (G02) and preimages.
    $pre = @{}
    foreach ($r in $packet.records) {
        $kind = $r.kind.ToLowerInvariant()
        try { $got = Read-R1Record $Client $kind $r.id } catch { return (& $finish 'blocked-before-write' "baseline read failed for $($r.id): $_") }
        if ($got.Count -ne 1) { return (& $finish 'blocked-before-write' "identity: $($got.Count) records for $kind/$($r.id)") }
        if ([string]$got[0].id -cne $r.id -or -not ([string]$got[0].id).StartsWith($script:KindPrefix[$kind], [StringComparison]::Ordinal)) { return (& $finish 'blocked-before-write' "identity: $kind/$($r.id) returned '$($got[0].id)'") }
        $pre["$kind|$($r.id)"] = $got[0]
    }
    & $addJournal 'identity-verified' "$($pre.Count) exact typed identities"

    # 3. Baseline per approved field (G03, G09) with attributed after-values.
    $attr = @{}
    foreach ($a in $Attributions) { $attr["$($a.Kind)|$($a.Id)|$($a.Field)"] = $a }
    $plan = [System.Collections.Generic.List[object]]::new()
    foreach ($r in $packet.records) {
        $kind = $r.kind.ToLowerInvariant()
        $live = $pre["$kind|$($r.id)"]
        $fields = [ordered]@{}
        foreach ($f in $r.changedFields) {
            $tn = if ($f -in 'body', 'condition') { 'description' } else { $f }
            $b = Get-R1Field $r.before $f
            $a = Get-R1Field $r.after $f
            $l = Get-R1Field $live $tn
            if (Test-R1FieldEqual $b $l $tn) { $fields[$tn] = $a.Value; continue }
            $key = "$kind|$($r.id)|$f"
            if ((Test-R1FieldEqual $a $l $tn) -and $attr.ContainsKey($key)) {
                $at = $attr[$key]
                $ok = $false
                if ($at.JournalPath -and (Test-Path -LiteralPath $at.JournalPath) -and (Get-FileHash -LiteralPath $at.JournalPath -Algorithm SHA256).Hash -ceq $at.JournalSha256) {
                    foreach ($line in Get-Content -LiteralPath $at.JournalPath) {
                        $j = $line | ConvertFrom-Json
                        if ($j.phase -eq 'after' -and $j.id -ceq $r.id -and $j.record.PSObject.Properties[$tn] -and (ConvertTo-R1CanonicalValue $j.record.$tn $tn) -ceq (ConvertTo-R1CanonicalValue $a.Value $tn)) { $ok = $true }
                    }
                }
                if ($ok) { & $addJournal 'baseline-verified' "$($r.id).$tn already at approved after-value; attributed to $($at.JournalPath)"; continue }
                return (& $finish 'blocked-before-write' "$($r.id).$tn attribution not verifiable")
            }
            return (& $finish 'blocked-before-write' "baseline drift or presence/type difference on $($r.id).$tn")
        }
        if ($fields.Count) {
            $patch = [ordered]@{ kind = $kind; id = $r.id }
            foreach ($k in $fields.Keys) { $patch[$k] = $fields[$k] }
            $plan.Add([pscustomobject]$patch)
        }
    }
    & $addJournal 'baseline-verified' "$($packet.records.Count) records match approved before-state on every approved field"

    # Mapping preflight and target existence (G08).
    $rows = [System.Collections.Generic.List[object]]::new()
    foreach ($m in $packet.mappingsAfter) {
        $all = @(@($m.trIds) + @($m.testIds))
        if (@($all | ForEach-Object { $_.ToUpperInvariant() } | Sort-Object -Unique).Count -ne $all.Count) { return (& $finish 'blocked-before-write' "duplicate or case-only edge in approved row $($m.frId)") }
        foreach ($pair in @(@($m.trIds | ForEach-Object { [pscustomobject]@{ K = 'tr'; Id = $_ } }) + @($m.testIds | ForEach-Object { [pscustomobject]@{ K = 'test'; Id = $_ } }))) {
            try { $g = Read-R1Record $Client $pair.K $pair.Id } catch { return (& $finish 'blocked-before-write' "target read failed for $($pair.Id): $_") }
            if ($g.Count -ne 1 -or [string]$g[0].id -cne $pair.Id) { return (& $finish 'blocked-before-write' "mapping target missing or ambiguous: $($pair.K)/$($pair.Id)") }
        }
        $before = $packet.mappingsBefore | Where-Object { $_.frId -ceq $m.frId }
        try { $liveRow = & $Client.GetMapping $m.frId } catch { return (& $finish 'blocked-before-write' "mapping read failed for $($m.frId): $_") }
        $lset = ConvertTo-R1CanonicalValue (Get-R1RowEdgeSet $liveRow) ''
        $bset = ConvertTo-R1CanonicalValue (Get-R1RowEdgeSet $before) ''
        $aset = ConvertTo-R1CanonicalValue (Get-R1RowEdgeSet $m) ''
        $mode = if ($bset -ceq $aset -and $lset -ceq $bset) { 'unchanged' } elseif ($lset -ceq $bset) { 'planned' } elseif ($lset -ceq $aset) { 'already-applied' } else { $null }
        if (-not $mode) { return (& $finish 'blocked-before-write' "mapping drift on $($m.frId)") }
        $rows.Add([pscustomobject]@{ Row = $m; Before = $before; Mode = $mode })
    }

    $payloadJson = ConvertTo-Json -InputObject @($plan) -Depth 30 -Compress
    $digest = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($payloadJson)))
    & $addJournal 'ready-not-written' "$($plan.Count) record patches, digest $digest" @($plan)

    # 4. One batch, no blind retry (G10-G14).
    $expectRecord = {
        param($r, $live)
        $kind = $r.kind.ToLowerInvariant()
        $p0 = $pre["$kind|$($r.id)"]
        $changed = @($r.changedFields | ForEach-Object { if ($_ -in 'body', 'condition') { 'description' } else { $_ } })
        $isAfter = $true; $isBefore = $true
        foreach ($f in $r.changedFields) {
            $tn = if ($f -in 'body', 'condition') { 'description' } else { $f }
            $l = Get-R1Field $live $tn
            if (-not (Test-R1FieldEqual (Get-R1Field $r.after $f) $l $tn)) { $isAfter = $false }
            if (-not (Test-R1FieldEqual (Get-R1Field $p0 $tn) $l $tn)) { $isBefore = $false }
        }
        foreach ($n in @($p0.PSObject.Properties.Name + $live.PSObject.Properties.Name | Sort-Object -Unique)) {
            if ($n -in $changed -or $n -in $script:Volatile) { continue }
            if (-not (Test-R1FieldEqual (Get-R1Field $p0 $n) (Get-R1Field $live $n) $n)) { $isAfter = $false; $isBefore = $false }
        }
        if ($isAfter) { return 'after' }
        if ($isBefore) { return 'before' }
        return 'mixed'
    }
    $readAll = {
        $states = @{}
        foreach ($r in $packet.records) {
            $got = Read-R1Record $Client $r.kind.ToLowerInvariant() $r.id
            if ($got.Count -ne 1) { throw "readback returned $($got.Count) records for $($r.id)" }
            $states[$r.id] = & $expectRecord $r $got[0]
        }
        return $states
    }

    if ($plan.Count) {
        if (-not (& $windowOpen)) { return (& $finish 'blocked-before-write' 'exclusive-writer window closed before the batch') }
        $batchError = $null
        try { $null = & $Client.UpdateBatch @($plan) } catch { $batchError = "$_" }
        try { $states = & $readAll } catch { return (& $finish 'submitted-outcome-unknown' "batch error '$batchError'; readback unavailable: $_") }
        $vals = @($states.Values | Sort-Object -Unique)
        if ($vals.Count -eq 1 -and $vals[0] -eq 'after') { & $addJournal 'records-confirmed-after' $(if ($batchError) { "applied despite lost acknowledgement: $batchError" } else { 'batch applied; full readback exact' }) }
        elseif ($batchError -and $vals.Count -eq 1 -and $vals[0] -eq 'before') { return (& $finish 'records-confirmed-before' "batch not applied: $batchError") }
        else { return (& $finish 'mixed-or-drift-stop' "batch error '$batchError'; per-record states: $(($states.GetEnumerator() | Sort-Object Key | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ', ')") }
    }
    else { & $addJournal 'records-confirmed-after' 'no record patch needed; every approved field already at after-value' }

    # 5. Mapping rows, one at a time, full union, revalidated (G08, G15).
    foreach ($x in $rows) {
        $m = $x.Row
        if ($x.Mode -eq 'unchanged') { & $addJournal 'mapping-row-unchanged' $m.frId; continue }
        if ($x.Mode -eq 'already-applied') { & $addJournal 'mapping-row-already-applied' $m.frId; continue }
        if (-not (& $windowOpen)) { return (& $finish 'mapping-partial-stop' "exclusive-writer window closed before $($m.frId)") }
        $bset = ConvertTo-R1CanonicalValue (Get-R1RowEdgeSet $x.Before) ''
        $aset = ConvertTo-R1CanonicalValue (Get-R1RowEdgeSet $m) ''
        try { $now = ConvertTo-R1CanonicalValue (Get-R1RowEdgeSet (& $Client.GetMapping $m.frId)) '' } catch { return (& $finish 'mapping-partial-stop' "revalidation read failed for $($m.frId): $_") }
        if ($now -cne $bset) { return (& $finish 'mixed-or-drift-stop' "mapping drift before write on $($m.frId)") }
        $upErr = $null
        try { $null = & $Client.UpsertMapping $m.frId @($m.trIds) @($m.testIds) } catch { $upErr = "$_" }
        try { $after = ConvertTo-R1CanonicalValue (Get-R1RowEdgeSet (& $Client.GetMapping $m.frId)) '' } catch { return (& $finish 'mapping-partial-stop' "row $($m.frId) outcome unknown: $upErr; readback failed: $_") }
        if ($after -ceq $aset) { & $addJournal 'mapping-row-confirmed' $(if ($upErr) { "$($m.frId) applied despite error: $upErr" } else { $m.frId }); continue }
        if ($after -ceq $bset) { return (& $finish 'mapping-partial-stop' "row $($m.frId) not applied: $upErr") }
        return (& $finish 'mixed-or-drift-stop' "row $($m.frId) third state after upsert")
    }

    # 6. Final whole-scope readback (G16).
    try { $states = & $readAll } catch { return (& $finish 'submitted-outcome-unknown' "final readback unavailable: $_") }
    if (@($states.Values | Where-Object { $_ -ne 'after' }).Count) { return (& $finish 'mixed-or-drift-stop' 'final readback differs from approved after-state') }
    foreach ($x in $rows) {
        try { $fin = ConvertTo-R1CanonicalValue (Get-R1RowEdgeSet (& $Client.GetMapping $x.Row.frId)) '' } catch { return (& $finish 'submitted-outcome-unknown' "final mapping readback unavailable: $_") }
        if ($fin -cne (ConvertTo-R1CanonicalValue (Get-R1RowEdgeSet $x.Row) '')) { return (& $finish 'mixed-or-drift-stop' "final mapping $($x.Row.frId) differs") }
    }
    & $addJournal 'final-readback-verified' "$($packet.records.Count) records and $($rows.Count) rows equal approved after-state"

    # 7. Export evidence (G17).
    try { $result.Export = & $Client.Export } catch { $result.Export = [pscustomobject]@{ ExitCode = -1; Output = "export threw: $_" } }
    if ($result.Export.ExitCode -ne 0) { $result.ExportFailed = $true; return (& $finish 'verified-data-export-failed' "export exit $($result.Export.ExitCode)") }
    return (& $finish 'verified-data-awaiting-gates' 'records and mappings verified; requirement-gate review deferred per D3')
}

Export-ModuleMember -Function Invoke-R1Coordinator
