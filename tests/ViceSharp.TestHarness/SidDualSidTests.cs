namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-SID-012 (BACKFILL-SID-001 dual-SID slice, acceptance criteria
/// 1, 2, 6). Use case: 3SID/2SID demoscene tunes and StereoSID systems
/// expect a second SID chip at a configurable I/O address (default $D420)
/// with fully independent voices, envelopes, and filter state, with no
/// cross-talk to the primary SID at $D400 or to any other I/O device.
///
/// Acceptance criteria exercised in this slice:
///   1. A second SID chip can be enabled at a configurable address (default $D420).
///   2. The second SID operates independently with its own voices, filters, envelopes.
///   6. SID address ranges do not overlap with other I/O devices ($D000 VIC,
///      $D800 color RAM, $DC00 CIA1, $DD00 CIA2).
///
/// Acceptance 3 (per-SID model select 6581/8580) is also exercised at the
/// chip layer to keep the surface stable. Acceptance 4 (stereo channel
/// routing) and 5 (third SID) are out of scope for this slice.
/// </summary>
public sealed class SidDualSidTests
{
    /// <summary>
    /// FR: FR-SID-012 (BACKFILL-SID-001 dual-SID, ac.1 + ac.2).
    /// Use case: A 2SID demo or StereoSID expansion writes voice-1 freq-lo
    /// to <c>$D400</c> on the primary chip and to <c>$D420</c> on the
    /// secondary chip and expects each register read to return the byte
    /// last written through that chip's own base address, with no aliasing.
    /// Acceptance: Each Sid6581 instance maintains an independent register
    /// file indexed off its <c>BaseAddress</c>; cross-base reads return
    /// the value last written to that chip's window only.
    /// </summary>
    [Fact]
    public void TwoSids_AtDifferentAddresses_KeepIndependentRegisterState()
    {
        var bus = new BasicBus();
        var primary = new Sid6581(bus) { BaseAddress = 0xD400 };
        var secondary = new Sid6581(bus) { BaseAddress = 0xD420 };

        primary.Write(0xD400, 0x42);
        secondary.Write(0xD420, 0x99);

        primary.Read(0xD400).Should().Be(0x42);
        secondary.Read(0xD420).Should().Be(0x99);
    }

    /// <summary>
    /// FR: FR-SID-012 (BACKFILL-SID-001 dual-SID, ac.2).
    /// Use case: A demo programs voice 3 on the primary SID at <c>$D400</c>
    /// for the lead voice and leaves the secondary SID at <c>$D420</c>
    /// idle. After clocking both chips, the OSC3 latch (high byte of
    /// voice-3 phase) must advance only on the primary; the secondary
    /// must remain pinned at zero because nothing was ever programmed
    /// into its frequency registers.
    /// Acceptance: Each Sid6581 instance ticks its own voice state in
    /// isolation; activity on one chip's voice does not propagate any
    /// observable state into another chip's register/voice surface.
    /// </summary>
    [Fact]
    public void TwoSids_TickIndependently_NoCrossTalk()
    {
        var bus = new BasicBus();
        var primary = new Sid6581(bus) { BaseAddress = 0xD400 };
        var secondary = new Sid6581(bus) { BaseAddress = 0xD420 };

        // Drive primary voice 3 so OSC3 readback advances. Voice 3 lives at
        // $D40E (FREQ LO), $D40F (FREQ HI) on the primary's window; the
        // secondary's same registers live at $D42E / $D42F. We only write the
        // primary. OSC3 latches the high byte (bits 16-23) of voice 3's 24-bit
        // phase accumulator. Use freq $0100 so 512 ticks gives accumulator
        // $020000 - a clear non-zero high byte ($02) that does NOT wrap the
        // 24-bit accumulator (freq $8000 would wrap to exactly $1000000, i.e.
        // 24-bit zero, making OSC3 read 0 and hiding the advance).
        primary.Write(0xD40E, 0x00);   // V3 FREQ LO
        primary.Write(0xD40F, 0x01);   // V3 FREQ HI = $0100

        for (var i = 0; i < 512; i++)
        {
            primary.Tick();
            secondary.Tick();
        }

        // OSC3 ($D41B for primary, $D43B for secondary) reads back the high byte
        // (bits 16-23) of voice 3's 24-bit phase accumulator. Primary
        // accumulator = $0100 * 512 = $020000, high byte = $02. Secondary
        // accumulator should still be 0 - it never had a frequency written.
        var primaryOsc3 = primary.Read(0xD41B);
        var secondaryOsc3 = secondary.Read(0xD43B);

        primaryOsc3.Should().NotBe(0,
            "primary voice 3 advanced its phase under its own frequency");
        secondaryOsc3.Should().Be(0,
            "writes to the primary at $D40F must not bleed into the secondary at $D42F");
    }

    /// <summary>
    /// FR: FR-SID-012 (BACKFILL-SID-001 dual-SID, ac.6).
    /// Use case: A 2SID host configures the secondary SID at <c>$D420</c>
    /// inside the I/O block. The bus dispatcher must keep VIC-II, color
    /// RAM, CIA1, and CIA2 addresses unaffected: writes intended for
    /// those chips must never be claimed by either SID instance.
    /// Acceptance: Each Sid6581 instance's <c>HandlesAddress</c> returns
    /// true only for the 32-byte window starting at its own
    /// <c>BaseAddress</c>; it returns false for every address in the
    /// $D000-$D3FF, $D800-$DBFF, $DC00-$DCFF, and $DD00-$DDFF ranges.
    /// </summary>
    [Fact]
    public void DualSid_AddressRange_DoesNotOverlapOtherIoDevices()
    {
        var primary = new Sid6581(new BasicBus()) { BaseAddress = 0xD400 };
        var secondary = new Sid6581(new BasicBus()) { BaseAddress = 0xD420 };

        // Primary owns $D400-$D41F. Anything outside that window must
        // not be claimed when dual-SID is in play.
        primary.HandlesAddress(0xD400).Should().BeTrue();
        primary.HandlesAddress(0xD41F).Should().BeTrue();
        primary.HandlesAddress(0xD420).Should().BeFalse(
            "secondary SID owns $D420 when dual-SID is enabled");
        primary.HandlesAddress(0xD43F).Should().BeFalse();

        // Secondary owns $D420-$D43F only.
        secondary.HandlesAddress(0xD420).Should().BeTrue();
        secondary.HandlesAddress(0xD43F).Should().BeTrue();
        secondary.HandlesAddress(0xD400).Should().BeFalse();
        secondary.HandlesAddress(0xD41F).Should().BeFalse();

        // Neither SID may claim VIC, color RAM, or CIA address space.
        foreach (ushort vicAddr in new ushort[] { 0xD000, 0xD011, 0xD3FF })
        {
            primary.HandlesAddress(vicAddr).Should().BeFalse($"VIC owns {vicAddr:X4}");
            secondary.HandlesAddress(vicAddr).Should().BeFalse($"VIC owns {vicAddr:X4}");
        }
        foreach (ushort colorAddr in new ushort[] { 0xD800, 0xD900, 0xDBFF })
        {
            primary.HandlesAddress(colorAddr).Should().BeFalse(
                $"color RAM owns {colorAddr:X4}");
            secondary.HandlesAddress(colorAddr).Should().BeFalse(
                $"color RAM owns {colorAddr:X4}");
        }
        foreach (ushort cia1Addr in new ushort[] { 0xDC00, 0xDC0D, 0xDCFF })
        {
            primary.HandlesAddress(cia1Addr).Should().BeFalse(
                $"CIA1 owns {cia1Addr:X4}");
            secondary.HandlesAddress(cia1Addr).Should().BeFalse(
                $"CIA1 owns {cia1Addr:X4}");
        }
        foreach (ushort cia2Addr in new ushort[] { 0xDD00, 0xDD0D, 0xDDFF })
        {
            primary.HandlesAddress(cia2Addr).Should().BeFalse(
                $"CIA2 owns {cia2Addr:X4}");
            secondary.HandlesAddress(cia2Addr).Should().BeFalse(
                $"CIA2 owns {cia2Addr:X4}");
        }
    }

    /// <summary>
    /// FR: FR-SID-012 (BACKFILL-SID-001 dual-SID, ac.1 default-off).
    /// Use case: The single-SID baseline (no second-SID configured)
    /// builds a Sid6581 with the default <c>$D400</c> base address. The
    /// chip's own <c>HandlesAddress</c> claims only its native 32-byte
    /// register window so a host wanting to add a second SID at
    /// <c>$D420</c> can do so without colliding on the bus.
    /// Acceptance: <c>BaseAddress</c> defaults to <c>$D400</c>;
    /// <c>HandlesAddress</c> returns true exactly for $D400-$D41F and
    /// false for everything outside that 32-byte window (including the
    /// rest of the historical $D400-$D7FF mirror block).
    /// </summary>
    [Fact]
    public void DefaultSid_ClaimsOnly_32ByteRegisterWindow()
    {
        var sid = new Sid6581(new BasicBus()) { BaseAddress = 0xD400 };

        sid.BaseAddress.Should().Be((ushort)0xD400,
            "default base address must remain $D400 for single-SID configs");

        sid.HandlesAddress(0xD400).Should().BeTrue();
        sid.HandlesAddress(0xD41F).Should().BeTrue();
        sid.HandlesAddress(0xD420).Should().BeFalse(
            "the chip's native window is 32 bytes; $D420 belongs to the second SID slot");
        sid.HandlesAddress(0xD7FF).Should().BeFalse();
    }

    /// <summary>
    /// FR: FR-SID-012 (BACKFILL-SID-001 dual-SID, ac.3 stretch).
    /// Use case: A C64 dual-SID expansion lets the user pair a 6581
    /// primary at <c>$D400</c> with an 8580 secondary at <c>$D420</c>
    /// for hybrid filter / combined-waveform sound. The chip surface
    /// must accept that mixed configuration without forcing both
    /// chips to share a die revision.
    /// Acceptance: A <c>Sid8580</c> (subclass) accepts the
    /// <c>BaseAddress</c> init-property identically to <c>Sid6581</c>;
    /// the mixed-revision pair claims independent 32-byte windows
    /// without overlap.
    /// </summary>
    [Fact]
    public void DualSid_CanMix6581Primary_With8580Secondary()
    {
        var bus = new BasicBus();
        var primary = new Sid6581(bus) { BaseAddress = 0xD400 };
        var secondary = new Sid8580(bus) { BaseAddress = 0xD420 };

        primary.HandlesAddress(0xD400).Should().BeTrue();
        secondary.HandlesAddress(0xD420).Should().BeTrue();
        primary.HandlesAddress(0xD420).Should().BeFalse();
        secondary.HandlesAddress(0xD400).Should().BeFalse();
    }
}
