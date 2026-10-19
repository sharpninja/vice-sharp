#Requires -Version 7.4
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.7.0' }
<#
    R1 guard suite, binding phase (BDPv4 step 5): the SAME scenarios and the SAME Assert-R1All from
    R1Assertions.psm1 (frozen in mockgate-freeze.json) bound to the real coordinator R1Coordinator.psm1,
    driven by the scripted fake client R1FakeClient.psm1 (also frozen). Expected values come from the
    packet via Get-R1ExpectedAfterStore, never from coordinator output. Each case also states the exact
    terminal journal state the contract requires.
#>
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'Test helpers build in-memory fixtures only; no system state changes.')]
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', '', Justification = 'WrapClient scriptblocks share the (client, store) signature; some use only one of them.')]
param()

BeforeAll {
    $here = $PSScriptRoot
    # -Global: the fake client's GetNewClosure() scriptblocks resolve Copy-R1Deep from the global scope.
    Import-Module (Join-Path $here 'R1FakeClient.psm1') -Force -Global
    Import-Module (Join-Path $here 'R1Assertions.psm1') -Force
    Import-Module (Join-Path $here 'R1Coordinator.psm1') -Force
    $script:PacketPath = Join-Path $here '..\20261013T231703-r0-anchor-remediation\proposed-field-delta.json'
    $script:PacketSha = (Get-FileHash -LiteralPath $script:PacketPath -Algorithm SHA256).Hash
    $script:Packet = Get-Content -LiteralPath $script:PacketPath -Raw | ConvertFrom-Json

    function New-Approval {
        param([string]$Sha = $script:PacketSha, [int]$Records = 16, [int]$Rows = 9, [bool]$Departure = $true, [datetime]$Start = (Get-Date).AddMinutes(-5), [datetime]$Expiry = (Get-Date).AddHours(1))
        [pscustomobject]@{ PacketSha256 = $Sha; RecordCount = $Records; MappingRowCount = $Rows; LockedDecision2DepartureAccepted = $Departure; WriterWindow = [pscustomobject]@{ StartLocal = $Start.ToString('s'); ExpiryLocal = $Expiry.ToString('s') } }
    }

    function Invoke-Bound {
        param(
            [hashtable]$Scenario = @{},
            [hashtable]$Faults = @{},
            [scriptblock]$Mutate = {},
            $Approval = (New-Approval),
            [object[]]$Attributions = @(),
            [string]$PacketPath = $script:PacketPath,
            [scriptblock]$WrapClient = {},
            [string]$ExpectState
        )
        $store = New-R1FixtureStore $script:Packet
        & $Mutate $store
        $initial = New-R1FixtureStore $script:Packet
        & $Mutate $initial
        $client = New-R1FakeClient -Store $store -Faults $Faults
        & $WrapClient $client $store
        $journalPath = Join-Path $TestDrive ("journal-{0}.jsonl" -f [guid]::NewGuid().ToString('N'))
        $res = Invoke-R1Coordinator -PacketPath $PacketPath -Approval $Approval -Client $client -Attributions $Attributions -JournalPath $journalPath
        $res | Add-Member -NotePropertyName Store -NotePropertyValue $store -Force
        $ctx = [pscustomobject]@{ Packet = $script:Packet; InitialStore = $initial; Scenario = $Scenario; Attributions = $Attributions }
        $res.State | Should -Be $ExpectState -Because (@($res.Journal)[-1].Detail)
        Assert-R1All -Result $res -Context $ctx
        return $res
    }

    function New-D8Attribution {
        $j = Join-Path $TestDrive 'd8-journal.jsonl'
        $entry = [ordered]@{ phase = 'after'; kind = 'fr'; id = 'FR-BACKFILL-MEDIA-001'; record = [ordered]@{ id = 'FR-BACKFILL-MEDIA-001'; status = 'pending' } }
        ($entry | ConvertTo-Json -Depth 5 -Compress) | Set-Content -LiteralPath $j -Encoding utf8NoBOM
        [pscustomobject]@{ Kind = 'fr'; Id = 'FR-BACKFILL-MEDIA-001'; Field = 'status'; JournalPath = $j; JournalSha256 = (Get-FileHash -LiteralPath $j -Algorithm SHA256).Hash }
    }

    $script:SetMediaPending = { param($s) $s.Records['fr|FR-BACKFILL-MEDIA-001'].status = 'pending' }
}

