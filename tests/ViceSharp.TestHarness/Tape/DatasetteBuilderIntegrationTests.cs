namespace ViceSharp.TestHarness.Tape;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Tape;
using ViceSharp.Core.Wiring;
using Xunit;

/// <summary>
/// FR/TR: FR-TAPE + FR-CIA (BACKFILL-TAPE builder integration).
/// Use case: BACKFILL-TAPE #78 landed DatasetteCia1FlagBinding as a
/// standalone wire helper, but the C64 ArchitectureBuilder did not
/// actually instantiate it; a built C64 machine therefore would not
/// deliver tape pulses through to CIA1 FLAG under normal Tick()
/// pumping. This slice closes that gap by registering the binding as
/// an IClockedDevice in the C64 build path so the system clock ticks
/// it alongside CIA1, VIC-II, etc.
/// Acceptance: For a C64 profile with TapePortConnected, the built
/// machine exposes both an ITapeDevice and a DatasetteCia1FlagBinding;
/// inserting a TAP image with motor+play and stepping the clock for a
/// pulse's cycle budget latches CIA1 ICR bit 4; with no tape inserted
/// even long Step runs leave CIA1 FLAG quiet.
/// </summary>
[Collection("NativeVice")]
public sealed class DatasetteBuilderIntegrationTests
{
    private const string Signature = "C64-TAPE-RAW";

    private static byte[] BuildTapImage(byte version, byte[] pulseData)
    {
        var buffer = new byte[20 + pulseData.Length];
        var sig = System.Text.Encoding.ASCII.GetBytes(Signature);
        sig.CopyTo(buffer, 0);
        buffer[12] = version;
        var len = pulseData.Length;
        buffer[16] = (byte)(len & 0xFF);
        buffer[17] = (byte)((len >> 8) & 0xFF);
        buffer[18] = (byte)((len >> 16) & 0xFF);
        buffer[19] = (byte)((len >> 24) & 0xFF);
        pulseData.CopyTo(buffer, 20);
        return buffer;
    }

    private static IMachine? TryBuildC64Machine()
    {
        try
        {
            return MachineTestFactory.CreateC64Machine();
        }
        catch (DirectoryNotFoundException)
        {
            // No C64 ROMs available in this worktree; the binding wire
            // shape is still covered by the unit-level DatasetteCia1FlagTests
            // suite. The full-build integration assertions only run when
            // ROMs are present.
            return null;
        }
    }

    /// <summary>
    /// FR/TR: FR-TAPE + FR-CIA (BACKFILL-TAPE builder integration).
    /// Use case: A C64 profile that declares TapePortConnected (the
    /// default "c64" breadbox) must end up with the Datasette AND the
    /// DatasetteCia1FlagBinding registered in the device set, so that
    /// the built machine has a complete pulse-to-FLAG path on every
    /// host phi2 cycle.
    /// Acceptance: After building a default C64 machine, the device
    /// registry contains exactly one ITapeDevice (the Datasette) and
    /// exactly one DatasetteCia1FlagBinding.
    /// </summary>
    [Fact]
    public void DefaultC64Build_RegistersDatasetteAndCia1FlagBinding()
    {
        var machine = TryBuildC64Machine();
        Assert.SkipWhen(machine is null, "C64 ROM set is not available in this environment; integration assertions require BASIC/KERNAL/character ROMs.");

        machine.Devices.All.OfType<ITapeDevice>().Should().HaveCount(1,
            "C64 with TapePortConnected exposes a Datasette");
        machine.Devices.All.OfType<DatasetteCia1FlagBinding>().Should().HaveCount(1,
            "the builder must instantiate the Datasette to CIA1 FLAG binding alongside the Datasette");
    }

    /// <summary>
    /// FR/TR: FR-TAPE + FR-CIA (BACKFILL-TAPE builder integration).
    /// Use case: With a tape inserted, motor on, and play pressed, the
    /// system clock ticking the binding must deliver each tape pulse
    /// through to CIA1's FLAG latch (ICR bit 4) without any extra
    /// caller-side plumbing.
    /// Acceptance: After loading a TAP image with a 4*8=32-cycle first
    /// pulse and stepping the C64 clock for at least 32 cycles, CIA1
    /// ICR reports bit 4 set.
    /// </summary>
    [Fact]
    public void BuiltC64_WithTape_DeliversPulseToCia1Flag()
    {
        var machine = TryBuildC64Machine();
        Assert.SkipWhen(machine is null, "C64 ROM set is not available in this environment; integration assertions require BASIC/KERNAL/character ROMs.");
        var datasette = machine.Devices.All.OfType<Datasette>().Single();
        var cia1 = machine.Devices.GetByRole(DeviceRole.Cia1) as Mos6526
            ?? throw new InvalidOperationException("Built C64 did not expose CIA1.");

        // Single 32-cycle pulse (4 * 8 = 32 cycles).
        datasette.InsertTape(BuildTapImage(version: 0, pulseData: new byte[] { 0x04 }));
        datasette.MotorEnabled = true;
        datasette.PlayPressed = true;

        // Step the clock long enough for the binding to consume the pulse.
        // The binding is Phi2 + divisor 1 so each Step() fires one binding tick.
        machine.Clock.Step(64);

        var icr = cia1.Read(0xDC0D);
        (icr & 0x10).Should().Be(0x10,
            "with motor+play and a tape inserted, the binding ticked by the system clock latches CIA1 FLAG");
    }

    /// <summary>
    /// FR/TR: FR-TAPE + FR-CIA (BACKFILL-TAPE builder integration).
    /// Use case: When no tape is inserted into the built machine, the
    /// system clock can still tick the binding indefinitely without
    /// ever asserting CIA1 FLAG. This isolates the tape path so non
    /// tape software is never disturbed.
    /// Acceptance: After 10_000 Step()s with no tape inserted, CIA1
    /// ICR bit 4 stays clear.
    /// </summary>
    [Fact]
    public void BuiltC64_WithoutTape_NeverLatchesCia1Flag()
    {
        var machine = TryBuildC64Machine();
        Assert.SkipWhen(machine is null, "C64 ROM set is not available in this environment; integration assertions require BASIC/KERNAL/character ROMs.");
        var datasette = machine.Devices.All.OfType<Datasette>().Single();
        var cia1 = machine.Devices.GetByRole(DeviceRole.Cia1) as Mos6526
            ?? throw new InvalidOperationException("Built C64 did not expose CIA1.");

        // No tape inserted; even with motor/play asserted the gate inside
        // Datasette.TryReadNextPulse returns false so no pulses flow.
        datasette.MotorEnabled = true;
        datasette.PlayPressed = true;

        machine.Clock.Step(10_000);

        var icr = cia1.Read(0xDC0D);
        (icr & 0x10).Should().Be(0x00,
            "without a tape, no pulses are produced so CIA1 FLAG never latches");
    }
}
