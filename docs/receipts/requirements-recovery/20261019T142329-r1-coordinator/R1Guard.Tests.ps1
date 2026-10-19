#Requires -Version 7.4
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.7.0' }
<#
    R1 guard suite, G01-G18 (R0-R1-GUARD-CONTRACT.md). Two phases, BDPv4:

    Tag MockGate: each group's assertion is run against a scripted CORRECT result (must pass) and every
    scripted CORRUPT result named by the contract (must be rejected inside this green harness). No
    coordinator code is involved. This is the mock gate that precedes production code.

    Tag Binding: the SAME scenarios and the SAME Assert-R1All are bound to the real coordinator
    (R1Coordinator.psm1) driven by the scripted fake client. Run only after the mock gate is green.

    Fixture: the real R0 packet (proposed-field-delta.json, SHA256 63386E92...55D702) and a fake store
    built from its before-state. Expected values come from the packet, never from coordinator output.
#>
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'Test helpers build in-memory scripted results only; no system state changes.')]
param()

BeforeAll {
    $here = $PSScriptRoot
    Import-Module (Join-Path $here 'R1FakeClient.psm1') -Force
    Import-Module (Join-Path $here 'R1Assertions.psm1') -Force
    $script:PacketPath = Join-Path $here '..\20261013T231703-r0-anchor-remediation\proposed-field-delta.json'
    $script:PacketSha = (Get-FileHash -LiteralPath $script:PacketPath -Algorithm SHA256).Hash
    $script:Packet = Get-Content -LiteralPath $script:PacketPath -Raw | ConvertFrom-Json

    function New-Ctx {
        param([hashtable]$Scenario = @{}, [object[]]$Attributions = @())
        [pscustomobject]@{ Packet = $script:Packet; InitialStore = (New-R1FixtureStore $script:Packet); Scenario = $Scenario; Attributions = $Attributions }
    }

    function New-Journal {
        param([string[]]$States)
        $i = 0
        foreach ($s in $States) { $i++; [pscustomobject]@{ Seq = $i; State = $s; Detail = ''; Persistence = 'journal-primary' } }
    }

    function New-Patch {
        param($Ctx)
        foreach ($r in $Ctx.Packet.records) {
            $p = [ordered]@{ kind = $r.kind.ToLowerInvariant(); id = $r.id }
            foreach ($f in $r.changedFields) { $p[(Get-R1TypedFieldName $f)] = $r.after.$f }
            [pscustomobject]$p
        }
    }

    function New-HappyResult {
        <# Scripted correct end-to-end result, built from the packet oracle (not from any coordinator). #>
        param($Ctx)
        $calls = [System.Collections.Generic.List[object]]::new()
        foreach ($r in $Ctx.Packet.records) { $calls.Add([pscustomobject]@{ Method = 'GetRecord'; Kind = $r.kind.ToLowerInvariant(); Id = $r.id }) }
        $calls.Add([pscustomobject]@{ Method = 'UpdateBatch'; Records = @(New-Patch $Ctx) })
        foreach ($r in $Ctx.Packet.records) { $calls.Add([pscustomobject]@{ Method = 'GetRecord'; Kind = $r.kind.ToLowerInvariant(); Id = $r.id }) }
        $rowStates = @()
        foreach ($m in $Ctx.Packet.mappingsAfter) {
            $before = $Ctx.Packet.mappingsBefore | Where-Object frId -eq $m.frId
            $calls.Add([pscustomobject]@{ Method = 'GetMapping'; FrId = $m.frId })
            if ((ConvertTo-R1Canonical (Get-R1EdgeSet $before)) -ceq (ConvertTo-R1Canonical (Get-R1EdgeSet $m))) { $rowStates += 'mapping-row-unchanged'; continue }
            $calls.Add([pscustomobject]@{ Method = 'UpsertMapping'; FrId = $m.frId; TrIds = @($m.trIds); TestIds = @($m.testIds) })
            $calls.Add([pscustomobject]@{ Method = 'GetMapping'; FrId = $m.frId })
            $rowStates += 'mapping-row-confirmed'
        }
        $calls.Add([pscustomobject]@{ Method = 'Export' })
        $states = @('approval-verified', 'identity-verified', 'baseline-verified', 'ready-not-written', 'records-confirmed-after') + $rowStates + @('final-readback-verified', 'verified-data-awaiting-gates')
        [pscustomobject]@{
            State        = 'verified-data-awaiting-gates'
            Journal      = @(New-Journal $states)
            Calls        = @($calls)
            Store        = (Get-R1ExpectedAfterStore -Packet $Ctx.Packet -InitialStore $Ctx.InitialStore)
            Export       = [pscustomobject]@{ ExitCode = 0; Output = 'export ok' }
            ExportFailed = $false
        }
    }

    function New-StopResult {
        param($Ctx, [string]$State, [int]$Batches = 0, [switch]$AppliedStore)
        $calls = @(foreach ($r in $Ctx.Packet.records) { [pscustomobject]@{ Method = 'GetRecord'; Kind = $r.kind.ToLowerInvariant(); Id = $r.id } })
        for ($i = 0; $i -lt $Batches; $i++) { $calls += [pscustomobject]@{ Method = 'UpdateBatch'; Records = @(New-Patch $Ctx) } }
        $store = if ($AppliedStore) { Get-R1ExpectedAfterStore -Packet $Ctx.Packet -InitialStore $Ctx.InitialStore } else { $Ctx.InitialStore }
        [pscustomobject]@{ State = $State; Journal = @(New-Journal @($State)); Calls = $calls; Store = $store; Export = $null; ExportFailed = $false }
    }

    function Copy-Result { param($R) $R | ConvertTo-Json -Depth 40 | ConvertFrom-Json }

    function Set-StoreField {
        param($Result, [string]$Key, [string]$Field, $Value)
        $Result.Store.Records.$Key | Add-Member -NotePropertyName $Field -NotePropertyValue $Value -Force
    }
}

