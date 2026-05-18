namespace ViceSharp.TestHarness.UserPort;

using FluentAssertions;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-USERPORT-001 (Phase D1a).
/// Use case: C64 user-port peripherals (RS232 cart, paddles, modems) or
/// peer-to-peer links between two C64s attach to this bus to exchange
/// signal state on PB0..PB7 + handshake lines.
/// </summary>
public sealed class UserPortInterSystemBusTests
{
    /// <summary>
    /// FR/TR: ARCH-USERPORT-001
    /// Use case: Factory returns a bus named "UserPort" with the canonical
    /// 17-signal set.
    /// Acceptance: bus.Name = "UserPort"; Signals contains all PB lines,
    /// PA2, PC2, FLAG2, CNT1/CNT2, SP1/SP2, ATN, RESET.
    /// </summary>
    [Fact]
    public void Factory_ProducesBusWithCanonicalUserPortSignals()
    {
        var bus = UserPortInterSystemBus.Create();

        bus.Name.Should().Be("UserPort");
        bus.Signals.Should().Contain(new[]
        {
            UserPortInterSystemBus.Pb0,
            UserPortInterSystemBus.Pb7,
            UserPortInterSystemBus.Pa2,
            UserPortInterSystemBus.Pc2,
            UserPortInterSystemBus.Flag2,
            UserPortInterSystemBus.Cnt1,
            UserPortInterSystemBus.Cnt2,
            UserPortInterSystemBus.Sp1,
            UserPortInterSystemBus.Sp2,
            UserPortInterSystemBus.Atn,
            UserPortInterSystemBus.Reset,
        });
        bus.Signals.Count.Should().Be(17);
    }

    /// <summary>
    /// FR/TR: ARCH-USERPORT-001
    /// Use case: All user-port pins start idle-high.
    /// Acceptance: every signal reads true on a fresh bus.
    /// </summary>
    [Fact]
    public void FreshBus_AllPinsIdleHigh()
    {
        var bus = UserPortInterSystemBus.Create();

        foreach (var signal in UserPortInterSystemBus.Signals)
            bus.ReadLine(signal).Should().BeTrue($"signal {signal} should idle high");
    }

    /// <summary>
    /// FR/TR: ARCH-USERPORT-001
    /// Use case: PortB read/write helpers pack PB0..PB7 into a byte. With
    /// no endpoint pulling, ReadPortB returns $FF.
    /// Acceptance: Idle bus + read = $FF.
    /// </summary>
    [Fact]
    public void ReadPortB_OnIdleBus_Returns_0xFF()
    {
        var bus = UserPortInterSystemBus.Create();
        var ep = bus.AttachEndpoint("c64");

        UserPortInterSystemBus.ReadPortB(ep).Should().Be(0xFF);
    }

    /// <summary>
    /// FR/TR: ARCH-USERPORT-001
    /// Use case: One endpoint writes a byte to PortB; another reads back
    /// the same byte (wired-OR with a single driver).
    /// Acceptance: Writing $5A via one endpoint reads back $5A on the peer.
    /// </summary>
    [Fact]
    public void WritePortB_OneEndpoint_PeerReadsValue()
    {
        var bus = UserPortInterSystemBus.Create();
        var src = bus.AttachEndpoint("c64-source");
        var peer = bus.AttachEndpoint("c64-peer");

        UserPortInterSystemBus.WritePortB(src, 0x5A);

        UserPortInterSystemBus.ReadPortB(peer).Should().Be(0x5A);
    }

    /// <summary>
    /// FR/TR: ARCH-USERPORT-001
    /// Use case: Both endpoints driving overlapping low bits: result is
    /// wired-OR of the bytes (any bit pulled low by any endpoint reads low).
    /// Acceptance: src writes $0F (bits 4-7 driven low), peer writes $F0
    /// (bits 0-3 driven low): combined ReadPortB = 0x00.
    /// </summary>
    [Fact]
    public void WritePortB_BothEndpoints_ResolvesWiredOr()
    {
        var bus = UserPortInterSystemBus.Create();
        var src = bus.AttachEndpoint("c64-a");
        var peer = bus.AttachEndpoint("c64-b");

        UserPortInterSystemBus.WritePortB(src, 0x0F);
        UserPortInterSystemBus.WritePortB(peer, 0xF0);

        UserPortInterSystemBus.ReadPortB(src).Should().Be(0x00);
        UserPortInterSystemBus.ReadPortB(peer).Should().Be(0x00);
    }

    /// <summary>
    /// FR/TR: ARCH-USERPORT-001
    /// Use case: FLAG2 input toggles when a peripheral asserts; the C64-side
    /// endpoint sees the low pulse. Models the CIA2 FLAG2 interrupt source
    /// for incoming RS232 START bits.
    /// Acceptance: Peripheral pulls FLAG2; C64 endpoint reads FLAG2 low.
    /// </summary>
    [Fact]
    public void Flag2_PulledByPeripheral_HostObservesLow()
    {
        var bus = UserPortInterSystemBus.Create();
        var c64 = bus.AttachEndpoint("c64");
        var modem = bus.AttachEndpoint("vic1011");

        modem.Pull(UserPortInterSystemBus.Flag2, low: true);

        c64.ReadLine(UserPortInterSystemBus.Flag2).Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-USERPORT-001
    /// Use case: Bus rejects writes to unknown signals (catches firmware
    /// typos in peripheral implementations).
    /// Acceptance: Pull on "DSR" (not on the canonical list) throws.
    /// </summary>
    [Fact]
    public void UnknownSignal_Rejected()
    {
        var bus = UserPortInterSystemBus.Create();
        var ep = bus.AttachEndpoint("vic1011");

        Assert.Throws<ArgumentException>(() => ep.Pull("DSR", true));
    }
}
