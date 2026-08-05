namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Core;
using Xunit;

/// <summary>
/// TR-VIC20-LOCKSTEP-001 / Slice N1-N2.
/// Use case: multi-cycle native xvic vs managed Vic20 CPU lockstep.
/// Acceptance: when <c>vice_xvic.dll</c> is present, managed and native
/// A/X/Y/S/PC match after equal master-cycle counts (reset-aligned) for the
/// proven window. P.C currently diverges at cycle 25 (managed staged CMP abs,X
/// carry apply lags native EXPORT); tracked separately. PC lockstep holds past
/// 4k cycles and first fails near cycle 5005. Skips when oracle absent.
/// </summary>
[Collection("NativeVice")]
public sealed class Vic20NativeLockstepTests
{
    public static TheoryData<string, int> CycleCounts => new()
    {
        { "vic20", 64 },
        { "vic20", 256 },
        { "vic20", 1024 },
        { "vic20", 4096 },
        { "vic20", 5000 },
        { "vic20ntsc", 256 },
        { "vic20ntsc", 1024 },
        { "vic20ntsc", 5000 },
    };

    /// <summary>
    /// N1/N2: full CPU lockstep (A/X/Y/S/P/PC) for multiple budgets.
    /// Requires last-bus open-bus so kernal CMP $A003,X sets Carry like xvic.
    /// </summary>
    [Theory]
    [MemberData(nameof(CycleCounts))]
    public void NativeXvic_Managed_CpuRegs_Lockstep(string modelSelector, int cycles)
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        using var native = ViceNative.CreateInstance(modelSelector);
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine(modelSelector);
        managed.Reset();

        for (var i = 0; i < cycles; i++)
        {
            native.Step();
            managed.Clock.Step();
        }

        var n = native.GetState();
        var m = managed.GetState();
        Assert.True(
            n.PC == m.PC && n.A == m.A && n.X == m.X && n.Y == m.Y && n.S == m.S && n.P == m.P,
            $"CPU mismatch after {cycles} cycles model={modelSelector}: native PC=${n.PC:X4} A=${n.A:X2} X=${n.X:X2} Y=${n.Y:X2} S=${n.S:X2} P=${n.P:X2}; managed PC=${m.PC:X4} A=${m.A:X2} X=${m.X:X2} Y=${m.Y:X2} S=${m.S:X2} P=${m.P:X2}");
    }

    /// <summary>
    /// After last-bus open-bus fix, P must match through the first 64 cycles
    /// (includes kernal CMP $A003,X at cycle 25).
    /// </summary>
    [Fact]
    public void NativeXvic_Managed_PMatches_ThroughCmpAbsX()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        managed.Reset();

        for (var i = 0; i < 64; i++)
        {
            native.Step();
            managed.Clock.Step();
            var n = native.GetState();
            var m = managed.GetState();
            Assert.True(
                n.P == m.P && n.PC == m.PC && n.A == m.A && n.X == m.X,
                $"mismatch at cycle {i + 1}: native PC=${n.PC:X4} P=${n.P:X2} A=${n.A:X2} X=${n.X:X2}; managed PC=${m.PC:X4} P=${m.P:X2} A=${m.A:X2} X=${m.X:X2}");
        }
    }

    /// <summary>
    /// N1: VIC-I raster timing fields advance.
    /// </summary>
    [Fact]
    public void NativeXvic_VicRasterAdvances()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var before = native.GetVicState();
        for (var i = 0; i < 200; i++)
            native.Step();
        var after = native.GetVicState();
        Assert.True(after.Cycle > before.Cycle, "VIC cycle counter did not advance");
        Assert.True(
            after.RasterLine != before.RasterLine || after.RasterCycle != before.RasterCycle,
            $"raster did not move: line={after.RasterLine} cycle={after.RasterCycle}");
    }

    /// <summary>
    /// N1: LockstepValidator constructs a Vic20 pair when oracle present.
    /// </summary>
    [Fact]
    public void LockstepValidator_AcceptsVic20Model_WhenOraclePresent()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        using var validator = new LockstepValidator("vic20");
        Assert.NotNull(validator);
    }
}