Describe 'G01 ExactPacketAndApproval' -Tag MockGate {
    It 'passes a correct binding' { $c = New-Ctx; { Assert-R1G01 (New-HappyResult $c) $c } | Should -Not -Throw }
    It 'passes a blocked run when approval is absent' { $c = New-Ctx @{ ApprovalDefect = 'absent' }; { Assert-R1G01 (New-StopResult $c 'blocked-before-write') $c } | Should -Not -Throw }
    It 'rejects a fake that reports approved anyway (<_>)' -ForEach 'absent', 'stale-hash', 'changed-nested-ac', 'prior-scope' {
        $c = New-Ctx @{ ApprovalDefect = $_ }; { Assert-R1G01 (New-HappyResult $c) $c } | Should -Throw '*G01*'
    }
    It 'rejects a valid binding without an approval-verified entry' {
        $c = New-Ctx; $r = New-HappyResult $c; $r.Journal = @($r.Journal | Where-Object State -ne 'approval-verified'); { Assert-R1G01 $r $c } | Should -Throw '*G01*'
    }
}

Describe 'G02 UniqueOrdinalTypedIdentity' -Tag MockGate {
    It 'passes exact identities' { $c = New-Ctx; { Assert-R1G02 (New-HappyResult $c) $c } | Should -Not -Throw }
    It 'rejects a fake that proceeds on <_>' -ForEach 'missing', 'duplicate', 'wrong-kind', 'case-only' {
        $c = New-Ctx @{ IdentityDefect = $_ }; { Assert-R1G02 (New-HappyResult $c) $c } | Should -Throw '*G02*'
    }
    It 'passes a blocked run on a duplicate' { $c = New-Ctx @{ IdentityDefect = 'duplicate' }; { Assert-R1G02 (New-StopResult $c 'blocked-before-write') $c } | Should -Not -Throw }
    It 'rejects a fake that substitutes a differently cased read' {
        $c = New-Ctx; $r = New-HappyResult $c; $r.Calls = @($r.Calls | ForEach-Object { if ($_.Method -eq 'GetRecord' -and $_.Id -eq 'FR-CPU-004') { [pscustomobject]@{ Method = 'GetRecord'; Kind = 'fr'; Id = 'fr-cpu-004' } } else { $_ } })
        { Assert-R1G02 $r $c } | Should -Throw '*G02*'
    }
}