Describe 'Binding: happy path' -Tag Binding {
    It 'applies 16 records and 6 changed rows, skips 3 unchanged rows, verifies, exports' {
        $r = Invoke-Bound -ExpectState 'verified-data-awaiting-gates'
        @($r.Calls | Where-Object Method -eq 'UpdateBatch').Count | Should -Be 1
        @($r.Calls | Where-Object Method -eq 'UpsertMapping').Count | Should -Be 6
        @($r.Journal | Where-Object State -eq 'mapping-row-unchanged').Count | Should -Be 3
    }
    It 'survives read-synthesized timestamp drift (G05)' { $null = Invoke-Bound -Scenario @{ VolatileDrift = $true } -Faults @{ VolatileDrift = $true } -ExpectState 'verified-data-awaiting-gates' }
    It 'surfaces a failed export (G17)' { $r = Invoke-Bound -Scenario @{ ExportExitCode = 1 } -Faults @{ ExportExitCode = 1 } -ExpectState 'verified-data-export-failed'; $r.ExportFailed | Should -BeTrue }
}

Describe 'Binding: approval (G01)' -Tag Binding {
    It 'blocks with no approval' { $null = Invoke-Bound -Scenario @{ ApprovalDefect = 'absent' } -Approval $null -ExpectState 'blocked-before-write' }
    It 'blocks a stale hash' { $null = Invoke-Bound -Scenario @{ ApprovalDefect = 'stale-hash' } -Approval (New-Approval -Sha ('0' * 64)) -ExpectState 'blocked-before-write' }
    It 'blocks a packet with a changed nested criterion' {
        $p = Get-Content -LiteralPath $script:PacketPath -Raw | ConvertFrom-Json
        $p.records[0].after.acceptanceCriteria[0].text = 'tampered'
        $path = Join-Path $TestDrive 'tampered.json'; ($p | ConvertTo-Json -Depth 40) | Set-Content -LiteralPath $path -Encoding utf8NoBOM
        $null = Invoke-Bound -Scenario @{ ApprovalDefect = 'changed-nested-ac' } -PacketPath $path -ExpectState 'blocked-before-write'
    }
    It 'blocks an approval for a prior scope' { $null = Invoke-Bound -Scenario @{ ApprovalDefect = 'prior-scope' } -Approval (New-Approval -Records 14 -Rows 7) -ExpectState 'blocked-before-write' }
    It 'blocks without the locked decision 2 departure' { $null = Invoke-Bound -Scenario @{ ApprovalDefect = 'no-departure' } -Approval (New-Approval -Departure $false) -ExpectState 'blocked-before-write' }
    It 'blocks outside the writer window' { $null = Invoke-Bound -Scenario @{ ApprovalDefect = 'window' } -Approval (New-Approval -Start (Get-Date).AddHours(-2) -Expiry (Get-Date).AddHours(-1)) -ExpectState 'blocked-before-write' }
}

Describe 'Binding: identity, presence, targets, drift (G02, G03, G08, G09)' -Tag Binding {
    It 'blocks identity defect <_>' -ForEach 'missing', 'duplicate', 'wrong-kind', 'case-only' {
        $null = Invoke-Bound -Scenario @{ IdentityDefect = $_ } -Faults @{ IdentityDefect = @{ Key = 'fr|FR-CPU-004'; Mode = $_ } } -ExpectState 'blocked-before-write'
    }
    It 'blocks presence/type defect <Name>' -ForEach @(
        @{ Name = 'present-null'; Mutate = { param($s) $s.Records['fr|FR-TAP-001'].description = $null } }
        @{ Name = 'absent'; Mutate = { param($s) $s.Records['fr|FR-TAP-001'].PSObject.Properties.Remove('description') } }
        @{ Name = 'empty-array'; Mutate = { param($s) $s.Records['fr|FR-TAP-001'].acceptanceCriteria = @() } }
        @{ Name = 'string-for-number'; Mutate = { param($s) $s.Records['tr|TR-TAP-EDGE-001'].title = 5 } }
        @{ Name = 'bool-for-string'; Mutate = { param($s) $s.Records['tr|TR-TAP-EDGE-001'].title = $true } }
    ) { $null = Invoke-Bound -Scenario @{ PresenceDefect = $Name } -Mutate $Mutate -ExpectState 'blocked-before-write' }
    It 'blocks a missing mapping target' { $null = Invoke-Bound -Scenario @{ MissingTarget = 'TEST-IEC-TIMING-001' } -Mutate { param($s) $s.Records.Remove('test|TEST-IEC-TIMING-001') } -ExpectState 'blocked-before-write' }
    It 'blocks baseline drift' { $null = Invoke-Bound -Scenario @{ Drift = 'FR-CPU-004 description' } -Mutate { param($s) $s.Records['fr|FR-CPU-004'].description = 'concurrently edited' } -ExpectState 'blocked-before-write' }
    It 'blocks mapping drift' { $null = Invoke-Bound -Scenario @{ Drift = 'FR-TAP-001 mapping' } -Mutate { param($s) $s.Mappings['FR-TAP-001'].trIds = @($s.Mappings['FR-TAP-001'].trIds) + 'TR-OTHER-WRITER-001' } -ExpectState 'blocked-before-write' }
}

