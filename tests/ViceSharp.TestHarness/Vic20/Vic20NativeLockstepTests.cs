namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using ViceSharp.Chips.Vic;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// TR-VIC20-LOCKSTEP-001 / Criterion 3.
/// Use case: multi-cycle native xvic vs managed Vic20 lockstep on CPU, dual VIA,
/// and basic VIC-I register samples (production create/step/peek path).
/// Acceptance: when <c>vice_xvic.dll</c> is present, after equal cycle counts
/// managed and native agree on A/X/Y/S/P/PC, VIA1/VIA2 control-register peeks
/// ($9110/$9120 windows), and VIC-I peeks ($9000-$900F static + raster fields).
/// Skips when oracle absent.
/// </summary>
[Collection("NativeVice")]
public sealed class Vic20NativeLockstepTests
{
    private const ushort Via1Base = 0x9110;
    private const ushort Via2Base = 0x9120;
    private const ushort VicBase = 0x9000;

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
    /// Full CPU lockstep (A/X/Y/S/P/PC) for multiple budgets.
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
    /// Criterion 3: dual-VIA control register samples agree after create+step.
    /// Compares DDRA/DDRB, ACR, PCR, IER (offsets 2,3,11,12,14) at $9110 and $9120
    /// via side-effect-free peeks on both sides.
    /// </summary>
    [Theory]
    [InlineData(64)]
    [InlineData(256)]
    [InlineData(1024)]
    public void NativeXvic_Managed_DualVia_ControlRegs_Lockstep(int cycles)
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        managed.Reset();

        for (var i = 0; i < cycles; i++)
        {
            native.Step();
            managed.Clock.Step();
        }

        AssertViaControlRegsAgree(native, managed, Via1Base, "VIA1");
        AssertViaControlRegsAgree(native, managed, Via2Base, "VIA2");
        var vias = managed.Devices.GetAll<Via6522>().OrderBy(v => v.BaseAddress).ToArray();
        Assert.Equal(2, vias.Length);
        Assert.Equal(Via1Base, vias[0].BaseAddress);
        Assert.Equal(Via2Base, vias[1].BaseAddress);
    }

    /// <summary>
    /// Criterion 3: basic VIC-I register samples agree (managed Mos6561 vs native)
    /// during the CPU lockstep window (kernal has not yet programmed $9000; both
    /// start power-on zeros and raster counters advance with matching VICE encoding).
    /// </summary>
    [Theory]
    [InlineData(64)]
    [InlineData(256)]
    [InlineData(1024)]
    [InlineData(5000)]
    public void NativeXvic_Managed_VicI_Regs_Lockstep(int cycles)
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        managed.Reset();

        // Power-on: static control regs agree (zeros on both sides).
        foreach (byte off in new byte[] { 0x00, 0x01, 0x02, 0x05, 0x0E, 0x0F })
        {
            var addr = (ushort)(VicBase + off);
            Assert.Equal(native.PeekBus(addr), managed.Bus.Peek(addr));
        }

        for (var i = 0; i < cycles; i++)
        {
            native.Step();
            managed.Clock.Step();
        }

        // Static regs remain power-on (kernal video init is ~500k cycles later).
        foreach (byte off in new byte[] { 0x00, 0x01, 0x02, 0x05, 0x0F })
        {
            var addr = (ushort)(VicBase + off);
            var n = native.PeekBus(addr);
            var m = managed.Bus.Peek(addr);
            Assert.True(n == m, $"VIC-I ${addr:X4} mismatch after {cycles} cycles: native=${n:X2} managed=${m:X2}");
        }

        Assert.IsType<Mos6561>(managed.Devices.GetByRole(DeviceRole.VideoChip));
        // Raster peeks use VICE encoding ($9004 = line>>1; $9003 bit7 = line bit0).
        Assert.Equal(native.PeekBus((ushort)(VicBase + 0x04)), managed.Bus.Peek((ushort)(VicBase + 0x04)));
        Assert.Equal(native.PeekBus((ushort)(VicBase + 0x03)), managed.Bus.Peek((ushort)(VicBase + 0x03)));

        var nVic = native.GetVicState();
        Assert.True(nVic.RasterLine < 400, $"native raster line out of range: {nVic.RasterLine}");
        if (cycles >= 200)
            Assert.True(nVic.RasterLine > 0 || nVic.RasterCycle > 0, "native raster did not advance");
    }

    /// <summary>
    /// VIC-I raster advances on native and matches managed $9004 peek (VICE encoding).
    /// </summary>
    [Fact]
    public void NativeXvic_VicRasterAdvances_AndMatchesManaged()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        managed.Reset();
        var before = native.GetVicState();
        for (var i = 0; i < 200; i++)
        {
            native.Step();
            managed.Clock.Step();
        }

        var after = native.GetVicState();
        Assert.True(after.Cycle > before.Cycle, "VIC cycle counter did not advance");
        Assert.True(
            after.RasterLine != before.RasterLine || after.RasterCycle != before.RasterCycle,
            $"raster did not move: line={after.RasterLine} cycle={after.RasterCycle}");
        Assert.Equal(
            native.PeekBus((ushort)(VicBase + 0x04)),
            managed.Bus.Peek((ushort)(VicBase + 0x04)));
        Assert.Equal(
            native.PeekBus((ushort)(VicBase + 0x03)),
            managed.Bus.Peek((ushort)(VicBase + 0x03)));
    }

    /// <summary>
    /// LockstepValidator constructs a Vic20 pair when oracle present.
    /// </summary>
    [Fact]
    public void LockstepValidator_AcceptsVic20Model_WhenOraclePresent()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        using var validator = new LockstepValidator("vic20");
        Assert.NotNull(validator);
    }

    private static void AssertViaControlRegsAgree(IViceNative native, IMachine managed, ushort baseAddress, string label)
    {
        // Offsets: 2=DDRA, 3=DDRB, 11=ACR, 12=PCR, 14=IER.
        byte[] offsets = [2, 3, 11, 12, 14];
        foreach (var off in offsets)
        {
            var addr = (ushort)(baseAddress + off);
            var n = native.PeekBus(addr);
            var m = managed.Bus.Peek(addr);
            Assert.True(n == m, $"{label} ${addr:X4} mismatch: native=${n:X2} managed=${m:X2}");
        }
    }
}