Describe 'G03 PresenceAndType' -Tag MockGate {
    It 'passes a blocked run on <_>' -ForEach 'present-null', 'absent', 'empty-array', 'string-for-number', 'bool-for-string' {
        $c = New-Ctx @{ PresenceDefect = $_ }; { Assert-R1G03 (New-StopResult $c 'blocked-before-write') $c } | Should -Not -Throw
    }
    It 'rejects a fake that null-coalesces <_>' -ForEach 'present-null', 'absent', 'empty-array' {
        $c = New-Ctx @{ PresenceDefect = $_ }; { Assert-R1G03 (New-HappyResult $c) $c } | Should -Throw '*G03*'
    }
}

Describe 'G04 StableMetadata' -Tag MockGate {
    It 'passes when every unmodified field survives' { $c = New-Ctx; { Assert-R1G04 (New-HappyResult $c) $c } | Should -Not -Throw }
    It 'rejects cleared notes' { $c = New-Ctx; $r = New-HappyResult $c; Set-StoreField $r 'fr|FR-CPU-004' 'notes' $null; { Assert-R1G04 $r $c } | Should -Throw '*G04*' }
    It 'rejects a dropped field' { $c = New-Ctx; $r = Copy-Result (New-HappyResult $c); $r.Store.Records.'fr|FR-TAP-001'.PSObject.Properties.Remove('area'); { Assert-R1G04 $r $c } | Should -Throw '*G04*' }
    It 'rejects a changed area' { $c = New-Ctx; $r = New-HappyResult $c; Set-StoreField $r 'test|TEST-CPU-001' 'area' 'XXX'; { Assert-R1G04 $r $c } | Should -Throw '*G04*' }
    It 'rejects an unclassified new field' { $c = New-Ctx; $r = New-HappyResult $c; Set-StoreField $r 'tr|TR-TAP-EDGE-001' 'surprise' 1; { Assert-R1G04 $r $c } | Should -Throw '*G04*' }
}

Describe 'G05 VolatileReadTimestamps' -Tag MockGate {
    It 'passes a completed run under timestamp drift' { $c = New-Ctx @{ VolatileDrift = $true }; { Assert-R1G05 (New-HappyResult $c) $c } | Should -Not -Throw }
    It 'rejects a fake that blocks on timestamp drift' { $c = New-Ctx @{ VolatileDrift = $true }; { Assert-R1G05 (New-StopResult $c 'blocked-before-write') $c } | Should -Throw '*G05*' }
    It 'rejects a fake that writes observed timestamps' {
        $c = New-Ctx; $r = New-HappyResult $c; ($r.Calls | Where-Object Method -eq 'UpdateBatch').Records[0] | Add-Member -NotePropertyName updatedAt -NotePropertyValue '2026-10-19T00:00:00'
        { Assert-R1G05 $r $c } | Should -Throw '*G05*'
    }
}

Describe 'G06 ExactChangedFields' -Tag MockGate {
    It 'passes exact patches' { $c = New-Ctx; { Assert-R1G06 (New-HappyResult $c) $c } | Should -Not -Throw }
    It 'rejects a body alias' { $c = New-Ctx; $r = New-HappyResult $c; ($r.Calls | Where-Object Method -eq 'UpdateBatch').Records[0] | Add-Member -NotePropertyName body -NotePropertyValue 'x'; { Assert-R1G06 $r $c } | Should -Throw '*G06*' }
    It 'rejects an unapproved status' { $c = New-Ctx; $r = New-HappyResult $c; (($r.Calls | Where-Object Method -eq 'UpdateBatch').Records | Where-Object id -eq 'FR-CPU-004') | Add-Member -NotePropertyName status -NotePropertyValue 'pending'; { Assert-R1G06 $r $c } | Should -Throw '*G06*' }
    It 'rejects the whole projection' { $c = New-Ctx; $r = New-HappyResult $c; ($r.Calls | Where-Object Method -eq 'UpdateBatch').Records[1] | Add-Member -NotePropertyName notes -NotePropertyValue 'fixture'; { Assert-R1G06 $r $c } | Should -Throw '*G06*' }
    It 'rejects a value that is not the approved after-value' { $c = New-Ctx; $r = New-HappyResult $c; ($r.Calls | Where-Object Method -eq 'UpdateBatch').Records[2].description = 'stale'; { Assert-R1G06 $r $c } | Should -Throw '*G06*' }
    It 'rejects resending a field already attributed at its after-value' {
        $c = New-Ctx -Attributions @([pscustomobject]@{ Kind = 'fr'; Id = 'FR-BACKFILL-MEDIA-001'; Field = 'status' }); { Assert-R1G06 (New-HappyResult $c) $c } | Should -Throw '*G06*'
    }
}

