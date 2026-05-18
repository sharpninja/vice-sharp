namespace ViceSharp.TestHarness.Wiring;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using ViceSharp.Core.Wiring;
using Xunit;

/// <summary>
/// FR/TR: ARCH-WIRING-007 (Phase G2b).
/// Use case: A 1541 drive firmware steps the head via VIA2 PB0/PB1 4-phase
/// Gray code, controls the motor via PB2, and reads sector bytes from the
/// disk via PA. This test set exercises the head-step decoder, motor
/// transitions, and PA byte stream against a known D64 image.
/// </summary>
public sealed class C1541Via2HeadStepTests
{
    private static Via6522 BuildIsolatedVia()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        return new Via6522(bus, irq) { BaseAddress = 0x1C00, Size = 0x0400 };
    }

    private static D64DiskImageDevice MakeImageWithDirectoryMarker()
    {
        var bytes = new byte[D64Image.DiskSize35Track];
        // Sector 18/0 is the BAM; mark first 4 bytes with a recognisable pattern.
        // Sector offset for track 18 sector 0: 17 tracks * 21 sectors_avg... use
        // the D64Image SectorOffset internally; here we just inject via track 18.
        // Track 18 sector 0 starts at offset 0x16500 in the 35-track layout.
        bytes[0x16500] = 0xCA;
        bytes[0x16501] = 0xFE;
        bytes[0x16502] = 0xBA;
        bytes[0x16503] = 0xBE;
        return new D64DiskImageDevice(new D64Image(bytes));
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: Writing PB2=1 (motor on) starts the motor; the binding
    /// resets byte+sector cursors.
    /// Acceptance: Reading PA after motor-on returns the BAM marker bytes
    /// from track 18 sector 0 in order.
    /// </summary>
    [Fact]
    public void MotorOn_PaRead_StreamsBytesFromTrack18Sector0()
    {
        var via = BuildIsolatedVia();
        var disk = MakeImageWithDirectoryMarker();
        C1541Via2BusBinding.Bind(via, disk);
        // DDRA left at default 0 (all inputs) so PortAInput surfaces.

        // PB2 set to enable motor; other bits clear (no step transition).
        via.Write(0x1C02, 0xFF); // DDRB outputs
        via.Write(0x1C00, 0x04); // PB = motor on

        var b0 = via.Read(0x1C01);
        var b1 = via.Read(0x1C01);
        var b2 = via.Read(0x1C01);
        var b3 = via.Read(0x1C01);
        b0.Should().Be(0xCA);
        b1.Should().Be(0xFE);
        b2.Should().Be(0xBA);
        b3.Should().Be(0xBE);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: With motor off, PA reads return the high-Z default (0xFF)
    /// regardless of disk mount.
    /// Acceptance: PA read with PB2=0 returns 0xFF.
    /// </summary>
    [Fact]
    public void MotorOff_PaRead_ReturnsHighZ()
    {
        var via = BuildIsolatedVia();
        var disk = MakeImageWithDirectoryMarker();
        C1541Via2BusBinding.Bind(via, disk);
        via.Write(0x1C02, 0xFF);
        via.Write(0x1C00, 0x00); // motor off

        via.Read(0x1C01).Should().Be(0xFF);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: 4-phase head step inward (00 -> 01 -> 11 -> 10 -> 00)
    /// increments track by 1 per full cycle of 4 phases.
    /// Acceptance: 4-phase Gray sequence inward moves track from 18 to 19.
    /// </summary>
    [Fact]
    public void HeadStep_InwardGraySequence_IncrementsTrack()
    {
        var state = new C1541Via2BusBinding.DriveHeadState();
        state.Track.Should().Be(18);

        state.UpdateHeadStep(0);
        state.UpdateHeadStep(1);
        state.UpdateHeadStep(3);
        state.UpdateHeadStep(2);
        state.UpdateHeadStep(0);

        state.Track.Should().Be(19);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: 4-phase head step outward (00 -> 10 -> 11 -> 01 -> 00)
    /// decrements track by 1.
    /// Acceptance: 4-phase outward sequence moves track from 18 to 17.
    /// </summary>
    [Fact]
    public void HeadStep_OutwardGraySequence_DecrementsTrack()
    {
        var state = new C1541Via2BusBinding.DriveHeadState();

        state.UpdateHeadStep(0);
        state.UpdateHeadStep(2);
        state.UpdateHeadStep(3);
        state.UpdateHeadStep(1);
        state.UpdateHeadStep(0);

        state.Track.Should().Be(17);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: Track is clamped to [1, 35] - cannot step out below 1 or
    /// in beyond 35.
    /// Acceptance: Many outward steps from track 18 stop at 1; many inward
    /// at 35.
    /// </summary>
    [Fact]
    public void HeadStep_Clamps_AtTrack1_And_Track35()
    {
        var stateOut = new C1541Via2BusBinding.DriveHeadState();
        for (int i = 0; i < 50; i++)
        {
            stateOut.UpdateHeadStep(0);
            stateOut.UpdateHeadStep(2);
            stateOut.UpdateHeadStep(3);
            stateOut.UpdateHeadStep(1);
        }
        stateOut.Track.Should().Be(1);

        var stateIn = new C1541Via2BusBinding.DriveHeadState();
        for (int i = 0; i < 50; i++)
        {
            stateIn.UpdateHeadStep(0);
            stateIn.UpdateHeadStep(1);
            stateIn.UpdateHeadStep(3);
            stateIn.UpdateHeadStep(2);
        }
        stateIn.Track.Should().Be(35);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: SectorCount per track follows the 1541 standard zone layout.
    /// Acceptance: 21 sectors on tracks 1-17, 19 on 18-24, 18 on 25-30, 17
    /// on 31-35.
    /// </summary>
    [Theory]
    [InlineData(1, 21)]
    [InlineData(17, 21)]
    [InlineData(18, 19)]
    [InlineData(24, 19)]
    [InlineData(25, 18)]
    [InlineData(30, 18)]
    [InlineData(31, 17)]
    [InlineData(35, 17)]
    public void SectorCount_FollowsZoneLayout(int track, int expected)
    {
        C1541Via2BusBinding.DriveHeadState.SectorCount(track).Should().Be(expected);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: After 256 PA reads on motor-on, the byte cursor rolls over
    /// to the next sector on the same track.
    /// Acceptance: Read 256 bytes from sector 0, then bytes start from
    /// sector 1.
    /// </summary>
    [Fact]
    public void PaRead_AcrossSectorBoundary_AdvancesSector()
    {
        var via = BuildIsolatedVia();
        var bytes = new byte[D64Image.DiskSize35Track];
        // Tag start of sector 0 and sector 1 on track 18 with distinct markers.
        // Track 18 sector 0 @ 0x16500; sector 1 @ 0x16500 + 256 = 0x16600.
        bytes[0x16500] = 0x11;
        bytes[0x16600] = 0x22;
        var disk = new D64DiskImageDevice(new D64Image(bytes));
        C1541Via2BusBinding.Bind(via, disk);

        via.Write(0x1C02, 0xFF);
        via.Write(0x1C00, 0x04); // motor on

        var first = via.Read(0x1C01);
        first.Should().Be(0x11);
        for (int i = 0; i < 255; i++) via.Read(0x1C01); // skip remainder of sector 0
        var next = via.Read(0x1C01);
        next.Should().Be(0x22);
    }
}
