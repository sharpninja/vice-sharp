#Requires -Version 7.0
<#
PLAN-NATIVERESTORE-001 D2 A05 controls on mock TRX files: the happy path passes; an equal-sized swapped failure
set, a missing project, discovery drift against the retained identifier list, a partial or aborted run, a skipped
case, a ShimLegacy leak, a real-bound class, a missing contract class, and a missing or non-empty load log each fail.
#>
BeforeAll {
    $script:Script = [IO.Path]::Combine($PSScriptRoot, '..', 'Test-OrdinaryGate.ps1')

    function Write-MockTrx {
        param([string] $Directory, [string] $Assembly, [object[]] $Cases, [string] $RunOutcome = 'Completed', [int] $ExecutedOverride = -1)
        $defs = @(); $results = @(); $i = 0
        foreach ($c in $Cases) {
            $i++
            $id = ('{0:D8}-0000-0000-0000-000000000000' -f $i)
            $defs += ('<UnitTest name="{0}" id="{1}"><Execution id="{1}" /><TestMethod codeBase="/x/{2}.dll" className="{3}" name="{0}" /></UnitTest>' -f $c.method, $id, $Assembly, $c.className)
            $results += ('<UnitTestResult executionId="{0}" testId="{0}" testName="{1}" outcome="{2}" />' -f $id, $c.method, $c.outcome)
        }
        $total = $Cases.Count
        $passed = @($Cases | Where-Object { $_.outcome -eq 'Passed' }).Count
        $failed = @($Cases | Where-Object { $_.outcome -eq 'Failed' }).Count
        $notExecuted = @($Cases | Where-Object { $_.outcome -eq 'NotExecuted' }).Count
        $executed = if ($ExecutedOverride -ge 0) { $ExecutedOverride } else { $total - $notExecuted }
        $xml = '<?xml version="1.0" encoding="utf-8"?><TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results>' + ($results -join '') + '</Results><TestDefinitions>' + ($defs -join '') + '</TestDefinitions>' +
            ('<ResultSummary outcome="{0}"><Counters total="{1}" executed="{2}" passed="{3}" failed="{4}" notExecuted="{5}" /></ResultSummary></TestRun>' -f $RunOutcome, $total, $executed, $passed, $failed, $notExecuted)
        [IO.File]::WriteAllText([IO.Path]::Combine($Directory, $Assembly + '.trx'), $xml)
    }

    function Get-MockCase { param([string] $ClassName, [string] $Method, [string] $Outcome = 'Passed') [pscustomobject]@{ className = $ClassName; method = $Method; outcome = $Outcome } }

    function Initialize-HappyRun {
        param([string] $Root)
        $results = [IO.Path]::Combine($Root, 'results'); New-Item -ItemType Directory -Path $results -Force | Out-Null
        Write-MockTrx -Directory $results -Assembly 'ViceSharp.TestHarness' -Cases @(
            (Get-MockCase -ClassName 'ViceSharp.TestHarness.CpuInterruptTests' -Method 'Red_One' -Outcome 'Failed'),
            (Get-MockCase -ClassName 'ViceSharp.TestHarness.CpuInterruptTests' -Method 'Red_Two' -Outcome 'Failed'),
            (Get-MockCase -ClassName 'ViceSharp.TestHarness.GreenTests' -Method 'Green_One'),
            (Get-MockCase -ClassName 'ViceSharp.TestHarness.Vic20.Vic20HalfCycleComparatorTests' -Method 'RegistersMatch_BusAddressDiffers_ReportsExactHalfCycle'))
        Write-MockTrx -Directory $results -Assembly 'ViceSharp.VsfCompatibility.Tests' -Cases @(
            (Get-MockCase -ClassName 'ViceSharp.VsfCompatibility.Tests.VsfSlice1PrerequisiteContractTests' -Method 'Contract_One'))
        [IO.File]::WriteAllLines([IO.Path]::Combine($Root, 'known-red.txt'), @('# two', 'ViceSharp.TestHarness.CpuInterruptTests.Red_One', 'ViceSharp.TestHarness.CpuInterruptTests.Red_Two'))
        [IO.File]::WriteAllText([IO.Path]::Combine($Root, 'native-load.log'), '')
        $inventory = [pscustomobject]@{ consumers = @([pscustomobject]@{ class = 'LockstepValidationTests'; method = 'Shim_One' }) }
        $inventory | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath ([IO.Path]::Combine($Root, 'inventory.json')) -Encoding utf8
    }

    function Invoke-Gate {
        param([string] $Root, [hashtable] $Extra = @{})
        $params = @{
            ResultsDirectory = [IO.Path]::Combine($Root, 'results')
            ExpectedProjects = 'ViceSharp.TestHarness,ViceSharp.VsfCompatibility.Tests'
            KnownRedPath = [IO.Path]::Combine($Root, 'known-red.txt')
            LoadLogPath = [IO.Path]::Combine($Root, 'native-load.log')
            InventoryPath = [IO.Path]::Combine($Root, 'inventory.json')
            ContractClasses = 'VsfSlice1PrerequisiteContractTests'
            RealClasses = 'NativeViceSnapshotPrerequisiteTests'
            ReportPath = [IO.Path]::Combine($Root, 'report.json')
        }
        foreach ($k in $Extra.Keys) { $params[$k] = $Extra[$k] }
        & $script:Script @params
    }
}