Describe 'G07 AcIdentityAndMeaning' -Tag MockGate {
    It 'passes exact criteria' { $c = New-Ctx; { Assert-R1G07 (New-HappyResult $c) $c } | Should -Not -Throw }
    It 'rejects a dropped criterion with the count kept' {
        $c = New-Ctx; $r = Copy-Result (New-HappyResult $c); $ac = @($r.Store.Records.'fr|FR-TAP-005'.acceptanceCriteria); $ac[1] = $ac[0]; $r.Store.Records.'fr|FR-TAP-005'.acceptanceCriteria = $ac
        { Assert-R1G07 $r $c } | Should -Throw '*G07*'
    }
    It 'rejects stale text' { $c = New-Ctx; $r = Copy-Result (New-HappyResult $c); $r.Store.Records.'fr|FR-CPU-004'.acceptanceCriteria[0].text = 'old'; { Assert-R1G07 $r $c } | Should -Throw '*G07*' }
    It 'rejects a satisfied criterion' { $c = New-Ctx; $r = Copy-Result (New-HappyResult $c); $r.Store.Records.'test|TEST-CPU-001'.acceptanceCriteria[0].isSatisfied = $true; { Assert-R1G07 $r $c } | Should -Throw '*G07*' }
    It 'rejects added evidence' { $c = New-Ctx; $r = Copy-Result (New-HappyResult $c); $r.Store.Records.'fr|FR-TAP-001'.acceptanceCriteria[0] | Add-Member -NotePropertyName evidence -NotePropertyValue 'x'; { Assert-R1G07 $r $c } | Should -Throw '*G07*' }
}

Describe 'G08 MappingExistenceAndPreservation' -Tag MockGate {
    It 'passes full-union upserts' { $c = New-Ctx; { Assert-R1G08 (New-HappyResult $c) $c } | Should -Not -Throw }
    It 'rejects a lost old edge' { $c = New-Ctx; $r = Copy-Result (New-HappyResult $c); $r.Store.Mappings.'FR-TAP-001'.trIds = @($r.Store.Mappings.'FR-TAP-001'.trIds | Select-Object -Skip 1); { Assert-R1G08 $r $c } | Should -Throw '*G08*' }
    It 'rejects an additions-only upsert' {
        $c = New-Ctx; $r = New-HappyResult $c; $u = $r.Calls | Where-Object Method -eq 'UpsertMapping' | Select-Object -First 1
        $before = $c.Packet.mappingsBefore | Where-Object frId -eq $u.FrId; $u.TrIds = @($u.TrIds | Where-Object { $_ -notin $before.trIds }); { Assert-R1G08 $r $c } | Should -Throw '*G08*'
    }
    It 'rejects a duplicate edge' { $c = New-Ctx; $r = Copy-Result (New-HappyResult $c); $r.Store.Mappings.'FR-TAP-002'.testIds = @($r.Store.Mappings.'FR-TAP-002'.testIds) + @($r.Store.Mappings.'FR-TAP-002'.testIds)[0]; { Assert-R1G08 $r $c } | Should -Throw '*G08*' }
    It 'passes a blocked run on a missing target' { $c = New-Ctx @{ MissingTarget = 'TEST-IEC-TIMING-001' }; { Assert-R1G08 (New-StopResult $c 'blocked-before-write') $c } | Should -Not -Throw }
    It 'rejects a fake that admits a missing target' { $c = New-Ctx @{ MissingTarget = 'TEST-IEC-TIMING-001' }; { Assert-R1G08 (New-HappyResult $c) $c } | Should -Throw '*G08*' }
}