Describe 'Binding: batch outcomes (G10-G14)' -Tag Binding {
    It 'stops unchanged on a rejected batch' { $null = Invoke-Bound -Scenario @{ Fault = 'reject' } -Faults @{ UpdateBatchMode = 'reject' } -ExpectState 'records-confirmed-before' }
    It 'records a lost acknowledgement that applied, never retries, and completes' { $null = Invoke-Bound -Scenario @{ Fault = 'timeout-applied' } -Faults @{ UpdateBatchMode = 'timeout-applied' } -ExpectState 'verified-data-awaiting-gates' }
    It 'stops on a lost acknowledgement that did not apply' { $null = Invoke-Bound -Scenario @{ Fault = 'timeout-unapplied' } -Faults @{ UpdateBatchMode = 'timeout-unapplied' } -ExpectState 'records-confirmed-before' }
    It 'stops as unknown when readback is unavailable' { $null = Invoke-Bound -Scenario @{ Fault = 'read-unavailable' } -Faults @{ UpdateBatchMode = 'timeout-applied'; ReadUnavailableAfterWrite = $true } -ExpectState 'submitted-outcome-unknown' }
    It 'stops mixed on a partial batch' { $null = Invoke-Bound -Scenario @{ Fault = 'partial' } -Faults @{ UpdateBatchMode = 'partial' } -ExpectState 'mixed-or-drift-stop' }
    It 'stops mixed on a concurrent third state' {
        $wrap = { param($c, $s) $orig = $c.UpdateBatch; $c.UpdateBatch = { param($Records) $out = & $orig $Records; $s.Records['fr|FR-TAP-002'].description = 'other writer third state'; $out }.GetNewClosure() }
        $null = Invoke-Bound -Scenario @{ Fault = 'concurrent' } -WrapClient $wrap -ExpectState 'mixed-or-drift-stop'
    }
}

Describe 'Binding: mapping sequence (G15)' -Tag Binding {
    It 'stops at the failing third changed row with two rows confirmed' { $null = Invoke-Bound -Scenario @{ Fault = 'mapping-fail'; FailAtRow = 3 } -Faults @{ MappingFailAtRow = 3 } -ExpectState 'mapping-partial-stop' }
    It 'skips a row already at its approved union' {
        $r = Invoke-Bound -Mutate { param($s) $a = $script:Packet.mappingsAfter | Where-Object frId -eq 'FR-TAP-001'; $s.Mappings['FR-TAP-001'] = [pscustomobject]@{ frId = 'FR-TAP-001'; trIds = @($a.trIds); testIds = @($a.testIds) } } -ExpectState 'verified-data-awaiting-gates'
        @($r.Calls | Where-Object { $_.Method -eq 'UpsertMapping' -and $_.FrId -eq 'FR-TAP-001' }).Count | Should -Be 0
    }
    It 'stops before a mapping write when the writer window closes mid-run' {
        $wrap = { param($c, $s) $n = [ref]0; $c.Now = { $n.Value++; if ($n.Value -le 2) { Get-Date } else { (Get-Date).AddDays(1) } }.GetNewClosure() }
        $r = Invoke-Bound -WrapClient $wrap -ExpectState 'mapping-partial-stop'
        @($r.Calls | Where-Object Method -eq 'UpsertMapping').Count | Should -Be 0
    }
}

Describe 'Binding: attributed prior write (D8 status on FR-BACKFILL-MEDIA-001)' -Tag Binding {
    It 'accepts a verified attribution and omits status from the patch' {
        $att = New-D8Attribution
        $r = Invoke-Bound -Mutate $script:SetMediaPending -Attributions @($att) -ExpectState 'verified-data-awaiting-gates'
        $patch = (@($r.Calls | Where-Object Method -eq 'UpdateBatch')[0].Records | Where-Object id -eq 'FR-BACKFILL-MEDIA-001')
        $patch.PSObject.Properties.Name | Should -Not -Contain 'status'
    }
    It 'blocks the same state without an attribution' { $null = Invoke-Bound -Scenario @{ Drift = 'status without attribution' } -Mutate $script:SetMediaPending -ExpectState 'blocked-before-write' }
    It 'blocks an attribution whose journal hash does not match' {
        $att = New-D8Attribution; $att.JournalSha256 = 'F' * 64
        $null = Invoke-Bound -Scenario @{ Drift = 'tampered attribution' } -Mutate $script:SetMediaPending -Attributions @($att) -ExpectState 'blocked-before-write'
    }
}
