#Requires -Version 7.0
<#
PLAN-NATIVERESTORE-001 D2 (R6, N06 controls at the tagging step): Add-ShimLegacyTraits.ps1 tags exactly the
inventory's methods, leaves a managed fake, a string-only mention and an uninventoried plain test untouched, is
idempotent, preserves line endings and encoding, uses the qualified attribute when Xunit is not imported, inserts
before multi-line attribute blocks, reports a missing method as a failure and writes nothing under -WhatIf.
#>
BeforeAll {
    $script:Script = [IO.Path]::Combine($PSScriptRoot, '..', 'Add-ShimLegacyTraits.ps1')

    function Get-MockPath {
        param([string] $Root, [string[]] $Parts)
        [string[]] $all = @($Root) + $Parts
        return [IO.Path]::Combine($all)
    }

    function Initialize-MockTree {
        param([string] $Root)
        New-Item -ItemType Directory -Path (Get-MockPath -Root $Root -Parts 'Proj') -Force | Out-Null
        $direct = @(
            'using Xunit;',
            'using ViceSharp.Core;',
            '',
            'namespace Proj;',
            '',
            'public sealed class DirectTests',
            '{',
            '    /// <summary>Modeled on SnapshotResumeSpikeTests: plain Fact, direct use.</summary>',
            '    [Fact]',
            '    public void Direct_UsesNative()',
            '    {',
            '        using var native = ViceNative.CreateInstance();',
            '    }',
            '',
            '    [Fact]',
            '    public void StringOnly_MentionsViceNative()',
            '    {',
            '        Assert.Contains("ViceNative", "ViceNative is a string here");',
            '    }',
            '',
            '    [Theory]',
            '    [InlineData(',
            '        1,',
            '        2)]',
            '    public void Transitive_ViaHelper(int a, int b)',
            '    {',
            '        Helper.Touch();',
            '    }',
            '}'
        )
        [IO.File]::WriteAllText((Get-MockPath -Root $Root -Parts 'Proj', 'DirectTests.cs'), ($direct -join "`r`n"), [Text.UTF8Encoding]::new($true))
        $fake = @(
            'using ViceSharp.Abstractions;',
            '',
            'namespace Proj;',
            '',
            'public sealed class FakeTests',
            '{',
            '    [Xunit.Fact]',
            '    public void ManagedFake_IsNotAConsumer()',
            '    {',
            '        var fake = new FakeNative();',
            '        fake.Step();',
            '    }',
            '',
            '    [Xunit.Fact]',
            '    public void NoUsingXunit_DirectConsumer()',
            '    {',
            '        ViceSharp.Core.ViceNative.CreateInstance();',
            '    }',
            '}'
        )
        [IO.File]::WriteAllText((Get-MockPath -Root $Root -Parts 'Proj', 'FakeTests.cs'), ($fake -join "`n"), [Text.UTF8Encoding]::new($false))
        $inventory = [pscustomobject]@{
            consumers = @(
                [pscustomobject]@{ file = 'Proj/DirectTests.cs'; project = 'Proj'; class = 'DirectTests'; method = 'Direct_UsesNative'; attribute = 'Fact'; reason = 'direct'; via = $null },
                [pscustomobject]@{ file = 'Proj/DirectTests.cs'; project = 'Proj'; class = 'DirectTests'; method = 'Transitive_ViaHelper'; attribute = 'Theory'; reason = 'transitive'; via = 'Helper' },
                [pscustomobject]@{ file = 'Proj/FakeTests.cs'; project = 'Proj'; class = 'FakeTests'; method = 'NoUsingXunit_DirectConsumer'; attribute = 'Fact'; reason = 'direct'; via = $null }
            )
        }
        $inventoryPath = Get-MockPath -Root $Root -Parts 'inventory.json'
        $inventory | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $inventoryPath -Encoding utf8
        return $inventoryPath
    }

    function Get-FileBytesHex {
        param([string] $Path)
        return [Convert]::ToHexString([IO.File]::ReadAllBytes($Path))
    }
}

