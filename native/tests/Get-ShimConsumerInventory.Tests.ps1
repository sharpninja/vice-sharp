#Requires -Version 7.0
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.7.0' }

# PLAN-NATIVERESTORE-001 D0 semantic per-method shim consumer inventory (R6, round 2 F03, round 3b N06):
# contract of native/Get-ShimConsumerInventory.ps1. Tag MockGate proves the four controls on synthetic C# files:
# a plain-Fact method using ViceNative directly is reported; a method whose only path to the shim is test to helper
# to provider is reported (transitive); a managed fake implementing IViceNative with no native use is not reported;
# a string-only or comment-only mention is not reported; ViceFact and ViceTheory attributes are reported.

BeforeAll {
    $script:ScriptPath = (Resolve-Path -LiteralPath (Join-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath '..') -ChildPath 'Get-ShimConsumerInventory.ps1')).Path

    function Write-CsFile {
        param([string]$Root, [string]$Name, [string]$Content)
        $path = Join-Path $Root $Name
        New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
        [IO.File]::WriteAllText($path, $Content, [Text.UTF8Encoding]::new($false))
    }
}

Describe 'Get-ShimConsumerInventory controls' -Tag MockGate {
    BeforeEach {
        $script:Work = Join-Path ([IO.Path]::GetTempPath()) ('sci-' + [Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $script:Work -Force | Out-Null
        Write-CsFile -Root $script:Work -Name 'ProjT/DirectTests.cs' -Content @'
namespace T;
using Xunit;
public sealed class DirectTests
{
    [Fact]
    public void PlainFact_UsesNativeDirectly()
    {
        using var vice = ViceNative.CreateInstance("x64sc");
        Assert.NotNull(vice);
    }

    [Fact]
    public void PlainFact_PureManaged()
    {
        Assert.Equal(2, 1 + 1);
    }

    [ViceFact]
    public void AttributeBound()
    {
        Assert.True(true);
    }

    [ViceTheory]
    [InlineData(1)]
    public void TheoryBound(int x)
    {
        Assert.True(x > 0);
    }
}
'@
        Write-CsFile -Root $script:Work -Name 'ProjT/Helpers/OracleProvider.cs' -Content @'
namespace T.Helpers;
public static class OracleProvider
{
    public static object Open() => ViceNativeXvic.CreateInstance();
}
'@
        Write-CsFile -Root $script:Work -Name 'ProjT/Helpers/LockstepHelper.cs' -Content @'
namespace T.Helpers;
public static class LockstepHelper
{
    public static object Session() => OracleProvider.Open();
}
'@
        Write-CsFile -Root $script:Work -Name 'ProjT/TransitiveTests.cs' -Content @'
namespace T;
using Xunit;
using T.Helpers;
public sealed class TransitiveTests
{
    [Fact]
    public void HelperOnly_ReachesShimThroughProvider()
    {
        var s = LockstepHelper.Session();
        Assert.NotNull(s);
    }
}
'@
        Write-CsFile -Root $script:Work -Name 'ProjT/FakeTests.cs' -Content @'
namespace T;
using Xunit;
public sealed class FakeVice : IViceNative
{
    public int WriteSnapshot(string path) => 0;
}
public sealed class FakeTests
{
    [Fact]
    public void ManagedFake_NoNativeLoad()
    {
        IViceNative fake = new FakeVice();
        Assert.Equal(0, fake.WriteSnapshot("x"));
    }
}
'@
        Write-CsFile -Root $script:Work -Name 'ProjT/StringOnlyTests.cs' -Content @'
namespace T;
using Xunit;
/// <summary>Documentation that mentions ViceNative.CreateInstance and [ViceFact] without using them.</summary>
public sealed class StringOnlyTests
{
    // comment: ViceNativeBridge is not used here
    [Fact]
    public void StringOnly_MentionsNamesInLiterals()
    {
        var sample = "[ViceFact] ViceNative.CreateInstance XvicOraclePrerequisites";
        Assert.Contains("ViceFact", sample);
    }
}
'@
    }
    AfterEach { if (Test-Path -LiteralPath $script:Work) { Remove-Item -LiteralPath $script:Work -Recurse -Force } }

    It 'reports direct, attribute-bound and transitive consumers and nothing else' {
        $r = & $script:ScriptPath -TestRoot $script:Work
        $names = @($r.consumers | ForEach-Object { $_.method })
        $names | Should -Contain 'PlainFact_UsesNativeDirectly'
        $names | Should -Contain 'AttributeBound'
        $names | Should -Contain 'TheoryBound'
        $names | Should -Contain 'HelperOnly_ReachesShimThroughProvider'
        $names | Should -Not -Contain 'PlainFact_PureManaged'
        $names | Should -Not -Contain 'ManagedFake_NoNativeLoad'
        $names | Should -Not -Contain 'StringOnly_MentionsNamesInLiterals'
        $r.consumers.Count | Should -Be 4
        ($r.consumers | Where-Object method -eq 'PlainFact_UsesNativeDirectly').reason | Should -Be 'direct'
        ($r.consumers | Where-Object method -eq 'AttributeBound').reason | Should -Be 'attribute'
        ($r.consumers | Where-Object method -eq 'HelperOnly_ReachesShimThroughProvider').reason | Should -Be 'transitive'
        ($r.consumers | Where-Object method -eq 'HelperOnly_ReachesShimThroughProvider').via | Should -Be 'LockstepHelper'
        @($r.helpers | ForEach-Object { $_.type }) | Should -Contain 'OracleProvider'
        @($r.helpers | ForEach-Object { $_.type }) | Should -Contain 'LockstepHelper'
        @($r.helpers | ForEach-Object { $_.type }) | Should -Not -Contain 'FakeVice'
        $r.counts.files | Should -Be 6
        $r.counts.testMethods | Should -Be 7
        $r.counts.consumers | Should -Be 4
    }

    It 'does not let a helper type name leak across projects and ignores typeof and nameof reflection' {
        Write-CsFile -Root $script:Work -Name 'ProjA/Program.cs' -Content @'
namespace A;
public static class Program
{
    public static void Main() { ViceNative.CreateInstance("x64sc"); }
}
'@
        Write-CsFile -Root $script:Work -Name 'ProjB/OtherTests.cs' -Content @'
namespace B;
using Xunit;
public sealed class Program { public static int Run() => 1; }
public sealed class OtherTests
{
    [Fact]
    public void SameName_OtherProject_IsManaged()
    {
        Assert.Equal(1, Program.Run());
    }
}
'@
        Write-CsFile -Root $script:Work -Name 'ProjT/ReflectionTests.cs' -Content @'
namespace T;
using Xunit;
using T.Helpers;
public sealed class ReflectionTests
{
    [Fact]
    public void TypeofOnly_DoesNotExecuteHelper()
    {
        var t = typeof(LockstepHelper);
        Assert.Equal("LockstepHelper", nameof(LockstepHelper));
        Assert.NotNull(t);
    }
}
'@
        $r = & $script:ScriptPath -TestRoot $script:Work
        $names = @($r.consumers | ForEach-Object { $_.method })
        $names | Should -Not -Contain 'SameName_OtherProject_IsManaged'
        $names | Should -Not -Contain 'TypeofOnly_DoesNotExecuteHelper'
        @($r.helpers | Where-Object { $_.type -eq 'Program' -and $_.project -eq 'ProjA' }).Count | Should -Be 1
        @($r.helpers | Where-Object { $_.type -eq 'Program' -and $_.project -eq 'ProjB' }).Count | Should -Be 0
        $r.counts.consumers | Should -Be 4
    }

    It 'lists each consumer with its file, class, method and attribute' {
        $r = & $script:ScriptPath -TestRoot $script:Work
        $c = $r.consumers | Where-Object method -eq 'TheoryBound'
        $c.file | Should -Be 'ProjT/DirectTests.cs'
        $c.class | Should -Be 'DirectTests'
        $c.attribute | Should -Be 'ViceTheory'
    }
}
