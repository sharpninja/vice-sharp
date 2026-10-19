#Requires -Version 7.4
<#
.SYNOPSIS
    Scripted requirements client for the R1 guard suite (R0-R1-GUARD-CONTRACT.md "Scripted mock/provider
    test design"). In-memory store of typed records and mapping rows, an ordered call log, and a fault
    script. Exposes the same scriptblock interface the real client binding exposes:
    GetRecord(Kind, Id), UpdateBatch(Records), GetMapping(FrId), UpsertMapping(FrId, TrIds, TestIds), Export().
#>
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'New-R1* build in-memory fixtures and a scripted fake client; no system state changes.')]
param()

Set-StrictMode -Version Latest

function Copy-R1Deep {
    param($Value)
    if ($null -eq $Value) { return $null }
    return ($Value | ConvertTo-Json -Depth 30 -Compress | ConvertFrom-Json)
}

function ConvertTo-R1TypedRecord {
    <# Packet before/after objects use body (FR/TR) or condition (TEST); typed reads use description. #>
    param([Parameter(Mandatory)]$PacketObject)
    $o = [ordered]@{}
    foreach ($p in $PacketObject.PSObject.Properties) {
        $name = if ($p.Name -in 'body', 'condition') { 'description' } else { $p.Name }
        if ($name -eq 'workspaceId') { continue }
        $o[$name] = $p.Value
    }
    return (Copy-R1Deep ([pscustomobject]$o))
}

function New-R1FixtureStore {
    <#
        Builds the initial fake store from the real packet: the 16 records at their packet before-state
        (typed field names), plus stable metadata the packet surface omits (area, notes, createdAt,
        updatedAt) so preservation is exercised, plus a minimal record for every mapping target that is
        not one of the 16, plus the nine mapping rows at their before-state.
    #>
    param([Parameter(Mandatory)]$Packet)
    $records = [ordered]@{}
    foreach ($r in $Packet.records) {
        $t = ConvertTo-R1TypedRecord $r.before
        $t | Add-Member -NotePropertyName area -NotePropertyValue ($r.id -split '-')[1] -Force
        $t | Add-Member -NotePropertyName notes -NotePropertyValue "fixture-notes-$($r.id)" -Force
        $t | Add-Member -NotePropertyName createdAt -NotePropertyValue '2026-01-01T00:00:00' -Force
        $t | Add-Member -NotePropertyName updatedAt -NotePropertyValue '2026-01-01T00:00:00' -Force
        $records["$($r.kind.ToLowerInvariant())|$($r.id)"] = $t
    }
    $targets = foreach ($m in @($Packet.mappingsBefore) + @($Packet.mappingsAfter)) {
        foreach ($id in @($m.trIds)) { "tr|$id" }
        foreach ($id in @($m.testIds)) { "test|$id" }
        "fr|$($m.frId)"
    }
    foreach ($key in ($targets | Sort-Object -Unique)) {
        if ($records.Contains($key)) { continue }
        $id = ($key -split '\|', 2)[1]
        $records[$key] = [pscustomobject]@{ id = $id; title = "fixture $id"; description = "fixture $id"; status = 'pending'; priority = 'medium'; area = 'FIX'; acceptanceCriteria = @(); createdAt = '2026-01-01T00:00:00'; updatedAt = '2026-01-01T00:00:00' }
    }
    $mappings = [ordered]@{}
    foreach ($m in $Packet.mappingsBefore) { $mappings[$m.frId] = [pscustomobject]@{ frId = $m.frId; trIds = @($m.trIds); testIds = @($m.testIds) } }
    return [pscustomobject]@{ Records = $records; Mappings = $mappings }
}