Describe 'G09 DriftBeforeFirstWrite' -Tag MockGate {
    It 'passes a blocked run on drift' { $c = New-Ctx @{ Drift = 'FR-CPU-004 description' }; { Assert-R1G09 (New-StopResult $c 'blocked-before-write') $c } | Should -Not -Throw }
    It 'rejects a fake that compares a cached projection' { $c = New-Ctx @{ Drift = 'FR-CPU-004 description' }; { Assert-R1G09 (New-HappyResult $c) $c } | Should -Throw '*G09*' }
    It 'rejects a blocked state that still wrote' { $c = New-Ctx @{ Drift = 'x' }; { Assert-R1G09 (New-StopResult $c 'blocked-before-write' -Batches 1) $c } | Should -Throw '*G09*' }
}

Describe 'G10 BatchSuccessNeedsReadback' -Tag MockGate {
    It 'passes readback before mappings' { $c = New-Ctx; { Assert-R1G10 (New-HappyResult $c) $c } | Should -Not -Throw }
    It 'rejects mapping before full readback' {
        $c = New-Ctx; $r = New-HappyResult $c; $b = [Array]::FindIndex([object[]]$r.Calls, [Predicate[object]] { param($x) $x.Method -eq 'UpdateBatch' })
        $r.Calls = @($r.Calls[0..$b]) + @($r.Calls[($b + 1)..($r.Calls.Count - 1)] | Where-Object { -not ($_.Method -eq 'GetRecord' -and $_.Id -eq 'TEST-PRF-001') }); { Assert-R1G10 $r $c } | Should -Throw '*G10*'
    }
    It 'rejects count-only success' { $c = New-Ctx; $r = New-HappyResult $c; $r.Journal = @($r.Journal | Where-Object State -ne 'records-confirmed-after'); { Assert-R1G10 $r $c } | Should -Throw '*G10*' }
}

Describe 'G11 BatchRejectedNoMutation' -Tag MockGate {
    It 'passes an unchanged stop' { $c = New-Ctx @{ Fault = 'reject' }; { Assert-R1G11 (New-StopResult $c 'records-confirmed-before' -Batches 1) $c } | Should -Not -Throw }
    It 'rejects a changed record' { $c = New-Ctx @{ Fault = 'reject' }; { Assert-R1G11 (New-StopResult $c 'records-confirmed-before' -Batches 1 -AppliedStore) $c } | Should -Throw '*G11*' }
    It 'rejects a mapping call' { $c = New-Ctx @{ Fault = 'reject' }; $r = New-StopResult $c 'records-confirmed-before' -Batches 1; $r.Calls += [pscustomobject]@{ Method = 'UpsertMapping'; FrId = 'FR-TAP-001'; TrIds = @(); TestIds = @() }; { Assert-R1G11 $r $c } | Should -Throw '*G11*' }
    It 'rejects reported success' { $c = New-Ctx @{ Fault = 'reject' }; { Assert-R1G11 (New-HappyResult $c) $c } | Should -Throw '*G11*' }
}

Describe 'G12 LostAcknowledgementApplied' -Tag MockGate {
    It 'passes applied-once' { $c = New-Ctx @{ Fault = 'timeout-applied' }; { Assert-R1G12 (New-HappyResult $c) $c } | Should -Not -Throw }
    It 'rejects a blind retry' { $c = New-Ctx @{ Fault = 'timeout-applied' }; $r = New-HappyResult $c; $r.Calls += ($r.Calls | Where-Object Method -eq 'UpdateBatch'); { Assert-R1G12 $r $c } | Should -Throw '*G12*' }
    It 'rejects reporting not applied' { $c = New-Ctx @{ Fault = 'timeout-applied' }; { Assert-R1G12 (New-StopResult $c 'records-confirmed-before' -Batches 1 -AppliedStore) $c } | Should -Throw '*G12*' }
}

Describe 'G13 LostAcknowledgementUnapplied' -Tag MockGate {
    It 'passes a before-state stop' { $c = New-Ctx @{ Fault = 'timeout-unapplied' }; { Assert-R1G13 (New-StopResult $c 'records-confirmed-before' -Batches 1) $c } | Should -Not -Throw }
    It 'rejects reported success' { $c = New-Ctx @{ Fault = 'timeout-unapplied' }; { Assert-R1G13 (New-HappyResult $c) $c } | Should -Throw '*G13*' }
    It 'rejects a retry without revalidation' { $c = New-Ctx @{ Fault = 'timeout-unapplied' }; { Assert-R1G13 (New-StopResult $c 'records-confirmed-before' -Batches 2) $c } | Should -Throw '*G13*' }
}

