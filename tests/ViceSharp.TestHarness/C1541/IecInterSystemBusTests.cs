namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Chips.IEC;
using Xunit;

/// <summary>
/// FR/TR: ARCH-TRUEDRIVE-1541-001 (Phase B1d-2).
/// Use case: A canonical IEC bus is constructed via the factory and behaves
/// like an InterSystemBus with the four canonical signals (ATN, CLK, DATA,
/// SRQ). Wired-OR endpoint semantics hold; protocol-level handshake (LISTEN
/// / TALK / byte transfer) is left to the device firmware (CIA2 + 1541
/// VIAs) once they're wired in.
/// </summary>
public sealed class IecInterSystemBusTests
{
    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Factory returns a bus named "IEC" carrying ATN, CLK, DATA,
    /// SRQ as signal lines.
    /// Acceptance: bus.Name = "IEC"; bus.Signals contains ATN + CLK + DATA + SRQ.
    /// </summary>
    [Fact]
    public void Factory_ProducesBusWithCanonicalIecSignals()
    {
        var bus = IecInterSystemBus.Create();

        bus.Name.Should().Be("IEC");
        bus.Signals.Should().BeEquivalentTo(new[]
        {
            IecInterSystemBus.Atn,
            IecInterSystemBus.Clk,
            IecInterSystemBus.Data,
            IecInterSystemBus.Srq,
        });
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: All IEC lines start in their idle (high) state.
    /// Acceptance: ReadLine returns true for ATN, CLK, DATA, SRQ on a fresh
    /// bus with no endpoints pulling.
    /// </summary>
    [Fact]
    public void FreshBus_AllLinesIdleHigh()
    {
        var bus = IecInterSystemBus.Create();

        bus.ReadLine(IecInterSystemBus.Atn).Should().BeTrue();
        bus.ReadLine(IecInterSystemBus.Clk).Should().BeTrue();
        bus.ReadLine(IecInterSystemBus.Data).Should().BeTrue();
        bus.ReadLine(IecInterSystemBus.Srq).Should().BeTrue();
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Bus name can be customized for multi-bus topologies (rare,
    /// but possible if the same machine has multiple IEC ports).
    /// Acceptance: Factory honors a supplied name.
    /// </summary>
    [Fact]
    public void Factory_HonorsCustomName()
    {
        var bus = IecInterSystemBus.Create("IEC2");

        bus.Name.Should().Be("IEC2");
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Endpoints registered on the IEC bus participate in wired-OR
    /// resolution exactly as on a generic InterSystemBus.
    /// Acceptance: Two endpoints pull ATN; line stays low until both release.
    /// </summary>
    [Fact]
    public void Endpoints_WiredOr_OnAtn()
    {
        var bus = IecInterSystemBus.Create();
        var host = bus.AttachEndpoint("c64");
        var drive = bus.AttachEndpoint("drive-8");

        host.Pull(IecInterSystemBus.Atn, low: true);
        drive.Pull(IecInterSystemBus.Atn, low: true);
        bus.ReadLine(IecInterSystemBus.Atn).Should().BeFalse();

        host.Pull(IecInterSystemBus.Atn, low: false);
        bus.ReadLine(IecInterSystemBus.Atn).Should().BeFalse();

        drive.Pull(IecInterSystemBus.Atn, low: false);
        bus.ReadLine(IecInterSystemBus.Atn).Should().BeTrue();
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: IEC bus rejects writes to unknown signals; protects against
    /// firmware typos.
    /// Acceptance: Pulling a non-existent "RESET" line throws.
    /// </summary>
    [Fact]
    public void UnknownSignal_RejectedByEndpoint()
    {
        var bus = IecInterSystemBus.Create();
        var ep = bus.AttachEndpoint("c64");

        Assert.Throws<ArgumentException>(() => ep.Pull("RESET", true));
    }
}
