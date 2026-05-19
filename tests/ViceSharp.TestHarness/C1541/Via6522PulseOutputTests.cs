namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIA (BACKFILL-VIA CA2/CB2 pulse output).
/// Use case: 1541 + IEC drivers configure CA2 or CB2 in PCR mode 101 to emit
/// a one-cycle low pulse on a Port A read (CA2) or Port B output write (CB2),
/// returning high automatically on the next phi2 tick. This slice closes the
/// final deferred CA2/CB2 mode left after commits b154dbe (CA2 handshake) and
/// d6a703b (CB2 handshake).
/// </summary>
public sealed class Via6522PulseOutputTests
{
    private const ushort Base = 0x1800;

    private static (Via6522 via, InterruptLine irq) CreateVia()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var via = new Via6522(bus, irq) { BaseAddress = Base, Size = 0x0400 };
        via.Reset();
        return (via, irq);
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA CA2 pulse output).
    /// Use case: PCR bits 1-3 = 101 selects CA2 pulse output. CA2 idles high
    /// and drops low for exactly one phi2 cycle when the CPU reads ORA/IRA
    /// ($01); the next Tick restores it to high automatically.
    /// Acceptance: PCR = 0x0A, read $1801 -&gt; Ca2State = false; Tick() -&gt;
    /// Ca2State = true.
    /// </summary>
    [Fact]
    public void PcrCa2PulseOut_OraReadDropsLow_NextTickRestoresHigh()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0C, 0x0A); // PCR bits 3..1 = 101 (CA2 pulse out)

        via.Ca2State.Should().BeTrue("pulse mode idles high");

        _ = via.Read(Base + 0x01); // ORA read triggers the pulse
        via.Ca2State.Should().BeFalse("pulse drops the line low for one cycle");

        via.Tick();
        via.Ca2State.Should().BeTrue("pulse expires after one phi2 tick");
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA CB2 pulse output).
    /// Use case: PCR bits 5-7 = 101 selects CB2 pulse output. CB2 idles high
    /// and drops low for exactly one phi2 cycle when the CPU writes ORB ($00);
    /// the next Tick restores it to high automatically.
    /// Acceptance: PCR = 0xA0, write $1800 -&gt; Cb2State = false; Tick() -&gt;
    /// Cb2State = true.
    /// </summary>
    [Fact]
    public void PcrCb2PulseOut_OrbWriteDropsLow_NextTickRestoresHigh()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0C, 0xA0); // PCR bits 7..5 = 101 (CB2 pulse out)

        via.Cb2State.Should().BeTrue("pulse mode idles high");

        via.Write(Base + 0x00, 0x55); // ORB write triggers the pulse
        via.Cb2State.Should().BeFalse("pulse drops the line low for one cycle");

        via.Tick();
        via.Cb2State.Should().BeTrue("pulse expires after one phi2 tick");
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA CB2 pulse output).
    /// Use case: With CB2 in pulse-output mode but no ORB write, the line must
    /// remain at the idle high level across many phi2 ticks. Pulse mode does
    /// not free-run; it only fires on the trigger event.
    /// Acceptance: PCR = 0xA0, no ORB write, 16 Tick() calls -&gt; Cb2State stays
    /// true throughout.
    /// </summary>
    [Fact]
    public void PcrCb2PulseOut_NoTrigger_KeepsCb2High()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0C, 0xA0); // PCR bits 7..5 = 101 (CB2 pulse out)

        for (var i = 0; i < 16; i++)
        {
            via.Tick();
            via.Cb2State.Should().BeTrue("pulse mode without ORB write must idle high");
        }
    }

    /// <summary>
    /// FR/TR: FR-VIA (BACKFILL-VIA CB2 pulse output).
    /// Use case: Each ORB write in CB2 pulse-output mode emits a fresh
    /// one-cycle low pulse. Successive writes after the line has restored
    /// must re-arm the pulse.
    /// Acceptance: Two trigger / tick cycles each drive Cb2State low after
    /// the ORB write and back to high after a single Tick().
    /// </summary>
    [Fact]
    public void PcrCb2PulseOut_RepeatedOrbWrites_EachEmitPulse()
    {
        var (via, _) = CreateVia();
        via.Write(Base + 0x0C, 0xA0); // PCR bits 7..5 = 101 (CB2 pulse out)

        via.Write(Base + 0x00, 0x11); // first trigger
        via.Cb2State.Should().BeFalse();
        via.Tick();
        via.Cb2State.Should().BeTrue();

        via.Write(Base + 0x00, 0x22); // second trigger re-arms pulse
        via.Cb2State.Should().BeFalse();
        via.Tick();
        via.Cb2State.Should().BeTrue();
    }
}