Describe 'G14 UnknownOrMixedOutcome' -Tag MockGate {
    It 'passes unknown on unavailable readback' { $c = New-Ctx @{ Fault = 'read-unavailable' }; { Assert-R1G14 (New-StopResult $c 'submitted-outcome-unknown' -Batches 1) $c } | Should -Not -Throw }
    It 'rejects treating missing readback as before-state' { $c = New-Ctx @{ Fault = 'read-unavailable' }; { Assert-R1G14 (New-StopResult $c 'records-confirmed-before' -Batches 1) $c } | Should -Throw '*G14*' }
    It 'rejects continuing to mappings' { $c = New-Ctx @{ Fault = 'read-unavailable' }; $r = New-StopResult $c 'submitted-outcome-unknown' -Batches 1; $r.Calls += [pscustomobject]@{ Method = 'UpsertMapping'; FrId = 'FR-TAP-001'; TrIds = @(); TestIds = @() }; { Assert-R1G14 $r $c } | Should -Throw '*G14*' }
    It 'passes a mixed stop on partial batch' { $c = New-Ctx @{ Fault = 'partial' }; { Assert-R1G14 (New-StopResult $c 'mixed-or-drift-stop' -Batches 1) $c } | Should -Not -Throw }
    It 'rejects unapproved compensation' { $c = New-Ctx @{ Fault = 'partial' }; { Assert-R1G14 (New-StopResult $c 'mixed-or-drift-stop' -Batches 2) $c } | Should -Throw '*G14*' }
    It 'passes a mixed stop on a concurrent third state' { $c = New-Ctx @{ Fault = 'concurrent' }; { Assert-R1G14 (New-StopResult $c 'mixed-or-drift-stop' -Batches 1) $c } | Should -Not -Throw }
}

Describe 'G15 PartialMappingSequence' -Tag MockGate {
    BeforeAll {
        function New-PartialResult {
            param($Ctx, [int]$Upserts, [int]$Confirmed, [string]$State = 'mapping-partial-stop', [switch]$Duplicate)
            $r = New-StopResult $Ctx $State -Batches 1 -AppliedStore
            $rows = @($Ctx.Packet.mappingsAfter | Where-Object { (ConvertTo-R1Canonical (Get-R1EdgeSet $_)) -cne (ConvertTo-R1Canonical (Get-R1EdgeSet ($Ctx.Packet.mappingsBefore | Where-Object frId -eq $_.frId))) })
            for ($i = 0; $i -lt $Upserts; $i++) { $m = if ($Duplicate -and $i -eq $Upserts - 1) { $rows[0] } else { $rows[$i] }; $r.Calls += [pscustomobject]@{ Method = 'UpsertMapping'; FrId = $m.frId; TrIds = @($m.trIds); TestIds = @($m.testIds) } }
            $r.Journal = @(New-Journal (@('records-confirmed-after') + @(1..$Confirmed | ForEach-Object { 'mapping-row-confirmed' }) + @($State)))
            $r
        }
    }
    It 'passes two confirmed rows then a stop at row 3' { $c = New-Ctx @{ Fault = 'mapping-fail'; FailAtRow = 3 }; { Assert-R1G15 (New-PartialResult $c 3 2) $c } | Should -Not -Throw }
    It 'rejects a retried completed row' { $c = New-Ctx @{ Fault = 'mapping-fail'; FailAtRow = 3 }; { Assert-R1G15 (New-PartialResult $c 3 2 -Duplicate) $c } | Should -Throw '*G15*' }
    It 'rejects an all-or-nothing claim' { $c = New-Ctx @{ Fault = 'mapping-fail'; FailAtRow = 3 }; { Assert-R1G15 (New-PartialResult $c 3 2 -State 'verified-data-awaiting-gates') $c } | Should -Throw '*G15*' }
    It 'rejects continuing past the failure' { $c = New-Ctx @{ Fault = 'mapping-fail'; FailAtRow = 3 }; { Assert-R1G15 (New-PartialResult $c 5 4) $c } | Should -Throw '*G15*' }
}