Describe 'Add-ShimLegacyTraits' {
    BeforeEach {
        $script:Root = [IO.Path]::Combine([IO.Path]::GetTempPath(), ('shimlegacy-' + [guid]::NewGuid().ToString('N')))
        New-Item -ItemType Directory -Path $script:Root | Out-Null
        $script:Inventory = Initialize-MockTree -Root $script:Root
        $script:DirectPath = Get-MockPath -Root $script:Root -Parts 'Proj', 'DirectTests.cs'
        $script:FakePath = Get-MockPath -Root $script:Root -Parts 'Proj', 'FakeTests.cs'
    }
    AfterEach { Remove-Item -LiteralPath $script:Root -Recurse -Force -ErrorAction SilentlyContinue }

    It 'tags exactly the inventory methods, last in the attribute block, and leaves the fake, the string-only mention and the plain test alone' {
        $report = & $script:Script -InventoryPath $script:Inventory -TestRoot $script:Root
        $report.counts.tagged | Should -Be 3
        $report.counts.relocated | Should -Be 0
        $report.counts.notFound | Should -Be 0
        $direct = @(Get-Content -LiteralPath $script:DirectPath)
        @($direct | Where-Object { $_ -like '*Trait("Category", "ShimLegacy")*' }).Count | Should -Be 2
        @($direct | Where-Object { $_ -like '*Xunit.Trait*' }).Count | Should -Be 0
        $idxDecl = [array]::IndexOf($direct, '    public void Direct_UsesNative()')
        $direct[$idxDecl - 1] | Should -Be '    [Trait("Category", "ShimLegacy")]'
        $direct[$idxDecl - 2] | Should -Be '    [Fact]'
        $direct[$idxDecl - 3] | Should -BeLike '    /// <summary>*'
        $idxString = [array]::IndexOf($direct, '    public void StringOnly_MentionsViceNative()')
        $direct[$idxString - 1] | Should -Be '    [Fact]'
        $direct[$idxString - 2] | Should -Be ''
        $fake = @(Get-Content -LiteralPath $script:FakePath)
        @($fake | Where-Object { $_ -like '*ShimLegacy*' }).Count | Should -Be 1
        $idxFakeTest = [array]::IndexOf($fake, '    public void ManagedFake_IsNotAConsumer()')
        $fake[$idxFakeTest - 1] | Should -Be '    [Xunit.Fact]'
        $fake[$idxFakeTest - 2] | Should -Be '{'
    }

    It 'inserts after a multi-line attribute block and uses the qualified form without using Xunit' {
        & $script:Script -InventoryPath $script:Inventory -TestRoot $script:Root | Out-Null
        $direct = @(Get-Content -LiteralPath $script:DirectPath)
        $idx = [array]::IndexOf($direct, '    public void Transitive_ViaHelper(int a, int b)')
        $direct[$idx - 1] | Should -Be '    [Trait("Category", "ShimLegacy")]'
        $direct[$idx - 2] | Should -Be '        2)]'
        $direct[$idx - 5] | Should -Be '    [Theory]'
        $fake = @(Get-Content -LiteralPath $script:FakePath)
        $idx2 = [array]::IndexOf($fake, '    public void NoUsingXunit_DirectConsumer()')
        $fake[$idx2 - 1] | Should -Be '    [Xunit.Trait("Category", "ShimLegacy")]'
        $fake[$idx2 - 2] | Should -Be '    [Xunit.Fact]'
    }

    It 'relocates a trait found earlier in the block to the end, once' {
        $lines = [Collections.Generic.List[string]]::new([string[]](Get-Content -LiteralPath $script:DirectPath))
        $idxFact = [array]::IndexOf($lines.ToArray(), '    [Fact]')
        $lines.Insert($idxFact, '    [Trait("Category", "ShimLegacy")]')
        [IO.File]::WriteAllText($script:DirectPath, ($lines.ToArray() -join "`r`n"), [Text.UTF8Encoding]::new($true))
        $report = & $script:Script -InventoryPath $script:Inventory -TestRoot $script:Root
        $report.counts.relocated | Should -Be 1
        $report.counts.tagged | Should -Be 2
        $direct = @(Get-Content -LiteralPath $script:DirectPath)
        @($direct | Where-Object { $_ -like '*ShimLegacy*' }).Count | Should -Be 2
        $idxDecl = [array]::IndexOf($direct, '    public void Direct_UsesNative()')
        $direct[$idxDecl - 1] | Should -Be '    [Trait("Category", "ShimLegacy")]'
        $direct[$idxDecl - 2] | Should -Be '    [Fact]'
        $direct[$idxDecl - 3] | Should -BeLike '    /// <summary>*'
        $second = & $script:Script -InventoryPath $script:Inventory -TestRoot $script:Root
        $second.counts.relocated | Should -Be 0
        $second.counts.alreadyTagged | Should -Be 3
    }

    It 'preserves CRLF, LF and byte order marks' {
        & $script:Script -InventoryPath $script:Inventory -TestRoot $script:Root | Out-Null
        $directBytes = [IO.File]::ReadAllBytes($script:DirectPath)
        ($directBytes[0] -eq 0xEF -and $directBytes[1] -eq 0xBB) | Should -BeTrue
        $directText = [Text.Encoding]::UTF8.GetString($directBytes, 3, $directBytes.Length - 3)
        $directText.Contains("`r`n") | Should -BeTrue
        ($directText -replace "`r`n", '').Contains("`n") | Should -BeFalse
        $fakeBytes = [IO.File]::ReadAllBytes($script:FakePath)
        ($fakeBytes[0] -eq 0xEF) | Should -BeFalse
        [Text.Encoding]::UTF8.GetString($fakeBytes).Contains("`r") | Should -BeFalse
    }

    It 'is idempotent: a second run tags nothing, reports alreadyTagged and leaves the bytes unchanged' {
        & $script:Script -InventoryPath $script:Inventory -TestRoot $script:Root | Out-Null
        $before = Get-FileBytesHex -Path $script:DirectPath
        $report = & $script:Script -InventoryPath $script:Inventory -TestRoot $script:Root
        $report.counts.tagged | Should -Be 0
        $report.counts.alreadyTagged | Should -Be 3
        $report.counts.filesWritten | Should -Be 0
        Get-FileBytesHex -Path $script:DirectPath | Should -Be $before
    }

    It 'fails and leaves the file untouched when an inventory method is missing' {
        $inv = Get-Content -LiteralPath $script:Inventory -Raw | ConvertFrom-Json
        $inv.consumers += [pscustomobject]@{ file = 'Proj/DirectTests.cs'; project = 'Proj'; class = 'DirectTests'; method = 'Does_Not_Exist'; attribute = 'Fact'; reason = 'direct'; via = $null }
        $inv | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $script:Inventory -Encoding utf8
        $before = Get-FileBytesHex -Path $script:DirectPath
        { & $script:Script -InventoryPath $script:Inventory -TestRoot $script:Root -ErrorAction Stop | Out-Null } | Should -Throw -ExpectedMessage '*notFound 1*'
        Get-FileBytesHex -Path $script:DirectPath | Should -Be $before
    }

    It 'classifies VIC-20 consumers to Phase F and the rest to Phase E' {
        $inv = Get-Content -LiteralPath $script:Inventory -Raw | ConvertFrom-Json
        $inv.consumers[0].file = 'Proj/Vic20/DirectTests.cs'
        New-Item -ItemType Directory -Path (Get-MockPath -Root $script:Root -Parts 'Proj', 'Vic20') -Force | Out-Null
        Copy-Item -LiteralPath $script:DirectPath -Destination (Get-MockPath -Root $script:Root -Parts 'Proj', 'Vic20', 'DirectTests.cs')
        $inv | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $script:Inventory -Encoding utf8
        $report = & $script:Script -InventoryPath $script:Inventory -TestRoot $script:Root
        @($report.tagged | Where-Object { $_.method -eq 'Direct_UsesNative' })[0].rebind | Should -BeLike 'Phase F*'
        @($report.tagged | Where-Object { $_.method -eq 'Transitive_ViaHelper' })[0].rebind | Should -BeLike 'Phase E*'
    }

    It 'writes nothing under -WhatIf' {
        $before = Get-FileBytesHex -Path $script:DirectPath
        $reportPath = Get-MockPath -Root $script:Root -Parts 'report.json'
        & $script:Script -InventoryPath $script:Inventory -TestRoot $script:Root -ReportPath $reportPath -WhatIf | Out-Null
        Get-FileBytesHex -Path $script:DirectPath | Should -Be $before
        Test-Path -LiteralPath $reportPath | Should -BeFalse
    }
}