function New-R1FakeClient {
    <#
        Faults (hashtable, all optional):
          UpdateBatchMode   ok | reject | timeout-applied | timeout-unapplied | partial
          ReadUnavailableAfterWrite  [bool]  every GetRecord after the first UpdateBatch call throws
          MappingFailAtRow  [int]  the Nth UpsertMapping call (1-based) throws without applying
          MutateOnRead      @{ AtCall = n; Key = 'kind|id'; Field = 'name'; Value = v }  concurrent writer
          VolatileDrift     [bool]  every GetRecord returns a fresh createdAt/updatedAt
          ExportExitCode    [int]   Export result exit code (default 0)
          IdentityDefect    @{ Key = 'kind|id'; Mode = missing | duplicate | wrong-kind | case-only }
    #>
    param([Parameter(Mandatory)]$Store, [hashtable]$Faults = @{})
    $state = [pscustomobject]@{ Store = $Store; Faults = $Faults; Calls = [System.Collections.Generic.List[object]]::new(); ReadCount = 0; UpdateCalls = 0; UpsertCalls = 0 }

    $client = @{ State = $state }
    $client.GetRecord = {
        param([string]$Kind, [string]$Id)
        $s = $state
        $s.ReadCount++
        $s.Calls.Add([pscustomobject]@{ Method = 'GetRecord'; Kind = $Kind; Id = $Id })
        if ($s.Faults.ContainsKey('ReadUnavailableAfterWrite') -and $s.Faults.ReadUnavailableAfterWrite -and $s.UpdateCalls -gt 0) { throw 'fake: read unavailable' }
        if ($s.Faults.ContainsKey('MutateOnRead')) {
            $m = $s.Faults.MutateOnRead
            if ($s.ReadCount -eq $m.AtCall) { $s.Store.Records[$m.Key] | Add-Member -NotePropertyName $m.Field -NotePropertyValue $m.Value -Force }
        }
        $key = "$($Kind.ToLowerInvariant())|$Id"
        if ($s.Faults.ContainsKey('IdentityDefect') -and $s.Faults.IdentityDefect.Key -eq $key) {
            $rec = Copy-R1Deep $s.Store.Records[$key]
            switch ($s.Faults.IdentityDefect.Mode) {
                'missing' { return , @() }
                'duplicate' { return , @($rec, (Copy-R1Deep $rec)) }
                'wrong-kind' {
                    $other = if ($Kind -eq 'fr') { 'TR-' } else { 'FR-' }
                    $rec.id = $rec.id -replace '^(FR|TR|TEST)-', $other
                    return , @($rec)
                }
                'case-only' { $rec.id = $rec.id.ToLowerInvariant(); return , @($rec) }
            }
        }
        if (-not $s.Store.Records.Contains($key)) { return , @() }
        $rec = Copy-R1Deep $s.Store.Records[$key]
        if ($s.Faults.ContainsKey('VolatileDrift') -and $s.Faults.VolatileDrift) {
            $rec.createdAt = "2026-10-19T00:00:$('{0:00}' -f ($s.ReadCount % 60))"
            $rec.updatedAt = $rec.createdAt
        }
        return , @($rec)
    }.GetNewClosure()

    $client.UpdateBatch = {
        param([object[]]$Records)
        $s = $state
        $s.UpdateCalls++
        $s.Calls.Add([pscustomobject]@{ Method = 'UpdateBatch'; Records = (Copy-R1Deep $Records) })
        $mode = if ($s.Faults.ContainsKey('UpdateBatchMode')) { $s.Faults.UpdateBatchMode } else { 'ok' }
        $apply = {
            param($subset)
            foreach ($u in $subset) {
                $key = "$($u.kind)|$($u.id)"
                $target = $s.Store.Records[$key]
                foreach ($p in $u.PSObject.Properties) {
                    if ($p.Name -in 'kind', 'id') { continue }
                    $target | Add-Member -NotePropertyName $p.Name -NotePropertyValue (Copy-R1Deep $p.Value) -Force
                }
            }
        }
        switch ($mode) {
            'ok' { & $apply $Records; return [pscustomobject]@{ updated = @($Records).Count } }
            'reject' { throw 'fake: batch validation rejected' }
            'timeout-applied' { & $apply $Records; throw 'fake: timeout after commit' }
            'timeout-unapplied' { throw 'fake: timeout before commit' }
            'partial' { & $apply @($Records | Select-Object -First ([Math]::Max(1, [int](@($Records).Count / 2)))); throw 'fake: partial failure' }
        }
    }.GetNewClosure()

    $client.GetMapping = {
        param([string]$FrId)
        $s = $state
        $s.Calls.Add([pscustomobject]@{ Method = 'GetMapping'; FrId = $FrId })
        if ($s.Faults.ContainsKey('ReadUnavailableAfterWrite') -and $s.Faults.ReadUnavailableAfterWrite -and $s.UpdateCalls -gt 0) { throw 'fake: read unavailable' }
        if (-not $s.Store.Mappings.Contains($FrId)) { return $null }
        return (Copy-R1Deep $s.Store.Mappings[$FrId])
    }.GetNewClosure()

    $client.UpsertMapping = {
        param([string]$FrId, [string[]]$TrIds, [string[]]$TestIds)
        $s = $state
        $s.UpsertCalls++
        $s.Calls.Add([pscustomobject]@{ Method = 'UpsertMapping'; FrId = $FrId; TrIds = @($TrIds); TestIds = @($TestIds) })
        if ($s.Faults.ContainsKey('MappingFailAtRow') -and $s.UpsertCalls -eq $s.Faults.MappingFailAtRow) { throw 'fake: mapping upsert failed' }
        $s.Store.Mappings[$FrId] = [pscustomobject]@{ frId = $FrId; trIds = @($TrIds); testIds = @($TestIds) }
        return [pscustomobject]@{ frId = $FrId }
    }.GetNewClosure()

    $client.Export = {
        $s = $state
        $s.Calls.Add([pscustomobject]@{ Method = 'Export' })
        $code = if ($s.Faults.ContainsKey('ExportExitCode')) { $s.Faults.ExportExitCode } else { 0 }
        return [pscustomobject]@{ ExitCode = $code; Output = "fake export exit $code" }
    }.GetNewClosure()

    $client.Now = { Get-Date }
    return $client
}

Export-ModuleMember -Function Copy-R1Deep, ConvertTo-R1TypedRecord, New-R1FixtureStore, New-R1FakeClient