Describe 'Test-OrdinaryGate' {
    BeforeEach {
        $script:Root = [IO.Path]::Combine([IO.Path]::GetTempPath(), ('gate-' + [guid]::NewGuid().ToString('N')))
        New-Item -ItemType Directory -Path $script:Root | Out-Null
        Initialize-HappyRun -Root $script:Root
    }
    AfterEach { Remove-Item -LiteralPath $script:Root -Recurse -Force -ErrorAction SilentlyContinue }

    It 'passes the happy path and writes the identifier list' {
        $ids = [IO.Path]::Combine($script:Root, 'identifiers.txt')
        $out = Invoke-Gate -Root $script:Root -Extra @{ WriteIdentifiersPath = $ids }
        ($out -join "`n") | Should -Match 'gate Pass'
        @(Get-Content -LiteralPath $ids).Count | Should -Be 5
        $r = Get-Content -LiteralPath ([IO.Path]::Combine($script:Root, 'report.json')) -Raw | ConvertFrom-Json
        $r.outcome | Should -Be 'Pass'
        $r.totals.failed | Should -Be 2
        $r.contractCounts.VsfSlice1PrerequisiteContractTests | Should -Be 1
    }

    It 'tolerates and counts a TRX whose every case the filter excluded' {
        $empty = '<?xml version="1.0" encoding="utf-8"?><TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results /><TestDefinitions /><ResultSummary outcome="Completed"><Counters total="0" executed="0" passed="0" failed="0" /></ResultSummary></TestRun>'
        [IO.File]::WriteAllText([IO.Path]::Combine($script:Root, 'results', '_HOST_2026-10-20_22_48_03_net10.0.trx'), $empty)
        $out = Invoke-Gate -Root $script:Root
        ($out -join "`n") | Should -Match 'gate Pass'
        ($out -join "`n") | Should -Match 'empty runs 1'
        $r = Get-Content -LiteralPath ([IO.Path]::Combine($script:Root, 'report.json')) -Raw | ConvertFrom-Json
        @($r.emptyRuns) | Should -Contain '_HOST_2026-10-20_22_48_03_net10.0.trx'
    }

    It 'rejects an equal-sized swapped failure set' {
        [IO.File]::WriteAllLines([IO.Path]::Combine($script:Root, 'known-red.txt'), @('ViceSharp.TestHarness.CpuInterruptTests.Red_One', 'ViceSharp.TestHarness.CpuInterruptTests.Red_Three'))
        { Invoke-Gate -Root $script:Root -ErrorAction Stop | Out-Null } | Should -Throw
        $r = Get-Content -LiteralPath ([IO.Path]::Combine($script:Root, 'report.json')) -Raw | ConvertFrom-Json
        @($r.unexpectedFailures) | Should -Contain 'ViceSharp.TestHarness.CpuInterruptTests.Red_Two'
        @($r.missingKnownRed) | Should -Contain 'ViceSharp.TestHarness.CpuInterruptTests.Red_Three'
    }

    It 'rejects a missing project' {
        Remove-Item -LiteralPath ([IO.Path]::Combine($script:Root, 'results', 'ViceSharp.VsfCompatibility.Tests.trx')) -Force
        { Invoke-Gate -Root $script:Root -ErrorAction Stop | Out-Null } | Should -Throw
        $r = Get-Content -LiteralPath ([IO.Path]::Combine($script:Root, 'report.json')) -Raw | ConvertFrom-Json
        $expected = 'project ViceSharp.VsfCompatibility.Tests has no TRX in ' + [IO.Path]::Combine($script:Root, 'results')
        @($r.violations) | Should -Contain $expected
    }

    It 'rejects discovery drift against the retained identifier list' {
        $ids = [IO.Path]::Combine($script:Root, 'identifiers.txt')
        Invoke-Gate -Root $script:Root -Extra @{ WriteIdentifiersPath = $ids } | Out-Null
        $lines = @(Get-Content -LiteralPath $ids); [IO.File]::WriteAllLines($ids, @($lines | Select-Object -Skip 1))
        { Invoke-Gate -Root $script:Root -Extra @{ RetainedIdentifiersPath = $ids } -ErrorAction Stop | Out-Null } | Should -Throw
    }

    It 'rejects a partial or aborted run and a skipped case' {
        $results = [IO.Path]::Combine($script:Root, 'results')
        $contract = Get-MockCase -ClassName 'ViceSharp.VsfCompatibility.Tests.VsfSlice1PrerequisiteContractTests' -Method 'Contract_One'
        Write-MockTrx -Directory $results -Assembly 'ViceSharp.VsfCompatibility.Tests' -RunOutcome 'Aborted' -Cases @($contract)
        { Invoke-Gate -Root $script:Root -ErrorAction Stop | Out-Null } | Should -Throw
        Write-MockTrx -Directory $results -Assembly 'ViceSharp.VsfCompatibility.Tests' -ExecutedOverride 0 -Cases @($contract)
        { Invoke-Gate -Root $script:Root -ErrorAction Stop | Out-Null } | Should -Throw
        $skipped = Get-MockCase -ClassName 'ViceSharp.VsfCompatibility.Tests.Other' -Method 'Skipped_One' -Outcome 'NotExecuted'
        Write-MockTrx -Directory $results -Assembly 'ViceSharp.VsfCompatibility.Tests' -Cases @($contract, $skipped)
        { Invoke-Gate -Root $script:Root -ErrorAction Stop | Out-Null } | Should -Throw
    }

    It 'rejects a ShimLegacy leak, a real-bound class and a missing contract class' {
        $results = [IO.Path]::Combine($script:Root, 'results')
        $contract = Get-MockCase -ClassName 'ViceSharp.VsfCompatibility.Tests.VsfSlice1PrerequisiteContractTests' -Method 'Contract_One'
        $leak = Get-MockCase -ClassName 'ViceSharp.TestHarness.LockstepValidationTests' -Method 'Shim_One'
        Write-MockTrx -Directory $results -Assembly 'ViceSharp.VsfCompatibility.Tests' -Cases @($contract, $leak)
        { Invoke-Gate -Root $script:Root -ErrorAction Stop | Out-Null } | Should -Throw -ExpectedMessage '*gate evaluation failed*'
        $r = Get-Content -LiteralPath ([IO.Path]::Combine($script:Root, 'report.json')) -Raw | ConvertFrom-Json
        @($r.shimLegacyLeaks) | Should -Contain 'ViceSharp.TestHarness.LockstepValidationTests.Shim_One'
        $real = Get-MockCase -ClassName 'ViceSharp.VsfCompatibility.Tests.NativeViceSnapshotPrerequisiteTests' -Method 'Real_One'
        Write-MockTrx -Directory $results -Assembly 'ViceSharp.VsfCompatibility.Tests' -Cases @($contract, $real)
        { Invoke-Gate -Root $script:Root -ErrorAction Stop | Out-Null } | Should -Throw
        $other = Get-MockCase -ClassName 'ViceSharp.VsfCompatibility.Tests.Other' -Method 'Other_One'
        Write-MockTrx -Directory $results -Assembly 'ViceSharp.VsfCompatibility.Tests' -Cases @($other)
        { Invoke-Gate -Root $script:Root -ErrorAction Stop | Out-Null } | Should -Throw
    }

    It 'rejects a missing or non-empty load log' {
        [IO.File]::WriteAllText([IO.Path]::Combine($script:Root, 'native-load.log'), "vice_x64,none,unresolved`n")
        { Invoke-Gate -Root $script:Root -ErrorAction Stop | Out-Null } | Should -Throw
        Remove-Item -LiteralPath ([IO.Path]::Combine($script:Root, 'native-load.log')) -Force
        { Invoke-Gate -Root $script:Root -ErrorAction Stop | Out-Null } | Should -Throw
    }
}