Describe 'G16 FinalWholeScopeReadback' -Tag MockGate {
    It 'passes a whole-scope match' { $c = New-Ctx; { Assert-R1G16 (New-HappyResult $c) $c } | Should -Not -Throw }
    It 'rejects verifying only one record' { $c = New-Ctx; $r = New-HappyResult $c; Set-StoreField $r 'fr|FR-PRF-003' 'description' 'not applied'; { Assert-R1G16 $r $c } | Should -Throw '*G16*' }
    It 'rejects a wrong final mapping' { $c = New-Ctx; $r = Copy-Result (New-HappyResult $c); $r.Store.Mappings.'FR-TAP-003'.testIds = @(); { Assert-R1G16 $r $c } | Should -Throw '*G16*' }
    It 'rejects completion without final readback' { $c = New-Ctx; $r = New-HappyResult $c; $r.Journal = @($r.Journal | Where-Object State -ne 'final-readback-verified'); { Assert-R1G16 $r $c } | Should -Throw '*G16*' }
}

Describe 'G17 PersistenceAndExportEvidence' -Tag MockGate {
    It 'passes retained export and labelled journal' { $c = New-Ctx; { Assert-R1G17 (New-HappyResult $c) $c } | Should -Not -Throw }
    It 'passes a surfaced export failure' { $c = New-Ctx @{ ExportExitCode = 1 }; $r = New-HappyResult $c; $r.Export = [pscustomobject]@{ ExitCode = 1; Output = 'failed' }; $r.State = 'verified-data-export-failed'; $r.ExportFailed = $true; { Assert-R1G17 $r $c } | Should -Not -Throw }
    It 'rejects missing export evidence' { $c = New-Ctx; $r = New-HappyResult $c; $r.Export = $null; { Assert-R1G17 $r $c } | Should -Throw '*G17*' }
    It 'rejects a hidden export failure' { $c = New-Ctx; $r = New-HappyResult $c; $r.Export = [pscustomobject]@{ ExitCode = 1; Output = 'failed' }; { Assert-R1G17 $r $c } | Should -Throw '*G17*' }
    It 'rejects an unlabelled journal entry' { $c = New-Ctx; $r = New-HappyResult $c; $r.Journal[0].Persistence = 'unknown'; { Assert-R1G17 $r $c } | Should -Throw '*G17*' }
}

Describe 'G18 NoUnapprovedRecoveryOrDone' -Tag MockGate {
    It 'passes a clean run' { $c = New-Ctx; { Assert-R1G18 (New-HappyResult $c) $c } | Should -Not -Throw }
    It 'rejects a delete call' { $c = New-Ctx; $r = New-HappyResult $c; $r.Calls += [pscustomobject]@{ Method = 'DeleteRecord'; Id = 'FR-TAP-001' }; { Assert-R1G18 $r $c } | Should -Throw '*G18*' }
    It 'rejects a queue replay' { $c = New-Ctx; $r = New-HappyResult $c; $r.Calls += [pscustomobject]@{ Method = 'ReplayQueue' }; { Assert-R1G18 $r $c } | Should -Throw '*G18*' }
    It 'rejects a done status' { $c = New-Ctx; $r = New-HappyResult $c; ($r.Calls | Where-Object Method -eq 'UpdateBatch').Records[0] | Add-Member -NotePropertyName status -NotePropertyValue 'completed'; { Assert-R1G18 $r $c } | Should -Throw '*G18*' }
    It 'rejects a satisfied criterion in a patch' { $c = New-Ctx; $r = Copy-Result (New-HappyResult $c); (($r.Calls | Where-Object Method -eq 'UpdateBatch').Records | Where-Object id -eq 'FR-CPU-004').acceptanceCriteria[0].isSatisfied = $true; { Assert-R1G18 $r $c } | Should -Throw '*G18*' }
    It 'rejects a recovery batch after a fault' { $c = New-Ctx @{ Fault = 'partial' }; { Assert-R1G18 (New-StopResult $c 'mixed-or-drift-stop' -Batches 2) $c } | Should -Throw '*G18*' }
}
